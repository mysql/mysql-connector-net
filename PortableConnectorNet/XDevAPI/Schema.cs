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

using MySql.XDevAPI.Results;
using System;
using System.Collections.Generic;

namespace MySql.XDevAPI
{
  public class Schema : DatabaseObject
  {
    internal Schema(BaseSession session, string name) : base(null, name)
    {
      Schema = this;
      Session = session;
    }

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
      Collection c = new Collection<JsonDoc>(this, name);
      if (ValidateExistence)
        if (!c.ExistsInDatabase())
          throw new MySqlException(String.Format("Collection '{0}' does not exist.", name));
      return c;
    }

    public Collection<T> GetCollection<T>(string name)
    {
      return new Collection<T>(this, name);
    }

    public Table GetTable(string name)
    {
      return new Table(this, name);
    }

    public View GetView(string name)
    {
      return new View(this, name);
    }

    #endregion

    #region Create Functions

    public Collection CreateCollection(string collectionName, bool ReuseExistingObject = false)
    {
      Session.XSession.CreateCollection(Name, collectionName);
      return new Collection<JsonDoc>(this, collectionName);
    }

    public View CreateView(string name)
    {
      throw new NotImplementedException();
    }

    #endregion

    public void DropCollection(string name)
    {
      Collection c = new Collection<JsonDoc>(this, name);
      c.Drop();
    }

    #region Base Class

    public override bool ExistsInDatabase()
    {
      string sql = String.Format("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name like '{0}'", Name);
      long count = (long)Session.InternalSession.ExecuteQueryAsScalar(sql);
      return count > 0;
    }

    #endregion
  }
}
