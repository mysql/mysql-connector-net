// Copyright � 2016, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.Data.MySqlClient
{
  internal class TableCache
  {
    private static readonly BaseTableCache cache;

    static TableCache()
    {
      cache = new BaseTableCache(480 /* 8 hour max by default */);
    }

    public static void AddToCache(string commandText, ResultSet resultSet)
    {
      cache.AddToCache(commandText, resultSet);
    }

    public static ResultSet RetrieveFromCache(string commandText, int cacheAge)
    {
      return (ResultSet)cache.RetrieveFromCache(commandText, cacheAge);
    }

    public static void RemoveFromCache(string commandText)
    {
      cache.RemoveFromCache(commandText);
    }

    public static void DumpCache()
    {
      cache.Dump();
    }
  }

  public class BaseTableCache
  {
    protected int MaxCacheAge;
    private Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();

    public BaseTableCache(int maxCacheAge)
    {
      MaxCacheAge = maxCacheAge;
    }

    public virtual void AddToCache(string commandText, object resultSet)
    {
      CleanCache();
      CacheEntry entry = new CacheEntry();
      entry.CacheTime = DateTime.Now;
      entry.CacheElement = resultSet;
      lock (cache)
      {
        if (cache.ContainsKey(commandText)) return;
        cache.Add(commandText, entry);
      }
    }

    public virtual object RetrieveFromCache(string commandText, int cacheAge)
    {
      CleanCache();
      lock (cache)
      {
        if (!cache.ContainsKey(commandText)) return null;
        CacheEntry entry = cache[commandText];
        if (DateTime.Now.Subtract(entry.CacheTime).TotalSeconds > cacheAge) return null;
        return entry.CacheElement;
      }
    }

    public void RemoveFromCache(string commandText)
    {
      lock (cache)
      {
        if (!cache.ContainsKey(commandText)) return;
        cache.Remove(commandText);
      }
    }

    public virtual void Dump()
    {
      lock (cache)
        cache.Clear();
    }

    protected virtual void CleanCache()
    {
      DateTime now = DateTime.Now;
      List<string> keysToRemove = new List<string>();

      lock (cache)
      {
        keysToRemove.AddRange(from key in cache.Keys let diff = now.Subtract(cache[key].CacheTime) where diff.TotalSeconds > MaxCacheAge select key);

        foreach (string key in keysToRemove)
          cache.Remove(key);
      }
    }

    private struct CacheEntry
    {
      public DateTime CacheTime;
      public object CacheElement;
    }
  }

}