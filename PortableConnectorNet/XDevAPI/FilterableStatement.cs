using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI
{
  public abstract class FilterableStatement<T> : CrudStatement
    where T : FilterableStatement<T>
  {
    private FilterParams filter = new FilterParams();

    public FilterableStatement(Collection collection) : base(collection)
    {
    }

    internal FilterParams FilterData
    {
      get { return filter;  }
    }

    public T Where(string condition)
    {
      filter.Condition = condition;
      return (T)this;
    }

    public T Limit(long rows)
    {
      filter.Limit = rows;
      return (T)this;
    }
  }
}
