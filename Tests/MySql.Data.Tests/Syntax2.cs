// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
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
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class Syntax2 : BaseTest
  {
    public Syntax2()
    {
      csAdditions += ";logging=true;";
    }

    [Test]
    public void CommentsInSQL()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      string sql = "INSERT INTO Test /* my table */ VALUES (1 /* this is the id */, 'Test' );" +
        "/* These next inserts are just for testing \r\n" +
        "   comments */\r\n" +
        "INSERT INTO \r\n" +
        "  # This table is bogus\r\n" +
        "Test VALUES (2, 'Test2')";
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable table = new DataTable();
      da.Fill(table);
      Assert.AreEqual(1, table.Rows[0]["id"]);
      Assert.AreEqual("Test", table.Rows[0]["name"]);
      Assert.AreEqual(2, table.Rows.Count);
      Assert.AreEqual(2, table.Rows[1]["id"]);
      Assert.AreEqual("Test2", table.Rows[1]["name"]);
    }

    [Test]
    public void LastInsertid()
    {
      execSQL("CREATE TABLE Test(id int auto_increment, name varchar(20), primary key(id))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(NULL, 'test')", conn);
      cmd.ExecuteNonQuery();
      Assert.AreEqual(1, cmd.LastInsertedId);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }
      Assert.AreEqual(2, cmd.LastInsertedId);

      cmd.CommandText = "SELECT id FROM Test";
      cmd.ExecuteScalar();
      Assert.AreEqual(-1, cmd.LastInsertedId);
    }

    [Test]
    public void ParsingBugTest()
    {
      if (Version.Major < 5) return;

      execSQL("DROP FUNCTION IF EXISTS `TestFunction`");
      execSQL(@"CREATE FUNCTION `TestFunction`(A INTEGER (11), B INTEGER (11), C VARCHAR (20)) 
				  RETURNS int(11)
				  RETURN 1");

      MySqlCommand command = new MySqlCommand("TestFunction", conn);
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = "TestFunction";
      command.Parameters.AddWithValue("@A", 1);
      command.Parameters.AddWithValue("@B", 2);
      command.Parameters.AddWithValue("@C", "test");
      command.Parameters.Add("@return", MySqlDbType.Int32).Direction = ParameterDirection.ReturnValue;
      command.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #44960	backslash in string - connector return exeption
    /// </summary>
    [Test]
    public void EscapedBackslash()
    {
      execSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");

      MySqlCommand cmd = new MySqlCommand(@"INSERT INTO Test VALUES (1, '\\=\\')", conn);
      cmd.ExecuteNonQuery();
    }
    /*        [Category("NotWorking")]
        [Test]
        public void TestCase()
        {
          string importQuery = "SET FOREIGN_KEY_CHECKS = 1;DELETE FROM Category " +
            "WHERE id=\'0205342903\';SET FOREIGN_KEY_CHECKS = 0;INSERT INTO Category " +
            "VALUES(\'d0450f050a0dfd8e00e6da7bda3bb07e\',\'0205342903\',\'000000000000000 " +
            "00000000000000000\',\'\',\'0\');INSERT INTO Attribute " +
            "VALUES(\'d0450f050a0dfd8e00e6da7b00dfa3c5\',\'d0450f050a0dfd8e00e6da7bda3bb0 " +
            "7e\',\'eType\',\'machine\',null);SET FOREIGN_KEY_CHECKS = 1;";
          string deleteQuery = "SET FOREIGN_KEY_CHECKS=1;DELETE FROM Attribute " +
            "WHERE foreignuuid=\'d0450f050a0dfd8e00e6da7bda3bb07e\' AND " +
            "name=\'eType\'";
          string insertQuery = "SET FOREIGN_KEY_CHECKS = 0;INSERT INTO Attribute " +
            "VALUES(\'d0563ba70a0dfd8e01df43e22395b352\',\'d0450f050a0dfd8e00e6da7bda3bb0 " +
            "7e\',\'eType\',\'machine\',null);SET FOREIGN_KEY_CHECKS = 1";
          string updateQuery = "SET FOREIGN_KEY_CHECKS = 1;DELETE FROM Attribute " +
            "WHERE foreignuuid=\'d0450f050a0dfd8e00e6da7bda3bb07e\' AND " + 
            "name=\'eType\';SET FOREIGN_KEY_CHECKS = 0;INSERT INTO Attribute " + 
            "VALUES(\'d0563ba70a0dfd8e01df43e22395b352\',\'d0450f050a0dfd8e00e6da7bda3bb0 " +
            "7e\',\'eType\',\'machine\',null);SET FOREIGN_KEY_CHECKS = 1;";
          string bugQuery = "SELECT name,value FROM Attribute WHERE " +
            "foreignuuid=\'d0450f050a0dfd8e00e6da7bda3bb07e\'";

          execSQL("SET FOREIGN_KEY_CHECKS=0");
          execSQL("DROP TABLE IF EXISTS Attribute");
          execSQL("CREATE TABLE IF NOT EXISTS Attribute (uuid char(32) NOT NULL," +
            "foreignuuid char(32), name character varying(254), value character varying(254)," +
            "fid integer, PRIMARY KEY (uuid), INDEX foreignuuid (foreignuuid), " +
            "INDEX name (name(16)), INDEX value (value(8)), CONSTRAINT `attribute_fk_1` " +
            "FOREIGN KEY (`foreignuuid`) REFERENCES `Category` (`uuid`) ON DELETE CASCADE" +
            ") CHARACTER SET utf8 ENGINE=InnoDB;");

          execSQL("DROP TABLE IF EXISTS Category");
          execSQL("CREATE TABLE IF NOT EXISTS Category (uuid char(32) NOT NULL," +
            "id character varying(254), parentuuid char(32), name character varying(254)," +
            "sort integer, PRIMARY KEY (uuid), INDEX parentuuid (parentuuid), INDEX id (id)," +
            "CONSTRAINT `parent_fk_1` FOREIGN KEY (`parentuuid`) REFERENCES `Category` " +
            "(`uuid`) ON DELETE CASCADE) CHARACTER SET utf8 ENGINE=InnoDB;");
          execSQL("SET FOREIGN_KEY_CHECKS=1");

          conn.InfoMessage += new MySqlInfoMessageEventHandler(conn_InfoMessage);
          MySqlCommand cmd = new MySqlCommand(importQuery, conn);
          cmd.ExecuteNonQuery();

          for (int i = 0; i <= 5000; i++)
          {
            cmd.CommandText = deleteQuery;
            cmd.ExecuteNonQuery();

            cmd.CommandText = insertQuery;
            cmd.ExecuteNonQuery();

            cmd.CommandText = bugQuery;
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
              reader.Close();
            }
          }
        }

        void conn_InfoMessage(object sender, MySqlInfoMessageEventArgs args)
        {
          throw new Exception("The method or operation is not implemented.");
        }
        */
  }
}
