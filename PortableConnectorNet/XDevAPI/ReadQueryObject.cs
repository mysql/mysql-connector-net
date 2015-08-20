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

using MySql.Data;
using System;
using System.Collections.Generic;

namespace MySql.XDevAPI
{
  public class ReadQueryObject : QueryObject
  {
    internal DatabaseObject databaseObject;
    internal List<string> projection;
    internal string where = null;
    internal Dictionary<string, object> parameters;
    internal List<string> orderBy;




    public ReadQueryObject(DatabaseObject databaseObject)
    {
      this.databaseObject = databaseObject;
      projection = new List<string>();
      orderBy = new List<string>();
    }


    public ReadQueryObject Select(params string[] columns)
    {
      projection = new List<string>(columns);
      return this;
    }

    public ReadQueryObject Where(string condition)
    {
      where = condition;
      return this;
    }

    public ReadQueryObject OrderBy(string p)
    {
      throw new NotImplementedException();
    }

    public ReadQueryObject Bind(Dictionary<string, object> namedParameters)
    {
      this.parameters = namedParameters;
      return this;
    }

    public ReadQueryObject Bind(params object[] values)
    {
      this.parameters = new Dictionary<string, object>();
      int i = 0;
      foreach (object value in values)
      {
        this.parameters.Add(i++.ToString(), value);
      }
      return this;
    }

    public ResultSet Execute()
    {
      SelectStatement statement = new SelectStatement
      {
        schema = databaseObject.Schema.Name,
        isTable = databaseObject is Table,
        table = databaseObject.Name,
        columns = projection,
        parameters = parameters,
        where = where,
        orderBy = orderBy
      };
      var result = databaseObject.Session.InternalSession.Find(statement);
      return result;
    }

  }
}
