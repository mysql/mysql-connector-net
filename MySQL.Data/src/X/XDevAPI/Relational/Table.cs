// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
  /// Represents a server Table or View.
  /// </summary>
  public class Table : DatabaseObject
  {
    private bool? isView;

    /// <summary>
    /// Gets a value indicating whether the object is 
    /// a View (True) or a Table (False).
    /// </summary>
    public bool IsView {
      get
      {
        return CheckIsView();
      }
      internal set
      {
        isView = value;
      }
    }

    private bool CheckIsView()
    {
      if (!isView.HasValue)
      {
        string type = Session.XSession.GetObjectType(Schema, Name).ToUpperInvariant();
        isView = (type == "VIEW");
      }
      return isView.Value;
    }

    internal Table(Schema schema, string name, bool isView)
      : base(schema, name)
    {
      this.isView = isView;
    }

    internal Table(Schema schema, string name)
      : base(schema, name)
    {
    }

    internal Table() : base(null, null) { }

    /// <summary>
    /// Creates a <see cref="TableSelectStatement"/> set with the columns to select. The table select
    /// statement can be further modified before execution. This method is intended to select a set
    /// of table rows.
    /// </summary>
    /// <param name="columns">The optional column names to select.</param>
    /// <returns>A <see cref="TableSelectStatement"/> object for select chain operations.</returns>
    public TableSelectStatement Select(params string[] columns)
    {
      return new TableSelectStatement(this, columns);
    }

    /// <summary>
    /// Creates a <see cref="TableInsertStatement"/> set with the fileds to insert to. The table
    /// insert statement can be further modified before exeuction. This method is intended to
    /// insert one or multiple rows into a table.
    /// </summary>
    /// <param name="fields">The list of fields to insert.</param>
    /// <returns>A <see cref="TableInsertStatement"/> object for insert chain operations.</returns>
    public TableInsertStatement Insert(params string[] fields)
    {
      return new TableInsertStatement(this, fields);
    }

    /// <summary>
    /// Creates a <see cref="TableUpdateStatement"/>. This method is intended to update table rows 
    /// values.
    /// </summary>
    /// <returns>A <see cref="TableUpdateStatement"/> object for update chain operations.</returns>
    public TableUpdateStatement Update()
    {
      return new TableUpdateStatement(this);
    }

    /// <summary>
    /// Creates a <see cref="TableDeleteStatement"/>. This method is intended to delete rows from a 
    /// table.
    /// </summary>
    /// <returns>A <see cref="TableDeleteStatement"/> object for delete chain operations.</returns>
    public TableDeleteStatement Delete()
    {
      return new TableDeleteStatement(this, null);
    }

    /// <summary>
    /// Returns the number of rows in the table on the server.
    /// </summary>
    /// <returns>The number of rows.</returns>
    public long Count()
    {
      return Session.XSession.TableCount(Schema, Name);
    }

    /// <summary>
    /// Verifies if the table exists in database.
    /// </summary>
    /// <returns>True if table exists, false otherwise.</returns>
    public override bool ExistsInDatabase()
    {
      return Session.XSession.TableExists(Schema, Name);
    }
  }
}
