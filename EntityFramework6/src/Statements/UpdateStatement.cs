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
  class UpdateStatement : SqlFragment
  {
    public UpdateStatement()
    {
      Properties = new List<SqlFragment>();
      Values = new List<SqlFragment>();
    }

    public SqlFragment Target { get; set; }
    public List<SqlFragment> Properties { get; private set; }
    public List<SqlFragment> Values { get; private set; }
    public SqlFragment Where { get; set; }
    public SelectStatement ReturningSelect;

    public override void WriteSql(StringBuilder sql)
    {
      sql.Append("UPDATE ");
      Target.WriteSql(sql);
      sql.Append(" SET ");

      string seperator = "";
      for (int i = 0; i < Properties.Count; i++)
      {
        sql.Append(seperator);
        Properties[i].WriteSql(sql);
        sql.Append("=");
        Values[i].WriteSql(sql);
        seperator = ", ";
      }
      if (Where != null)
      {
        sql.Append(" WHERE ");
        Where.WriteSql(sql);
      }
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
