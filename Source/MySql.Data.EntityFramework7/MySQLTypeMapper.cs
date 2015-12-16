// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;

namespace MySQL.Data.Entity
{
  public class MySQLTypeMapper : RelationalTypeMapper
  {
    private static int _medTextMaxLength = ((int)Math.Pow(2, 24) - 1)/3;
    private static int _textMaxLength = ((int)Math.Pow(2, 16) - 1) / 3;
    private static int _longTextMaxLength = ((int)Math.Pow(2, 32) - 1) / 3;

    //    private readonly SqlServerMaxLengthMapping _nvarcharmax = new SqlServerMaxLengthMapping("nvarchar(max)");
    //  private readonly SqlServerMaxLengthMapping _nvarchar450 = new SqlServerMaxLengthMapping("nvarchar(450)");
    // private readonly SqlServerMaxLengthMapping _varbinarymax = new SqlServerMaxLengthMapping("varbinary(max)", DbType.Binary);
    // private readonly SqlServerMaxLengthMapping _varbinary900 = new SqlServerMaxLengthMapping("varbinary(900)", DbType.Binary);
    //    private readonly RelationalSizedTypeMapping _rowversion = new RelationalSizedTypeMapping("rowversion", DbType.Binary, 8);
    private readonly RelationalTypeMapping _int = new RelationalTypeMapping("int", typeof(Int32));
    private readonly RelationalTypeMapping _bigint = new RelationalTypeMapping("bigint", typeof(Int64)); 
    private readonly RelationalTypeMapping _bit = new RelationalTypeMapping("bit", typeof(SByte)); 
    private readonly RelationalTypeMapping _smallint = new RelationalTypeMapping("smallint", typeof(Int16)); 
    private readonly RelationalTypeMapping _tinyint = new RelationalTypeMapping("tinyint", typeof(Byte));
    //private readonly RelationalTypeMapping _mediumint = new RelationalTypeMapping("mediumint", );

    // private readonly SqlServerMaxLengthMapping _nchar = new SqlServerMaxLengthMapping("nchar", DbType.StringFixedLength);
    // private readonly SqlServerMaxLengthMapping _nvarchar = new SqlServerMaxLengthMapping("nvarchar");
    //  private readonly RelationalTypeMapping _varcharmax = new SqlServerMaxLengthMapping("varchar(max)", DbType.AnsiString);
    // private readonly SqlServerMaxLengthMapping _char = new SqlServerMaxLengthMapping("char", DbType.AnsiStringFixedLength);

    private readonly RelationalTypeMapping _longText = new RelationalTypeMapping("longtext", typeof(String)); 
    private readonly RelationalTypeMapping _mediumText = new RelationalTypeMapping("mediumtext", typeof(String));
    private readonly RelationalTypeMapping _Text = new RelationalTypeMapping("text", typeof(String));
    private readonly RelationalTypeMapping _tinyText = new RelationalTypeMapping("tinytext", typeof(String));

    private readonly RelationalSizedTypeMapping _varchar = new RelationalSizedTypeMapping("varchar(1000)", typeof(String), 1000);
    //private readonly SqlServerMaxLengthMapping _varbinary = new SqlServerMaxLengthMapping("varbinary", DbType.Binary);
    private readonly RelationalTypeMapping _datetime = new RelationalTypeMapping("datetime", typeof(DateTime)); 
    private readonly RelationalTypeMapping _date = new RelationalTypeMapping("date", typeof(DateTime)); 
    private readonly RelationalTypeMapping _time = new RelationalTypeMapping("time", typeof(DateTime));
    private readonly RelationalTypeMapping _double = new RelationalTypeMapping("float", typeof(Single)); 
    private readonly RelationalTypeMapping _real = new RelationalTypeMapping("real", typeof(Single));
    //    private readonly RelationalTypeMapping _uniqueidentifier = new RelationalTypeMapping("uniqueidentifier");
    private readonly RelationalTypeMapping _decimal = new RelationalTypeMapping("decimal(18, 2)", typeof(Decimal)); 

    private readonly Dictionary<string, RelationalTypeMapping> _simpleNameMappings;
    private readonly Dictionary<Type, RelationalTypeMapping> _simpleMappings;

    public MySQLTypeMapper()
    {
      _simpleNameMappings = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
      {
        { "datetime", _datetime },
        { "varchar", _varchar }
      };

      _simpleMappings
          = new Dictionary<Type, RelationalTypeMapping>
          {
                    { typeof(int), _int },
                    { typeof(long), _bigint },
                    { typeof(DateTime), _datetime },
//                    { typeof(string), _varchar },
//                    { typeof(Guid), _uniqueidentifier },
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
                  //  { typeof(TimeSpan), _time }
          };
    }

    protected override IReadOnlyDictionary<Type, RelationalTypeMapping> SimpleMappings
    {
      get { return _simpleMappings; }
    }

    protected override IReadOnlyDictionary<string, RelationalTypeMapping> SimpleNameMappings
    {
      get { return _simpleNameMappings; }
    }

    protected override string GetColumnType(IProperty property)
    {
      return property.MySQL().ColumnType;
    }

    protected override RelationalTypeMapping GetCustomMapping([NotNull] IProperty property)
    {
      ThrowIf.Argument.IsNull(property, "property");

      ///TODO:  fix this
      //var clrType = property.ClrType.UnwrapEnumType();

      if (property.ClrType == typeof(string))
        return GetStringMapping(property, _medTextMaxLength, maxLen => GetBoundedMapping(maxLen), 
          _longText, _varchar);

      return base.GetCustomMapping(property);
    }

    private RelationalTypeMapping GetBoundedMapping(int maxLen)
    {
      if (maxLen <= 1000) return new RelationalTypeMapping("varchar(" + maxLen + ")", typeof(string));
      if (maxLen <= _medTextMaxLength) return _Text;
      if (maxLen <= _longTextMaxLength) return _mediumText;
      return _longText;
    }
  }
}
