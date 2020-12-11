// Copyright (c) 2016, 2020 Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient.Authentication;
using System;
using System.Data;
using NUnit.Framework;
using MySql.Data.Common;
using System.Text;

namespace MySql.Data.MySqlClient.Tests
{
  public class AuthTests : TestBase
  {
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      var users = Utils.FillTable(("SELECT user, host FROM mysql.user WHERE user NOT LIKE 'mysql%' AND user NOT LIKE 'root'"), Root);
      foreach (DataRow row in users.Rows)
        ExecuteSQL(string.Format("DROP USER '{0}'@'{1}'", row[0].ToString(), row[1].ToString()), true);
      ExecuteSQL("FLUSH PRIVILEGES", true);
    }

    #region Windows Authentication Plugin

    [Test]
    [Property("Category", "Security")]
    public void TestIntegratedSecurityNoPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, false);
    }

    [Test]
    [Property("Category", "Security")]
    public void TestIntegratedSecurityPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, true);
    }

    [Test]
    [Property("Category", "Security")]
    public void TestIntegratedSecurityNoPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", false);
    }

    [Test]
    [Property("Category", "Security")]
    public void TestIntegratedSecurityPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", true);
    }

    [Test]
    [Property("Category", "Security")]
    public void TestWinAuthWithoutProxyNoUserNoPooling()
    {
      TestIntegratedSecurityWithoutProxy(null, false);
    }

    [Test]
    [Property("Category", "Security")]
    public void TestWinAuthWithoutProxyNoUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }

    [Test]
    [Property("Category", "Security")]
    public void TestWinAuthWithoutProxyAndUser()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", false);
    }

    [Test]
    [Property("Category", "Security")]
    public void TestWinAuthWithoutProxyAndUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }

    private void TestIntegratedSecurityWithoutProxy(string user, bool pooling)
    {
      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      // Check if server has windows authentication plugin is installed
      MySqlCommand cmd = new MySqlCommand("show plugins", Root);

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
      string userMapping = "fergs, Administrators";

      try
      {
        if (!haveAuthWindowsUser)
        {
          ExecuteSQL(
            "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
             userMapping + "'", true);
        }
        else
        {
          // extend mapping string for current user
          ExecuteSQL(
            "UPDATE mysql.user SET authentication_string='" + userMapping +
            "," + authenticationString + "' where user='" + UserName + "'", true);
        }
        ExecuteSQL(string.Format("grant all privileges on *.* to '{0}'@'%'", UserName), true);


        // Finally, use IntegratedSecurity=true for the newly created user
        string connStr = Root.ConnectionString + ";Integrated Security=SSPI";

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
            Assert.AreEqual(1, ret);

            command.CommandText = "select user()";
            string myUser = (string)command.ExecuteScalar();
            // Check if proxy user is correct
            StringAssert.StartsWith(UserName + "@", myUser);

            // check if mysql user is correct
            // (foo_user is mapped to current  OS user)
            command.CommandText = "select current_user()";
            string currentUser = (string)command.ExecuteScalar();
            StringAssert.StartsWith(UserName, currentUser);
          }
        }

        if (pooling)
        {
          ExecuteSQL("KILL " + threadId, true);
        }
      }
      finally
      {
        // Cleanup

        // Drop test user
        ExecuteSQL(string.Format("drop user {0}", UserName), true);
      }
    }

    private void TestIntegratedSecurityWithUser(string user, bool pooling)
    {
      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      // Check if server has windows authentication plugin is installed
      MySqlDataReader reader = ExecuteReader("show plugins", true);

      bool haveWindowsAuthentication = false;
      using (reader)
      {
        if (reader.HasRows)
        {
          while (reader.Read())
          {
            string name = (string)reader["Name"];
            if (name == PluginName)
            {
              haveWindowsAuthentication = true;
              break;
            }
          }
        }
      }
      if (!haveWindowsAuthentication)
        return;

      bool haveAuthWindowsUser = false;
      string pluginName = null;
      string authenticationString = "";

      // Check if predefined proxy user exists
      string sql = string.Format("select plugin, authentication_string from mysql.user where user='{0}'", UserName);
      using (MySqlDataReader reader2 = ExecuteReader(sql, true))
      {
        if (reader2.Read())
        {
          haveAuthWindowsUser = true;
          pluginName = (string)reader2["plugin"];
          authenticationString =
            (string)((reader2["authentication_string"] == DBNull.Value) ?
            "" : reader2["authentication_string"]);
        }
      }

      // Create mapping for current Windows user=>foo_user
#pragma warning disable CS1702 // Assuming assembly reference matches identity
      String windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
#pragma warning restore CS1702 // Assuming assembly reference matches identity
      windowsUser = windowsUser.Replace("\\", "\\\\");
      string userMapping = windowsUser + "=foo_user";

      if (!haveAuthWindowsUser)
      {
        ExecuteSQL($"DROP USER IF EXISTS {UserName}");
        ExecuteSQL(
          "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
           userMapping + "'", true);
      }
      else
      {
        // extend mapping string for current user
        ExecuteSQL(
          "UPDATE mysql.user SET authentication_string='" + userMapping +
          "," + authenticationString + "' where user='" + UserName + "'", true);
      }
      ExecuteSQL($"DROP USER IF EXISTS foo_user");
      ExecuteSQL("create user foo_user identified by 'pass'", true);
      ExecuteSQL("grant all privileges on *.* to 'foo_user'@'%'", true);
      ExecuteSQL("grant proxy on foo_user to " + UserName, true);


      // Finally, use IntegratedSecurity=true for the newly created user
      string connStr = Root.ConnectionString + ";Integrated Security=SSPI";

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
          Assert.AreEqual(1, ret);

          command.CommandText = "select user()";
          string myUser = (string)command.ExecuteScalar();
          // Check if proxy user is correct
          StringAssert.StartsWith(UserName + "@", myUser);

          // check if mysql user is correct
          // (foo_user is mapped to current  OS user)
          command.CommandText = "select current_user()";
          string currentUser = (string)command.ExecuteScalar();
          StringAssert.StartsWith("foo_user@", currentUser);
        }
      }

      if (pooling)
      {
        ExecuteSQL("KILL " + threadId, true);
      }
    }

    #endregion

    #region MySql Native Password Authentication Plugin

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingMySqlNativePasswordPlugin()
    {
      string userName = "testNtvPass";
      string password = "mysql";
      string pluginName = "mysql_native_password";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      settings.UserID = userName;
      settings.Password = password;
      settings.Database = "";
      CreateUser(userName, password, pluginName);

      // User with password over TLS connection.
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.StartsWith("TLSv1", reader.GetString(1));
        }

        command.CommandText = String.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.AreEqual(userName, reader.GetString(0));
          Assert.AreEqual(pluginName, reader.GetString(1));
        }

        connection.Close();
      }

      // User with password over non-TLS connection.
      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    #endregion

    #region Sha256 Password Authentication Plugin

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingSha256PasswordPlugin()
    {
      if (Version <= new Version("5.6")) return;

      string userName = "testSha256";
      string password = "mysql";
      string pluginName = "sha256_password";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      settings.UserID = userName;
      settings.Password = password;
      settings.Database = "";
      CreateUser(userName, password, pluginName);

      // User with password over TLS connection.
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.StartsWith("TLSv1", reader.GetString(1));
        }

        command.CommandText = String.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.AreEqual(userName, reader.GetString(0));
          Assert.AreEqual(pluginName, reader.GetString(1));
        }

        connection.Close();
      }

      // Connect over non-TLS connection using RSA keys. Only available in servers compiled with OpenSSL (E.g. Commercial)
      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Rsa_public_key';", Connection);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          if (reader.HasRows)
          {
            reader.Read();
            if (!string.IsNullOrEmpty(reader.GetString(1))) serverCompiledUsingOpenSsl = true;
          }
        }
      }

      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        if (serverCompiledUsingOpenSsl)
        {
          Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
          Assert.AreEqual("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        }
        else Assert.Throws<MySqlException>(() => connection.Open());
      }

      if (serverCompiledUsingOpenSsl)
      {
        settings.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
        {
          connection.Open();
          connection.Close();
        }
        settings.AllowPublicKeyRetrieval = false;
      }

      // User without password over TLS connection.
      password = "";
      settings.Password = password;
      CreateUser(userName, password, pluginName);
      settings.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.StartsWith("TLSv1", reader.GetString(1));
        }

        command.CommandText = String.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.AreEqual(userName, reader.GetString(0));
          Assert.AreEqual(pluginName, reader.GetString(1));
        }

        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void AllowPublicKeyRetrievalForSha256PasswordPlugin()
    {
      if (Version <= new Version("5.6")) return;

      string userName = "testSha256";
      string password = "mysql";
      string pluginName = "sha256_password";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      settings.UserID = userName;
      settings.Password = password;
      settings.Database = "";
      CreateUser(userName, password, pluginName);

      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Rsa_public_key';", Connection);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          if (reader.HasRows)
          {
            reader.Read();
            if (!string.IsNullOrEmpty(reader.GetString(1))) serverCompiledUsingOpenSsl = true;
          }
        }
      }

      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => connection.Open()); ;
        if (serverCompiledUsingOpenSsl)
          Assert.AreEqual("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        else
          StringAssert.StartsWith("Authentication to host", ex.Message);
      }

      if (serverCompiledUsingOpenSsl)
      {
        settings.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
        {
          connection.Open();
          connection.Close();
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void EmptyPasswordOnSslDisabledSha256Password()
    {
      if (Version <= new Version("5.6")) return;

      string userName = "testSha256";
      string password = "";
      string pluginName = "sha256_password";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      settings.UserID = userName;
      settings.Password = password;
      CreateUser(userName, password, pluginName);

      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    #endregion

    #region Caching Sha2 Password Authentication Plugin

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingCachingSha2Plugin()
    {
      if (Version < new Version(8, 0, 3)) return;

      MySqlDataReader pluginReader = ExecuteReader("SELECT * FROM INFORMATION_SCHEMA.PLUGINS WHERE PLUGIN_NAME = 'caching_sha2_password'");
      if (!pluginReader.HasRows)
        throw new Exception("The caching_sha2_password plugin isn't available.");
      pluginReader.Close();

      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.UserID = "testCachingSha2";
      builder.Password = "test";
      builder.Database = "";
      CreateUser(builder.UserID, builder.Password, pluginName);

      // Authentication success with full authentication - TLS connection.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.connectionState);
        Assert.AreEqual(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // Authentication success with fast authentication - Any connection.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.connectionState);
        Assert.AreEqual(AuthStage.FAST_AUTH, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // Flush privileges clears the cache.
      ExecuteSQL("flush privileges");
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // Authentication failure - TLS connection.
      builder.Password = "incorrectPassword";
      Exception ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
      StringAssert.StartsWith("Access denied for user", ex.InnerException.Message);

      // Authentication success with empty password – Any connection.
      builder.UserID = "testCachingSha2NoPassword";
      builder.Password = "";
      CreateUser(builder.UserID, builder.Password, pluginName);

      // TLS enabled.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.connectionState);
        Assert.AreEqual(AuthStage.GENERATE_SCRAMBLE, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // TLS not enabled.
      builder.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.connectionState);
        Assert.AreEqual(AuthStage.GENERATE_SCRAMBLE, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // Authentication failure with empty password – Any connection.
      // TLS enabled.
      builder.UserID = "testCachingSha2";
      builder.SslMode = MySqlSslMode.Required;
      ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
      StringAssert.StartsWith("Access denied for user", ex.InnerException.Message);

      // TLS not enabled.
      builder.SslMode = MySqlSslMode.None;
      ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
      StringAssert.StartsWith("Access denied for user", ex.InnerException.Message);

      // Authentication using RSA keys. Only available in servers compiled with OpenSSL (E.g. Commercial).
      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Rsa_public_key';", Connection);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          if (reader.HasRows)
          {
            reader.Read();
            if (!string.IsNullOrEmpty(reader.GetString(1))) serverCompiledUsingOpenSsl = true;
          }
        }
      }

      if (serverCompiledUsingOpenSsl)
      {
        builder.UserID = "testCachingSha2";
        builder.Password = "test";
        builder.SslMode = MySqlSslMode.None;

        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          ex = Assert.Throws<MySqlException>(() => connection.Open());
          Assert.AreEqual("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        }

        builder.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          connection.Open();
          Assert.AreEqual(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
          connection.Close();
        }

        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          connection.Open();
          Assert.AreEqual(AuthStage.FAST_AUTH, CachingSha2AuthenticationPlugin._authStage);
          connection.Close();
        }

        // Flush privileges clears the cache.
        ExecuteSQL("flush privileges");
        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          connection.Open();
          Assert.AreEqual(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
          connection.Close();
        }

        builder.Password = "incorrectPassword";
        ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
        StringAssert.StartsWith("Access denied for user", ex.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void AllowPublicKeyRetrievalForCachingSha2PasswordPlugin()
    {
      if (Version < new Version("8.0.3")) return;

      string userName = "testCachingSha2";
      string password = "mysql";
      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      settings.UserID = userName;
      settings.Password = password;
      settings.Database = "";
      CreateUser(userName, password, pluginName);

      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Rsa_public_key';", Connection);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          if (reader.HasRows)
          {
            reader.Read();
            if (!string.IsNullOrEmpty(reader.GetString(1))) serverCompiledUsingOpenSsl = true;
          }
        }
      }

      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
        if (serverCompiledUsingOpenSsl)
          Assert.AreEqual("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        else
          StringAssert.StartsWith("Authentication to host", ex.Message);
      }

      if (serverCompiledUsingOpenSsl)
      {
        settings.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
        {
          connection.Open();
          connection.Close();
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void CachingSha2AuthFailsAfterFlushPrivileges()
    {
      if (Version < new Version("8.0.3")) return;

      string userName = "testCachingSha2";
      string password = "mysql";
      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      settings.UserID = userName;
      settings.Password = password;
      settings.Database = "";
      CreateUser(userName, password, pluginName);

      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Rsa_public_key';", Connection);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          if (reader.HasRows)
          {
            reader.Read();
            if (!string.IsNullOrEmpty(reader.GetString(1))) serverCompiledUsingOpenSsl = true;
          }
        }
      }

      settings.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        Assert.True(connection.State == ConnectionState.Open);
        connection.Close();
      }

      // Success since the user exists in the cache.
      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        Assert.True(connection.State == ConnectionState.Open);
        connection.Close();
      }

      ExecuteSQL("flush privileges");

      // Fail since the user no longer exists in the cache and public key retrieval is disabled by default.
      Exception ex = null;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        ex = Assert.Throws<MySqlException>(() => connection.Open());
        if (serverCompiledUsingOpenSsl)
          Assert.AreEqual("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        else
          StringAssert.StartsWith("Authentication to host", ex.Message);
      }

      settings.AllowPublicKeyRetrieval = true;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        // Success when activating public key retrieval for commercial servers.
        if (serverCompiledUsingOpenSsl)
        {
          connection.Open();
          Assert.True(connection.State == ConnectionState.Open);
          connection.Close();
        }
        // Fail since AllowPublicKeyRetrieval is ignored in gpl servers.
        else
        {
          ex = Assert.Throws<MySqlException>(() => connection.Open());
          StringAssert.StartsWith("Authentication to host", ex.Message);
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void EmptyPasswordOnSslDisableCachingSha2Password()
    {
      if (Version < new Version("8.0.3")) return;

      string userName = "testCachingSha256";
      string password = "";
      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      settings.UserID = userName;
      settings.Password = password;
      CreateUser(userName, password, pluginName);

      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void CheckAllowPublicKeyRetrievalOptionIsAvailable()
    {
      string connectionString = ConnectionSettings.ConnectionString;
      connectionString += ";allowpublickeyretrieval=true";
      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        Assert.True(connection.Settings.AllowPublicKeyRetrieval);
        connection.Close();
      }
    }

    #endregion

    #region mysql_clear_password Authentication plugin
    [Test]
    [Ignore("This test require start the mysql server commercial with the configuration specified in file Resources/my.ini")]
    [Property("Category", "Security")]
    public void ConnectUsingClearTextPlugin()
    {
      //Verify plugin is loaded
      MySqlDataReader pluginReader = ExecuteReader("SELECT * FROM INFORMATION_SCHEMA.PLUGINS WHERE PLUGIN_NAME = 'authentication_ldap_simple'");
      if (!pluginReader.HasRows)
        throw new Exception("The authentication_ldap_simple plugin isn't available.");
      pluginReader.Close();

      // Test connection for VALID user in LDAP server with right password, expected result PASS
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      string userName = "test1@MYSQL.LOCAL";
      string ldapstr = "CN=test1,CN=Users,DC=mysql,DC=local";
      string pluginName = "authentication_ldap_simple";
      CreateUser(userName, ldapstr, pluginName);
      settings.UserID = userName;
      settings.Password = "Testpw1";

      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.connectionState);
        var sql = string.Format("select user,plugin from mysql.user where user like '{0}'", settings.UserID);
        MySqlCommand command = new MySqlCommand(sql, connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.AreEqualIgnoringCase("test1@MYSQL.LOCAL", reader.GetString(0));
          StringAssert.AreEqualIgnoringCase("authentication_ldap_simple", reader.GetString(1));
        }
        //test the new user can execute sql statements FR1_1	
        sql = "create table testinserts( id int, name varchar(50),age int)";
        command = new MySqlCommand(sql, connection);
        command.ExecuteNonQuery();
        sql = @"insert into testinserts values(1,""John"",30);
          insert into testinserts values(2,""Paul"",31);
          insert into testinserts values(3,""George"",34);
          insert into testinserts values(4,""Ringo"",32);";
        command = new MySqlCommand(sql, connection);
        command.ExecuteNonQuery();
        sql = "select count(*) from testinserts";
        command = new MySqlCommand(sql, connection);
        var counter = command.ExecuteScalar();
        Assert.AreEqual(4, counter);
        //check ssl
        command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.StartsWith("TLSv1", reader.GetString(1));
        }

      }

      //Testing unix protocol
      if (!Platform.IsWindows())
      {
        string unixConnectionString = $"server={UnixSocket};user={settings.UserID};password={settings.Password};protocol=unix;";
        using (MySqlConnection conn = new MySqlConnection(unixConnectionString))
        {
          conn.Open();
          Assert.AreEqual(ConnectionState.Open, conn.State);
        }

        using (MySqlConnection connection = new MySqlConnection(unixConnectionString + "sslmode=none"))
        {
          connection.Open();
          Assert.AreEqual(ConnectionState.Open, connection.State);
        }
      }

      // Test connection for VALID user in LDAP server with wrong password, expected result FAIL
      settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      userName = "test1@MYSQL.LOCAL";
      ldapstr = "CN=test1,CN=Users,DC=mysql,DC=local";
      pluginName = "authentication_ldap_simple";
      CreateUser(userName, ldapstr, pluginName);
      settings.UserID = userName;
      settings.Password = "wrongpw";
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
        StringAssert.StartsWith("Access denied for user", ex.InnerException.Message);
      }

      // Test connection for INVALID user in LDAP server, expected result FAIL
      settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      userName = "william.wallace@MYSQL.LOCAL";
      ldapstr = "CN=william.wallace,CN=Users,DC=mysql,DC=local";
      pluginName = "authentication_ldap_simple";
      CreateUser(userName, ldapstr, pluginName);
      settings.UserID = userName;
      settings.Password = "testpw1";
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
        StringAssert.StartsWith("Access denied for user", ex.InnerException.Message);
      }

      // Test connection for VALID user in LDAP server with SSLMode=none, expected result FAIL
      settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      userName = "test1@MYSQL.LOCAL";
      ldapstr = "CN=test1,CN=Users,DC=mysql,DC=local";
      pluginName = "authentication_ldap_simple";
      CreateUser(userName, ldapstr, pluginName);
      settings.UserID = userName;
      settings.Password = "Testpw1";
      settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
        StringAssert.Contains("Clear-password authentication is not supported over insecure channels", ex.Message);
      }

      // Test connection for VALID user in LDAP server with different SSLMode values, expected result pass
      string assemblyPath = TestContext.CurrentContext.TestDirectory;
      string _sslCa = assemblyPath + "\\ca.pem";
      string _sslCert = assemblyPath + "\\client-cert.pem";
      string _sslKey = assemblyPath + "\\client-key.pem";

      settings = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      userName = "test1@MYSQL.LOCAL";
      ldapstr = "CN=test1,CN=Users,DC=mysql,DC=local";
      pluginName = "authentication_ldap_simple";
      CreateUser(userName, ldapstr, pluginName);
      settings.UserID = userName;
      settings.Password = "Testpw1";
      settings.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.State);
        connection.Close();
      }

      settings.SslCa = _sslCa;
      settings.SslMode = MySqlSslMode.VerifyCA;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.State);
        connection.Close();
      }

      settings.SslCa = _sslCa;
      settings.SslCert = _sslCert;
      settings.SslKey = _sslKey;
      settings.SslMode = MySqlSslMode.VerifyFull;
      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.State);
        connection.Close();
      }

    }
    #endregion

    #region LDAP SASL Plugin
    #region SCRAM-SHA Mechanisms
    /// <summary>
    /// WL14116 - Add support for SCRAM-SHA-1 / authentication_ldap_sasl_auth_method_name='SCRAM-SHA-1' 
    /// WL14255 - Add support for SCRAM-SHA-256 / authentication_ldap_sasl_auth_method_name='SCRAM-SHA-256'
    /// This test require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini
    /// It uses preconfigured LDAP servers present in the labs.
    /// </summary>
    /// <param name="mechanism">Should be 'SCRAM-SHA-1' or 'SCRAM-SHA-256' according to server's configuration.</param>
    [TestCase("sadmin", "perola", "common", true, "SCRAM-SHA-256")]
    [TestCase("wrongUser", "perola", "common", false)]
    [TestCase("sadmin", "wrongPassword", "common", false)]
    [Ignore("This test require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini")]
    [Property("Category", "Security")]
    public void ConnectUsingMySqlSASLPluginSCRAMSHA(string userName, string password, string proxyUser, bool shouldPass, string mechanism = "")
    {
      string plugin = "authentication_ldap_sasl";

      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString)
      {
        UserID = userName,
        Password = password,
        Database = string.Empty
      };

      CreateUser(userName, password, plugin, "%");
      CreateUser(proxyUser, "", null, "%");
      ExecuteSQL($@"GRANT ALL ON *.* TO '{proxyUser}';
        GRANT PROXY on '{proxyUser}' TO '{userName}';", true);

      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        if (shouldPass)
        {
          connection.Open();
          MySqlCommand command = new MySqlCommand($"SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{userName}';", connection);
          using (MySqlDataReader reader = command.ExecuteReader())
          {
            StringAssert.AreEqualIgnoringCase(mechanism, MySqlSASLPlugin.scramMechanism.MechanismName);
            Assert.AreEqual(ScramBase.AuthState.VALIDATE, MySqlSASLPlugin.scramMechanism._state);
            Assert.True(reader.Read());
            StringAssert.AreEqualIgnoringCase(userName, reader.GetString(0));
            StringAssert.AreEqualIgnoringCase(plugin, reader.GetString(1));
          }
        }
        else
          Assert.Throws<MySqlException>(() => connection.Open());
      }
    }

    [Test]
    public void AssertScramSha1()
    {
      string expected = "c=bixhPXVzZXIs,r=fyko+d2lbbFgONRv9qkxdawL3rfcNHYJY1ZVvWVs7j,p=NdEpo1qMJaCn9xyrYplfuEKubqQ=";
      string challenge1 = "r=fyko+d2lbbFgONRv9qkxdawL3rfcNHYJY1ZVvWVs7j,s=QSXCR+Q6sek8bf92,i=4096";
      string challenge2 = "v=n1qgUn3vi9dh7nG1+Giie5qsaVQ=";
      string fixedNonce = "fyko+d2lbbFgONRv9qkxdawL";
      byte[] response;

      ScramSha1Mechanism scramSha1 = new ScramSha1Mechanism("user", "pencil", "localhost");
      scramSha1._cnonce = fixedNonce;
      Assert.AreEqual(ScramBase.AuthState.INITIAL, scramSha1._state);

      var challenge = Encoding.UTF8.GetString(scramSha1.Challenge(null));
      Assert.AreEqual("n,a=user,n=user,r=" + fixedNonce, challenge);
      Assert.AreEqual(ScramBase.AuthState.FINAL, scramSha1._state);

      response = Encoding.UTF8.GetBytes(challenge1);
      challenge = Encoding.UTF8.GetString(scramSha1.Challenge(response));
      Assert.AreEqual(expected, challenge);
      Assert.AreEqual(ScramBase.AuthState.VALIDATE, scramSha1._state);

      response = Encoding.UTF8.GetBytes(challenge2);
      Assert.IsNull(scramSha1.Challenge(response));
    }

    [Test]
    public void AssertScramSha256()
    {
      string expected = "c=bixhPXVzZXIs,r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,p=t03aUuq4eobF+sIe9aMDq7lKPDwSPmgQxsHhaE9hQnc=";
      string challenge1 = "r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096";
      string challenge2 = "v=s/GjApLe1lkg2qcPV+thFIArK07tHFCZvdc4Y+q94sg=";
      string fixedNonce = "rOprNGfwEbeRWgbNEkqO";
      byte[] response;

      ScramSha256Mechanism scramSha256 = new ScramSha256Mechanism("user", "pencil", "localhost");
      scramSha256._cnonce = fixedNonce;
      Assert.AreEqual(ScramBase.AuthState.INITIAL, scramSha256._state);

      var challenge = Encoding.UTF8.GetString(scramSha256.Challenge(null));
      Assert.AreEqual("n,a=user,n=user,r=" + fixedNonce, challenge);
      Assert.AreEqual(ScramBase.AuthState.FINAL, scramSha256._state);

      response = Encoding.UTF8.GetBytes(challenge1);
      challenge = Encoding.UTF8.GetString(scramSha256.Challenge(response));
      Assert.AreEqual(expected, challenge);
      Assert.AreEqual(ScramBase.AuthState.VALIDATE, scramSha256._state);

      response = Encoding.UTF8.GetBytes(challenge2);
      Assert.IsNull(scramSha256.Challenge(response));
    }
    #endregion

    #region GSSAPI/Kerberos Mechanism
    /// <summary>
    /// WL14210 - [Classic] Add LDAP kerberos support (GSSAPI)
    /// This test require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini
    /// It uses preconfigured LDAP servers present in the labs.
    /// For configuration of the server, theres a quick guide in Resources/KerberosConfig.txt to setup the environment.
    /// </summary>
    [TestCase("test1@MYSQL.LOCAL", "Testpw1", "authentication_ldap_sasl", true)]
    [TestCase("invalidUser@MYSQL.LOCAL", "Testpw1", "authentication_ldap_sasl", false)]
    [TestCase("test1@MYSQL.LOCAL", "wrongPassword", "authentication_ldap_sasl", false)]
    [Ignore("This test require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini")]
    [Property("Category", "Security")]
    public void ConnectUsingMySqlSASLPluginGSSAPI(string userName, string password, string pluginName, bool shouldPass)
    {
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString)
      {
        UserID = userName,
        Password = password,
        Database = string.Empty,
        SslMode = MySqlSslMode.None
      };

      using (MySqlConnection connection = new MySqlConnection(settings.ConnectionString))
      {
        if (shouldPass)
        {
          connection.Open();
          MySqlCommand command = new MySqlCommand($"SELECT user();", connection);
          using (MySqlDataReader reader = command.ExecuteReader())
          {
            StringAssert.AreEqualIgnoringCase("GSSAPI", MySqlSASLPlugin.gssapiMechanism.MechanismName);
            Assert.True(reader.Read());
            StringAssert.Contains(userName, reader.GetString(0));
          }
        }
        else
          Assert.Throws<MySqlException>(() => connection.Open());
      }
    }
    #endregion

    [Test]
    public void AssertSaslPrep()
    {
      // Valid String
      Assert.AreEqual("my,0TEXT", MySqlSASLPlugin.SaslPrep("my,0TEXT"));
      Assert.AreEqual("my,0 TEXT", MySqlSASLPlugin.SaslPrep("my,0 TEXT"));

      // Queries for matching strings MAY contain unassigned code points.
      Assert.AreEqual("\u0888my,0TEXT", MySqlSASLPlugin.SaslPrep("\u0888my,0TEXT"));
      Assert.AreEqual("my,0\u0890TEXT", MySqlSASLPlugin.SaslPrep("my,0\u0890TEXT"));
      Assert.AreEqual("my,0TEXT\u089F", MySqlSASLPlugin.SaslPrep("my,0TEXT\u089F"));

      // Mapping: non-ASCII space characters.
      Assert.AreEqual("my,0 TEXT", MySqlSASLPlugin.SaslPrep("my,0\u1680TEXT"));
      Assert.AreEqual("my,0 TEXT", MySqlSASLPlugin.SaslPrep("my,0\u200BTEXT"));
      Assert.AreEqual(" my,0 TEXT ", MySqlSASLPlugin.SaslPrep("\u00A0my,0\u2000TEXT\u3000"));

      // Mapping: the "commonly mapped to nothing" characters.
      Assert.AreEqual("my,0TEXT", MySqlSASLPlugin.SaslPrep("my,0\u00ADTEXT"));
      Assert.AreEqual("my,0TEXT", MySqlSASLPlugin.SaslPrep("my,0\uFE0ATEXT"));
      Assert.AreEqual("my,0TEXT", MySqlSASLPlugin.SaslPrep("\u00ADmy,0\u1806TE\uFE0FXT\uFEFF"));

      // KC Normalization.
      Assert.AreEqual("my,0 fi TEXT", MySqlSASLPlugin.SaslPrep("my,0 \uFB01 TEXT"));
      Assert.AreEqual("my,0 fi TEXT", MySqlSASLPlugin.SaslPrep("my,0 \uFB01 TEXT"));

      // Prohibited Output: ASCII control characters.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\u007Fmy,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\u001FTEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\u0000"));

      // Prohibited Output: non-ASCII control characters.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\uFFFCmy,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\u008DTEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uD834\uDD73TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\u2028"));

      // Prohibited Output: private use characters.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\uE000my,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uF8FFTEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uDBC0\uDC00TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\uDB80\uDC46"));

      // Prohibited Output: non-character code points.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\uDB3F\uDFFFmy,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uFDD0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uD9FF\uDFFETEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\uDBBF\uDFFF"));

      // Prohibited Output: surrogate code points.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\uD83D\uDC2Cmy,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uD83C\uDF63TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\uD83C\uDF7B"));

      // Prohibited Output: inappropriate for plain text characters.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\uFFFACmy,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uFFFDTEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\uFFFC"));

      // Prohibited Output: inappropriate for canonical representation characters.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\u2FF0my,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\u2FFBTEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\u2FF8"));

      // Prohibited Output: change display properties or deprecated characters.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\u206Fmy,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\u200ETEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\u202E"));

      // Prohibited Output: tagging characters.
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("\uDB40\uDC7Fmy,0TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0\uDB40\uDC21TEXT"));
      Assert.Throws<ArgumentException>(() => MySqlSASLPlugin.SaslPrep("my,0TEXT\uDB40\uDC01"));
    }
    #endregion
  }
}