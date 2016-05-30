// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Properties;
using NUnit.Framework;
using System.Security;
using System.Net;
#if !RT
using System.Configuration;
using System.Security.Permissions;
#endif

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for ConnectionTests.
  /// </summary>
  [TestFixture]
  public class ConnectionTests : BaseTest
  {
    [Test]
    public void TestConnectionStrings()
    {
      MySqlConnection c = new MySqlConnection();

      // public properties
      Assert.AreEqual(15, c.ConnectionTimeout, "ConnectionTimeout");
      Assert.AreEqual("", c.Database, "Database");
      Assert.AreEqual(String.Empty, c.DataSource, "DataSource");
      Assert.AreEqual(false, c.UseCompression, "Use Compression");
      Assert.AreEqual(ConnectionState.Closed, c.State, "State");

      c = new MySqlConnection("connection timeout=25; user id=myuser; " +
          "password=mypass; database=Test;server=myserver; use compression=true; " +
          "pooling=false;min pool size=5; max pool size=101");
      // public properties
      Assert.AreEqual(25, c.ConnectionTimeout, "ConnectionTimeout");
      Assert.AreEqual("Test", c.Database, "Database");
      Assert.AreEqual("myserver", c.DataSource, "DataSource");
      Assert.AreEqual(true, c.UseCompression, "Use Compression");
      Assert.AreEqual(ConnectionState.Closed, c.State, "State");

      c.ConnectionString = "connection timeout=15; user id=newuser; " +
          "password=newpass; port=3308; database=mydb; data source=myserver2; " +
          "use compression=true; pooling=true; min pool size=3; max pool size=76";

      // public properties
      Assert.AreEqual(15, c.ConnectionTimeout, "ConnectionTimeout");
      Assert.AreEqual("mydb", c.Database, "Database");
      Assert.AreEqual("myserver2", c.DataSource, "DataSource");
      Assert.AreEqual(true, c.UseCompression, "Use Compression");
      Assert.AreEqual(ConnectionState.Closed, c.State, "State");
    }

#if !RT
    //*
    [Test]
    public void TestSha256SecurityWithoutSSL()
    {
      if (Version < new Version(5, 6, 6))
      {
#if !CF
        Trace.WriteLine("No Sha256 authentication, server version does not support it.");
#endif
        return;
      }

      const string PluginName = "sha256_password";

      // Check if server has windows authentication plugin is installed			
      MySqlCommand cmd = new MySqlCommand("show plugins", rootConn);

      bool haveSha256Auth = false;
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        while (r.Read())
        {
          string name = (string)r["Name"];
          if (name == PluginName)
          {
            haveSha256Auth = true;
            break;
          }
        }
      }

      if (!haveSha256Auth)
      {
#if !CF
        Trace.WriteLine("No Sha256 authentication, server version does not support it.");
#endif
        return;
      }

      cmd.CommandText = "show variables like 'have_openssl'";
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        r.Read();
        string name = (string)r.GetValue(1);
        if (name.ToUpper() != "YES")
        {
#if !CF
          Trace.WriteLine("No Sha256 without SSL tested, server must be compiled against OpenSsl");
#endif
          return;
        }
      }

      cmd.CommandText = "SHOW STATUS LIKE 'Rsa_public_key'";
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        if (!r.Read())
        {
#if !CF
          Trace.WriteLine("No Sha256 without SSL tested, server must have a public rsa key configured.");
#endif
          return;
        }
      }

      // setup account
      string user = "mytester256";
      bool userExists = false;
      cmd.CommandText = string.Format(
        "select count( * ) from mysql.user where user = '{0}' and host = 'localhost'", user);
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        r.Read();
        if (Convert.ToInt32(r.GetValue(0)) != 0)
        {
          userExists = true;
        }
      }

      if (userExists)
      {
        ExecuteSQLAsRoot(string.Format("drop user '{0}'@'localhost';", user));
      }

      ExecuteSQLAsRoot(string.Format("create user '{0}'@'localhost' identified with sha256_password;", user));
      try
      {
        cmd.CommandText = "show variables like 'old_passwords'";
        int oldValOldPasswords = 0;
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          r.Read();
          oldValOldPasswords = Convert.ToInt32(r.GetValue(1));
        }
        ExecuteSQLAsRoot("set old_passwords = 2;");
        ExecuteSQLAsRoot(string.Format("set password for '{0}'@'localhost' = password( '123' );", user));
        ExecuteSQLAsRoot(string.Format("set old_passwords = {0};", oldValOldPasswords));

        string connstr = GetConnectionString(true);
        MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(connstr);
        csb.UserID = user;
        csb.Password = "123";
        using (MySqlConnection c = new MySqlConnection(csb.ConnectionString))
        {
          ExecuteSQLAsRoot(string.Format("grant all on `{0}`.* to '{1}'@'localhost';",
            c.Settings.Database, user));
          c.Open();
          Assert.AreEqual(ConnectionState.Open, c.State);
        }
      }
      finally
      {
        // cleanup
        ExecuteSQLAsRoot(string.Format("drop user '{0}'@'localhost'", user));
      }
    }

    [Test]
    public void TestSha256SecurityWithSSL()
    {
      if (Version < new Version(5, 6, 6))
      {
#if !CF
        Trace.WriteLine("No Sha256 authentication, server version does not support it.");
#endif
        return;
      }

      const string PluginName = "sha256_password";

      // Check if server has windows authentication plugin is installed			
      MySqlCommand cmd = new MySqlCommand("show plugins", rootConn);

      bool haveSha256Auth = false;
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        while (r.Read())
        {
          string name = (string)r["Name"];
          if (name == PluginName)
          {
            haveSha256Auth = true;
            break;
          }
        }
      }

      if (!haveSha256Auth)
      {
#if !CF
        Trace.WriteLine("No Sha256 authentication, server version does not support it.");
#endif
        return;
      }

      // setup account
      string user = "mytester256";
      bool userExists = false;
      cmd.CommandText = string.Format(
        "select count( * ) from mysql.user where user = '{0}' and host = 'localhost'", user);
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        r.Read();
        if (Convert.ToInt32(r.GetValue(0)) != 0)
        {
          userExists = true;
        }
      }

      if (userExists)
      {
        ExecuteSQLAsRoot(string.Format("drop user '{0}'@'localhost';", user));
      }

      ExecuteSQLAsRoot(string.Format("create user '{0}'@'localhost' identified with sha256_password;", user));
      try
      {
        cmd.CommandText = "show variables like 'old_passwords'";
        int newValOldPasswords, oldValOldPasswords = 0;
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          r.Read();
          oldValOldPasswords = Convert.ToInt32(r.GetValue(1));
        }

        // 
        ExecuteSQLAsRoot("set old_passwords = 2;");
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          r.Read();
          newValOldPasswords = Convert.ToInt32(r.GetValue(1));
        }

        ExecuteSQLAsRoot(string.Format("set password for '{0}'@'localhost' = password( '123' );", user));
        ExecuteSQLAsRoot(string.Format("set old_passwords = {0};", oldValOldPasswords));

        string connstr = GetConnectionString(true);
        connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
        MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(connstr);
        csb.UserID = user;
        csb.Password = "123";
        using (MySqlConnection c = new MySqlConnection( csb.ConnectionString ))
        {
          ExecuteSQLAsRoot(string.Format("grant all on `{0}`.* to '{1}'@'localhost';",
            c.Settings.Database, user));
          c.Open();
          Assert.AreEqual(ConnectionState.Open, c.State);
        }
      }
      finally
      {
        // cleanup
        ExecuteSQLAsRoot(string.Format("drop user '{0}'@'localhost'", user));
      }
    } //*/   
#endif

#if !CF && !RT  //No Security.Principal on CF

    [Test]
    public void TestIntegratedSecurityNoPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, false);
    }

    [Test]
    public void TestIntegratedSecurityPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, true);
    }

    public void TestIntegratedSecurityWithUser(string user, bool pooling)
    {
      if (Version < new Version(5, 5)) return;

      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      // Check if server has windows authentication plugin is installed			
      MySqlCommand cmd = new MySqlCommand("show plugins", rootConn);

      bool haveWindowsAuthentication = false;
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        while (r.Read())
        {
          string name = (string)r["Name"];
          if (name == PluginName)
          {
            haveWindowsAuthentication = true;
            break;
          }
        }
      }
      if (!haveWindowsAuthentication)
        return;

      bool haveAuthWindowsUser = false;
      string pluginName = null;
      string authenticationString = "";

      // Check if predefined proxy user exists
      cmd.CommandText = string.Format(
        "select plugin, authentication_string from mysql.user where user='{0}'",
        UserName);
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        if (r.Read())
        {
          haveAuthWindowsUser = true;
          pluginName = (string)r["plugin"];
          authenticationString =
            (string)((r["authentication_string"] == DBNull.Value) ?
            "" : r["authentication_string"]);
        }
      }

      // Create mapping for current Windows user=>foo_user
      String windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      windowsUser = windowsUser.Replace("\\", "\\\\");
      string userMapping = windowsUser + "=foo_user";

      try
      {
        if (!haveAuthWindowsUser)
        {
          suExecSQL(
            "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
             userMapping + "'");
        }
        else
        {
          // extend mapping string for current user
          suExecSQL(
            "UPDATE mysql.user SET authentication_string='" + userMapping +
            "," + authenticationString + "' where user='" + UserName + "'");
        }
        suExecSQL("create user foo_user identified by 'pass'");
        suExecSQL("grant all privileges on *.* to 'foo_user'@'%'");
        suExecSQL("grant proxy on foo_user to " + UserName);


        // Finally, use IntegratedSecurity=true for the newly created user
        string connStr = GetConnectionString(true) + ";Integrated Security=SSPI";

        MySqlConnectionStringBuilder sb =
            new MySqlConnectionStringBuilder(connStr);
        sb.UserID = user;
        connStr = sb.ConnectionString;

        /* If pooling is requested, we'll  run test twice, with connection reset in between */
        if (pooling)
        {
          connStr += ";Connection Reset=true;Pooling=true";
        }
        int testIterations = pooling ? 2 : 1;

        int threadId = -1;
        for (int i = 0; i < testIterations; i++)
        {
          using (MySqlConnection c = new MySqlConnection(connStr))
          {
            c.Open();
            threadId = c.ServerThread;
            MySqlCommand command = new MySqlCommand("SELECT 1", c);
            long ret = (long)command.ExecuteScalar();
            Assert.AreEqual(ret, 1);

            command.CommandText = "select user()";
            string myUser = (string)command.ExecuteScalar();
            // Check if proxy user is correct
            Assert.IsTrue(myUser.StartsWith(UserName + "@"));

            // check if mysql user is correct 
            // (foo_user is mapped to current  OS user)
            command.CommandText = "select current_user()";
            string currentUser = (string)command.ExecuteScalar();
            Assert.IsTrue(currentUser.StartsWith("foo_user@"));
          }
        }

        if (pooling)
        {
          suExecSQL("KILL " + threadId);
        }
      }
      finally
      {
        // Cleanup

        // Drop test user
        suExecSQL("drop user foo_user");
        if (!haveAuthWindowsUser)
        {
          // drop proxy user if we created it
          suExecSQL("drop user " + UserName);
        }
        else
        {
          // revert changes in the mapping string
          suExecSQL("UPDATE mysql.user SET authentication_string='" +
            authenticationString + "' where user='" + UserName + "'");
        }
      }
    }

    [Test]
    public void TestIntegratedSecurityNoPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", false);
    }

    [Test]
    public void TestIntegratedSecurityPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", true);
    }

    public void TestIntegratedSecurityWithoutProxy(string user, bool pooling)
    {
      if (Version < new Version(5, 5)) return;

      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      // Check if server has windows authentication plugin is installed			
      MySqlCommand cmd = new MySqlCommand("show plugins", rootConn);

      bool haveWindowsAuthentication = false;
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        while (r.Read())
        {
          string name = (string)r["Name"];
          if (name == PluginName)
          {
            haveWindowsAuthentication = true;
            break;
          }
        }
      }
      if (!haveWindowsAuthentication)
        return;

      bool haveAuthWindowsUser = false;
      string pluginName = null;
      string authenticationString = "";

      // Check if predefined proxy user exists
      cmd.CommandText = string.Format(
        "select plugin, authentication_string from mysql.user where user='{0}'",
        UserName);
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        if (r.Read())
        {
          haveAuthWindowsUser = true;
          pluginName = (string)r["plugin"];
          authenticationString =
            (string)((r["authentication_string"] == DBNull.Value) ?
            "" : r["authentication_string"]);
        }
      }

      // Create mapping for current Windows user=>foo_user
      String windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      windowsUser = windowsUser.Replace("\\", "\\\\");
      string userMapping = windowsUser + ", Administrators";

      try
      {
        if (!haveAuthWindowsUser)
        {
          suExecSQL(
            "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
             userMapping + "'");
        }
        else
        {
          // extend mapping string for current user
          suExecSQL(
            "UPDATE mysql.user SET authentication_string='" + userMapping +
            "," + authenticationString + "' where user='" + UserName + "'");
        }
        suExecSQL(string.Format("grant all privileges on *.* to '{0}'@'%'", UserName));

        // Finally, use IntegratedSecurity=true for the newly created user
        string connStr = GetConnectionString(true) + ";Integrated Security=SSPI";

        MySqlConnectionStringBuilder sb =
            new MySqlConnectionStringBuilder(connStr);
        sb.UserID = user;
        connStr = sb.ConnectionString;

        /* If pooling is requested, we'll  run test twice, with connection reset in between */
        if (pooling)
        {
          connStr += ";Connection Reset=true;Pooling=true";
        }
        int testIterations = pooling ? 2 : 1;

        int threadId = -1;
        for (int i = 0; i < testIterations; i++)
        {
          using (MySqlConnection c = new MySqlConnection(connStr))
          {
            c.Open();
            threadId = c.ServerThread;
            MySqlCommand command = new MySqlCommand("SELECT 1", c);
            long ret = (long)command.ExecuteScalar();
            Assert.AreEqual(ret, 1);

            command.CommandText = "select user()";
            string myUser = (string)command.ExecuteScalar();
            // Check if proxy user is correct
            Assert.IsTrue(myUser.StartsWith(UserName + "@"));

            // check if mysql user is correct 
            // (foo_user is mapped to current  OS user)
            command.CommandText = "select current_user()";
            string currentUser = (string)command.ExecuteScalar();
            Assert.IsTrue(currentUser.StartsWith(UserName));
          }
        }

        if (pooling)
        {
          suExecSQL("KILL " + threadId);
        }
      }
      finally
      {
        // Cleanup

        // Drop test user
        suExecSQL(string.Format("drop user {0}", UserName));
      }
    }

    [Test]
    public void TestWinAuthWithoutProxyNoUserNoPooling()
    {
      TestIntegratedSecurityWithoutProxy(null, false);
    }

    [Test]
    public void TestWinAuthWithoutProxyNoUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }

    [Test]
    public void TestWinAuthWithoutProxyAndUser()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", false);
    }

    [Test]
    public void TestWinAuthWithoutProxyAndUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }
#endif

    [Test]
    public void TestConnectingSocketBadUserName()
    {
      suExecSQL("DELETE FROM mysql.user WHERE length(user) = 0");
      suExecSQL("FLUSH PRIVILEGES");

      string connStr = "server={0};user id=dummy;password=;database=Test;pooling=false";
      MySqlConnection c = new MySqlConnection(
          String.Format(connStr, host));
      try
      {
        c.Open();
        c.Close();
        throw new Exception("Open should not have worked");
      }
      catch (MySqlException)
      {
      }
    }

    [Test]
    public void TestConnectingSocketBadDbName()
    {
      string connStr = "server={0};user id={1};password={2};database=dummy; " +
          "pooling=false";
      MySqlConnection c = new MySqlConnection(
          String.Format(connStr, host, user, password));
      try
      {
        c.Open();
        c.Close();
        throw new Exception("Open should not have worked");
      }
      catch (MySqlException)
      {
      }
    }

    [Test]
    public void TestPersistSecurityInfoCachingPasswords()
    {
      string connStr = GetConnectionString(true);
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      c.Close();

      // this shouldn't work
      connStr = GetConnectionString(user, "bad_password", true);
      c = new MySqlConnection(connStr);
      try
      {
        c.Open();
        Assert.Fail("Thn is should not work");
        c.Close();
        return;
      }
      catch (MySqlException)
      {
      }

      // this should work
      connStr = GetConnectionString(true);
      c = new MySqlConnection(connStr);
      c.Open();
      c.Close();
    }

    [Test]
    public void ChangeDatabase()
    {
      string connStr = GetConnectionString(true);
      MySqlConnection c = new MySqlConnection(connStr + ";pooling=false");
      c.Open();
      Assert.IsTrue(c.State == ConnectionState.Open);

      Assert.AreEqual(database0.ToLower(), c.Database.ToLower());

      c.ChangeDatabase(database1);

      Assert.AreEqual(database1.ToLower(), c.Database.ToLower());

      c.Close();
    }

    [Test]
    public void ConnectionTimeout()
    {
      MySqlConnection c = new MySqlConnection(
          "server=1.1.1.1;user id=bogus;pwd=bogus;Connection timeout=5;" +
          "pooling=false");
      DateTime start = DateTime.Now;
      try
      {
        c.Open();
      }
      catch (Exception)
      {
        TimeSpan diff = DateTime.Now.Subtract(start);
        Assert.IsTrue(diff.TotalSeconds < 15, "Timeout exceeded");
      }
    }

    /*        [Test]
            public void AnonymousLogin()
            {
                suExecSQL(String.Format("GRANT ALL ON *.* to ''@'{0}' IDENTIFIED BY 'set_to_blank'", host));
                suExecSQL("UPDATE mysql.user SET password='' WHERE password='set_to_blank'");

                MySqlConnection c = new MySqlConnection(String.Empty);
                c.Open();
                c.Close();
            }
            */
    [Test]
    public void ConnectInVariousWays()
    {
      // connect with no db
      string connStr2 = GetConnectionString(false);
      MySqlConnection c = new MySqlConnection(connStr2);
      c.Open();
      c.Close();

      suExecSQL("GRANT ALL ON *.* to 'nopass'@'%'");
      suExecSQL("GRANT ALL ON *.* to 'nopass'@'localhost'");
      suExecSQL("FLUSH PRIVILEGES");

      // connect with no password
      connStr2 = GetConnectionString("nopass", null, false);
      c = new MySqlConnection(connStr2);
      c.Open();
      c.Close();

      connStr2 = GetConnectionString("nopass", "", false);
      c = new MySqlConnection(connStr2);
      c.Open();
      c.Close();
    }

    [Test]
    public void ConnectingAsUTF8()
    {
      if (Version < new Version(4, 1)) return;

      string connStr = GetConnectionString(true) + ";charset=utf8";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand(
            "CREATE TABLE test (id varbinary(16), active bit) CHARACTER SET utf8", conn);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x1234567890 AS Binary), true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x123456789a AS Binary), true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x123456789b AS Binary), true)";
        cmd.ExecuteNonQuery();
      }

      using (MySqlConnection d = new MySqlConnection(connStr))
      {
        d.Open();

        MySqlCommand cmd2 = new MySqlCommand("SELECT id, active FROM test", d);
        using (MySqlDataReader reader = cmd2.ExecuteReader())
        {
          Assert.IsTrue(reader.Read());
          Assert.IsTrue(reader.GetBoolean(1));
        }
      }
    }

    /// <summary>
    /// Bug #10281 Clone issue with MySqlConnection 
    /// Bug #27269 MySqlConnection.Clone does not mimic SqlConnection.Clone behaviour 
    /// </summary>
    [Test]
    public void TestConnectionClone()
    {
      MySqlConnection c = new MySqlConnection();
      MySqlConnection clone = (MySqlConnection)((ICloneable)c).Clone();
      clone.ToString();

      string connStr = GetConnectionString(true);
      connStr = connStr.Replace("persist security info=true", "persist security info=false");
      c = new MySqlConnection(connStr);
      c.Open();
      c.Close();
      MySqlConnection c2 = (MySqlConnection)((ICloneable)c).Clone();
      c2.Open();
      c2.Close();
    }

    /// <summary>
    /// Bug #13321  	Persist security info does not woek
    /// </summary>
    [Test]
    public void PersistSecurityInfo()
    {
      string s = GetConnectionString(true).ToLower();
      int start = s.IndexOf("persist security info");
      int end = s.IndexOf(";", start);
      string connStr = s.Substring(0, start);
      connStr += s.Substring(end, s.Length - (end));

      string p = "password";
      if (connStr.IndexOf("pwd") != -1)
        p = "pwd";
      else if (connStr.IndexOf("passwd") != -1)
        p = "passwd";

      string newConnStr = connStr + ";persist security info=true";
      MySqlConnection conn2 = new MySqlConnection(newConnStr);
      Assert.IsTrue(conn2.ConnectionString.IndexOf(p) != -1);
      conn2.Open();
      conn2.Close();
      Assert.IsTrue(conn2.ConnectionString.IndexOf(p) != -1);

      newConnStr = connStr + ";persist security info=false";
      conn2 = new MySqlConnection(newConnStr);
      Assert.IsTrue(conn2.ConnectionString.IndexOf(p) != -1);
      conn2.Open();
      conn2.Close();
      Assert.IsTrue(conn2.ConnectionString.IndexOf(p) == -1);
    }

    /// <summary>
    /// Bug #13658  	connection.state does not update on Ping()
    /// </summary>
    [Test]
    public void PingUpdatesState()
    {
      MySqlConnection conn2 = new MySqlConnection(GetConnectionString(true));
      conn2.Open();
      KillConnection(conn2);
      Assert.IsFalse(conn2.Ping());
      Assert.IsTrue(conn2.State == ConnectionState.Closed);
      conn2.Open();
      conn2.Close();
    }

    /// <summary>
    /// Bug #16659  	Can't use double quotation marks(") as password access server by Connector/NET
    /// </summary>
    [Test]
    public void ConnectWithQuotePassword()
    {
      suExecSQL("GRANT ALL ON *.* to 'quotedUser'@'%' IDENTIFIED BY '\"'");
      suExecSQL("GRANT ALL ON *.* to 'quotedUser'@'localhost' IDENTIFIED BY '\"'");
      string connStr = GetConnectionString("quotedUser", null, false);
      connStr += ";pwd='\"'";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #24802 Error Handling 
    /// </summary>
    [Test]
    public void TestConnectingSocketBadHostName()
    {
      string connStr = "server=foobar;user id=foouser;password=;database=Test;" +
          "pooling=false";
      try
      {
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();
        }
      }
      catch (MySqlException ex)
      {
        Assert.AreEqual((int)MySqlErrorCode.UnableToConnectToHost, ex.Number);
      }
    }

    /// <summary>
    /// Bug #29123  	Connection String grows with each use resulting in OutOfMemoryException
    /// </summary>
    [Test]
    public void ConnectionStringNotAffectedByChangeDatabase()
    {
      for (int i = 0; i < 10; i++)
      {
        string connStr = GetConnectionString(true) + ";pooling=false";
        connStr = connStr.Replace("database", "Initial Catalog");
        connStr = connStr.Replace("persist security info=true",
            "persist security info=false");
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();
          string str = c.ConnectionString;
          int index = str.IndexOf("Database=");
          Assert.AreEqual(-1, index);
        }
      }
    }


    class ConnectionClosedCheck
    {
      public bool closed = false;
      public void stateChangeHandler(object sender, StateChangeEventArgs e)
      {
        if (e.CurrentState == ConnectionState.Closed)
          closed = true;
      }
    }
    [Test]
    public void ConnectionCloseByGC()
    {
      int threadId;
      ConnectionClosedCheck check = new ConnectionClosedCheck();
      string connStr = GetConnectionString(true) + ";pooling=true";
      MySqlConnection c = new MySqlConnection(connStr);
      c.StateChange += new StateChangeEventHandler(check.stateChangeHandler);
      c.Open();
      threadId = c.ServerThread;
      WeakReference wr = new WeakReference(c);
      Assert.IsTrue(wr.IsAlive);
      c = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      Assert.IsFalse(wr.IsAlive);
      Assert.IsTrue(check.closed);

      MySqlCommand cmd = new MySqlCommand("KILL " + threadId, conn);
      cmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Bug #30964 StateChange imperfection 
    /// </summary>
    MySqlConnection rqConnection;
    [Test]
    public void RunningAQueryFromStateChangeHandler()
    {
      string connStr = GetConnectionString(true);
      using (rqConnection = new MySqlConnection(connStr))
      {
        rqConnection.StateChange += new StateChangeEventHandler(RunningQueryStateChangeHandler);
        rqConnection.Open();
      }
    }

    void RunningQueryStateChangeHandler(object sender, StateChangeEventArgs e)
    {
      if (e.CurrentState == ConnectionState.Open)
      {
        MySqlCommand cmd = new MySqlCommand("SELECT 1", rqConnection);
        object o = cmd.ExecuteScalar();
        Assert.AreEqual(1, o);
      }
    }

    /// <summary>
    /// Bug #31262 NullReferenceException in MySql.Data.MySqlClient.NativeDriver.ExecuteCommand 
    /// </summary>
    [Test]
    public void ConnectionNotOpenThrowningBadException()
    {
      MySqlConnection c2 = new MySqlConnection();
      c2.ConnectionString = GetConnectionString(true); // "DataSource=localhost;Database=test;UserID=root;Password=********;PORT=3306;Allow Zero Datetime=True;logging=True;";
      //conn.Open();                      << REM
      MySqlCommand command = new MySqlCommand();
      command.Connection = c2;

      MySqlCommand cmdCreateTable = new MySqlCommand("DROP TABLE IF EXISTS `test`.`contents_catalog`", c2);
      cmdCreateTable.CommandType = CommandType.Text;
      cmdCreateTable.CommandTimeout = 0;
      try
      {
        cmdCreateTable.ExecuteNonQuery();
      }
      catch (InvalidOperationException)
      {
      }
    }

    /// <summary>
    /// Bug #31433 Username incorrectly cached for logon where case sensitive 
    /// </summary>
    [Test]
    public void CaseSensitiveUserId()
    {
      string connStr = GetConnectionString("Test", "test", true);
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        try
        {
          c.Open();
        }
        catch (MySqlException)
        {
        }
      }

      connStr = GetConnectionString("test", "test", true);
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #35619 creating a MySql connection from toolbox generates an error 
    /// </summary>
    [Test]
    public void NullConnectionString()
    {
      MySqlConnection c = new MySqlConnection();
      c.ConnectionString = null;
    }

    /// <summary>
    /// Bug #53097  	Connection.Ping() closes connection if executed on a connection with datareader
    /// </summary>
    [Test]
    public void PingWhileReading()
    {
      MySqlCommand command = new MySqlCommand("SELECT 1", conn);

      using (MySqlDataReader reader = command.ExecuteReader())
      {
        reader.Read();
        try
        {
          conn.Ping();
          Assert.Fail("Test Failed.");
        }
        catch (MySqlException ex)
        {
          Assert.AreEqual(Resources.DataReaderOpen, ex.Message);
        }
      }
    }

    /// <summary>
    /// Test if keepalive parameters work.
    /// </summary>
    [Test]
    public void Keepalive()
    {
      string connstr = GetConnectionString("test", "test", true);
      connstr += ";keepalive=1;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
      }
    }

#if !CF && !RT
    [Test]
    public void CanOpenConnectionInMediumTrust()
    {
      AppDomain appDomain = PartialTrustSandbox.CreatePartialTrustDomain();

      PartialTrustSandbox sandbox = (PartialTrustSandbox)appDomain.CreateInstanceAndUnwrap(
          typeof(PartialTrustSandbox).Assembly.FullName,
          typeof(PartialTrustSandbox).FullName);

      try
      {
        MySqlConnection connection = sandbox.TryOpenConnection(GetConnectionString(true));
        Assert.IsNotNull(connection);
        Assert.IsTrue(connection.State == ConnectionState.Open);
        connection.Close();

        //Now try with logging enabled
        connection = sandbox.TryOpenConnection(GetConnectionString(true) + ";logging=true");
        Assert.IsNotNull(connection);
        Assert.IsTrue(connection.State == ConnectionState.Open);
        connection.Close();

        //Now try with Usage Advisor enabled
        connection = sandbox.TryOpenConnection(GetConnectionString(true) + ";Use Usage Advisor=true");
        Assert.IsNotNull(connection);
        Assert.IsTrue(connection.State == ConnectionState.Open);
        connection.Close();
      }
      finally
      {
        AppDomain.Unload(appDomain);
      }
    }

    /// <summary>
    /// A client can connect to MySQL server using SSL and a pfx file.
    /// <remarks>
    /// This test requires starting the server with SSL support. 
    /// For instance, the following command line enables SSL in the server:
    /// mysqld --no-defaults --standalone --console --ssl-ca='MySQLServerDir'\mysql-test\std_data\cacert.pem --ssl-cert='MySQLServerDir'\mysql-test\std_data\server-cert.pem --ssl-key='MySQLServerDir'\mysql-test\std_data\server-key.pem
    /// </remarks>
    /// </summary>
    [Test]
    public void CanConnectUsingFileBasedCertificate()
    {
      if (Version < new Version(5, 1)) return;

      string connstr = GetConnectionString(true);
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }
    }
#endif

#if CF
        /// <summary>
        /// A client running in .NET Compact Framework can't connect to MySQL server using SSL and a pfx file.
        /// <remarks>
        /// This test requires starting the server with SSL support. 
        /// For instance, the following command line enables SSL in the server:
        /// mysqld --no-defaults --standalone --console --ssl-ca='MySQLServerDir'\mysql-test\std_data\cacert.pem --ssl-cert='MySQLServerDir'\mysql-test\std_data\server-cert.pem --ssl-key='MySQLServerDir'\mysql-test\std_data\server-key.pem
        /// </remarks>
        /// </summary>
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void CannotConnectUsingFileBasedCertificateInCF()
        {
            string connstr = GetConnectionString(true);
            connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";

            MySqlConnection c = new MySqlConnection(connstr);
        }
#endif

    [Test]
    public void CanOpenConnectionAfterAborting()
    {
      MySqlConnection connection = new MySqlConnection(GetConnectionString(true));
      connection.Open();
      Assert.AreEqual(ConnectionState.Open, connection.State);

      connection.Abort();
      Assert.AreEqual(ConnectionState.Closed, connection.State);

      connection.Open();
      Assert.AreEqual(ConnectionState.Open, connection.State);

      connection.Close();
    }

    /// <summary>
    /// A client can authenticate to a server that requires using old passwords.
    /// <remarks>
    /// This test requires starting the server with the old-passwords option.
    /// </remarks>
    /// </summary>
    [Test]
    public void CanAuthenticateUsingOldPasswords()
    {
      suExecSQL(String.Format("GRANT USAGE ON `{0}`.* TO 'oldpassworduser'@'%' IDENTIFIED BY '123456'", database0));
      suExecSQL(String.Format("GRANT SELECT ON `{0}`.* TO 'oldpassworduser'@'%'", database0));

      MySqlConnection connection = null;

      Assert.DoesNotThrow(delegate()
      {
        try
        {
          connection = new MySqlConnection(GetConnectionString("oldpassworduser", "123456", true));
          connection.Open();

          Assert.IsTrue(connection.State == ConnectionState.Open);
          connection.Close();
        }
        finally
        {
          suExecSQL("drop user oldpassworduser");
        }
      });
    }


#if !CF
    /// <summary>
    /// Fix for bug http://bugs.mysql.com/bug.php?id=63942 (Connections not closed properly when using pooling)
    /// </summary>
    [Test]
    public void ReleasePooledConnectionsProperly()
    {
      MySqlConnection con = new MySqlConnection(GetConnectionString(true));
      MySqlCommand cmd = new MySqlCommand( "show global status like 'aborted_clients'", con );
      con.Open();
      MySqlDataReader r = cmd.ExecuteReader();
      r.Read();
      int numClientsAborted = r.GetInt32( 1 );
      r.Close();

#if !RT
      AppDomain appDomain = FullTrustSandbox.CreateFullTrustDomain();
      

      FullTrustSandbox sandbox = (FullTrustSandbox)appDomain.CreateInstanceAndUnwrap(
          typeof(FullTrustSandbox).Assembly.FullName,
          typeof(FullTrustSandbox).FullName);
#endif

      try
      {
        for (int i = 0; i < 200; i++)
        {
#if RT
          MySqlConnection connection = new MySqlConnection(GetPoolingConnectionString());
          connection.Open();
#else
          MySqlConnection connection = sandbox.TryOpenConnection(GetPoolingConnectionString());
#endif
          Assert.IsNotNull(connection);
          Assert.IsTrue(connection.State == ConnectionState.Open);
          connection.Close();
        }
      }
      finally
      {
#if !RT
        AppDomain.Unload(appDomain);
#endif
      }
      r = cmd.ExecuteReader();
      r.Read();
      int numClientsAborted2 = r.GetInt32( 1 );
      r.Close();
      Assert.AreEqual( numClientsAborted, numClientsAborted2 );
      con.Close();
    }  
#endif

    /// <summary>
    /// Test for Connect attributes feature used in MySql Server > 5.6.6
    /// (Stores client connection data on server)
    /// </summary>
    [Test]
    public void ConnectAttributes()
    {
      if (Version < new Version(5, 6, 6)) return;
      using (MySqlConnection connection = new MySqlConnection(GetConnectionString(rootUser, rootPassword, false)))
      {
        connection.Open();
        if (connection.driver.SupportsConnectAttrs)
        {
          MySqlCommand cmd = new MySqlCommand("SELECT * FROM performance_schema.session_connect_attrs WHERE PROCESSLIST_ID = connection_id()", connection);
          MySqlDataReader dr = cmd.ExecuteReader();
          Assert.True(dr.HasRows, "No session_connect_attrs found");
          MySqlConnectAttrs connectAttrs = new MySqlConnectAttrs();
          bool isValidated = false;
          while (dr.Read())
          {
            if (dr.GetString(1) == "_client_name")
            {
              Assert.AreEqual(connectAttrs.ClientName, dr.GetString(2));
              isValidated = true;
              break;
            }
          }
          Assert.True(isValidated, "Missing _client_version attribute");
        }
      }
    }

    /// <summary>
    /// Test for password expiration feature in MySql Server 5.6 or higher
    /// </summary>
    [Test]
    public void PasswordExpiration()
    {
      const string expireduser = "expireduser";
      const string expiredhost = "localhost";
      string expiredfull = string.Format("'{0}'@'{1}'", expireduser, expiredhost);

      using (MySqlConnection conn = new MySqlConnection(GetConnectionString(rootUser, rootPassword, true)))
      {
        conn.Open();
        if (Version >= new Version(5, 6, 6))
        {
          MySqlCommand cmd = new MySqlCommand("", conn);

          // creates expired user
          cmd.CommandText = string.Format("SELECT COUNT(*) FROM mysql.user WHERE user='{0}' AND host='{1}'", expireduser, expiredhost);
          long count = (long)cmd.ExecuteScalar();
          if (count > 0)
            suExecSQL(string.Format("DROP USER " + expiredfull));

          suExecSQL(string.Format("CREATE USER {0} IDENTIFIED BY '{1}1'", expiredfull, expireduser));
          suExecSQL(string.Format("GRANT SELECT ON `{0}`.* TO {1}", conn.Database, expiredfull));
          suExecSQL(string.Format("ALTER USER {0} PASSWORD EXPIRE", expiredfull));
          conn.Close();

          // validates expired user
          conn.ConnectionString = GetConnectionString(expireduser, expireduser + "1", true);
          conn.Open();
          try
          {
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();
            Assert.Fail();
          }
          catch (MySqlException ex)
          {
            Assert.AreEqual(1820, ex.Number);
          }
          cmd.CommandText = string.Format("SET PASSWORD = PASSWORD('{0}1')", expireduser);
          cmd.ExecuteNonQuery();
          conn.Close();

          conn.Open();
          cmd.CommandText = "SELECT 1";
          cmd.ExecuteScalar();

          suExecSQL(string.Format("DROP USER " + expiredfull));
        }
        else
        {
          System.Diagnostics.Debug.WriteLine("Password expire not supported in this server version.");
        }
      }
    }

    /// <summary>
    /// As part of feedback from bug http://bugs.mysql.com/bug.php?id=66647 (Arithmetic operation resulted in an overflow).
    /// </summary>
    [Test]
    public void OldPasswordNotSupported()
    {
      //get value of flag 'old_passwords'
      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(GetConnectionString(true));
      MySqlConnection con = new MySqlConnection( csb.ToString() );
      MySqlCommand cmd = new MySqlCommand("show variables like 'old_passwords'", con);
      string db = con.Settings.Database;
      con.Open();
      MySqlDataReader r = cmd.ExecuteReader();
      r.Read();
      object o = r.GetValue(1);
      if (o.ToString() == "OFF")
        o = "0";
      int old_passwords = Convert.ToInt32( o );
      r.Close();
      con.Close();
      if (old_passwords == 0)
      {
        //System.Diagnostics.Debug.Write("This test must be ran with old_passwords=0");
        ExecuteSQLAsRoot("set old_passwords=1;");
        //return;
      }
      // create user
      cmd.CommandText = "select count( * ) from mysql.user where user = 'myoldpassuser' and host = 'localhost'";
      cmd.Connection = rootConn;
      int n = Convert.ToInt32(cmd.ExecuteScalar());
      if (n != 0)
      {
        ExecuteSQLAsRoot("drop user 'myoldpassuser'@'localhost'");
      }
      // user with old password is different depending upon the version.
      if (Version.Minor >= 6 )
      {
        ExecuteSQLAsRoot("create user 'myoldpassuser'@'localhost' IDENTIFIED with 'mysql_old_password'");
      }
      else
      {
        ExecuteSQLAsRoot("create user 'myoldpassuser'@'localhost' ");
      }
      bool bExceptionThrown = false;
      try
      {
        // setup user with old password, attempt to open connection with it, must fail
        ExecuteSQLAsRoot(string.Format("grant all on `{0}`.* to 'myoldpassuser'@'localhost'", db));
        ExecuteSQLAsRoot("set password for 'myoldpassuser'@'localhost' = old_password( '123' )");
        //con.Settings.UserID = "myoldpassuser";
        //con.Settings.Password = "123";
        csb.UserID = "myoldpassuser";
        csb.Password = "123";
        con.ConnectionString = csb.ToString();
        con.Open();
        con.Close();
      }
      catch (MySqlException e )
      {
        Assert.AreEqual( Resources.OldPasswordsNotSupported, e.Message);
        bExceptionThrown = true;
      }
      catch (Exception)
      {
        Assert.Fail();
      }
      finally
      {
        if (old_passwords == 0)
        {
          ExecuteSQLAsRoot("set old_passwords=0;");
        }
        ExecuteSQLAsRoot("drop user 'myoldpassuser'@'localhost'");
        Assert.AreEqual(true, bExceptionThrown);
      }
    }	
  }
}
