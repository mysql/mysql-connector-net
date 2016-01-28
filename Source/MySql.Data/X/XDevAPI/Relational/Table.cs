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
using MySqlX.XDevAPI.Relational;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a server Table
  /// </summary>
  public class Table : DatabaseObject
  {
    internal Table(Schema schema, string name)
      : base(schema, name)
    {
    }

    /// <summary>
    /// Selects a set of table rows
    /// </summary>
    /// <param name="columns">Optional column names to select</param>
    /// <returns>TableSelectStatement object for select chain</returns>
    public TableSelectStatement Select(params string[] columns)
    {
      return new TableSelectStatement(this, columns);
    }

    /// <summary>
    /// Inserts one or multiple rows into a table
    /// </summary>
    /// <param name="fields">Optional list of fields for insert</param>
    /// <returns>TableInsertStatement object for insert chain</returns>
    public TableInsertStatement Insert(params string[] fields)
    {
      return new TableInsertStatement(this, fields);
    }

    /// <summary>
    /// Updates table rows values
    /// </summary>
    /// <returns>TableUpdateStatement object for update chain</returns>
    public TableUpdateStatement Update()
    {
      return new TableUpdateStatement(this);
    }

    /// <summary>
    /// Deletes rows from a Table
    /// </summary>
    /// <returns>DeleteStatement object</returns>
    public TableDeleteStatement Delete()
    {
      return new TableDeleteStatement(this, null);
    }

    /// <summary>
    /// Returns the number of rows in the table on the server.
    /// </summary>
    /// <returns>Number of rows</returns>
    public long Count()
    {
      return Session.XSession.TableCount(Schema, Name);
    }

    /// <summary>
    /// Verifies if the table exists in database
    /// </summary>
    /// <returns>True if table exists</returns>
    public override bool ExistsInDatabase()
    {
      return Session.XSession.TableExists(Schema, Name);
    }
  }
}
