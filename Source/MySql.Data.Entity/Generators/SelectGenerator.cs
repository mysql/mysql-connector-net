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
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Data;

namespace MySql.Data.Entity
{
  class SelectGenerator : SqlGenerator
  {
    Stack<SelectStatement> selectStatements = new Stack<SelectStatement>();

    #region Properties

    private SelectStatement CurrentSelect
    {
      get { return selectStatements.Count == 0 ? null : selectStatements.Peek(); }
    }

    #endregion

    public override string GenerateSQL(DbCommandTree tree)
    {
      DbQueryCommandTree commandTree = tree as DbQueryCommandTree;

      SqlFragment fragment = null;

      DbExpression e = commandTree.Query;
      switch (commandTree.Query.ExpressionKind)
      {
        case DbExpressionKind.Project:
          fragment = e.Accept(this);
          Debug.Assert(fragment is SelectStatement);
          break;
      }

      return fragment.ToString();
    }

    public override SqlFragment Visit(DbDistinctExpression expression)
    {
      SelectStatement select = VisitInputExpressionEnsureSelect(expression.Argument, null, null);
      select.IsDistinct = true;
      return select;
    }

    public override SqlFragment Visit(DbFilterExpression expression)
    {
      SelectStatement select = VisitInputExpressionEnsureSelect(expression.Input.Expression,
          expression.Input.VariableName, expression.Input.VariableType);
      select = WrapIfNotCompatible(select, expression.ExpressionKind);
      select.Where = expression.Predicate.Accept(this);
      return select;
    }

    public override SqlFragment Visit(DbGroupByExpression expression)
    {
      // first process the input
      DbGroupExpressionBinding e = expression.Input;
      SelectStatement innerSelect = VisitInputExpressionEnsureSelect(e.Expression, e.VariableName, e.VariableType);
      scope.Add(e.GroupVariableName, innerSelect);

      SelectStatement select = WrapIfNotCompatible(innerSelect, expression.ExpressionKind);

      CollectionType ct = (CollectionType)expression.ResultType.EdmType;
      RowType rt = (RowType)ct.TypeUsage.EdmType;

      int propIndex = 0;

      foreach (DbExpression key in expression.Keys)
      {
        var fragment = key.Accept(this);
        select.AddGroupBy(fragment);
        propIndex++;

        var colFragment = fragment as ColumnFragment;

        if (colFragment != null)
        {
          colFragment = colFragment.Clone();
          colFragment.ColumnAlias = String.Format("K{0}", propIndex);
          select.Columns.Add(colFragment);
        }
      }

      for (int agg = 0; agg < expression.Aggregates.Count; agg++)
      {
        DbAggregate a = expression.Aggregates[agg];
        DbFunctionAggregate fa = a as DbFunctionAggregate;
        if (fa == null) throw new NotSupportedException();

        string alias = rt.Properties[propIndex++].Name;
        ColumnFragment functionCol = new ColumnFragment(null, null);
        functionCol.Literal = HandleFunction(fa, a.Arguments[0].Accept(this));
        functionCol.ColumnAlias = alias;
        select.Columns.Add(functionCol);
      }

      return select;
    }

    private SqlFragment HandleFunction(DbFunctionAggregate fa, SqlFragment arg)
    {
      Debug.Assert(fa.Arguments.Count == 1);

      if (fa.Function.NamespaceName != "Edm")
        throw new NotSupportedException();

      FunctionFragment fragment = new FunctionFragment();
      fragment.Name = fa.Function.Name;
      if (fa.Function.Name == "BigCount")
        fragment.Name = "COUNT";
      else
        fragment.Name = fa.Function.Name.ToUpperInvariant();

      fragment.Distinct = fa.Distinct;
      fragment.Argmument = arg;
      return fragment;
      //return new CastExpression(aggregate, GetDbType(functionAggregate.ResultType.EdmType));
    }

    public override SqlFragment Visit(DbCrossJoinExpression expression)
    {
      Debug.Assert(expression.Inputs.Count == 2);
      return HandleJoinExpression(expression.Inputs[0], expression.Inputs[1],
          expression.ExpressionKind, null);
    }

    public override SqlFragment Visit(DbApplyExpression expression)
    {
      DbExpressionBinding inputBinding = expression.Input;
      InputFragment input = VisitInputExpression(inputBinding.Expression, inputBinding.VariableName, inputBinding.VariableType);
      DbExpressionBinding applyBinding = expression.Apply;
      InputFragment apply = VisitInputExpression(applyBinding.Expression, applyBinding.VariableName, applyBinding.VariableType);
      SelectStatement select = new SelectStatement();
      bool isInputSelect = false;
      if (!(input is TableFragment))
      {
        input.Wrap(scope);
        isInputSelect = true;
      }
      apply.Wrap(scope);
      select.From = input;
      select.Wrap(scope);
      if (apply is SelectStatement)
      {        
        SelectStatement applySel = apply as SelectStatement;
        foreach (ColumnFragment f in applySel.Columns)
        {
          SelectStatement newColSelect = new SelectStatement();
          newColSelect.From = applySel.From;
		  newColSelect.Where = applySel.Where;
          if (isInputSelect)
          {
            VisitAndReplaceTableName(newColSelect.Where, (input as SelectStatement).From.Name, input.Name);
          }
          newColSelect.Limit = applySel.Limit;
          newColSelect.Columns.Add( f );

          newColSelect.Wrap(scope);
          scope.Add(applySel.From.Name, applySel.From);
          
          ColumnFragment newCol = new ColumnFragment(apply.Name, f.ColumnName);
          newCol.Literal = newColSelect;
          newCol.PushInput(newCol.ColumnName);
          newCol.PushInput(apply.Name);
          select.AddColumn( newCol, scope);
          if (string.IsNullOrEmpty(newCol.ColumnAlias))
          {
            newColSelect.Name = newCol.ColumnName;
            newCol.ColumnAlias = newCol.ColumnName;
          }
          scope.Remove(newColSelect);
        }
        scope.Remove(applySel.From);
        scope.Remove(apply);
      }
      return select;
    }

    private void VisitAndReplaceTableName(SqlFragment sf, string oldTable, string newTable)
    {
      BinaryFragment bf = sf as BinaryFragment;
      ColumnFragment cf = sf as ColumnFragment;
      if (bf != null)
      {
        VisitAndReplaceTableName(bf.Left, oldTable, newTable);
        VisitAndReplaceTableName(bf.Right, oldTable, newTable);
      }
      else if ( (cf != null) && (cf.TableName == oldTable))
      {
        cf.TableName = newTable;
      }
    }

    public override SqlFragment Visit(DbJoinExpression expression)
    {
      return HandleJoinExpression(expression.Left, expression.Right,
          expression.ExpressionKind, expression.JoinCondition);
    }

    private SqlFragment HandleJoinExpression(DbExpressionBinding left, DbExpressionBinding right,
        DbExpressionKind joinType, DbExpression joinCondition)
    {
      JoinFragment join = new JoinFragment();
      join.JoinType = Metadata.GetOperator(joinType);

      join.Left = VisitInputExpression(left.Expression, left.VariableName, left.VariableType);
      join.Left = WrapJoinInputIfNecessary(join.Left, false);

      join.Right = VisitInputExpression(right.Expression, right.VariableName, right.VariableType);
      join.Right = WrapJoinInputIfNecessary(join.Right, true);

      if (join.Right is SelectStatement)
      {
        SelectStatement select = join.Right as SelectStatement;
        if (select.IsWrapped)
          select.Name = right.VariableName;
      }

      // now handle the ON case
      if (joinCondition != null)
        join.Condition = joinCondition.Accept(this);
      return join;
    }

    public SelectStatement WrapIfNotCompatible(SelectStatement select, DbExpressionKind expressionKind)
    {
      if (select.IsCompatible(expressionKind)) return select;
      SelectStatement newSelect = new SelectStatement();
      select.Wrap(scope);
      select.Scoped = true;
      newSelect.From = select;
      return newSelect;
    }

    private InputFragment WrapJoinInputIfNecessary(InputFragment fragment, bool isRightPart)
    {
      if (fragment is SelectStatement || fragment is UnionFragment)
      {
        fragment.Wrap(scope);
        fragment.Scoped = true;
      }
      else if (fragment is JoinFragment && isRightPart)
      {
        SelectStatement select = new SelectStatement();
        select.From = fragment;
        select.Name = fragment.Name;
        select.Wrap(scope);
        return select;
      }
      return fragment;
    }

    public override SqlFragment Visit(DbNewInstanceExpression expression)
    {
      Debug.Assert(expression.ResultType.EdmType is CollectionType);

      SelectStatement s = new SelectStatement();

      ColumnFragment c = new ColumnFragment(null, null);
      if (expression.Arguments.Count != 0)
        c.Literal = (LiteralFragment)expression.Arguments[0].Accept(this);
      else
        c.Literal = new LiteralFragment("NULL");
      c.ColumnAlias = "X";
      s.Columns.Add(c);
      return s;
    }

    public override SqlFragment Visit(DbProjectExpression expression)
    {
      SelectStatement select = VisitInputExpressionEnsureSelect(expression.Input.Expression,
          expression.Input.VariableName, expression.Input.VariableType);

      // see if we need to wrap this select inside a new select
      select = WrapIfNotCompatible(select, expression.ExpressionKind);

      Debug.Assert(expression.Projection is DbNewInstanceExpression);
      VisitNewInstanceExpression(select, expression.Projection as DbNewInstanceExpression);

      return select;
    }

    private SelectStatement VisitInputExpressionEnsureSelect(DbExpression e, string name, TypeUsage type)
    {
      InputFragment fragment = VisitInputExpression(e, name, type);
      if (fragment is SelectStatement) return (fragment as SelectStatement);

      SelectStatement s = new SelectStatement();

      // if the fragment is a union then it needs to be wrapped
      if (fragment is UnionFragment)
        (fragment as UnionFragment).Wrap(scope);

      s.From = fragment;
      return s;
    }

    public override SqlFragment Visit(DbElementExpression expression)
    {
      SelectStatement s = VisitInputExpressionEnsureSelect(expression.Argument, null, null);
      s.Wrap(scope);
      return s;
    }

    public override SqlFragment Visit(DbSortExpression expression)
    {
      SelectStatement select = VisitInputExpressionEnsureSelect(expression.Input.Expression,
          expression.Input.VariableName, expression.Input.VariableType);

      select = WrapIfNotCompatible(select, expression.ExpressionKind);

      foreach (DbSortClause sortClause in expression.SortOrder)
      {
        select.AddOrderBy(new SortFragment(
            sortClause.Expression.Accept(this), sortClause.Ascending));
      }
      return select;
    }

    public override SqlFragment Visit(DbLimitExpression expression)
    {
      SelectStatement select = (SelectStatement)VisitInputExpressionEnsureSelect(
          expression.Argument, null, null);
      select = WrapIfNotCompatible(select, expression.ExpressionKind);
      select.Limit = expression.Limit.Accept(this);
      return select;
    }

    public override SqlFragment Visit(DbSkipExpression expression)
    {
      SelectStatement select = VisitInputExpressionEnsureSelect(expression.Input.Expression, expression.Input.VariableName,
          expression.Input.VariableType);

      select = WrapIfNotCompatible(select, DbExpressionKind.Sort);
      foreach (DbSortClause sortClause in expression.SortOrder)
      {
        select.AddOrderBy(
            new SortFragment(sortClause.Expression.Accept(this), sortClause.Ascending));
      }

      // if we wrapped above, then this wrap will not create a new one so there
      // is no harm in calling it
      select = WrapIfNotCompatible(select, expression.ExpressionKind);
      select.Skip = expression.Count.Accept(this);
      return select;
    }

    public override SqlFragment Visit(DbUnionAllExpression expression)
    {
      UnionFragment f = new UnionFragment();
      Debug.Assert(expression.Left is DbProjectExpression);
      Debug.Assert(expression.Right is DbProjectExpression);

      SelectStatement left = VisitInputExpressionEnsureSelect(expression.Left, null, null);
      Debug.Assert(left.Name == null);
      //            left.Wrap(null);

      SelectStatement right = VisitInputExpressionEnsureSelect(expression.Right, null, null);
      Debug.Assert(right.Name == null);
      //          right.Wrap(null);

      f.Left = left;
      f.Right = right;
      return f;
    }
  }
}
