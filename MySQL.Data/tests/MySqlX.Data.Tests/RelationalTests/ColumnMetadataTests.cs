// Copyright © 2016, 2024, Oracle and/or its affiliates.
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
      Assert.That(r.Columns.Count, Is.EqualTo(3));
      Assert.That(r.Columns[0].DatabaseName, Is.EqualTo("def"));
      Assert.That(r.Columns[0].SchemaName, Is.Null);
      Assert.That(r.Columns[0].TableName, Is.Null);
      Assert.That(r.Columns[0].TableLabel, Is.Null);
      Assert.That(r.Columns[0].ColumnName, Is.Null);
      Assert.That(r.Columns[0].ColumnLabel, Is.EqualTo("a"));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.Tinyint));
      Assert.That(r.Columns[0].Length, Is.EqualTo(3u));
      Assert.That(r.Columns[0].FractionalDigits, Is.EqualTo(0u));
      Assert.That(r.Columns[0].IsNumberSigned);
      Assert.That(r.Columns[0].CharacterSetName, Is.Null);
      Assert.That(r.Columns[0].CollationName, Is.Null);
      Assert.That(r.Columns[0].IsPadded, Is.False);

      Assert.That(r.Columns[1].SchemaName, Is.EqualTo(schemaName));
      Assert.That(r.Columns[1].TableName, Is.EqualTo("test"));
      Assert.That(r.Columns[1].TableLabel, Is.EqualTo("test"));
      Assert.That(r.Columns[1].ColumnName, Is.EqualTo("b"));
      Assert.That(r.Columns[1].ColumnLabel, Is.EqualTo("b"));
      Assert.That(r.Columns[1].Type, Is.EqualTo(ColumnType.String));
      Assert.That(r.Columns[1].Length, Is.EqualTo(255u));
      Assert.That(r.Columns[1].FractionalDigits, Is.EqualTo(0u));
      Assert.That(r.Columns[1].IsNumberSigned, Is.False);
      Assert.That(r.Columns[1].CharacterSetName, Is.EqualTo("utf8mb4"));
      Assert.That(r.Columns[1].CollationName, Is.EqualTo("utf8mb4_0900_ai_ci"));
      Assert.That(r.Columns[1].IsPadded, Is.False);

      Assert.That(r.Columns[2].SchemaName, Is.EqualTo(schemaName));
      Assert.That(r.Columns[2].TableName, Is.EqualTo("test"));
      Assert.That(r.Columns[2].TableLabel, Is.EqualTo("test"));
      Assert.That(r.Columns[2].ColumnName, Is.EqualTo("c"));
      Assert.That(r.Columns[2].ColumnLabel, Is.EqualTo("c"));
      Assert.That(r.Columns[2].Type, Is.EqualTo(ColumnType.String));
      Assert.That(r.Columns[2].Length, Is.EqualTo(20u));
      Assert.That(r.Columns[2].FractionalDigits, Is.EqualTo(0u));
      Assert.That(r.Columns[2].IsNumberSigned, Is.False);
      Assert.That(r.Columns[2].CharacterSetName, Is.EqualTo("utf8mb4"));
      Assert.That(r.Columns[2].CollationName, Is.EqualTo("utf8mb4_0900_ai_ci"));
      Assert.That(r.Columns[2].IsPadded, Is.False);
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

      Assert.That(r.Columns[0].SchemaName, Is.EqualTo(schemaName));
      Assert.That(r.Columns[0].TableName, Is.EqualTo("test"));
      Assert.That(r.Columns[0].TableLabel, Is.EqualTo("test"));
      Assert.That(r.Columns[0].ColumnName, Is.EqualTo("b"));
      Assert.That(r.Columns[0].ColumnLabel, Is.EqualTo("b"));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.String));
      Assert.That(r.Columns[0].FractionalDigits, Is.EqualTo(0u));
      Assert.That(r.Columns[0].IsNumberSigned, Is.EqualTo(false));
      Assert.That(r.Columns[0].CharacterSetName, Is.EqualTo(defaultValues[0][0]));
      Assert.That(r.Columns[0].CollationName, Is.EqualTo(defaultValues[0][1]));
      Assert.That(r.Columns[0].IsPadded, Is.EqualTo(false));
      Assert.That(rows[0][0], Is.EqualTo("CAR"));

      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        if (connection.driver.Version.isAtLeast(8, 0, 1))
          Assert.That(r.Columns[0].Length, Is.EqualTo(1020u));
        else
          Assert.That(r.Columns[0].Length, Is.EqualTo(255u));
      }
    }

    [Test]
    public void ColumnNames()
    {
      ExecuteSQL("CREATE TABLE test(columnA VARCHAR(255), columnB INT, columnX BIT)");
      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select());

      Assert.That(r.ColumnCount, Is.EqualTo(3));
      Assert.That(r.ColumnCount, Is.EqualTo(r.Columns.Count));
      Assert.That(r.ColumnNames[0], Is.EqualTo("columnA"));
      Assert.That(r.ColumnNames[1], Is.EqualTo("columnB"));
      Assert.That(r.ColumnNames[2], Is.EqualTo("columnX"));
    }

    [Test]
    public void TableDefaultCharset()
    {
      ExecuteSQL("Drop Table if exists test");
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255)) CHARSET greek");
      ExecuteSQL("INSERT INTO test VALUES('Δ')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select("b"));
      var rows = r.FetchAll();

      Assert.That(r.Columns[0].SchemaName, Is.EqualTo(schemaName));
      Assert.That(r.Columns[0].TableName, Is.EqualTo("test"));
      Assert.That(r.Columns[0].TableLabel, Is.EqualTo("test"));
      Assert.That(r.Columns[0].ColumnName, Is.EqualTo("b"));
      Assert.That(r.Columns[0].ColumnLabel, Is.EqualTo("b"));
      Assert.That(r.Columns[0].Type, Is.EqualTo(ColumnType.String));
      Assert.That(r.Columns[0].Length, Is.EqualTo(255u));
      Assert.That(r.Columns[0].FractionalDigits, Is.EqualTo(0u));
      Assert.That(r.Columns[0].IsNumberSigned, Is.False);
      Assert.That(r.Columns[0].CharacterSetName, Is.EqualTo("utf8mb4"));
      Assert.That(r.Columns[0].CollationName, Is.EqualTo("utf8mb4_0900_ai_ci"));
      Assert.That(r.Columns[0].IsPadded, Is.False);
      Assert.That(rows[0][0], Is.EqualTo("Δ"));
    }
  }
}
