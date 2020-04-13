// Copyright (c) 2013, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Text;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class CharSetUTF8Tests : TestBase
  {
    public CharSetUTF8Tests(TestFixture fixture) : base(fixture)
    {
    }

    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.CharacterSet = "utf8";
    }

    [Fact]
    public void UTF8BlogsTruncating()
    {

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test (name LONGTEXT) CHARSET utf8");

      string szParam = "test:éàçùêû";
      string szSQL = "INSERT INTO test Values (?monParametre)";

      MySqlCommand cmd = new MySqlCommand(szSQL, Connection);
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

    /// <summary>
    /// Bug #14592 Wrong column length returned for VARCHAR UTF8 columns 
    /// </summary>
    [Fact(Skip = "Not compatible with linux")]
    public void GetSchemaOnUTF8()
    {

      executeSQL("CREATE TABLE Test(name VARCHAR(40) NOT NULL, name2 VARCHAR(20)) " +
        "CHARACTER SET utf8");
      executeSQL("INSERT INTO Test VALUES('Test', 'Test')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.Equal(40, dt.Rows[0]["ColumnSize"]);
        Assert.Equal(20, dt.Rows[1]["ColumnSize"]);
      }
    }

    /// <summary>
    /// Bug #31117  	Connector/NET exceptions do not support server charset
    /// </summary>
    [Fact]
    public void NonLatin1Exception()
    {
      executeSQL("CREATE TABLE Test (id int)");

      MySqlCommand cmd = new MySqlCommand("select `Numéro` from Test", Connection);
      var exception = Record.Exception(() => cmd.ExecuteScalar());
      Assert.NotNull(exception);
      Assert.Equal("Unknown column 'Numéro' in 'field list'", exception.Message);
    }

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
      MySqlCommand cmd = new MySqlCommand("select * from t62094", Connection);
      MySqlDataAdapter ad = new MySqlDataAdapter(cmd);
      DataSet ds = new DataSet();
      ad.Fill(ds);
      ad.FillSchema(ds, SchemaType.Mapped);
      Assert.Equal(1, ds.Tables[0].Columns["name"].MaxLength);
      Assert.Equal(20, ds.Tables[0].Columns["longname"].MaxLength);
    }

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
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?p1, ?p2, ?p3, ?p4, ?p5, ?p5)", Connection);
      cmd.Parameters.AddWithValue("?p1", utf8_bytes);
      cmd.Parameters.AddWithValue("?p2", utf8_bytes);
      cmd.Parameters.AddWithValue("?p3", utf8_bytes);
      cmd.Parameters.AddWithValue("?p4", utf8_bytes);
      cmd.Parameters.AddWithValue("?p5", utf8_bytes);
      cmd.Parameters.AddWithValue("?p6", utf8_bytes);
      cmd.ExecuteNonQuery();

      // now check that the on/off is working
      string connStr = Connection.ConnectionString + ";Treat Blobs As UTF8=yes;BlobAsUTF8IncludePattern=.*";
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
      connStr = Connection.ConnectionString + ";Treat Blobs As UTF8=yes;BlobAsUTF8ExcludePattern=exclude.*";
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
      connStr = Connection.ConnectionString + ";Treat Blobs As UTF8=yes;BlobAsUTF8IncludePattern=include.*";
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
      string connStr = Root.ConnectionString + ";charset=utf8;pooling=false";
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

    [Fact]
    public void Unicode()
    {
      executeSQL("CREATE TABLE Test (u2 varchar(255) CHARACTER SET ucs2)");

      executeSQL("INSERT INTO Test VALUES ( CONVERT('困巫忘否役' using ucs2))");

      using (MySqlDataReader reader = ExecuteReader("SELECT * FROM Test"))
      {
        reader.Read();
        string s1 = reader.GetString(0);
        Assert.Equal("困巫忘否役", s1);
      }
    }

    [Fact]
    public void UTF8()
    {
      executeSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET utf8)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'ЁЄЉҖҚ')", Connection); //russian
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test VALUES(2, '兣冘凥凷冋')";	// simplified Chinese
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test VALUES(3, '困巫忘否役')";	// traditional Chinese
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test VALUES(4, '涯割晦叶角')";	// Japanese
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test VALUES(5, 'ברחפע')";		// Hebrew
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test VALUES(6, 'ψόβΩΞ')";		// Greek
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test VALUES(7, 'þðüçöÝÞÐÜÇÖ')";	// Turkish
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test VALUES(8, 'ฅๆษ')";			// Thai
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("ЁЄЉҖҚ", reader.GetString(1));
        reader.Read();
        Assert.Equal("兣冘凥凷冋", reader.GetString(1));
        reader.Read();
        Assert.Equal("困巫忘否役", reader.GetString(1));
        reader.Read();
        Assert.Equal("涯割晦叶角", reader.GetString(1));
        reader.Read();
        Assert.Equal("ברחפע", reader.GetString(1));
        reader.Read();
        Assert.Equal("ψόβΩΞ", reader.GetString(1));
        reader.Read();
        Assert.Equal("þðüçöÝÞÐÜÇÖ", reader.GetString(1));
        reader.Read();
        Assert.Equal("ฅๆษ", reader.GetString(1));
      }
    }

    [Fact]
    public void UTF8PreparedAndUsingParameters()
    {
      executeSQL("CREATE TABLE Test (name VARCHAR(200) CHAR SET utf8)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(?val)", Connection);
      cmd.Parameters.Add("?val", MySqlDbType.VarChar);
      cmd.Prepare();

      cmd.Parameters[0].Value = "ЁЄЉҖҚ";			// Russian
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = "兣冘凥凷冋";		// simplified Chinese
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = "困巫忘否役";		// traditional Chinese
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = "涯割晦叶角";		// Japanese
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = "ברחפע";			// Hebrew
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = "ψόβΩΞ";			// Greek
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = "þðüçöÝÞÐÜÇÖ";	// Turkish
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = "ฅๆษ";				// Thai
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("ЁЄЉҖҚ", reader.GetString(0));
        reader.Read();
        Assert.Equal("兣冘凥凷冋", reader.GetString(0));
        reader.Read();
        Assert.Equal("困巫忘否役", reader.GetString(0));
        reader.Read();
        Assert.Equal("涯割晦叶角", reader.GetString(0));
        reader.Read();
        Assert.Equal("ברחפע", reader.GetString(0));
        reader.Read();
        Assert.Equal("ψόβΩΞ", reader.GetString(0));
        reader.Read();
        Assert.Equal("þðüçöÝÞÐÜÇÖ", reader.GetString(0));
        reader.Read();
        Assert.Equal("ฅๆษ", reader.GetString(0));
      }
    }

#if !(NETCOREAPP2_2 || NETCOREAPP3_0)
    [Fact]
    public void Chinese()
    {
      executeSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET big5, name2 VARCHAR(200) CHAR SET gb2312)");

      executeSQL("INSERT INTO Test VALUES(1, '困巫忘否役', '涝搞谷侪魍' )");

      using (MySqlDataReader reader = ExecuteReader("SELECT * FROM Test"))
      {
        reader.Read();
        Assert.Equal("困巫忘否役", reader.GetString(1));
        Assert.Equal("涝搞谷侪魍", reader.GetString(2));
      }
    }

    [Fact]
    public void Russian()
    {
      executeSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET cp1251)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'щьеи')", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("щьеи", reader.GetString(1));
      }
    }
#endif

    /// <summary>
    /// Bug #25651 SELECT does not work properly when WHERE contains UTF-8 characters 
    /// </summary>
    [Fact]
    public void UTF8Parameters()
    {
      executeSQL("CREATE TABLE Test (id int(11) NOT NULL, " +
          "value varchar(100) NOT NULL, PRIMARY KEY (id)) " +
          "ENGINE=MyISAM DEFAULT CHARSET=utf8");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, 'šđčćžŠĐČĆŽ')", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id FROM Test WHERE value =  ?parameter";
      cmd.Parameters.Add("?parameter", MySqlDbType.VarString);
      cmd.Parameters[0].Value = "šđčćžŠĐČĆŽ";
      object o = cmd.ExecuteScalar();
      Assert.Equal(1, o);
    }

#if !(NETCOREAPP2_2 || NETCOREAPP3_0)
    [Fact]
    public void Turkish()
    {
      executeSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET latin5 )");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'ĞËÇÄŞ')", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("ĞËÇÄŞ", reader.GetString(1));
      }
    }
#endif
  }
}
