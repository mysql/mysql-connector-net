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

using MySql.Data.MySqlClient.Authentication;
using System;
using System.Data;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class AuthTests : TestBase
  {
    public AuthTests(TestFixture fixture) : base(fixture)
    {
    }

    #region Windows Authentication Plugin

    [Fact]
    [Trait("Category", "Security")]
    public void TestIntegratedSecurityNoPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, false);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void TestIntegratedSecurityPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, true);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void TestIntegratedSecurityNoPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", false);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void TestIntegratedSecurityPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", true);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void TestWinAuthWithoutProxyNoUserNoPooling()
    {
      TestIntegratedSecurityWithoutProxy(null, false);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void TestWinAuthWithoutProxyNoUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void TestWinAuthWithoutProxyAndUser()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", false);
    }

    [Fact]
    [Trait("Category", "Security")]
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
        executeSQL(
          "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
           userMapping + "'", true);
      }
      else
      {
        // extend mapping string for current user
        executeSQL(
          "UPDATE mysql.user SET authentication_string='" + userMapping +
          "," + authenticationString + "' where user='" + UserName + "'", true);
      }
        executeSQL(string.Format("grant all privileges on *.* to '{0}'@'%'", UserName), true);


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
          Assert.Equal(1, ret);

          command.CommandText = "select user()";
          string myUser = (string)command.ExecuteScalar();
          // Check if proxy user is correct
          Assert.StartsWith(UserName + "@", myUser);

            // check if mysql user is correct
          // (foo_user is mapped to current  OS user)
          command.CommandText = "select current_user()";
          string currentUser = (string)command.ExecuteScalar();
            Assert.StartsWith(UserName, currentUser);
        }
      }

      if (pooling)
      {
        executeSQL("KILL " + threadId, true);
      }
    }
      finally
    {
        // Cleanup

        // Drop test user
        executeSQL(string.Format("drop user {0}", UserName), true);
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
        executeSQL($"DROP USER IF EXISTS {UserName}");
          executeSQL(
            "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
             userMapping + "'", true);
        }
        else
        {
          // extend mapping string for current user
          executeSQL(
            "UPDATE mysql.user SET authentication_string='" + userMapping +
            "," + authenticationString + "' where user='" + UserName + "'", true);
        }
      executeSQL($"DROP USER IF EXISTS foo_user");
      executeSQL("create user foo_user identified by 'pass'", true);
      executeSQL("grant all privileges on *.* to 'foo_user'@'%'", true);
      executeSQL("grant proxy on foo_user to " + UserName, true);


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
            Assert.Equal(1, ret);

            command.CommandText = "select user()";
            string myUser = (string)command.ExecuteScalar();
            // Check if proxy user is correct
            Assert.StartsWith(UserName + "@", myUser);

          // check if mysql user is correct
            // (foo_user is mapped to current  OS user)
            command.CommandText = "select current_user()";
            string currentUser = (string)command.ExecuteScalar();
          Assert.StartsWith("foo_user@", currentUser);
          }
        }

        if (pooling)
        {
          executeSQL("KILL " + threadId, true);
        }
      }

    #endregion

    #region MySql Native Password Authentication Plugin

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingMySqlNativePasswordPlugin()
      {
      string userName = "testNtvPass";
      string password = "mysql";
      string pluginName = "mysql_native_password";
      MySqlConnectionStringBuilder Settings = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      Settings.UserID = userName;
      Settings.Password = password;
      Settings.Database = "";
      Fixture.CreateUser(userName, password, pluginName);

      // User with password over TLS connection.
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.StartsWith("TLSv1", reader.GetString(1));
      }

        command.CommandText = String.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(userName, reader.GetString(0));
          Assert.Equal(pluginName, reader.GetString(1));
    }

        connection.Close();
      }

      // User with password over non-TLS connection.
      Settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    #endregion

    #region Sha256 Password Authentication Plugin

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingSha256PasswordPlugin()
    {
      if (Fixture.Version <= new Version("5.6")) return;

      string userName = "testSha256";
      string password = "mysql";
      string pluginName = "sha256_password";
      MySqlConnectionStringBuilder Settings = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      Settings.UserID = userName;
      Settings.Password = password;
      Settings.Database = "";
      Fixture.CreateUser(userName, password, pluginName);

      // User with password over TLS connection.
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.StartsWith("TLSv1", reader.GetString(1));
    }

        command.CommandText = String.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(userName, reader.GetString(0));
          Assert.Equal(pluginName, reader.GetString(1));
        }

        connection.Close();
      }

      // Connect over non-TLS connection using RSA keys. Only available in servers compiled with OpenSSL (E.g. Commercial)
      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
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

      Settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        if (serverCompiledUsingOpenSsl)
        {
          Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
          Assert.Equal("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        }
        else Assert.Throws<MySqlException>(() => connection.Open());
      }

      if (serverCompiledUsingOpenSsl)
      {
        Settings.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
        {
          connection.Open();
          connection.Close();
        }
        Settings.AllowPublicKeyRetrieval = false;
      }

      // User without password over TLS connection.
      password = "";
      Settings.Password = password;
      Fixture.CreateUser(userName, password, pluginName);
      Settings.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.StartsWith("TLSv1", reader.GetString(1));
        }

        command.CommandText = String.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(userName, reader.GetString(0));
          Assert.Equal(pluginName, reader.GetString(1));
        }

        connection.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void AllowPublicKeyRetrievalForSha256PasswordPlugin()
    {
      if (Fixture.Version <= new Version("5.6")) return;

      string userName = "testSha256";
      string password = "mysql";
      string pluginName = "sha256_password";
      MySqlConnectionStringBuilder Settings = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      Settings.UserID = userName;
      Settings.Password = password;
      Settings.Database = "";
      Fixture.CreateUser(userName, password, pluginName);

      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
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

      Settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => connection.Open()); ;
        if (serverCompiledUsingOpenSsl)
          Assert.Equal("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        else
          Assert.StartsWith("Authentication to host", ex.Message);
      }

      if (serverCompiledUsingOpenSsl)
      {
        Settings.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
        {
          connection.Open();
          connection.Close();
        }
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void EmptyPasswordOnSslDisabledSha256Password()
    {
      if (Fixture.Version <= new Version("5.6")) return;

      string userName = "testSha256";
      string password = "";
      string pluginName = "sha256_password";
      MySqlConnectionStringBuilder Settings = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      Settings.UserID = userName;
      Settings.Password = password;
      Fixture.CreateUser(userName, password, pluginName);

      Settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        connection.Close();
    }
    }

    #endregion

    #region Caching Sha2 Password Authentication Plugin

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingCachingSha2Plugin()
    {
      if (Fixture.Version < new Version(8, 0, 3)) return;

      MySqlDataReader pluginReader = ExecuteReader("SELECT * FROM INFORMATION_SCHEMA.PLUGINS WHERE PLUGIN_NAME = 'caching_sha2_password'");
      if (!pluginReader.HasRows)
        throw new Exception("The caching_sha2_password plugin isn't available.");
      pluginReader.Close();

      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      builder.UserID = "testCachingSha2";
      builder.Password = "test";
      builder.Database = "";
      Fixture.CreateUser(builder.UserID, builder.Password, pluginName);

      // Authentication success with full authentication - TLS connection.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.Equal(ConnectionState.Open, connection.connectionState);
        Assert.Equal(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
    }

      // Authentication success with fast authentication - Any connection.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.Equal(ConnectionState.Open, connection.connectionState);
        Assert.Equal(AuthStage.FAST_AUTH, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
  }

      // Flush privileges clears the cache.
      executeSQL("flush privileges");
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.Equal(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // Authentication failure - TLS connection.
      builder.Password = "incorrectPassword";
      Exception ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
      Assert.StartsWith("Access denied for user", ex.InnerException.Message);

      // Authentication success with empty password – Any connection.
      builder.UserID = "testCachingSha2NoPassword";
      builder.Password = "";
      Fixture.CreateUser(builder.UserID, builder.Password, pluginName);

      // TLS enabled.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.Equal(ConnectionState.Open, connection.connectionState);
        Assert.Equal(AuthStage.GENERATE_SCRAMBLE, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // TLS not enabled.
      builder.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        Assert.Equal(ConnectionState.Open, connection.connectionState);
        Assert.Equal(AuthStage.GENERATE_SCRAMBLE, CachingSha2AuthenticationPlugin._authStage);
        connection.Close();
      }

      // Authentication failure with empty password – Any connection.
      // TLS enabled.
      builder.UserID = "testCachingSha2";
      builder.SslMode = MySqlSslMode.Required;
      ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
      Assert.StartsWith("Access denied for user", ex.InnerException.Message);

      // TLS not enabled.
      builder.SslMode = MySqlSslMode.None;
      ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
      Assert.StartsWith("Access denied for user", ex.InnerException.Message);

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
          Assert.Equal("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        }

        builder.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          connection.Open();
          Assert.Equal(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
          connection.Close();
        }

        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          connection.Open();
          Assert.Equal(AuthStage.FAST_AUTH, CachingSha2AuthenticationPlugin._authStage);
          connection.Close();
        }

        // Flush privileges clears the cache.
        executeSQL("flush privileges");
        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          connection.Open();
          Assert.Equal(AuthStage.FULL_AUTH, CachingSha2AuthenticationPlugin._authStage);
          connection.Close();
        }

        builder.Password = "incorrectPassword";
        ex = Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());
        Assert.StartsWith("Access denied for user", ex.InnerException.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void AllowPublicKeyRetrievalForCachingSha2PasswordPlugin()
    {
      if (Fixture.Version < new Version("8.0.3")) return;

      string userName = "testCachingSha2";
      string password = "mysql";
      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder Settings = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      Settings.UserID = userName;
      Settings.Password = password;
      Settings.Database = "";
      Fixture.CreateUser(userName, password, pluginName);

      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
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

      Settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
        if (serverCompiledUsingOpenSsl)
          Assert.Equal("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        else
          Assert.StartsWith("Authentication to host", ex.Message);
      }

      if (serverCompiledUsingOpenSsl)
      {
        Settings.AllowPublicKeyRetrieval = true;
        using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
        {
          connection.Open();
          connection.Close();
        }
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void CachingSha2AuthFailsAfterFlushPrivileges()
    {
      if (Fixture.Version < new Version("8.0.3")) return;

      string userName = "testCachingSha2";
      string password = "mysql";
      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder Settings = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      Settings.UserID = userName;
      Settings.Password = password;
      Settings.Database = "";
      Fixture.CreateUser(userName, password, pluginName);

      bool serverCompiledUsingOpenSsl = false;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
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

      Settings.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        Assert.True(connection.State == ConnectionState.Open);
        connection.Close();
      }

      // Success since the user exists in the cache.
      Settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        Assert.True(connection.State == ConnectionState.Open);
        connection.Close();
      }

      executeSQL("flush privileges");

      // Fail since the user no longer exists in the cache and public key retrieval is disabled by default.
      Exception ex = null;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        ex = Assert.Throws<MySqlException>(() => connection.Open());
        if (serverCompiledUsingOpenSsl)
          Assert.Equal("Retrieval of the RSA public key is not enabled for insecure connections.", ex.Message);
        else
          Assert.StartsWith("Authentication to host", ex.Message);
      }

      Settings.AllowPublicKeyRetrieval = true;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
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
          Assert.StartsWith("Authentication to host", ex.Message);
        }
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void EmptyPasswordOnSslDisableCachingSha2Password()
    {
      if (Fixture.Version < new Version("8.0.3")) return;

      string userName = "testCachingSha256";
      string password = "";
      string pluginName = "caching_sha2_password";
      MySqlConnectionStringBuilder Settings = new MySqlConnectionStringBuilder(Fixture.Settings.ConnectionString);
      Settings.UserID = userName;
      Settings.Password = password;
      Fixture.CreateUser(userName, password, pluginName);

      Settings.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Fact]
    [Trait("Category", "Security")]
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
  }
}
