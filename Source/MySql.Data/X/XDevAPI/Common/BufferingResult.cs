// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using MySqlX.XDevAPI.Relational;
using MySqlX.Session;
using System.Collections.ObjectModel;
using MySql.Data.MySqlClient;
using MySqlX.Properties;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Abstract class for Buffered results
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public abstract class BufferingResult<T> : BaseResult, IEnumerable<T>, IEnumerator<T>
  {
    protected int _position;
    protected List<T> _items = new List<T>();
    protected bool _isComplete;
    Dictionary<string, int> _nameMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal List<Column> _columns = null;


    internal BufferingResult(InternalSession session) : base(session)
    {
      LoadCoumnData();
      PageSize = 20;
      _position = -1;
    }

    protected Dictionary<string, int> NameMap
    {
      get { return _nameMap;  }
    }

    public int PageSize { get; private set; }

    protected void LoadCoumnData()
    {
      _columns = new List<Column>();
      if (_hasData)
      {
        _columns = Protocol.LoadColumnMetadata();
        if (_columns.Count == 0)
          _hasData = false;
        for (int i = 0; i < _columns.Count; i++)
          _nameMap.Add(_columns[i].ColumnName ?? _columns[i].ColumnLabel, i);
      }
      else
        Protocol.CloseResult(this);
    }

    public ReadOnlyCollection<T> FetchAll()
    {
      while (PageInItems()) ;
      return _items.AsReadOnly();
    }

    internal void Dump()
    {
      if (_isComplete) return;
      while (true)
      {
        if (ReadItem(true) == null) break;
      }
      _isComplete = true;
    }

    public T FetchOne()
    {
      if (!Next())
        return default(T);
      return Current;
    }

    public bool Next()
    {
      _position++;
      if (_position >= _items.Count)
      {
        if (_isComplete) return false;
        if (!PageInItems())
        {
          _isComplete = true;
          return false;
        }
      }
      return true;
    }

    protected abstract T ReadItem(bool dumping);

    private bool PageInItems()
    {
      if (_isComplete) return false;
      int count = 0;
      for (int i = 0; i < PageSize; i++)
      {
        T item = ReadItem(false);
        if (item == null)
        {
          _isComplete = !_hasData;
          _session.ActiveResult = null;
          break;
        }
        _items.Add(item);
        count++;
      }
      return count > 0;
    }

    public T Current
    {
      get 
      {
        if (_position == _items.Count)
          throw new InvalidOperationException(String.Format(ResourcesX.NoDataAtIndex, _position));
        return _items[_position];
      }
    }

    object IEnumerator.Current
    {
      get { return this.Current;  }
    }

    public bool MoveNext()
    {
      return Next();
    }

    public void Reset()
    {
      _position = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
      return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this;
    }

    protected override void Buffer()
    {
      FetchAll();
    }

    public void Dispose()
    {
    }
  }
}
