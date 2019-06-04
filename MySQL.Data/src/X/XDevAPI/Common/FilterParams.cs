// Copyright © 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.Protocol.X;
using Mysqlx.Crud;
using Mysqlx.Expr;
using System.Collections.Generic;
using Mysqlx.Datatypes;
using System;
using MySqlX;
using MySql.Data;

namespace MySqlX.XDevAPI.Common
{
  internal class FilterParams
  {
    public long Limit = -1;
    public long Offset = -1;
    public string Condition;
    public Dictionary<string, object> Parameters = new Dictionary<string, object>();
    public Dictionary<string, int> placeholderNameToPosition;
    public bool IsRelational;
    public string[] OrderBy;
    public bool hadLimit = false;
    public bool hadOffset = false;

    public bool HasLimit
    {
      get { return Limit != -1; }
    }

    public List<Order> GetOrderByExpressions(bool allowRelational)
    {
      return new ExprParser(ExprUtil.JoinString(OrderBy), allowRelational).ParseOrderSpec();
    }

    public Expr GetConditionExpression(bool allowRelational)
    {
      ExprParser parser = new ExprParser(Condition, allowRelational);
      Expr expr = parser.Parse();
      if (parser.GetPositionalPlaceholderCount() > 0)
      {
        this.placeholderNameToPosition = parser.GetPlaceholderNameToPositionMap();
      }
      return expr;
    }

    public IEnumerable<Scalar> GetArgsExpression(Dictionary<string, object> parameters)
    {
      if (placeholderNameToPosition == null || placeholderNameToPosition.Count == 0)
        throw new ArgumentException(ResourcesX.NoPlaceholders);

      Scalar[] paramsList = new Scalar[placeholderNameToPosition.Count];
      foreach (var param in parameters)
      {
        if (!placeholderNameToPosition.ContainsKey(param.Key.ToLowerInvariant()))
          throw new ArgumentNullException(string.Format(ResourcesX.UnknownPlaceholder, param.Key));
        paramsList[placeholderNameToPosition[param.Key.ToLowerInvariant()]] = ExprUtil.ArgObjectToScalar(param.Value)
          ?? throw new ArgumentException(param.Key);
      }
      return paramsList;
    }

    public FilterParams Clone()
    {
      return (FilterParams)this.MemberwiseClone();
    }
  }
}
