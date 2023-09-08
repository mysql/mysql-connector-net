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
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.Common;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLStringTypeMapping : MySQLTypeMapping
  {
    private const int UnicodeMax = 4000;
    private const int AnsiMax = 8000;

    private readonly int _maxSpecificSize;

    public MySQLStringTypeMapping(
      [NotNull] string storeType,
      bool unicode = true,
      int? size = null,
      bool fixedLength = false,
      StoreTypePostfix? storeTypePostfix = null)
      : this(
        new RelationalTypeMappingParameters(
          new CoreTypeMappingParameters(typeof(string)),
          storeType,
          storeTypePostfix ?? StoreTypePostfix.None,
          GetDbType(unicode, fixedLength),
          unicode,
          size,
          fixedLength),
        fixedLength
        ? MySqlDbType.String
        : MySqlDbType.VarString)
    {
    }

    private static DbType? GetDbType(bool unicode, bool fixedLength)
      => unicode
        ? fixedLength
          ? System.Data.DbType.StringFixedLength
          : System.Data.DbType.String
        : fixedLength
          ? System.Data.DbType.AnsiStringFixedLength
          : System.Data.DbType.AnsiString;

    protected MySQLStringTypeMapping(RelationalTypeMappingParameters parameters, MySqlDbType mySqlDbType)
      : base(parameters, mySqlDbType)
    {
      _maxSpecificSize = CalculateSize(parameters.Unicode, parameters.Size);
    }

    private static int CalculateSize(bool unicode, int? size)
      => unicode
        ? size.HasValue && size <= UnicodeMax ? size.Value : UnicodeMax
        : size.HasValue && size <= AnsiMax ? size.Value : AnsiMax;

    /// <summary>
    ///   Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters"> The parameters for this mapping. </param>
    /// <returns> The newly created mapping. </returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLStringTypeMapping(parameters, MySqlDbType);

    /// <inheritdoc/>
    protected override void ConfigureParameter(DbParameter parameter)
    {
      var value = parameter.Value;
      int? length;

      if (value is string stringValue)
        length = stringValue.Length;
      else if (value is byte[] byteArray)
        length = byteArray.Length;
      else
        length = null;

      parameter.Value = value;
      parameter.Size = value == null || value == DBNull.Value || length != null && length <= _maxSpecificSize
        ? _maxSpecificSize
        : -1;
    }

    protected override string GenerateNonNullSqlLiteral(object value)
      => EscapeLineBreaks((string)value);

    protected string EscapeSqlLiteral(string literal)
    => literal.Replace("'", "''");

    private static readonly char[] LineBreakChars = new char[] { '\r', '\n' };
    private string EscapeLineBreaks(string value)
    {
      var escapedLiteral = $"'{EscapeSqlLiteral(value)}'";

      if (value.IndexOfAny(LineBreakChars) != -1)
        escapedLiteral = "CONCAT(" + escapedLiteral
              .Replace("\r\n", "', CHAR(13, 10), '")
              .Replace("\r", "', CHAR(13), '")
              .Replace("\n", "', CHAR(10), '") + ")";

      return escapedLiteral;
    }
  }
}
