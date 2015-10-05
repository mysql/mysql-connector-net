// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Properties;
using MySql.XDevAPI.Relational;
using MySql.Session;

namespace MySql.XDevAPI.Common
{
  /// <summary>
  /// Abstract class for Buffered results
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public abstract class BufferingResult<T> : BaseResult, IEnumerable<T>, IEnumerator<T>
  {
    int _position;
    List<T> _items = new List<T>();
    protected bool _isComplete;
    Dictionary<string, int> _nameMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal List<Column> _columns = new List<Column>();


    internal BufferingResult(InternalSession session) : base(session)
    {
      _columns = Protocol.LoadColumnMetadata();
      for (int i = 0; i < _columns.Count; i++)
        _nameMap.Add(_columns[i].Name, i);

      //      _autoClose = autoClose;
      PageSize = 20;
      _position = -1;
    }

    protected Dictionary<string, int> NameMap
    {
      get { return _nameMap;  }
    }

    protected int Position
    {
      get { return _position;  }
    }

    protected List<T> Items
    {
      get { return _items;  }
    }

    public int PageSize { get; private set; }

    public void Buffer()
    {
      while (!_isComplete)
        PageInItems() ;
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
        throw new MySqlException(Resources.NoMoreData);
      return Current;
    }

    public bool Next()
    {
      _position++;
      if (Position == _items.Count)
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
      int count = 0;
      for (int i = 0; i < PageSize; i++)
      {
        T item = ReadItem(false);
        if (item == null)
        {
          _isComplete = true;
          break;
        }
        _items.Add(item);
        count++;
      }
      return count > 0;
    }

    public void Dispose()
    {
    }

    public T Current
    {
      get 
      {
        if (_position == _items.Count)
          throw new InvalidOperationException(String.Format(Resources.NoDataAtIndex, _position));
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
  }
}
