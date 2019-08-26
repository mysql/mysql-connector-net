// Copyright (c) 2016, 2019, Oracle and/or its affiliates. All rights reserved.
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
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestBase : IClassFixture<TestFixture>, IDisposable
  {
    protected TestFixture Fixture { get; set; }
    protected MySqlConnection Connection { get; set; }
    protected MySqlConnection Root { get; set; }

    public TestBase(TestFixture fixture, bool reinitDatabase = false)
    {
      Fixture = fixture;
      Fixture.Setup(this, reinitDatabase);

      if (String.IsNullOrEmpty(Fixture.Settings.Database))
        Console.WriteLine("database is empty in ctor");
      Connection = Fixture.GetConnection(false);
      Root = Fixture.GetConnection(true);
      AdjustConnections();
    }

    public MySqlConnectionStringBuilder ConnectionSettings
    {
      get { return Fixture.Settings; }
    }

    internal virtual void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
    }

    protected virtual void AdjustConnections()
    {
    }

    protected virtual void Cleanup()
    {
    }

    protected void executeSQL(string sql, bool asRoot = false)
    {
      var connection = asRoot ? Root : Connection;
      var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }

    public MySqlDataReader ExecuteReader(string sql, bool asRoot = false)
    {
      var conn = asRoot ? Root : Connection;
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      return cmd.ExecuteReader();
    }

    public DataTable FillTable(string sql)
    {
      DataTable dt = new DataTable();
      MySqlDataAdapter da = new MySqlDataAdapter(sql, Connection);
      da.Fill(dt);
      return dt;
    }

    public bool TableExists(string tableName)
    {
      MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {tableName} LIMIT 0", Connection);
      try
      {
        cmd.ExecuteScalar();
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    internal protected void KillConnection(MySqlConnection c)
    {
      int threadId = c.ServerThread;
      var root = Fixture.GetConnection(true);
      MySqlCommand cmd = new MySqlCommand("KILL " + threadId, root);
      cmd.ExecuteNonQuery();

      // the kill flag might need a little prodding to do its thing
      try
      {
        cmd.CommandText = "SELECT 1";
        cmd.Connection = c;
        cmd.ExecuteNonQuery();
      }
      catch (Exception) { }

      // now wait till the process dies
      while (true)
      {
        bool processStillAlive = false;
        MySqlCommand cmdProcess = new MySqlCommand("SHOW PROCESSLIST", root);
        MySqlDataReader dr = cmdProcess.ExecuteReader();
        while (dr.Read())
        {
          if (dr.GetInt32(0) == threadId) processStillAlive = true;
        }
        dr.Close();

        if (!processStillAlive) break;
        System.Threading.Thread.Sleep(500);
      }
      root.Close();
    }

    internal protected void KillPooledConnection(string connStr)
    {
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      KillConnection(c);
    }

    public void Dispose()
    {
      if (String.IsNullOrEmpty(Fixture.Settings.Database))
        Console.WriteLine("database is empty in dispose");

      executeSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
      executeSQL(String.Format("DROP TABLE IF EXISTS `{0}`.test", Connection.Database));
      Cleanup();
      if (Connection != null && Connection.State == ConnectionState.Open)
        Connection.Close();
      if (Root != null && Root.State == ConnectionState.Open)
        Root.Close();
    }
  }
}
