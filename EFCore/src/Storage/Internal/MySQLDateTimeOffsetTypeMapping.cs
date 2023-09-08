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
using System;
using System.Data;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  /// <summary>
  ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
  ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
  ///   any release. You should only use it directly in your code with extreme caution and knowing that
  ///   doing so can result in application failures when updating to a new Entity Framework Core release.
  /// </summary>
  internal class MySQLDateTimeOffsetTypeMapping : DateTimeOffsetTypeMapping
  {
    private const string _dateTimeFormatConst = @"'{0:yyyy-MM-dd HH:mm:ss.fff}'";
    // Note: this array will be accessed using the precision as an index
    // so the order of the entries in this array is important
    private readonly string[] _dateTimeOffsetFormats =
    {
        "'{0:yyyy-MM-dd HH:mm:ss}'",
        "'{0:yyyy-MM-dd HH:mm:ss.f}'",
        "'{0:yyyy-MM-dd HH:mm:ss.ff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.fff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.ffff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.fffff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.ffffff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.fffffff}'"
    };

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public MySQLDateTimeOffsetTypeMapping(
      string storeType,
      DbType? dbType = System.Data.DbType.DateTimeOffset,
      int? precision = null,
      StoreTypePostfix storeTypePostfix = StoreTypePostfix.Precision)
      : base(
        new RelationalTypeMappingParameters(
          new CoreTypeMappingParameters(typeof(DateTimeOffset)),
          storeType,
          storeTypePostfix,
          dbType,
          precision: precision))
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="DateTimeOffsetTypeMapping" /> class.
    /// </summary>
    /// <param name="parameters">Parameter object for <see cref="RelationalTypeMapping" />.</param>
    protected MySQLDateTimeOffsetTypeMapping(RelationalTypeMappingParameters parameters)
      : base(parameters)
    {
    }

    /// <summary>
    ///   Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters">The parameters for this mapping.</param>
    /// <returns>The newly created mapping.</returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLDateTimeOffsetTypeMapping(parameters);

    public override string GenerateProviderValueSqlLiteral(object? value)
      => value == null
      ? "NULL"
      : GenerateNonNullSqlLiteral(
        value is DateTimeOffset dateTimeOffset
        ? dateTimeOffset.UtcDateTime
        : value);

    /// <summary>
    ///   Gets the string format to be used to generate SQL literals of this type.
    /// </summary>
    protected override string SqlLiteralFormatString
    {
      get
      {
        if (Precision.HasValue)
        {
          var precision = Precision.Value;
          if (precision <= 7 && precision >= 0)
            return "TIMESTAMP " + _dateTimeOffsetFormats[precision];
        }

        return "TIMESTAMP " + _dateTimeFormatConst;
      }
    }
  }
}
