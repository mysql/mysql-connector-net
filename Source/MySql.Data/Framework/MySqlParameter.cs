// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using MySql.Data.MySqlClient;
#if !NETCORE10
using System.ComponentModel.Design.Serialization;
using ParameterDirection = System.Data.ParameterDirection;
#endif

namespace MySql.Data.MySqlClient
{
  [TypeConverter(typeof(MySqlParameterConverter))]
#if NETCORE10
  public sealed partial class MySqlParameter : DbParameter
#else
  public sealed partial class MySqlParameter : DbParameter, IDbDataParameter
#endif
  {
    private DbType _dbType;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name, the <see cref="MySqlParameter.MySqlDbType"/>, the size, and the source column name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to map. </param>
    /// <param name="dbType">One of the <see cref="MySqlParameter.MySqlDbType"/> values. </param>
    /// <param name="size">The length of the parameter. </param>
    /// <param name="sourceColumn">The name of the source column. </param>
    public MySqlParameter(string parameterName, MySqlDbType dbType, int size, string sourceColumn) : this(parameterName, dbType)
    {
      Size = size;
      Direction = ParameterDirection.Input;
      SourceColumn = sourceColumn;
#if !NETCORE10
      SourceVersion = DataRowVersion.Current;
#endif
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name, the type of the parameter, the size of the parameter, a <see cref="ParameterDirection"/>, the precision of the parameter, the scale of the parameter, the source column, a <see cref="DataRowVersion"/> to use, and the value of the parameter.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to map. </param>
    /// <param name="dbType">One of the <see cref="MySqlParameter.MySqlDbType"/> values. </param>
    /// <param name="size">The length of the parameter. </param>
    /// <param name="direction">One of the <see cref="ParameterDirection"/> values. </param>
    /// <param name="isNullable">true if the value of the field can be null, otherwise false. </param>
    /// <param name="precision">The total number of digits to the left and right of the decimal point to which <see cref="MySqlParameter.Value"/> is resolved.</param>
    /// <param name="scale">The total number of decimal places to which <see cref="MySqlParameter.Value"/> is resolved. </param>
    /// <param name="sourceColumn">The name of the source column. </param>
    /// <param name="sourceVersion">One of the <see cref="DataRowVersion"/> values. </param>
    /// <param name="value">An <see cref="Object"/> that is the value of the <see cref="MySqlParameter"/>. </param>
    /// <exception cref="ArgumentException"/>
#if NETCORE10
    public MySqlParameter(string parameterName, MySqlDbType dbType, int size, ParameterDirection direction,
                          bool isNullable, byte precision, byte scale, string sourceColumn, object value)
      : this(parameterName, dbType, size, sourceColumn)
#else
    public MySqlParameter(string parameterName, MySqlDbType dbType, int size, ParameterDirection direction,
                          bool isNullable, byte precision, byte scale, string sourceColumn,
                          DataRowVersion sourceVersion, object value) 
      : this(parameterName, dbType, size, sourceColumn)
#endif
    {
      Direction = direction;
      IsNullable = isNullable;
      Precision = precision;
      Scale = scale;
      Value = value;
#if !NETCORE10
      SourceVersion = sourceVersion;
#endif

    }

#if NETCORE10
    internal MySqlParameter(string name, MySqlDbType type, ParameterDirection dir, string col, object val)
      : this(name, type)

#else
    internal MySqlParameter(string name, MySqlDbType type, ParameterDirection dir, string col, DataRowVersion sourceVersion, object val)
      : this(name, type)
#endif
    {
      Direction = dir;
      SourceColumn = col;
      Value = val;
#if !NETCORE10
      SourceVersion = sourceVersion;
#endif
    }

    partial void Init()
    {
#if !NETCORE10
      SourceVersion = DataRowVersion.Current;
#endif
      Direction = ParameterDirection.Input;
    }

#if !NETCORE10
    /// <summary>
    /// Gets or sets the <see cref="DataRowVersion"/> to use when loading <see cref="Value"/>.
    /// </summary>
    [Category("Data")]
    public override DataRowVersion SourceVersion { get; set; }
#endif

    /// <summary>
    /// Gets or sets the name of the source column that is mapped to the <see cref="DataSet"/> and used for loading or returning the <see cref="MySqlParameter.Value"/>.
    /// </summary>
#if !NETCORE10
    [Category("Data")]
#endif
    public override String SourceColumn { get; set; }

    /// <summary>
    /// Resets the <b>DbType</b> property to its original settings. 
    /// </summary>
    public override void ResetDbType()
    {
      _inferType = true;
    }

    /// <summary>
    /// Sets or gets a value which indicates whether the source column is nullable. 
    /// This allows <see cref="DbCommandBuilder"/> to correctly generate Update statements 
    /// for nullable columns. 
    /// </summary>
    public override bool SourceColumnNullMapping { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbType"/> of the parameter.
    /// </summary>
    public override DbType DbType
    {
      get { return _dbType; }
      set
      {
        SetDbType(value);
        _inferType = false;
      }
    }

    partial void SetDbTypeFromMySqlDbType()
    {
      switch (_mySqlDbType)
      {
        case MySqlDbType.NewDecimal:
        case MySqlDbType.Decimal:
          _dbType = DbType.Decimal;
          break;
        case MySqlDbType.Byte:
          _dbType = DbType.SByte;
          break;
        case MySqlDbType.UByte:
          _dbType = DbType.Byte;
          break;
        case MySqlDbType.Int16:
          _dbType = DbType.Int16;
          break;
        case MySqlDbType.UInt16:
          _dbType = DbType.UInt16;
          break;
        case MySqlDbType.Int24:
        case MySqlDbType.Int32:
          _dbType = DbType.Int32;
          break;
        case MySqlDbType.UInt24:
        case MySqlDbType.UInt32:
          _dbType = DbType.UInt32;
          break;
        case MySqlDbType.Int64:
          _dbType = DbType.Int64;
          break;
        case MySqlDbType.UInt64:
          _dbType = DbType.UInt64;
          break;
        case MySqlDbType.Bit:
          _dbType = DbType.UInt64;
          break;
        case MySqlDbType.Float:
          _dbType = DbType.Single;
          break;
        case MySqlDbType.Double:
          _dbType = DbType.Double;
          break;
        case MySqlDbType.Timestamp:
        case MySqlDbType.DateTime:
          _dbType = DbType.DateTime;
          break;
        case MySqlDbType.Date:
        case MySqlDbType.Newdate:
        case MySqlDbType.Year:
          _dbType = DbType.Date;
          break;
        case MySqlDbType.Time:
          _dbType = DbType.Time;
          break;
        case MySqlDbType.Enum:
        case MySqlDbType.Set:
        case MySqlDbType.VarChar:
          _dbType = DbType.String;
          break;
        case MySqlDbType.TinyBlob:
        case MySqlDbType.MediumBlob:
        case MySqlDbType.LongBlob:
        case MySqlDbType.Blob:
          _dbType = DbType.Object;
          break;
        case MySqlDbType.String:
          _dbType = DbType.StringFixedLength;
          break;
        case MySqlDbType.Guid:
          _dbType = DbType.Guid;
          break;
      }
    }


    private void SetDbType(DbType dbType)
    {
      _dbType = dbType;
      switch (_dbType)
      {
        case DbType.Guid:
          _mySqlDbType = MySqlDbType.Guid;
          break;

        case DbType.AnsiString:
        case DbType.String:
          _mySqlDbType = MySqlDbType.VarChar;
          break;

        case DbType.AnsiStringFixedLength:
        case DbType.StringFixedLength:
          _mySqlDbType = MySqlDbType.String;
          break;

        case DbType.Boolean:
        case DbType.Byte:
          _mySqlDbType = MySqlDbType.UByte;
          break;

        case DbType.SByte:
          _mySqlDbType = MySqlDbType.Byte;
          break;

        case DbType.Date:
          _mySqlDbType = MySqlDbType.Date;
          break;
        case DbType.DateTime:
          _mySqlDbType = MySqlDbType.DateTime;
          break;

        case DbType.Time:
          _mySqlDbType = MySqlDbType.Time;
          break;
        case DbType.Single:
          _mySqlDbType = MySqlDbType.Float;
          break;
        case DbType.Double:
          _mySqlDbType = MySqlDbType.Double;
          break;

        case DbType.Int16:
          _mySqlDbType = MySqlDbType.Int16;
          break;
        case DbType.UInt16:
          _mySqlDbType = MySqlDbType.UInt16;
          break;

        case DbType.Int32:
          _mySqlDbType = MySqlDbType.Int32;
          break;
        case DbType.UInt32:
          _mySqlDbType = MySqlDbType.UInt32;
          break;

        case DbType.Int64:
          _mySqlDbType = MySqlDbType.Int64;
          break;
        case DbType.UInt64:
          _mySqlDbType = MySqlDbType.UInt64;
          break;

        case DbType.Decimal:
        case DbType.Currency:
          _mySqlDbType = MySqlDbType.Decimal;
          break;

        case DbType.Object:
        case DbType.VarNumeric:
        case DbType.Binary:
        default:
          _mySqlDbType = MySqlDbType.Blob;
          break;
      }

      if (_dbType == DbType.Object)
      {
        var value = this._paramValue as byte[];
        if (value != null && value.Length == GEOMETRY_LENGTH)
          _mySqlDbType = MySqlDbType.Geometry;
      }

      ValueObject = MySqlField.GetIMySqlValue(_mySqlDbType);
    }
  }

  internal class MySqlParameterConverter : TypeConverter
  {

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
#if !NETCORE10
      if (destinationType == typeof(InstanceDescriptor))
      {
        return true;
      }
#endif

      // Always call the base to see if it can perform the conversion.
      return base.CanConvertTo(context, destinationType);
    }

    public override object ConvertTo(ITypeDescriptorContext context,
                                     CultureInfo culture, object value, Type destinationType)
    {
#if !NETCORE10
      if (destinationType == typeof(InstanceDescriptor))
      {
        ConstructorInfo ci = typeof(MySqlParameter).GetConstructor(
            new[]
                            {
                                typeof (string), typeof (MySqlDbType), typeof (int), typeof (ParameterDirection),
                                typeof (bool), typeof (byte), typeof (byte), typeof (string),
                                typeof (object)
                            });
        MySqlParameter p = (MySqlParameter)value;
        return new InstanceDescriptor(ci, new object[]
                                                          {
                                                              p.ParameterName, p.DbType, p.Size, p.Direction,
                                                              p.IsNullable, p.Precision,
                                                              p.Scale, p.SourceColumn, p.Value
                                                          });
      }
#endif

      // Always call base, even if you can't convert.
      return base.ConvertTo(context, culture, value, destinationType);
    }
  }
}
