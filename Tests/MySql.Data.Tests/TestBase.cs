using System;
using System.Collections.Generic;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestBase : IDisposable
  {

    protected static string databaseName;
    protected MySqlConnectionStringBuilder Settings;

    static TestBase()
    {
      databaseName = "db0";
    }

    public TestBase()
    {
      Settings = new MySqlConnectionStringBuilder();
      Settings.Server = "localhost";
      Settings.UserID = "user0";
      Settings.Password = "pwd";
      Settings.Database = "mysql";
      Settings.AllowUserVariables = true;
      Settings.Pooling = false;
      Settings.PersistSecurityInfo = true;

      // cleanup
      for (int x = 0; x < 3; x++)
      {
        executeAsRoot("DROP DATABASE IF EXISTS db" + x);
        executeAsRoot(String.Format("DROP USER IF EXISTS 'user{0}'@'localhost'", x));
      }
      executeAsRoot("FLUSH PRIVILEGES");

      executeAsRoot("CREATE DATABASE " + databaseName);
      executeAsRoot("CREATE USER 'user0'@'localhost' IDENTIFIED BY 'pwd'");
      executeAsRoot(String.Format("GRANT ALL ON *.* TO 'user0'@'localhost'", databaseName));
      executeAsRoot("FLUSH PRIVILEGES");

      Settings.Database = databaseName;
    }

    private List<string> GetUserList(bool includeRoot)
    {
      var list = new List<string>();
      var reader = ExecuteReaderAsRoot("select user,host from mysql.user");
      while (reader.Read())
      {
        string user = reader.GetString(0);
        if (user == "root" && !includeRoot) continue;
        list.Add(String.Format("'{0}'@'{1}'", reader.GetString(0), reader.GetString(1)));
      }
      return list;
    }

    protected MySqlConnection GetConnection(bool asRoot = false)
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder(Settings.GetConnectionString(true));
      if (asRoot)
      {
        s.UserID = "root";
        s.Password = null;
      }
      return new MySqlConnection(s.GetConnectionString(true));
    }

    protected MySqlConnection GetRoot()
    {
      return GetConnection(true);
    }

    protected void executeSQL(string sql, bool asRoot = false)
    {
      using (var conn = GetConnection(asRoot))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
      }
    }

    protected void executeAsRoot(string sql)
    {
      executeSQL(sql, true);
    }

    protected MySqlDataReader ExecuteReaderAsRoot(string sql)
    {
      MySqlConnection root = GetRoot();
      root.Open();
      MySqlCommand cmd = new MySqlCommand(sql, root);
      return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
    }

    protected void KillConnection(MySqlConnection c)
    {
      int threadId = c.ServerThread;
      var root = GetRoot();
      root.Open();
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
      bool processStillAlive = false;
      while (true)
      {
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
    }

    public void Dispose()
    {
    }
  }
}
