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
using MySql.XDevAPI.Relational;

namespace MySql.XDevAPI.Relational
{
  public class Table : DatabaseObject
  {
    internal Table(Schema schema, string name)
      : base(schema, name)
    {
    }

    public TableSelectStatement Select(params string[] columns)
    {
      return new TableSelectStatement(this, columns);
    }

    public TableInsertStatement Insert(params string[] fields)
    {
      return new TableInsertStatement(this, fields);
    }

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

    public override bool ExistsInDatabase()
    {
      return Session.XSession.TableExists(Schema, Name);
    }
  }
}
