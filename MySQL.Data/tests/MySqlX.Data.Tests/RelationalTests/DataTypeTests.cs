// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class DataTypeTests : BaseTest
  {
    [TearDown]
    public void TearDown() => ExecuteSQL("DROP TABLE IF EXISTS test");

    [Test]
    public void Float()
    {
      ExecuteSQL("CREATE TABLE test(rvalue FLOAT(14,8))");
      ExecuteSQL("INSERT INTO test VALUES(23.4), (14.8), (11.9)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("rvalue"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(float), r.Columns[0].ClrType);
      Assert.AreEqual(14, (int)r.Columns[0].Length);
      Assert.AreEqual(8, (int)r.Columns[0].FractionalDigits);
      Assert.AreEqual(ColumnType.Float, r.Columns[0].Type);
      Assert.AreEqual(3, rows.Count);
      Assert.AreEqual(23.4f, (float)rows[0][0]);
      Assert.AreEqual(14.8f, rows[1][0]);
      Assert.AreEqual(11.9f, rows[2][0]);
    }

    [Test]
    public void Double()
    {
      ExecuteSQL("CREATE TABLE test(rvalue DOUBLE(12,4))");
      ExecuteSQL("INSERT INTO test VALUES(23.4), (14.8), (11.9)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("rvalue"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(double), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Double, r.Columns[0].Type);
      Assert.AreEqual(12, (int)r.Columns[0].Length);
      Assert.AreEqual(4, (int)r.Columns[0].FractionalDigits);
      Assert.AreEqual(3, rows.Count);
      Assert.AreEqual(23.4, rows[0][0]);
      Assert.AreEqual(14.8, rows[1][0]);
      Assert.AreEqual(11.9, rows[2][0]);
    }

    [Test]
    public void Set()
    {
      ExecuteSQL("CREATE TABLE test(rvalue SET('A','B','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('A'), ('B,A'), ('B')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("rvalue"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(string), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Set, r.Columns[0].Type);
      Assert.AreEqual(3, rows.Count);
      Assert.AreEqual("A", rows[0][0]);
      Assert.AreEqual("A,B", rows[1][0]);
      Assert.AreEqual("B", rows[2][0]);
    }

    [Test]
    public void Enum()
    {
      ExecuteSQL("CREATE TABLE test(rvalue Enum('Alpha','Beta','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('Alpha'), ('Beta'), ('C')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("rvalue"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(string), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Enum, r.Columns[0].Type);
      Assert.AreEqual(3, rows.Count);
      Assert.AreEqual("Alpha", rows[0][0]);
      Assert.AreEqual("Beta", rows[1][0]);
      Assert.AreEqual("C", rows[2][0]);
    }

    [Test]
    public void SignedIntegers()
    {
      ExecuteSQL("CREATE TABLE test(tinyCol TINYINT, smallCol SMALLINT, mediumCol MEDIUMINT, intCol INT, bigCol BIGINT)");
      ExecuteSQL("INSERT INTO test VALUES(127, 32767, 8388607, 2147483647, 9223372036854775807)");
      ExecuteSQL("INSERT INTO test VALUES(-128, -32768, -8388608, -2147483648, -9223372036854775808)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.AreEqual(5, r.Columns.Count);
      Assert.AreEqual(typeof(sbyte), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Tinyint, r.Columns[0].Type);
      Assert.AreEqual((sbyte)127, rows[0][0]);
      Assert.AreEqual((sbyte)-128, rows[1][0]);
      Assert.AreEqual(typeof(Int16), r.Columns[1].ClrType);
      Assert.AreEqual(ColumnType.Smallint, r.Columns[1].Type);
      Assert.AreEqual((short)32767, rows[0][1]);
      Assert.AreEqual((short)-32768, rows[1][1]);
      Assert.AreEqual(typeof(Int32), r.Columns[2].ClrType);
      Assert.AreEqual(ColumnType.Mediumint, r.Columns[2].Type);
      Assert.AreEqual(8388607, rows[0][2]);
      Assert.AreEqual(-8388608, rows[1][2]);
      Assert.AreEqual(typeof(Int32), r.Columns[3].ClrType);
      Assert.AreEqual(ColumnType.Int, r.Columns[3].Type);
      Assert.AreEqual(2147483647, rows[0][3]);
      Assert.AreEqual(-2147483648, rows[1][3]);
      Assert.AreEqual(typeof(Int64), r.Columns[4].ClrType);
      Assert.AreEqual(ColumnType.Bigint, r.Columns[4].Type);
      Assert.AreEqual((long)9223372036854775807, rows[0][4]);
      Assert.AreEqual((long)-9223372036854775808, rows[1][4]);
    }

    [Test]
    public void UnsignedIntegers()
    {
      ExecuteSQL("CREATE TABLE test(tinyCol TINYINT UNSIGNED, smallCol SMALLINT UNSIGNED, mediumCol MEDIUMINT UNSIGNED, intCol INT UNSIGNED, bigCol BIGINT UNSIGNED)");
      ExecuteSQL("INSERT INTO test VALUES(255, 65535, 16777215, 4294967295, 18446744073709551615)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.AreEqual(5, r.Columns.Count);
      Assert.AreEqual(typeof(byte), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Tinyint, r.Columns[0].Type);
      Assert.AreEqual((byte)255, rows[0][0]);
      Assert.AreEqual(typeof(UInt16), r.Columns[1].ClrType);
      Assert.AreEqual(ColumnType.Smallint, r.Columns[1].Type);
      Assert.AreEqual((ushort)65535, rows[0][1]);
      Assert.AreEqual(typeof(UInt32), r.Columns[2].ClrType);
      Assert.AreEqual(ColumnType.Mediumint, r.Columns[2].Type);
      Assert.AreEqual((uint)16777215, rows[0][2]);
      Assert.AreEqual(typeof(UInt32), r.Columns[3].ClrType);
      Assert.AreEqual(ColumnType.Int, r.Columns[3].Type);
      Assert.AreEqual((uint)4294967295, rows[0][3]);
      Assert.AreEqual(typeof(UInt64), r.Columns[4].ClrType);
      Assert.AreEqual(ColumnType.Bigint, r.Columns[4].Type);
      Assert.AreEqual((ulong)18446744073709551615, rows[0][4]);
    }

    [Test]
    public void Bit()
    {
      ExecuteSQL("CREATE TABLE test(bitCol BIT(8))");
      ExecuteSQL("INSERT INTO test VALUES(b'1111111')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(UInt64), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Bit, r.Columns[0].Type);
      Assert.AreEqual((ulong)127, rows[0][0]);
    }

    [Test]
    public void Decimal()
    {
      ExecuteSQL("CREATE TABLE test(decCol1 DECIMAL(20,9))");
      ExecuteSQL("INSERT INTO test VALUES(-1.23), (-12.345), (5), (43)");
      ExecuteSQL("INSERT INTO test VALUES(14523.2887238), (-8947.8923784)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(decimal), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Decimal, r.Columns[0].Type);
      Assert.AreEqual(-1.23m, rows[0][0]);
      Assert.AreEqual(-12.345m, rows[1][0]);
      Assert.AreEqual(5m, rows[2][0]);
      Assert.AreEqual(43m, rows[3][0]);
      Assert.AreEqual(14523.2887238m, rows[4][0]);
      Assert.AreEqual(-8947.8923784m, rows[5][0]);
    }

    [Test]
    public void Json()
    {
      ExecuteSQL("CREATE TABLE test(jdoc JSON)");
      ExecuteSQL("INSERT INTO test VALUES('{ \"id\": 1, \"name\": \"John\" }')");
      ExecuteSQL("INSERT INTO test VALUES('[ \"a\", 1, \"b\", 2 ]')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(string), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Json, r.Columns[0].Type);
      Assert.AreEqual("{\"id\": 1, \"name\": \"John\"}", rows[0][0]);
      Assert.AreEqual("[\"a\", 1, \"b\", 2]", rows[1][0]);
    }

    [Test]
    public void Strings()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) COLLATE cp932_japanese_ci)");
      ExecuteSQL("INSERT INTO test VALUES('表')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(string), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.String, r.Columns[0].Type);
      Assert.AreEqual("表", rows[0][0]);
    }

    [Test]
    [Ignore("Fix for 8.0.13")]
    public void UnsingedZeroFill()
    {
      ExecuteSQL("CREATE TABLE test(id INT ZEROFILL)");
      ExecuteSQL("INSERT INTO test VALUES(100)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(UInt32), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Int, r.Columns[0].Type);
      Assert.False(r.Columns[0].IsNumberSigned);
      Assert.True(r.Columns[0].IsPadded);
      Assert.AreEqual("0000000100", rows[0][0].ToString());
    }

    [Test]
    public void Bytes()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) BINARY)");
      ExecuteSQL("INSERT INTO test VALUES('John')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(string), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.String, r.Columns[0].Type);
      Assert.AreEqual("John", rows[0][0]);
    }

    [Test]
    public void BytesUsingCollation()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) COLLATE utf8_bin)");
      ExecuteSQL("INSERT INTO test VALUES('Mark')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(string), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.String, r.Columns[0].Type);
      Assert.AreEqual("Mark", rows[0][0]);
    }

    [Test]
    public void Geometry()
    {
      ExecuteSQL("CREATE TABLE test(line GEOMETRY)");
      ExecuteSQL("INSERT INTO test VALUES(ST_GeomFromText('LINESTRING(0 0, 10 10, 20 25, 50 60)'))");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.AreEqual(typeof(byte[]), r.Columns[0].ClrType);
      Assert.AreEqual(ColumnType.Geometry, r.Columns[0].Type);
      Assert.AreEqual("0000000001020000000400000000000000000000000000000000000000000000000000244000000000000024400000000000003440000000000000394000000000000049400000000000004E40",
        BitConverter.ToString((byte[])rows[0][0]).Replace("-", ""));
    }

    [Test]
    public void BlobTypes()
    {
      ExecuteSQL("CREATE TABLE test(a BLOB, b TEXT)");
      ExecuteSQL("INSERT INTO test VALUES('Car', 'Plane')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.AreEqual(2, r.Columns.Count);
      Assert.AreEqual(typeof(byte[]), r.Columns[0].ClrType);
      Assert.AreEqual(typeof(string), r.Columns[1].ClrType);
      Assert.AreEqual(ColumnType.Bytes, r.Columns[0].Type);
      Assert.AreEqual(ColumnType.String, r.Columns[1].Type);
      Assert.AreEqual(CharSetMap.GetEncoding(new DBVersion(), r.Columns[0].CharacterSetName).GetBytes("Car"), rows[0][0]);
      Assert.AreEqual("Plane", rows[0][1]);
    }
  }
}
