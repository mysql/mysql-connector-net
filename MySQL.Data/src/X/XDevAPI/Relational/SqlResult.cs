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
using MySqlX.Sessions;

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
    /// Gets a boolean value indicating if this result has data.
    /// </summary>
    public bool HasData
    {
      get { return _hasData;  }
    }

    /// <summary>
    /// Moves to next resultset.
    /// </summary>
    /// <returns>True if there is a new resultset, false otherwise.</returns>
    public bool NextResult()
    {
      if (!_hasMoreResults)
        return false;
      _hasData = Protocol.HasData(this);
      LoadColumnData();
      _isComplete = !_hasData;
      _position = -1;
      _items.Clear();
      return _hasData;
    }
  }
}
