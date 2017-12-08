// Copyright � 2004, 2016, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;


namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for MySqlPoolManager.
  /// </summary>
  internal class MySqlPoolManager
  {
    private static readonly Dictionary<string, MySqlPool> Pools = new Dictionary<string, MySqlPool>();
    private static readonly List<MySqlPool> ClearingPools = new List<MySqlPool>();

    // Timeout in seconds, after which an unused (idle) connection 
    // should be closed.
    internal static int maxConnectionIdleTime = 180;


#if !NETSTANDARD1_3
    static MySqlPoolManager()
    {
      AppDomain.CurrentDomain.ProcessExit += EnsureClearingPools;
      AppDomain.CurrentDomain.DomainUnload += EnsureClearingPools;
    }

    private static void EnsureClearingPools( object sender, EventArgs e )
    {
      ClearAllPools();
    }
#endif


    // we add a small amount to the due time to let the cleanup detect
    //expired connections in the first cleanup.
    private static Timer timer = new Timer(CleanIdleConnections,
      null, (maxConnectionIdleTime * 1000) + 8000, maxConnectionIdleTime * 1000);
 
    private static string GetKey(MySqlConnectionStringBuilder settings)
    {
      string key = "";
      lock (settings)
      {
        key = settings.ConnectionString;
      }

      if (!settings.IntegratedSecurity || settings.ConnectionReset) return key;

      try
      {
        // Append SID to the connection string to generate a key
        // With Integrated security different Windows users with the same
        // connection string may be mapped to different MySQL accounts.
        System.Security.Principal.WindowsIdentity id =
          System.Security.Principal.WindowsIdentity.GetCurrent();

        key += ";" + id.User;
      }
      catch (System.Security.SecurityException ex)
      {
        // Documentation for WindowsIdentity.GetCurrent() states 
        // SecurityException can be thrown. In this case the 
        // connection can only be pooled if reset is done.
        throw new MySqlException(Resources.NoWindowsIdentity, ex);
      }

      return key;
    }
    public static MySqlPool GetPool(MySqlConnectionStringBuilder settings)
    {
      string text = GetKey(settings);

      lock (Pools)
      {
        MySqlPool pool;
        Pools.TryGetValue(text, out pool);

        if (pool == null)
        {
          pool = new MySqlPool(settings);
          Pools.Add(text, pool);
        }
        else
          pool.Settings = settings;

        return pool;
      }
    }

    public static void RemoveConnection(Driver driver)
    {
      Debug.Assert(driver != null);

      MySqlPool pool = driver.Pool;

      pool?.RemoveConnection(driver);
    }

    public static void ReleaseConnection(Driver driver)
    {
      Debug.Assert(driver != null);

      MySqlPool pool = driver.Pool;

      pool?.ReleaseConnection(driver);
    }

    public static void ClearPool(MySqlConnectionStringBuilder settings)
    {
      Debug.Assert(settings != null);
      string text;
      try
      {
        text = GetKey(settings);
      }
      catch (MySqlException)
      {
        // Cannot retrieve windows identity for IntegratedSecurity=true
        // This can be ignored.
        return;
      }
      ClearPoolByText(text);
    }

    private static void ClearPoolByText(string key)
    {
      lock (Pools)
      {
        // if pools doesn't have it, then this pool must already have been cleared
        if (!Pools.ContainsKey(key)) return;

        // add the pool to our list of pools being cleared
        MySqlPool pool = (Pools[key] as MySqlPool);
        ClearingPools.Add(pool);

        // now tell the pool to clear itself
        pool.Clear();

        // and then remove the pool from the active pools list
        Pools.Remove(key);
      }
    }

    public static void ClearAllPools()
    {
      lock (Pools)
      {
        // Create separate keys list.
        List<string> keys = new List<string>(Pools.Count);
        keys.AddRange(Pools.Keys);

        // Remove all pools by key.
        foreach (string key in keys)
          ClearPoolByText(key);
      }
    }

    public static void RemoveClearedPool(MySqlPool pool)
    {
      Debug.Assert(ClearingPools.Contains(pool));
      ClearingPools.Remove(pool);
    }

    /// <summary>
    /// Remove drivers that have been idle for too long.
    /// </summary>
    public static void CleanIdleConnections(object obj)
    {
      List<Driver> oldDrivers = new List<Driver>();
      lock (Pools)
      {
        foreach (MySqlPool pool in Pools.Keys.Select(key => Pools[key]))
        {
          oldDrivers.AddRange(pool.RemoveOldIdleConnections());
        }
      }
      foreach (Driver driver in oldDrivers)
      {
        driver.Close();
      }
    }
  }
}