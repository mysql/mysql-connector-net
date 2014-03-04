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
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

namespace MySql.Data.Entity
{
  internal abstract class SqlFragment
  {
    static char[] quoteChars = new char[3] { '\'', '"', '`' };

    protected string QuoteIdentifier(string id)
    {
      if (id.IndexOfAny(quoteChars) < 0)
        return String.Format("`{0}`", id);
      else return id;
    }

    public abstract void WriteSql(StringBuilder sql);

    public override string ToString()
    {
      StringBuilder sqlText = new StringBuilder();
      WriteSql(sqlText);
      return sqlText.ToString();
    }

    protected void WriteList(IEnumerable list, StringBuilder sql)
    {
      string sep = "";
      foreach (SqlFragment s in list)
      {
        sql.Append(sep);
        sql.Append("\r\n");
        s.WriteSql(sql);
        sep = ", ";
      }
    }

    internal abstract void Accept(SqlFragmentVisitor visitor);
  }

  /// <summary>
  /// Visitor for SqlFragment and derived classes, useful for postprocessing and optimizations.
  /// </summary>
  internal interface SqlFragmentVisitor
  {
    void Visit(SqlFragment f);
    void Visit(IsNullFragment f);
    void Visit(CaseFragment f);
    void Visit(ExistsFragment f);
    void Visit(LikeFragment f);
    void Visit(FunctionFragment f);
    void Visit(InFragment f);
    void Visit(BinaryFragment f);
    void Visit(ColumnFragment f);
    void Visit(LiteralFragment f);
  }

  /// <summary>
  /// Visitor to replace old table names with new table names. Used as part postprocessing of the code for fusing nested selects.
  /// </summary>
  internal class ReplaceTableNameVisitor : SqlFragmentVisitor
  {
    private string _oldTableName;
    private string _newTableName;

    internal ReplaceTableNameVisitor(string oldTableName, string newTableName)
    {
      _oldTableName = oldTableName;
      _newTableName = newTableName;
    }

    public void Visit(SqlFragment f)
    {
    }

    public void Visit(IsNullFragment f)
    {
    }

    public void Visit(CaseFragment f)
    {
    }

    public void Visit(ExistsFragment f)
    {
    }

    public void Visit(LikeFragment f)
    {
    }

    public void Visit(FunctionFragment f)
    {
    }

    public void Visit(InFragment f)
    { 
    }

    public void Visit(BinaryFragment f)
    {
    }

    public void Visit(ColumnFragment f)
    {
      if ((f != null) && (f.TableName == _oldTableName))
        f.TableName = _newTableName;
    }

    public void Visit(LiteralFragment f)
    {
      // In Code first, functions like IEnumerable.Contains, are translated to strings (LiteralFragment)
      // instead of a FunctionFragment.
      f.Literal = f.Literal.Replace(string.Format("`{0}`", _oldTableName),
        string.Format("`{0}`", _newTableName));
    }
  }  

  internal class BinaryFragment : NegatableFragment
  {
    public SqlFragment Left;
    public SqlFragment Right;
    public string Operator;
    public bool WrapLeft;
    public bool WrapRight;

    public override void WriteSql(StringBuilder sql)
    {
      if (IsNegated && Operator != "=")
        sql.Append("NOT (");

      // do left arg
      if (WrapLeft)
        sql.Append("(");
      Left.WriteSql(sql);
      if (WrapLeft)
        sql.Append(")");

      if (IsNegated && Operator == "=")
        sql.Append(" != ");
      else
        sql.AppendFormat(" {0} ", Operator);

      // now right arg
      if (WrapRight)
        sql.Append("(");
      Right.WriteSql(sql);
      if (WrapRight)
        sql.Append(")");
      if (IsNegated && Operator != "=")
        sql.Append(")");
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      Left.Accept(visitor);
      Right.Accept(visitor);
      visitor.Visit(this);
    }
  }

  internal class InFragment : NegatableFragment
  {
    public List<LiteralFragment> InList;
    public ColumnFragment Argument;

    internal InFragment()
    {
      InList = new List<LiteralFragment>();
    }

    public override void WriteSql(StringBuilder sql)
    {
      Argument.WriteSql(sql);
      if (IsNegated)
        sql.Append(" NOT ");
      sql.Append(" IN ( ");
      foreach (LiteralFragment lit in InList)
      {
        sql.Append(lit).Append(",");
      }
      sql.Length = sql.Length - 1;
      sql.Append(" )");
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      Argument.Accept(visitor);
      for (int i = 0; i < InList.Count; i++)
        InList[i].Accept(visitor);
      visitor.Visit(this);
    }
  }

  internal class CaseFragment : SqlFragment
  {
    public List<SqlFragment> When = new List<SqlFragment>();
    public List<SqlFragment> Then = new List<SqlFragment>();
    public SqlFragment Else = null;

    public override void WriteSql(StringBuilder sql)
    {
      sql.Append("CASE");
      for (int i = 0; i < When.Count; i++)
      {
        sql.Append(" WHEN (");
        When[i].WriteSql(sql);
        sql.Append(") THEN (");
        Then[i].WriteSql(sql);
        sql.Append(") ");
      }
      if (Else != null)
      {
        sql.Append(" ELSE (");
        Else.WriteSql(sql);
        sql.Append(") ");
      }
      sql.Append("END");
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      Else.Accept(visitor);
      for (int i = 0; i < Then.Count; i++)
        Then[i].Accept(visitor);
      for (int i = 0; i < When.Count; i++)
        When[i].Accept(visitor);
      visitor.Visit(this);
    }
  }

  internal class ColumnFragment : SqlFragment
  {
    public ColumnFragment(string tableName, string columnName)
    {
      TableName = tableName;
      ColumnName = columnName;
    }

    public SqlFragment Literal { get; set; }
    public string TableName { get; set; }
    public string ColumnName { get; set; }
    public string ColumnAlias { get; set; }
    public string ActualColumnName
    {
      get { return String.IsNullOrEmpty(ColumnName) ? ColumnAlias : ColumnName; }
    }
    public PropertyFragment PropertyFragment { get; set; }

    public override void WriteSql(StringBuilder sql)
    {
      if (Literal != null)
      {
        Debug.Assert(ColumnAlias != null);
        Literal.WriteSql(sql);
      }
      else
      {
        if (TableName != null)
          sql.AppendFormat("{0}.", QuoteIdentifier(TableName));
        sql.AppendFormat("{0}", QuoteIdentifier(ColumnName));
      }

      if (ColumnAlias != null && ColumnAlias != ColumnName)
        sql.AppendFormat(" AS {0}", QuoteIdentifier(ColumnAlias));
    }

    public ColumnFragment Clone()
    {
      ColumnFragment cf = new ColumnFragment(TableName, ColumnName);
      cf.ColumnAlias = ColumnAlias;
      cf.Literal = Literal;
      return cf;
    }

    public void PushInput(string inputName)
    {
      if (PropertyFragment == null)
        PropertyFragment = new PropertyFragment();
      PropertyFragment.PushProperty(inputName);
    }

    public override bool Equals(object obj)
    {
      if (!(obj is ColumnFragment)) return false;
      ColumnFragment column = obj as ColumnFragment;
      if (column.PropertyFragment != null && PropertyFragment != null)
        return column.PropertyFragment.Equals(PropertyFragment);
      if (column.TableName != TableName) return false;
      if (column.ColumnName != ColumnName) return false;
      if (column.ColumnAlias != ColumnAlias) return false;
      return true;
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      visitor.Visit(this);
    }
  }

  internal class ExistsFragment : NegatableFragment
  {
    public SqlFragment Argument;

    public ExistsFragment(SqlFragment f)
    {
      Argument = f;
    }

    public override void WriteSql(StringBuilder sql)
    {
      sql.Append(IsNegated ? "NOT " : "");
      sql.Append("EXISTS(");
      Argument.WriteSql(sql);
      sql.Append(")");
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      Argument.Accept(visitor);
      visitor.Visit(this);
    }
  }

  internal class FunctionFragment : SqlFragment
  {
    public bool Distinct;
    public SqlFragment Argument;
    public string Name;
    public bool Quoted;

    public override void WriteSql(StringBuilder sql)
    {
      string name = Quoted ? QuoteIdentifier(Name) : Name;
      sql.AppendFormat("{0}({1}", name, Distinct ? "DISTINCT " : "");
      Argument.WriteSql(sql);
      sql.Append(")");
    }

    internal override void  Accept(SqlFragmentVisitor visitor)
    {
      Argument.Accept(visitor);
    }
  }

  internal class IsNullFragment : NegatableFragment
  {
    public SqlFragment Argument;

    public override void WriteSql(StringBuilder sql)
    {
      Argument.WriteSql(sql);
      sql.AppendFormat(" IS {0} NULL", IsNegated ? "NOT" : "");
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      Argument.Accept(visitor);
      visitor.Visit(this);
    }
  }

  internal class LikeFragment : NegatableFragment
  {
    public SqlFragment Argument;
    public SqlFragment Pattern;
    public SqlFragment Escape;

    public override void WriteSql(StringBuilder sql)
    {
      Argument.WriteSql(sql);
      if (IsNegated)
        sql.Append(" NOT ");
      sql.Append(" LIKE ");
      Pattern.WriteSql(sql);
      if (Escape != null)
      {
        sql.Append(" ESCAPE ");
        Escape.WriteSql(sql);
      }
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      Argument.Accept(visitor);
      visitor.Visit(this);
    }
  }

  internal class ListFragment : SqlFragment
  {
    public List<SqlFragment> Fragments = new List<SqlFragment>();

    public void Append(string s)
    {
      Fragments.Add(new LiteralFragment(s));
    }

    public void Append(SqlFragment s)
    {
      Fragments.Add(s);
    }

    public override void WriteSql(StringBuilder sql)
    {
      foreach (SqlFragment f in Fragments)
        f.WriteSql(sql);
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      for (int i = 0; i < Fragments.Count; i++)
        Fragments[i].Accept(visitor);
    }
  }

  internal class NegatableFragment : SqlFragment
  {
    public bool IsNegated;

    public void Negate()
    {
      IsNegated = !IsNegated;
    }

    public override void WriteSql(StringBuilder sql)
    {
      Debug.Fail("This method should be overridden");
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      throw new System.NotImplementedException();
    }
  }

  internal class LiteralFragment : SqlFragment
  {
    public string Literal;

    public LiteralFragment(string literal)
    {
      Literal = literal;
    }

    public override void WriteSql(StringBuilder sql)
    {
      sql.Append(Literal);
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      visitor.Visit(this);
    }
  }

  internal class PropertyFragment : SqlFragment
  {
    public PropertyFragment()
    {
      Properties = new List<string>();
    }

    public List<string> Properties { get; private set; }

    public override void WriteSql(StringBuilder sql)
    {
      throw new NotImplementedException();
    }

    public string LastProperty
    {
      get { return Properties.Count == 0 ? null : Properties[Properties.Count - 1]; }
    }

    public void Trim(string name)
    {
      int index = Properties.LastIndexOf(name);
      Properties.RemoveRange(index + 1, Properties.Count - index - 1);
    }

    public void PushProperty(string property)
    {
      Properties.Insert(0, property);
    }

    public override bool Equals(object obj)
    {
      if (!(obj is PropertyFragment)) return false;
      PropertyFragment prop = obj as PropertyFragment;
      Debug.Assert(Properties != null && prop.Properties != null);
      int aIndex = Properties.Count - 1;
      int bIndex = prop.Properties.Count - 1;
      int cnt = 0;
      while (aIndex >= 0 && bIndex >= 0)
      {
        if (String.Compare(Properties[aIndex--], prop.Properties[bIndex--], true) != 0) return false;
        if (++cnt == 2) break;
      }
      return true;
    }

    public PropertyFragment Clone()
    {
      PropertyFragment newPF = new PropertyFragment();
      foreach (string prop in Properties)
        newPF.Properties.Add(prop);
      return newPF;
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      throw new System.NotImplementedException();
    }
  }

  internal class SortFragment : SqlFragment
  {
    public SortFragment(SqlFragment column, bool ascending)
    {
      Column = column;
      Ascending = ascending;
    }

    public SqlFragment Column
    {
      get;
      set;
    }
    public bool Ascending { get; set; }

    public override void WriteSql(StringBuilder sql)
    {
      ColumnFragment columnFragment = Column as ColumnFragment;
      Debug.Assert(columnFragment != null);
      columnFragment.WriteSql(sql);
      sql.AppendFormat(" {0}", Ascending ? "ASC" : "DESC");
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      Column.Accept(visitor);
    }
  }

  internal class UnionFragment : InputFragment
  {
    public bool Distinct = false;

    public override void WriteInnerSql(StringBuilder sql)
    {
      sql.Append('(');
      Left.WriteSql(sql);
      sql.Append(')');
      sql.Append(Distinct ? " UNION DISTINCT " : " UNION ALL ");
      sql.Append('(');
      Right.WriteSql(sql);
      sql.Append(')');
    }

    public bool HasDifferentNameForColumn(ColumnFragment column)
    {
      Debug.Assert(Left is SelectStatement);
      Debug.Assert(Right is SelectStatement);
      if ((Left as SelectStatement).HasDifferentNameForColumn(column))
        return true;
      return (Right as SelectStatement).HasDifferentNameForColumn(column);
    }

    internal override void Accept(SqlFragmentVisitor visitor)
    {
      throw new System.NotImplementedException();
    }
  }


}
