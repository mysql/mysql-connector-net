// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.Protocol.X;
using Mysqlx.Crud;
using Mysqlx.Expr;
using System.Collections.Generic;
using Mysqlx.Datatypes;
using System;
using MySqlX.Properties;

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
        paramsList[placeholderNameToPosition[param.Key.ToLowerInvariant()]] = ExprUtil.ArgObjectToScalar(param.Value);
      }
      return paramsList;
    }
  }
}
