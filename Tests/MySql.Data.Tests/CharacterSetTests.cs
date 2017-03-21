// Copyright © 2013, 2016 Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class CharacterSetTests : TestBase
  {
    protected TestSetup ts;

    public CharacterSetTests(TestSetup setup) : base(setup, "characterset")
    {
      ts = setup;
    }

    [Fact]
    public void UseFunctions()
    {
      executeSQL("CREATE TABLE Test (valid char, UserCode varchar(100), password varchar(100)) CHARSET latin1");

      MySqlConnection c = new MySqlConnection(ts.GetConnection(false).ConnectionString + ";charset=latin1");
      c.Open();
      MySqlCommand cmd = new MySqlCommand("SELECT valid FROM Test WHERE Valid = 'Y' AND " +
        "UserCode = 'username' AND Password = AES_ENCRYPT('Password','abc')", c);
      cmd.ExecuteScalar();
      c.Close();
    }

    [Fact]
    public void VarBinary()
    {
      executeSQL("CREATE TABLE test (id int, name varchar(200) collate utf8_bin) charset utf8");
      executeSQL("INSERT INTO test VALUES (1, 'Test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", connection);
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

      MySqlConnection c = new MySqlConnection(ts.GetConnection(false).ConnectionString + ";charset=latin1");
      c.Open();

      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test WHERE name LIKE 'Test'", c);
      object id = cmd.ExecuteScalar();
      Assert.Equal(1, id);
      c.Close();
    }

#if !NETCORE10
    /// <summary>
    /// Bug #14592 Wrong column length returned for VARCHAR UTF8 columns 
    /// </summary>
    [Fact]
    public void GetSchemaOnUTF8()
    {

      executeSQL("CREATE TABLE Test(name VARCHAR(40) NOT NULL, name2 VARCHAR(20)) " +
        "CHARACTER SET utf8");
      executeSQL("INSERT INTO Test VALUES('Test', 'Test')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.Equal(40, dt.Rows[0]["ColumnSize"]);
        Assert.Equal(20, dt.Rows[1]["ColumnSize"]);
      }
    }
#endif

    [Fact]
    public void UTF8BlogsTruncating()
    {

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test (name LONGTEXT) CHARSET utf8");

      string szParam = "test:éàçùêû";
      string szSQL = "INSERT INTO test Values (?monParametre)";

      string connStr = ts.GetConnection(false).ConnectionString + ";charset=utf8";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand(szSQL, c);
        cmd.Parameters.Add(new MySqlParameter("?monParametre", MySqlDbType.VarChar));
        cmd.Parameters[0].Value = szParam;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM test";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          string s = reader.GetString(0);
          Assert.Equal(szParam, s);
        }
      }
    }

#if !NETCORE10

    [Fact]
    public void BlobAsUtf8()
    {
      executeSQL(@"CREATE TABLE Test(include_blob BLOB, include_tinyblob TINYBLOB, 
            include_longblob LONGBLOB, exclude_tinyblob TINYBLOB, exclude_blob BLOB, 
            exclude_longblob LONGBLOB)");

      byte[] utf8_bytes = new byte[4] { 0xf0, 0x90, 0x80, 0x80 };
      Encoding utf8 = Encoding.GetEncoding("UTF-8");
      string utf8_string = utf8.GetString(utf8_bytes, 0, utf8_bytes.Length);

      // insert our UTF-8 bytes into the table
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?p1, ?p2, ?p3, ?p4, ?p5, ?p5)", connection);
      cmd.Parameters.AddWithValue("?p1", utf8_bytes);
      cmd.Parameters.AddWithValue("?p2", utf8_bytes);
      cmd.Parameters.AddWithValue("?p3", utf8_bytes);
      cmd.Parameters.AddWithValue("?p4", utf8_bytes);
      cmd.Parameters.AddWithValue("?p5", utf8_bytes);
      cmd.Parameters.AddWithValue("?p6", utf8_bytes);
      cmd.ExecuteNonQuery();

      // now check that the on/off is working
      string connStr = ts.GetConnection(false).ConnectionString + ";Treat Blobs As UTF8=yes;BlobAsUTF8IncludePattern=.*";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        foreach (DataColumn col in dt.Columns)
        {
          Assert.Equal(typeof(string), col.DataType);
          string s = (string)dt.Rows[0][0];
          byte[] b = utf8.GetBytes(s);
          Assert.Equal(utf8_string, dt.Rows[0][col.Ordinal].ToString());
        }
      }

      // now check that exclusion works
      connStr = ts.GetConnection(false).ConnectionString + ";Treat Blobs As UTF8=yes;BlobAsUTF8ExcludePattern=exclude.*";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        foreach (DataColumn col in dt.Columns)
        {
          if (col.ColumnName.StartsWith("exclude", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(typeof(byte[]), col.DataType);
          else
          {
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal(utf8_string, dt.Rows[0][col.Ordinal].ToString());
          }
        }
      }

      // now check that inclusion works
      connStr = ts.GetConnection(false).ConnectionString + ";Treat Blobs As UTF8=yes;BlobAsUTF8IncludePattern=include.*";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        foreach (DataColumn col in dt.Columns)
        {
          if (col.ColumnName.StartsWith("include", StringComparison.OrdinalIgnoreCase))
          {
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal(utf8_string, dt.Rows[0][col.Ordinal].ToString());
          }
          else
            Assert.Equal(typeof(byte[]), col.DataType);
        }
      }
    }



    /// <summary>
    /// Bug #31185  	columns names are incorrect when using the 'AS' clause and name with accents
    /// Bug #38721  	GetOrdinal doesn't accept column names accepted by MySQL 5.0
    /// </summary>
    [Fact]
    public void UTF8AsColumnNames()
    {
      string connStr = ts.GetConnection(true).ConnectionString + ";charset=utf8;pooling=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlDataAdapter da = new MySqlDataAdapter("select now() as 'Numéro'", c);
        DataTable dt = new DataTable();
        da.Fill(dt);

        Assert.Equal("Numéro", dt.Columns[0].ColumnName);

        MySqlCommand cmd = new MySqlCommand("SELECT NOW() AS 'Numéro'", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          int ord = reader.GetOrdinal("Numéro");
          Assert.Equal(0, ord);
        }
      }
    }

#endif

    /// <summary>
    /// Bug #31117  	Connector/Net exceptions do not support server charset
    /// </summary>
    [Fact]
    public void NonLatin1Exception()
    {
      string connStr = ts.GetConnection(false).ConnectionString + ";charset=utf8";

      executeSQL("CREATE TABLE Test (id int)");

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        try
        {
          MySqlCommand cmd = new MySqlCommand("select `Numéro` from Test", c);
          cmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
          Assert.Equal("Unknown column 'Numéro' in 'field list'", ex.Message);
        }
      }
    }

    /// <summary>
    /// Bug #40076	"Functions Return String" option does not set the proper encoding for the string
    /// </summary>
    [Fact]
    public void FunctionReturnsStringWithCharSet()
    {
      string connStr = ts.GetConnection(true).ConnectionString + ";functions return string=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand(
          "SELECT CONCAT('Trädgårdsvägen', 1)", c);

        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.Equal("Trädgårdsvägen1", reader.GetString(0));
        }
      }
    }


#if !NETCORE10
    [Fact]
    public void RespectBinaryFlags()
    {
      if (ts.version.Major >= 5 && ts.version.Minor >= 5) return;

      string connStr = ts.GetConnection(true).ConnectionString + ";respect binary flags=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT CONCAT('Trädgårdsvägen', 1)", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.True(dt.Rows[0][0] is byte[]);
      }
      connStr = ts.GetConnection(true).ConnectionString + ";respect binary flags=false";
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
      if (ts.version < new Version(5, 5))
        return;

      string connectionString = ts.GetConnection(true).ConnectionString;
      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand("SHOW VARIABLES LIKE '%lc_messages'", connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          if (!reader.GetString(1).Equals("ru_RU"))
          {
            Console.Error.WriteLine("This test requires starting the server with Russian language.");
            return;
          }
        }
      }

      connectionString += "; Character Set=cp1251";
      string expected = "У вас ошибка в запросе. Изучите документацию по используемой версии MySQL на предмет корректного синтаксиса около 'query with error' на строке 1";

      try
      {
        MySqlHelper.ExecuteNonQuery(connectionString, "query with error");
      }
      catch (MySqlException e)
      {
        Assert.Equal(expected, e.Message);
      }
    }


#if !NETCORE10
    /// <summary>
    /// Tests for bug http://bugs.mysql.com/bug.php?id=62094
    /// (char field mapped to System.String of MaxLength=3*len(char) in .NET/Connector).
    /// </summary>
    [Fact]
    public void GetCharLengthInUTF8()
    {
      executeSQL(
        @"CREATE TABLE `t62094` ( `id` int(11) NOT NULL, `name` char(1) DEFAULT NULL, 
                `longname` char(20) DEFAULT NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8;");
      MySqlCommand cmd = new MySqlCommand("select * from t62094", connection);
      MySqlDataAdapter ad = new MySqlDataAdapter(cmd);
      DataSet ds = new DataSet();
      ad.Fill(ds);
      ad.FillSchema(ds, SchemaType.Mapped);
      Assert.Equal(1, ds.Tables[0].Columns["name"].MaxLength);
      Assert.Equal(20, ds.Tables[0].Columns["longname"].MaxLength);
    }
#endif

    /// <summary>
    /// Test for fix of Connector/NET cannot read data from a MySql table using UTF-16/UTF-32
    /// (MySql bug #69169, Oracle bug #16776818).
    /// </summary>
    [Fact]
    public void UsingUtf16()
    {
      MySqlConnection con = new MySqlConnection(ts.GetConnection(false).ConnectionString);
      con.Open();
      try
      {
        MySqlCommand cmd = new MySqlCommand("", con);
        cmd.CommandText = "drop table if exists `actor`";
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"CREATE TABLE `actor` (
    `actor_id` smallint(5) unsigned NOT NULL DEFAULT '0',
    `first_name` varchar(45) NOT NULL,
    `last_name` varchar(45) NOT NULL,
    `last_update` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00'
  ) ENGINE=InnoDB DEFAULT CHARSET=utf16";
        cmd.ExecuteNonQuery();

        string[] firstNames = new string[] { "PENELOPE", "NICK", "ED" };
        string[] lastNames = new string[] { "GUINESS", "WAHLBERG", "CHASE" };
        DateTime[] lastUpdates = new DateTime[] {
          new DateTime(2006, 2, 15, 4, 34, 33), new DateTime(2007, 2, 15, 4, 34, 33), new DateTime(2008, 4, 15, 4, 34, 33) };
        for (int i = 0; i < firstNames.Length; i++)
        {
          cmd.CommandText = string.Format(
            "insert into `actor`( actor_id, first_name, last_name, last_update ) values ( {0}, '{1}', '{2}', '{3}' )",
            i, firstNames[i], lastNames[i], lastUpdates[i].ToString("yyyy/MM/dd hh:mm:ss"));
          cmd.ExecuteNonQuery();
        }

        cmd.CommandText = "select actor_id, first_name, last_name, last_update from `actor`";

        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          int j = 0;
          while (r.Read())
          {
            for (int i = 0; i < r.FieldCount; i++)
            {
              Assert.True(j == r.GetInt32(0));
              Assert.True(firstNames[j] == r.GetString(1));
              Assert.True(lastNames[j] == r.GetString(2));
              Assert.True(lastUpdates[j] == r.GetDateTime(3));
            }
            j++;
          }
        }
      }
      finally
      {
        MySqlCommand cmd = new MySqlCommand("drop table if exists `actor`", con);
        cmd.ExecuteNonQuery();
        con.Close();
      }
    }

    /// <summary>
    /// 2nd part of tests for fix of Connector/NET cannot read data from a MySql table using UTF-16/UTF-32
    /// (MySql bug #69169, Oracle bug #16776818).
    /// </summary>
    [Fact]
    public void UsingUtf32()
    {
      MySqlConnection con = new MySqlConnection(ts.GetConnection(false).ConnectionString);
      con.Open();
      try
      {
        MySqlCommand cmd = new MySqlCommand("", con);
        cmd.CommandText = "drop table if exists `actor`";
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"CREATE TABLE `actor` (
    `actor_id` smallint(5) unsigned NOT NULL DEFAULT '0',
    `first_name` varchar(45) NOT NULL,
    `last_name` varchar(45) NOT NULL,
    `last_update` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00'
  ) ENGINE=InnoDB DEFAULT CHARSET=utf32";
        cmd.ExecuteNonQuery();

        string[] firstNames = new string[] { "PENELOPE", "NICK", "ED" };
        string[] lastNames = new string[] { "GUINESS", "WAHLBERG", "CHASE" };
        DateTime[] lastUpdates = new DateTime[] {
          new DateTime(2006, 2, 15, 4, 34, 33), new DateTime(2007, 2, 15, 4, 34, 33), new DateTime(2008, 4, 15, 4, 34, 33) };
        for (int i = 0; i < firstNames.Length; i++)
        {
          cmd.CommandText = string.Format(
            "insert into `actor`( actor_id, first_name, last_name, last_update ) values ( {0}, '{1}', '{2}', '{3}' )",
            i, firstNames[i], lastNames[i], lastUpdates[i].ToString("yyyy/MM/dd hh:mm:ss"));
          cmd.ExecuteNonQuery();
        }

        cmd.CommandText = "select actor_id, first_name, last_name, last_update from `actor`";

        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          int j = 0;
          while (r.Read())
          {
            for (int i = 0; i < r.FieldCount; i++)
            {
              Assert.True(j == r.GetInt32(0));
              Assert.True(firstNames[j] == r.GetString(1));
              Assert.True(lastNames[j] == r.GetString(2));
              Assert.True(lastUpdates[j] == r.GetDateTime(3));
            }
            j++;
          }
        }
      }
      finally
      {
        MySqlCommand cmd = new MySqlCommand("drop table if exists `actor`", con);
        cmd.ExecuteNonQuery();
        con.Close();
      }
    }



    /// <summary>
    /// Test for new functionality on 5.7.9 supporting chinese character sets gb18030
    /// WL #4024
    /// (Oracle bug #21098546).
    /// </summary>
    [Fact]
    public void CanInsertChineseCharacterSetGB18030()
    {
      if (ts.version < new Version(5, 7, 4)) return;

      try
      {
        executeSQL("CREATE TABLE Test (id int, name VARCHAR(100) CHAR SET gb18030, KEY(name(20)))");
        using (MySqlConnection c = new MySqlConnection(ts.GetConnection(false).ConnectionString + ";charset=gb18030"))
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
      finally
      {
        executeSQL("drop table if exists `Test`");
      }
    }



    /// <summary>
    /// Test for new functionality on 5.7.9 supporting chinese character sets on gb18030
    /// WL #4024
    /// (Oracle bug #21098546).
    /// </summary>
    [Fact]
    public void CanCreateDbUsingChineseCharacterSetGB18030()
    {
      if (ts.version < new Version(5, 7, 4)) return;

      MySqlConnectionStringBuilder rootSb = new MySqlConnectionStringBuilder(ts.GetConnection(true).ConnectionString);
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

    public override void Dispose()
    {
      executeSQL("DROP TABLE IF EXISTS TEST");
      executeSQL("DROP TABLE IF EXISTS t62094");
      base.Dispose();
    }

    [Fact]
    public void UTF16LETest()
    {
      if (ts.version < new Version(5, 6)) return;

      using (MySqlDataReader reader = ExecuteAsReader("select _utf16le 'utf16le test';", connection))
      {
        while (reader.Read())
        {
          Assert.Equal("瑵ㅦ氶⁥整瑳", reader[0].ToString());
        }
      }
    }

#if !NETCORE10
    [Fact]
    public void GEOSTD8Test()
    {
      MySqlConnection dbconn = new MySqlConnection(ts.GetConnection(false).ConnectionString);
      try
      {
        using (MySqlCommand cmd = new MySqlCommand("select _geostd8 'geostd8 test';", dbconn))
        {
          dbconn.Open();
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              Assert.Equal("geostd8 test", reader[0].ToString());
            }
          }
        }
        throw new Exception("The test should have failed with a MySqlException but it does not.");
      }
      catch (MySqlException ex)
      {
        while (ex.InnerException != null)
          ex = (MySqlException)ex.InnerException;

        Assert.Equal(typeof(MySqlException), ex.GetType());
      }
      catch (Exception ex)
      {
        Assert.Equal(typeof(MySqlException), ex.GetType());
      }
      finally
      {
        dbconn.Close();
      }
    }
#endif

    [Fact]
    public void ExtendedCharsetOnConnection()
    {
      MySqlConnectionStringBuilder rootSb = new MySqlConnectionStringBuilder(ts.GetConnection(true).ConnectionString);
      rootSb.CharacterSet = "utf8";
      using (MySqlConnection rootConnection = new MySqlConnection(rootSb.ToString()))
      {
        string database = "数据库";
        string user = "用户";
        string password = "test";

        rootConnection.Open();
        MySqlCommand rootCommand = new MySqlCommand();
        rootCommand.Connection = rootConnection;
        rootCommand.CommandText = string.Format("CREATE DATABASE `{0}`;", database);
        rootCommand.CommandText += string.Format("GRANT ALL ON `{0}`.* to '{1}'@'localhost' identified by '{2}';", database, user, password);
        rootCommand.ExecuteNonQuery();

        string connString = ts.GetConnection(false).ConnectionString;
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
  }
}
