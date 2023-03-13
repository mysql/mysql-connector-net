// Copyright (c) 2013, 2023, Oracle and/or its affiliates.
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

using NUnit.Framework;
using System;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Tests
{
  public class CmdTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL($"DROP TABLE IF EXISTS `{Connection.Database}`.TestForeignKey");
      ExecuteSQL($"DROP TABLE IF EXISTS `{Connection.Database}`.Test");
    }

    /// <summary>
    /// Tests for MySql bug #64633 - System.InvalidCastException when executing a stored function.
    /// </summary>
    [Test]
    public void InvalidCast()
    {
      string host = Host == "localhost" ? Host : "%";
      ExecuteSQL($@"CREATE FUNCTION `{Connection.Database}`.`MyTwice`( val int ) RETURNS INT DETERMINISTIC
                                  READS SQL DATA BEGIN return val * 2; END;", true);
      ExecuteSQL($@"CREATE PROCEDURE `{Connection.Database}`.`spMyTwice`( out result int, val int ) 
                                  DETERMINISTIC READS SQL DATA BEGIN set result = val * 2; END;", true);
      string user = CreateUser("1", "123");
      ExecuteSQL($"GRANT EXECUTE ON FUNCTION `{Connection.Database}`.`MyTwice` TO '{user}'@'{host}';", true);
      ExecuteSQL($"GRANT EXECUTE ON PROCEDURE `{Connection.Database}`.`spMyTwice` TO '{user}'@'{host}'", true);

      if (Connection.driver.Version.isAtLeast(8, 0, 1))
        ExecuteSQL($"GRANT SELECT ON TABLE mysql.db TO '{user}'@'{host}'", true);
      else
        ExecuteSQL($"GRANT SELECT ON TABLE mysql.proc TO '{user}'@'{host}'", true);

      ExecuteSQL("FLUSH PRIVILEGES", true);

      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.UserID = user;
      connStr.Password = "123";
      MySqlConnection con = new MySqlConnection(connStr.GetConnectionString(true));

      // Invoke the function
      var cmd = con.CreateCommand();
      using (con)
      {
        con.Open();
        cmd.CommandText = "MyTwice";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter("val", System.DBNull.Value));
        cmd.Parameters.Add("@p", MySqlDbType.Int32);
        cmd.Parameters[1].Direction = ParameterDirection.ReturnValue;
        cmd.Parameters[0].Value = 20;
        cmd.ExecuteNonQuery();
        Assert.AreEqual(40, cmd.Parameters[1].Value);

        cmd.CommandText = "spMyTwice";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Clear();
        cmd.Parameters.Add(new MySqlParameter("result", System.DBNull.Value));
        cmd.Parameters.Add("val", MySqlDbType.Int32);
        cmd.Parameters[0].Direction = ParameterDirection.Output;
        cmd.Parameters[1].Value = 20;
        cmd.ExecuteNonQuery();
        Assert.AreEqual(40, cmd.Parameters[0].Value);
      }
    }

    [Test]
    public void InsertTest()
    {
      ExecuteSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      // do the insert
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES(10,'Test')", Connection);
      int cnt = cmd.ExecuteNonQuery();
      Assert.True(cnt == 1, "Insert Count");

      // make sure we get the right value back out
      cmd.CommandText = "SELECT name FROM Test WHERE id=10";
      string name = (string)cmd.ExecuteScalar();
      Assert.True(name == "Test", "Insert result");

      // now do the insert with parameters
      cmd.CommandText = "INSERT INTO Test (id,name) VALUES(?id, ?name)";
      cmd.Parameters.Add(new MySqlParameter("?id", 11));
      cmd.Parameters.Add(new MySqlParameter("?name", "Test2"));
      cnt = cmd.ExecuteNonQuery();
      Assert.True(cnt == 1, "Insert with Parameters Count");

      // make sure we get the right value back out
      cmd.Parameters.Clear();
      cmd.CommandText = "SELECT name FROM Test WHERE id=11";
      name = (string)cmd.ExecuteScalar();
      Assert.True(name == "Test2", "Insert with parameters result");
    }

    [Test]
    public void UpdateTest()
    {
      ExecuteSQL("CREATE TABLE test (id int NOT NULL, name VARCHAR(100))");
      ExecuteSQL("INSERT INTO test (id,name) VALUES(10, 'Test')");
      ExecuteSQL("INSERT INTO test (id,name) VALUES(11, 'Test2')");

      // do the update
      MySqlCommand cmd = new MySqlCommand("UPDATE test SET name='Test3' WHERE id=10 OR id=11", Connection);
      int cnt = cmd.ExecuteNonQuery();
      Assert.AreEqual(2, cnt);

      // make sure we get the right value back out
      cmd.CommandText = "SELECT name FROM test WHERE id=10";
      string name = (string)cmd.ExecuteScalar();
      Assert.AreEqual("Test3", name);

      cmd.CommandText = "SELECT name FROM test WHERE id=11";
      name = (string)cmd.ExecuteScalar();
      Assert.AreEqual("Test3", name);

      // now do the update with parameters
      cmd.CommandText = "UPDATE test SET name=?name WHERE id=?id";
      cmd.Parameters.Add(new MySqlParameter("?id", 11));
      cmd.Parameters.Add(new MySqlParameter("?name", "Test5"));
      cnt = cmd.ExecuteNonQuery();
      Assert.True(cnt == 1, "Update with Parameters Count");

      // make sure we get the right value back out
      cmd.Parameters.Clear();
      cmd.CommandText = "SELECT name FROM test WHERE id=11";
      name = (string)cmd.ExecuteScalar();
      Assert.AreEqual("Test5", name);

    }

    [Test]
    public void DeleteTest()
    {
      ExecuteSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES(1, 'Test')");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES(2, 'Test2')");

      // make sure we get the right value back out
      MySqlCommand cmd = new MySqlCommand("DELETE FROM Test WHERE id=1 or id=2", Connection);
      int delcnt = cmd.ExecuteNonQuery();
      Assert.AreEqual(2, delcnt);

      // find out how many rows we have now
      cmd.CommandText = "SELECT COUNT(*) FROM Test";
      object after_cnt = cmd.ExecuteScalar();
      Assert.AreEqual(0, Convert.ToInt32(after_cnt));
    }

    [Test]
    public void CtorTest()
    {
      ExecuteSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      MySqlTransaction txn = Connection.BeginTransaction();
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);

      MySqlCommand clone = new MySqlCommand(cmd.CommandText, (MySqlConnection)cmd.Connection,
        (MySqlTransaction)cmd.Transaction);
      clone.Parameters.AddWithValue("?test", 1);
      txn.Rollback();
    }

    [Test]
    [Ignore("Fix it!")]
    public void TableWithOVer100Columns()
    {
      string sql = "create table IF NOT EXISTS zvan (id int(8) primary key " +
        "unique auto_increment, name varchar(250)) ENGINE=INNODB;  ";
      /*				"create table IF NOT EXISTS ljudyna (id int(8) primary key " +
              "unique auto_increment, name varchar(250), data_narod date, " +
              "id_in_zvan int(8), kandidat varchar(250), tel_rob_vn varchar(250), " +
              "tel_rob_mis varchar(250), n_kabin varchar(250), n_nak_zvan varchar(250), " +
              "d_nak_zvan date, sex tinyint(1), n_nak_pos varchar(250), " +
              "d_nak_pos date, posad_data varchar(250), visl1 varchar(250), visl2 " +
              "varchar(250), visl3 varchar(250), cpidr_f int(8), cposad_f int(8), sumis " +
              "tinyint(1), zs_s date, zs_po date, ovs_z date, ovs_po date, naiavn_zviln " +
              "tinyint(1), ovs_z1 date, ovs_po1 date, ovs_z2 date, ovs_po2 date, ovs_z3 date, " +
              "ovs_po3 date, ovs_prakt varchar(250), data_atest date, data_sp date, v_akad_z " +
              "date, z_akad_zvln tinyint(1), v_akad_period varchar(250), nauk_stup " +
              "varchar(250), vch_zvan varchar(250), n_sprav varchar(250), n_posv varchar(250), " +
              "nacional varchar(250), osvita varchar(250), osvita_zakin_sho varchar(250), " +
              "osvita_zakin_koli date, osvita_special varchar(250), osvita_kvalifikac " +
              "varchar(250), de_navchaet varchar(250), data_vstupu date, termin_navch " +
              "varchar(250), adresa varchar(250), tel_dom varchar(250), marka_avto " +
              "varchar(250), n_avto varchar(250), color_avto varchar(250), vikor_avto " +
              "varchar(250), posv_avto varchar(250), marka_zbr varchar(250), nomer_calibr_zbr " +
              "varchar(250), vid_zbr varchar(250), nomer_data_razreshen varchar(250), pasport " +
              "varchar(250), oklad1 varchar(250), prem07_2003 varchar(250), nadb07_2003 " +
              "varchar(250), osob_nom varchar(250), nadbavka_stag_max varchar(250), " +
              "nadbavka_stag_08_2003 varchar(250), nadbavka_stag_10_2003 varchar(250), " +
              "nadbavka_stag_11_2003 varchar(250), nadbavka_stag_02_2004 varchar(250), " +
              "vidp_vikoristav varchar(250), vidp_plan varchar(250), vidp_vidgil varchar(250), " +
              "vidp_nevidgil_dniv varchar(250), nadb111 varchar(250), prem_3_1 varchar(250), " +
              "nadb_4_1 varchar(250), prem_3_2 varchar(250), nadb_3_2 varchar(250), nedolos " +
              "varchar(250), sposl int(8), cposl int(8), czaoh int(8), 07_2003_oklad " +
              "varchar(250), 05_2003_oklad varchar(250), deti_jeni varchar(250), nadb_volny " +
              "varchar(250), prem_volny varchar(250), dispanser tinyint(1), posl_spisok " +
              "tinyint(1), anketa_avtobiogr tinyint(1), photokartka tinyint(1), sp1 tinyint(1), " +
              "inshe varchar(250), oklad2 varchar(250), slugbova tinyint(1), atestuvan " +
              "varchar(250), 09_2004_oklad_vstan varchar(250), golosuvannia varchar(250), " +
              "stag_kalendar varchar(250), data_stag_kalendar varchar(250), medali " +
              "varchar(250), medali_mae varchar(250), visluga_cal_ovs_and_zs varchar(250), " +
              "FOREIGN KEY (id_in_zvan) REFERENCES zvan(id) ON DELETE CASCADE ON UPDATE " +
              "CASCADE) TYPE=INNODB DEFAULT CHARACTER SET cp1251 COLLATE cp1251_ukrainian_ci";
              */

      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #12245  	using Prepare() on an insert command causes null parameters to convert to "0"
    /// </summary>
    [Test]
    [Ignore("Fix This")]
    public virtual void InsertingPreparedNulls()
    {
      //executeSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      //MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, ?str)", Connection);
      //cmd.Parameters.Add("?str", MySqlDbType.VarChar);
      //cmd.Prepare();

      //cmd.Parameters[0].Value = null;
      //cmd.ExecuteNonQuery();

      //cmd.CommandText = "SELECT * FROM Test";
      //using (MySqlDataReader reader = cmd.ExecuteReader())
      //{
      //  Assert.True(reader.Read());
      //  Assert.AreEqual(DBNull.Value, reader[1]);
      //}
    }

    /// <summary>
    /// MySQL Bugs: #32127591: MySqlCommand.Cancel throws NullReferenceException for a Closed Connection
    /// </summary>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void MySqlCommandCancelWithClosedConnection(int test)
    {
      using (MySqlConnection conn = new MySqlConnection(Settings.ConnectionString))
      {
        switch (test)
        {
          case 1:
            using (MySqlCommand cmd1 = conn.CreateCommand())
            {
              conn.Open();
              conn.Close();
              Assert.DoesNotThrow(cmd1.Cancel);
            }
            break;

          case 2:
            using (MySqlCommand cmd2 = conn.CreateCommand())
            {
              Assert.DoesNotThrow(cmd2.Cancel);
            }
            break;

          case 3:
            using (MySqlCommand cmd3 = new MySqlCommand())
            {
              Assert.DoesNotThrow(cmd3.Cancel);
            }
            break;
        }
      }
    }

    /// <summary>
    /// MySQL Bugs: #12163: Insert using prepared statement causes double insert
    /// </summary>
    [Test]
    public void PreparedInsertUsingReader()
    {
      ExecuteSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'Test')", Connection);
      Assert.False(cmd.IsPrepared);
      cmd.Prepare();
      Assert.True(cmd.IsPrepared);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
      }

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    /// <summary>
    /// Bug #7248 There is already an open DataReader associated with this Connection which must 
    /// </summary>
    //[Test]
    //public void GenWarnings()
    //{
    //  executeSQL("CREATE TABLE Test (id INT, dt DATETIME)");
    //  executeSQL("INSERT INTO Test VALUES (1, NOW())");
    //  executeSQL("INSERT INTO Test VALUES (2, NOW())");
    //  executeSQL("INSERT INTO Test VALUES (3, NOW())");

    //  MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test WHERE dt = '" +
    //    DateTime.Now + "'", connection);
    //  DataSet ds = new DataSet();
    //  da.Fill(ds);
    //}

    /// <summary>
    /// Bug #11991 ExecuteScalar 
    /// </summary>
    [Test]
    public void CloseReaderAfterFailedConvert()
    {
      ExecuteSQL("CREATE TABLE Test (dt DATETIME)");
      ExecuteSQL("INSERT INTO Test VALUES ('00-00-0000 00:00:00')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      try
      {
        cmd.ExecuteScalar();
      }
      catch (Exception)
      {
      }

      Connection.BeginTransaction();
    }

    /// <summary>
    /// Bug #25443 ExecuteScalar() hangs when more than one bad result 
    /// </summary>
    [Test]
    public void ExecuteWithOneBadQuery()
    {
      MySqlCommand command = new MySqlCommand("SELECT 1; SELECT * FROM foo", Connection);
      try
      {
        command.ExecuteScalar();
      }
      catch (MySqlException)
      {
      }

      // now try using ExecuteNonQuery
      try
      {
        command.ExecuteNonQuery();
      }
      catch (MySqlException)
      {
      }
    }

    /// <summary>
    /// Bug #27958 Cannot use Data Source Configuration Wizard on large databases
    /// </summary>
    [Test]
    public void DefaultCommandTimeout()
    {
      MySqlConnection c = new MySqlConnection($"server={Host}");
      MySqlCommand cmd = new MySqlCommand("", c);
      Assert.AreEqual(30, cmd.CommandTimeout);

      c = new MySqlConnection($"server={Host};default command timeout=47");
      cmd = new MySqlCommand("", c);
      Assert.AreEqual(47, cmd.CommandTimeout);

      cmd = new MySqlCommand("");
      Assert.AreEqual(30, cmd.CommandTimeout);

      cmd.CommandTimeout = 66;
      cmd.Connection = c;
      Assert.AreEqual(66, cmd.CommandTimeout);
      cmd.CommandTimeout = 0;
      Assert.AreEqual(0, cmd.CommandTimeout);

      c = new MySqlConnection($"server={Host};default command timeout=0");
      cmd = new MySqlCommand("", c);
      Assert.AreEqual(0, cmd.CommandTimeout);

      // Defaults to Int32.MaxValue/1000 when provided value is larger. 
      c = new MySqlConnection(Connection.ConnectionString);
      cmd = new MySqlCommand("", c);
      c.Open();
      cmd.CommandTimeout = Int32.MaxValue;
      Assert.AreEqual(Int32.MaxValue / 1000, cmd.CommandTimeout);
      c.Close();
    }

    /// <summary>
    /// Bug #38276 Short circuit evaluation error in MySqlCommand.CheckState() 
    /// </summary>
    [Test]
    public void SetNullConnection()
    {
      MySqlCommand command = new MySqlCommand();
      command.CommandText = "SELECT 1";
      command.Connection = null;
      try
      {
        object o = command.ExecuteScalar();
      }
      catch (InvalidOperationException)
      {
      }
    }

    /// <summary>
    /// Bug #44194	ExecuteNonQuery for update commands does not match actual rows updated
    /// </summary>
    [Test]
    public void UseAffectedRows()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'A')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'B')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'C')");

      MySqlCommand cmd = new MySqlCommand("UPDATE Test SET name='C' WHERE id=3", Connection);
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.UseAffectedRows = true;
      using (MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true)))
      {
        c.Open();
        cmd.Connection = c;
        Assert.AreEqual(0, cmd.ExecuteNonQuery());
      }
    }

    /// <summary>
    /// Bug #45502 error if "Allow Batch=False" 
    /// </summary>
    [Test]
    public void DontAllowBatching()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.AllowBatch = false;
      connStr.CharacterSet = "utf8";
      using (MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true)))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT 1", c);
        cmd.ExecuteScalar();
      }
    }

    [Test]
    public void TableCommandType()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'A')");
      ExecuteSQL("CREATE TABLE Test1 (id INT, name VARCHAR(20))");
      ExecuteSQL("INSERT INTO Test1 VALUES (2, 'B')");

      MySqlCommand cmd = new MySqlCommand("Test,Test1", Connection);
      cmd.CommandType = CommandType.TableDirect;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual("A", reader.GetString(1));
        Assert.AreEqual(2, reader.GetInt32(2));
        Assert.AreEqual("B", reader.GetString(3));
      }
    }

    /// <summary>
    /// Bug #57501	MySql Connector/NET 6.3.5.0 fails to read from DataReader
    /// </summary>
    [Test]
    public void HelperTest()
    {
      string connStr = Connection.ConnectionString;
      using (MySqlDataReader reader = MySqlHelper.ExecuteReader(connStr, "SHOW TABLES"))
      {
        while (reader.Read())
        {
        }
      }
    }

    /// <summary>
    /// Bug #58652	ExecuteReader throws NullReferenceException when using CommandBehavior.Close
    /// </summary>
    [Test]
    public void SyntaxErrorWithCloseConnection()
    {
      MySqlConnection c = new MySqlConnection(Connection.ConnectionString);
      using (c)
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELE 1", c);
        var ex = Assert.Throws<MySqlException>(() => cmd.ExecuteReader(CommandBehavior.CloseConnection));
        Assert.AreEqual("You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version for the right syntax to use near 'SELE 1' at line 1", ex.Message);
        Assert.True(c.State == ConnectionState.Closed);
      }
    }

    /// <summary>
    /// Bug #59537	Different behavior from console and
    /// </summary>
    [Test]
    public void EmptyOrJustSemiCommand()
    {
      MySqlCommand cmd = new MySqlCommand("", Connection);
      cmd.CommandText = ";";
      MySqlException ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      // Error: 1065  Message: Query was empty
      Assert.AreEqual(1065, ex.Number);
    }

    /// <summary>
    /// MySql Bug #64092, Oracle bug #13624659 
    /// If MySqlCommand.CommandText equal to null, then MySqlCommand.ExecuteReader() 
    /// throw NullReferenceException instead of InvalidOperationException.
    /// </summary>
    [Test]
    public void CommandTextIsNull()
    {
      MySqlCommand cmd = new MySqlCommand(null, Connection);
      Exception ex = Assert.Throws<InvalidOperationException>(() => cmd.ExecuteReader());
      Assert.True(ex.Message != String.Empty);
    }

    /// <summary>
    /// Tests fix for http://bugs.mysql.com/bug.php?id=65452 / http://clustra.no.oracle.com/orabugs/14171960 
    /// (MySqlCommand.LastInsertedId can only have 32 bit values but has type long).
    /// </summary>
    [Test]
    public void LongLastInsertId()
    {
      string sql = @"CREATE TABLE longids (id BIGINT NOT NULL AUTO_INCREMENT, PRIMARY KEY (id));
                       alter table longids AUTO_INCREMENT = 2147483640;";
      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.ExecuteNonQuery();
      long seed = 2147483640;
      for (int i = 1; i < 10; ++i)
      {
        cmd.CommandText = "INSERT INTO longids VALUES ();";
        cmd.ExecuteNonQuery();
        Assert.AreEqual(seed++, cmd.LastInsertedId);
      }
    }

    #region Async

    [Test]
    public async Task ExecuteNonQueryAsync()
    {
      ExecuteSQL("CREATE TABLE CMDNonQueryAsyncTest (id int)");
      ExecuteSQL("CREATE PROCEDURE CMDNonQueryAsyncSpTest() BEGIN SET @x=0; REPEAT INSERT INTO CMDNonQueryAsyncTest VALUES(@x); SET @x=@x+1; UNTIL @x = 100 END REPEAT; END");

      MySqlCommand proc = new MySqlCommand("CMDNonQueryAsyncSpTest", Connection);
      proc.CommandType = CommandType.StoredProcedure;
      int result = await proc.ExecuteNonQueryAsync();

      Assert.AreNotEqual(-1, result);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM CMDNonQueryAsyncTest;", Connection);
      cmd.CommandType = CommandType.Text;
      object cnt = cmd.ExecuteScalar();
      Assert.AreEqual(100, Convert.ToInt32(cnt));
    }

    [Test]
    public async Task ExecuteReaderAsync()
    {
      ExecuteSQL("CREATE TABLE CMDReaderAsyncTest (id int)");
      ExecuteSQL("CREATE PROCEDURE CMDReaderAsyncSpTest() BEGIN INSERT INTO CMDReaderAsyncTest VALUES(1); SELECT SLEEP(2); SELECT 'done'; END");

      MySqlCommand proc = new MySqlCommand("CMDReaderAsyncSpTest", Connection);
      proc.CommandType = CommandType.StoredProcedure;

      using (MySqlDataReader reader = await proc.ExecuteReaderAsync() as MySqlDataReader)
      {
        Assert.NotNull(reader);
        Assert.True(reader.Read(), "can read");
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.AreEqual("done", reader.GetString(0));
        reader.Close();

        proc.CommandType = CommandType.Text;
        proc.CommandText = "SELECT COUNT(*) FROM CMDReaderAsyncTest";
        object cnt = proc.ExecuteScalar();
        Assert.AreEqual(1, Convert.ToInt32(cnt));
      }
    }

    [Test]
    public async Task ExecuteScalarAsync()
    {
      ExecuteSQL("CREATE PROCEDURE CMDScalarAsyncSpTest( IN valin VARCHAR(50), OUT valout VARCHAR(50) ) BEGIN  SET valout=valin;  SELECT 'Test'; END");

      MySqlCommand cmd = new MySqlCommand("CMDScalarAsyncSpTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", "valuein");
      cmd.Parameters.Add(new MySqlParameter("?valout", MySqlDbType.VarChar));
      cmd.Parameters[1].Direction = ParameterDirection.Output;

      object result = await cmd.ExecuteScalarAsync();
      Assert.AreEqual("Test", result);
      Assert.AreEqual("valuein", cmd.Parameters[1].Value);
    }

#if NET452
    [Test]
    public void ThrowingExceptions()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT xxx", Connection);
      IAsyncResult r = cmd.BeginExecuteReader();
      Exception ex = Assert.Throws<MySqlException>(() => cmd.EndExecuteReader(r));
      Assert.AreEqual("Unknown column 'xxx' in 'field list'", ex.Message);
    }
#endif
    #endregion

    /// <summary>
    /// Bug #59616	Only INSERTs are batched
    /// </summary>
    [Test]
    public void BatchUpdatesAndDeletes()
    {
      ExecuteSQL("CREATE TABLE Test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'boo'), (2, 'boo'), (3, 'boo')");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);

      var connectionStringCustom = Connection.ConnectionString;

      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connectionStringCustom);
      connStr.AllowBatch = true;
      connStr.Logging = true;
      using (MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true)))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
        da.UpdateCommand = cb.GetUpdateCommand();
        da.UpdateCommand.UpdatedRowSource = UpdateRowSource.None;
        da.UpdateBatchSize = 100;

        DataTable dt = new DataTable();
        da.Fill(dt);

        dt.Rows[0]["name"] = "boo2";
        dt.Rows[1]["name"] = "boo2";
        dt.Rows[2]["name"] = "boo2";
        da.Update(dt);
      }

      Assert.AreEqual(1, listener.Find("Query Opened: UPDATE"));
    }

    [Test]
    public void ExecuteReaderReturnsReaderAfterCancel()
    {
      ExecuteSQL("CREATE TABLE TableWithDateAsPrimaryKey(PrimaryKey date NOT NULL, PRIMARY KEY  (PrimaryKey)) ENGINE=InnoDB");
      ExecuteSQL("CREATE TABLE TableWithStringAsPrimaryKey(PrimaryKey nvarchar(50) NOT NULL, PRIMARY KEY  (PrimaryKey)) ENGINE=InnoDB");

      MySqlCommand command = new MySqlCommand("SELECT PrimaryKey FROM TableWithDateAsPrimaryKey", Connection);
      IDataReader reader = command.ExecuteReader(CommandBehavior.KeyInfo);
      DataTable dataTableSchema = reader.GetSchemaTable();
      command.Cancel();
      reader.Close();

      command = new MySqlCommand("SELECT PrimaryKey FROM TableWithStringAsPrimaryKey", Connection);
      reader = command.ExecuteReader(CommandBehavior.KeyInfo);
      Assert.NotNull(reader);

      dataTableSchema = reader.GetSchemaTable();
      Assert.True("PrimaryKey" == (string)dataTableSchema.Rows[0][dataTableSchema.Columns[0]]);
      reader.Close();
    }

    [Test]
    public void CloneCommand()
    {
      using var cmd = new MySqlCommand();
      cmd.Attributes.SetAttribute("attr", "attr_value");
      cmd.Parameters.AddWithValue("@param", "param_value");

      var cmd2 = (MySqlCommand)cmd.Clone();

      Assert.AreEqual(1, cmd2.Parameters.Count);
      Assert.AreEqual(1, cmd2.Attributes.Count);
      StringAssert.AreEqualIgnoringCase("attr_value", cmd2.Attributes[0].Value.ToString());
      StringAssert.AreEqualIgnoringCase("param_value", cmd2.Parameters[0].Value.ToString());
    }

    /// <summary>
    /// Bug 27441433 - RESETREADER: MYSQLDATAREADER CANNOT OUTLIVE PARENT MYSQLCOMMAND SINCE 6.10
    /// </summary>
    [Test]
    public void ExecuteReaderAfterClosingCommand()
    {
      MySqlDataReader reader;

      using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
      {
        using (MySqlCommand cmd = new MySqlCommand("SELECT 'TEST'", conn))
        {
          conn.Open();
          cmd.CommandType = CommandType.Text;
          reader = cmd.ExecuteReader();
        }

        Assert.True(reader.Read());
        Assert.AreEqual("TEST", reader.GetString(0));
        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug #26574860 - SETTING COMMANDTIMEOUT=-1 DOESN'T THROW ARGUMENTEXCEPTION
    /// </summary>
    [Test]
    public void CommandNegativeTimeout()
    {
      MySqlConnection conn = new MySqlConnection($"server={Host};default command timeout=10");
      MySqlCommand cmd = new MySqlCommand("", conn);
      Assert.AreEqual(10, cmd.CommandTimeout);

      Assert.Throws<ArgumentException>(() => conn = new MySqlConnection($"server={Host};default command timeout=-1"));
      var ex = Assert.Throws<ArgumentException>(() => cmd.CommandTimeout = -1);
      StringAssert.AreEqualIgnoringCase("Command timeout must not be negative", ex.Message);

      cmd.CommandTimeout = 15;
      Assert.AreEqual(15, cmd.CommandTimeout);
    }

    #region SQL Injection

    /// <summary>
    /// Bug #45941	SQL-Injection attack
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void SqlInjection1()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test(name VARCHAR(100)) ENGINE=MyISAM DEFAULT CHARSET=utf8");
      ExecuteSQL("INSERT INTO Test VALUES ('name1'), ('name2'), ('name3')");

      MySqlCommand cnt = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Int64 count = (Int64)cnt.ExecuteScalar();

      MySqlCommand cmd = new MySqlCommand("DELETE FROM Test WHERE name=?name", Connection);
      cmd.Parameters.Add("?name", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "\u2032 OR 1=1;-- --";
      cmd.ExecuteNonQuery();

      Assert.AreEqual(count, (Int64)cnt.ExecuteScalar());
    }

    #endregion

    [Test, Description("Timeout using Big Table ")]
    public void TimeoutBigTable()
    {
      var txt = @"
      CREATE TABLE `inventory` (
     `inventory_id` mediumint unsigned NOT NULL AUTO_INCREMENT,
     `film_id` int unsigned NOT NULL,
     `store_id` tinyint unsigned NOT NULL,
     `last_update` timestamp NOT NULL,
      PRIMARY KEY (`inventory_id`)
      )";

      ExecuteSQL(txt);
      Random rnd = new();
      StringBuilder cmdString = new();

      for (int i = 0; i < 5000; i++)
      {
        var film = rnd.Next(1, 100);
        var store = rnd.Next(2);
        cmdString.Append($"insert into inventory(film_id,store_id,last_update) values({film},{store},'2006-02-15 05:09:17');");
      }

      var cmd = Connection.CreateCommand();
      cmd.CommandTimeout = 3500;
      cmd.CommandText = cmdString.ToString();
      cmd.ExecuteNonQuery();

      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        cmd = new MySqlCommand();
        cmd.Connection = conn;
        cmd.CommandTimeout = 999999;
        cmd.CommandText = "SELECT * FROM inventory;";
        cmd.ExecuteNonQuery();

        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            Assert.AreEqual(999999, cmd.CommandTimeout);
            Assert.IsNotEmpty(reader.GetValue(0).ToString());
            Assert.IsNotEmpty(reader.GetValue(1).ToString());
            Assert.IsNotEmpty(reader.GetValue(2).ToString());
            Assert.IsNotEmpty(reader.GetValue(3).ToString());
          }
        }
      }

      ExecuteSQL("drop table if exists inventory");
    }

    [Test, Description("MySQL Reserved Word used")]
    public void ReservedWordUse()
    {
      var connStr = $"server={Host};user={Settings.UserID};database={Settings.Database};port={Port};password={Settings.Password};logging=true;sslmode=none";
      using (var conn = new MySqlConnection(connStr))
      {
        var cmd = new MySqlCommand();
        conn.Open();
        cmd.Connection = conn;
        cmd.CommandText = "DROP TABLE IF EXISTS mitabla";
        cmd.ExecuteNonQuery();
        cmd.CommandText =
            "CREATE TABLE mitabla ( value VARCHAR(45) NOT NULL, unknown VARCHAR(45) NOT NULL, status VARCHAR(45) NOT NULL)";
        cmd.ExecuteNonQuery();
        cmd.CommandText =
            "insert into mitabla values('1','test','status')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "select value, unknown, status from mitabla;";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.AreEqual("1", rdr[0]);
            Assert.AreEqual("test", rdr[1]);
            Assert.AreEqual("status", rdr[2]);
          }
        }
      }
    }

    /// <summary>
    /// Bug 19474480
    /// </summary>
    [Test, Description("Memory Leak Orabug 19474480")]
    [Ignore("This test needs to be executed individually as it makes too much iterations")]
    public void MemoryLeak()
    {
      var sql = "SELECT 1";
      const int TestIterationCount = 100000;

      for (var i = 0; i < TestIterationCount; i++)
        using (var conn = new MySqlConnection(Settings.ConnectionString))
        using (var comm = new MySqlCommand(sql, conn))
        {
          conn.Open();
          comm.ExecuteNonQuery();
        }

      for (var i = 0; i < TestIterationCount; i++)
      {
        var conn = new MySqlConnection(Settings.ConnectionString);
        var comm = new MySqlCommand(sql, conn);
        {
          conn.Open();
          comm.ExecuteNonQuery();
          conn.Close();
          conn.Dispose();
        }
      }
    }

    /// <summary>
    /// Bug #21971751 - USING TABS FOR WHITESPACE RESULTS IN "YOU HAVE AN ERROR IN YOUR SQL SYNTAX..."
    /// At the moment the query was analyzed, the tabs ('\t') and new lines ('\n') were not considered.
    /// </summary>
    [Test]
    public void ExecuteCmdWithTabsAndNewLines()
    {
      ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL PRIMARY KEY); 
        INSERT INTO Test VALUES (1);");

      using (var cmd = Connection.CreateCommand())
      {
        cmd.CommandText = "SELECT\nCOUNT(*)\nFROM\nTest;";
        Assert.AreEqual(1, cmd.ExecuteScalar());

        cmd.CommandText = "SELECT\tCOUNT(*)\n\t\tFROM\tTest;";
        Assert.AreEqual(1, cmd.ExecuteScalar());
      }
    }

    /// <summary>
    /// Bug#30365157 [MYSQLCOMMAND.LASTINSERTEDID RETURNS 0 AFTER EXECUTING MULTIPLE STATEMENTS]
    /// </summary>
    [Test]
    public void LastInsertedIdInMultipleStatements()
    {
      ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, Column1 CHAR(100))");
      ExecuteSQL(@"CREATE TABLE TestForeignKey (foreign_id INT NOT NULL, Column2 CHAR(100), FOREIGN KEY (foreign_id) REFERENCES Test (id))");

      using var cmd = Connection.CreateCommand();
      cmd.CommandText = "INSERT INTO Test(column1) VALUES ('hello'); "
      + "INSERT INTO TestForeignKey (foreign_id, column2) VALUES(LAST_INSERT_ID(), 'test');";

      cmd.ExecuteNonQuery();
      Assert.AreEqual(1, cmd.LastInsertedId);

      cmd.ExecuteNonQuery();
      Assert.AreEqual(2, cmd.LastInsertedId);

      cmd.CommandText = "SELECT * FROM Test";
      int id = 1;

      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        Assert.IsTrue(reader.GetInt32(0) == id);
        id++;
      }

      Assert.AreEqual(-1, cmd.LastInsertedId);
    }

    /// <summary>
    /// Bug# 34993798 - MySqlCommand.LastInsertedId is incorrect if multiple rows are inserted and all rows gnereate a value.
    /// </summary>
    [Test]
    public void LastInserteIdRedux() 
    {
      ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, text CHAR(100))");

      using var cmd = Connection.CreateCommand();
      cmd.CommandText = @"INSERT INTO Test (text) VALUES ('test1'); INSERT INTO Test (text) VALUES ('test2');";

      Assert.AreEqual(2, cmd.ExecuteNonQuery());
      Assert.AreEqual(2, cmd.LastInsertedId);
    }

  }
}