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

namespace MySql.XDevAPI.Statements
{
  public abstract class FilterableStatement<T, TOwner, TResult> : BaseStatement<TOwner, TResult>
    where T : FilterableStatement<T, TOwner, TResult>
    where TOwner : DatabaseObject
  {
    private FilterParams filter = new FilterParams();

    public FilterableStatement(TOwner owner, string condition = null) : base(owner)
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
    /// <returns>The implementing statement type</returns>    /// <returns></returns>
    public T OrderBy(params string[] order)
    {
      filter.OrderBy = order;
      return (T)this;
    }
  }
}
