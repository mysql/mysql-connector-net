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

using MySqlX.Serialization;
using MySqlX.Common;
using System.Collections.Generic;
using System;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Abstract class for filterable statements
  /// </summary>
  /// <typeparam name="T">Filterable statement</typeparam>
  /// <typeparam name="TTarget">Database object</typeparam>
  /// <typeparam name="TResult">Type of Result</typeparam>
  public abstract class FilterableStatement<T, TTarget, TResult> : TargetedBaseStatement<TTarget, TResult>
    where T : FilterableStatement<T, TTarget, TResult>
    where TTarget : DatabaseObject
    where TResult : BaseResult
  {
    private FilterParams filter = new FilterParams();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="target">Database object</param>
    /// <param name="condition">Optional filter condition</param>
    public FilterableStatement(TTarget target, string condition = null) : base(target)
    {
      if (condition != null)
        Where(condition);
    }

    internal FilterParams FilterData
    {
      get { return filter;  }
    }

    /// <summary>
    /// Allows the user to set the where condition for this operation.
    /// </summary>
    /// <param name="condition">Where condition</param>
    /// <returns>The implementing statement type</returns>
    public T Where(string condition)
    {
      filter.Condition = condition;
      return (T)this;
    }

    /// <summary>
    /// Allows the user to set the limit and offset for the operation
    /// </summary>
    /// <param name="rows">How many items should be returned</param>
    /// <returns>The implementing statement type</returns>
    public T Limit(long rows)
    {
      filter.Limit = rows;
      filter.Offset = -1;
      return (T)this;
    }

    /// <summary>
    /// Allows the user to set the sorting criteria for the operation.  The strings use normal SQL syntax like
    /// "order ASC"  or "pages DESC, age ASC"
    /// </summary>
    /// <param name="order">The order criteria</param>
    /// <returns>The implementing statement type</returns>
    public T OrderBy(params string[] order)
    {
      filter.OrderBy = order;
      return (T)this;
    }

    /// <summary>
    /// Binds the parameter values in filter expression
    /// </summary>
    /// <param name="parameterName">Parameter name</param>
    /// <param name="value">Value of parameter</param>
    /// <returns>The implementing statement type</returns>
    public T Bind(string parameterName, object value)
    {
      FilterData.Parameters.Add(parameterName, value);
      return (T)this;
    }

    /// <summary>
    /// Binds the parameter values in filter expression
    /// </summary>
    /// <param name="dbDocParams">Parameters as DbDoc object</param>
    /// <returns>The implementing statement type</returns>
    public T Bind(DbDoc dbDocParams)
    {
      return Bind(dbDocParams.ToString());
    }

    /// <summary>
    /// Binds the parameter values in filter expression
    /// </summary>
    /// <param name="jsonParams">Parameters as JSON string</param>
    /// <returns>The implementing statement type</returns>
    public T Bind(string jsonParams)
    {
      foreach(var item in JsonParser.Parse(jsonParams))
      {
        Bind(item.Key, item.Value);
      }
      return (T)this;
    }

    /// <summary>
    /// Binds the parameter values in filter expression
    /// </summary>
    /// <param name="jsonParams">Parameters as anonymous: new { param1 = value1, param2 = value2, ... }</param>
    /// <returns>The implementing statement type</returns>
    public T Bind(object jsonParams)
    {
      return Bind(new DbDoc(jsonParams));
    }

    protected virtual TResult Execute(Func<T, TResult> executeFunc, T t)
    {
      try
      {
        return executeFunc(t);
      }
      finally
      {
        FilterData.Parameters.Clear();
      }
    }
  }
}
