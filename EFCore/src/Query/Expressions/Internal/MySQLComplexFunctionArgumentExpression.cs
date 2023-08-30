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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  internal class MySQLComplexFunctionArgumentExpression : SqlExpression
  {
    /// <summary>
    ///   The arguments parts.
    /// </summary>
    public virtual IReadOnlyList<SqlExpression> ArgumentParts { get; }
    public virtual string Delimiter { get; }

    public MySQLComplexFunctionArgumentExpression(
      IEnumerable<SqlExpression> argumentParts,
      string delimiter,
      Type type,
      RelationalTypeMapping typeMapping)
      : base(type, typeMapping)
    {
      Delimiter = delimiter;
      ArgumentParts = argumentParts.ToList().AsReadOnly();
    }

    /// <summary>
    ///   Dispatches to the specific visit method for this node type.
    /// </summary>
    protected override Expression Accept(ExpressionVisitor visitor) =>
      visitor is MySQLQuerySqlGenerator mySqlQuerySqlGenerator
        ? mySqlQuerySqlGenerator.VisitMySQLComplexFunctionArgumentExpression(this)
        : base.Accept(visitor);
    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
      var argumentParts = new SqlExpression[ArgumentParts.Count];

      for (var i = 0; i < argumentParts.Length; i++)
      {
        argumentParts[i] = (SqlExpression)visitor.Visit(ArgumentParts[i]);
      }

      return Update(argumentParts, Delimiter);
    }

    public virtual MySQLComplexFunctionArgumentExpression Update(IReadOnlyList<SqlExpression> argumentParts, string delimiter)
        => !argumentParts.SequenceEqual(ArgumentParts)
          ? new MySQLComplexFunctionArgumentExpression(argumentParts, delimiter, Type, TypeMapping!)
          : this;


    protected override void Print(ExpressionPrinter expressionPrinter)
      => expressionPrinter.Append(ToString());

    public override bool Equals(object? obj)
      => obj != null
      && (ReferenceEquals(this, obj)
        || obj is MySQLComplexFunctionArgumentExpression sqlFragmentExpression
          && Equals(sqlFragmentExpression));

    private bool Equals(MySQLComplexFunctionArgumentExpression other)
      => base.Equals(other)
         && Type == other.Type
         && ArgumentParts.SequenceEqual(other.ArgumentParts);

    /// <summary>
    ///   Returns a hash code for this object.
    /// </summary>
    /// <returns>
    ///   A hash code for this object.
    /// </returns>
    public override int GetHashCode()
    {
      unchecked
      {
        var hashCode = ArgumentParts.Aggregate(0, (current, argument) => current + ((current * 397) ^ argument.GetHashCode()));
        hashCode = (hashCode * 397) ^ Type.GetHashCode();
        return hashCode;
      }
    }

    /// <summary>
    ///   Creates a <see cref="string" /> representation of the Expression.
    /// </summary>
    /// <returns>A <see cref="string" /> representation of the Expression.</returns>
    public override string ToString()
      => string.Join(" ", ArgumentParts);
  }
}
