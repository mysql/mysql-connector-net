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
using System.Collections.Generic;
using System;

namespace MySql.XDevAPI.Statements
{
  public class SelectStatement : FilterableStatement<SelectStatement, Table, TableResult>
  {
    internal FindParams findParams = new FindParams();

    public SelectStatement(Table t, params string[] projection) : base(t)
    {
      findParams.Projection = projection;
      FilterData.IsRelational = true;
    }

    public SelectStatement GroupBy(params string[] groupBy)
    {
      findParams.GroupBy = groupBy;
      return this;
    }


    public SelectStatement Having(string having)
    {
      findParams.GroupByCritieria = having;
      return this;
    }

    public override TableResult Execute()
    {
      return CollectionOrTable.Schema.Session.XSession.FindRows(this);
    }

    public SelectStatement Bind(Dictionary<string, object> namedParameters)
    {
      this.FilterData.Parameters = namedParameters;
      return this;
    }

    public SelectStatement Bind(params object[] values)
    {
      this.FilterData.Parameters = new Dictionary<string, object>();
      int i = 0;
      foreach (object value in values)
      {
        this.FilterData.Parameters.Add(i++.ToString(), value);
      }
      return this;
    }
  }
}
