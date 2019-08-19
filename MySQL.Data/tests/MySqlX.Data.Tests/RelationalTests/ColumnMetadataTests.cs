// Copyright (c) 2016, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class ColumnMetadataTests : BaseTest
  {
    [Fact]
    public void ColumnMetadata()
    {
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255) COLLATE latin1_swedish_ci, c VARCHAR(20) CHARSET greek)");
      ExecuteSQL("INSERT INTO test VALUES('Bob', 'Δ')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select("1 + 1 as a", "b", "c"));
      var rows = r.FetchAll();
      Assert.Equal(3, r.Columns.Count);
      Assert.Equal("def", r.Columns[0].DatabaseName);
      Assert.Null(r.Columns[0].SchemaName);
      Assert.Null(r.Columns[0].TableName);
      Assert.Null(r.Columns[0].TableLabel);
      Assert.Null(r.Columns[0].ColumnName);
      Assert.Equal("a", r.Columns[0].ColumnLabel);
      Assert.Equal(ColumnType.Tinyint, r.Columns[0].Type);
      Assert.Equal(3u, r.Columns[0].Length);
      Assert.Equal(0u, r.Columns[0].FractionalDigits);
      Assert.True(r.Columns[0].IsNumberSigned);
      Assert.Null(r.Columns[0].CharacterSetName);
      Assert.Null(r.Columns[0].CollationName);
      Assert.False(r.Columns[0].IsPadded);

      Assert.Equal(schemaName, r.Columns[1].SchemaName);
      Assert.Equal("test", r.Columns[1].TableName);
      Assert.Equal("test", r.Columns[1].TableLabel);
      Assert.Equal("b", r.Columns[1].ColumnName);
      Assert.Equal("b", r.Columns[1].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[1].Type);
      Assert.Equal(255u, r.Columns[1].Length);
      Assert.Equal(0u, r.Columns[1].FractionalDigits);
      Assert.False(r.Columns[1].IsNumberSigned);
      Assert.Equal("utf8mb4", r.Columns[1].CharacterSetName);
      Assert.Equal("utf8mb4_general_ci", r.Columns[1].CollationName);
      Assert.False(r.Columns[1].IsPadded);

      Assert.Equal(schemaName, r.Columns[2].SchemaName);
      Assert.Equal("test", r.Columns[2].TableName);
      Assert.Equal("test", r.Columns[2].TableLabel);
      Assert.Equal("c", r.Columns[2].ColumnName);
      Assert.Equal("c", r.Columns[2].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[2].Type);
      Assert.Equal(20u, r.Columns[2].Length);
      Assert.Equal(0u, r.Columns[2].FractionalDigits);
      Assert.False(r.Columns[2].IsNumberSigned);
      Assert.Equal("utf8mb4", r.Columns[2].CharacterSetName);
      Assert.Equal("utf8mb4_general_ci", r.Columns[2].CollationName);
      Assert.False(r.Columns[2].IsPadded);
      //Assert.Equal("Δ", rows[0][2]);
    }

    [Fact]
    public void SchemaDefaultCharset()
    {
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255))");
      ExecuteSQL("INSERT INTO test VALUES('CAR')");

      var defaultValues = ExecuteSQLStatement(GetSession(true).SQL("SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME " +
        $"FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{schemaName}'; ")).FetchAll();

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select("b"));
      var rows = r.FetchAll();

      Assert.Equal(schemaName, r.Columns[0].SchemaName);
      Assert.Equal("test", r.Columns[0].TableName);
      Assert.Equal("test", r.Columns[0].TableLabel);
      Assert.Equal("b", r.Columns[0].ColumnName);
      Assert.Equal("b", r.Columns[0].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[0].Type);
      Assert.Equal(0u, r.Columns[0].FractionalDigits);
      Assert.Equal(false, r.Columns[0].IsNumberSigned);
      Assert.Equal(defaultValues[0][0], r.Columns[0].CharacterSetName);
      Assert.Equal(defaultValues[0][1], r.Columns[0].CollationName);
      Assert.Equal(false, r.Columns[0].IsPadded);
      Assert.Equal("CAR", rows[0][0]);
      
      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        if (connection.driver.Version.isAtLeast(8,0,1))
          Assert.Equal(1020u, r.Columns[0].Length);
        else
          Assert.Equal(255u, r.Columns[0].Length);
      }
    }

    [Fact]
    public void ColumnNames()
    {
      ExecuteSQL("CREATE TABLE test(columnA VARCHAR(255), columnB INT, columnX BIT)");
      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select());

      Assert.Equal(3, r.ColumnCount);
      Assert.Equal(r.Columns.Count, r.ColumnCount);
      Assert.Equal("columnA", r.ColumnNames[0]);
      Assert.Equal("columnB", r.ColumnNames[1]);
      Assert.Equal("columnX", r.ColumnNames[2]);
    }

#if !NETCOREAPP2_2
        [Fact]
    public void TableDefaultCharset()
    {
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255)) CHARSET greek");
      ExecuteSQL("INSERT INTO test VALUES('Δ')");

      RowResult r = ExecuteSelectStatement(GetSession().GetSchema(schemaName).GetTable("test").Select("b"));
      var rows = r.FetchAll();

      Assert.Equal(schemaName, r.Columns[0].SchemaName);
      Assert.Equal("test", r.Columns[0].TableName);
      Assert.Equal("test", r.Columns[0].TableLabel);
      Assert.Equal("b", r.Columns[0].ColumnName);
      Assert.Equal("b", r.Columns[0].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[0].Type);
      Assert.Equal(255u, r.Columns[0].Length);
      Assert.Equal(0u, r.Columns[0].FractionalDigits);
      Assert.False(r.Columns[0].IsNumberSigned);
      Assert.Equal("utf8mb4", r.Columns[0].CharacterSetName);
      Assert.Equal("utf8mb4_general_ci", r.Columns[0].CollationName);
      Assert.False(r.Columns[0].IsPadded);
      Assert.Equal("Δ", rows[0][0]);
    }
#endif
    }
}
