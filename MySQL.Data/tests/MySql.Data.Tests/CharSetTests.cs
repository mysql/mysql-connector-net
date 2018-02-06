// Copyright © 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System;
using Xunit;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class CharSetTests : TestBase
  {
    public CharSetTests(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void UseFunctions()
    {
      executeSQL("CREATE TABLE Test (valid char, UserCode varchar(100), password varchar(100)) CHARSET latin1");

      using (var conn = new MySqlConnection(Connection.ConnectionString + ";charset=latin1"))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT valid FROM Test WHERE Valid = 'Y' AND " +
          "UserCode = 'username' AND Password = AES_ENCRYPT('Password','abc')", conn);
        cmd.ExecuteScalar();
      }
    }

    [Fact]
    public void VarBinary()
    {
      executeSQL("CREATE TABLE Test (id int, name varchar(200) collate utf8_bin) charset utf8");
      executeSQL("INSERT INTO Test VALUES (1, 'Test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        object o = reader.GetValue(1);
        Assert.True(o is string);
      }
    }

    [Fact]
    public void Latin1Connection()
    {
      executeSQL("CREATE TABLE Test (id INT, name VARCHAR(200)) CHARSET latin1");
      executeSQL("INSERT INTO Test VALUES( 1, _latin1 'Test')");

      using (var conn = new MySqlConnection(Connection.ConnectionString + ";charset=latin1"))
      {
        conn.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test WHERE name LIKE 'Test'", conn);
        object id = cmd.ExecuteScalar();
        Assert.Equal(1, id);
      }
    }

    /// <summary>
    /// Bug #40076	"Functions Return String" option does not set the proper encoding for the string
    /// </summary>
    [Fact]
    public void FunctionReturnsStringWithCharSet()
    {
      string connStr = Connection.ConnectionString + ";functions return string=true";
      using (var conn = new MySqlConnection(connStr))
      {
        conn.Open();

        MySqlCommand cmd = new MySqlCommand(
          "SELECT CONCAT('Trädgårdsvägen', 1)", conn);

        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.Equal("Trädgårdsvägen1", reader.GetString(0));
        }
      }
    }


#if !NETCOREAPP1_1
    [Fact]
    public void RespectBinaryFlags()
    {
      if (Connection.driver.Version.isAtLeast(5,5,0)) return;

      string connStr = Connection.ConnectionString + ";respect binary flags=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT CONCAT('Trädgårdsvägen', 1)", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.True(dt.Rows[0][0] is byte[]);
      }
      connStr = Connection.ConnectionString + ";respect binary flags=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT CONCAT('Trädgårdsvägen', 1)", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.True(dt.Rows[0][0] is string);
        Assert.Equal("Trädgårdsvägen1", dt.Rows[0][0]);
      }
    }
#endif

    [Fact]
    public void RussianErrorMessagesShowCorrectly()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW VARIABLES LIKE '%lc_messages'", Root);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        if (!reader.GetString(1).Equals("ru_RU"))
        {
          Console.Error.WriteLine("This test requires starting the server with Russian language.");
          return;
        }
      }

      string expected = "У вас ошибка в запросе. Изучите документацию по используемой версии MySQL на предмет корректного синтаксиса около 'query with error' на строке 1";
      try
      {
        string connectionString = Connection.ConnectionString + "; Character Set=cp1251";
        MySqlHelper.ExecuteNonQuery(connectionString, "query with error");
      }
      catch (MySqlException e)
      {
        Assert.Equal(expected, e.Message);
      }
    }



    /// <summary>
    /// Test for fix of Connector/NET cannot read data from a MySql table using UTF-16/UTF-32
    /// (MySql bug #69169, Oracle bug #16776818).
    /// </summary>
    [Fact]
    public void UsingUtf16()
    {
      executeSQL(@"CREATE TABLE Test (
        `actor_id` smallint(5) unsigned NOT NULL DEFAULT '0',
        `first_name` varchar(45) NOT NULL,
        `last_name` varchar(45) NOT NULL,
        `last_update` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00'
        ) ENGINE=InnoDB DEFAULT CHARSET=utf16");

      string[] firstNames = new string[] { "PENELOPE", "NICK", "ED" };
      string[] lastNames = new string[] { "GUINESS", "WAHLBERG", "CHASE" };
      DateTime[] lastUpdates = new DateTime[] {
          new DateTime(2006, 2, 15, 4, 34, 33), new DateTime(2007, 2, 15, 4, 34, 33), new DateTime(2008, 4, 15, 4, 34, 33) };
      for (int i = 0; i < firstNames.Length; i++)
      {
        string sql2 = String.Format(
          "INSERT INTO Test( actor_id, first_name, last_name, last_update ) values ( {0}, '{1}', '{2}', '{3}' )",
          i, firstNames[i], lastNames[i], lastUpdates[i].ToString("yyyy/MM/dd hh:mm:ss"));
        executeSQL(sql2);
      }

      string sql = "select actor_id, first_name, last_name, last_update from Test";

      using (var reader = ExecuteReader(sql))
      {
        int j = 0;
        while (reader.Read())
        {
          for (int i = 0; i < reader.FieldCount; i++)
          {
            Assert.True(j == reader.GetInt32(0));
            Assert.True(firstNames[j] == reader.GetString(1));
            Assert.True(lastNames[j] == reader.GetString(2));
            Assert.True(lastUpdates[j] == reader.GetDateTime(3));
          }
          j++;
        }
      }
    }

    /// <summary>
    /// 2nd part of tests for fix of Connector/NET cannot read data from a MySql table using UTF-16/UTF-32
    /// (MySql bug #69169, Oracle bug #16776818).
    /// </summary>
    [Fact]
    public void UsingUtf32()
    {
      executeSQL(@"CREATE TABLE `Test` (
          `actor_id` smallint(5) unsigned NOT NULL DEFAULT '0',
          `first_name` varchar(45) NOT NULL,
          `last_name` varchar(45) NOT NULL,
          `last_update` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00'
          ) ENGINE=InnoDB DEFAULT CHARSET=utf32");

      string[] firstNames = new string[] { "PENELOPE", "NICK", "ED" };
      string[] lastNames = new string[] { "GUINESS", "WAHLBERG", "CHASE" };
      DateTime[] lastUpdates = new DateTime[] {
          new DateTime(2006, 2, 15, 4, 34, 33), new DateTime(2007, 2, 15, 4, 34, 33), new DateTime(2008, 4, 15, 4, 34, 33) };
      for (int i = 0; i < firstNames.Length; i++)
      {
        string sql2 = string.Format(
          "insert into `Test`( actor_id, first_name, last_name, last_update ) values ( {0}, '{1}', '{2}', '{3}' )",
          i, firstNames[i], lastNames[i], lastUpdates[i].ToString("yyyy/MM/dd hh:mm:ss"));
        executeSQL(sql2);
      }

      string sql = "select actor_id, first_name, last_name, last_update from `Test`";

      using (var reader = ExecuteReader(sql))
      {
        int j = 0;
        while (reader.Read())
        {
          for (int i = 0; i < reader.FieldCount; i++)
          {
            Assert.True(j == reader.GetInt32(0));
            Assert.True(firstNames[j] == reader.GetString(1));
            Assert.True(lastNames[j] == reader.GetString(2));
            Assert.True(lastUpdates[j] == reader.GetDateTime(3));
          }
          j++;
        }
      }
    }



    /// <summary>
    /// Test for new functionality on 5.7.9 supporting chinese character sets gb18030
    /// WL #4024
    /// (Oracle bug #21098546).
    /// Disabled due to intermittent failure. Documented under Oracle bug #27010958
    /// </summary>
    [Fact (Skip="Fix this")]
    public void CanInsertChineseCharacterSetGB18030()
    {
      if (Fixture.Version < new Version(5, 7, 4)) return;

      executeSQL("CREATE TABLE Test (id int, name VARCHAR(100) CHAR SET gb18030, KEY(name(20)))");
      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString + ";charset=gb18030"))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, '㭋玤䂜蚌')", c);
        cmd.ExecuteNonQuery();
        cmd = new MySqlCommand("INSERT INTO Test VALUES(2, 0xC4EEC5ABBDBFA1A4B3E0B1DABBB3B9C520A1A4CBD5B6ABC6C2)", c);
        cmd.ExecuteNonQuery();
        cmd = new MySqlCommand("SELECT id, name from Test", c);
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
          if (reader.GetUInt32(0) == 1)
            Assert.Equal("㭋玤䂜蚌", reader.GetString(1));
          if (reader.GetUInt32(0) == 2)
            Assert.Equal("念奴娇·赤壁怀古 ·苏东坡", reader.GetString(1));
        }
      }
    }

    /// <summary>
    /// Test for new functionality on 5.7.9 supporting chinese character sets on gb18030
    /// WL #4024
    /// (Oracle bug #21098546).
    /// Disabled due to intermittent failure. Documented under Oracle bug #27010958
    /// </summary>
    [Fact (Skip = "Fix this")]
    public void CanCreateDbUsingChineseCharacterSetGB18030()
    {
      if (Fixture.Version < new Version(5, 7, 4)) return;

      MySqlConnectionStringBuilder rootSb = new MySqlConnectionStringBuilder(Root.ConnectionString);
      rootSb.CharacterSet = "gb18030";
      using (MySqlConnection rootConnection = new MySqlConnection(rootSb.ToString()))
      {
        string database = "㭋玤䂜蚌";

        rootConnection.Open();
        MySqlCommand rootCommand = new MySqlCommand();
        rootCommand.Connection = rootConnection;
        rootCommand.CommandText = string.Format("CREATE DATABASE `{0}` CHARSET=gb18030;", database);
        rootCommand.ExecuteNonQuery();

        try
        {
          rootSb.Database = database;
          using (MySqlConnection conn = new MySqlConnection(rootSb.ConnectionString))
          {
            conn.Open();
            Assert.Equal(database, conn.Database);
          }
        }
        finally
        {
          if (rootConnection.State == ConnectionState.Open)
          {
            rootCommand.CommandText = string.Format("DROP DATABASE `{0}`;", database);
            rootCommand.ExecuteNonQuery();
          }
        }
      }
    }

    [Fact]
    public void UTF16LETest()
    {
      if (Fixture.Version < new Version(5, 6)) return;

      using (MySqlDataReader reader = ExecuteReader("select _utf16le 'utf16le test';"))
      {
        while (reader.Read())
        {
          Assert.Equal("瑵ㅦ氶⁥整瑳", reader[0].ToString());
        }
      }
    }

    /// <summary>
    /// Bug #13806  	Does not support Code Page 932
    /// </summary>
    [Fact]
    public void CP932()
    {
      using (var connection = new MySqlConnection(Connection.ConnectionString + ";charset=cp932"))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT '涯割晦叶角'", connection);
        string s = (string)cmd.ExecuteScalar();
        Assert.Equal("涯割晦叶角", s);
      }
    }

    [Fact]
    public void VariousCollations()
    {
      executeSQL(@"CREATE TABLE Test(`test` VARCHAR(255) NOT NULL) 
                            CHARACTER SET utf8 COLLATE utf8_swedish_ci");
      executeSQL("INSERT INTO Test VALUES ('myval')");
      MySqlCommand cmd = new MySqlCommand("SELECT test FROM Test", Connection);
      cmd.ExecuteScalar();
    }

    [Fact (Skip = "Fix for 8.0.5")]
    public void ExtendedCharsetOnConnection()
    {
      MySqlConnectionStringBuilder rootSb = new MySqlConnectionStringBuilder(Root.ConnectionString);
      rootSb.CharacterSet = "utf8";
      using (MySqlConnection rootConnection = new MySqlConnection(rootSb.ToString()))
      {
        string database = "数据库";
        string user = "用户";
        string password = "tést€";

        rootConnection.Open();
        MySqlCommand rootCommand = new MySqlCommand();
        rootCommand.Connection = rootConnection;
        rootCommand.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`;", database);
        rootCommand.CommandText += string.Format("GRANT ALL ON `{0}`.* to '{1}'@'localhost' identified by '{2}';", database, user, password);
        rootCommand.ExecuteNonQuery();

        string connString = Connection.ConnectionString;
        MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(connString);
        sb.Database = database;
        sb.UserID = user;
        sb.Password = password;
        sb.CharacterSet = "utf8";
        try
        {
          using (MySqlConnection conn = new MySqlConnection(sb.ToString()))
          {
            conn.Open();
            Assert.Equal(database, conn.Database);
          }
        }
        finally
        {
          if (rootConnection.State == ConnectionState.Open)
          {
            rootCommand.CommandText = string.Format("DROP DATABASE `{0}`;DROP USER '{1}'@'localhost'", database, user);
            rootCommand.ExecuteNonQuery();
          }
        }
      }
    }

    [Fact]
    public void DefaultCharSet()
    {
      using (var connection = new MySqlConnection(Connection.ConnectionString))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand("SHOW VARIABLES LIKE 'character_set_connection'", connection);
        MySqlDataReader reader = cmd.ExecuteReader();
        reader.Read();

        if (Connection.driver.Version.isAtLeast(8,0,1))
          Assert.Equal("utf8mb4", reader.GetString("Value"));
        else
          Assert.Equal("latin1", reader.GetString("Value"));
      }
    }

    [Fact]
    public void CharacterVariablesByDefault()
    {
      MySqlConnectionStringBuilder rootSb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      rootSb.CharacterSet = string.Empty;
      using (MySqlConnection rootConnection = new MySqlConnection(rootSb.ToString()))
      {
        rootConnection.Open();
        MySqlCommand cmd = rootConnection.CreateCommand();
        cmd.CommandText = "SELECT @@character_set_server";
        string characterSet = cmd.ExecuteScalar().ToString();
        Assert.False(string.IsNullOrWhiteSpace(characterSet));

        cmd.CommandText = "SHOW VARIABLES LIKE 'character_set_c%'";
        using (MySqlDataReader dr = cmd.ExecuteReader())
        {
          Assert.True(dr.HasRows);
          while (dr.Read())
          {
            switch (dr.GetString(0).ToLowerInvariant())
            {
              case "character_set_client":
                Assert.Equal(characterSet, dr.GetString(1));
                break;
              case "character_set_connection":
                Assert.Equal(characterSet, dr.GetString(1));
                break;
              default:
                throw new InvalidOperationException(string.Format("Variable '{0}' not expected.", dr.GetString(0)));
            }
          }
        }

        cmd.CommandText = "SELECT @@character_set_results";
        Assert.Equal(DBNull.Value, cmd.ExecuteScalar());
      }
    }
  }
}
