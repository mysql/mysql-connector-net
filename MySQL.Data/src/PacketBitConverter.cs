// Copyright © 2004, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

namespace MySql.Data
{
  public static class PacketBitConverter
  {
    // Due to the lack of support of the C# BinaryPrimitives class
    // on net452 and net48 this class is necessary for back compatibility.
    // All instances of PacketBitConverter can be replaced with corresponding
    // BinaryPrimitives methods for future versions.

    // The server sends MySql Packets in LittleEndian encoding
    // The methods provided by the BitConverter class check for the
    // endian-ness of the client system and do conversions accordingly
    // which lead to incorrect conversions on BigEndian systems

    // Following function are analogues with BinaryPrimitives.Write*LittleEndian
    public static byte[] GetBytes(int value)
    {
      return new byte[] {
                (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24)
            };
    }

    public static byte[] GetBytes(long value)
    {
      return new byte[] {
                (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24),
                (byte)(value >> 32), (byte)(value >> 40), (byte)(value >> 48), (byte)(value >> 56)
            };
    }

    unsafe public static byte[] GetBytes(float value)
    {
      int val = *(int*)&value;
      return GetBytes(val);
    }

    unsafe public static byte[] GetBytes(double value)
    {
      long val = *(long*)&value;
      return GetBytes(val);
    }

    // Following functions are analogous to BinaryPrimitives.Read*LittleEndian
    unsafe public static float ToSingle(byte[] byteArray, int startIndex)
    {
      int val = ToInt32(byteArray, startIndex);
      return *(float*)&val;
    }

    unsafe public static double ToDouble(byte[] byteArray, int startIndex)
    {
      long val = ToInt64(byteArray, startIndex);
      return *(double*)&val;
    }

    public static ushort ToUInt16(byte[] byteArray, int startIndex)
    {
      return (ushort)(byteArray[startIndex++] | byteArray[startIndex] << 8);
    }

    public static uint ToUInt32(byte[] byteArray, int startIndex)
    {
      return (uint)(byteArray[startIndex++] | byteArray[startIndex++] << 8
            | byteArray[startIndex++] << 16 | byteArray[startIndex] << 24);
    }

    public static ulong ToUInt64(byte[] byteArray, int startIndex)
    {
      return (ulong)ToUInt32(byteArray, startIndex) + ((ulong)ToUInt32(byteArray, startIndex + 4) << 32);
    }

    public static short ToInt16(byte[] byteArray, int startIndex)
    {
      return (short)ToUInt16(byteArray, startIndex);
    }

    public static int ToInt32(byte[] byteArray, int startIndex)
    {
      return (int)ToUInt32(byteArray, startIndex);
    }

    public static long ToInt64(byte[] byteArray, int startIndex)
    {
      return (long)ToUInt64(byteArray, startIndex);
    }
  }
}
