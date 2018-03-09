// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MySql.Data;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Represents a general statement result.
  /// </summary>
  public class Result : BaseResult
  {
    internal Result(InternalSession session) : base(session)
    {
      if (session == null) return;
      GeneratedIds = new ReadOnlyCollection<string>(_documentIds);
      session.GetProtocol().CloseResult(this);
    }

    /// <summary>
    /// Gets the number of records affected by the statement that generated this result.
    /// </summary>
    public UInt64 RecordsAffected
    {
      get { return _recordsAffected; }
    }

    /// <summary>
    /// Gets the last inserted identifier (if there is one) by the statement that generated this result.
    /// </summary>
    public UInt64 AutoIncrementValue
    {
      get { return _autoIncrementValue; }
    }

    /// <summary>
    /// Gets the list of generated identifiers in the order of the Add() calls.
    /// </summary>
    public ReadOnlyCollection<string> GeneratedIds { get; }
  }
}
