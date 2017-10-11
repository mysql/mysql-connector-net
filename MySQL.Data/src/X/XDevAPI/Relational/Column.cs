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
