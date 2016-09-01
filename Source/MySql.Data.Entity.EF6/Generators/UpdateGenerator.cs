// Copyright © 2008, 2011, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
#if EF6
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
#endif


namespace MySql.Data.Entity
{
  class UpdateGenerator : SqlGenerator
  {
    private bool _onReturningSelect;

    public override string GenerateSQL(DbCommandTree tree)
    {
      DbUpdateCommandTree commandTree = tree as DbUpdateCommandTree;

      UpdateStatement statement = new UpdateStatement();

      _onReturningSelect = false;
      statement.Target = commandTree.Target.Expression.Accept(this);
      scope.Add("target", statement.Target as InputFragment);

      if (values == null)
        values = new Dictionary<EdmMember, SqlFragment>();

      foreach (DbSetClause setClause in commandTree.SetClauses)
      {
        statement.Properties.Add(setClause.Property.Accept(this));
        DbExpression value = setClause.Value;
        SqlFragment valueFragment = value.Accept(this);
        statement.Values.Add(valueFragment);

        if (value.ExpressionKind != DbExpressionKind.Null)
        {
          EdmMember property = ((DbPropertyExpression)setClause.Property).Property;
          values.Add(property, valueFragment);
        }
      }
      
      statement.Where = commandTree.Predicate.Accept(this);

      _onReturningSelect = true;
      if (commandTree.Returning != null)
        statement.ReturningSelect = GenerateReturningSql(commandTree, commandTree.Returning);

      return statement.ToString();
    }

    protected override SelectStatement GenerateReturningSql(DbModificationCommandTree tree, DbExpression returning)
    {
      SelectStatement select = base.GenerateReturningSql(tree, returning);
      ListFragment where = new ListFragment();
      where.Append(" row_count() > 0 and ");
      where.Append( ((DbUpdateCommandTree)tree).Predicate.Accept(this) );
      select.Where = where;

      return select;
    }

    private Stack<EdmMember> _columnsVisited = new Stack<EdmMember>();

    protected override SqlFragment VisitBinaryExpression(DbExpression left, DbExpression right, string op)
    {
      BinaryFragment f = new BinaryFragment();
      f.Operator = op;
      f.Left = left.Accept(this);
      f.WrapLeft = ShouldWrapExpression(left);
      if (f.Left is ColumnFragment)
      {
        _columnsVisited.Push( (( DbPropertyExpression )left ).Property );
      }
      f.Right = right.Accept(this);
      if (f.Left is ColumnFragment)
      {
        _columnsVisited.Pop();
      }
      f.WrapRight = ShouldWrapExpression(right);
      return f;
    }

    public override SqlFragment Visit(DbConstantExpression expression)
    {
      SqlFragment value = null;
      if ( _onReturningSelect && values.TryGetValue(_columnsVisited.Peek(), out value))
      {
        if (value is LiteralFragment)
        {
          MySqlParameter par = Parameters.Find(p => p.ParameterName == ( value as LiteralFragment ).Literal );
          if (par != null)
            return new LiteralFragment(par.ParameterName);
        }
      }
      return base.Visit(expression);
    }
  }
}
