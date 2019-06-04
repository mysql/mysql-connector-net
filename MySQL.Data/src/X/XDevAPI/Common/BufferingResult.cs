// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using MySqlX.XDevAPI.Relational;
using MySqlX.Sessions;
using System.Collections.ObjectModel;
using MySql.Data;
using MySqlX;
using MySql.Data.MySqlClient;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Abstract class for buffered results.
  /// </summary>
  /// <typeparam name="T">Generic result type.</typeparam>
  public abstract class BufferingResult<T> : BaseResult, IEnumerable<T>, IEnumerator<T>
  {
    /// <summary>
    /// Index of the current item.
    /// </summary>
    protected int _position;
    /// <summary>
    /// List of generic items in this buffered result.
    /// </summary>
    protected List<T> _items = new List<T>();
    /// <summary>
    /// Flag that indicates if all items have been read.
    /// </summary>
    protected bool _isComplete;
    Dictionary<string, int> _nameMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal List<Column> _columns = null;


    internal BufferingResult(InternalSession session) : base(session)
    {
      LoadColumnData();
      PageSize = 20;
      _position = -1;
    }

    /// <summary>
    /// Gets a dictionary containing the column names and their index.
    /// </summary>
    protected Dictionary<string, int> NameMap
    {
      get { return _nameMap;  }
    }

    /// <summary>
    /// Gets the page size set for this buffered result.
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// Loads the column data into the <see cref="_nameMap"/> field.
    /// </summary>
    protected void LoadColumnData()
    {
      _columns = new List<Column>();
      if (_hasData)
      {
        _columns = Protocol.LoadColumnMetadata();
        if (_columns.Count == 0)
          _hasData = false;
        for (int i = 0; i < _columns.Count; i++)
          _nameMap.Add(_columns[i].ColumnLabel ?? _columns[i].ColumnName, i);
      }
      else
        Protocol.CloseResult(this);
    }

    /// <summary>
    /// Retrieves a read-only list of the generic items associated to this buffered result.
    /// </summary>
    /// <returns>A generic <see cref="ReadOnlyCollection{T}"/> list representing items in this buffered result.</returns>
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

    /// <summary>
    /// Retrieves one element from the generic items associated to this buffered result.
    /// </summary>
    /// <returns>A generic object that corresponds to the current or default item.</returns>
    public T FetchOne()
    {
      if (!Next())
        return default(T);
      return Current;
    }

    /// <summary>
    /// Determines if all items have already been read.
    /// </summary>
    /// <returns>True if all items have been retrived, false otherwise.</returns>
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

    /// <summary>
    /// Gets the current item.
    /// </summary>
    /// <exception cref="InvalidOperationException">All items have already been read.</exception>
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

    /// <summary>
    /// Determines if all items have already been read.
    /// </summary>
    /// <returns>True if all items have been retrived, false otherwise.</returns>
    public bool MoveNext()
    {
      return Next();
    }

    /// <summary>
    /// Resets the value of the <see cref="_position"/> field to zero.
    /// </summary>
    public void Reset()
    {
      _position = 0;
    }

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> representation of this object.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{T}"/> representation of this object.</returns>
    public IEnumerator<T> GetEnumerator()
    {
      return this;
    }

    /// <summary>
    /// Gets an <see cref="IEnumerator"/> representation of this object.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> representation of this object.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return this;
    }

    /// <summary>
    /// Retrieves a read-only list of the generic items associated to this buffered result.
    /// </summary>
    /// <returns>A generic <see cref="ReadOnlyCollection{T}"/> list representing items in this buffered result.</returns>
    protected override void Buffer()
    {
      FetchAll();
    }

    /// <summary>
    /// No body has been defined for this method.
    /// </summary>
    public void Dispose()
    {
    }
  }
}
