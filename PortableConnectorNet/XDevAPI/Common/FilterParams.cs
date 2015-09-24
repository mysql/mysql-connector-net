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
