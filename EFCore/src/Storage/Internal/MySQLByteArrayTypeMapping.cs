// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Storage;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Data;
using System.Data.Common;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLByteArrayTypeMapping : ByteArrayTypeMapping
  {
    private const int MaxSize = 8000;

    private readonly int _maxSpecificSize;

    public MySQLByteArrayTypeMapping(
      string? storeType = null,
      int? size = null,
      bool fixedLength = false)
      : this(System.Data.DbType.Binary,
        storeType!,
        size.HasValue && size < MaxSize ? size : null,
        fixedLength)
    {
    }

    protected MySQLByteArrayTypeMapping(
      DbType type,
      string storeType,
      int? size,
      bool fixedLength)
      : this(
        new RelationalTypeMappingParameters(
          new CoreTypeMappingParameters(typeof(byte[])),
          storeType ?? GetBaseType(size, fixedLength),
          GetStoreTypePostfix(size),
          type,
          size: size,
          fixedLength: fixedLength))
    {
    }

    private static string GetBaseType(int? size, bool isFixedLength)
      => size == null
        ? "longblob"
        : isFixedLength ? "binary" : "varbinary";

    private static StoreTypePostfix GetStoreTypePostfix(int? size)
      => size != null && size <= MaxSize ? StoreTypePostfix.Size : StoreTypePostfix.None;

    protected MySQLByteArrayTypeMapping(RelationalTypeMappingParameters parameters)
      : base(parameters)
    {
      _maxSpecificSize = CalculateSize(parameters.Size);
    }

    /// <summary>
    ///   Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters"> The parameters for this mapping. </param>
    /// <returns> The newly created mapping. </returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLByteArrayTypeMapping(parameters);

    private static int CalculateSize(int? size)
      => size.HasValue && size < MaxSize ? size.Value : MaxSize;

    /// <inheritdoc/>
    protected override void ConfigureParameter(DbParameter parameter)
    {
      // For strings and byte arrays, set the max length to the size facet if specified, or
      // 8000 bytes if no size facet specified, if the data will fit so as to avoid query cache
      // fragmentation by setting lots of different Size values otherwise always set to
      // -1 (unbounded) to avoid SQL client size inference.

      var value = parameter.Value;
      var length = (value as string)?.Length ?? (value as byte[])?.Length;

      parameter.Size = value == null || value == DBNull.Value || length != null && length <= _maxSpecificSize
        ? _maxSpecificSize
        : -1;
    }

    /// <summary>
    ///   Generates the SQL representation of a literal value.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>
    ///   The generated string.
    /// </returns>
    protected override string GenerateNonNullSqlLiteral(object value) => ByteArrayFormatter.ToHex((byte[])value);
  }
}