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

namespace MySqlX.DataAccess
{
  internal enum ConnectionMode
  {
    Offline = 0,
    ReadOnly = 1,
    WriteOnly = 2,
    ReadWrite = 3
  }

  internal enum AuthenticationMode
  { 
    PlainAccess = 0,
    MySQL41 = 1  
  }

  internal enum OS
  {
    Unknown = 0,
    Windows,
    Linux,
    MacOS
  }

  /// <summary>
  /// Defines how the server processes the view.
  /// </summary>
  public enum ViewAlgorithmEnum
  {
    /// <summary>
    /// The server chooses which algorithm to use.
    /// </summary>
    Undefined = Mysqlx.Crud.ViewAlgorithm.Undefined,
    /// <summary>
    /// The text of a statement that refers to the view and the view definition are merged.
    /// </summary>
    Merge = Mysqlx.Crud.ViewAlgorithm.Merge,
    /// <summary>
    /// The view is retrieved into a temporary table.
    /// </summary>
    TempTable = Mysqlx.Crud.ViewAlgorithm.Temptable
  }

  /// <summary>
  /// Defines the security context in which the view is going to be
  /// executed, this means that VIEW can be executed with current user permissions or
  /// with permissions of the users who defined the VIEW.
  /// </summary>
  public enum ViewSqlSecurityEnum
  {
    /// <summary>
    /// The view is executed under the invoker context.
    /// </summary>
    Invoker = Mysqlx.Crud.ViewSqlSecurity.Invoker,
    /// <summary>
    /// The view is executed under the definer context.
    /// </summary>
    Definer = Mysqlx.Crud.ViewSqlSecurity.Definer
  }

  /// <summary>
  /// Limits the write operations done on a `VIEW`
  /// (`INSERT`, `UPDATE`, `DELETE`) to rows in which the `WHERE` clause is `TRUE`.
  /// </summary>
  public enum ViewCheckOptionEnum
  {
    /// <summary>
    /// The view WHERE clause is checked, but no underlying views are checked.
    /// </summary>
    Local = Mysqlx.Crud.ViewCheckOption.Local,
    /// <summary>
    /// The view WHERE clause is checked, then checking recurses to underlying views.
    /// </summary>
    Cascaded = Mysqlx.Crud.ViewCheckOption.Cascaded
  }
}
