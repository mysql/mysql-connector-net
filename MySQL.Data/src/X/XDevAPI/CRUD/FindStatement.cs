// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
    internal List<string> projection;
    internal string[] orderBy;
    internal FindParams findParams = new FindParams();


    internal FindStatement(Collection c, string condition) : base (c, condition)
    {
    }

    /// <summary>
    /// List of column projections that shall be returned.
    /// </summary>
    /// <param name="columns">List of columns.</param>
    /// <returns>This <see cref="FindStatement"/> object set with the specified columns/fields.</returns>
    public FindStatement Fields(params string[] columns)
    {
      projection = new List<string>(columns);
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
    /// Allows the user to set the limit and offset for the operation.
    /// </summary>
    /// <param name="rows">Number of items to be returned.</param>
    /// <param name="offset">Number of items to be skipped.</param>
    /// <returns>This same <see cref="FindStatement"/> object set with the specified limit.</returns>
    public FindStatement Limit(long rows, long offset)
    {
      FilterData.Limit = rows;
      FilterData.Offset = offset;
      return this;
    }

    /// <summary>
    /// Locks matching rows against updates.
    /// </summary>
    /// <returns>This same <see cref="FindStatement"/> object set with the lock shared option.</returns>
    /// <exception cref="MySqlException">The server version is lower than 8.0.3.</exception>
    public FindStatement LockShared()
    {
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8,0,3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));

      findParams.Locking = Protocol.X.RowLock.SharedLock;
      return this;
    }

    /// <summary>
    /// Locks matching rows so no other transaction can read or write to it.
    /// </summary>
    /// <returns>This same <see cref="FindStatement"/> object set with the lock exclusive option.</returns>
    /// <exception cref="MySqlException">The server version is lower than 8.0.3.</exception>
    public FindStatement LockExclusive()
    {
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8,0,3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));

      findParams.Locking = Protocol.X.RowLock.ExclusiveLock;
      return this;
    }
  }
}
