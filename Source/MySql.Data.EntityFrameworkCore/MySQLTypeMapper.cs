// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore;

namespace MySQL.Data.EntityFrameworkCore
{
  public class MySQLTypeMapper : RelationalTypeMapper
  {

    //max lenght for text types considering 3-bytes character sets.
    private static int _medTextMaxLength = ((int)Math.Pow(2, 24) - 1)/3;
    private static int _textMaxLength = ((int)Math.Pow(2, 16) - 1) / 3;
    private static int _longTextMaxLength = ((int)Math.Pow(2, 32) - 1) / 3;
    


    private readonly RelationalTypeMapping _int = new RelationalTypeMapping("int", typeof(Int32));
    private readonly RelationalTypeMapping _bigint = new RelationalTypeMapping("bigint", typeof(Int64)); 
    private readonly RelationalTypeMapping _bit = new RelationalTypeMapping("bit", typeof(SByte)); 
    private readonly RelationalTypeMapping _smallint = new RelationalTypeMapping("smallint", typeof(Int16)); 
    private readonly RelationalTypeMapping _tinyint = new RelationalTypeMapping("tinyint", typeof(Byte));
    private readonly RelationalTypeMapping _char = new RelationalTypeMapping("char", typeof(Byte));
    private readonly RelationalTypeMapping _string = new RelationalTypeMapping("varchar", typeof(String), dbType: DbType.AnsiString, unicode: false, size: null, hasNonDefaultUnicode: true);


    //private readonly RelationalTypeMapping _mediumint = new RelationalTypeMapping("mediumint", );

    private readonly RelationalTypeMapping _longText = new RelationalTypeMapping("longtext", typeof(string)); 
    private readonly RelationalTypeMapping _mediumText = new RelationalTypeMapping("mediumtext", typeof(string));
    private readonly RelationalTypeMapping _Text = new RelationalTypeMapping("text", typeof(string));
    private readonly RelationalTypeMapping _tinyText = new RelationalTypeMapping("tinytext", typeof(string));

    private readonly RelationalTypeMapping _varchar = new RelationalTypeMapping("varchar(1000)", typeof(string), dbType: DbType.AnsiString, unicode: false, size: 1000, hasNonDefaultUnicode: true);
    private readonly RelationalTypeMapping _varbinary = new RelationalTypeMapping("blob", typeof(byte[]));
    private readonly RelationalTypeMapping _datetime = new RelationalTypeMapping("datetime", typeof(DateTime)); 
    private readonly RelationalTypeMapping _date = new RelationalTypeMapping("date", typeof(DateTime)); 
    private readonly RelationalTypeMapping _time = new RelationalTypeMapping("time", typeof(DateTime));
    private readonly RelationalTypeMapping _double = new RelationalTypeMapping("float", typeof(Single)); 
    private readonly RelationalTypeMapping _real = new RelationalTypeMapping("real", typeof(Single));
    private readonly RelationalTypeMapping _decimal = new RelationalTypeMapping("decimal(18, 2)", typeof(Decimal));


    private readonly Dictionary<string, RelationalTypeMapping> _storeTypeMappings;
    private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;

    //private readonly Dictionary<string, RelationalTypeMapping> _simpleNameMappings;
    //private readonly Dictionary<Type, RelationalTypeMapping> _simpleMappings;

    public MySQLTypeMapper()
    {

      _storeTypeMappings = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
      {
        {"bigint", _bigint},
        { "decimal", _decimal },
        { "double", _double },
        { "float", _double },
        {"int", _int},        
        { "mediumint", _int },        
        { "real", _real },
        { "smallint", _smallint },
        { "tinyint", _tinyint },
        { "char", _char },        
        { "varchar", _varchar},
        { "longtext", _longText},
        { "mediumtext", _mediumText},
        { "text", _Text},
        { "tinytext", _tinyText},
        { "datetime", _datetime },
        { "timestamp", _datetime },
        { "bit", _bit },
        { "string", _string },
        { "blob", _varbinary }
      };

      _clrTypeMappings
          = new Dictionary<Type, RelationalTypeMapping>
          {
                    { typeof(int), _int },
                    { typeof(long), _bigint },
                    { typeof(DateTime), _datetime },
                    { typeof(bool), _bit },
                    { typeof(byte), _tinyint },
                    { typeof(double), _double },
                    { typeof(char), _int },
                    { typeof(sbyte), new RelationalTypeMapping("smallint", _smallint.GetType()) },
                    { typeof(ushort), new RelationalTypeMapping("int", _int.GetType()) },
                    { typeof(uint), new RelationalTypeMapping("bigint", _bigint.GetType()) },
                    { typeof(ulong), new RelationalTypeMapping("numeric(20, 0)" ,_decimal.GetType()) },
                    { typeof(short), _smallint },
                    { typeof(float), _real },
                    { typeof(decimal), _decimal },
                    { typeof(string), _string },
                    { typeof(byte[]), _varbinary }
          };
   
    }

    protected override IReadOnlyDictionary<Type, RelationalTypeMapping> GetClrTypeMappings()
    => _clrTypeMappings;

    protected override IReadOnlyDictionary<string, RelationalTypeMapping> GetStoreTypeMappings()
    => _storeTypeMappings;


    protected override string GetColumnType(IProperty property)
    {
      return property.MySQL().ColumnType;
    }

    //protected override RelationalTypeMapping FindCustomMapping(IProperty property, bool unicode = true)
    //{
    //  if (property.ClrType == typeof(String))
    //  {
    //    int maxLength = property.GetMaxLength() ?? 255;
    //    if (maxLength <= _medTextMaxLength) return new RelationalSizedTypeMapping("varchar(" + maxLength + ")", typeof(String), unicode, maxLength); ;
    //    if (maxLength <= _longTextMaxLength) return _mediumText;
    //    return _longText;
    //  }

    //  if (property.ClrType == typeof(byte[]))
    //  {
    //    return _varbinary;
    //  }

    //  return base.FindCustomMapping(property);
    //}
  }
}
