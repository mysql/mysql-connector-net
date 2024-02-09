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

using System;
using System.Collections.Generic;
#if !NETFRAMEWORK
using System.Text.Json;
#endif
using MySqlX.XDevAPI.Common;

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Represents a collection statement.
  /// </summary>
  /// <typeparam name="TResult">Type of <see cref="Result"/></typeparam>
  /// <typeparam name="T">Type of object</typeparam>
  public abstract class CrudStatement<TResult, T> : TargetedBaseStatement<Collection<T>, TResult, T>
    where TResult : Result
  {
    internal CrudStatement(Collection<T> collection) : base(collection)
    {
    }

    /// <summary>
    /// Converts base <see cref="System.Object"/>s into <typeparamref name="T"/> objects.
    /// </summary>
    /// <param name="items">Array of objects to be converted to <typeparamref name="T"/> objects.</param>
    /// <returns>An enumerable collection of <typeparamref name="T"/> objects.</returns>
    protected IEnumerable<T> GetDocs(object[] items)
    {
      foreach (object item in items)
      {
        if (typeof(T).Name == "DbDoc")
        {
          DbDoc d = item is DbDoc ? item as DbDoc : new DbDoc(item);
          yield return (T)Convert.ChangeType(d, typeof(T));
        }
        else
          yield return (T)item;
      }
    }
  }
}
