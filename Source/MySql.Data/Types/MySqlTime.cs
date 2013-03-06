// Copyright (c) 2004-2008 MySQL AB, 2008-2009 Sun Microsystems, Inc.
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
using System.Data;
using MySql.Data.MySqlClient;

namespace MySql.Data.Types
{
  internal struct MySqlTimeSpan : IMySqlValue
  {
    private TimeSpan mValue;
    private bool isNull;

    public MySqlTimeSpan(bool isNull)
    {
      this.isNull = isNull;
      mValue = TimeSpan.MinValue;
    }

    public MySqlTimeSpan(TimeSpan val)
    {
      this.isNull = false;
      mValue = val;
    }

    #region IMySqlValue Members

    public bool IsNull
    {
      get { return isNull; }
    }

    MySqlDbType IMySqlValue.MySqlDbType
    {
      get { return MySqlDbType.Time; }
    }

    DbType IMySqlValue.DbType
    {
      get { return DbType.Time; }
    }

    object IMySqlValue.Value
    {
      get { return mValue; }
    }

    public TimeSpan Value
    {
      get { return mValue; }
    }

    Type IMySqlValue.SystemType
    {
      get { return typeof(TimeSpan); }
    }

    string IMySqlValue.MySqlTypeName
    {
      get { return "TIME"; }
    }

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      if (!(val is TimeSpan))
        throw new MySqlException("Only TimeSpan objects can be serialized by MySqlTimeSpan");

      TimeSpan ts = (TimeSpan)val;
      bool negative = ts.TotalMilliseconds < 0;
      ts = ts.Duration();

      if (binary)
      {
        if (ts.Milliseconds > 0)
          packet.WriteByte(12);
        else
          packet.WriteByte(8);

        packet.WriteByte((byte)(negative ? 1 : 0));        
        packet.WriteInteger(ts.Days, 4);
        packet.WriteByte((byte)ts.Hours);
        packet.WriteByte((byte)ts.Minutes);
        packet.WriteByte((byte)ts.Seconds);
        if (ts.Milliseconds > 0)
        {
          long mval = ts.Milliseconds*1000;
          packet.WriteInteger(mval, 4);          
        }
      }
      else
      {
        String s = String.Format("'{0}{1} {2:00}:{3:00}:{4:00}.{5}'",
            negative ? "-" : "", ts.Days, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
			
        packet.WriteStringNoNull(s);
      }
    }


    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal) return new MySqlTimeSpan(true);

      if (length >= 0)
      {
        string value = packet.ReadString(length);
        ParseMySql(value);
        return this;
      }

      long bufLength = packet.ReadByte();
      int negate = 0;
      if (bufLength > 0)
        negate = packet.ReadByte();

      isNull = false;
      if (bufLength == 0)
        isNull = true;
      else if (bufLength == 5)
        mValue = new TimeSpan(packet.ReadInteger(4), 0, 0, 0);
      else if (bufLength == 8)
        mValue = new TimeSpan(packet.ReadInteger(4),
             packet.ReadByte(), packet.ReadByte(), packet.ReadByte());
      else
        mValue = new TimeSpan(packet.ReadInteger(4),
             packet.ReadByte(), packet.ReadByte(), packet.ReadByte(),
             packet.ReadInteger(4) / 1000000);

      if (negate == 1)
        mValue = mValue.Negate();
      return this;
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      int len = packet.ReadByte();
      packet.Position += len;
    }

    #endregion

    internal static void SetDSInfo(DataTable dsTable)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      DataRow row = dsTable.NewRow();
      row["TypeName"] = "TIME";
      row["ProviderDbType"] = MySqlDbType.Time;
      row["ColumnSize"] = 0;
      row["CreateFormat"] = "TIME";
      row["CreateParameters"] = null;
      row["DataType"] = "System.TimeSpan";
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
      dsTable.Rows.Add(row);
    }

    public override string ToString()
    {
      return String.Format("{0} {1:00}:{2:00}:{3:00}",
        mValue.Days, mValue.Hours, mValue.Minutes, mValue.Seconds);
    }

    private void ParseMySql(string s)
    {

      string[] parts = s.Split(':', '.');
      int hours = Int32.Parse(parts[0]);
      int mins = Int32.Parse(parts[1]);
      int secs = Int32.Parse(parts[2]);
      int msecs = 0;

      if (parts.Length > 3)
        msecs = Int32.Parse(parts[3])/1000;


      if (hours < 0 || parts[0].StartsWith("-", StringComparison.Ordinal))
      {
        mins *= -1;
        secs *= -1;
        msecs *= -1;
      }
      int days = hours / 24;
      hours = hours - (days * 24);
      mValue = new TimeSpan(days, hours, mins, secs, msecs);
      isNull = false;
    }
  }
}
