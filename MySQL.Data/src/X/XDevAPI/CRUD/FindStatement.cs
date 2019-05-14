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

using System.Collections.Generic;
using MySqlX.XDevAPI.Common;
using System;
using MySql.Data.MySqlClient;
using MySql.Data;

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Represents a chaining collection find statement.
  /// </summary>
  public class FindStatement : FilterableStatement<FindStatement, Collection, DocResult>
  {
    internal FindParams findParams = new FindParams();

    internal FindStatement(Collection c, string condition) : base(c, condition)
    {
    }

    /// <summary>
    /// List of column projections that shall be returned.
    /// </summary>
    /// <param name="columns">List of columns.</param>
    /// <returns>This <see cref="FindStatement"/> object set with the specified columns or fields.</returns>
    public FindStatement Fields(params string[] columns)
    {
      if (columns == null)
        return this;

      var projectionList = new List<string>();
      foreach (var item in columns)
      {
        if (item != null)
          projectionList.Add(item);
      }

      findParams.Projection = projectionList.Count > 0 ? projectionList.ToArray() : null;
      SetChanged();
      return this;
    }

    /// <summary>
    /// Executes the Find statement.
    /// </summary>
    /// <returns>A <see cref="DocResult"/> object containing the results of execution and data.</returns>
    public override DocResult Execute()
    {
      return Execute(Target.Session.XSession.FindDocs, this);
    }

    /// <summary>
    /// Locks matching rows against updates.
    /// </summary>
    /// <param name="lockOption">Optional row <see cref="LockContention">lock option</see> to use.</param>
    /// <returns>This same <see cref="FindStatement"/> object set with the lock shared option.</returns>
    /// <exception cref="MySqlException">The server version is lower than 8.0.3.</exception>
    public FindStatement LockShared(LockContention lockOption = LockContention.Default)
    {
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));

      findParams.Locking = Protocol.X.RowLock.SharedLock;
      findParams.LockingOption = lockOption;
      SetChanged();
      return this;
    }

    /// <summary>
    /// Locks matching rows so no other transaction can read or write to it.
    /// </summary>
    /// <param name="lockOption">Optional row <see cref="LockContention">lock option</see> to use.</param>
    /// <returns>This same <see cref="FindStatement"/> object set with the lock exclusive option.</returns>
    /// <exception cref="MySqlException">The server version is lower than 8.0.3.</exception>
    public FindStatement LockExclusive(LockContention lockOption = LockContention.Default)
    {
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));

      findParams.Locking = Protocol.X.RowLock.ExclusiveLock;
      findParams.LockingOption = lockOption;
      SetChanged();
      return this;
    }

    /// <summary>
    /// Sets the collection aggregation.
    /// </summary>
    /// <param name="groupBy">The field list for aggregation.</param>
    /// <returns>This same <see cref="TableSelectStatement"/> object set with the specified group-by criteria.</returns>
    public FindStatement GroupBy(params string[] groupBy)
    {
      if (groupBy == null)
        return this;

      var groupByList = new List<string>();
      foreach (var item in groupBy)
      {
        if (item != null)
          groupByList.Add(item);
      }

      findParams.GroupBy = groupByList.Count > 0 ? groupByList.ToArray() : null;
      SetChanged();
      return this;
    }

    /// <summary>
    /// Filters criteria for aggregated groups.
    /// </summary>
    /// <param name="having">The filter criteria for aggregated groups.</param>
    /// <returns>This same <see cref="TableSelectStatement"/> object set with the specified filter criteria.</returns>
    public FindStatement Having(string having)
    {
      findParams.GroupByCritieria = having;
      SetChanged();
      return this;
    }

    /// <summary>
    /// Sets user-defined sorting criteria for the operation. The strings use normal SQL syntax like
    /// "order ASC"  or "pages DESC, age ASC".
    /// </summary>
    /// <param name="order">The order criteria.</param>
    /// <returns>A generic object representing the implementing statement type.</returns>
    public FindStatement Sort(params string[] order)
    {
      FilterData.OrderBy = order;
      SetChanged();
      return this;
    }

    /// <summary>
    /// Enables the setting of Where condition for this operation.
    /// </summary>
    /// <param name="condition">The Where condition.</param>
    /// <returns>The implementing statement type.</returns>
    [Obsolete("Where(string condition) has been deprecated since version 8.0.17.")] 
    public new FindStatement Where(string condition)
    {
      return base.Where(condition);
    }
  }
}
