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
using System.Globalization;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLTimeSpanMapping : TimeSpanTypeMapping
  {
    public MySQLTimeSpanMapping(
      [NotNull] string storeType,
      [NotNull] Type clrType,
      int? precision = null)
      : this(
        new RelationalTypeMappingParameters(
          new CoreTypeMappingParameters(clrType),
          storeType,
          StoreTypePostfix.Precision,
          System.Data.DbType.Time,
          precision: precision))
    {
    }

    protected MySQLTimeSpanMapping(RelationalTypeMappingParameters parameters)
      : base(parameters)
    {
    }

    /// <summary>
    ///   Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters"> The parameters for this mapping. </param>
    /// <returns> The newly created mapping. </returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLTimeSpanMapping(parameters);

    /// <summary>
    ///   Generates the MySQL representation of a non-null literal value.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>
    ///   The generated string.
    /// </returns>
    protected override string GenerateNonNullSqlLiteral(object value)
    {
      var literal = string.Format(CultureInfo.InvariantCulture, $"{{0:{GetTimeFormatString(value, Precision)}}}", value);
      return literal.EndsWith(".")
        ? $"TIME '{literal[..^1]}'"
        : $"TIME '{literal}'";
    }

    protected static string GetTimeFormatString(object value, int? precision)
    {
      var validPrecision = Math.Min(Math.Max(precision.GetValueOrDefault(), 0), 6);

      var format = value switch
      {
        TimeOnly => @"HH\:mm\:ss",
        TimeSpan => @"hh\:mm\:ss",
        _ => throw new InvalidCastException($"Can't generate time SQL literal for CLR type '{value.GetType()}'.")
      };

      return validPrecision > 0
        ? $@"{format}\.{new string('F', validPrecision)}"
        : format;
    }
  }
}
