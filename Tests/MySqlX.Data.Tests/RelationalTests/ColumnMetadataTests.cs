// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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

      RowResult r = GetSession().GetSchema(schemaName).GetTable("test").Select("1 + 1 as a", "b", "c").Execute();
      var rows = r.FetchAll();
      Assert.Equal(3, r.Columns.Count);
      //Assert.Null(r.Columns[0].DatabaseName);
      Assert.Null(r.Columns[0].SchemaName);
      Assert.Null(r.Columns[0].TableName);
      Assert.Null(r.Columns[0].TableLabel);
      Assert.Null(r.Columns[0].ColumnName);
      Assert.Equal("a", r.Columns[0].ColumnLabel);
      Assert.Equal(ColumnType.Tinyint, r.Columns[0].Type);
      Assert.Equal(3u, r.Columns[0].Length);
      Assert.Equal(0u, r.Columns[0].FractionalDigits);
      Assert.Equal(true, r.Columns[0].IsNumberSigned);
      Assert.Null(r.Columns[0].CharacterSetName);
      Assert.Null(r.Columns[0].CollationName);
      Assert.Equal(false, r.Columns[0].IsPadded);

      Assert.Equal(schemaName, r.Columns[1].SchemaName);
      Assert.Equal("test", r.Columns[1].TableName);
      Assert.Equal("test", r.Columns[1].TableLabel);
      Assert.Equal("b", r.Columns[1].ColumnName);
      Assert.Equal("b", r.Columns[1].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[1].Type);
      Assert.Equal(255u, r.Columns[1].Length);
      Assert.Equal(0u, r.Columns[1].FractionalDigits);
      Assert.Equal(false, r.Columns[1].IsNumberSigned);
      Assert.Equal("latin1", r.Columns[1].CharacterSetName);
      Assert.Equal("latin1_swedish_ci", r.Columns[1].CollationName);
      Assert.Equal(false, r.Columns[1].IsPadded);

      Assert.Equal(schemaName, r.Columns[2].SchemaName);
      Assert.Equal("test", r.Columns[2].TableName);
      Assert.Equal("test", r.Columns[2].TableLabel);
      Assert.Equal("c", r.Columns[2].ColumnName);
      Assert.Equal("c", r.Columns[2].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[2].Type);
      Assert.Equal(20u, r.Columns[2].Length);
      Assert.Equal(0u, r.Columns[2].FractionalDigits);
      Assert.Equal(false, r.Columns[2].IsNumberSigned);
      Assert.Equal("greek", r.Columns[2].CharacterSetName);
      Assert.Equal("greek_general_ci", r.Columns[2].CollationName);
      Assert.Equal(false, r.Columns[2].IsPadded);
      Assert.Equal("Δ", rows[0][2]);
    }

    [Fact]
    public void SchemaDefaultCharset()
    {
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255))");
      ExecuteSQL("INSERT INTO test VALUES('CAR')");

      var defaultValues = GetNodeSession().SQL("SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME " +
        $"FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{schemaName}'; ").Execute().FetchAll();

      RowResult r = GetSession().GetSchema(schemaName).GetTable("test").Select("b").Execute();
      var rows = r.FetchAll();

      Assert.Equal(schemaName, r.Columns[0].SchemaName);
      Assert.Equal("test", r.Columns[0].TableName);
      Assert.Equal("test", r.Columns[0].TableLabel);
      Assert.Equal("b", r.Columns[0].ColumnName);
      Assert.Equal("b", r.Columns[0].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[0].Type);
      Assert.Equal(255u, r.Columns[0].Length);
      Assert.Equal(0u, r.Columns[0].FractionalDigits);
      Assert.Equal(false, r.Columns[0].IsNumberSigned);
      Assert.Equal(defaultValues[0][0], r.Columns[0].CharacterSetName);
      Assert.Equal(defaultValues[0][1], r.Columns[0].CollationName);
      Assert.Equal(false, r.Columns[0].IsPadded);
      Assert.Equal("CAR", rows[0][0]);
    }

    [Fact]
    public void TableDefaultCharset()
    {
      ExecuteSQL("CREATE TABLE test(b VARCHAR(255)) CHARSET greek");
      ExecuteSQL("INSERT INTO test VALUES('Δ')");

      RowResult r = GetSession().GetSchema(schemaName).GetTable("test").Select("b").Execute();
      var rows = r.FetchAll();

      Assert.Equal(schemaName, r.Columns[0].SchemaName);
      Assert.Equal("test", r.Columns[0].TableName);
      Assert.Equal("test", r.Columns[0].TableLabel);
      Assert.Equal("b", r.Columns[0].ColumnName);
      Assert.Equal("b", r.Columns[0].ColumnLabel);
      Assert.Equal(ColumnType.String, r.Columns[0].Type);
      Assert.Equal(255u, r.Columns[0].Length);
      Assert.Equal(0u, r.Columns[0].FractionalDigits);
      Assert.Equal(false, r.Columns[0].IsNumberSigned);
      Assert.Equal("greek", r.Columns[0].CharacterSetName);
      Assert.Equal("greek_general_ci", r.Columns[0].CollationName);
      Assert.Equal(false, r.Columns[0].IsPadded);
      Assert.Equal("Δ", rows[0][0]);
    }
  }
}
