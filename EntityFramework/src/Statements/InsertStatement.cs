// Copyright © 2008, 2017, Oracle and/or its affiliates. All rights reserved.
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

using System.Text;
using System.Collections.Generic;

namespace MySql.Data.EntityFramework
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
