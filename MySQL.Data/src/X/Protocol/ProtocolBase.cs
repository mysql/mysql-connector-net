// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;

namespace MySqlX.Protocol
{

  /// <summary>
  /// Abstract class for the protocol base operations in client/server communication.
  /// </summary>  
  /// 
  public abstract class ProtocolBase
  {
    /// <summary>
    /// Reads a row from the base result.
    /// </summary>
    /// <param name="rs">The base result to be queried.</param>
    /// <returns>A list containing a byte representation of the value for each field found in the row.</returns>
    public abstract List<byte[]> ReadRow(BaseResult rs);

    /// <summary>
    /// Executes an SQL statement.
    /// </summary>
    /// <param name="sql">The statement to be executed.</param>
    /// <param name="args">The arguments associated to the SQL statement.</param>
    public abstract void SendSQL(string sql, params object[] args);

    /// <summary>
    /// Determines if the base result contains retrievable items.
    /// </summary>
    /// <param name="rs">The base result to be queried.</param>
    /// <returns><c>true</c> if the base result contains data; otherwise, <c>false</c>.</returns>
    public abstract bool HasData(BaseResult rs);

    /// <summary>
    /// Loads metadata associated to the retrieved columns.
    /// </summary>
    /// <returns>A list of <c>Column</c> objects containing metadata for each column.</returns>
    public abstract List<Column> LoadColumnMetadata();

    /// <summary>
    /// Closes the base result preventing the retrieval of data.
    /// </summary>
    /// <param name="rs">The base result to close.</param>
    public abstract void CloseResult(BaseResult rs); 
  }
}
