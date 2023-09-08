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
using MySql.EntityFrameworkCore.Utils;
using System;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLGuidTypeMapping : GuidTypeMapping
  {
    private readonly MySqlGuidFormat _guidFormat;

    public MySQLGuidTypeMapping(MySqlGuidFormat guidFormat)
      : this(new RelationalTypeMappingParameters(
          new CoreTypeMappingParameters(typeof(Guid)),
          GetStoreType(guidFormat),
          StoreTypePostfix.Size,
          System.Data.DbType.Guid,
          false,
          GetSize(guidFormat),
          true),
        guidFormat)
    {
    }

    protected MySQLGuidTypeMapping(RelationalTypeMappingParameters parameters, MySqlGuidFormat guidFormat)
      : base(parameters)
    {
      _guidFormat = guidFormat;
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLGuidTypeMapping(parameters, _guidFormat);

    protected override string GenerateNonNullSqlLiteral(object value)
    {
      switch (_guidFormat)
      {
        case MySqlGuidFormat.Char36:
          return $"'{value:D}'";

        case MySqlGuidFormat.Char32:
          return $"'{value:N}'";

        case MySqlGuidFormat.Binary16:
        case MySqlGuidFormat.TimeSwapBinary16:
        case MySqlGuidFormat.LittleEndianBinary16:
          return ByteArrayFormatter.ToHex(GetBytesFromGuid(_guidFormat, (Guid)value));

        case MySqlGuidFormat.None:
        case MySqlGuidFormat.Default:
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private static string GetStoreType(MySqlGuidFormat guidFormat)
    {
      switch (guidFormat)
      {
        case MySqlGuidFormat.Char36:
        case MySqlGuidFormat.Char32:
          return "char";

        case MySqlGuidFormat.Binary16:
        case MySqlGuidFormat.TimeSwapBinary16:
        case MySqlGuidFormat.LittleEndianBinary16:
          return "binary";

        case MySqlGuidFormat.None:
        case MySqlGuidFormat.Default:
        default:
          throw new InvalidOperationException();
      }
    }

    private static int GetSize(MySqlGuidFormat guidFormat)
    {
      switch (guidFormat)
      {
        case MySqlGuidFormat.Char36:
          return 36;

        case MySqlGuidFormat.Char32:
          return 32;

        case MySqlGuidFormat.Binary16:
        case MySqlGuidFormat.TimeSwapBinary16:
        case MySqlGuidFormat.LittleEndianBinary16:
          return 16;

        case MySqlGuidFormat.None:
        case MySqlGuidFormat.Default:
        default:
          throw new InvalidOperationException();
      }
    }

    public static bool IsValidGuidFormat(MySqlGuidFormat guidFormat)
      => guidFormat != MySqlGuidFormat.None &&
         guidFormat != MySqlGuidFormat.Default;

    protected static byte[] GetBytesFromGuid(MySqlGuidFormat guidFormat, Guid guid)
    {
      var bytes = guid.ToByteArray();

      if (guidFormat == MySqlGuidFormat.Binary16)
      {
        return new[] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] };
      }

      if (guidFormat == MySqlGuidFormat.TimeSwapBinary16)
      {
        return new[] { bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] };
      }

      return bytes;
    }
  }
}
