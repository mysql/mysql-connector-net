// Copyright (c) 2008 MySQL AB, 2008-2009 Sun Microsystems, Inc.
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
using System.Diagnostics;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace MySql.Data.Entity
{
  abstract class SqlGenerator : DbExpressionVisitor<SqlFragment>
  {
    protected string tabs = String.Empty;
    private int parameterCount = 1;
    protected Scope scope = new Scope();
    protected int propertyLevel;
    protected Dictionary<EdmMember, SqlFragment> values;

    public SqlGenerator()
    {
      Parameters = new List<MySqlParameter>();
    }

    #region Properties

    public List<MySqlParameter> Parameters { get; private set; }
    //        protected SymbolTable Symbols { get; private set; }

    #endregion

    public virtual string GenerateSQL(DbCommandTree commandTree)
    {
      throw new NotImplementedException();
    }

    protected string CreateUniqueParameterName()
    {
      return String.Format("@gp{0}", parameterCount++);
    }

    #region DbExpressionVisitor Base Implementations

    public override SqlFragment Visit(DbVariableReferenceExpression expression)
    {
      PropertyFragment fragment = new PropertyFragment();
      fragment.Properties.Add(expression.VariableName);
      return fragment;
    }

    public override SqlFragment Visit(DbPropertyExpression expression)
    {
      propertyLevel++;
      PropertyFragment fragment = expression.Instance.Accept(this) as PropertyFragment;
      fragment.Properties.Add(expression.Property.Name);
      propertyLevel--;

      // if we are not at the top level property then just return
      if (propertyLevel > 0) return fragment;

      ColumnFragment column = new ColumnFragment(null, fragment.LastProperty);
      column.PropertyFragment = fragment;
      InputFragment input = scope.FindInputFromProperties(fragment);
      if (input != null)
        column.TableName = input.Name;

      // now we need to check if our column name was possibly renamed
      if (input is TableFragment) return column;

      SelectStatement select = input as SelectStatement;
      UnionFragment union = input as UnionFragment;

      if (select != null)
        select.HasDifferentNameForColumn(column);
      else if (union != null)
        union.HasDifferentNameForColumn(column);

      // input is a table, selectstatement, or unionstatement
      return column;
    }

    public override SqlFragment Visit(DbScanExpression expression)
    {
      EntitySetBase target = expression.Target;
      TableFragment fragment = new TableFragment();

      MetadataProperty property;
      bool propExists = target.MetadataProperties.TryGetValue("DefiningQuery", true, out property);
      if (propExists && property.Value != null)
        fragment.DefiningQuery = new LiteralFragment(property.Value as string);
      else
      {
        fragment.Schema = target.EntityContainer.Name;
        fragment.Table = target.Name;

        propExists = target.MetadataProperties.TryGetValue("Schema", true, out property);
        if (propExists && property.Value != null)
          fragment.Schema = property.Value as string;
        propExists = target.MetadataProperties.TryGetValue("Table", true, out property);
        if (propExists && property.Value != null)
          fragment.Table = property.Value as string;
      }
      return fragment;
    }

    public override SqlFragment Visit(DbParameterReferenceExpression expression)
    {
      return new LiteralFragment("@" + expression.ParameterName);
    }

    public override SqlFragment Visit(DbNotExpression expression)
    {
      SqlFragment f = expression.Argument.Accept(this);
      Debug.Assert(f is NegatableFragment);
      NegatableFragment nf = f as NegatableFragment;
      nf.Negate();
      return nf;
    }

    public override SqlFragment Visit(DbIsEmptyExpression expression)
    {
      ExistsFragment f = new ExistsFragment(expression.Argument.Accept(this));
      f.Negate();
      return f;
    }

    public override SqlFragment Visit(DbFunctionExpression expression)
    {
      FunctionProcessor gen = new FunctionProcessor();
      return gen.Generate(expression, this);
    }

    public override SqlFragment Visit(DbConstantExpression expression)
    {
      PrimitiveTypeKind pt = ((PrimitiveType)expression.ResultType.EdmType).PrimitiveTypeKind;
      string literal = Metadata.GetNumericLiteral(pt, expression.Value);
      if (literal != null)
        return new LiteralFragment(literal);
      else if (pt == PrimitiveTypeKind.Boolean)
        return new LiteralFragment((bool)expression.Value ? "1" : "0");
      else
      {
        // use a parameter for non-numeric types so we get proper
        // quoting
        MySqlParameter p = new MySqlParameter();
        p.ParameterName = CreateUniqueParameterName();
        p.DbType = Metadata.GetDbType(expression.ResultType);
        p.Value = Metadata.NormalizeValue(expression.ResultType, expression.Value);
        Parameters.Add(p);
        return new LiteralFragment(p.ParameterName);
      }
    }

    public override SqlFragment Visit(DbComparisonExpression expression)
    {
      return VisitBinaryExpression(expression.Left, expression.Right,
          Metadata.GetOperator(expression.ExpressionKind));
    }    

    public override SqlFragment Visit(DbAndExpression expression)
    {
      return VisitBinaryExpression(expression.Left, expression.Right, "AND");
    }

    public override SqlFragment Visit(DbOrExpression expression)
    {
      return VisitBinaryExpression(expression.Left, expression.Right, "OR");
    }

    public override SqlFragment Visit(DbCastExpression expression)
    {
      //TODO: handle casting
      return expression.Argument.Accept(this);
    }

    public override SqlFragment Visit(DbLikeExpression expression)
    {
      LikeFragment f = new LikeFragment();

      f.Argument = expression.Argument.Accept(this);
      f.Pattern = expression.Pattern.Accept(this);

      if (expression.Escape.ExpressionKind != DbExpressionKind.Null)
        f.Escape = expression.Escape.Accept(this);

      return f;
    }

    public override SqlFragment Visit(DbCaseExpression expression)
    {
      CaseFragment c = new CaseFragment();

      Debug.Assert(expression.When.Count == expression.Then.Count);

      for (int i = 0; i < expression.When.Count; ++i)
      {
        c.When.Add(expression.When[i].Accept(this));
        c.Then.Add(expression.Then[i].Accept(this));
      }
      if (expression.Else != null && !(expression.Else is DbNullExpression))
        c.Else = expression.Else.Accept(this);
      return c;
    }

    public override SqlFragment Visit(DbIsNullExpression expression)
    {
      IsNullFragment f = new IsNullFragment();
      f.Argument = expression.Argument.Accept(this);
      return f;
    }

    public override SqlFragment Visit(DbIntersectExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbNullExpression expression)
    {
      return new LiteralFragment("NULL");
    }

    public override SqlFragment Visit(DbArithmeticExpression expression)
    {
      if (expression.ExpressionKind == DbExpressionKind.UnaryMinus)
      {
        ListFragment f = new ListFragment();
        f.Append("-(");
        f.Append(expression.Arguments[0].Accept(this));
        f.Append(")");
        return f;
      }

      string op = String.Empty;
      switch (expression.ExpressionKind)
      {
        case DbExpressionKind.Divide:
          op = "/"; break;
        case DbExpressionKind.Minus:
          op = "-"; break;
        case DbExpressionKind.Modulo:
          op = "%"; break;
        case DbExpressionKind.Multiply:
          op = "*"; break;
        case DbExpressionKind.Plus:
          op = "+"; break;
        default:
          throw new NotSupportedException();
      }
      return VisitBinaryExpression(expression.Arguments[0], expression.Arguments[1], op);
    }

    protected void VisitNewInstanceExpression(SelectStatement select,
        DbNewInstanceExpression expression)
    {
      Debug.Assert(expression.ResultType.EdmType is RowType);

      RowType row = expression.ResultType.EdmType as RowType;

      for (int i = 0; i < expression.Arguments.Count; i++)
      {
        ColumnFragment col;

        SqlFragment fragment = expression.Arguments[i].Accept(this);
        if (fragment is ColumnFragment)
          col = fragment as ColumnFragment;
        else
        {
          col = new ColumnFragment(null, null);
          col.Literal = fragment;
        }

        col.ColumnAlias = row.Properties[i].Name;
        select.Columns.Add(col);
      }
    }

    public override SqlFragment Visit(DbTreatExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbRelationshipNavigationExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbRefExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbOfTypeExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbIsOfExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbRefKeyExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbEntityRefExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbExceptExpression expression)
    {
      throw new NotSupportedException();
    }

    public override SqlFragment Visit(DbExpression expression)
    {
      throw new InvalidOperationException();
    }

    public override SqlFragment Visit(DbDerefExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbApplyExpression expression)
    {
      throw new NotSupportedException();
    }

    #endregion

    #region DBExpressionVisitor methods normally overridden

    public override SqlFragment Visit(DbUnionAllExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbSortExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbSkipExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbQuantifierExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbProjectExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbNewInstanceExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbLimitExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbJoinExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbGroupByExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbFilterExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbElementExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbDistinctExpression expression)
    {
      throw new NotImplementedException();
    }

    public override SqlFragment Visit(DbCrossJoinExpression expression)
    {
      throw new NotImplementedException();
    }

    #endregion

    protected InputFragment VisitInputExpression(DbExpression e, string name, TypeUsage type)
    {
      SqlFragment f = e.Accept(this);
      Debug.Assert(f is InputFragment);

      InputFragment inputFragment = f as InputFragment;
      inputFragment.Name = name;

      if (inputFragment is TableFragment && type != null)
        (inputFragment as TableFragment).Type = type;

      if ((inputFragment is SelectStatement) && ((inputFragment as SelectStatement).From is TableFragment))
        ((inputFragment as SelectStatement).From as TableFragment).Type = type;

      if (name != null)
        scope.Add(name, inputFragment);

      return inputFragment;
    }

    protected virtual SelectStatement GenerateReturningSql(DbModificationCommandTree tree, DbExpression returning)
    {
      SelectStatement select = new SelectStatement();

      Debug.Assert(returning is DbNewInstanceExpression);
      VisitNewInstanceExpression(select, returning as DbNewInstanceExpression);

      select.From = (InputFragment)tree.Target.Expression.Accept(this);

      ListFragment where = new ListFragment();      
      select.Where = where;
      return select;
    }

    #region Private Methods

    /// <summary>
    /// Examines a binary expression to see if it is an special case suitable to conversion 
    /// to a more efficient and equivalent LIKE sql expression.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="op"></param>
    /// <returns></returns>
    protected LikeFragment TryPromoteToLike(DbExpression left, DbExpression right, string op)
    {
      DbFunctionExpression fl = left as DbFunctionExpression;
      if ((fl != null) && (right is DbConstantExpression))
      {
        LikeFragment like = new LikeFragment();
        if (fl.Function.FullName == "Edm.IndexOf")
        {
          int value = Convert.ToInt32(((DbConstantExpression)right).Value);
          like.Argument = fl.Arguments[1].Accept(this);
          if ((value == 1) && (op == "="))
          {
            DbFunctionExpression fr1;
            DbFunctionExpression fr2;
            if ((fl.Arguments[0] is DbConstantExpression))
            {
              // Case LIKE 'pattern%'
              DbConstantExpression c = (DbConstantExpression)fl.Arguments[0];
              like.Pattern = new LiteralFragment(string.Format("'{0}%'", c.Value));
              return like;
            }
            else if ((fl.Arguments.Count == 2) &&
              ((fr1 = fl.Arguments[0] as DbFunctionExpression) != null) &&
              ((fr2 = fl.Arguments[1] as DbFunctionExpression) != null) &&
              (fr1.Function.FullName == "Edm.Reverse") &&
              (fr2.Function.FullName == "Edm.Reverse"))
            {
              // Case LIKE '%pattern' in EF .NET 4.0
              if (fr1.Arguments[0] is DbConstantExpression)
              {
                DbConstantExpression c = (DbConstantExpression)fr1.Arguments[0];
                like.Pattern = new LiteralFragment(string.Format("'%{0}'", c.Value));
                like.Argument = fr2.Arguments[0].Accept(this);
                return like;
              }
            }
          }
          else if (value == 0)
          {
            if ((op == ">") && (fl.Arguments[0] is DbConstantExpression))
            {
              // Case LIKE '%pattern%'
              DbConstantExpression c = (DbConstantExpression)fl.Arguments[0];
              like.Pattern = new LiteralFragment(string.Format("'%{0}%'", c.Value));
              return like;
            }
          }
        }
        // Like '%pattern' in EF .NET 3.5 (yes, is different than in .NET 4.0)
        else if (fl.Function.FullName == "Edm.Right")
        {
          DbFunctionExpression fLength = fl.Arguments[1] as DbFunctionExpression;
          if ((fLength != null) && (fLength.Function.FullName == "Edm.Length") && (fLength.Arguments[0] is DbConstantExpression))
          {
            DbConstantExpression c2 = fLength.Arguments[0] as DbConstantExpression;
            DbConstantExpression c1 = (DbConstantExpression)right;
            if (c1.Value == c2.Value)
            {
              like.Argument = fl.Arguments[0].Accept(this);
              like.Pattern = new LiteralFragment(string.Format("'%{0}'", c1.Value));
              return like;
            }
          }
        }
      }
      return null;
    }

    protected virtual SqlFragment VisitBinaryExpression(DbExpression left, DbExpression right, string op)
    {
      // Optimization: try to use 'like' instead of 'locate' (Edm.IndexOf) for these
      // cases: (like 'word%'), (like '%word') & (like '%word%').
      LikeFragment like = TryPromoteToLike(left, right, op);
      if (like != null)
        return like;
      // normal flow
      BinaryFragment f = new BinaryFragment();
      f.Operator = op;
      f.Left = left.Accept(this);
      f.WrapLeft = ShouldWrapExpression(left);
      f.Right = right.Accept(this);
      f.WrapRight = ShouldWrapExpression(right);
      // Optimization, try to promote to In expression
      return TryToPromoteToIn(f);
    }

    protected virtual SqlFragment TryToPromoteToIn(BinaryFragment bf)
    {
      // TODO: Remember Morgan's theorem
      // Only try to merge if they are OR'ed.
      if ((bf.Operator == "OR") )
      {
        InFragment inf = bf.Left as InFragment;
        InFragment inf2 = bf.Right as InFragment;
        if (inf == null)
        {
          BinaryFragment bfLeft = bf.Left as BinaryFragment;
          if( bfLeft == null ) 
            return bf;
          if (inf2 == null)
          {
            // try to create a new infragment
            BinaryFragment bfRight = bf.Right as BinaryFragment;
            if (bfRight == null)
              return bf;
            InFragment inff = TryMergeTwoBinaryFragments(bfLeft, bfRight);
            if (inff != null)
              return inff;
          }
          else
          {
            // try to merge an existing infragment & a binaryfragment.
            SqlFragment sf = TryMergeBinaryFragmentAndInFragment(bfLeft, inf2);
            if (sf != null)
              return sf;
          }
        }
        else if (inf2 == null)
        {
          BinaryFragment bfRight = bf.Right as BinaryFragment;
          if (bfRight == null)
            return bf;
          else
          {
            // try to merge an existing infragment & a binaryfragment.
            SqlFragment sf = TryMergeBinaryFragmentAndInFragment(bfRight, inf);
            if (sf != null)
              return sf;
          }
        }
        else
        {
          // try to merge both InFragments
          SqlFragment sf = TryMergeTwoInFragments( inf, inf2 );
          if (sf != null)
            return sf;
        }
      }
      return bf;
    }

    protected InFragment TryMergeTwoInFragments(InFragment infLeft, InFragment infRight)
    {
      if (infLeft.Argument.Equals(infRight.Argument) && 
        ( infLeft.IsNegated == infRight.IsNegated ) && ( !infLeft.IsNegated ) )
      {
        infLeft.InList.AddRange(infRight.InList);
        return infLeft;
      }
      return null;
    }

    protected InFragment TryMergeTwoBinaryFragments(BinaryFragment left, BinaryFragment right)
    {
      if( ( left.IsNegated == right.IsNegated ) && ( ! left.IsNegated ) &&
        ( left.Operator == "=" ) && ( right.Operator == "=" ) )
      {
        ColumnFragment cf;
        LiteralFragment lf;
        GetBinaryFragmentPartsForIn(left, out lf, out cf);
        if ((lf != null) && (cf != null))
        {
          ColumnFragment cf2;
          LiteralFragment lf2;
          GetBinaryFragmentPartsForIn(right, out lf2, out cf2);          
          if ( (lf2 != null) && (cf2 != null) && cf.Equals(cf2))
          {
            InFragment inf = new InFragment();
            inf.Argument = cf;
            inf.InList.Add(lf);
            inf.InList.Add(lf2);
            return inf;
          }
        }
      }
      return null;
    }    

    protected InFragment TryMergeBinaryFragmentAndInFragment(BinaryFragment bf, InFragment inf)
    {
      if( !bf.IsNegated && ( bf.Operator == "=" ))
      {
        ColumnFragment cf;
        LiteralFragment lf;
        GetBinaryFragmentPartsForIn(bf, out lf, out cf);
        if ((lf != null) && (cf != null))
        {
          if (inf.Argument.Equals(cf))
          {
            if (!inf.InList.Contains(lf))
              inf.InList.Add(lf);
            return inf;
          }
        }
      }
      return null;
    }

    protected void GetBinaryFragmentPartsForIn(BinaryFragment bf, out LiteralFragment lf, out ColumnFragment cf)
    {
      cf = bf.Right as ColumnFragment;
      lf = bf.Left as LiteralFragment;
      if (lf == null)
      {
        lf = bf.Right as LiteralFragment;
        cf = bf.Left as ColumnFragment;
      }
    }

    protected bool ShouldWrapExpression(DbExpression e)
    {
      switch (e.ExpressionKind)
      {
        case DbExpressionKind.Property:
        case DbExpressionKind.ParameterReference:
        case DbExpressionKind.Constant:
          return false;
      }
      return true;
    }

    #endregion

  }
}
