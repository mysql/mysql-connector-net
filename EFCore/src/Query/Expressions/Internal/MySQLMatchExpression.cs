// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  internal class MySQLMatchExpression : SqlExpression
  {
    public MySQLMatchExpression(
      SqlExpression match,
      SqlExpression against,
      MySQLMatchSearchMode searchMode,
      RelationalTypeMapping? typeMapping)
      : base(typeof(bool), typeMapping)
    {
      Check.NotNull(match, nameof(match));
      Check.NotNull(against, nameof(against));

      Match = match;
      Against = against;
      SearchMode = searchMode;
    }

    public virtual MySQLMatchSearchMode SearchMode { get; }

    public virtual SqlExpression Match { get; }
    public virtual SqlExpression Against { get; }

    protected override Expression Accept(ExpressionVisitor visitor)
    {
      Check.NotNull(visitor, nameof(visitor));

      return visitor is MySQLQuerySqlGenerator mySqlQuerySqlGenerator
        ? mySqlQuerySqlGenerator.VisitMySqlMatch(this)
        : base.Accept(visitor);
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
      var match = (SqlExpression)visitor.Visit(Match);
      var against = (SqlExpression)visitor.Visit(Against);

      return Update(match, against);
    }

    public virtual MySQLMatchExpression Update(SqlExpression match, SqlExpression against)
      => match != Match || against != Against
        ? new MySQLMatchExpression(
          match,
          against,
          SearchMode,
          TypeMapping!)
        : this;

    public override bool Equals(object? obj)
      => obj != null && ReferenceEquals(this, obj)
      || obj is MySQLMatchExpression matchExpression && Equals(matchExpression);

    private bool Equals(MySQLMatchExpression matchExpression)
      => base.Equals(matchExpression)
      && SearchMode == matchExpression.SearchMode
      && Match.Equals(matchExpression.Match)
      && Against.Equals(matchExpression.Against);

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), SearchMode, Match, Against);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
      expressionPrinter.Append("MATCH ");
      expressionPrinter.Append($"({expressionPrinter.Visit(Match)})");
      expressionPrinter.Append(" AGAINST ");
      expressionPrinter.Append($"({expressionPrinter.Visit(Against)}");

      switch (SearchMode)
      {
        case MySQLMatchSearchMode.NaturalLanguage:
          break;
        case MySQLMatchSearchMode.NaturalLanguageWithQueryExpansion:
          expressionPrinter.Append(" WITH QUERY EXPANSION");
          break;
        case MySQLMatchSearchMode.Boolean:
          expressionPrinter.Append(" IN BOOLEAN MODE");
          break;
      }

      expressionPrinter.Append(")");
    }
  }
}
