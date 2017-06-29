// Copyright © 2015, 2017 Oracle and/or its affiliates. All rights reserved.
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
    protected List<object> parameters = new List<object>();

    /// <summary>
    /// Executes the current Sql statement.
    /// </summary>
    /// <returns>A <see cref="SqlResult"/> object with the resultset and execution status.</returns>
    public override SqlResult Execute()
    {
      return Session.XSession.GetSQLResult(SQL, parameters.ToArray());
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
