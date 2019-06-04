// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.Serialization;
using MySqlX.Common;
using System.Collections.Generic;
using System;
using MySql.Data;
using System.Collections;
using System.Linq;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Abstract class for filterable statements.
  /// </summary>
  /// <typeparam name="T">The filterable statement.</typeparam>
  /// <typeparam name="TTarget">The database object.</typeparam>
  /// <typeparam name="TResult">The type of result.</typeparam>
  public abstract class FilterableStatement<T, TTarget, TResult> : TargetedBaseStatement<TTarget, TResult>
    where T : FilterableStatement<T, TTarget, TResult>
    where TTarget : DatabaseObject
    where TResult : BaseResult
  {
    private FilterParams filter = new FilterParams();

    /// <summary>
    /// Initializes a new instance of the FiltarableStatement class based on the target and condition.
    /// </summary>
    /// <param name="target">The database object.</param>
    /// <param name="condition">The optional filter condition.</param>
    public FilterableStatement(TTarget target, string condition = null) : base(target)
    {
      if (condition != null)
        Where(condition);
    }

    internal FilterParams FilterData
    {
      get { return filter; }
    }

    /// <summary>
    /// Enables the setting of Where condition for this operation.
    /// </summary>
    /// <param name="condition">The Where condition.</param>
    /// <returns>The implementing statement type.</returns>
    public T Where(string condition)
    {
      filter.Condition = condition;
      SetChanged();
      return (T)this;
    }

    /// <summary>
    /// Sets the number of items to be returned by the operation.
    /// </summary>
    /// <param name="rows">The number of items to be returned.</param>
    /// <returns>The implementing statement type.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rows"/> is equal or lower than 0.</exception>
    public T Limit(long rows)
    {
      if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), string.Format(ResourcesX.NumberNotGreaterThanZero, nameof(rows)));
      filter.Limit = rows;
      return (T)this;
    }

    /// <summary>
    /// Sets the number of items to be skipped before including them into the result.
    /// </summary>
    /// <param name="rows">The number of items to be skipped.</param>
    /// <returns>The implementing statement type.</returns>
    public T Offset(long rows)
    {
      filter.Offset = rows;
      return (T)this;
    }

    /// <summary>
    /// Binds the parameter values in filter expression.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>A generic object representing the implementing statement type.</returns>
    public T Bind(string parameterName, object value)
    {
      FilterData.Parameters[parameterName.ToLowerInvariant()] = value is string ? QuoteString((string)value) : value;
      return (T)this;
    }

    /// <summary>
    /// Binds the parameter values in filter expression.
    /// </summary>
    /// <param name="dbDocParams">The parameters as a DbDoc object.</param>
    /// <returns>A generic object representing the implementing statement type.</returns>
    public T Bind(DbDoc dbDocParams)
    {
      return Bind(dbDocParams.ToString());
    }

    /// <summary>
    /// Binds the parameter values in filter expression.
    /// </summary>
    /// <param name="jsonParams">The parameters as a JSON string.</param>
    /// <returns>The implementing statement type.</returns>
    public T Bind(string jsonParams)
    {
      foreach (var item in JsonParser.Parse(jsonParams))
      {
        Bind(item.Key, item.Value);
      }
      return (T)this;
    }

    /// <summary>
    /// Binds the parameter values in filter expression.
    /// </summary>
    /// <param name="jsonParams">The parameters as an anonymous object: new { param1 = value1, param2 = value2, ... }.</param>
    /// <returns>The implementing statement type.</returns>
    public T Bind(object jsonParams)
    {
      return Bind(new DbDoc(jsonParams));
    }

    /// <summary>
    /// Executes the statement.
    /// </summary>
    /// <param name="executeFunc">The function to execute.</param>
    /// <param name="t">The generic object to use.</param>
    /// <returns>A generic result object containing the results of the execution.</returns>
    protected virtual TResult Execute(Func<T, TResult> executeFunc, T t)
    {
      try
      {
        ValidateOpenSession();
        List<object> parameters = new List<object>(FilterData.Parameters.Values);
        if (_isPrepared && FilterData.hadLimit != FilterData.HasLimit)
        {
          SetChanged();
        }
        // Add the prepared statement placeholder values for limit and offset
        if (!_hasChanged)
        {
          // Limit and offset placeholder values
          if (FilterData.HasLimit)
          {
            parameters.Add(FilterData.Limit);
            parameters.Add(FilterData.Offset == -1 ? 0 : FilterData.Offset);
          }
        }
        var result = ConvertToPreparedStatement<T>(executeFunc, t, parameters);
        _hasChanged = false;
        return result;
      }
      finally
      {
        FilterData.hadLimit = FilterData.HasLimit;
        FilterData.hadOffset = FilterData.Offset != -1;
      }
    }

    /// <summary>
    /// Clones the filterable data but Session and Target remain the
    /// same.
    /// </summary>
    /// <returns>A clone of this filterable statement.</returns>
    public virtual T Clone()
    {
      var t = (T)this.MemberwiseClone();
      t.filter = t.FilterData.Clone();
      return t;
    }

    private static string QuoteString(string value)
    {
      return "'" + value.Trim().Replace("'", "\\'") + "'";
    }
  }
}
