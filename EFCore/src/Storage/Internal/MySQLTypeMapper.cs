// Copyright (c) 2020 Oracle and/or its affiliates.
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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.EntityFrameworkCore.Properties;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLTypeMapper : RelationalTypeMappingSource
  {
    private static int CHAR_MAX_LENGTH = 255;
    private static int VARCHAR_MAX_LENGTH = 4000;
    private static int _keyMaxLength = 767;

    private readonly MySQLNumberTypeMapping _int = new MySQLNumberTypeMapping("int", typeof(Int32), DbType.Int32);
    private readonly MySQLNumberTypeMapping _uint = new MySQLNumberTypeMapping("int unsigned", typeof(Int32), DbType.UInt32);
    private readonly MySQLNumberTypeMapping _bigint = new MySQLNumberTypeMapping("bigint", typeof(Int64), DbType.Int64);
    private readonly MySQLNumberTypeMapping _ubigint = new MySQLNumberTypeMapping("bigint unsigned", typeof(Int64), DbType.UInt64);
    private readonly MySQLNumberTypeMapping _bit = new MySQLNumberTypeMapping("bit", typeof(short), DbType.Int16);
    private readonly MySQLNumberTypeMapping _smallint = new MySQLNumberTypeMapping("smallint", typeof(Int16), DbType.Int16);
    private readonly MySQLNumberTypeMapping _usmallint = new MySQLNumberTypeMapping("smallint unsigned", typeof(Int16), DbType.UInt16);
    private readonly MySQLNumberTypeMapping _tinyint = new MySQLNumberTypeMapping("tinyint", typeof(Byte), DbType.SByte);
    private readonly MySQLNumberTypeMapping _utinyint = new MySQLNumberTypeMapping("tinyint unsigned", typeof(Byte), DbType.Byte);

    private readonly MySQLStringTypeMapping _charUnicode = new MySQLStringTypeMapping("char", DbType.StringFixedLength, unicode: true, fixedLength: true);
    private readonly MySQLStringTypeMapping _varcharUnicode = new MySQLStringTypeMapping($"varchar", DbType.String, unicode: true);
    private readonly MySQLStringTypeMapping _varcharmaxUnicode = new MySQLStringTypeMapping("longtext", DbType.String, unicode: true);

    private readonly MySQLStringTypeMapping _nchar = new MySQLStringTypeMapping("nchar", DbType.StringFixedLength, unicode: true, fixedLength: true);
    private readonly MySQLStringTypeMapping _nvarchar = new MySQLStringTypeMapping("nvarchar", DbType.String, unicode: true);

    private readonly MySQLDateTimeTypeMapping _datetime = new MySQLDateTimeTypeMapping("datetime", typeof(DateTime));
    private readonly MySQLDateTimeTypeMapping _datetimeoffset = new MySQLDateTimeTypeMapping("timestamp", typeof(DateTimeOffset));
    private readonly MySQLDateTimeTypeMapping _date = new MySQLDateTimeTypeMapping("date", typeof(DateTime));
    private readonly MySQLTimeSpanMapping _time = new MySQLTimeSpanMapping("time");
    private readonly RelationalTypeMapping _timestamp = new MySQLDateTimeTypeMapping("timestamp", typeof(DateTime));
    private readonly RelationalTypeMapping _timestampoffset = new MySQLDateTimeTypeMapping("timestamp", typeof(DateTimeOffset));

    private readonly MySQLNumberTypeMapping _float = new MySQLNumberTypeMapping("float", typeof(float));
    private readonly MySQLNumberTypeMapping _double = new MySQLNumberTypeMapping("double", typeof(double));
    private readonly MySQLNumberTypeMapping _real = new MySQLNumberTypeMapping("real", typeof(Single));
    private readonly MySQLNumberTypeMapping _decimal = new MySQLNumberTypeMapping("decimal(18, 2)", typeof(Decimal));

    private readonly RelationalTypeMapping _binary = new MySQLBinaryTypeMapping("binary");
    private readonly RelationalTypeMapping _varbinary = new MySQLBinaryTypeMapping("varbinary");
    private readonly MySQLBinaryTypeMapping _tinyblob = new MySQLBinaryTypeMapping("tinyblob");
    private readonly MySQLBinaryTypeMapping _mediumblob = new MySQLBinaryTypeMapping("mediumblob");
    private readonly MySQLBinaryTypeMapping _blob = new MySQLBinaryTypeMapping("blob");
    private readonly MySQLBinaryTypeMapping _longblob = new MySQLBinaryTypeMapping("longblob");

    private readonly MySQLStringTypeMapping _enum = new MySQLStringTypeMapping("enum", DbType.String, unicode: true);
    private readonly MySQLGeometryTypeMapping _geometry = new MySQLGeometryTypeMapping("geometry");

    private readonly MySQLBoolTypeMapping _bool = new MySQLBoolTypeMapping("bit");

    private readonly Dictionary<string, RelationalTypeMapping[]> _storeTypeMappings;
    private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;

    // These are disallowed only if specified without any kind of length specified in parenthesis.
    private readonly HashSet<string> _disallowedMappings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "binary",
      "char",
      "nchar",
      "varbinary",
      "varchar",
      "nvarchar"
    };

    public MySQLTypeMapper([NotNull] TypeMappingSourceDependencies dependencies,
      [NotNull] RelationalTypeMappingSourceDependencies relationalDependencies)
      : base(dependencies, relationalDependencies)

    {
      _storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
      {
        // integers
        { "bigint",new[] {_bigint } },
        { "bigint unsigned", new[] {_ubigint } },
        { "int", new[] {_int} },
        { "int unsigned", new[] {_uint} },
        { "mediumint", new[] {_int } },
        { "mediumint unsigned", new[] { _uint }},
        { "smallint", new[] {_smallint }},
        { "smallint unsigned", new[] {_usmallint }},
        { "tinyint", new[] { _tinyint }},
        { "tinyint unsigned", new[] {_utinyint }},

        // decimals
        { "decimal", new[] {_decimal } },
        { "double", new[] {_double } },
        { "float", new[] {_float } },
        { "real", new[] {_real }},

        // binary
        { "tinyblob",new[] { _tinyblob } },
        { "blob", new[] {_blob } },
        { "mediumblob", new[] {_mediumblob } },
        { "longblob",new[] { _longblob }},
        { "binary", new[] {_binary }},
        { "varbinary", new[] {_varbinary }},

        // string
        { "char", new[] {_charUnicode } },
        { "varchar", new[] {_varcharUnicode} },
        { "tinytext", new[] {_varcharmaxUnicode} },
        { "text", new[] { _varcharmaxUnicode} },
        { "mediumtext", new[] { _varcharmaxUnicode} },
        { "longtext", new[] { _varcharmaxUnicode} },
        { "enum", new[] {_enum } },

        // DateTime
        { "date", new[]{_date } },
        { "time",new[]{ _time } },
        { "year", new[]{_smallint } },
        { "datetime", new RelationalTypeMapping[] { _datetime, _datetimeoffset }  },
        { "timestamp", new RelationalTypeMapping[] { _timestamp, _timestampoffset } },

        // bit
        { "bit", new[]{_bit } },
        
        // other
        { "geometry", new[]{_geometry } },
        { "json", new[]{_varcharmaxUnicode } }
      };

      _clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
      {
        // integers
        { typeof(ulong), new MySQLNumberTypeMapping("numeric(20, 0)" ,_decimal.GetType()) },
        { typeof(short), _smallint },
        { typeof(int), _int },
        { typeof(long), _bigint },
        { typeof(byte), _tinyint },
        { typeof(sbyte), _smallint },
        { typeof(ushort), _int },
        { typeof(uint), _bigint },
        
        // DateTime
        { typeof(DateTime), _datetime },
        { typeof(DateTimeOffset), _datetimeoffset },
        { typeof(TimeSpan), _time },

        // boolean
        { typeof(bool), _bool },

        // decimals
        { typeof(float), _float },
        { typeof(double), _double },
        { typeof(decimal), _decimal },

        { typeof(char), _int },
        { typeof(Types.MySqlGeometry), _geometry },
      };
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ValidateMapping(CoreTypeMapping mapping, IProperty property)
    {
      var relationalMapping = mapping as RelationalTypeMapping;

      if (_disallowedMappings.Contains(relationalMapping?.StoreType))
      {
        if (property == null)
        {
          throw new ArgumentException(String.Format(MySQLStrings.UnqualifiedDataType, relationalMapping.StoreType));
        }
      }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        => base.FindMapping(mappingInfo) ??
      FindRawMapping(mappingInfo)?.Clone(mappingInfo);

    private RelationalTypeMapping FindRawMapping(RelationalTypeMappingInfo mappingInfo)
    {
      var clrType = mappingInfo.ClrType;
      var storeTypeName = mappingInfo.StoreTypeName;
      var storeTypeNameBase = mappingInfo.StoreTypeNameBase;      

      if (storeTypeName != null)
      {
        if (_storeTypeMappings.TryGetValue(storeTypeName, out var mappings))
        {
          return clrType == null
              ? mappings[0]
              : mappings.FirstOrDefault(m => m.ClrType == clrType);
        }

        if (_storeTypeMappings.TryGetValue(storeTypeNameBase, out mappings))
        {
          return clrType == null
              ? mappings[0]
                  .Clone(in mappingInfo)
              : mappings.FirstOrDefault(m => m.ClrType == clrType)
                  ?.Clone(in mappingInfo);
        }
      }

      if (clrType != null)
      {
        if (_clrTypeMappings.TryGetValue(clrType, out var mapping))
        {
          if (mappingInfo.Precision.HasValue)
          {
            if (clrType == typeof(DateTime) ||
                            clrType == typeof(DateTimeOffset) ||
                            clrType == typeof(TimeSpan))
            {
              return mapping.Clone(mappingInfo.Precision.Value, null);
            }
          }

          return mapping;
        }
        else if (clrType == typeof(string))
        {
          bool isAnsi = mappingInfo.IsUnicode == false;
          bool isFixedLength = mappingInfo.IsFixedLength == true;
          var maxSize = isAnsi ? 8000 : 4000;
          int? size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? (int?)_keyMaxLength : null);

          if (size > maxSize)
            size = isFixedLength ? maxSize : (int?)null;

          string baseName = size.HasValue ? (isFixedLength ? "char" : "varchar") : "text";

          return new MySQLStringTypeMapping(
            $"{baseName}{(size.HasValue ? $"({size})" : string.Empty)}",
            isFixedLength ? DbType.StringFixedLength : DbType.String,
            !isAnsi,
            size);
        }
        else if (clrType == typeof(byte[]))
        {
          bool isFixedLength = mappingInfo.IsFixedLength == true;
          int size = mappingInfo.Size.HasValue ? mappingInfo.Size.Value : (isFixedLength ? CHAR_MAX_LENGTH : VARCHAR_MAX_LENGTH);
          return new MySQLBinaryTypeMapping($"{(isFixedLength ? "binary" : "varbinary")}({size})", DbType.Binary, size, isFixedLength);
        }
      }

      return null;
    }

    protected override string ParseStoreTypeName(string storeTypeName, out bool? unicode, out int? size, out int? precision, out int? scale)
    {
      var storeTypeBaseName = base.ParseStoreTypeName(storeTypeName, out unicode, out size, out precision, out scale);

      return (storeTypeName?.IndexOf("unsigned", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
          ? storeTypeBaseName + " unsigned"
          : storeTypeBaseName;
    }
  }
}