// Copyright (c) 2020, 2021, Oracle and/or its affiliates.
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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Properties;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace MySql.EntityFrameworkCore.Storage.Internal
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
    private readonly MySQLNumberTypeMapping _bit = new MySQLNumberTypeMapping("bit", typeof(ulong), DbType.UInt64);
    private readonly MySQLNumberTypeMapping _smallint = new MySQLNumberTypeMapping("smallint", typeof(Int16), DbType.Int16);
    private readonly MySQLNumberTypeMapping _usmallint = new MySQLNumberTypeMapping("smallint unsigned", typeof(Int16), DbType.UInt16);
    private readonly MySQLNumberTypeMapping _tinyint = new MySQLNumberTypeMapping("tinyint", typeof(Byte), DbType.SByte);
    private readonly MySQLNumberTypeMapping _utinyint = new MySQLNumberTypeMapping("tinyint unsigned", typeof(Byte), DbType.Byte);

    private readonly MySQLStringTypeMapping _charUnicode = new MySQLStringTypeMapping("char", DbType.StringFixedLength, unicode: true, fixedLength: true);
    private readonly MySQLStringTypeMapping _varcharUnicode = new MySQLStringTypeMapping($"varchar", DbType.String, unicode: true);
    private readonly MySQLStringTypeMapping _varcharmaxUnicode = new MySQLStringTypeMapping("longtext", DbType.String, unicode: true);

    private readonly MySQLDateTimeTypeMapping _datetime = new MySQLDateTimeTypeMapping("datetime", typeof(DateTime));
    private readonly MySQLDateTimeTypeMapping _datetimeoffset = new MySQLDateTimeTypeMapping("timestamp", typeof(DateTimeOffset));
    private readonly MySQLDateTimeTypeMapping _date = new MySQLDateTimeTypeMapping("date", typeof(DateTime));
    private readonly MySQLTimeSpanMapping _time = new MySQLTimeSpanMapping("time");

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

    private readonly MySQLBoolTypeMapping _bitBool = new MySQLBoolTypeMapping("bit", size: 1);
    private readonly MySQLBoolTypeMapping _tinyintBool = new MySQLBoolTypeMapping("tinyint", size: 1);
    private GuidTypeMapping _guid;
    private MySQLGuidFormat guidFormat = MySQLGuidFormat.Default;

    private Dictionary<string, RelationalTypeMapping> _storeTypeMappings;
    private Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;

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

    private readonly IMySQLOptions _options;
    private bool _initialized;

    public MySQLTypeMapper([NotNull] TypeMappingSourceDependencies dependencies,
      [NotNull] RelationalTypeMappingSourceDependencies relationalDependencies,
      IMySQLOptions options)
      : base(dependencies, relationalDependencies)
    {
      _options = options;
    }

    protected void Initialize()
    {
      if (guidFormat == MySQLGuidFormat.Default)
      {
        guidFormat = _options.ConnectionSettings.OldGuids
            ? MySQLGuidFormat.LittleEndianBinary16
            : MySQLGuidFormat.Char36;
      }

      _guid = MySQLGuidTypeMapping.IsValidGuidFormat(guidFormat)
                      ? new MySQLGuidTypeMapping(guidFormat)
                      : null;
      _storeTypeMappings = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
      {
        // integers
        { "bigint", _bigint },
        { "bigint unsigned", _ubigint },
        { "int", _int },
        { "int unsigned", _uint },
        { "integer", _int },
        { "integer unsigned", _uint },
        { "mediumint", _int },
        { "mediumint unsigned", _uint },
        { "smallint", _smallint },
        { "smallint unsigned", _usmallint },
        { "tinyint", _tinyint },
        { "tinyint unsigned", _utinyint },

        // decimals
        { "decimal", _decimal },
        { "numeric", _decimal },
        { "dec", _decimal },
        { "fixed", _decimal },
        { "double", _double },
        { "float", _float },
        { "real", _real },

        // binary
        { "tinyblob", _tinyblob },
        { "blob", _blob },
        { "mediumblob", _mediumblob },
        { "longblob", _longblob },
        { "binary", _binary },
        { "varbinary", _varbinary },

        // string
        { "char", _charUnicode },
        { "varchar", _varcharUnicode },
        { "tinytext", _varcharmaxUnicode },
        { "text", _varcharmaxUnicode },
        { "mediumtext", _varcharmaxUnicode },
        { "longtext", _varcharmaxUnicode },
        { "enum", _enum },

        // DateTime
        { "date", _date },
        { "time", _time },
        { "year", _smallint },
        { "datetime", _datetime },

        // bit
        { "bit", _bit },

        // other
        { "geometry", _geometry },
        { "json", _varcharmaxUnicode }
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
        { typeof(byte), _utinyint },
        { typeof(sbyte), _tinyint },

        // DateTime
        { typeof(DateTime), _datetime },
        { typeof(DateTimeOffset), _datetimeoffset },
        { typeof(TimeSpan), _time },

        // decimals
        { typeof(float), _float },
        { typeof(double), _double },
        { typeof(decimal), _decimal },

        { typeof(char), _int },
        { typeof(Data.Types.MySqlGeometry), _geometry }
      };

      // bool
      if (_options.ConnectionSettings.TreatTinyAsBoolean)
        _clrTypeMappings[typeof(bool)] = _tinyintBool;
      else
        _clrTypeMappings[typeof(bool)] = _bitBool;

      // Guid
      if (_options.ConnectionSettings.OldGuids)
      {
        _storeTypeMappings.Add(_guid.StoreType, _guid);
        _clrTypeMappings.Add(typeof(Guid), _guid);
      }

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
        if (_options.ConnectionSettings.TreatTinyAsBoolean)
        {
          if (storeTypeNameBase.Equals(_bitBool.StoreTypeNameBase, StringComparison.OrdinalIgnoreCase)
            && mappingInfo.Size == 1)
            return _bitBool;
          else if (storeTypeNameBase.Equals(_tinyintBool.StoreTypeNameBase, StringComparison.OrdinalIgnoreCase)
            && mappingInfo.Size == 1)
            return _tinyintBool;
        }

        if (_storeTypeMappings.TryGetValue(storeTypeName, out var mapping)
          || _storeTypeMappings.TryGetValue(storeTypeNameBase, out mapping))
        {
          return clrType == null
                 || mapping.ClrType == clrType
              ? mapping
              : null;
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