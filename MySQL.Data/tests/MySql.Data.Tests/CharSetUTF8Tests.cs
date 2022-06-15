// Copyright (c) 2013, 2022, Oracle and/or its affiliates.
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
using NUnit.Framework;
using System;
using System.Data;
using System.Text;

namespace MySql.Data.MySqlClient.Tests
{
  public class CharSetUTF8Tests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.CharacterSet = "utf8";
    }

    [Test]
    public void UTF8BlogsTruncating()
    {

      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("CREATE TABLE test (name LONGTEXT) CHARSET utf8");

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
        Assert.AreEqual(szParam, s);
      }
    }

    /// <summary>
    /// Bug #14592 Wrong column length returned for VARCHAR UTF8 columns
    /// </summary>
    [Test]
    public void GetSchemaOnUTF8()
    {
#if !NETFRAMEWORK
      if (!Platform.IsWindows()) Assert.Ignore();
#endif
      ExecuteSQL("CREATE TABLE Test(name VARCHAR(40) NOT NULL, name2 VARCHAR(20)) " +
        "CHARACTER SET utf8");
      ExecuteSQL("INSERT INTO Test VALUES('Test', 'Test')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.AreEqual(40, dt.Rows[0]["ColumnSize"]);
        Assert.AreEqual(20, dt.Rows[1]["ColumnSize"]);
      }
    }

    /// <summary>
    /// Bug #31117  	Connector/NET exceptions do not support server charset
    /// </summary>
    [Test]
    public void NonLatin1Exception()
    {
      ExecuteSQL("CREATE TABLE Test (id int)");

      MySqlCommand cmd = new MySqlCommand("select `Numéro` from Test", Connection);
      var exception = Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
      Assert.AreEqual("Unknown column 'Numéro' in 'field list'", exception.Message);
    }

    /// <summary>
    /// Tests for bug http://bugs.mysql.com/bug.php?id=62094
    /// (char field mapped to System.String of MaxLength=3*len(char) in .NET/Connector).
    /// </summary>
    [Test]
    public void GetCharLengthInUTF8()
    {
      ExecuteSQL(
        @"CREATE TABLE `t62094` ( `id` int(11) NOT NULL, `name` char(1) DEFAULT NULL, 
                `longname` char(20) DEFAULT NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8;");
      MySqlCommand cmd = new MySqlCommand("select * from t62094", Connection);
      MySqlDataAdapter ad = new MySqlDataAdapter(cmd);
      DataSet ds = new DataSet();
      ad.Fill(ds);
      ad.FillSchema(ds, SchemaType.Mapped);
      Assert.AreEqual(1, ds.Tables[0].Columns["name"].MaxLength);
      Assert.AreEqual(20, ds.Tables[0].Columns["longname"].MaxLength);
    }

    [Test]
    public void BlobAsUtf8()
    {
      ExecuteSQL(@"CREATE TABLE Test(include_blob BLOB, include_tinyblob TINYBLOB, 
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
          Assert.AreEqual(typeof(string), col.DataType);
          string s = (string)dt.Rows[0][0];
          byte[] b = utf8.GetBytes(s);
          Assert.AreEqual(utf8_string, dt.Rows[0][col.Ordinal].ToString());
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
            Assert.AreEqual(typeof(byte[]), col.DataType);
          else
          {
            Assert.AreEqual(typeof(string), col.DataType);
            Assert.AreEqual(utf8_string, dt.Rows[0][col.Ordinal].ToString());
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
            Assert.AreEqual(typeof(string), col.DataType);
            Assert.AreEqual(utf8_string, dt.Rows[0][col.Ordinal].ToString());
          }
          else
            Assert.AreEqual(typeof(byte[]), col.DataType);
        }
      }
    }

    /// <summary>
    /// Bug #31185 columns names are incorrect when using the 'AS' clause and name with accents
    /// Bug #38721 GetOrdinal doesn't accept column names accepted by MySQL 5.0
    /// </summary>
    [Test]
    public void UTF8AsColumnNames()
    {
      string connStr = Root.ConnectionString + ";charset=utf8;pooling=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlDataAdapter da = new MySqlDataAdapter("select now() as 'Numéro'", c);
        DataTable dt = new DataTable();
        da.Fill(dt);

        Assert.AreEqual("Numéro", dt.Columns[0].ColumnName);

        MySqlCommand cmd = new MySqlCommand("SELECT NOW() AS 'Numéro'", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          int ord = reader.GetOrdinal("Numéro");
          Assert.AreEqual(0, ord);
        }
      }
    }

    [Test]
    public void Unicode()
    {
      ExecuteSQL("CREATE TABLE Test (u2 varchar(255) CHARACTER SET ucs2)");

      ExecuteSQL("INSERT INTO Test VALUES ( CONVERT('困巫忘否役' using ucs2))");

      using (MySqlDataReader reader = ExecuteReader("SELECT * FROM Test"))
      {
        reader.Read();
        string s1 = reader.GetString(0);
        Assert.AreEqual("困巫忘否役", s1);
      }
    }

    [Test]
    public void UTF8()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET utf8)");

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
        Assert.AreEqual("ЁЄЉҖҚ", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("兣冘凥凷冋", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("困巫忘否役", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("涯割晦叶角", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("ברחפע", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("ψόβΩΞ", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("þðüçöÝÞÐÜÇÖ", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("ฅๆษ", reader.GetString(1));
      }
    }

    [Test]
    public void UTF8PreparedAndUsingParameters()
    {
      ExecuteSQL("CREATE TABLE Test (name VARCHAR(200) CHAR SET utf8)");

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
        Assert.AreEqual("ЁЄЉҖҚ", reader.GetString(0));
        reader.Read();
        Assert.AreEqual("兣冘凥凷冋", reader.GetString(0));
        reader.Read();
        Assert.AreEqual("困巫忘否役", reader.GetString(0));
        reader.Read();
        Assert.AreEqual("涯割晦叶角", reader.GetString(0));
        reader.Read();
        Assert.AreEqual("ברחפע", reader.GetString(0));
        reader.Read();
        Assert.AreEqual("ψόβΩΞ", reader.GetString(0));
        reader.Read();
        Assert.AreEqual("þðüçöÝÞÐÜÇÖ", reader.GetString(0));
        reader.Read();
        Assert.AreEqual("ฅๆษ", reader.GetString(0));
      }
    }

    [Test]
    public void Chinese()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET big5, name2 VARCHAR(200) CHAR SET gb2312)");

      ExecuteSQL("INSERT INTO Test VALUES(1, '困巫忘否役', '涝搞谷侪魍' )");

      using (MySqlDataReader reader = ExecuteReader("SELECT * FROM Test"))
      {
        reader.Read();
        Assert.AreEqual("困巫忘否役", reader.GetString(1));
        Assert.AreEqual("涝搞谷侪魍", reader.GetString(2));
      }
    }

    [Test]
    public void Russian()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET cp1251)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'щьеи')", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual("щьеи", reader.GetString(1));
      }
    }

    /// <summary>
    /// Bug #25651 SELECT does not work properly when WHERE contains UTF-8 characters
    /// </summary>
    [Test]
    public void UTF8Parameters()
    {
      ExecuteSQL("CREATE TABLE Test (id int(11) NOT NULL, " +
          "value varchar(100) NOT NULL, PRIMARY KEY (id)) " +
          "ENGINE=MyISAM DEFAULT CHARSET=utf8");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, 'šđčćžŠĐČĆŽ')", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id FROM Test WHERE value =  ?parameter";
      cmd.Parameters.Add("?parameter", MySqlDbType.VarString);
      cmd.Parameters[0].Value = "šđčćžŠĐČĆŽ";
      object o = cmd.ExecuteScalar();
      Assert.AreEqual(1, o);
    }

    [Test]
    public void Turkish()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET latin5 )");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'ĞËÇÄŞ')", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual("ĞËÇÄŞ", reader.GetString(1));
      }
    }
  }
}
