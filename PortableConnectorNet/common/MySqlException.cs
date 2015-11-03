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

using System;
using MySql.XDevAPI.Common;

namespace MySql
{
  /// <summary>
  /// Implemented exception type for MySql server specific
  /// </summary>
  public sealed class MySqlException : Exception
  {
    private bool isFatal;

    /// <summary>
    /// True if this exception was fatal and cause the closing of the connection, false otherwise.
    /// </summary>
    internal bool IsFatal
    {
      get { return isFatal; }
    }

     internal MySqlException()
      : base()
    {

    }

    internal MySqlException(UInt32 code, string sqlState, string msg) : base(msg)
    {
      Code = code;
      SqlState = sqlState;
    }

    internal MySqlException(string message)
      : base(message)
    {

    }

    internal MySqlException(string message, Exception innerException)
      : base(message, innerException)
    {

    }

    internal MySqlException(string msg, bool isFatal, Exception inner)
      : base(msg, inner)
    {
      this.isFatal = isFatal;
    }

    public string SqlState { get; private set; }
    public UInt32 Code { get; private set; }
  }
}
