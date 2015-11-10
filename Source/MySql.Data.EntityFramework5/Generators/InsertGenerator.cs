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

using System.Text;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Diagnostics;
using System;
#if EF6
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
#endif


namespace MySql.Data.Entity
{
  class InsertGenerator : SqlGenerator
  {
    public override string GenerateSQL(DbCommandTree tree)
    {
      DbInsertCommandTree commandTree = tree as DbInsertCommandTree;

      InsertStatement statement = new InsertStatement();

      DbExpressionBinding e = commandTree.Target;
      statement.Target = (InputFragment)e.Expression.Accept(this);

      scope.Add("target", statement.Target);

      foreach (DbSetClause setClause in commandTree.SetClauses)
        statement.Sets.Add(setClause.Property.Accept(this));

      if (values == null)
        values = new Dictionary<EdmMember, SqlFragment>();

      foreach (DbSetClause setClause in commandTree.SetClauses)
      {
        DbExpression value = setClause.Value;
        SqlFragment valueFragment = value.Accept(this);
        statement.Values.Add(valueFragment);

        if (value.ExpressionKind != DbExpressionKind.Null)
        {
          EdmMember property = ((DbPropertyExpression)setClause.Property).Property;
          values.Add(property, valueFragment);
        }
      }

      if (commandTree.Returning != null)
        statement.ReturningSelect = GenerateReturningSql(commandTree, commandTree.Returning);

      return statement.ToString();
    }

    protected virtual SelectStatement GenerateReturningSql(DbModificationCommandTree tree, DbExpression returning)
    {      
      SelectStatement select = base.GenerateReturningSql(tree, returning);      

      ListFragment where = new ListFragment();

      EntitySetBase table = ((DbScanExpression)tree.Target.Expression).Target;
      bool foundIdentity = false;
      where.Append(" row_count() > 0");
      foreach (EdmMember keyMember in table.ElementType.KeyMembers)
      {
        SqlFragment value;
        if (!values.TryGetValue(keyMember, out value))
        {
          if (foundIdentity)
            throw new NotSupportedException();
          foundIdentity = true;
          PrimitiveTypeKind type = ((PrimitiveType)keyMember.TypeUsage.EdmType.BaseType).PrimitiveTypeKind;
          if ((type == PrimitiveTypeKind.Byte) || (type == PrimitiveTypeKind.SByte) ||
              (type == PrimitiveTypeKind.Int16) || (type == PrimitiveTypeKind.Int32) ||
              (type == PrimitiveTypeKind.Int64) || (type == PrimitiveTypeKind.Decimal && IsValidMySqlDataType(keyMember.TypeUsage.EdmType.FullName)))
          {
            value = new LiteralFragment("last_insert_id()");
	  }
          else if (keyMember.TypeUsage.EdmType.BaseType.Name == "Guid")
            value = new LiteralFragment(string.Format("ANY(SELECT guid FROM tmpIdentity_{0})", (table as MetadataItem).MetadataProperties["Table"].Value));
        }
        where.Append(String.Format(" AND `{0}`=", keyMember));
        where.Append(value);
      }
      select.Where = where;      
      return select;
    }
	
	private bool IsValidMySqlDataType(string dataType)
    {
      return (new List<string>() { "MySql.ubigint" }).Contains(dataType);
    }
  }
}
