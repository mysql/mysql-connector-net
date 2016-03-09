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

using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a chaining table update statement
  /// </summary>
  public class TableUpdateStatement : FilterableStatement<TableUpdateStatement, Table, Result>
  {
    internal List<UpdateSpec> updates = new List<UpdateSpec>();

    internal TableUpdateStatement(Table table) : base(table)
    {
      FilterData.IsRelational = true;
      FilterData.Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// Executes the update statement
    /// </summary>
    /// <returns>Result of the update statement</returns>
    public override Result Execute()
    {
      return Execute(Target.Session.XSession.UpdateRows, this);
    }

    /// <summary>
    /// Column and value to be updated
    /// </summary>
    /// <param name="tableField">Column name</param>
    /// <param name="value">Value to be updated</param>
    /// <returns>This same TableUpdateStatement object</returns>
    public TableUpdateStatement Set(string tableField, object value)
    {
      updates.Add(new UpdateSpec(Mysqlx.Crud.UpdateOperation.Types.UpdateType.SET, tableField).SetValue(value));
      return this;
    }
  }
}
