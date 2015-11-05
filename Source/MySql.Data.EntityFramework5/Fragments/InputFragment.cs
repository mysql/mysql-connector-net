// Copyright © 2008, 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Text;

namespace MySql.Data.Entity
{
  abstract class InputFragment : SqlFragment
  {
    // not all input classes will support two inputs but union and join do
    // in cases where only one input is used, Left is it
    public InputFragment Left;
    public InputFragment Right;

    public InputFragment()
    {
    }

    public InputFragment(string name)
    {
      Name = name;
    }

    public string Name { get; set; }
    public bool IsWrapped { get; private set; }
    public bool Scoped { get; set; }

    public virtual void Wrap(Scope scope)
    {
      IsWrapped = true;
      Scoped = true;

      if (scope == null) return;
      if (Left != null)
        scope.Remove(Left);
      if (Right != null)
        scope.Remove(Right);
    }

    public virtual void WriteInnerSql(StringBuilder sql)
    {
    }

    public override void WriteSql(StringBuilder sql)
    {
      if (IsWrapped)
        sql.Append("(");
      WriteInnerSql(sql);
      if (IsWrapped)
        sql.Append(")");
      if (Name == null) return;
      if (this is TableFragment ||
          (IsWrapped && !(this is JoinFragment)))
        sql.AppendFormat(" AS {0}", QuoteIdentifier(Name));
    }

    public ColumnFragment GetColumnFromProperties(PropertyFragment properties)
    {
      ColumnFragment col = Left.GetColumnFromProperties(properties);
      if (col == null)
        col = Right.GetColumnFromProperties(properties);
      return col;
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      if (Left != null)
        Left.Accept(visitor);
      if (Right != null)
        Right.Accept(visitor);
    }
  }
}

