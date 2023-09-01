// Copyright (c) 2016, 2023, Oracle and/or its affiliates.
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

using Microsoft.Win32;
using MySql.Data.Authentication.FIDO.Utility;
using MySql.Data.Common;
using MySql.Data.MySqlClient.Authentication;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Reflection;
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
      settings.SslMode = MySqlSslMode.Disabled;
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

      settings.SslMode = MySqlSslMode.Disabled;
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

      settings.SslMode = MySqlSslMode.Disabled;
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

      settings.SslMode = MySqlSslMode.Disabled;
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
      builder.SslMode = MySqlSslMode.Disabled;
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
      builder.SslMode = MySqlSslMode.Disabled;
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
        builder.SslMode = MySqlSslMode.Disabled;

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

      settings.SslMode = MySqlSslMode.Disabled;
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
      settings.SslMode = MySqlSslMode.Disabled;
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

      settings.SslMode = MySqlSslMode.Disabled;
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
      string connectionString = Settings.ConnectionString;
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
    public void ConnectUsingClearPasswordPlugin()
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
      settings.SslMode = MySqlSslMode.Disabled;
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

      ExecuteSQL($"CREATE USER '{userName}'@'%' IDENTIFIED WITH '{plugin}' BY '{password}'", true);
      ExecuteSQL($"CREATE USER '{proxyUser}'@'%' IDENTIFIED BY ''", true);
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

      ScramSha1Mechanism scramSha1 = new ScramSha1Mechanism("user", "pencil", Host);
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

      ScramSha256Mechanism scramSha256 = new ScramSha256Mechanism("user", "pencil", Host);
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
        SslMode = MySqlSslMode.Disabled
      };

      ExecuteSQL("CREATE USER 'test1@MYSQL.LOCAL' IDENTIFIED WITH authentication_ldap_sasl; GRANT ALL ON *.* to 'test1@MYSQL.LOCAL';", true);

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

    #region PureKerberos Mechanism
    /// <summary>
    /// WL14429 - [Classic] Support for authentication_kerberos_client authentication plugin
    /// WL14654 - [Classic] Integrate new SSPI kerberos library
    /// WL15341 - [Classic] Support MIT Kerberos library on Windows
    /// These tests require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini
    /// It uses preconfigured LDAP servers present in the labs.
    /// For configuration of the server, there's a quick guide in Resources/KerberosConfig.txt to setup the environment.
    /// For SSPI, Windows client should be part of Windows server domain.
    /// Please refer to the WLs to check the specifications for each case.
    /// </summary>

    [TestCase(false, "invalidUser", "", "", null)]
    [TestCase(false, "invalidUser", "fakePassword", "", null)]
    [TestCase(false, "", "", "caching_sha2_password", null)]
    [TestCase(true, "", "", "authentication_kerberos_client", KerberosAuthMode.GSSAPI)] // TRUE: if there's a TGT in cache or the login user is in KDC. 'AUTO' if logged-in user is properly configured
    [TestCase(true, "", "falsePassword", "authentication_kerberos_client", KerberosAuthMode.GSSAPI)]
    [TestCase(true, "", "Testpw1", "authentication_kerberos_client", KerberosAuthMode.GSSAPI)]
    [TestCase(true, "test1", "Testpw1", "authentication_kerberos_client", KerberosAuthMode.AUTO)]
    [TestCase(true, "test1", "Testpw1", "sha256_password", KerberosAuthMode.AUTO)]
    [TestCase(true, "test1", "Testpw1", "", KerberosAuthMode.AUTO)]
    [TestCase(true, "test1", "wrongPassword", "", KerberosAuthMode.GSSAPI)] // TRUE: if there's a TGT in cache.
    [TestCase(true, "test1", "", "", KerberosAuthMode.GSSAPI)]
    [Ignore("This test require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini")]
    [Property("Category", "Security")]
    public void ConnectUsingMySqlPluginKerberosAUTO(bool shouldPass, string userName, string password, string pluginName, KerberosAuthMode mode)
    {
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString)
      {
        UserID = userName,
        Password = password,
        DefaultAuthenticationPlugin = pluginName
      };

      TestKerberosConnection(shouldPass, userName, settings, mode);
    }

    [TestCase(false, "invalidUser", "", "", null)]
    [TestCase(false, "invalidUser", "fakePassword", "", null)]
    [TestCase(false, "", "", "caching_sha2_password", null)]
    [TestCase(false, "", "", "authentication_kerberos_client", null)] // TRUE if logged-in user is properly configured in MySQL Server
    [TestCase(false, "", "falsePassword", "authentication_kerberos_client", null)] // SSPI will use the logged-in user and the password provided
    [TestCase(false, "test1", "wrongPassword", "", null)]
    [TestCase(true, "", "Testpw1", "authentication_kerberos_client", KerberosAuthMode.SSPI)] // logged-in user should be properly configured in MySQL server
    [TestCase(true, "test1", "Testpw1", "authentication_kerberos_client", KerberosAuthMode.SSPI)]
    [TestCase(true, "test1", "Testpw1", "sha256_password", KerberosAuthMode.SSPI)]
    [TestCase(true, "test1", "Testpw1", "", KerberosAuthMode.SSPI)]
    [TestCase(true, "test1", "", "", KerberosAuthMode.SSPI)] // MySQL user should match with logged-in Windows user
    [Ignore("This test require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini")]
    [Property("Category", "Security")]
    public void ConnectUsingMySqlPluginKerberosSSPI(bool shouldPass, string userName, string password, string pluginName, KerberosAuthMode mode)
    {
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString)
      {
        UserID = userName,
        Password = password,
        DefaultAuthenticationPlugin = pluginName,
        KerberosAuthMode = KerberosAuthMode.SSPI
      };

      TestKerberosConnection(shouldPass, userName, settings, mode);
    }

    [TestCase(false, "invalidUser", "", "", null)]
    [TestCase(false, "invalidUser", "fakePassword", "", null)]
    [TestCase(false, "", "", "caching_sha2_password", null)]
    [TestCase(true, "", "", "authentication_kerberos_client", KerberosAuthMode.GSSAPI)] // TRUE: if there's a TGT in cache or the login user is in KDC.
    [TestCase(true, "", "falsePassword", "authentication_kerberos_client", KerberosAuthMode.GSSAPI)] // TRUE: if there's a TGT in cache or the login user is in KDC.
    [TestCase(true, "", "Testpw1", "authentication_kerberos_client", KerberosAuthMode.GSSAPI)] // TRUE: if there's a TGT in cache or the login user is in KDC.
    [TestCase(true, "test1", "Testpw1", "authentication_kerberos_client", KerberosAuthMode.GSSAPI)]
    [TestCase(true, "test1", "Testpw1", "sha256_password", KerberosAuthMode.GSSAPI)]
    [TestCase(true, "test1", "Testpw1", "", KerberosAuthMode.GSSAPI)]
    [TestCase(true, "test1", "wrongPassword", "", KerberosAuthMode.GSSAPI)] // TRUE: if there's a TGT in cache
    [TestCase(true, "test1", "", "", KerberosAuthMode.GSSAPI)] // TRUE: if there's a TGT in cache
    [Ignore("This test require to start MySQL Commercial Server with the configuration specified in file Resources/my.ini")]
    [Property("Category", "Security")]
    public void ConnectUsingMySqlPluginKerberosGSSAPI(bool shouldPass, string userName, string password, string pluginName, KerberosAuthMode mode)
    {
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Settings.ConnectionString)
      {
        UserID = userName,
        Password = password,
        DefaultAuthenticationPlugin = pluginName,
        KerberosAuthMode = KerberosAuthMode.GSSAPI
      };

      TestKerberosConnection(shouldPass, userName, settings, mode);
    }

    private void TestKerberosConnection(bool shouldPass, string username, MySqlConnectionStringBuilder settings, KerberosAuthMode mode)
    {
      ExecuteSQL("CREATE USER IF NOT EXISTS 'test1'@'%' IDENTIFIED WITH authentication_kerberos BY 'MYSQL.LOCAL'; " +
        "GRANT ALL ON *.* to 'test1'@'%';", true);

      using (MySqlConnection conn = new MySqlConnection(settings.ConnectionString))
      {
        if (shouldPass)
        {
          conn.Open();
          MySqlCommand command = new MySqlCommand($"SELECT user();", conn);
          using (MySqlDataReader reader = command.ExecuteReader())
          {
            Assert.True(reader.Read());
            StringAssert.Contains(username, reader.GetString(0));
          }

          Assert.True(conn.Settings.KerberosAuthMode == mode);
        }
        else
          Assert.Throws<MySqlException>(() => conn.Open());
      }
    }
    #endregion

    #region OCI IAM Authentication
    /// <summary>
    /// WL14708 - Support OCI IAM authentication
    /// WL15489 - Support OCI Ephemeral key-based authentication
    /// This test require to have a server running in the OCI and have at least one user configured with 
    /// the authentication_oci authentication plugin (see server WL11102 for further details)
    /// </summary>
    [TestCase("cnetuser1", "", Description = "By not setting a custom path, it takes the default value from the OCI SDK for .NET")]
    [TestCase("cnetuser1", "C:\\config", Description = "Uses a custom path for the config file")]
    [TestCase("", "", Description = "Uses OS logged in user")]
    [TestCase("", "C:\\config", Description = "Uses OS logged in user and custom path for the config file")]
    [Ignore("This test require a server running in the OCI.")]
    public void ConnectUsingOciIamAuthentication(string userName, string configFilePath)
    {
      string host = "100.101.74.201";
      uint port = 3307;

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = userName,
        Server = host,
        Port = port,
        OciConfigFile = configFilePath,
        OciConfigProfile = "TEST" // when using ephemeral key-based auth
      };

      using (var conn = new MySqlConnection(connStringBuilder.ConnectionString))
      {
        conn.Open();
        MySqlCommand command = new MySqlCommand($"SELECT user();", conn);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          userName = string.IsNullOrEmpty(userName) ? Environment.UserName : userName;
          StringAssert.Contains(userName, reader.GetString(0));
        }
      }
    }

    [Test]
    public void NonExistingKeyFile()
    {
      OciAuthenticationPlugin plugin = new OciAuthenticationPlugin();
      string keyFileInvalidPath = "C:\\invalid\\Path";
      string exMsg = Assert.Throws<MySqlException>(() => OciAuthenticationPlugin.SignData(new byte[0], keyFileInvalidPath)).Message;
      StringAssert.AreEqualIgnoringCase(Resources.OciKeyFileDoesNotExists, exMsg);
    }

    public struct Profiles
    {
      public Dictionary<string, Dictionary<string, string>> profiles { get; set; }
      public bool missingEntry { get; set; }
      public string ociProfile { get; set; }
    }

    [DatapointSource]
    public Profiles[] profiles = new Profiles[]
    {
      // does not contain key_file entry
      new Profiles{
        profiles = new Dictionary<string, Dictionary<string, string>>() {
          { "DEFAULT", new Dictionary<string, string>(){ { "fingerprint", "11:22:33:44:55:66:77" } } }
        },
        missingEntry = true,
        ociProfile = "DEFAULT"
      },
            // does not contain fingerprint entry
      new Profiles{
        profiles = new Dictionary<string, Dictionary<string, string>>() {
          { "TEST", new Dictionary<string, string>(){ { "key_file", "keyFilePath" } } }
        },
        missingEntry = true,
        ociProfile = "TEST"
      },
      // points to a invalid private key file
      new Profiles{
        profiles = new Dictionary<string, Dictionary<string, string>>() {
          { "DEFAULT", new Dictionary<string, string>(){ { "key_file", System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory.Substring(0, TestContext.CurrentContext.TestDirectory.LastIndexOf("bin")), "Resources", "my.ini") }, { "fingerprint", "11:22:33:44:55:66:77" } } }
        },
        ociProfile ="DEFAULT"
      }
    };

    [Theory]
    public void ValidatesEntries(Profiles profiles)
    {
      OciAuthenticationPlugin plugin = new OciAuthenticationPlugin();
      plugin._ociConfigProfile = profiles.ociProfile;
      string exMsg;

      if (profiles.missingEntry)
      {
        exMsg = Assert.Throws<MySqlException>(() => plugin.GetOciConfigValues(profiles.profiles, out string keyFile, out string fingerprint, out string securityTokenFilePath)).Message;
        StringAssert.AreEqualIgnoringCase(Resources.OciEntryNotFound, exMsg);
      }
      else
      {
        plugin.GetOciConfigValues(profiles.profiles, out string keyFile, out string fingerprint, out string securityTokenFilePath);
        exMsg = Assert.Throws<MySqlException>(() => OciAuthenticationPlugin.SignData(new byte[0], keyFile)).Message;
        StringAssert.AreEqualIgnoringCase(Resources.OciInvalidKeyFile, exMsg);
      }
    }

    [Test]
    public void OtherThanDefaultProfile()
    {
      Dictionary<string, Dictionary<string, string>> profiles = new();
      Dictionary<string, string> valuesDefault = new() { { "fingerprint", "11:22:33:44:55:66" } };
      Dictionary<string, string> valuesTest = new() { { "key_file", "keyFilePath" }, { "fingerprint", "66:55:44:33:22:11" }, { "security_token_file", "securityTokenFilePath" } };
      profiles.Add("DEFAULT", valuesDefault);
      profiles.Add("TEST", valuesTest);

      OciAuthenticationPlugin plugin = new OciAuthenticationPlugin();
      plugin._ociConfigProfile = "TEST";
      plugin.GetOciConfigValues(profiles, out string keyFilePath, out string fingerprint, out string securityTokenFilePath);

      StringAssert.AreEqualIgnoringCase("keyFilePath", keyFilePath);
      StringAssert.AreEqualIgnoringCase("66:55:44:33:22:11", fingerprint);
      StringAssert.AreEqualIgnoringCase("securityTokenFilePath", securityTokenFilePath);
    }

    [DatapointSource]
    public string[] invalidPaths = new string[]
    {
      "\\invalid\\Path//Bad",
      System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "config")
    };

    [Theory]
    [Ignore("This test requires the OCI SDK for .NET")]
    public void NonExistingConfigFile(string invalidPath)
    {
      string userName = "cnetuser1";
      string host = "100.101.74.201";
      uint port = 3307;
      string configFilePath = invalidPath;

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = userName,
        Server = host,
        Port = port,
        OciConfigFile = configFilePath
      };

      using (var conn = new MySqlConnection(connStringBuilder.ConnectionString))
      {
        string exMsg = Assert.Throws<MySqlException>(() => conn.Open()).Message;
        StringAssert.AreEqualIgnoringCase(Resources.OciConfigFileNotFound, exMsg);
      }
    }

    [Test]
    public void OciSdkNotInstalled()
    {
      OciAuthenticationPlugin plugin = new OciAuthenticationPlugin();

      string exMsg = Assert.Throws<MySqlException>(() => plugin.AuthenticateAsync(false, false).GetAwaiter().GetResult()).Message;
      StringAssert.AreEqualIgnoringCase(Resources.OciSDKNotFound, exMsg);
    }

    [Test]
    [Ignore("This test requires the OCI SDK for .NET")]
    public void NonExistingConfigProfile()
    {
      string userName = "cnetuser1";
      string host = "100.101.74.201";
      uint port = 3307;
      string configProfile = "NonExistentProfile";

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = userName,
        Server = host,
        Port = port,
        OciConfigProfile = configProfile
      };

      using (var conn = new MySqlConnection(connStringBuilder.ConnectionString))
      {
        string exMsg = Assert.Throws<MySqlException>(() => conn.Open()).Message;
        StringAssert.AreEqualIgnoringCase(Resources.OciConfigProfileNotFound, exMsg);
      }
    }

    [Test]
    [Ignore("This test requires to have a 'security_token_file' larger than 10KB")]
    public void SecurityTokenLargerThan10KB()
    {
      string securityTokenPath = "C:\\largePayload";
      Assert.Throws<MySqlException>(() => OciAuthenticationPlugin.LoadSecurityToken(securityTokenPath));
    }
    #endregion

    #region Multi Factor Authentication (MFA)
    /// <summary>
    /// WL14653 - Support for MFA (multi factor authentication) authentication
    /// </summary>
    [Test]
    [Ignore("This test requires the plugin module 'auth_test_plugin' loaded. See WL14653 LLD.")]
    [Property("Category", "Security")]
    public void ConnectUsing1FAuth()
    {
      ExecuteSQL("CREATE USER IF NOT EXISTS user_1f IDENTIFIED WITH cleartext_plugin_server BY 'password1'", true);

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_1f",
        Password = "password1",
        Password2 = "otherPassword",
        Password3 = "thirdPassword",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.State);
    }

    [TestCase("user_2f", "password1", "password2", true)]
    [TestCase("sadmin", "perola1", "perola", true)] // using authentication_ldap_sasl as 2f auth (requires LDAP setup)
    [TestCase("user_2f", "wrong", "password2", false)]
    [TestCase("user_2f", "password1", "wrong", false)]
    [TestCase("user_2f", "password1", "", false)]
    [Ignore("This test requires the plugin module 'auth_test_plugin' loaded. See WL14653 LLD.")]
    [Property("Category", "Security")]
    public void ConnectUsing2FAuth(string user, string pwd, string pwd2, bool shouldPass)
    {
      // Requires LDAP setup
      //ExecuteSQL("CREATE USER sadmin IDENTIFIED BY 'perola1' AND IDENTIFIED WITH authentication_ldap_sasl;");
      ExecuteSQL($"CREATE USER IF NOT EXISTS {user} IDENTIFIED WITH cleartext_plugin_server BY 'password1'" +
        "AND IDENTIFIED WITH cleartext_plugin_server BY 'password2'", true);

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = user,
        Password = pwd,
        Password2 = pwd2,
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);

      if (!shouldPass)
        Assert.Throws<MySqlException>(() => conn.Open());
      else
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
      }
    }

    [TestCase("user_3f", "password1", "password2", "password3", true)]
    [TestCase("user_3f", "wrong", "password2", "password3", false)]
    [TestCase("user_3f", "password1", "wrong", "password3", false)]
    [TestCase("user_3f", "password1", "password2", "wrong", false)]
    [TestCase("user_3f", "password1", "", "password3", false)]
    [Ignore("This test requires the plugin module 'auth_test_plugin' loaded. See WL14653 LLD.")]
    [Property("Category", "Security")]
    public void ConnectUsing3FAuth(string user, string pwd, string pwd2, string pwd3, bool shouldPass)
    {
      ExecuteSQL("CREATE USER IF NOT EXISTS user_3f IDENTIFIED WITH cleartext_plugin_server BY 'password1'" +
        "AND IDENTIFIED WITH cleartext_plugin_server BY 'password2'" +
        "AND IDENTIFIED WITH cleartext_plugin_server BY 'password3'", true);

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = user,
        Password = pwd,
        Password2 = pwd2,
        Password3 = pwd3,
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);

      if (!shouldPass)
        Assert.Throws<MySqlException>(() => conn.Open());
      else
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
      }
    }
    #endregion

    #region FIDO Authentication
    /// <summary>
    /// WL14871 - Support FIDO authentication [classic]
    /// </summary>
    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library")]
    [Property("Category", "Security")]
    public void FidoAuthentication1F()
    {
      // Install FIDO plugin
      ExecuteSQL("INSTALL PLUGIN authentication_fido SONAME 'authentication_fido.so';", true);
      // Create user
      // The INITIAL AUTHENTICATION IDENTIFIED clause must be specified to set a random or a static password.
      ExecuteSQL("CREATE USER 'user_f1'@'localhost' IDENTIFIED WITH authentication_fido INITIAL AUTHENTICATION IDENTIFIED BY 'bar';", true);

      // Register the authenticator
      // $ mysql --user=user_f1 --fido-register-factor=1

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f1",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.FidoActionRequested += Conn_FidoActionRequested;
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.State);
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library")]
    [Property("Category", "Security")]
    public void FidoAuthentication2F()
    {
      // Install FIDO plugin
      ExecuteSQL("INSTALL PLUGIN authentication_fido SONAME 'authentication_fido.so';", true);
      // Create user
      ExecuteSQL("CREATE USER 'user_f2'@'localhost' IDENTIFIED BY 'bar' AND IDENTIFIED WITH authentication_fido;", true);

      // Register the authenticator
      // $ mysql --user=user_f2 --fido-register-factor=2

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f2",
        Password = "bar",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.FidoActionRequested += Conn_FidoActionRequested;
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.State);
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library")]
    [Property("Category", "Security")]
    public void FidoAuthentication3F()
    {
      // Install FIDO plugin
      ExecuteSQL("INSTALL PLUGIN authentication_fido SONAME 'authentication_fido.so';", true);
      // Create user
      ExecuteSQL("CREATE USER 'user_f3'@'localhost' IDENTIFIED BY 'bar' AND IDENTIFIED BY 'baz' AND IDENTIFIED WITH authentication_fido;", true);

      // Register the authenticator
      // $ mysql --user=user_f3 --fido-register-factor=3

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f3",
        Password = "bar",
        Password2 = "baz",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.FidoActionRequested += Conn_FidoActionRequested;
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.State);
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library")]
    [Property("Category", "Security")]
    public void FidoAuthenticationNoUserGestureException()
    {
      // Install FIDO plugin
      ExecuteSQL("INSTALL PLUGIN authentication_fido SONAME 'authentication_fido.so';", true);
      // Create user
      ExecuteSQL("CREATE USER 'user_f2'@'localhost' IDENTIFIED BY 'bar' AND IDENTIFIED WITH authentication_fido;", true);

      // Register the authenticator
      // $ mysql --user=user_f2 --fido-register-factor=2

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f2",
        Password = "bar",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.FidoActionRequested += Conn_FidoActionRequested;
      Assert.Throws<CtapException>(() => conn.Open());
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library")]
    [Property("Category", "Security")]
    public void FidoAuthenticationUnregisteredUserException()
    {
      // Install FIDO plugin
      ExecuteSQL("INSTALL PLUGIN authentication_fido SONAME 'authentication_fido.so';", true);
      // Create user
      ExecuteSQL("CREATE USER 'user_f2'@'localhost' IDENTIFIED BY 'bar' AND IDENTIFIED WITH authentication_fido;", true);

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f2",
        Password = "bar",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.FidoActionRequested += Conn_FidoActionRequested;
      Assert.Throws<MySqlException>(() => conn.Open());
    }

    private static void Conn_FidoActionRequested()
    {
      Console.WriteLine("Please insert FIDO device and perform gesture action for authentication to complete.");
    }
    #endregion

    #region WebAuthn Authentication
    /// <summary>
    /// WL15193 - Support WebauthN in fido authentication plugin [Classic]
    /// </summary>
    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library and user interaction.")]
    [Property("Category", "Security")]
    public void WebAuthnAuthenticationPasswordless()
    {
      // Install WebAuthn plugin
      ExecuteSQL("INSTALL PLUGIN authentication_webauthn SONAME 'authentication_webauthn.so';", true);
      // Create user
      // The INITIAL AUTHENTICATION IDENTIFIED clause must be specified to set a random or a static password.
      ExecuteSQL("CREATE USER 'user_f1'@'localhost' IDENTIFIED WITH authentication_webauthn INITIAL AUTHENTICATION IDENTIFIED BY 'bar';", true);
      // Register the authenticator
      // $ mysql --user=user_f1 --password=bar --register-factor=2

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f1",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.WebAuthnActionRequested += Conn_WebAuthnActionRequested;
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.State);
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library and user interaction.")]
    [Property("Category", "Security")]
    public void WebAuthnAuthentication2F()
    {
      //Install WebAuthn plugin
      ExecuteSQL("INSTALL PLUGIN authentication_webauthn SONAME 'authentication_webauthn.so';", true);
      //Create user
      ExecuteSQL("CREATE USER 'user_f2' IDENTIFIED BY 'bar' AND IDENTIFIED WITH authentication_webauthn;", true);
      //Register the authenticator
      //$ mysql --user=user_f2 --password=bar --register-factor=2

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f2",
        Server = Settings.Server,
        Port = Settings.Port,
        Password = "bar"
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.WebAuthnActionRequested += Conn_WebAuthnActionRequested;
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.State);
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library and user interaction.")]
    [Property("Category", "Security")]
    public void WebAuthnAuthentication3F()
    {
      // Install WebAuthn plugin
      ExecuteSQL("INSTALL PLUGIN authentication_webauthn SONAME 'authentication_webauthn.so';", true);
      // Create user
      ExecuteSQL("CREATE USER 'user_f3' IDENTIFIED BY 'bar' AND IDENTIFIED WITH cleartext_plugin_server BY 'baz' AND IDENTIFIED WITH authentication_webauthn;", true);
      // Register the authenticator
      // $  mysql --user=user_f3 --password=bar --password2=baz --register-factor=3

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "user_f3",
        Server = Settings.Server,
        Port = Settings.Port,
        Password = "bar",
        Password2 = "baz"
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.WebAuthnActionRequested += Conn_WebAuthnActionRequested;
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.State);
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library and user interaction.")]
    [Property("Category", "Security")]
    public void WebAuthnAuthenticationUnregisteredUserException()
    {
      // Install WebAuthn plugin
      ExecuteSQL("INSTALL PLUGIN authentication_webauthn SONAME 'authentication_webauthn.so';", true);
      // Create user
      // The INITIAL AUTHENTICATION IDENTIFIED clause must be specified to set a random or a static password.
      ExecuteSQL("CREATE USER 'foo'@'localhost' IDENTIFIED WITH authentication_webauthn INITIAL AUTHENTICATION IDENTIFIED BY 'bar';", true);

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "foo",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.WebAuthnActionRequested += Conn_WebAuthnActionRequested;
      Assert.Throws<MySqlException>(() => conn.Open());
    }

    [Test]
    [Ignore("This should be executed manually since it depends on libfido2 library and user interaction.")]
    [Property("Category", "Security")]
    public void WebAuthnAuthenticationNoUserGestureException()
    {
      // Install WebAuthn plugin
      ExecuteSQL("INSTALL PLUGIN authentication_webauthn SONAME 'authentication_webauthn.so';", true);
      // Create user
      // The INITIAL AUTHENTICATION IDENTIFIED clause must be specified to set a random or a static password.
      ExecuteSQL("CREATE USER 'foo'@'localhost' IDENTIFIED WITH authentication_webauthn INITIAL AUTHENTICATION IDENTIFIED BY 'bar';", true);
      // Register the authenticator
      // $ mysql --user=foo --password=bar --register-factor=2

      var connStringBuilder = new MySqlConnectionStringBuilder()
      {
        UserID = "foo",
        Server = Settings.Server,
        Port = Settings.Port
      };

      using var conn = new MySqlConnection(connStringBuilder.ConnectionString);
      conn.WebAuthnActionRequested += Conn_WebAuthnActionRequested;
      Assert.Throws<MySqlException>(() => conn.Open());
    }

    private static void Conn_WebAuthnActionRequested()
    {
      Console.WriteLine("Please insert FIDO device and perform gesture action for authentication to complete.");
    }
    #endregion

    [Test, Description("Test User Authentication Fails with classic protocol")]
    public void AuthPlainAndMySql41()
    {
      if (Version <= new Version("5.7")) return;
      MySqlConnection connection = null;
      var connectionString = $"server={Settings.Server};user={Settings.UserID};port={Port};password={Settings.Password};auth=PLAIN";
      Assert.Throws<ArgumentException>(() => connection = new MySqlConnection(connectionString));

      connectionString = $"server={Settings.Server};user={Settings.UserID};port={Port};password={Settings.Password};auth=MySQL41";
      Assert.Throws<ArgumentException>(() => connection = new MySqlConnection(connectionString));
    }

    [Test, Description("Test caching_sha2_password feature in the client(auth plugin=sha2_password and native password) in the server(>=8.0.4) " +
                  "with secure connections(classic connection).Server started with mysql native password plugin")]
    public void Sha256AndNativeWithCertificates()
    {
      if (Version <= new Version("8.0.4")) Assert.Ignore("This test is for MySql 8.0.4 or higher");

      using (var rdr = ExecuteReader("SELECT * FROM INFORMATION_SCHEMA.PLUGINS WHERE PLUGIN_NAME = 'caching_sha2_password'"))
      {
        if (!rdr.HasRows) Assert.Ignore("This test needs plugin caching_sha2_password");
      }

      // Test connection for VALID user in LDAP server with different SSLMode values, expected result pass
      string assemblyPath = Assembly.GetExecutingAssembly().Location.Replace(String.Format("{0}.dll",
              Assembly.GetExecutingAssembly().GetName().Name), string.Empty);

      string _sslCa = _sslCa = assemblyPath + "ca.pem";
      string _sslWrongCert = assemblyPath + "client-incorrect.pfx";

      FileAssert.Exists(_sslCa);
      FileAssert.Exists(_sslWrongCert);

      string pluginName = "sha256_password";
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
      builder.Server = Host;
      builder.Port = Convert.ToUInt32(Port);
      builder.UserID = "testCachingSha2";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);

      // Authentication using RSA keys. Only available in servers compiled with OpenSSL (E.g. Commercial).
      bool serverCompiledUsingOpenSsl = false;
      builder.Password = "test";
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Rsa_public_key';", connection);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          if (reader.HasRows)
          {
            reader.Read();
            if (!string.IsNullOrEmpty(reader.GetString(1))) serverCompiledUsingOpenSsl = true;
          }
        }
      }

      // Authentication success with full authentication - TLS connection.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      // Authentication fails with full authentication - TLS connection.SSL Mode default disabled
      builder.SslMode = MySqlSslMode.Disabled;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        Assert.Throws<MySqlException>(() => connection.Open());
      }

      string connStr = null;
      connStr = $"server={Host};port={Port};user={builder.UserID};password={builder.Password};";
      connStr += $";SSL Mode=Required;CertificateFile={_sslCa};CertificatePassword=pass;";
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        connection.Open();
        connection.Close();
      }

      connStr = string.Empty;
      connStr = $"server={Host};port={Port};user={builder.UserID};password={builder.Password};";
      connStr += $";SSL Mode=VerifyCA;CertificateFile={_sslCa};CertificatePassword=pass;";
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        connection.Open();
        connection.Close();
      }

      connStr = string.Empty;
      connStr = $"server={Host};port={Port};user={builder.UserID};password={builder.Password};";
      connStr += $";SSL Mode=Required;CertificateFile={_sslCa};CertificatePassword=wrongpass;";
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        connection.Open();
        connection.Close();
      }

      connStr = string.Empty;
      connStr = $"server={Host};port={Port};user={builder.UserID};password={builder.Password};";
      connStr += $";SSL Mode=VerifyCA;CertificateFile={_sslCa};CertificatePassword=wrongpass;";
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        connection.Open();
        connection.Close();
      }

      connStr = string.Empty;
      connStr = $"server={Host};port={Port};user={builder.UserID};password={builder.Password};";
      connStr += $";SSL Mode=Required;CertificateFile={_sslWrongCert};CertificatePassword=pass;";
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        Assert.Catch(() => connection.Open());
      }

      // Flush privileges clears the cache.
      ExecuteSQL("flush privileges");
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString + ";pooling=false"))
      {
        Assert.Throws<MySqlException>(() => connection.Open());
      }

      if (serverCompiledUsingOpenSsl)
      {
        builder.SslMode = MySqlSslMode.Disabled;
        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString + ";AllowPublicKeyRetrieval=false;pooling=false"))
        {
          Assert.Throws<MySqlException>(() => connection.Open());
        }

        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString + ";AllowPublicKeyRetrieval=true;pooling=false"))
        {
          connection.Open();
        }
      }

      // Authentication - TLS connection.
      builder.UserID = "testCachingSha2";
      builder.Password = "test";
      builder.SslMode = MySqlSslMode.Preferred;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.UserID = "testCachingSha2";
      builder.Password = "test";
      builder.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      // Flush privileges clears the cache.
      ExecuteSQL("flush privileges");
      builder.Password = "incorrectPassword";
      Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());

      // Authentication success with empty password – Any connection.
      builder = new MySqlConnectionStringBuilder();
      builder.Server = Host;
      builder.Port = Convert.ToUInt32(Port);
      builder.UserID = "testCachingSha2NoPassword";
      builder.Password = "";
      CreateUser(builder.UserID, builder.Password, pluginName);

      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.SslMode = MySqlSslMode.Disabled;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.UserID = "testCachingSha2";
      builder.SslMode = MySqlSslMode.Required;
      Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());

      builder.SslMode = MySqlSslMode.Disabled;
      Assert.Throws<MySqlException>(() => new MySqlConnection(builder.ConnectionString).Open());

      pluginName = "mysql_native_password";
      builder = new MySqlConnectionStringBuilder(RootSettings.ConnectionString);
      builder.UserID = "testNative";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);

      // TLS enabled.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }


      builder.SslMode = MySqlSslMode.Disabled;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      ExecuteSQL("flush privileges");
      builder.SslMode = MySqlSslMode.Disabled;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      connStr = string.Empty;
      connStr = $"server={Host};port={Port};user={builder.UserID};password={builder.Password};";
      connStr += $";SSL Mode=Required;CertificateFile={_sslCa};CertificatePassword=pass;";
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        connection.Open();
        connection.Close();
      }

      pluginName = "mysql_native_password";
      builder = new MySqlConnectionStringBuilder();
      builder.Server = Host;
      builder.Port = Convert.ToUInt32(Port);
      builder.UserID = "testNativeBlankPassword";
      builder.Password = "";
      CreateUser(builder.UserID, builder.Password, pluginName);

      // TLS enabled.
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      // TLS not enabled.
      builder.SslMode = MySqlSslMode.Disabled;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.SslMode = MySqlSslMode.Required;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      ExecuteSQL("flush privileges");
      builder.SslMode = MySqlSslMode.Disabled;
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      connStr = string.Empty;
      connStr = $"server={Host};port={Port};user={builder.UserID};password={builder.Password};";
      connStr += $";SSL Mode=Required;CertificateFile={_sslCa};CertificatePassword=pass;";
      using (MySqlConnection connection = new MySqlConnection(connStr))
      {
        connection.Open();
        connection.Close();
      }
    }
  }
}