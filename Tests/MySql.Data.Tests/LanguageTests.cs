// Copyright © 2004, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.IO;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class LanguageTests : BaseTest
  {
    [Test]
    public void Unicode()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test (u2 varchar(255) CHARACTER SET ucs2)");

      using (MySqlConnection c = new MySqlConnection(conn.ConnectionString + ";charset=utf8"))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES ( CONVERT('困巫忘否役' using ucs2))", c);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          string s1 = reader.GetString(0);
          Assert.AreEqual("困巫忘否役", s1);
        }
      }
    }

    /// <summary>
    /// Bug #13806  	Does not support Code Page 932
    /// </summary>
    [Test]
    public void CP932()
    {
      using (MySqlConnection c = new MySqlConnection(GetConnectionString(true) + ";charset=cp932"))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT '涯割晦叶角'", c);
        string s = (string)cmd.ExecuteScalar();
        Assert.AreEqual("涯割晦叶角", s);
      }
    }

    [Test]
    public void UTF8()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET utf8)");

      using (MySqlConnection c = new MySqlConnection(conn.ConnectionString + ";charset=utf8"))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'ЁЄЉҖҚ')", c); //russian
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
    }

    [Test]
    public void UTF8PreparedAndUsingParameters()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test (name VARCHAR(200) CHAR SET utf8)");

      using (MySqlConnection c = new MySqlConnection(conn.ConnectionString + ";charset=utf8"))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(?val)", c);
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
    }

    [Test]
    public void Chinese()
    {
      if (Version < new Version(4, 1)) return;

      using (MySqlConnection c = new MySqlConnection(conn.ConnectionString + ";charset=utf8"))
      {
        c.Open();

        execSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET big5, name2 VARCHAR(200) CHAR SET gb2312)");

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, '困巫忘否役', '涝搞谷侪魍' )", c);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.AreEqual("困巫忘否役", reader.GetString(1));
          Assert.AreEqual("涝搞谷侪魍", reader.GetString(2));
        }
      }
    }

    [Test]
    public void Turkish()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET latin5 )");

      using (MySqlConnection c = new MySqlConnection(conn.ConnectionString + ";charset=utf8"))
      {
        c.Open();


        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'ĞËÇÄŞ')", c);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.AreEqual("ĞËÇÄŞ", reader.GetString(1));
        }
      }
    }

    [Test]
    public void Russian()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test (id int, name VARCHAR(200) CHAR SET cp1251)");

      using (MySqlConnection c = new MySqlConnection(conn.ConnectionString + ";charset=utf8"))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'щьеи')", c);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.AreEqual("щьеи", reader.GetString(1));
        }
      }
    }

    [Test]
    public void VariousCollations()
    {
      if (Version < new Version(4, 1)) return;

      createTable(@"CREATE TABLE `test_tbl`(`test` VARCHAR(255) NOT NULL) 
                            CHARACTER SET utf8 COLLATE utf8_swedish_ci", "MYISAM");
      execSQL("INSERT INTO test_tbl VALUES ('myval')");
      MySqlCommand cmd = new MySqlCommand("SELECT test FROM test_tbl", conn);
      cmd.ExecuteScalar();
    }

    /// <summary>
    /// Bug #25651 SELECT does not work properly when WHERE contains UTF-8 characters 
    /// </summary>
    [Test]
    public void UTF8Parameters()
    {
      execSQL("CREATE TABLE test (id int(11) NOT NULL, " +
          "value varchar(100) NOT NULL, PRIMARY KEY (id)) " +
          "ENGINE=MyISAM DEFAULT CHARSET=utf8");

      string conString = GetConnectionString(true) + ";charset=utf8";
      using (MySqlConnection con = new MySqlConnection(conString))
      {
        con.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES (1, 'šđčćžŠĐČĆŽ')", con);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT id FROM test WHERE value =  ?parameter";
        cmd.Parameters.Add("?parameter", MySqlDbType.VarString);
        cmd.Parameters[0].Value = "šđčćžŠĐČĆŽ";
        object o = cmd.ExecuteScalar();
        Assert.AreEqual(1, o);
      }
    }
  }
}
