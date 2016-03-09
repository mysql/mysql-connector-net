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

using System.Collections.Generic;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a chaining table select statement
  /// </summary>
  public class TableSelectStatement : FilterableStatement<TableSelectStatement, Table, RowResult>
  {
    internal FindParams findParams = new FindParams();

    internal TableSelectStatement(Table t, params string[] projection) : base(t)
    {
      findParams.Projection = projection;
      FilterData.IsRelational = true;
    }

    /// <summary>
    /// Set table aggregation
    /// </summary>
    /// <param name="groupBy">Column list for aggregation</param>
    /// <returns>This same TableSelectStatement object</returns>
    public TableSelectStatement GroupBy(params string[] groupBy)
    {
      findParams.GroupBy = groupBy;
      return this;
    }

    /// <summary>
    /// Filter criteria for aggregated groups
    /// </summary>
    /// <param name="having">Filter criteria for aggregated groups</param>
    /// <returns>This same TableSelectStatement object</returns>
    public TableSelectStatement Having(string having)
    {
      findParams.GroupByCritieria = having;
      return this;
    }

    /// <summary>
    /// Executes the select statement
    /// </summary>
    /// <returns>Result of execution and data</returns>
    public override RowResult Execute()
    {
      return Execute(Target.Session.XSession.FindRows, this);
    }

    /// <summary>
    /// Allows the user to set the limit and offset for the operation
    /// </summary>
    /// <param name="rows">How many items should be returned</param>
    /// <param name="offset">How many items should be skipped</param>
    /// <returns>This same TableSelectStatement object</returns>
    public TableSelectStatement Limit(long rows, long offset)
    {
      FilterData.Limit = rows;
      FilterData.Offset = offset;
      return this;
    }
  }
}
