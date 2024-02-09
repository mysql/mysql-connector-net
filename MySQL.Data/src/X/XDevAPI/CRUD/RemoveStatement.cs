// Copyright © 2015, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using MySqlX.XDevAPI.Common;
using System;

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Represents a chaining collection remove statement.
  /// <typeparam name="T"/>
  /// </summary>
  public class RemoveStatement<T> : FilterableStatement<RemoveStatement<T>, Collection<T>, Result, T>
  {
    internal RemoveStatement(Collection<T> collection, string condition) : base(collection, condition)
    {
    }

    /// <summary>
    /// Sets user-defined sorting criteria for the operation. The strings use normal SQL syntax like
    /// "order ASC" or "pages DESC, age ASC".
    /// </summary>
    /// <param name="order">The order criteria.</param>
    /// <returns>A generic object representing the implementing statement type.</returns>
    public RemoveStatement<T> Sort(params string[] order)
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
    public new RemoveStatement<T> Where(string condition)
    {
      return base.Where(condition);
    }

    /// <summary>
    /// Executes the remove statement.
    /// </summary>
    /// <returns>A <see cref="Result"/>object containing the results of the execution.</returns>
    public override Result Execute()
    {
      return Execute(Target.Session.XSession.DeleteDocs, this);
    }
  }
}
