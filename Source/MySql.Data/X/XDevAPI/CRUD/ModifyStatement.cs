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
using Mysqlx.Crud;
using MySqlX.XDevAPI.Common;

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Represents a chaining collection modify statement
  /// </summary>
  public class ModifyStatement : FilterableStatement<ModifyStatement, Collection, Result>
  {
    internal ModifyStatement(Collection collection, string condition) : base(collection, condition)
    {
      Updates = new List<UpdateSpec>();
    }

    internal List<UpdateSpec> Updates;

    /// <summary>
    /// Sets key and value
    /// </summary>
    /// <param name="docPath">Document path key</param>
    /// <param name="value">New value</param>
    /// <returns>This ModifyStatement object</returns>
    public ModifyStatement Set(string docPath, object value)
    {
      Updates.Add(new UpdateSpec(UpdateOperation.Types.UpdateType.ITEM_SET, docPath).SetValue(value));
      return this;
    }

    /// <summary>
    /// Changes value for a key
    /// </summary>
    /// <param name="docPath">Document path key</param>
    /// <param name="value">New value</param>
    /// <returns>This ModifyStatement object</returns>
    public ModifyStatement Change(string docPath, object value)
    {
      Updates.Add(new UpdateSpec(UpdateOperation.Types.UpdateType.ITEM_REPLACE, docPath).SetValue(value));
      return this;
    }

    /// <summary>
    /// Removes a key/value from a document
    /// </summary>
    /// <param name="docPath"></param>
    /// <returns>This ModifyStatement object</returns>
    public ModifyStatement Unset(string docPath)
    {
      Updates.Add(new UpdateSpec(UpdateOperation.Types.UpdateType.ITEM_REMOVE, docPath));
      return this;
    }

    /// <summary>
    /// Executes the modify statement
    /// </summary>
    /// <returns>Result of execution</returns>
    public override Result Execute()
    {
      return Execute(Target.Session.XSession.ModifyDocs, this);
    }
  }
}
