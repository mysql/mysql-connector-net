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
    public Schema(BaseSession session)
    {
      this.Session = session;
      this.Schema = this;
      this.Name = session.Settings.Database;
    }


    #region Browse Functions

    public List<Collection> GetCollections()
    {
      throw new NotImplementedException();
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
      throw new NotImplementedException();
    }

    public Table GetTable(string name)
    {
      return new Table(name);
    }

    public View GetView(string name)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Create Functions

    public Collection CreateCollection(string name, bool ReuseExistingObject = false)
    {
      throw new NotImplementedException();
    }

    public View CreateView(string name)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Base Class

    public override bool ExistsInDatabase()
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
