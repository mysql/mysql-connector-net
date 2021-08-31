// Copyright (c) 2016, 2021, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class ColumnMetadataTests : BaseTest
  {
    [TearDown]
    public void TearDown() => ExecuteSQL("DROP TABLE IF EXISTS test");
    [Test]
    public void ColumnMetadata()
    {
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255) COLLATE latin1_swedish_ci, c VARCHAR(20) CHARSET greek)");
      ExecuteSQL("INSERT INTO test VALUES('Bob', 'Δ')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select("1 + 1 as a", "b", "c"));
      var rows = r.FetchAll();
      Assert.AreEqual(3, r.Columns.Count);
      Assert.AreEqual("def", r.Columns[0].DatabaseName);
      Assert.Null(r.Columns[0].SchemaName);
      Assert.Null(r.Columns[0].TableName);
      Assert.Null(r.Columns[0].TableLabel);
      Assert.Null(r.Columns[0].ColumnName);
      Assert.AreEqual("a", r.Columns[0].ColumnLabel);
      Assert.AreEqual(ColumnType.Tinyint, r.Columns[0].Type);
      Assert.AreEqual(3u, r.Columns[0].Length);
      Assert.AreEqual(0u, r.Columns[0].FractionalDigits);
      Assert.True(r.Columns[0].IsNumberSigned);
      Assert.Null(r.Columns[0].CharacterSetName);
      Assert.Null(r.Columns[0].CollationName);
      Assert.False(r.Columns[0].IsPadded);

      Assert.AreEqual(schemaName, r.Columns[1].SchemaName);
      Assert.AreEqual("test", r.Columns[1].TableName);
      Assert.AreEqual("test", r.Columns[1].TableLabel);
      Assert.AreEqual("b", r.Columns[1].ColumnName);
      Assert.AreEqual("b", r.Columns[1].ColumnLabel);
      Assert.AreEqual(ColumnType.String, r.Columns[1].Type);
      Assert.AreEqual(255u, r.Columns[1].Length);
      Assert.AreEqual(0u, r.Columns[1].FractionalDigits);
      Assert.False(r.Columns[1].IsNumberSigned);
      Assert.AreEqual("utf8mb4", r.Columns[1].CharacterSetName);
      Assert.AreEqual("utf8mb4_0900_ai_ci", r.Columns[1].CollationName);
      Assert.False(r.Columns[1].IsPadded);

      Assert.AreEqual(schemaName, r.Columns[2].SchemaName);
      Assert.AreEqual("test", r.Columns[2].TableName);
      Assert.AreEqual("test", r.Columns[2].TableLabel);
      Assert.AreEqual("c", r.Columns[2].ColumnName);
      Assert.AreEqual("c", r.Columns[2].ColumnLabel);
      Assert.AreEqual(ColumnType.String, r.Columns[2].Type);
      Assert.AreEqual(20u, r.Columns[2].Length);
      Assert.AreEqual(0u, r.Columns[2].FractionalDigits);
      Assert.False(r.Columns[2].IsNumberSigned);
      Assert.AreEqual("utf8mb4", r.Columns[2].CharacterSetName);
      Assert.AreEqual("utf8mb4_0900_ai_ci", r.Columns[2].CollationName);
      Assert.False(r.Columns[2].IsPadded);
      //Assert.AreEqual("Δ", rows[0][2]);
    }

    [Test]
    public void SchemaDefaultCharset()
    {
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255))");
      ExecuteSQL("INSERT INTO test VALUES('CAR')");

      var defaultValues = ExecuteSQLStatement(GetSession(true).SQL("SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME " +
        $"FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{schemaName}'; ")).FetchAll();

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select("b"));
      var rows = r.FetchAll();

      Assert.AreEqual(schemaName, r.Columns[0].SchemaName);
      Assert.AreEqual("test", r.Columns[0].TableName);
      Assert.AreEqual("test", r.Columns[0].TableLabel);
      Assert.AreEqual("b", r.Columns[0].ColumnName);
      Assert.AreEqual("b", r.Columns[0].ColumnLabel);
      Assert.AreEqual(ColumnType.String, r.Columns[0].Type);
      Assert.AreEqual(0u, r.Columns[0].FractionalDigits);
      Assert.AreEqual(false, r.Columns[0].IsNumberSigned);
      Assert.AreEqual(defaultValues[0][0], r.Columns[0].CharacterSetName);
      Assert.AreEqual(defaultValues[0][1], r.Columns[0].CollationName);
      Assert.AreEqual(false, r.Columns[0].IsPadded);
      Assert.AreEqual("CAR", rows[0][0]);

      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        if (connection.driver.Version.isAtLeast(8, 0, 1))
          Assert.AreEqual(1020u, r.Columns[0].Length);
        else
          Assert.AreEqual(255u, r.Columns[0].Length);
      }
    }

    [Test]
    public void ColumnNames()
    {
      ExecuteSQL("CREATE TABLE test(columnA VARCHAR(255), columnB INT, columnX BIT)");
      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select());

      Assert.AreEqual(3, r.ColumnCount);
      Assert.AreEqual(r.Columns.Count, r.ColumnCount);
      Assert.AreEqual("columnA", r.ColumnNames[0]);
      Assert.AreEqual("columnB", r.ColumnNames[1]);
      Assert.AreEqual("columnX", r.ColumnNames[2]);
    }

    [Test]
    public void TableDefaultCharset()
    {
      ExecuteSQL("Drop Table if exists test");
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255)) CHARSET greek");
      ExecuteSQL("INSERT INTO test VALUES('Δ')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select("b"));
      var rows = r.FetchAll();

      Assert.AreEqual(schemaName, r.Columns[0].SchemaName);
      Assert.AreEqual("test", r.Columns[0].TableName);
      Assert.AreEqual("test", r.Columns[0].TableLabel);
      Assert.AreEqual("b", r.Columns[0].ColumnName);
      Assert.AreEqual("b", r.Columns[0].ColumnLabel);
      Assert.AreEqual(ColumnType.String, r.Columns[0].Type);
      Assert.AreEqual(255u, r.Columns[0].Length);
      Assert.AreEqual(0u, r.Columns[0].FractionalDigits);
      Assert.False(r.Columns[0].IsNumberSigned);
      Assert.AreEqual("utf8mb4", r.Columns[0].CharacterSetName);
      Assert.AreEqual("utf8mb4_0900_ai_ci", r.Columns[0].CollationName);
      Assert.False(r.Columns[0].IsPadded);
      Assert.AreEqual("Δ", rows[0][0]);
    }
  }
}
