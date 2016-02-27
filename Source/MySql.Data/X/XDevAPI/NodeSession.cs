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


using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a single MySql server session
  /// </summary>
  public class NodeSession : BaseSession
  {
    internal NodeSession(string connectionString)
      : base(connectionString)
    {

    }

    internal NodeSession(object connectionData)
      : base(connectionData)
    {

    }

    /// <summary>
    /// Returns a SqlStatement object that can be used to execute the given SQL
    /// </summary>
    /// <param name="sql">The SQL to execute</param>
    /// <returns>SqlStatement object</returns>
    public SqlStatement SQL(string sql)
    {
      return new SqlStatement(this, sql);
    }

    /// <summary>
    /// Sets the schema in the database
    /// </summary>
    /// <param name="schema">Schema name to be set</param>
    public void SetCurrentSchema(string schema)
    {
      InternalSession.ExecuteSqlNonQuery($"USE `{schema}`");
      GetSchema(schema);
    }

    /// <summary>
    /// Executes a query in the database to get the current schema
    /// </summary>
    /// <returns>Current Database Schema object or null if any schema is selected</returns>
    public Schema GetCurrentSchema()
    {
      string schemaName = (string)InternalSession.ExecuteQueryAsScalar("SELECT DATABASE()");
      return schemaName == null ? null : GetSchema(schemaName);
    }
  }
}
