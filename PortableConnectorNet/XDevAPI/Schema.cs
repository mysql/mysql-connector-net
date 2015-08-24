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

    public List<Collection> GetCollections()
    {
      RowResult r = Session.XSession.GetCollections(Name);
      List<Collection> docs = new List<Collection>();
      foreach (ResultRow row in r.Rows)
      {
        Collection<DbDocument> doc = new Collection<DbDocument>(this, row.GetString("name"));
        docs.Add(doc);
      }
      return docs;
    }

    public List<Table> GetTables()
    {
      throw new NotImplementedException();
    }

    public List<View> GetViews()
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Instance Functions

    public Collection GetCollection(string name, bool ValidateExistence = false)
    {
      Collection c = new Collection<DbDocument>(this, name);
      if (ValidateExistence)
        if (!c.ExistsInDatabase())
          throw new MySqlException(String.Format("Collection '{0}' does not exist.", name));
      return c;
    }

    public Table GetTable(string name)
    {
      return new Table(this, name);
    }

    public View GetView(string name)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Create Functions

    public Collection CreateCollection(string collectionName, bool ReuseExistingObject = false)
    {
      Session.XSession.CreateCollection(Name, collectionName);
      return new Collection<DbDocument>(this, collectionName);
    }

    public View CreateView(string name)
    {
      throw new NotImplementedException();
    }

    #endregion

    public void DropCollection(string name)
    {
      Collection c = new Collection<DbDocument>(this, name);
      c.Drop();
    }

    #region Base Class

    public override bool ExistsInDatabase()
    {
      string sql = String.Format("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name like '{0}'", Name);
      return Session.InternalSession.ExecuteQueryAsScalar(sql).Equals(1);
    }

    #endregion
  }
}
