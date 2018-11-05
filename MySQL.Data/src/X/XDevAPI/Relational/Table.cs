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
        ValidateOpenSession();
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
      ValidateOpenSession();
      return Session.XSession.TableCount(Schema, Name, "Table");
    }

    /// <summary>
    /// Verifies if the table exists in the database.
    /// </summary>
    /// <returns><c>true</c> if the table exists; otherwise, <c>false</c>.</returns>
    public override bool ExistsInDatabase()
    {
      ValidateOpenSession();
      return Session.XSession.TableExists(Schema, Name);
    }
  }
}
