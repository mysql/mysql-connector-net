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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a MySql schema or database
  /// </summary>
  public class Schema : DatabaseObject
  {
    internal Schema(BaseSession session, string name) : base(null, name)
    {
      Schema = this;
      Session = session;
    }

    /// <summary>
    /// Session related to current schema
    /// </summary>
    public BaseSession Session { get; private set; }


    #region Browse Functions

    /// <summary>
    /// Returns a list of all collections in this schema
    /// </summary>
    /// <returns>List<Collection></returns>
    public List<Collection> GetCollections()
    {
      return Session.XSession.GetObjectList<Collection>(this, "COLLECTION");
    }

    /// <summary>
    /// Returns list of all tables in this schema
    /// </summary>
    /// <returns>List<Table></returns>
    public List<Table> GetTables()
    {
      return Session.XSession.GetObjectList<Table>(this, "TABLE");
    }

    /// <summary>
    /// Returns list of all views in this schema
    /// </summary>
    /// <returns>List<View></returns>
    public List<View> GetViews()
    {
      return Session.XSession.GetObjectList<View>(this, "VIEW");
    }

    #endregion

    #region Instance Functions

    /// <summary>
    /// Get a collection by name
    /// </summary>
    /// <param name="name">The name of the collection to get</param>
    /// <param name="ValidateExistence">Ensure the collection exists in the schema</param>
    /// <returns>Collection object</returns>
    public Collection GetCollection(string name, bool ValidateExistence = false)
    {
      Collection c = new Collection<DbDoc>(this, name);
      if (ValidateExistence)
        if (!c.ExistsInDatabase())
          throw new MySqlException(String.Format("Collection '{0}' does not exist.", name));
      return c;
    }

    /// <summary>
    /// Returns a typed collection object.  This is useful for using domain objects.
    /// </summary>
    /// <typeparam name="T">The type of collection returned</typeparam>
    /// <param name="name">The name of collection to get</param>
    /// <returns>Collection object</returns>
    public Collection<T> GetCollection<T>(string name)
    {
      return new Collection<T>(this, name);
    }

    /// <summary>
    /// Returns the given collection as a table
    /// </summary>
    /// <param name="name">Name of the collection</param>
    /// <returns>Table object</returns>
    public Table GetCollectionAsTable(string name)
    {
      return GetTable(name);
    }

    /// <summary>
    /// Gets a table object.  Upon return the object may or may not be valid.
    /// </summary>
    /// <param name="name">Name of the table object</param>
    /// <returns>Table object</returns>
    public Table GetTable(string name)
    {
      return new Table(this, name);
    }

    /// <summary>
    /// Gets a view object. Upon return the object may or may not be valid.
    /// </summary>
    /// <param name="name">Name of the view object</param>
    /// <returns>View object.</returns>
    public View GetView(string name)
    {
      return new View(this, name);
    }

    #endregion

    #region Create Functions

    /// <summary>
    /// Creates a collection
    /// </summary>
    /// <param name="collectionName">Name of the collection to create</param>
    /// <param name="ReuseExistingObject"></param>
    /// <returns></returns>
    public Collection CreateCollection(string collectionName, bool ReuseExistingObject = false)
    {
      Collection coll = new Collection(this, collectionName);
      if (ReuseExistingObject && coll.ExistsInDatabase())
        return coll;
      Session.XSession.CreateCollection(Name, collectionName);
      return new Collection(this, collectionName);
    }

    #endregion

    /// <summary>
    /// Drops the given collection
    /// </summary>
    /// <param name="name">Name of the collection to drop</param>
    public void DropCollection(string name)
    {
      Collection c = GetCollection(name);
      if (!c.ExistsInDatabase()) return;
      Session.XSession.DropCollection(Name, name);
    }

    #region Base Class

    /// <summary>
    /// Determines if this schema actually exists
    /// </summary>
    /// <returns>True if exists, false otherwise</returns>
    public override bool ExistsInDatabase()
    {
      string sql = String.Format("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name like '{0}'", Name);
      long count = (long)Session.InternalSession.ExecuteQueryAsScalar(sql);
      return count > 0;
    }

    #endregion
  }
}
