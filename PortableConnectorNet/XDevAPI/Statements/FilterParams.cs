using MySql.Protocol.X;
using Mysqlx.Crud;
using Mysqlx.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI.Statements
{
  internal class FilterParams
  {
    public long Limit = -1;
    public long Offset = -1;
    public string Condition;
    public string OrderBy;

    public bool HasLimit
    {
      get { return Limit != -1; }
    }

    public List<Order> GetOrderByExpressions(bool allowRelational)
    {
      return new ExprParser(OrderBy, allowRelational).ParseOrderSpec();
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
  }
}
