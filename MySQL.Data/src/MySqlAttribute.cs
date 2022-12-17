// Copyright (c) 2021, 2022, Oracle and/or its affiliates.
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
using System.Reflection;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Represents a query attribute to a <see cref="MySqlCommand"/>.
  /// </summary>
  public class MySqlAttribute : ICloneable
  {
    private const int UNSIGNED_MASK = 0x8000;
    private string _attributeName;
    private object _attributeValue;
    private MySqlDbType _mySqlDbType;
    private int _size;

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlAttribute"/> class.
    /// </summary>
    public MySqlAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlAttribute"/> class with the attribute name and its value.
    /// </summary>
    /// <param name="attributeName">Name of the attribute.</param>
    /// <param name="attributeValue">Value of the attribute.</param>
    public MySqlAttribute(string attributeName, object attributeValue)
    {
      AttributeName = attributeName;
      Value = attributeValue;
    }
    #endregion

    #region Properties
    internal MySqlAttributeCollection Collection { get; set; }

    /// <summary>
    /// Name of the query attribute.
    /// </summary>
    public String AttributeName
    {
      get { return _attributeName; }
      set
      {
        if (String.IsNullOrWhiteSpace(value))
          throw new ArgumentException("'AttributeName' property can not be null or empty string.", "AttributeName");
        _attributeName = value;
      }
    }

    /// <summary>
    /// Value of the query attribute.
    /// </summary>
    public object Value
    {
      get { return _attributeValue; }
      set
      {
        _attributeValue = value;

        if (value is byte[] valueAsByte)
          _size = valueAsByte.Length;
        else if (value is string valueAsString)
          _size = valueAsString.Length;

        SetTypeFromValue();
      }
    }

    /// <summary>
    /// Gets or sets the <see cref="MySqlDbType"/> of the attribute.
    /// </summary>
    public MySqlDbType MySqlDbType
    {
      get { return _mySqlDbType; }
      set
      {
        SetMySqlDbType(value);
      }
    }

    internal IMySqlValue ValueObject { get; private set; }
    #endregion

    /// <summary>
    /// Sets the MySqlDbType from the Value
    /// </summary>
    private void SetTypeFromValue()
    {
      if (_attributeValue == null || _attributeValue == DBNull.Value) return;

      if (_attributeValue is Guid)
        MySqlDbType = MySqlDbType.Guid;
      else if (_attributeValue is TimeSpan)
        MySqlDbType = MySqlDbType.Time;
      else if (_attributeValue is bool)
        MySqlDbType = MySqlDbType.Byte;
      else
      {
        Type t = _attributeValue.GetType();
        if (t.GetTypeInfo().BaseType == typeof(Enum))
          t = t.GetTypeInfo().GetEnumUnderlyingType();
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
          case "MySqlGeometry": MySqlDbType = MySqlDbType.Geometry; break;
          case "Decimal": MySqlDbType = MySqlDbType.Decimal; break;
          case "Object":
          default:
            MySqlDbType = MySqlDbType.Blob;
            break;
        }
      }
    }

    private void SetMySqlDbType(MySqlDbType mysqlDbtype)
    {
      _mySqlDbType = mysqlDbtype == MySqlDbType.JSON ? MySqlDbType.VarChar : mysqlDbtype;
      ValueObject = MySqlField.GetIMySqlValue(_mySqlDbType);
    }

    /// <summary>
    /// Gets the value for the attribute type.
    /// </summary>
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

    /// <summary>
    /// Serialize the value of the query attribute.
    /// </summary>
    internal async Task SerializeAsync(MySqlPacket packet, bool binary, MySqlConnectionStringBuilder settings, bool execAsync)
    {
      if (!binary && (_attributeValue == null || _attributeValue == DBNull.Value))
        await packet.WriteStringNoNullAsync("NULL", execAsync).ConfigureAwait(false);
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
        await ValueObject.WriteValueAsync(packet, binary, _attributeValue, _size, execAsync).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Clones this object.
    /// </summary>
    /// <returns>An object that is a clone of this object.</returns>
    public MySqlAttribute Clone()
    {
      MySqlAttribute clone = new MySqlAttribute(_attributeName, _attributeValue);
      return clone;
    }

    object ICloneable.Clone()
    {
      return this.Clone();
    }
  }
}
