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

using System;
using System.Globalization;
using MySql.Data.MySqlClient;
using MySql.Data.Common;

namespace MySql.Data.Types
{

  /// <summary>
  /// Represents a datetime data type object in a MySql database.
  /// </summary>
  [Serializable]
  public struct MySqlDateTime : IMySqlValue, IComparable, IConvertible
  {
    private readonly MySqlDbType _type;
    private int _millisecond, _microsecond;

    /// <summary>
    /// Defines whether the UTF or local timezone will be used. 
    /// </summary>
    public int TimezoneOffset;

    /// <summary>
    /// Constructs a new <b>MySqlDateTime</b> object by setting the individual time properties to
    /// the given values.
    /// </summary>
    /// <param name="year">The year to use.</param>
    /// <param name="month">The month to use.</param>
    /// <param name="day">The day to use.</param>
    /// <param name="hour">The hour to use.</param>
    /// <param name="minute">The minute to use.</param>
    /// <param name="second">The second to use.</param>
    /// <param name="microsecond">The microsecond to use.</param>
    public MySqlDateTime(int year, int month, int day, int hour, int minute, int second, int microsecond)
      : this(MySqlDbType.DateTime, year, month, day, hour, minute, second, microsecond)
    {
    }

    /// <summary>
    /// Constructs a new <b>MySqlDateTime</b> object by using values from the given <see cref="DateTime"/> object.
    /// </summary>
    /// <param name="dt">The <see cref="DateTime"/> object to copy.</param>
    public MySqlDateTime(DateTime dt)
      : this(MySqlDbType.DateTime, dt)
    {
    }

    /// <summary>
    /// Constructs a new <b>MySqlDateTime</b> object by copying the current value of the given object.
    /// </summary>
    /// <param name="mdt">The <b>MySqlDateTime</b> object to copy.</param>
    public MySqlDateTime(MySqlDateTime mdt)
    {
      Year = mdt.Year;
      Month = mdt.Month;
      Day = mdt.Day;
      Hour = mdt.Hour;
      Minute = mdt.Minute;
      Second = mdt.Second;
      _microsecond = 0;
      _millisecond = 0;
      _type = MySqlDbType.DateTime;
      IsNull = false;
      TimezoneOffset = 0;
    }

    /// <summary>
    /// Enables the contruction of a <b>MySqlDateTime</b> object by parsing a string.
    /// </summary>
    public MySqlDateTime(string dateTime)
      : this(Parse(dateTime))
    {
    }

    internal MySqlDateTime(MySqlDbType type, int year, int month, int day, int hour, int minute,
      int second, int microsecond)
    {
      this.IsNull = false;
      this._type = type;
      this.Year = year;
      this.Month = month;
      this.Day = day;
      this.Hour = hour;
      this.Minute = minute;
      this.Second = second;
      this._microsecond = microsecond;
      this._millisecond = this._microsecond / 1000;
      this.TimezoneOffset = 0;
    }

    internal MySqlDateTime(MySqlDbType type, bool isNull)
      : this(type, 0, 0, 0, 0, 0, 0, 0)
    {
      this.IsNull = isNull;
    }

    internal MySqlDateTime(MySqlDbType type, DateTime val)
      : this(type, 0, 0, 0, 0, 0, 0, 0)
    {
      this.IsNull = false;
      Year = val.Year;
      Month = val.Month;
      Day = val.Day;
      Hour = val.Hour;
      Minute = val.Minute;
      Second = val.Second;
      Microsecond = (int)(val.Ticks % 10000000) / 10;
    }

    #region Properties

    /// <summary>
    /// Indicates if this object contains a value that can be represented as a DateTime
    /// </summary>
    public bool IsValidDateTime => Year != 0 && Month != 0 && Day != 0;

    /// <summary>Returns the year portion of this datetime</summary>
    public int Year { get; set; }

    /// <summary>Returns the month portion of this datetime</summary>
    public int Month { get; set; }

    /// <summary>Returns the day portion of this datetime</summary>
    public int Day { get; set; }

    /// <summary>Returns the hour portion of this datetime</summary>
    public int Hour { get; set; }

    /// <summary>Returns the minute portion of this datetime</summary>
    public int Minute { get; set; }

    /// <summary>Returns the second portion of this datetime</summary>
    public int Second { get; set; }

    /// <summary>
    /// Returns the milliseconds portion of this datetime 
    /// expressed as a value between 0 and 999
    /// </summary>
    public int Millisecond
    {
      get { return _millisecond; }
      set
      {
        if (value < 0 || value > 999)
          throw new ArgumentOutOfRangeException("Millisecond", Resources.InvalidMillisecondValue);
        _millisecond = value;
        _microsecond = value * 1000;
      }
    }

    /// <summary>
    /// Returns the microseconds portion of this datetime (6 digit precision)
    /// </summary>
    public int Microsecond
    {
      get { return _microsecond; }
      set
      {
        if (value < 0 || value > 999999)
          throw new ArgumentOutOfRangeException("Microsecond", Resources.InvalidMicrosecondValue);
        _microsecond = value;
        _millisecond = value / 1000;
      }
    }

    #endregion

    #region IMySqlValue Members

    /// <summary>
    /// Returns true if this datetime object has a null value
    /// </summary>
    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => _type;

    object IMySqlValue.Value => GetDateTime();

    /// <summary>
    /// Retrieves the value of this <see cref="MySqlDateTime"/> as a DateTime object.
    /// </summary>
    public DateTime Value => GetDateTime();

    Type IMySqlValue.SystemType => typeof(DateTime);

    string IMySqlValue.MySqlTypeName
    {
      get
      {
        switch (_type)
        {
          case MySqlDbType.Date: return "DATE";
          case MySqlDbType.Newdate: return "NEWDATE";
          case MySqlDbType.Timestamp: return "TIMESTAMP";
        }
        return "DATETIME";
      }
    }


    private void SerializeText(MySqlPacket packet, MySqlDateTime value)
    {
      var val = String.Format("{0:0000}-{1:00}-{2:00}",
        value.Year, value.Month, value.Day);
      if (_type != MySqlDbType.Date)
      {
        val = value.Microsecond > 0 ?
          $"{val} {value.Hour:00}:{value.Minute:00}:{value.Second:00}.{value.Microsecond:000000}"
          : $"{val} {value.Hour:00}:{value.Minute:00}:{value.Second:00}";
      }

      packet.WriteStringNoNull("'" + val + "'");
    }

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object value, int length)
    {
      MySqlDateTime dtValue;

      string valueAsString = value as string;

      if (value is DateTime)
        dtValue = new MySqlDateTime(_type, (DateTime)value);
      else if (valueAsString != null)
        dtValue = MySqlDateTime.Parse(valueAsString);
      else if (value is MySqlDateTime)
        dtValue = (MySqlDateTime)value;
      else
        throw new MySqlException("Unable to serialize date/time value.");

      if (!binary)
      {
        SerializeText(packet, dtValue);
        return;
      }

      if (dtValue.Microsecond > 0)
        packet.WriteByte(11);
      else
        packet.WriteByte(7);

      packet.WriteInteger(dtValue.Year, 2);
      packet.WriteByte((byte)dtValue.Month);
      packet.WriteByte((byte)dtValue.Day);
      if (_type == MySqlDbType.Date)
      {
        packet.WriteByte(0);
        packet.WriteByte(0);
        packet.WriteByte(0);
      }
      else
      {
        packet.WriteByte((byte)dtValue.Hour);
        packet.WriteByte((byte)dtValue.Minute);
        packet.WriteByte((byte)dtValue.Second);
      }

      if (dtValue.Microsecond > 0)
      {
        long val = dtValue.Microsecond;
        for (int x = 0; x < 4; x++)
        {
          packet.WriteByte((byte)(val & 0xff));
          val >>= 8;
        }
      }
    }

    internal static MySqlDateTime Parse(string s)
    {
      MySqlDateTime dt = new MySqlDateTime();
      return dt.ParseMySql(s);
    }

    internal static MySqlDateTime Parse(string s, DBVersion version)
    {
      MySqlDateTime dt = new MySqlDateTime();
      return dt.ParseMySql(s);
    }

    private MySqlDateTime ParseMySql(string s)
    {
      string[] parts = s.Split('-', ' ', ':', '/', '.');

      int year = int.Parse(parts[0], CultureInfo.InvariantCulture);
      int month = int.Parse(parts[1], CultureInfo.InvariantCulture);
      int day = int.Parse(parts[2], CultureInfo.InvariantCulture);

      int hour = 0, minute = 0, second = 0, microsecond = 0;
      if (parts.Length > 3)
      {
        hour = int.Parse(parts[3], CultureInfo.InvariantCulture);
        minute = int.Parse(parts[4], CultureInfo.InvariantCulture);
        second = int.Parse(parts[5], CultureInfo.InvariantCulture);
      }

      if (parts.Length > 6)
      {
        microsecond = int.Parse(parts[6].PadRight(6, '0'), CultureInfo.InvariantCulture);
      }

      return new MySqlDateTime(_type, year, month, day, hour, minute, second, microsecond);
    }

    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {

      if (nullVal) return new MySqlDateTime(_type, true);

      if (length >= 0)
      {
        string value = packet.ReadString(length);
        return ParseMySql(value);
      }

      long bufLength = packet.ReadByte();
      int year = 0, month = 0, day = 0;
      int hour = 0, minute = 0, second = 0, microsecond = 0;
      if (bufLength >= 4)
      {
        year = packet.ReadInteger(2);
        month = packet.ReadByte();
        day = packet.ReadByte();
      }

      if (bufLength > 4)
      {
        hour = packet.ReadByte();
        minute = packet.ReadByte();
        second = packet.ReadByte();
      }

      if (bufLength > 7)
      {
        microsecond = packet.Read3ByteInt();
        packet.ReadByte();
      }

      return new MySqlDateTime(_type, year, month, day, hour, minute, second, microsecond);
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      int len = packet.ReadByte();
      packet.Position += len;
    }

    #endregion

    /// <summary>Returns this value as a DateTime</summary>
    public DateTime GetDateTime()
    {
      if (!IsValidDateTime)
        throw new MySqlConversionException("Unable to convert MySQL date/time value to System.DateTime");

      DateTimeKind kind = DateTimeKind.Unspecified;
      if (_type == MySqlDbType.Timestamp)
      {
        if (TimezoneOffset == 0)
          kind = DateTimeKind.Utc;
        else
          kind = DateTimeKind.Local;
      }

      return new DateTime(Year, Month, Day, Hour, Minute, Second, kind).AddTicks(_microsecond * 10);
    }

    private static string FormatDateCustom(string format, int monthVal, int dayVal, int yearVal)
    {
      format = format.Replace("MM", "{0:00}");
      format = format.Replace("M", "{0}");
      format = format.Replace("dd", "{1:00}");
      format = format.Replace("d", "{1}");
      format = format.Replace("yyyy", "{2:0000}");
      format = format.Replace("yy", "{3:00}");
      format = format.Replace("y", "{4:0}");

      int year2digit = yearVal - ((yearVal / 1000) * 1000);
      year2digit -= ((year2digit / 100) * 100);
      int year1digit = year2digit - ((year2digit / 10) * 10);

      return String.Format(format, monthVal, dayVal, yearVal, year2digit, year1digit);
    }

    /// <summary>Returns a MySQL specific string representation of this value</summary>
    public override string ToString()
    {
      if (this.IsValidDateTime)
      {
        DateTime d = new DateTime(Year, Month, Day, Hour, Minute, Second).AddTicks(_microsecond * 10);
        return (_type == MySqlDbType.Date) ? d.ToString("d", CultureInfo.InvariantCulture) : d.ToString(CultureInfo.InvariantCulture);
      }

      string dateString = FormatDateCustom(
          CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern, Month, Day, Year);
      if (_type == MySqlDbType.Date)
        return dateString;

      DateTime dt = new DateTime(1, 2, 3, Hour, Minute, Second).AddTicks(_microsecond * 10);
      dateString = String.Format("{0} {1}", dateString, dt.ToLongTimeString());

      return dateString;
    }

    /// <summary></summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public static explicit operator DateTime(MySqlDateTime val)
    {
      if (!val.IsValidDateTime) return DateTime.MinValue;
      return val.GetDateTime();
    }

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      string[] types = new string[] { "DATE", "DATETIME", "TIMESTAMP" };
      MySqlDbType[] dbtype = new MySqlDbType[] { MySqlDbType.Date,
        MySqlDbType.DateTime, MySqlDbType.Timestamp };

      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      for (int x = 0; x < types.Length; x++)
      {
        MySqlSchemaRow row = sc.AddRow();
        row["TypeName"] = types[x];
        row["ProviderDbType"] = dbtype[x];
        row["ColumnSize"] = 0;
        row["CreateFormat"] = types[x];
        row["CreateParameters"] = null;
        row["DataType"] = "System.DateTime";
        row["IsAutoincrementable"] = false;
        row["IsBestMatch"] = true;
        row["IsCaseSensitive"] = false;
        row["IsFixedLength"] = true;
        row["IsFixedPrecisionScale"] = true;
        row["IsLong"] = false;
        row["IsNullable"] = true;
        row["IsSearchable"] = true;
        row["IsSearchableWithLike"] = false;
        row["IsUnsigned"] = false;
        row["MaximumScale"] = 0;
        row["MinimumScale"] = 0;
        row["IsConcurrencyType"] = DBNull.Value;
        row["IsLiteralSupported"] = false;
        row["LiteralPrefix"] = null;
        row["LiteralSuffix"] = null;
        row["NativeDataType"] = null;
      }
    }

    #region IComparable Members

    int IComparable.CompareTo(object obj)
    {
      MySqlDateTime otherDate = (MySqlDateTime)obj;

      if (Year < otherDate.Year) return -1;
      else if (Year > otherDate.Year) return 1;

      if (Month < otherDate.Month) return -1;
      else if (Month > otherDate.Month) return 1;

      if (Day < otherDate.Day) return -1;
      else if (Day > otherDate.Day) return 1;

      if (Hour < otherDate.Hour) return -1;
      else if (Hour > otherDate.Hour) return 1;

      if (Minute < otherDate.Minute) return -1;
      else if (Minute > otherDate.Minute) return 1;

      if (Second < otherDate.Second) return -1;
      else if (Second > otherDate.Second) return 1;

      if (Microsecond < otherDate.Microsecond) return -1;
      else if (Microsecond > otherDate.Microsecond) return 1;

      return 0;
    }

    #endregion

    #region IConvertible Members

    ulong IConvertible.ToUInt64(IFormatProvider provider)
    {
      return 0;
    }

    sbyte IConvertible.ToSByte(IFormatProvider provider)
    {
      // TODO:  Add MySqlDateTime.ToSByte implementation
      return 0;
    }

    double IConvertible.ToDouble(IFormatProvider provider)
    {
      return 0;
    }

    DateTime IConvertible.ToDateTime(IFormatProvider provider)
    {
      return GetDateTime();
    }

    float IConvertible.ToSingle(IFormatProvider provider)
    {
      return 0;
    }

    bool IConvertible.ToBoolean(IFormatProvider provider)
    {
      return false;
    }

    int IConvertible.ToInt32(IFormatProvider provider)
    {
      return 0;
    }

    ushort IConvertible.ToUInt16(IFormatProvider provider)
    {
      return 0;
    }

    short IConvertible.ToInt16(IFormatProvider provider)
    {
      return 0;
    }

    string System.IConvertible.ToString(IFormatProvider provider)
    {
      return null;
    }

    byte IConvertible.ToByte(IFormatProvider provider)
    {
      return 0;
    }

    char IConvertible.ToChar(IFormatProvider provider)
    {
      return '\0';
    }

    long IConvertible.ToInt64(IFormatProvider provider)
    {
      return 0;
    }

    System.TypeCode IConvertible.GetTypeCode()
    {
      return new System.TypeCode();
    }

    decimal IConvertible.ToDecimal(IFormatProvider provider)
    {
      return 0;
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider provider)
    {
      return null;
    }

    uint IConvertible.ToUInt32(IFormatProvider provider)
    {
      return 0;
    }

    #endregion

  }
}
