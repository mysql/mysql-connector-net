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


using MySql.Data;
using MySql.Data.MySqlClient;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a database object.
  /// </summary>
  public abstract class DatabaseObject
  {
    internal DatabaseObject(Schema schema, string name)
    {
      Schema = schema;
      Name = name;
    }

    /// <summary>
    /// Gets the session that owns the database object.
    /// </summary>
    public BaseSession Session
    {
      get { return Schema.Session;  }
    }

    /// <summary>
    /// Gets the schema that owns the database object.
    /// </summary>
    public Schema Schema { get; internal set; }

    /// <summary>
    /// Gets the database object name.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Verifies that the database object exists in the database.
    /// </summary>
    /// <returns>True if the object exists in database, false otherwise.</returns>
    public abstract bool ExistsInDatabase();

    protected void ValidateOpenSession()
    {
      if (Session.XSession.SessionState != SessionState.Open)
        throw new MySqlException(ResourcesX.InvalidSession);
    }
  }
}
