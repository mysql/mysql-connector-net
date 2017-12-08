// Copyright � 2013, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Globalization;
using MySql.Data.MySqlClient;

namespace MySql.Data.Types
{
  //Bytes structure is:
  //SRID       [0 - 3]
  //Byte order [4]
  //WKB type   [5 - 8]
  //X          [9 - 16]
  //Y          [17 - 24]
  //The byte order may be either 1 or 0 to indicate little-endian or
  //big-endian storage. The little-endian and big-endian byte orders
  //are also known as Network Data Representation (NDR) and External
  //Data Representation (XDR), respectively.

  //The WKB type is a code that indicates the geometry type. Values
  //from 1 through 7 indicate Point, LineString, Polygon, MultiPoint,
  //MultiLineString, MultiPolygon, and GeometryCollection.

  public struct MySqlGeometry : IMySqlValue
  {
    private readonly MySqlDbType _type;
    private readonly Double _xValue; 
    private readonly Double _yValue;
    private readonly int _srid;

    private const int GEOMETRY_LENGTH = 25;

    public Double? XCoordinate => _xValue;

    public Double? YCoordinate => _yValue;

    public int? SRID => _srid;

    public MySqlGeometry(bool isNull):this(MySqlDbType.Geometry, isNull)
    {
    }

    public MySqlGeometry(Double xValue, Double yValue)
      : this(MySqlDbType.Geometry, xValue, yValue, 0)
    { }

    public MySqlGeometry(Double xValue, Double yValue, int srid)
      : this(MySqlDbType.Geometry, xValue, yValue, srid)
    { }


    internal MySqlGeometry(MySqlDbType type, bool isNull)
    {
      this._type = type;
      isNull = true;
      _xValue = 0;
      _yValue = 0;
      _srid = 0;
      Value = null;
      this.IsNull = isNull;
    }


    internal MySqlGeometry(MySqlDbType type, Double xValue, Double yValue, int srid)
    {
      this._type = type;
      this._xValue = xValue;
      this._yValue = yValue;      
      this.IsNull = false;
      this._srid = srid;
      this.Value = new byte[GEOMETRY_LENGTH];

      byte[] sridBinary = BitConverter.GetBytes(srid);

      for (int i = 0; i < sridBinary.Length; i++)
        Value[i] = sridBinary[i];

      long xVal = BitConverter.DoubleToInt64Bits(xValue);
      long yVal = BitConverter.DoubleToInt64Bits(yValue);

      Value[4] = 1;
      Value[5] = 1;
     
      for (int i = 0; i < 8; i++)
        {
          Value[i + 9] = (byte)(xVal & 0xff);
          xVal >>= 8;
        }

      for (int i = 0; i < 8; i++)
      {
        Value[i + 17] = (byte)(yVal & 0xff);
        yVal >>= 8;
      }
    }

    public MySqlGeometry(MySqlDbType type, byte[] val)
    {

      if (val == null) 
        throw new ArgumentNullException(nameof(val));

      byte[] buffValue = new byte[val.Length];
 
      for (int i = 0; i < val.Length; i++)                  
           buffValue[i] = val[i];

      var xIndex = val.Length == GEOMETRY_LENGTH ? 9 : 5;
      var yIndex = val.Length == GEOMETRY_LENGTH ? 17 : 13;

      Value = buffValue;
      _xValue = BitConverter.ToDouble(val, xIndex);
      _yValue = BitConverter.ToDouble(val, yIndex);
      this._srid = val.Length == GEOMETRY_LENGTH ? BitConverter.ToInt32(val, 0) : 0;
      this.IsNull = false;
      this._type = type;
    }

    #region IMySqlValue Members

   
    MySqlDbType IMySqlValue.MySqlDbType => _type;

    public bool IsNull { get; }


    object IMySqlValue.Value => Value;

    public byte[] Value { get; }

    Type IMySqlValue.SystemType => typeof(byte[]);

    string IMySqlValue.MySqlTypeName => "GEOMETRY";

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      byte[] buffToWrite = null;
     
      try
      {
        buffToWrite = ((MySqlGeometry)val).Value;        
      }
      catch 
      {
        buffToWrite = val as Byte[];
      }

      if (buffToWrite == null)
      {
        MySqlGeometry v = new MySqlGeometry(0, 0);
        MySqlGeometry.TryParse(val.ToString(), out v);
        buffToWrite = v.Value;
      }

      byte[] result = new byte[GEOMETRY_LENGTH];
     
      for (int i = 0; i < buffToWrite.Length; i++)
      {
       if (buffToWrite.Length < GEOMETRY_LENGTH)
         result[i + 4] = buffToWrite[i];
       else
        result[i] = buffToWrite[i];
      }
      
        packet.WriteStringNoNull("_binary ");
        packet.WriteByte((byte)'\'');
        EscapeByteArray(result, GEOMETRY_LENGTH, packet);
        packet.WriteByte((byte)'\'');      
    }

    private static void EscapeByteArray(byte[] bytes, int length, MySqlPacket packet)
    {
      for (int x = 0; x < length; x++)
      {
        byte b = bytes[x];
        if (b == '\0')
        {
          packet.WriteByte((byte)'\\');
          packet.WriteByte((byte)'0');
        }

        else if (b == '\\' || b == '\'' || b == '\"')
        {
          packet.WriteByte((byte)'\\');
          packet.WriteByte(b);
        }
        else
          packet.WriteByte(b);
      }
    }

    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      MySqlGeometry g;
      if (nullVal)
        g = new MySqlGeometry(_type, true);
      else
      {
        if (length == -1)
          length = (long)packet.ReadFieldLength();

        byte[] newBuff = new byte[length];
        packet.Read(newBuff, 0, (int)length);
        g = new MySqlGeometry(_type, newBuff);        
      }
      return g;
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      int len = (int)packet.ReadFieldLength();
      packet.Position += len;
    }

    #endregion


    /// <summary>Returns the Well-Known Text representation of this value</summary>
    /// POINT({0} {1})", longitude, latitude
    /// http://dev.mysql.com/doc/refman/4.1/en/gis-wkt-format.html
    public override string ToString()
    {
      if (!this.IsNull)
        return _srid != 0 ? string.Format(CultureInfo.InvariantCulture.NumberFormat, "SRID={2};POINT({0} {1})", _xValue, _yValue, _srid) : string.Format(CultureInfo.InvariantCulture.NumberFormat, "POINT({0} {1})", _xValue, _yValue);      

      return String.Empty;
    }

    /// <summary>
    /// Get value from WKT format
    /// SRID=0;POINT (x y) or POINT (x y)
    /// </summary>
    /// <param name="value">WKT string format</param>    
    public static MySqlGeometry Parse(string value)
    {
      if (String.IsNullOrEmpty(value))
        throw new ArgumentNullException(nameof(value));

      if (!(value.Contains("SRID") || value.Contains("POINT(") || value.Contains("POINT (")))
        throw new FormatException("String does not contain a valid geometry value");

      MySqlGeometry result = new MySqlGeometry(0,0);
      MySqlGeometry.TryParse(value, out result);

      return result;
    }

    /// <summary>
    /// Try to get value from WKT format
    /// SRID=0;POINT (x y) or POINT (x y)
    /// </summary>
    /// <param name="value">WKT string format</param>    
    /// <param name="mySqlGeometryValue">Out mysqlGeometryValue</param>
    public static bool TryParse(string value, out MySqlGeometry mySqlGeometryValue)
    {
      string[] arrayResult = new string[0];
      string strResult = string.Empty;
      bool hasX = false;
      bool hasY = false;
      Double xVal = 0;
      Double yVal = 0;
      int sridValue = 0;

      try
      {
        if (value.Contains(";"))
          arrayResult = value.Split(';');
        else
          strResult = value;

        if (arrayResult.Length > 1 || strResult != String.Empty)
        {
          string point = strResult != String.Empty ? strResult : arrayResult[1];
          point = point.Replace("POINT (", "").Replace("POINT(", "").Replace(")", "");
          var coord = point.Split(' ');
          if (coord.Length > 1)
          {
            hasX = Double.TryParse(coord[0], out xVal);
            hasY = Double.TryParse(coord[1], out yVal);
          }
          if (arrayResult.Length >= 1)
            Int32.TryParse(arrayResult[0].Replace("SRID=", ""), out sridValue);
        }
        if (hasX && hasY)
        {
          mySqlGeometryValue = new MySqlGeometry(xVal, yVal, sridValue);
          return true;
        }
      }
      catch
      {  }
      
      mySqlGeometryValue = new MySqlGeometry(true);
      return false; 
    }
    
    public static void SetDSInfo(MySqlSchemaCollection dsTable)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = dsTable.AddRow();
      row["TypeName"] = "GEOMETRY";
      row["ProviderDbType"] = MySqlDbType.Geometry;
      row["ColumnSize"] = GEOMETRY_LENGTH;
      row["CreateFormat"] = "GEOMETRY";
      row["CreateParameters"] = DBNull.Value; ;
      row["DataType"] = "System.Byte[]";
      row["IsAutoincrementable"] = false;
      row["IsBestMatch"] = true;
      row["IsCaseSensitive"] = false;
      row["IsFixedLength"] = false;
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
      row["LiteralPrefix"] = DBNull.Value;
      row["LiteralSuffix"] = DBNull.Value;
      row["NativeDataType"] = DBNull.Value;
    }

    public string GetWKT()
    {
      if (!this.IsNull)
        return string.Format(CultureInfo.InvariantCulture.NumberFormat, "POINT({0} {1})", _xValue, _yValue);

      return String.Empty;
    }
 }
}
