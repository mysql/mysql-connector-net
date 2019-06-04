// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System.Collections.Generic;
using MySqlX.XDevAPI.Common;


namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a chaining table insert statement.
  /// </summary>
  public class TableInsertStatement : TargetedBaseStatement<Table, Result>
  {
    internal string[] fields;
    internal List<object[]> values = new List<object[]>();
    internal object[] parameters;

    internal TableInsertStatement(Table table, string[] fields) : base(table)
    {
      this.fields = fields;
    }

    /// <summary>
    /// Executes the insert statement.
    /// </summary>
    /// <returns>A <see cref="Result"/> object containing the results of the insert statement.</returns>
    public override Result Execute()
    {
      ValidateOpenSession();
      return Target.Session.XSession.InsertRows(this);
    }

    /// <summary>
    /// Values to be inserted.
    /// Multiple rows supported.
    /// </summary>
    /// <param name="values">The values to be inserted.</param>
    /// <returns>This same <see cref="TableInsertStatement"/> object.</returns>
    public TableInsertStatement Values(params object[] values)
    {
      this.values.Add(values);
      return this;
    }
  }
}
