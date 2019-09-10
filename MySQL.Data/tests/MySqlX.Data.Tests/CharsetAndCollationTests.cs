// Copyright (c) 2017, 2018, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using System.Collections.Generic;
using Xunit;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// Charset and collation related tests.
  /// </summary>
  public class CharsetAndCollationTests : BaseTest
  {
    private static DBVersion _serverVersion;

    static CharsetAndCollationTests()
    {
      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        _serverVersion = connection.driver.Version;
      }
    }

    [Fact]
    public void DefaultCharSet()
    {
      if (!_serverVersion.isAtLeast(8,0,1)) return;

      using (var session = MySQLX.GetSession(ConnectionString))
      {
        Assert.Equal("utf8mb4", session.Settings.CharacterSet);
      }

      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand("SHOW VARIABLES LIKE 'character_set_connection'", connection);
        MySqlDataReader reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal("utf8mb4", reader.GetString("Value"));
        reader.Close();

        cmd.CommandText = "SHOW VARIABLES LIKE 'character_set_database'";
        reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal("utf8mb4", reader.GetString("Value"));
        reader.Close();

        cmd.CommandText = "SHOW VARIABLES LIKE 'character_set_server'";
        reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal("utf8mb4", reader.GetString("Value"));
        reader.Close();

        cmd.CommandText = "SHOW VARIABLES LIKE 'collation_%'";
        reader = cmd.ExecuteReader();
        while (reader.Read())
        {
          Assert.Equal("utf8mb4_0900_ai_ci", reader.GetString("Value"));
        }
        reader.Close();
      }
    }

    [Fact]
    public void ValidateCollationMapList()
    {
      if (!_serverVersion.isAtLeast(8,0,1)) return;

      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        var command = new MySqlCommand("SELECT id, collation_name FROM INFORMATION_SCHEMA.COLLATIONS",connection);
        var reader = command.ExecuteReader();
        Assert.True(reader.HasRows);

        while(reader.Read())
        {
          var id = reader.GetInt32("id");
          var collationName = reader.GetString("collation_name");
          Assert.Equal(CollationMap.GetCollationName(id), collationName);
        }

        connection.Close();
      }
    }

    /// <summary>
    /// Bug #26163694 SELECT WITH/WO PARAMS(DIFF COMB) N PROC CALL FAIL WITH KEY NOT FOUND EX-WL#10561
    /// </summary>
    [Fact]
    public void Utf8mb4CharsetExists()
    {
      if (!_serverVersion.isAtLeast(8,0,1)) return;

      using (Session session = MySQLX.GetSession(ConnectionString))
      {
        // Search utf8mb4 database.
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE id = 255"));
        Assert.True(result.HasData);
        var data = result.FetchOne();
        Assert.Equal("utf8mb4_0900_ai_ci",data.GetString("Collation"));

        // Check in CollationMap.
        Assert.Equal("utf8mb4_0900_ai_ci", CollationMap.GetCollationName(255));
      }
    }

    /// <summary>
    /// Bug #26163703 SHOW COLLATION FAILS WITH MYSQL SERVER 8.0-WL#10561
    /// </summary>
    [Fact]
    public void IllegalMixCollations()
    {
      using (Session session = MySQLX.GetSession(ConnectionString))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.True(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=latin1"))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.True(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=utf8mb4"))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.True(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=utf-8"))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.True(result.HasData);
      }
    }
	
	/// <summary>
    /// Bug #26163678 VIEW.SELECT RETURNS TYPE INSTEAD OF THE TABLE-WL#10561
    /// Bug #26163667 COLLECTIONS.NAME RETURNS TYPE INSTEAD OF THE NAME OF THE COLLECTION-WL#10561
    /// </summary>
    [Fact]
    public void NamesAreReturnedAsStrings()
    {
      using (Session mySession = new Session(ConnectionString))
      {
        Schema test = mySession.GetSchema("test");

        ExecuteSQL("CREATE TABLE test1(id1 int,firstname varchar(20))");
        ExecuteSQL("INSERT INTO test1 values ('1','Rob')");
        ExecuteSQL("INSERT INTO test1 values ('2','Steve')");
        ExecuteSQL("CREATE TABLE test2(id2 int,lastname varchar(20))");
        ExecuteSQL("INSERT INTO test2 values ('1','Williams')");
        ExecuteSQL("INSERT INTO test2 values ('2','Waugh')");
        ExecuteSQL("CREATE VIEW view1 AS select * from test.test1");
        ExecuteSQL("SELECT * FROM view1");
        ExecuteSQL("CREATE VIEW view2 AS select * from test.test2");
        ExecuteSQL("SELECT * FROM view2");

        List<Table> tables = test.GetTables();
        Assert.Equal(4,tables.Count);
        Assert.Equal(2,tables.FindAll(i => !i.IsView).Count);
        Assert.Equal(2, tables.FindAll(i => i.IsView).Count);
        ExecuteSelectStatement(tables[0].Select());
        ExecuteSelectStatement(tables[1].Select());
        ExecuteSelectStatement(tables[2].Select());
        ExecuteSelectStatement(tables[3].Select());
        Assert.Equal("test1", tables[0].Name);
        Assert.Equal("test2", tables[1].Name);
        Assert.Equal("view1", tables[2].Name);
        Assert.Equal("view2", tables[3].Name);

        Table table = test.GetTable("test2");
        Assert.Equal("test2", table.Name);

        Collection c = test.CreateCollection("coll");

        List<Collection> collections = test.GetCollections();
        Assert.Single(collections);
        Assert.Equal("coll", collections[0].Name);

        Collection collection = test.GetCollection("coll");
        Assert.Equal("coll", collection.Name);
      }
    }
  }
}
