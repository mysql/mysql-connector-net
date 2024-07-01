// Copyright © 2015, 2024, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(float)));
      Assert.That((int)r.Columns[0].Length, Is.EqualTo(14));
      Assert.That((int)r.Columns[0].FractionalDigits, Is.EqualTo(8));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Float));
      Assert.That(rows.Count, Is.EqualTo(3));
      Assert.That((float)rows[0][0], Is.EqualTo(23.4f));
      Assert.That(rows[1][0], Is.EqualTo(14.8f));
      Assert.That(rows[2][0], Is.EqualTo(11.9f));
    }

    [Test]
    public void Double()
    {
      ExecuteSQL("CREATE TABLE test(rvalue DOUBLE(12,4))");
      ExecuteSQL("INSERT INTO test VALUES(23.4), (14.8), (11.9)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("rvalue"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(double)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Double));
      Assert.That((int)r.Columns[0].Length, Is.EqualTo(12));
      Assert.That((int)r.Columns[0].FractionalDigits, Is.EqualTo(4));
      Assert.That(rows.Count, Is.EqualTo(3));
      Assert.That(rows[0][0], Is.EqualTo(23.4));
      Assert.That(rows[1][0], Is.EqualTo(14.8));
      Assert.That(rows[2][0], Is.EqualTo(11.9));
    }

    [Test]
    public void Set()
    {
      ExecuteSQL("CREATE TABLE test(rvalue SET('A','B','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('A'), ('B,A'), ('B')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("rvalue"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(string)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Set));
      Assert.That(rows.Count, Is.EqualTo(3));
      Assert.That(rows[0][0], Is.EqualTo("A"));
      Assert.That(rows[1][0], Is.EqualTo("A,B"));
      Assert.That(rows[2][0], Is.EqualTo("B"));
    }

    [Test]
    public void Enum()
    {
      ExecuteSQL("CREATE TABLE test(rvalue Enum('Alpha','Beta','C','D'))");
      ExecuteSQL("INSERT INTO test VALUES('Alpha'), ('Beta'), ('C')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select("rvalue"));
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(string)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Enum));
      Assert.That(rows.Count, Is.EqualTo(3));
      Assert.That(rows[0][0], Is.EqualTo("Alpha"));
      Assert.That(rows[1][0], Is.EqualTo("Beta"));
      Assert.That(rows[2][0], Is.EqualTo("C"));
    }

    [Test]
    public void SignedIntegers()
    {
      ExecuteSQL("CREATE TABLE test(tinyCol TINYINT, smallCol SMALLINT, mediumCol MEDIUMINT, intCol INT, bigCol BIGINT)");
      ExecuteSQL("INSERT INTO test VALUES(127, 32767, 8388607, 2147483647, 9223372036854775807)");
      ExecuteSQL("INSERT INTO test VALUES(-128, -32768, -8388608, -2147483648, -9223372036854775808)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns.Count, Is.EqualTo(5));
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(sbyte)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Tinyint));
      Assert.That(rows[0][0], Is.EqualTo((sbyte)127));
      Assert.That(rows[1][0], Is.EqualTo((sbyte)-128));
      Assert.That(r.Columns[1].ClrType, Is.EqualTo(typeof(Int16)));
      Assert.That(r.Columns[1].Type, Is.EqualTo(ColumnType.Smallint));
      Assert.That(rows[0][1], Is.EqualTo((short)32767));
      Assert.That(rows[1][1], Is.EqualTo((short)-32768));
      Assert.That(r.Columns[2].ClrType, Is.EqualTo(typeof(Int32)));
      Assert.That(r.Columns[2].Type, Is.EqualTo(ColumnType.Mediumint));
      Assert.That(rows[0][2], Is.EqualTo(8388607));
      Assert.That(rows[1][2], Is.EqualTo(-8388608));
      Assert.That(r.Columns[3].ClrType, Is.EqualTo(typeof(Int32)));
      Assert.That(r.Columns[3].Type, Is.EqualTo(ColumnType.Int));
      Assert.That(rows[0][3], Is.EqualTo(2147483647));
      Assert.That(rows[1][3], Is.EqualTo(-2147483648));
      Assert.That(r.Columns[4].ClrType, Is.EqualTo(typeof(Int64)));
      Assert.That(r.Columns[4].Type, Is.EqualTo(ColumnType.Bigint));
      Assert.That(rows[0][4], Is.EqualTo((long)9223372036854775807));
      Assert.That(rows[1][4], Is.EqualTo((long)-9223372036854775808));
    }

    [Test]
    public void UnsignedIntegers()
    {
      ExecuteSQL("CREATE TABLE test(tinyCol TINYINT UNSIGNED, smallCol SMALLINT UNSIGNED, mediumCol MEDIUMINT UNSIGNED, intCol INT UNSIGNED, bigCol BIGINT UNSIGNED)");
      ExecuteSQL("INSERT INTO test VALUES(255, 65535, 16777215, 4294967295, 18446744073709551615)");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns.Count, Is.EqualTo(5));
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(byte)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Tinyint));
      Assert.That(rows[0][0], Is.EqualTo((byte)255));
      Assert.That(r.Columns[1].ClrType, Is.EqualTo(typeof(UInt16)));
      Assert.That(r.Columns[1].Type, Is.EqualTo(ColumnType.Smallint));
      Assert.That(rows[0][1], Is.EqualTo((ushort)65535));
      Assert.That(r.Columns[2].ClrType, Is.EqualTo(typeof(UInt32)));
      Assert.That(r.Columns[2].Type, Is.EqualTo(ColumnType.Mediumint));
      Assert.That(rows[0][2], Is.EqualTo((uint)16777215));
      Assert.That(r.Columns[3].ClrType, Is.EqualTo(typeof(UInt32)));
      Assert.That(r.Columns[3].Type, Is.EqualTo(ColumnType.Int));
      Assert.That(rows[0][3], Is.EqualTo((uint)4294967295));
      Assert.That(r.Columns[4].ClrType, Is.EqualTo(typeof(UInt64)));
      Assert.That(r.Columns[4].Type, Is.EqualTo(ColumnType.Bigint));
      Assert.That(rows[0][4], Is.EqualTo((ulong)18446744073709551615));
    }

    [Test]
    public void Bit()
    {
      ExecuteSQL("CREATE TABLE test(bitCol BIT(8))");
      ExecuteSQL("INSERT INTO test VALUES(b'1111111')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(UInt64)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Bit));
      Assert.That(rows[0][0], Is.EqualTo((ulong)127));
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
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(decimal)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Decimal));
      Assert.That(rows[0][0], Is.EqualTo(-1.23m));
      Assert.That(rows[1][0], Is.EqualTo(-12.345m));
      Assert.That(rows[2][0], Is.EqualTo(5m));
      Assert.That(rows[3][0], Is.EqualTo(43m));
      Assert.That(rows[4][0], Is.EqualTo(14523.2887238m));
      Assert.That(rows[5][0], Is.EqualTo(-8947.8923784m));
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
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(string)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Json));
      Assert.That(rows[0][0], Is.EqualTo("{\"id\": 1, \"name\": \"John\"}"));
      Assert.That(rows[1][0], Is.EqualTo("[\"a\", 1, \"b\", 2]"));
    }

    [Test]
    public void Strings()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) COLLATE cp932_japanese_ci)");
      ExecuteSQL("INSERT INTO test VALUES('表')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(string)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.String));
      Assert.That(rows[0][0], Is.EqualTo("表"));
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
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(UInt32)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Int));
      Assert.That(r.Columns[0].IsNumberSigned, Is.False);
      Assert.That(r.Columns[0].IsPadded);
      Assert.That(rows[0][0].ToString(), Is.EqualTo("0000000100"));
    }

    [Test]
    public void Bytes()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) BINARY)");
      ExecuteSQL("INSERT INTO test VALUES('John')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(string)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.String));
      Assert.That(rows[0][0], Is.EqualTo("John"));
    }

    [Test]
    public void BytesUsingCollation()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(255) COLLATE utf8_bin)");
      ExecuteSQL("INSERT INTO test VALUES('Mark')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(string)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.String));
      Assert.That(rows[0][0], Is.EqualTo("Mark"));
    }

    [Test]
    public void Geometry()
    {
      ExecuteSQL("CREATE TABLE test(line GEOMETRY)");
      ExecuteSQL("INSERT INTO test VALUES(ST_GeomFromText('LINESTRING(0 0, 10 10, 20 25, 50 60)'))");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns, Has.One.Items);
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(byte[])));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Geometry));
      Assert.That(BitConverter.ToString((byte[])rows[0][0]).Replace("-", ""), Is.EqualTo("0000000001020000000400000000000000000000000000000000000000000000000000244000000000000024400000000000003440000000000000394000000000000049400000000000004E40"));
    }

    [Test]
    public void BlobTypes()
    {
      ExecuteSQL("CREATE TABLE test(a BLOB, b TEXT)");
      ExecuteSQL("INSERT INTO test VALUES('Car', 'Plane')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select());
      var rows = r.FetchAll();
      Assert.That(r.Columns.Count, Is.EqualTo(2));
      Assert.That(r.Columns[0].ClrType, Is.EqualTo(typeof(byte[])));
      Assert.That(r.Columns[1].ClrType, Is.EqualTo(typeof(string)));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Bytes));
      Assert.That(r.Columns[1].Type, Is.EqualTo(ColumnType.String));
      Assert.That(rows[0][0], Is.EqualTo(CharSetMap.GetEncoding(r.Columns[0].CharacterSetName).GetBytes("Car")));
      Assert.That(rows[0][1], Is.EqualTo("Plane"));
    }
  }
}
