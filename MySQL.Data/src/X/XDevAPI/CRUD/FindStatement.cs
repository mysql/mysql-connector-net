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

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Represents a chaining collection find statement
  /// </summary>
  public class FindStatement : FilterableStatement<FindStatement, Collection, DocResult>
  {
    internal List<string> projection;
    internal string[] orderBy;


    internal FindStatement(Collection c, string condition) : base (c, condition)
    {
    }

    /// <summary>
    /// List of column projections that shall be returned
    /// </summary>
    /// <param name="columns">List of columns</param>
    /// <returns>This FindStatement object</returns>
    public FindStatement Fields(params string[] columns)
    {
      projection = new List<string>(columns);
      return this;
    }

    /// <summary>
    /// Executes the Find statement
    /// </summary>
    /// <returns>Result of execution and data</returns>
    public override DocResult Execute()
    {
      return Execute(Target.Session.XSession.FindDocs, this);
    }

    /// <summary>
    /// Allows the user to set the limit and offset for the operation
    /// </summary>
    /// <param name="rows">How many items should be returned</param>
    /// <param name="offset">How many items should be skipped</param>
    /// <returns>The implementing statement type</returns>
    public FindStatement Limit(long rows, long offset)
    {
      FilterData.Limit = rows;
      FilterData.Offset = offset;
      return this;
    }
  }
}
