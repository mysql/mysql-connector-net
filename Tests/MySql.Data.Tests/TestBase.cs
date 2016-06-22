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
      databaseName = "cnet_test_";
    }

    public TestBase()
    {
      // cleanup
      executeAsRoot("DROP DATABASE IF EXISTS " + databaseName);
      List<string> users = GetUserList(false);
      foreach (var user in users)
        executeAsRoot(String.Format("DROP USER IF EXISTS {0}", user));
      executeAsRoot("FLUSH PRIVILEGES");

      executeAsRoot("CREATE DATABASE " + databaseName);
      executeAsRoot("CREATE USER 'test'@'localhost' IDENTIFIED BY 'test'");
      executeAsRoot(String.Format("GRANT ALL ON *.* TO 'test'@'localhost'", databaseName));
      executeAsRoot("FLUSH PRIVILEGES");
      Settings = new MySqlConnectionStringBuilder();
      Settings.Server = "localhost";
      Settings.UserID = "test";
      Settings.Password = "test";
      Settings.Database = databaseName;
      Settings.AllowUserVariables = true;
      Settings.Pooling = false;
      Settings.PersistSecurityInfo = true;
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

    protected MySqlConnection GetRoot()
    {
      return new MySqlConnection("server=localhost;port=3306;user id=root;database=mysql");
    }
    protected void executeAsRoot(string sql)
    {
      using (MySqlConnection root = GetRoot())
      {
        try
        {
          root.Open();
          MySqlCommand cmd = new MySqlCommand(sql, root);
          cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
          string s = ex.Message;
        }
      }
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
