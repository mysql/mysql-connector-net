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

using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class DataTypeTests : BaseTest
  {
    [Fact]
    public void Float()
    {
      ExecuteSQL("CREATE TABLE test(rvalue FLOAT(14,8))");
      ExecuteSQL("INSERT INTO test VALUES(23.4), (14.8), (11.9)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(float), r.Columns[0].ClrType);
      Assert.Equal(14, (int)r.Columns[0].Length);
      Assert.Equal(8, (int)r.Columns[0].FractionalDigits);
      Assert.Equal(ColumnType.Float, r.Columns[0].Type);
      Assert.Equal(3, rows.Count);
      Assert.Equal(23.4f, (float)rows[0][0]);
      Assert.Equal(14.8f, rows[1][0]);
      Assert.Equal(11.9f, rows[2][0]);
    }

    [Fact]
    public void Double()
    {
      ExecuteSQL("CREATE TABLE test(rvalue DOUBLE(12,4))");
      ExecuteSQL("INSERT INTO test VALUES(23.4), (14.8), (11.9)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(double), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Double, r.Columns[0].Type);
      Assert.Equal(12, (int)r.Columns[0].Length);
      Assert.Equal(4, (int)r.Columns[0].FractionalDigits);
      Assert.Equal(3, rows.Count);
      Assert.Equal(23.4, rows[0][0]);
      Assert.Equal(14.8, rows[1][0]);
      Assert.Equal(11.9, rows[2][0]);
    }

    [Fact]
    public void Set()
    {
      ExecuteSQL("CREATE TABLE test(rvalue SET('A','B','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('A'), ('B,A'), ('B')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(string), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Set, r.Columns[0].Type);
      Assert.Equal(3, rows.Count);
      Assert.Equal("A", rows[0][0]);
      Assert.Equal("A,B", rows[1][0]);
      Assert.Equal("B", rows[2][0]);
    }

    [Fact]
    public void Enum()
    {
      ExecuteSQL("CREATE TABLE test(rvalue Enum('Alpha','Beta','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('Alpha'), ('Beta'), ('C')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select("rvalue").Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(string), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Enum, r.Columns[0].Type);
      Assert.Equal(3, rows.Count);
      Assert.Equal("Alpha", rows[0][0]);
      Assert.Equal("Beta", rows[1][0]);
      Assert.Equal("C", rows[2][0]);
    }

    [Fact]
    public void SignedIntegers()
    {
      ExecuteSQL("CREATE TABLE test(tinyCol TINYINT, smallCol SMALLINT, mediumCol MEDIUMINT, intCol INT, bigCol BIGINT)");
      ExecuteSQL("INSERT INTO test VALUES(127, 32767, 8388607, 2147483647, 9223372036854775807)");
      ExecuteSQL("INSERT INTO test VALUES(-128, -32768, -8388608, -2147483648, -9223372036854775808)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(5, r.Columns.Count);
      Assert.Equal(typeof(sbyte), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Tinyint, r.Columns[0].Type);
      Assert.Equal((sbyte)127, rows[0][0]);
      Assert.Equal((sbyte)-128, rows[1][0]);
      Assert.Equal(typeof(Int16), r.Columns[1].ClrType);
      Assert.Equal(ColumnType.Smallint, r.Columns[1].Type);
      Assert.Equal((short)32767, rows[0][1]);
      Assert.Equal((short)-32768, rows[1][1]);
      Assert.Equal(typeof(Int32), r.Columns[2].ClrType);
      Assert.Equal(ColumnType.Mediumint, r.Columns[2].Type);
      Assert.Equal(8388607, rows[0][2]);
      Assert.Equal(-8388608, rows[1][2]);
      Assert.Equal(typeof(Int32), r.Columns[3].ClrType);
      Assert.Equal(ColumnType.Int, r.Columns[3].Type);
      Assert.Equal(2147483647, rows[0][3]);
      Assert.Equal(-2147483648, rows[1][3]);
      Assert.Equal(typeof(Int64), r.Columns[4].ClrType);
      Assert.Equal(ColumnType.Bigint, r.Columns[4].Type);
      Assert.Equal((long)9223372036854775807, rows[0][4]);
      Assert.Equal((long)-9223372036854775808, rows[1][4]);
    }

    [Fact]
    public void UnsignedIntegers()
    {
      ExecuteSQL("CREATE TABLE test(tinyCol TINYINT UNSIGNED, smallCol SMALLINT UNSIGNED, mediumCol MEDIUMINT UNSIGNED, intCol INT UNSIGNED, bigCol BIGINT UNSIGNED)");
      ExecuteSQL("INSERT INTO test VALUES(255, 65535, 16777215, 4294967295, 18446744073709551615)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(5, r.Columns.Count);
      Assert.Equal(typeof(byte), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Tinyint, r.Columns[0].Type);
      Assert.Equal((byte)255, rows[0][0]);
      Assert.Equal(typeof(UInt16), r.Columns[1].ClrType);
      Assert.Equal(ColumnType.Smallint, r.Columns[1].Type);
      Assert.Equal((ushort)65535, rows[0][1]);
      Assert.Equal(typeof(UInt32), r.Columns[2].ClrType);
      Assert.Equal(ColumnType.Mediumint, r.Columns[2].Type);
      Assert.Equal((uint)16777215, rows[0][2]);
      Assert.Equal(typeof(UInt32), r.Columns[3].ClrType);
      Assert.Equal(ColumnType.Int, r.Columns[3].Type);
      Assert.Equal((uint)4294967295, rows[0][3]);
      Assert.Equal(typeof(UInt64), r.Columns[4].ClrType);
      Assert.Equal(ColumnType.Bigint, r.Columns[4].Type);
      Assert.Equal((ulong)18446744073709551615, rows[0][4]);
    }

    [Fact]
    public void Bit()
    {
      ExecuteSQL("CREATE TABLE test(bitCol BIT(8))");
      ExecuteSQL("INSERT INTO test VALUES(b'1111111')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(UInt64), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Bit, r.Columns[0].Type);
      Assert.Equal((ulong)127, rows[0][0]);
    }

    [Fact]
    public void Decimal()
    {
      ExecuteSQL("CREATE TABLE test(decCol1 DECIMAL(20,9))");
      ExecuteSQL("INSERT INTO test VALUES(-1.23), (-12.345), (5), (43)");
      ExecuteSQL("INSERT INTO test VALUES(14523.2887238), (-8947.8923784)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(decimal), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Decimal, r.Columns[0].Type);
      Assert.Equal(-1.23m, rows[0][0]);
      Assert.Equal(-12.345m, rows[1][0]);
      Assert.Equal(5m, rows[2][0]);
      Assert.Equal(43m, rows[3][0]);
      Assert.Equal(14523.2887238m, rows[4][0]);
      Assert.Equal(-8947.8923784m, rows[5][0]);
    }

    [Fact]
    public void Json()
    {
      ExecuteSQL("CREATE TABLE test(jdoc JSON)");
      ExecuteSQL("INSERT INTO test VALUES('{ \"id\": 1, \"name\": \"John\" }')");
      ExecuteSQL("INSERT INTO test VALUES('[ \"a\", 1, \"b\", 2 ]')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(string), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Json, r.Columns[0].Type);
      Assert.Equal("{\"id\": 1, \"name\": \"John\"}", rows[0][0]);
      Assert.Equal("[\"a\", 1, \"b\", 2]", rows[1][0]);
    }

    [Fact]
    public void Strings()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) COLLATE cp932_japanese_ci)");
      ExecuteSQL("INSERT INTO test VALUES('表')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(string), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.String, r.Columns[0].Type);
      Assert.Equal("表", rows[0][0]);
    }

    //[Fact]
    public void UnsingedZeroFill()
    {
      ExecuteSQL("CREATE TABLE test(id INT ZEROFILL)");
      ExecuteSQL("INSERT INTO test VALUES(100)");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(UInt32), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Int, r.Columns[0].Type);
      Assert.False(r.Columns[0].IsNumberSigned);
      Assert.True(r.Columns[0].IsPadded);
      Assert.Equal("0000000100", rows[0][0].ToString());
    }

    [Fact]
    public void Bytes()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) BINARY)");
      ExecuteSQL("INSERT INTO test VALUES('John')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(byte[]), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Bytes, r.Columns[0].Type);
      Assert.Equal(Encoding.UTF8.GetBytes("John"), rows[0][0]);
    }

    [Fact]
    public void BytesUsingCollation()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) COLLATE utf8_bin)");
      ExecuteSQL("INSERT INTO test VALUES('Mark')");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(byte[]), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Bytes, r.Columns[0].Type);
      Assert.Equal(Encoding.UTF8.GetBytes("Mark"), rows[0][0]);
    }

    [Fact]
    public void Geometry()
    {
      ExecuteSQL("CREATE TABLE test(line GEOMETRY)");
      ExecuteSQL("INSERT INTO test VALUES(ST_GeomFromText('LINESTRING(0 0, 10 10, 20 25, 50 60)'))");

      RowResult r = GetSession().GetSchema("test").GetTable("test").Select().Execute();
      var rows = r.FetchAll();
      Assert.Equal(1, r.Columns.Count);
      Assert.Equal(typeof(byte[]), r.Columns[0].ClrType);
      Assert.Equal(ColumnType.Geometry, r.Columns[0].Type);
      Assert.Equal("0000000001020000000400000000000000000000000000000000000000000000000000244000000000000024400000000000003440000000000000394000000000000049400000000000004E40", 
        BitConverter.ToString((byte[])rows[0][0]).Replace("-", ""));
    }
  }
}
