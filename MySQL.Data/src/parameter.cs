// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.Types;
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Represents a parameter to a <see cref="MySqlCommand"/>, This class cannot be inherited.
  /// </summary>
  [TypeConverter(typeof(MySqlParameterConverter))]
  public sealed partial class MySqlParameter : DbParameter, IDbDataParameter
  {
    private const int UNSIGNED_MASK = 0x8000;
    private object _paramValue;
    private string _paramName;
    private MySqlDbType _mySqlDbType;
    private bool _inferType = true;
    private const int GEOMETRY_LENGTH = 25;
    private DbType _dbType;

    #region Constructors

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
      SourceVersion = DataRowVersion.Default;
    }

    public MySqlParameter()
    {
      SourceVersion = DataRowVersion.Default;
      Direction = ParameterDirection.Input;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name and a value of the new MySqlParameter.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to map. </param>
    /// <param name="value">An <see cref="Object"/> that is the value of the <see cref="MySqlParameter"/>. </param>
    public MySqlParameter(string parameterName, object value) : this()
    {
      ParameterName = parameterName;
      Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name and the data type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to map. </param>
    /// <param name="dbType">One of the <see cref="MySqlDbType"/> values. </param>
    public MySqlParameter(string parameterName, MySqlDbType dbType) : this(parameterName, null)
    {
      MySqlDbType = dbType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name, the <see cref="MySqlDbType"/>, and the size.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to map. </param>
    /// <param name="dbType">One of the <see cref="MySqlDbType"/> values. </param>
    /// <param name="size">The length of the parameter. </param>
    public MySqlParameter(string parameterName, MySqlDbType dbType, int size) : this(parameterName, dbType)
    {
      Size = size;
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
    public MySqlParameter(string parameterName, MySqlDbType dbType, int size, ParameterDirection direction,
                          bool isNullable, byte precision, byte scale, string sourceColumn,
                          DataRowVersion sourceVersion, object value)
      : this(parameterName, dbType, size, sourceColumn)
    {
      Direction = direction;
      IsNullable = isNullable;
      Precision = precision;
      Scale = scale;
      Value = value;
      SourceVersion = sourceVersion;
    }

    internal MySqlParameter(string name, MySqlDbType type, ParameterDirection dir, string col, DataRowVersion sourceVersion, object val)
      : this(name, type)
    {
      Direction = dir;
      SourceColumn = col;
      Value = val;
      SourceVersion = sourceVersion;
    }

    #endregion

    #region Properties

    [Category("Misc")]
    public override String ParameterName
    {
      get { return _paramName; }
      set { SetParameterName(value); }
    }

    internal MySqlParameterCollection Collection { get; set; }
    internal Encoding Encoding { get; set; }

    internal bool TypeHasBeenSet => _inferType == false;


    internal string BaseName
    {
      get
      {
        if (ParameterName.StartsWith("@", StringComparison.Ordinal) || ParameterName.StartsWith("?", StringComparison.Ordinal))
          return ParameterName.Substring(1);
        return ParameterName;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
    /// As of MySql version 4.1 and earlier, input-only is the only valid choice.
    /// </summary>
    [Category("Data")]
    public override ParameterDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter accepts null values.
    /// </summary>
    [Browsable(false)]
    public override Boolean IsNullable { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="MySqlParameter.MySqlDbType"/> of the parameter.
    /// </summary>
    [Category("Data")]
    [DbProviderSpecificTypeProperty(true)]
    public MySqlDbType MySqlDbType
    {
      get { return _mySqlDbType; }
      set
      {
        SetMySqlDbType(value);
        _inferType = false;
      }
    }

    /// <summary>
    /// Gets or sets the maximum number of digits used to represent the <see cref="Value"/> property.
    /// </summary>
    [Category("Data")]
    public override byte Precision { get; set; }

    /// <summary>
    /// Gets or sets the number of decimal places to which <see cref="Value"/> is resolved.
    /// </summary>
    [Category("Data")]
    public override byte Scale { get; set; }

    /// <summary>
    /// Gets or sets the maximum size, in bytes, of the data within the column.
    /// </summary>
    [Category("Data")]
    public override int Size { get; set; }

    /// <summary>
    /// Gets or sets the value of the parameter.
    /// </summary>
    [TypeConverter(typeof(StringConverter))]
    [Category("Data")]
    public override object Value
    {
      get { return _paramValue; }
      set
      {
        _paramValue = value;
        byte[] valueAsByte = value as byte[];
        string valueAsString = value as string;

        if (valueAsByte != null)
          Size = valueAsByte.Length;
        else if (valueAsString != null)
          Size = valueAsString.Length;
        if (_inferType)
          SetTypeFromValue();
      }
    }

    internal IMySqlValue ValueObject { get; private set; }

    /// <summary>
    /// Returns the possible values for this parameter if this parameter is of type
    /// SET or ENUM.  Returns null otherwise.
    /// </summary>
    public IList PossibleValues { get; internal set; }

    /// <summary>
    /// Gets or sets the name of the source column that is mapped to the <see cref="System.Data.DataSet"/> and used for loading or returning the <see cref="MySqlParameter.Value"/>.
    /// </summary>
    [Category("Data")]
    public override String SourceColumn { get; set; }

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

    #endregion

    private void SetParameterName(string name)
    {
      Collection?.ParameterNameChanged(this, _paramName, name);
      _paramName = name;
    }

    /// <summary>
    /// Overridden. Gets a string containing the <see cref="ParameterName"/>.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => _paramName;

    internal int GetPSType()
    {
      switch (_mySqlDbType)
      {
        case MySqlDbType.Bit:
          return (int)MySqlDbType.Int64 | UNSIGNED_MASK;
        case MySqlDbType.UByte:
          return (int)MySqlDbType.Byte | UNSIGNED_MASK;
        case MySqlDbType.UInt64:
          return (int)MySqlDbType.Int64 | UNSIGNED_MASK;
        case MySqlDbType.UInt32:
          return (int)MySqlDbType.Int32 | UNSIGNED_MASK;
        case MySqlDbType.UInt24:
          return (int)MySqlDbType.Int32 | UNSIGNED_MASK;
        case MySqlDbType.UInt16:
          return (int)MySqlDbType.Int16 | UNSIGNED_MASK;
        case MySqlDbType.Guid:
          return (int)MySqlDbType.Guid - 600;
        default:
          int value = (int)_mySqlDbType;
          value = value > 255 ? value - 500 : value;
          return value;
      }
    }

    internal void Serialize(MySqlPacket packet, bool binary, MySqlConnectionStringBuilder settings)
    {
      if (!binary && (_paramValue == null || _paramValue == DBNull.Value))
        packet.WriteStringNoNull("NULL");
      else
      {
        if (ValueObject.MySqlDbType == MySqlDbType.Guid)
        {
          MySqlGuid g = (MySqlGuid)ValueObject;
          g.OldGuids = settings.OldGuids;
          ValueObject = g;
        }
        if (ValueObject.MySqlDbType == MySqlDbType.Geometry)
        {
          MySqlGeometry v = (MySqlGeometry)ValueObject;
          if (v.IsNull && Value != null)
          {
            MySqlGeometry.TryParse(Value.ToString(), out v);
          }
          ValueObject = v;
        }
        ValueObject.WriteValue(packet, binary, _paramValue, Size);
      }
    }

    private void SetMySqlDbType(MySqlDbType mysqlDbtype)
    {
      _mySqlDbType = mysqlDbtype;
      ValueObject = MySqlField.GetIMySqlValue(_mySqlDbType);
      SetDbTypeFromMySqlDbType();
    }

    private void SetTypeFromValue()
    {
      if (_paramValue == null || _paramValue == DBNull.Value) return;

      if (_paramValue is Guid)
        MySqlDbType = MySqlDbType.Guid;
      else if (_paramValue is TimeSpan)
        MySqlDbType = MySqlDbType.Time;
      else if (_paramValue is bool)
        MySqlDbType = MySqlDbType.Byte;
      else
      {
        Type t = _paramValue.GetType();
        switch (t.Name)
        {
          case "SByte": MySqlDbType = MySqlDbType.Byte; break;
          case "Byte": MySqlDbType = MySqlDbType.UByte; break;
          case "Int16": MySqlDbType = MySqlDbType.Int16; break;
          case "UInt16": MySqlDbType = MySqlDbType.UInt16; break;
          case "Int32": MySqlDbType = MySqlDbType.Int32; break;
          case "UInt32": MySqlDbType = MySqlDbType.UInt32; break;
          case "Int64": MySqlDbType = MySqlDbType.Int64; break;
          case "UInt64": MySqlDbType = MySqlDbType.UInt64; break;
          case "DateTime": MySqlDbType = MySqlDbType.DateTime; break;
          case "String": MySqlDbType = MySqlDbType.VarChar; break;
          case "Single": MySqlDbType = MySqlDbType.Float; break;
          case "Double": MySqlDbType = MySqlDbType.Double; break;

          case "Decimal": MySqlDbType = MySqlDbType.Decimal; break;
          case "Object":
          default:
            if (t.GetTypeInfo().BaseType == typeof(Enum))
              MySqlDbType = MySqlDbType.Int32;
            else
              MySqlDbType = MySqlDbType.Blob;
            break;
        }
      }
    }

    // this method is pretty dumb but we want it to be fast.  it doesn't return size based
    // on value and type but just on the value.
    internal long EstimatedSize()
    {
      if (Value == null || Value == DBNull.Value)
        return 4; // size of NULL
      if (Value is byte[])
        return ((byte[])Value).Length;
      if (Value is string)
        return ((string)Value).Length * 4; // account for UTF-8 (yeah I know)
      if (Value is decimal || Value is float)
        return 64;
      return 32;
    }

    /// <summary>
    /// Resets the <b>DbType</b> property to its original settings. 
    /// </summary>
    public override void ResetDbType()
    {
      _inferType = true;
    }


    void SetDbTypeFromMySqlDbType()
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
      if (destinationType == typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor))
      {
        return true;
      }

      // Always call the base to see if it can perform the conversion.
      return base.CanConvertTo(context, destinationType);
    }

    public override object ConvertTo(ITypeDescriptorContext context,
                                     CultureInfo culture, object value, Type destinationType)
    {
      if (destinationType == typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor))
      {
        ConstructorInfo ci = typeof(MySqlParameter).GetConstructor(
            new[]
                            {
                                typeof (string), typeof (MySqlDbType), typeof (int), typeof (ParameterDirection),
                                typeof (bool), typeof (byte), typeof (byte), typeof (string),
                                typeof (object)
                            });
        MySqlParameter p = (MySqlParameter)value;
        return new System.ComponentModel.Design.Serialization.InstanceDescriptor(ci, new object[]
                                                          {
                                                              p.ParameterName, p.DbType, p.Size, p.Direction,
                                                              p.IsNullable, p.Precision,
                                                              p.Scale, p.SourceColumn, p.Value
                                                          });
      }

      // Always call base, even if you can't convert.
      return base.ConvertTo(context, culture, value, destinationType);
    }

  }
}
