// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System.Collections.Generic;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using MySql.Data.MySqlClient;
using MySql.Data;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a chaining table select statement.
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
    /// Sets table aggregation.
    /// </summary>
    /// <param name="groupBy">The column list for aggregation.</param>
    /// <returns>This same <see cref="TableSelectStatement"/> object.</returns>
    public TableSelectStatement GroupBy(params string[] groupBy)
    {
      findParams.GroupBy = groupBy;
      return this;
    }

    /// <summary>
    /// Filters criteria for aggregated groups.
    /// </summary>
    /// <param name="having">The filter criteria for aggregated groups.</param>
    /// <returns>This same <see cref="TableSelectStatement"/> object set with the specified group by criteria.</returns>
    public TableSelectStatement Having(string having)
    {
      findParams.GroupByCritieria = having;
      return this;
    }

    /// <summary>
    /// Executes the select statement.
    /// </summary>
    /// <returns>A <see cref="Result"/> object containing the results of the execution and data.</returns>
    public override RowResult Execute()
    {
      return Execute(Target.Session.XSession.FindRows, this);
    }

    /// <summary>
    /// Allows the user to set the limit and offset for the operation.
    /// </summary>
    /// <param name="rows">The number of items to be returned.</param>
    /// <param name="offset">The number of items to be skipped.</param>
    /// <returns>This same <see cref="TableSelectStatement"/> object set with the specified limit.</returns>
    public TableSelectStatement Limit(long rows, long offset)
    {
      FilterData.Limit = rows;
      FilterData.Offset = offset;
      return this;
    }

    /// <summary>
    /// Locks matching rows against updates.
    /// </summary>
    /// <param name="lockOption">Optional row <see cref="LockContention">lock option</see> to use.</param>
    /// <returns>This same <see cref="TableSelectStatement"/> object set with lock shared option.</returns>
    /// <exception cref="MySqlException">The server version is lower than 8.0.3.</exception>
    public TableSelectStatement LockShared(LockContention lockOption = LockContention.Default)
    {
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8,0,3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));

      findParams.Locking = Protocol.X.RowLock.SharedLock;
      findParams.LockingOption = lockOption;
      return this;
    }

    /// <summary>
    /// Locks matching rows so no other transaction can read or write to it.
    /// </summary>
    /// <param name="lockOption">Optional row <see cref="LockContention">lock option</see> to use.</param>
    /// <returns>This same <see cref="TableSelectStatement"/> object set with the lock exclusive option.</returns>
    /// <exception cref="MySqlException">The server version is lower than 8.0.3.</exception>
    public TableSelectStatement LockExclusive(LockContention lockOption = LockContention.Default)
    {
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8,0,3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));

      findParams.Locking = Protocol.X.RowLock.ExclusiveLock;
      findParams.LockingOption = lockOption;
      return this;
    }
  }
}
