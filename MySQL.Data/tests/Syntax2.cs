// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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

using Xunit;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class Syntax2 : TestBase
  {
    public Syntax2(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void CommentsInSQL()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      string sql = "INSERT INTO Test /* my table */ VALUES (1 /* this is the id */, 'Test' );" +
        "/* These next inserts are just for testing \r\n" +
        "   comments */\r\n" +
        "INSERT INTO \r\n" +
        "  # This table is bogus\r\n" +
        "Test VALUES (2, 'Test2')";

      
      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      TestDataTable table = new TestDataTable();
      da.Fill(table);
      Assert.Equal(1, table.Rows[0]["id"]);
      Assert.Equal("Test", table.Rows[0]["name"]);
      Assert.Equal(2, table.Rows.Count);
      Assert.Equal(2, table.Rows[1]["id"]);
      Assert.Equal("Test2", table.Rows[1]["name"]);
    }

    [Fact]
    public void LastInsertid()
    {
      executeSQL("CREATE TABLE Test(id int auto_increment, name varchar(20), primary key(id))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(NULL, 'test')", Connection);
      cmd.ExecuteNonQuery();
      Assert.Equal(1, cmd.LastInsertedId);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }
      Assert.Equal(2, cmd.LastInsertedId);

      cmd.CommandText = "SELECT id FROM Test";
      cmd.ExecuteScalar();
      Assert.Equal(-1, cmd.LastInsertedId);
    }

    [Fact]
    public void ParsingBugTest()
    {
      executeSQL(@"CREATE FUNCTION `TestFunction`(A INTEGER (11), B INTEGER (11), C VARCHAR (20)) 
          RETURNS int(11)
          RETURN 1");

      MySqlCommand command = new MySqlCommand("TestFunction", Connection);
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
    [Fact]
    public void EscapedBackslash()
    {
      executeSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");

      MySqlCommand cmd = new MySqlCommand(@"INSERT INTO Test VALUES (1, '\\=\\')", Connection);
      cmd.ExecuteNonQuery();
    }
  }
}
