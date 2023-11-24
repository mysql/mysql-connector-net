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
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLTypeMappingSource : RelationalTypeMappingSource
  {
    private const int MAXKEYLENGTH = 3072;

    private readonly IntTypeMapping _int = new IntTypeMapping("int", DbType.Int32);
    private readonly UIntTypeMapping _uint = new UIntTypeMapping("int unsigned", DbType.UInt32);
    private readonly LongTypeMapping _bigint = new LongTypeMapping("bigint", DbType.Int64);
    private readonly ULongTypeMapping _ubigint = new ULongTypeMapping("bigint unsigned", DbType.UInt64);
    private readonly ShortTypeMapping _smallint = new ShortTypeMapping("smallint", DbType.Int16);
    private readonly UShortTypeMapping _usmallint = new UShortTypeMapping("smallint unsigned", DbType.UInt16);
    private readonly SByteTypeMapping _tinyint = new SByteTypeMapping("tinyint", DbType.SByte);
    private readonly ByteTypeMapping _utinyint = new ByteTypeMapping("tinyint unsigned", DbType.Byte);

    private readonly ULongTypeMapping _bit = new ULongTypeMapping("bit", DbType.UInt64);
    private readonly MySQLStringTypeMapping _charUnicode = new MySQLStringTypeMapping("char", fixedLength: true, storeTypePostfix: StoreTypePostfix.Size);
    private readonly MySQLStringTypeMapping _varcharUnicode = new MySQLStringTypeMapping("varchar", storeTypePostfix: StoreTypePostfix.Size);
    private readonly MySQLStringTypeMapping _textUnicode = new MySQLStringTypeMapping("text");
    private readonly MySQLStringTypeMapping _longtextUnicode = new MySQLStringTypeMapping("longtext");
    private readonly MySQLStringTypeMapping _mediumTextUnicode = new MySQLStringTypeMapping("mediumtext");
    private readonly MySQLStringTypeMapping _tinyTextUnicode = new MySQLStringTypeMapping("tinytext");

    private readonly MySQLStringTypeMapping _nchar = new MySQLStringTypeMapping("nchar", fixedLength: true, storeTypePostfix: StoreTypePostfix.Size);
    private readonly MySQLStringTypeMapping _nvarchar = new MySQLStringTypeMapping("nvarchar", storeTypePostfix: StoreTypePostfix.Size);

    private readonly MySQLDateTimeTypeMapping _datetime = new MySQLDateTimeTypeMapping("datetime", dbType: DbType.DateTime);
    private readonly MySQLDateTimeTypeMapping _timeStamp = new MySQLDateTimeTypeMapping("timestamp", dbType: DbType.DateTime);
    private readonly MySQLDateTimeOffsetTypeMapping _datetimeoffset = new MySQLDateTimeOffsetTypeMapping("datetime");
    private readonly MySQLDateTimeOffsetTypeMapping _timestampoffset = new MySQLDateTimeOffsetTypeMapping("timestamp");
    private readonly MySQLDateTypeMapping _date = new MySQLDateTypeMapping("date", typeof(DateTime));
    private readonly MySQLDateTypeMapping _dateonly = new MySQLDateTypeMapping("date", typeof(DateOnly));
    private readonly MySQLTimeSpanMapping _time = new MySQLTimeSpanMapping("time", typeof(TimeSpan));
    private readonly MySQLTimeSpanMapping _timeonly = new MySQLTimeSpanMapping("time", typeof(TimeOnly));

    private readonly MySQLFloatTypeMapping _float = new MySQLFloatTypeMapping("float", DbType.Single);
    private readonly MySQLDoubleTypeMapping _double = new MySQLDoubleTypeMapping("double", DbType.Double);
    private readonly MySQLDecimalTypeMapping _decimal = new MySQLDecimalTypeMapping("decimal", precision: 18, scale: 2);

    private readonly RelationalTypeMapping _binary = new MySQLByteArrayTypeMapping(fixedLength: true);
    private readonly RelationalTypeMapping _varbinary = new MySQLByteArrayTypeMapping();

    private readonly MySQLStringTypeMapping _enum = new MySQLStringTypeMapping("enum");
    private readonly MySQLStringTypeMapping _set = new MySQLStringTypeMapping("set");
    private readonly MySQLGeometryTypeMapping _geometry = new MySQLGeometryTypeMapping("geometry");

    private readonly MySQLBoolTypeMapping _bitBool = new MySQLBoolTypeMapping("bit", size: 1);
    private readonly MySQLBoolTypeMapping _tinyintBool = new MySQLBoolTypeMapping("tinyint", size: 1);

    private GuidTypeMapping? _guid;
    private MySqlGuidFormat guidFormat = MySqlGuidFormat.Default;

    private Dictionary<string, RelationalTypeMapping[]>? _storeTypeMappings;
    private Dictionary<Type, RelationalTypeMapping>? _clrTypeMappings;

    private readonly IMySQLOptions _options;
    private bool _initialized;

    public MySQLTypeMappingSource(TypeMappingSourceDependencies dependencies,
      RelationalTypeMappingSourceDependencies relationalDependencies,
      IMySQLOptions options)
      : base(dependencies, relationalDependencies)
    {
      _options = options;
    }

    protected void Initialize()
    {
      if (guidFormat == MySqlGuidFormat.Default)
      {
        guidFormat = _options.ConnectionSettings.OldGuids
          ? MySqlGuidFormat.LittleEndianBinary16
          : MySqlGuidFormat.Char36;
      }

      _guid = MySQLGuidTypeMapping.IsValidGuidFormat(guidFormat)
              ? new MySQLGuidTypeMapping(guidFormat)
              : null;

      _storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
      {
        // integers
        { "bigint", new[]  { _bigint } },
        { "bigint unsigned", new[] { _ubigint } },
        { "int", new[] { _int } },
        { "int unsigned", new[] { _uint } },
        { "integer", new[] { _int } },
        { "integer unsigned", new[] { _uint } },
        { "mediumint", new[] { _int }},
        { "mediumint unsigned",new[]  { _uint }},
        { "smallint", new[] { _smallint }},
        { "smallint unsigned", new[] { _usmallint }},
        { "tinyint", new[] { _tinyint }},
        { "tinyint unsigned", new[] { _utinyint }},
        
        // decimals
        { "decimal", new[] { _decimal }},
        { "numeric",new[]  { _decimal }},
        { "dec", new[] { _decimal }},
        { "fixed",new[]  { _decimal }},
        { "double",new[]  { _double }},
        { "float", new[] { _float }},
        { "real",new[]  { _double }},
        
        // binary
        { "binary", new[] { _binary }},
        { "varbinary",new[]  { _varbinary }},
        { "tinyblob", new[] { _varbinary }},
        { "blob",new[]  { _varbinary }},
        { "mediumblob",new[]  { _varbinary }},
        { "longblob",new[]  { _varbinary }},

        // string
        { "char",new[]  { _charUnicode }},
        { "varchar",new[]  { _varcharUnicode }},
        { "nchar", new[] { _nchar }},
        { "nvarchar", new[] { _nvarchar }},
        { "tinytext",new[]  { _tinyTextUnicode }},
        { "text", new[] { _textUnicode }},
        { "mediumtext", new[] { _mediumTextUnicode }},
        { "longtext",new[]  { _longtextUnicode }},
        { "enum",new[]  { _enum }},
        { "set",new[]  { _set }},

        // DateTime
        { "year",new[]  { _int }},
        { "date", new RelationalTypeMapping[] { _date, _dateonly }},
        { "time", new RelationalTypeMapping[] { _time, _timeonly }},
        { "timestamp", new RelationalTypeMapping[] { _timeStamp, _timestampoffset }},
        { "datetime", new RelationalTypeMapping[] { _datetime, _datetimeoffset }},

        // bit
        { "bit",new[]  { _bit }},

        // other
        { "geometry", new[]  { _geometry }},
        { "json", new[]  { _longtextUnicode } }
      };

      _clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
      {
        // integers
        { typeof(short), _smallint },
        { typeof(ushort), _usmallint },
        { typeof(int), _int },
        { typeof(uint), _uint },
        { typeof(long), _bigint },
        { typeof(ulong), _ubigint },

         // byte / char
        { typeof(byte), _utinyint },
        { typeof(sbyte), _tinyint },

        #if !NET8_0
        // DateTime
        { typeof(DateTime), _datetime.Clone(6, null) },
        { typeof(DateOnly), _dateonly },
        { typeof(DateTimeOffset), _datetimeoffset.Clone(6, null) },
        { typeof(TimeSpan), _time.Clone(6, null) },
        { typeof(TimeOnly), _timeonly },
        #else
                // DateTime
        { typeof(DateTime), _datetime.WithPrecisionAndScale(6, null) },
        { typeof(DateOnly), _dateonly },
        { typeof(DateTimeOffset), _datetimeoffset.Clone() },
        { typeof(TimeSpan), _time.WithPrecisionAndScale(6, null) },
        { typeof(TimeOnly), _timeonly },
        #endif

        // decimals
        { typeof(float), _float },
        { typeof(double), _double },
        { typeof(decimal), _decimal },

        { typeof(Data.Types.MySqlGeometry), _geometry }
      };

      // bool
      bool tinyAsBool = _options.ConnectionSettings.TreatTinyAsBoolean;
      _storeTypeMappings[tinyAsBool ? "tinyint(1)" : "bit(1)"] = new RelationalTypeMapping[] { tinyAsBool ? _tinyintBool : _bitBool };
      _clrTypeMappings[typeof(bool)] = tinyAsBool ? _tinyintBool : _bitBool;

      // Guid
      if (_guid != null)
      {
        _storeTypeMappings[_guid!.StoreType] = new RelationalTypeMapping[] { _guid };
        _clrTypeMappings[typeof(Guid)] = _guid;
      }
    }

    /// <inheritdoc/>
    protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
      => base.FindMapping(mappingInfo) ??
      FindRawMapping(mappingInfo)?.Clone(mappingInfo);

    private RelationalTypeMapping? FindRawMapping(RelationalTypeMappingInfo mappingInfo)
    {
      if (!_initialized)
      {
        Initialize();
        _initialized = true;
      }

      var clrType = mappingInfo.ClrType;
      var storeTypeName = mappingInfo.StoreTypeName;
      var storeTypeNameBase = mappingInfo.StoreTypeNameBase;

      if (storeTypeName != null)
      {
        if (_storeTypeMappings!.TryGetValue(storeTypeName, out var mapping))
        {
          return clrType == null
          ? mapping[0]
          : mapping.FirstOrDefault(m => m.ClrType == clrType);
        }

        if (_storeTypeMappings.TryGetValue(storeTypeNameBase!, out mapping))
        {
#if !NET8_0
          return clrType == null
          ? mapping[0].Clone(in mappingInfo)
          : mapping.FirstOrDefault(m => m.ClrType == clrType)?.Clone(in mappingInfo);
#else
          return clrType == null
          ? mapping[0].WithTypeMappingInfo(mappingInfo)
          : mapping.FirstOrDefault(m => m.ClrType == clrType)?.WithTypeMappingInfo(mappingInfo);
#endif
        }
      }

      if (clrType != null)
      {
        if (_clrTypeMappings!.TryGetValue(clrType, out var mapping))
        {
          if (mappingInfo.Precision.HasValue)
          {
            if (clrType == typeof(DateTime) ||
                    clrType == typeof(DateTimeOffset) ||
                    clrType == typeof(TimeSpan))
            {
#if !NET8_0
              return mapping.Clone(mappingInfo.Precision.Value, null);
#else
              return mapping.WithPrecisionAndScale(mappingInfo.Precision.Value, null);
#endif
            }
          }

          return mapping;
        }
        else if (clrType == typeof(string))
        {
          var isAnsi = mappingInfo.IsUnicode == false;
          var isFixedLength = mappingInfo.IsFixedLength == true;
          var size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ?
          Math.Min(MAXKEYLENGTH / (_options.CharSet!.byteCount * 2), 255)
          : null);

          if (size > 65_553 / _options.CharSet!.byteCount)
          {
            size = null;
            isFixedLength = false;
          }

          mapping = isFixedLength ? _charUnicode : size == null
          ? _longtextUnicode : _varcharUnicode;

#if !NET8_0
          return size == null ? mapping : mapping.Clone($"{mapping.StoreTypeNameBase}({size})", size);
#else
          return size == null ? mapping : mapping.WithStoreTypeAndSize($"{mapping.StoreTypeNameBase}({size})", size);
#endif
        }
        else if (clrType == typeof(byte[]))
        {
          bool isFixedLength = mappingInfo.IsFixedLength == true;
          var size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? MAXKEYLENGTH : null);
          return new MySQLByteArrayTypeMapping(
                size: size,
                fixedLength: isFixedLength);
        }
      }

      return null;
    }

#if !NET8_0
    protected override string ParseStoreTypeName(string? storeTypeName, out bool? unicode, out int? size, out int? precision, out int? scale)
    {
      var storeTypeBaseName = base.ParseStoreTypeName(storeTypeName, out unicode, out size, out precision, out scale);

      return (storeTypeName?.IndexOf("unsigned", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
        ? storeTypeBaseName + " unsigned"
        : storeTypeBaseName!;
    }
#else
    protected string ParseStoreTypeName(string? storeTypeName, ref bool? unicode, ref int? size, ref int? precision, ref int? scale)
    {
      var storeTypeBaseName = base.ParseStoreTypeName(storeTypeName, ref unicode, ref size, ref precision, ref scale);

      return (storeTypeName?.IndexOf("unsigned", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
        ? storeTypeBaseName + " unsigned"
        : storeTypeBaseName!;
    }
#endif
  }
}