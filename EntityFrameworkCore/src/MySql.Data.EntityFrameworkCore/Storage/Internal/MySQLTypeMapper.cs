// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  internal partial class MySQLTypeMapper : TypeMapperWrapper
  {
    private static int _longTextMaxLength = int.MaxValue;
    private static int _textMaxLength = 65535;
    private static int _keyMaxLength = 767;

    private readonly RelationalTypeMapping _int = new MySQLNumberTypeMapping("int", typeof(Int32), DbType.Int32);
    private readonly RelationalTypeMapping _bigint = new MySQLNumberTypeMapping("bigint", typeof(Int64), DbType.Int64);
    private readonly RelationalTypeMapping _bit = new MySQLNumberTypeMapping("bit", typeof(short), DbType.Int16);
    private readonly RelationalTypeMapping _smallint = new MySQLNumberTypeMapping("smallint", typeof(Int16), DbType.Int16);
    private readonly RelationalTypeMapping _tinyint = new MySQLNumberTypeMapping("tinyint", typeof(Byte), DbType.Byte);

    private readonly RelationalTypeMapping _char = new MySQLStringTypeMapping("char");
    private readonly RelationalTypeMapping _varchar = new MySQLStringTypeMapping($"varchar({_textMaxLength})", dbType: null, unicode: false, size: _textMaxLength, hasNonDefaultUnicode: true);
    private readonly RelationalTypeMapping _varcharkey = new MySQLStringTypeMapping($"varchar({_keyMaxLength})", dbType: null, unicode: false, size: _keyMaxLength, hasNonDefaultUnicode: true);

    private readonly RelationalTypeMapping _rowversion = new MySQLDatetimeTypeMapping("timestamp", typeof(DateTime), dbType: DbType.DateTime);

    private readonly RelationalTypeMapping _longText = new MySQLStringTypeMapping("longtext");
    private readonly RelationalTypeMapping _mediumText = new MySQLStringTypeMapping("mediumtext");
    private readonly RelationalTypeMapping _text = new MySQLStringTypeMapping("text");
    private readonly RelationalTypeMapping _tinyText = new MySQLStringTypeMapping("tinytext");

    private readonly RelationalTypeMapping _datetime = new MySQLDatetimeTypeMapping("datetime", typeof(DateTime));
    private readonly RelationalTypeMapping _datetimeoffset = new MySQLDatetimeTypeMapping("timestamp", typeof(DateTimeOffset));
    private readonly RelationalTypeMapping _date = new MySQLDatetimeTypeMapping("date", typeof(DateTime));
    private readonly RelationalTypeMapping _time = new MySQLDatetimeTypeMapping("time", typeof(TimeSpan), null);

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

    private readonly RelationalTypeMapping _enum = new MySQLStringTypeMapping("enum");
    private readonly RelationalTypeMapping _set = new MySQLStringTypeMapping("set");


    private readonly Dictionary<string, RelationalTypeMapping> _storeTypeMappings;
    private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;
    public override IStringRelationalTypeMapper StringMapper { get; }
    public override IByteArrayRelationalTypeMapper ByteArrayMapper { get; }

    public MySQLTypeMapper()
      : base()
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
        { "datetimeoffset", _datetimeoffset },
        { "date", _date },
        { "time", _time },
        { "timestamp", _datetime },
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
        { "set", _set },
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
        { typeof(byte[]), _varbinary },
        { typeof(TimeSpan), _time }
      };


      StringMapper
            = new StringRelationalTypeMapper(
                _textMaxLength,
                _text,
                _longText,
                _varcharkey,
                size => new MySQLStringTypeMapping(
                    "varchar(" + size + ")",
                    dbType: DbType.AnsiString,
                    unicode: false,
                    size: size,
                    hasNonDefaultUnicode: true),
                _textMaxLength,
                _text,
                _longText,
                _varcharkey,
                size => new MySQLStringTypeMapping(
                    "varchar(" + size + ")",
                    dbType: null,
                    unicode: true,
                    size: size,
                    hasNonDefaultUnicode: false));



      ByteArrayMapper
          = new ByteArrayRelationalTypeMapper(
              _longTextMaxLength,
              _varbinary,
              _varbinary,
              _varbinary,
              _rowversion, _ => _varbinary);
    }


    protected override IReadOnlyDictionary<Type, RelationalTypeMapping> GetClrTypeMappings()
    => _clrTypeMappings;

    protected override IReadOnlyDictionary<string, RelationalTypeMapping> GetStoreTypeMappings()
    => _storeTypeMappings;


    protected override string GetColumnType(IProperty property)
    {
      return property.Relational().ColumnType;
    }


    public override RelationalTypeMapping FindMapping(Type clrType)
    {
      ThrowIf.Argument.IsNull(clrType, "clrType");
      var sType = Nullable.GetUnderlyingType(clrType) ?? clrType;
      return sType == typeof(string)
          ? _varchar
          : (sType == typeof(byte[])
              ? _varbinary
              : base.FindMapping(clrType));
    }
  }
}
