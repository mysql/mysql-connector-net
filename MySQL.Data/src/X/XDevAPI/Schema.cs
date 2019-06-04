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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a schema or database.
  /// </summary>
  public class Schema : DatabaseObject
  {
    internal Schema(BaseSession session, string name) : base(null, name)
    {
      Schema = this;
      Session = session;
    }

    /// <summary>
    /// Session related to current schema.
    /// </summary>
    public new BaseSession Session { get; private set; }


    #region Browse Functions

    /// <summary>
    /// Returns a list of all collections in this schema.
    /// </summary>
    /// <returns>A <see cref="Collection"/> list representing all found collections.</returns>
    public List<Collection> GetCollections()
    {
      ValidateOpenSession();
      return Session.XSession.GetObjectList<Collection>(this, "COLLECTION");
    }

    /// <summary>
    /// Returns a list of all tables in this schema.
    /// </summary>
    /// <returns>A <see cref="Table"/> list representing all found tables.</returns>
    public List<Table> GetTables()
    {
      ValidateOpenSession();
      return Session.XSession.GetObjectList<Table>(this, "TABLE", "VIEW");
    }

    #endregion

    #region Instance Functions

    /// <summary>
    /// Gets a collection by name.
    /// </summary>
    /// <param name="name">The name of the collection to get.</param>
    /// <param name="ValidateExistence">Ensures the collection exists in the schema.</param>
    /// <returns>A <see cref="Collection"/> object matching the given name.</returns>
    public Collection GetCollection(string name, bool ValidateExistence = false)
    {
      Collection c = new Collection<DbDoc>(this, name);
      if (ValidateExistence)
      {
        ValidateOpenSession();
        if (!c.ExistsInDatabase())
          throw new MySqlException(String.Format("Collection '{0}' does not exist.", name));
      }
      return c;
    }

    /// <summary>
    /// Gets a typed collection object. This is useful for using domain objects.
    /// </summary>
    /// <typeparam name="T">The type of collection returned.</typeparam>
    /// <param name="name">The name of collection to get.</param>
    /// <returns>A generic <see cref="Collection"/> object set with the given name.</returns>
    public Collection<T> GetCollection<T>(string name)
    {
      return new Collection<T>(this, name);
    }

    /// <summary>
    /// Gets the given collection as a table.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <returns>A <see cref="Table"/> object set with the given name.</returns>
    public Table GetCollectionAsTable(string name)
    {
      return GetTable(name);
    }

    /// <summary>
    /// Gets a table object. Upon return the object may or may not be valid.
    /// </summary>
    /// <param name="name">The name of the table object.</param>
    /// <returns>A <see cref="Table"/> object set with the given name.</returns>
    public Table GetTable(string name)
    {
      return new Table(this, name);
    }

    #endregion

    #region Create Functions

    /// <summary>
    /// Creates a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="ReuseExistingObject">If false, it will throw an exception if collection exists.</param>
    /// <returns>Collection referente.</returns>
    public Collection CreateCollection(string collectionName, bool ReuseExistingObject = false)
    {
      ValidateOpenSession();
      Collection coll = new Collection(this, collectionName);
      if (ReuseExistingObject && coll.ExistsInDatabase())
        return coll;
      Session.XSession.CreateCollection(Name, collectionName);
      return new Collection(this, collectionName);
    }

    #endregion

    /// <summary>
    /// Drops the given collection.
    /// </summary>
    /// <param name="name">The name of the collection to drop.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public void DropCollection(string name)
    {
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
      ValidateOpenSession();
      Collection c = GetCollection(name);
      if (!c.ExistsInDatabase()) return;
      Session.XSession.DropCollection(Name, name);
    }

    #region Base Class

    /// <summary>
    /// Determines if this schema actually exists.
    /// </summary>
    /// <returns>True if exists, false otherwise.</returns>
    public override bool ExistsInDatabase()
    {
      ValidateOpenSession();
      string sql = String.Format("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name like '{0}'", Name);
      long count = (long)Session.InternalSession.ExecuteQueryAsScalar(sql);
      return count > 0;
    }

    #endregion
  }
}
