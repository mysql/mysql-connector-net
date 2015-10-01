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

using MySql.Protocol.X;
using Mysqlx.Crud;
using Mysqlx.Expr;
using System.Collections.Generic;
using Mysqlx.Datatypes;

namespace MySql.XDevAPI.Common
{
  internal class FilterParams
  {
    public long Limit = -1;
    public long Offset = -1;
    public string Condition;
    public Dictionary<string, object> Parameters;
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
      return expr;
//      if (parser.getPositionalPlaceholderCount() > 0)
  //    {
    //    this.placeholderNameToPosition = parser.getPlaceholderNameToPositionMap();
      //  this.args = new ArrayList<>(parser.getPositionalPlaceholderCount());
      //}
    }

    public IEnumerable<Scalar> GetArgsExpression(Dictionary<string, object> parameters)
    {
      List<Scalar> paramsList = new List<Scalar>();
      foreach (var param in parameters)
      {
        paramsList.Add(ExprUtil.ArgObjectToScalar(param.Value));
      }
      return paramsList;
    }
  }
}
