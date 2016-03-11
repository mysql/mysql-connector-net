// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Session;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a resultset that contains rows of data for relational operations.
  /// </summary>
  public class SqlResult : InternalRowResult
  {
    internal SqlResult(InternalSession session) : base(session)
    {

    }

    /// <summary>
    /// Property that indicates if there result has data.
    /// </summary>
    public bool HasData
    {
      get { return _hasData;  }
    }

    /// <summary>
    /// The number of records affected by the statement that generated this result.
    /// </summary>
    public UInt64 RecordsAffected
    {
      get { return _recordsAffected; }
    }

    /// <summary>
    /// The last inserted id (if there is one) by the statement that generated this result.
    /// </summary>
    public UInt64 AutoIncrementValue
    {
      get { return _autoIncrementValue; }
    }

    /// <summary>
    /// Moves to next resultset
    /// </summary>
    /// <returns>True if there is a new resultset</returns>
    public bool NextResult()
    {
      if (!_hasMoreResults)
        return false;
      _hasData = Protocol.HasData();
      LoadCoumnData();
      _isComplete = !_hasData;
      _position = -1;
      _items.Clear();
      return _hasData;
    }
  }
}
