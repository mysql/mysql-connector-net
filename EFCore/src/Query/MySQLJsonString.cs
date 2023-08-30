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

using System;

namespace MySql.EntityFrameworkCore.Query
{
  /// <summary>
  ///   This class can be used to represent a string that contains valid JSON data. 
  ///   To mark a string as containing JSON data, just cast the string to `MySQLJsonString`.
  /// </summary>

  public sealed class MySQLJsonString : IConvertible
  {
    private readonly string _json;

    private MySQLJsonString(string json)
      => _json = json;

    public static implicit operator string(MySQLJsonString jsonStringObject)
      => jsonStringObject._json;

    public static implicit operator MySQLJsonString(string stringObject)
      => new MySQLJsonString(stringObject);

    public static bool operator ==(MySQLJsonString left, MySQLJsonString right)
      => left?.Equals(right) ?? ReferenceEquals(right, null);

    public static bool operator !=(MySQLJsonString left, MySQLJsonString right)
      => !(left == right);

    public static bool operator ==(MySQLJsonString left, string right)
      => left?.Equals(right) ?? ReferenceEquals(right, null);

    public static bool operator !=(MySQLJsonString left, string right)
      => !(left == right);

    private bool Equals(MySQLJsonString other)
      => _json == other._json;

    private bool Equals(string other)
      => _json == other;

    public override bool Equals(object? obj)
      => ReferenceEquals(this, obj) ||
         obj is MySQLJsonString other && Equals(other) ||
         obj is string otherString && Equals(otherString);

    public override int GetHashCode()
      => HashCode.Combine(_json);

    public TypeCode GetTypeCode()
      => TypeCode.Object;

    public bool ToBoolean(IFormatProvider? provider)
      => Convert.ToBoolean(_json, provider);

    public byte ToByte(IFormatProvider? provider)
      => Convert.ToByte(_json, provider);

    public char ToChar(IFormatProvider? provider)
      => Convert.ToChar(_json, provider);

    public DateTime ToDateTime(IFormatProvider? provider)
      => Convert.ToDateTime(_json, provider);

    public decimal ToDecimal(IFormatProvider? provider)
      => Convert.ToDecimal(_json, provider);

    public double ToDouble(IFormatProvider? provider)
      => Convert.ToDouble(_json, provider);

    public short ToInt16(IFormatProvider? provider)
      => Convert.ToInt16(_json, provider);

    public int ToInt32(IFormatProvider? provider)
      => Convert.ToInt32(_json, provider);

    public long ToInt64(IFormatProvider? provider)
      => Convert.ToInt64(_json, provider);

    public sbyte ToSByte(IFormatProvider? provider)
      => Convert.ToSByte(_json, provider);

    public float ToSingle(IFormatProvider? provider)
      => Convert.ToSingle(_json, provider);

    public string ToString(IFormatProvider? provider)
      => Convert.ToString(_json, provider);

    public object ToType(Type conversionType, IFormatProvider? provider)
      => Convert.ChangeType(_json, conversionType, provider);

    public ushort ToUInt16(IFormatProvider? provider)
      => Convert.ToUInt16(_json, provider);

    public uint ToUInt32(IFormatProvider? provider)
      => Convert.ToUInt32(_json, provider);

    public ulong ToUInt64(IFormatProvider? provider)
      => Convert.ToUInt64(_json, provider);
  }
}
