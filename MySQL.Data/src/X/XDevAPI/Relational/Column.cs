// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Protocol;
using MySqlX.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a table column.
  /// </summary>
  public class Column
  {
    internal ValueDecoder _decoder;
    internal UInt64 _collationNumber;

    /// <summary>
    /// Gets the original column name.
    /// </summary>
    public string ColumnName { get; internal set; }
    /// <summary>
    /// Gets the alias of the column name.
    /// </summary>
    public string ColumnLabel { get; internal set; }
    /// <summary>
    /// Gets the table name the column orginates from.
    /// </summary>
    public string TableName { get; internal set; }
    /// <summary>
    /// Gets the alias of the table name .
    /// </summary>
    public string TableLabel { get; internal set; }
    /// <summary>
    /// Gets the schema name the column originates from.
    /// </summary>
    public string SchemaName { get; internal set; }
    /// <summary>
    /// Gets the catalog the schema originates from.
    /// In MySQL protocol this is `def` by default.
    /// </summary>
    public string DatabaseName { get; internal set;  }
    /// <summary>
    /// Gets the collation used for this column.
    /// </summary>
    public string CollationName { get; internal set; }
    /// <summary>
    /// Gets the character set used for this column.
    /// </summary>
    public string CharacterSetName { get; internal set; }
    /// <summary>
    /// Gets the column length.
    /// </summary>
    public UInt32 Length { get; internal set; }
    /// <summary>
    /// Gets the fractional decimal digits for floating point and fixed point numbers.
    /// </summary>
    public UInt32 FractionalDigits { get; internal set; }
    /// <summary>
    /// Gets the Mysql data type.
    /// </summary>
    public ColumnType Type { get; internal set; }
    /// <summary>
    /// Gets the .NET Clr data type.
    /// </summary>
    public Type ClrType { get; internal set; }
    /// <summary>
    /// True if it's a signed number.
    /// </summary>
    public bool IsNumberSigned { get; internal set; }
    /// <summary>
    /// True if column is UINT zerofill or BYTES rightpad.
    /// </summary>
    public bool IsPadded { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the Column class.
    /// </summary>
    public Column()
    {}
  }
}
