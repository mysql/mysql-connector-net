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

using MySqlX.XDevAPI.Common;
using System.Collections.Generic;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a sql statement.
  /// </summary>
  public class SqlStatement : BaseStatement<SqlResult>
  {
    /// <summary>
    /// Initializes a new instance of the SqlStament class bassed on the session and sql statement.
    /// </summary>
    /// <param name="session">The session the Sql statement belongs to.</param>
    /// <param name="sql">The Sql statement.</param>
    public SqlStatement(Session session, string sql) : base(session)
    {
      SQL = sql;
    }

    /// <summary>
    /// Gets the current Sql statement.
    /// </summary>
    public string SQL { get; private set; }
    /// <summary>
    /// Gets the list of parameters associated to this Sql statement.
    /// </summary>
    protected internal List<object> parameters = new List<object>();

    /// <summary>
    /// Executes the current Sql statement.
    /// </summary>
    /// <returns>A <see cref="SqlResult"/> object with the resultset and execution status.</returns>
    public override SqlResult Execute()
    {
      ValidateOpenSession();
      return GetSQLResult(this);
    }

    private SqlResult GetSQLResult(SqlStatement statement)
    {
      return Session.XSession.GetSQLResult(statement.SQL, parameters.ToArray());
    }

    /// <summary>
    /// Binds the parameters values by position.
    /// </summary>
    /// <param name="values">The parameter values.</param>
    /// <returns>This <see cref="SqlStatement"/> set with the binded parameters.</returns>
    public SqlStatement Bind(params object[] values)
    {
      if (values == null)
        parameters.Add(null);
      else
        parameters.AddRange(values);
      return this;
    }
  }
}
