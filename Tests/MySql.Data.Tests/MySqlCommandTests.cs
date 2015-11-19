// Copyright © 2013, 2015 Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
#if NET_45_OR_GREATER
using System.Threading.Tasks;
#endif

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlCommandTests : SpecialFixtureWithCustomConnectionString
  {
    protected override void Dispose(bool disposing)
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
      base.Dispose(disposing);
    }

    /// <summary>
    /// Tests for MySql bug #64633 - System.InvalidCastException when executing a stored function.
    /// </summary>
    [Fact]
    public void InvalidCast()
    {
      MySqlConnection con = st.rootConn;
      string sql = @"drop function if exists MyTwice; create function MyTwice( val int ) returns int begin return val * 2; end;";
      MySqlCommand cmd = new MySqlCommand(sql, con);
      cmd.ExecuteNonQuery();
      cmd.CommandText = "drop procedure if exists spMyTwice; create procedure spMyTwice( out result int, val int ) begin set result = val * 2; end;";
      cmd.ExecuteNonQuery();
      try
      {
        cmd.CommandText = "drop user 'tester2'@'localhost'";
        cmd.ExecuteNonQuery();
      }
      catch (Exception)
      {
      }
      cmd.CommandText = "CREATE USER 'tester2'@'localhost' IDENTIFIED BY '123';";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "grant execute on function `MyTwice` to 'tester2'@'localhost';";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "grant execute on procedure `spMyTwice` to 'tester2'@'localhost'";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "grant select on table mysql.proc to 'tester2'@'localhost'";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "flush privileges";
      cmd.ExecuteNonQuery();
      MySqlConnection con2 = new MySqlConnection(st.rootConn.ConnectionString);
      con2.Settings.UserID = "tester2";
      con2.Settings.Password = "123";

      // Invoke the function
      cmd.Connection = con2;
      con2.Open();
      cmd.CommandText = "MyTwice";
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add(new MySqlParameter("val", System.DBNull.Value));
      cmd.Parameters.Add("@p", MySqlDbType.Int32);
      cmd.Parameters[1].Direction = ParameterDirection.ReturnValue;
      cmd.Parameters[0].Value = 20;
      cmd.ExecuteNonQuery();
      con2.Close();
      Assert.Equal(cmd.Parameters[1].Value, 40);

      con2.Open();
      cmd.CommandText = "spMyTwice";
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Clear();
      cmd.Parameters.Add(new MySqlParameter("result", System.DBNull.Value));
      cmd.Parameters.Add("val", MySqlDbType.Int32);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      cmd.Parameters[1].Value = 20;
      cmd.ExecuteNonQuery();
      con2.Close();
      Assert.Equal(cmd.Parameters[0].Value, 40);
    }


    [Fact]
    public void InsertTest()
    {
      st.execSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      // do the insert
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES(10,'Test')", st.conn);
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

    [Fact]
    public void UpdateTest()
    {
      st.execSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      st.execSQL("INSERT INTO Test (id,name) VALUES(10, 'Test')");
      st.execSQL("INSERT INTO Test (id,name) VALUES(11, 'Test2')");

      // do the update
      MySqlCommand cmd = new MySqlCommand("UPDATE Test SET name='Test3' WHERE id=10 OR id=11", st.conn);
      MySqlConnection c = cmd.Connection;
      Assert.Equal(st.conn, c);
      int cnt = cmd.ExecuteNonQuery();
      Assert.Equal(2, cnt);

      // make sure we get the right value back out
      cmd.CommandText = "SELECT name FROM Test WHERE id=10";
      string name = (string)cmd.ExecuteScalar();
      Assert.Equal("Test3", name);

      cmd.CommandText = "SELECT name FROM Test WHERE id=11";
      name = (string)cmd.ExecuteScalar();
      Assert.Equal("Test3", name);

      // now do the update with parameters
      cmd.CommandText = "UPDATE Test SET name=?name WHERE id=?id";
      cmd.Parameters.Add(new MySqlParameter("?id", 11));
      cmd.Parameters.Add(new MySqlParameter("?name", "Test5"));
      cnt = cmd.ExecuteNonQuery();
      Assert.True(cnt == 1, "Update with Parameters Count");

      // make sure we get the right value back out
      cmd.Parameters.Clear();
      cmd.CommandText = "SELECT name FROM Test WHERE id=11";
      name = (string)cmd.ExecuteScalar();
      Assert.Equal("Test5", name);
    }

    [Fact]
    public void DeleteTest()
    {
      st.execSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      st.execSQL("INSERT INTO Test (id, name) VALUES(1, 'Test')");
      st.execSQL("INSERT INTO Test (id, name) VALUES(2, 'Test2')");

      // make sure we get the right value back out
      MySqlCommand cmd = new MySqlCommand("DELETE FROM Test WHERE id=1 or id=2", st.conn);
      int delcnt = cmd.ExecuteNonQuery();
      Assert.Equal(2, delcnt);

      // find out how many rows we have now
      cmd.CommandText = "SELECT COUNT(*) FROM Test";
      object after_cnt = cmd.ExecuteScalar();
      Assert.Equal(0, Convert.ToInt32(after_cnt));
    }

    [Fact]
    public void CtorTest()
    {
      st.execSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      MySqlTransaction txn = st.conn.BeginTransaction();
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);

      MySqlCommand clone = new MySqlCommand(cmd.CommandText, (MySqlConnection)cmd.Connection,
        (MySqlTransaction)cmd.Transaction);
      clone.Parameters.AddWithValue("?test", 1);
      txn.Rollback();
    }

    [Fact]
    public void CloneCommand()
    {
      MySqlCommand cmd = new MySqlCommand();
      MySqlCommand newCommand = cmd.Clone();
#if !RT
      IDbCommand newCommand2 = (IDbCommand)(cmd as ICloneable).Clone();
#endif
    }

    [Fact]
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

      MySqlCommand cmd = new MySqlCommand(sql, st.conn);
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #12245  	using Prepare() on an insert command causes null parameters to convert to "0"
    /// </summary>
    [Fact]
    public void InsertingPreparedNulls()
    {
      if (st.Version < new Version(4, 1)) return;

      st.execSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, ?str)", st.conn);
      cmd.Parameters.Add("?str", MySqlDbType.VarChar);
      cmd.Prepare();

      cmd.Parameters[0].Value = null;
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal(DBNull.Value, reader[1]);
      }
    }

    /// <summary>
    /// MySQL Bugs: #12163: Insert using prepared statement causes double insert
    /// </summary>
    [Fact]
    public void PreparedInsertUsingReader()
    {
      if (st.Version < new Version(4, 1)) return;

      st.execSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, 'Test')", st.conn);
      cmd.Prepare();
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

#if !RT
    /// <summary>
    /// Bug #7248 There is already an open DataReader associated with this Connection which must 
    /// </summary>
    [Fact]
    public void GenWarnings()
    {
      st.execSQL("CREATE TABLE Test (id INT, dt DATETIME)");
      st.execSQL("INSERT INTO Test VALUES (1, NOW())");
      st.execSQL("INSERT INTO Test VALUES (2, NOW())");
      st.execSQL("INSERT INTO Test VALUES (3, NOW())");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test WHERE dt = '" +
        DateTime.Now + "'", st.conn);
      DataSet ds = new DataSet();
      da.Fill(ds);
    }
#endif

    /// <summary>
    /// Bug #11991 ExecuteScalar 
    /// </summary>
    [Fact]
    public void CloseReaderAfterFailedConvert()
    {
      st.execSQL("CREATE TABLE Test (dt DATETIME)");
      st.execSQL("INSERT INTO Test VALUES ('00-00-0000 00:00:00')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
      try
      {
        cmd.ExecuteScalar();
      }
      catch (Exception)
      {
      }

      st.conn.BeginTransaction();
    }

    /// <summary>
    /// Bug #25443 ExecuteScalar() hangs when more than one bad result 
    /// </summary>
    [Fact]
    public void ExecuteWithOneBadQuery()
    {
      MySqlCommand command = new MySqlCommand("SELECT 1; SELECT * FROM foo", st.conn);
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
    [Fact]
    public void DefaultCommandTimeout()
    {
      MySqlConnection c = new MySqlConnection("server=localhost");
      MySqlCommand cmd = new MySqlCommand("", c);
      Assert.Equal(30, cmd.CommandTimeout);

      c = new MySqlConnection("server=localhost;default command timeout=47");
      cmd = new MySqlCommand("", c);
      Assert.Equal(47, cmd.CommandTimeout);

      cmd = new MySqlCommand("");
      Assert.Equal(30, cmd.CommandTimeout);

      cmd.CommandTimeout = 66;
      cmd.Connection = c;
      Assert.Equal(66, cmd.CommandTimeout);
      cmd.CommandTimeout = 0;
      Assert.Equal(0, cmd.CommandTimeout);

      c = new MySqlConnection("server=localhost;default command timeout=0");
      cmd = new MySqlCommand("", c);
      Assert.Equal(0, cmd.CommandTimeout);
    }

    /// <summary>
    /// Bug #38276 Short circuit evaluation error in MySqlCommand.CheckState() 
    /// </summary>
    [Fact]
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
    /// Bug #45941	SQL-Injection attack
    /// </summary>
    [Fact]
    public void SqlInjection1()
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test(name VARCHAR(100)) ENGINE=MyISAM DEFAULT CHARSET=utf8");
      st.execSQL("INSERT INTO Test VALUES ('name1'), ('name2'), ('name3')");

      MySqlCommand cnt = new MySqlCommand("SELECT COUNT(*) FROM Test", st.conn);
      Int64 count = (Int64)cnt.ExecuteScalar();

      MySqlCommand cmd = new MySqlCommand("DELETE FROM Test WHERE name=?name", st.conn);
      cmd.Parameters.Add("?name", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "\u2032 OR 1=1;-- --";
      cmd.ExecuteNonQuery();

      Assert.Equal(count, (Int64)cnt.ExecuteScalar());
    }

    /// <summary>
    /// Bug #44194	ExecuteNonQuery for update commands does not match actual rows updated
    /// </summary>
    [Fact]
    public void UseAffectedRows()
    {
      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      st.execSQL("INSERT INTO Test VALUES (1, 'A')");
      st.execSQL("INSERT INTO Test VALUES (2, 'B')");
      st.execSQL("INSERT INTO Test VALUES (3, 'C')");

      MySqlCommand cmd = new MySqlCommand("UPDATE Test SET name='C' WHERE id=3", st.conn);
      Assert.Equal(1, cmd.ExecuteNonQuery());

      string conn_str = st.GetConnectionString(true) + ";use affected rows=true";
      using (MySqlConnection c = new MySqlConnection(conn_str))
      {
        c.Open();
        cmd.Connection = c;
        Assert.Equal(0, cmd.ExecuteNonQuery());
      }
    }

    /// <summary>
    /// Bug #45502 error if "Allow Batch=False" 
    /// </summary>
    [Fact]
    public void DontAllowBatching()
    {
      string connStr = st.GetConnectionString(true) + ";allow batch=false;character set=utf8";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT 1", c);
        cmd.ExecuteScalar();
      }
    }

    [Fact]
    public void TableCommandType()
    {
      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      st.execSQL("INSERT INTO Test VALUES (1, 'A')");
      st.execSQL("CREATE TABLE Test1 (id INT, name VARCHAR(20))");
      st.execSQL("INSERT INTO Test1 VALUES (2, 'B')");

      MySqlCommand cmd = new MySqlCommand("Test,Test1", st.conn);
      cmd.CommandType = CommandType.TableDirect;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("A", reader.GetString(1));
        Assert.Equal(2, reader.GetInt32(2));
        Assert.Equal("B", reader.GetString(3));
      }
    }

    /// <summary>
    /// Bug #57501	MySql Connector/NET 6.3.5.0 fails to read from DataReader
    /// </summary>
    [Fact]
    public void HelperTest()
    {
      string connStr = st.GetConnectionString(true);
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
    [Fact]
    public void SyntaxErrorWithCloseConnection()
    {
      string connStr = st.GetConnectionString(true);
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELE 1", c);
        Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteReader(CommandBehavior.CloseConnection));

        Assert.Equal("You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version for the right syntax to use near 'SELE 1' at line 1", ex.Message);
        Assert.True(c.State == ConnectionState.Closed);
      }
    }

    /// <summary>
    /// Bug #59537	Different behavior from console and
    /// </summary>
    [Fact]
    public void EmptyOrJustSemiCommand()
    {
      MySqlCommand cmd = new MySqlCommand("", st.conn);
      cmd.CommandText = ";";
      MySqlException ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      // Error: 1065  Message: Query was empty
      Assert.Equal(1065, ex.Number);
    }

#if !RT
    /// <summary>
    /// Bug #59616	Only INSERTs are batched
    /// </summary>
    [Fact]
    public void BatchUpdatesAndDeletes()
    {
      st.execSQL("CREATE TABLE test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20))");
      st.execSQL("INSERT INTO test VALUES (1, 'boo'), (2, 'boo'), (3, 'boo')");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);

      string connStr = st.GetConnectionString(true) + ";logging=true;allow batch=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM test", c);
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

      Assert.Equal(1, listener.Find("Query Opened: UPDATE"));
    }
#endif

    [Fact]
    public void ExecuteReaderReturnsReaderAfterCancel()
    {
      st.execSQL("DROP TABLE IF EXISTS TableWithDateAsPrimaryKey");
      st.execSQL("DROP TABLE IF EXISTS TableWithStringAsPrimaryKey");
      st.createTable("CREATE TABLE TableWithDateAsPrimaryKey(PrimaryKey date NOT NULL, PRIMARY KEY  (PrimaryKey))", "InnoDB");
      st.createTable("CREATE TABLE TableWithStringAsPrimaryKey(PrimaryKey nvarchar(50) NOT NULL, PRIMARY KEY  (PrimaryKey))", "InnoDB");

      string connStr = st.GetConnectionString(true);
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SELECT PrimaryKey FROM TableWithDateAsPrimaryKey", connection);
#if RT
        MySqlDataReader reader = command.ExecuteReader(CommandBehavior.KeyInfo);
        while (reader.Read()) ;
#else
        IDataReader reader = command.ExecuteReader(CommandBehavior.KeyInfo);
        DataTable dataTableSchema = reader.GetSchemaTable();
#endif
        command.Cancel();
        reader.Close();

        command = new MySqlCommand("SELECT PrimaryKey FROM TableWithStringAsPrimaryKey", connection);
        reader = command.ExecuteReader(CommandBehavior.KeyInfo);
        Assert.NotNull(reader);

#if RT
        while (reader.Read()) ;
        Assert.Equal("PrimaryKey", reader.GetName(0));
#else
        dataTableSchema = reader.GetSchemaTable();
        Assert.True("PrimaryKey" == (string)dataTableSchema.Rows[0][dataTableSchema.Columns[0]]);
#endif
        reader.Close();
      }
    }

    /// <summary>
    /// MySql Bug #64092, Oracle bug #13624659 
    /// If MySqlCommand.CommandText equal to null, then MySqlCommand.ExecuteReader() 
    /// throw NullReferenceException instead of InvalidOperationException.
    /// </summary>
    [Fact]
    public void CommandTextIsNull()
    {
      using (MySqlConnection conn = new MySqlConnection(st.GetConnectionString(true)))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand(null, conn);
        Exception ex = Assert.Throws<InvalidOperationException>(() => cmd.ExecuteReader());
        Assert.True(ex.Message != String.Empty);
      }
    }

    /// <summary>
    /// Tests fix for http://bugs.mysql.com/bug.php?id=65452 / http://clustra.no.oracle.com/orabugs/14171960 
    /// (MySqlCommand.LastInsertedId can only have 32 bit values but has type long).
    /// </summary>
    [Fact]
    public void LongLastInsertId()
    {
      using (MySqlConnection conn = new MySqlConnection(st.GetConnectionString(true)))
      {
        conn.Open();
        string sql = @"CREATE TABLE longids (id BIGINT NOT NULL AUTO_INCREMENT, PRIMARY KEY (id));
alter table longids AUTO_INCREMENT = 2147483640;";
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
        long seed = 2147483640;
        for (int i = 1; i < 10; ++i)
        {
          cmd.CommandText = "INSERT INTO longids VALUES ();";
          cmd.ExecuteNonQuery();
          Assert.Equal(seed++, cmd.LastInsertedId);
        }
        conn.Close();
      }
    }

    [Fact]
    public void CommandWithoutSpacesTest()
    {
      string[] queries = new string[] { "select`user`from`mysql`.`user`;",
                                       "select(left('test',1));",
                                       "SET@test='test';",
                                       "do(1);",
                                       "commit",
                                       "rollback",
                                       //"use", //it fails because invalid syntax error
                                       "begin",
                                       //"end", //it fails because invalid syntax error
                                       "use`" + st.database0 + "`;",
                                       "select'test';",
                                       "select'1'=1;" };

      string logQuery = "SELECT argument FROM mysql.general_log WHERE argument = '{0}'";

      st.ExecuteSQLAsRoot("SET GLOBAL log_output = 'TABLE'");
      st.ExecuteSQLAsRoot("SET GLOBAL general_log = 'ON'");

      MySqlConnection dbConn = new MySqlConnection(st.GetConnectionString(st.rootUser, st.rootPassword, true));
      try
      {
        dbConn.Open();
        using (MySqlCommand cmd = new MySqlCommand())
        {
          cmd.Connection = dbConn;
          for (int ctr = 0; ctr < queries.Length; ctr++)
          {
            cmd.CommandText = queries[ctr];

            cmd.CommandType = CommandType.Text;
            //try
            //{
            cmd.ExecuteNonQuery();
            //}
            //catch(Exception) { }

            //verify that the query executed has not the call statement
            cmd.CommandText = string.Format(logQuery, queries[ctr].Replace("'", "''").Replace(";", ""));
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
              while (reader.Read())
              {
                Assert.False(reader[0].ToString().Contains("call"));
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }
      finally
      {
        dbConn.Close();
      }
    }

#if NET_45_OR_GREATER
    #region Async
    [Fact]
    public async Task ExecuteNonQueryAsync()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE CMDNonQueryAsyncTest (id int)");
      st.execSQL("CREATE PROCEDURE CMDNonQueryAsyncSpTest() BEGIN SET @x=0; REPEAT INSERT INTO CMDNonQueryAsyncTest VALUES(@x); SET @x=@x+1; UNTIL @x = 100 END REPEAT; END");

      MySqlCommand proc = new MySqlCommand("CMDNonQueryAsyncSpTest", st.conn);
      proc.CommandType = CommandType.StoredProcedure;
      int result = await proc.ExecuteNonQueryAsync();

      Assert.NotEqual(-1, result);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM CMDNonQueryAsyncTest;", st.conn);
      cmd.CommandType = CommandType.Text;
      object cnt = cmd.ExecuteScalar();
      Assert.Equal(100, Convert.ToInt32(cnt));
    }

    [Fact]
    public async Task ExecuteReaderAsync()
    {
      if (st.Version < new Version(5, 0)) return;

      if (st.conn.State != ConnectionState.Open)
        st.conn.Open();

      st.execSQL("CREATE TABLE CMDReaderAsyncTest (id int)");
      st.execSQL("CREATE PROCEDURE CMDReaderAsyncSpTest() BEGIN INSERT INTO CMDReaderAsyncTest VALUES(1); SELECT SLEEP(2); SELECT 'done'; END");

      MySqlCommand proc = new MySqlCommand("CMDReaderAsyncSpTest", st.conn);
      proc.CommandType = CommandType.StoredProcedure;

      using (MySqlDataReader reader = await proc.ExecuteReaderAsync() as MySqlDataReader)
      {
        Assert.NotNull(reader);
        Assert.True(reader.Read(), "can read");
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal("done", reader.GetString(0));
        reader.Close();

        proc.CommandType = CommandType.Text;
        proc.CommandText = "SELECT COUNT(*) FROM CMDReaderAsyncTest";
        object cnt = proc.ExecuteScalar();
        Assert.Equal(1, Convert.ToInt32(cnt));
      }
    }

    [Fact]
    public async Task ExecuteScalarAsync()
    {
      if (st.Version < new Version(5, 0)) return;

      if (st.conn.connectionState != ConnectionState.Open)
        st.conn.Open();

      st.execSQL("CREATE PROCEDURE CMDScalarAsyncSpTest( IN valin VARCHAR(50), OUT valout VARCHAR(50) ) BEGIN  SET valout=valin;  SELECT 'Test'; END");

      MySqlCommand cmd = new MySqlCommand("CMDScalarAsyncSpTest", st.conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", "valuein");
      cmd.Parameters.Add(new MySqlParameter("?valout", MySqlDbType.VarChar));
      cmd.Parameters[1].Direction = ParameterDirection.Output;

      object result = await cmd.ExecuteScalarAsync();
      Assert.Equal("Test", result);
      Assert.Equal("valuein", cmd.Parameters[1].Value);
    }
    #endregion
#endif
  }
}
