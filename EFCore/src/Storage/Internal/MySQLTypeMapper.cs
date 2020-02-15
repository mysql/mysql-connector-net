// Copyright (c) 2020, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.EntityFrameworkCore.Properties;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLTypeMapper : RelationalTypeMappingSource
  {
    private static int CHAR_MAX_LENGTH = 255;
    private static int VARCHAR_MAX_LENGTH = 4000;
    private static int _textMaxLength = 65535;
    private static int _keyMaxLength = 767;

    private readonly RelationalTypeMapping _int = new MySQLNumberTypeMapping("int", typeof(Int32), DbType.Int32);
    private readonly RelationalTypeMapping _bigint = new MySQLNumberTypeMapping("bigint", typeof(Int64), DbType.Int64);
    private readonly RelationalTypeMapping _bit = new MySQLNumberTypeMapping("bit", typeof(short), DbType.Int16);
    private readonly RelationalTypeMapping _smallint = new MySQLNumberTypeMapping("smallint", typeof(Int16), DbType.Int16);
    private readonly RelationalTypeMapping _tinyint = new MySQLNumberTypeMapping("tinyint", typeof(Byte), DbType.Byte);

    private readonly RelationalTypeMapping _char = new MySQLStringTypeMapping("char", DbType.StringFixedLength, unicode: true, fixedLength: true);
    private readonly RelationalTypeMapping _varchar = new MySQLStringTypeMapping($"varchar({_textMaxLength})", DbType.String, unicode: false);

    private readonly RelationalTypeMapping _longText = new MySQLStringTypeMapping("longtext", DbType.String, unicode: true);
    private readonly RelationalTypeMapping _mediumText = new MySQLStringTypeMapping("mediumtext", DbType.String, unicode: true);
    private readonly RelationalTypeMapping _text = new MySQLStringTypeMapping("text", DbType.String, unicode: true);
    private readonly RelationalTypeMapping _tinyText = new MySQLStringTypeMapping("tinytext", DbType.String, unicode: true);

    private readonly RelationalTypeMapping _datetime = new MySQLDateTimeTypeMapping("datetime", typeof(DateTime));
    private readonly RelationalTypeMapping _datetimeoffset = new MySQLDateTimeTypeMapping("timestamp", typeof(DateTimeOffset));
    private readonly RelationalTypeMapping _date = new MySQLDateTimeTypeMapping("date", typeof(DateTime));
    private readonly RelationalTypeMapping _time = new MySQLTimeSpanMapping("time");

    private readonly RelationalTypeMapping _float = new MySQLNumberTypeMapping("float", typeof(float));
    private readonly RelationalTypeMapping _double = new MySQLNumberTypeMapping("double", typeof(double));
    private readonly RelationalTypeMapping _real = new MySQLNumberTypeMapping("real", typeof(Single));
    private readonly RelationalTypeMapping _decimal = new MySQLNumberTypeMapping("decimal(18, 2)", typeof(Decimal));

    private readonly RelationalTypeMapping _binary = new MySQLBinaryTypeMapping("binary");
    private readonly RelationalTypeMapping _varbinary = new MySQLBinaryTypeMapping("varbinary");
    private readonly RelationalTypeMapping _tinyblob = new MySQLBinaryTypeMapping("tinyblob");
    private readonly RelationalTypeMapping _mediumblob = new MySQLBinaryTypeMapping("mediumblob");
    private readonly RelationalTypeMapping _blob = new MySQLBinaryTypeMapping("blob");
    private readonly RelationalTypeMapping _longblob = new MySQLBinaryTypeMapping("longblob");

    private readonly RelationalTypeMapping _enum = new MySQLStringTypeMapping("enum", DbType.String, unicode: true);
    private readonly RelationalTypeMapping _geometry = new MySQLGeometryTypeMapping("geometry");

    Dictionary<string, RelationalTypeMapping> extraMapping = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
    {
      { "timestamp", new MySQLDateTimeTypeMapping("timestamp", typeof(DateTime)) }
    };

    private readonly Dictionary<string, RelationalTypeMapping> _storeTypeMappings;
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
      _storeTypeMappings = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
      {
        { "bigint", _bigint },
        { "decimal", _decimal },
        { "double", _double },
        { "float", _float },
        { "int", _int},
        { "mediumint", _int },
        { "real", _real },
        { "smallint", _smallint },
        { "tinyint", _tinyint },
        { "char", _char },
        { "varchar", _varchar},
        { "tinytext", _tinyText},
        { "text", _text},
        { "mediumtext", _mediumText},
        { "longtext", _longText},
        { "datetime", _datetime },
        { "date", _date },
        { "time", _time },
        { "timestamp", _datetimeoffset },
        { "year", _smallint },
        { "bit", _bit },
        { "string", _varchar },
        { "tinyblob", _tinyblob },
        { "blob", _blob },
        { "mediumblob", _mediumblob },
        { "longblob", _longblob },
        { "binary", _binary },
        { "varbinary", _varbinary },
        { "enum", _enum },
        { "geometry", _geometry },
        { "json", _varchar }
      };

      _clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
      {
        { typeof(int), _int },
        { typeof(long), _bigint },
        { typeof(DateTime), _datetime },
        { typeof(DateTimeOffset), _datetimeoffset },
        { typeof(bool), _bit },
        { typeof(byte), _tinyint },
        { typeof(float), _float },
        { typeof(double), _double },
        { typeof(char), _int },
        { typeof(sbyte), _smallint },
        { typeof(ushort), _int },
        { typeof(uint), _bigint },
        { typeof(ulong), new MySQLNumberTypeMapping("numeric(20, 0)" ,_decimal.GetType()) },
        { typeof(short), _smallint },
        { typeof(decimal), _decimal },
        { typeof(Types.MySqlGeometry), _geometry },
        { typeof(TimeSpan), _time }
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
        => FindRawMapping(mappingInfo)?.Clone(mappingInfo)
            ?? base.FindMapping(mappingInfo);

    private RelationalTypeMapping FindRawMapping(RelationalTypeMappingInfo mappingInfo)
    {
      var clrType = mappingInfo.ClrType;
      var storeTypeName = mappingInfo.StoreTypeName;
      var storeTypeNameBase = mappingInfo.StoreTypeNameBase;
      RelationalTypeMapping mapping;

      if (storeTypeName != null)
      {
        if (_storeTypeMappings.TryGetValue(storeTypeName, out mapping)
            || _storeTypeMappings.TryGetValue(storeTypeNameBase, out mapping))
        {
          return clrType == null
                 || mapping.ClrType == clrType
              ? mapping
              : CheckExtraMapping(storeTypeName, storeTypeNameBase, clrType);
        }
      }

      if (clrType != null)
      {
        if (_clrTypeMappings.TryGetValue(clrType, out mapping)) return mapping;
        else if (clrType == typeof(string))
        {
          bool isAnsi = mappingInfo.IsUnicode == false;
          bool isFixedLength = mappingInfo.IsFixedLength == true;
          int? size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? (int?)_keyMaxLength : null);
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

    private RelationalTypeMapping CheckExtraMapping(string storeTypeName, string storeTypeNameBase, Type clrType)
    {
      RelationalTypeMapping mapping;
      if (extraMapping.TryGetValue(storeTypeName, out mapping)
        || extraMapping.TryGetValue(storeTypeNameBase, out mapping))
      {
        if (mapping.ClrType != clrType)
          mapping = null;
      }

      return mapping;
    }
  }
}
