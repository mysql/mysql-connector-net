// Copyright © 2008, 2013, Oracle and/or its affiliates. All rights reserved.
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

using System.Text;
using System.Collections.Generic;

namespace MySql.Data.Entity
{
  class InsertStatement : SqlFragment
  {
    public InsertStatement()
    {
      Sets = new List<SqlFragment>();
      Values = new List<SqlFragment>();
    }

    public InputFragment Target { get; set; }
    public List<SqlFragment> Sets { get; private set; }
    public List<SqlFragment> Values { get; private set; }
    public SelectStatement ReturningSelect;

    public override void WriteSql(StringBuilder sql)
    {
      // changes sql_mode to allow inserting data without identity columns
      if (ReturningSelect != null && ReturningSelect.Columns.Count > 0)
        sql.Append("SET SESSION sql_mode='ANSI';");

      sql.Append("INSERT INTO ");
      Target.WriteSql(sql);
      if (Sets.Count > 0)
      {
        sql.Append("(");
        WriteList(Sets, sql);
        sql.Append(")");
      }
      sql.Append(" VALUES ");
      sql.Append("(");
      WriteList(Values, sql);
      sql.Append(")");

      if (ReturningSelect != null)
      {
        sql.Append(";\r\n");
        ReturningSelect.WriteSql(sql);
      }
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      throw new System.NotImplementedException();
    }
  }
}
