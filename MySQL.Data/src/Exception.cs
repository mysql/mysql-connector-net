// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
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

using System;
using System.Data.Common;
using System.Runtime.Serialization;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// The exception that is thrown when MySQL returns an error. This class cannot be inherited.
  /// </summary>
  /// <remarks>
  ///    <para>
  ///      This class is created whenever the MySQL Data Provider encounters an error generated from the server.
  ///    </para>
  ///    <para>
  ///      Any open connections are not automatically closed when an exception is thrown.  If
  ///      the client application determines that the exception is fatal, it should close any open
  ///      <see cref="MySqlDataReader"/> objects or <see cref="MySqlConnection"/> objects.
  ///    </para>
  /// </remarks>
  [Serializable]
  public sealed class MySqlException : DbException
  {
    internal MySqlException()
    {
    }

    internal MySqlException(string msg)
      : base(msg)
    {
    }

    internal MySqlException(string msg, Exception ex)
      : base(msg, ex)
    {
    }

    internal MySqlException(string msg, bool isFatal, Exception inner)
      : base(msg, inner)
    {
      IsFatal = isFatal;
    }

    internal MySqlException(string msg, int errno, Exception inner)
      : this(msg, inner)
    {
      Number = errno;
      Data.Add("Server Error Code", errno);
    }

    internal MySqlException(string msg, int errno, bool isFatal)
      : this(msg)
    {
      Number = errno;
      IsFatal = isFatal;
      Data.Add("Server Error Code", errno);
    }

    internal MySqlException(string msg, int errno)
      : this(msg, errno, null)
    {
    }

    internal MySqlException(UInt32 code, string sqlState, string msg) : base(msg)
    {
      Code = code;
      SqlState = sqlState;
    }

    private MySqlException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary>
    /// Gets a number that identifies the type of error.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// True if this exception was fatal and cause the closing of the connection, false otherwise.
    /// </summary>
    internal bool IsFatal { get; }

    internal bool IsQueryAborted => (Number == (int)MySqlErrorCode.QueryInterrupted ||
                                     Number == (int)MySqlErrorCode.FileSortAborted);

    /// <summary>
    /// Gets the SQL state.
    /// </summary>
    public string SqlState { get; private set; }

    /// <summary>
    /// Gets an integer that representes the MySQL error code.
    /// </summary>
    public UInt32 Code { get; private set; }
  }
}
