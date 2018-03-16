// Copyright © 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System.Data;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Web.Security;
using Xunit;
using MySql.Web.Common;
using MySql.Web.Security;
using MySql.Data.MySqlClient;

namespace MySql.Web.Tests
{
  public class SchemaTests : WebTestBase
  {
    protected override void InitSchema()
    {
      // we override this and leave it empty because we don't want
      // to init the schema for this test.
    }

    /// <summary>
    /// Bug #37469 autogenerateschema optimizing
    /// </summary>
    [Fact]
    public void SchemaCheck()
    {
      for (int i = 0; i <= SchemaManager.Version; i++)
      {
        MySQLMembershipProvider provider = new MySQLMembershipProvider();
        NameValueCollection config = new NameValueCollection();
        config.Add("connectionStringName", "LocalMySqlServer");
        config.Add("applicationName", "/");
        config.Add("passwordFormat", "Clear");

        if (i > 0)
          for (int x = 1; x <= i; x++)
            LoadSchema(x);

        try
        {
          provider.Initialize(null, config);
          Assert.False(i < SchemaManager.Version, "This should have failed");
        }
        catch (ProviderException)
        {
          Assert.False(i == SchemaManager.Version, "This should not have failed");
        }
      }
    }

    /// <summary>
    /// Bug #36444 'autogenerateschema' produces tables with 'random' collations 
    /// </summary>
    [Fact(Skip ="Fix this")]
    public void CurrentSchema()
    {
      execSQL("set character_set_database=utf8");

      LoadSchema(1);
      LoadSchema(2);
      LoadSchema(3);
      LoadSchema(4);

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM my_aspnet_schemaversion", Connection);
      object ver = cmd.ExecuteScalar();
      Assert.Equal(4, ver);

      cmd.CommandText = "SHOW CREATE TABLE my_aspnet_membership";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createSql = reader.GetString(1);
        Assert.True(createSql.IndexOf("CHARSET=utf8") != -1);
      }
    }

    [Fact]
    public void UpgradeV1ToV2()
    {
      LoadSchema(1);

      MySqlCommand cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createTable = reader.GetString(1);
        int index = createTable.IndexOf("COMMENT='1'");
        Assert.NotEqual(-1, index);
      }

      LoadSchema(2);
      cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createTable = reader.GetString(1);
        int index = createTable.IndexOf("COMMENT='2'");
        Assert.NotEqual(-1, index);
      }
    }

    [Fact]
    private void LoadData()
    {
      LoadSchema(1);
      LoadSchema(2);
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('1', 'user1', '', 'app1', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('2', 'user2', '', 'app1', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('3', 'user1', '', 'app2', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('4', 'user2', '', 'app2', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role1', 'app1')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role2', 'app1')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role1', 'app2')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role2', 'app2')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user1', 'role1', 'app1')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user2', 'role2', 'app1')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user1', 'role1', 'app2')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user2', 'role2', 'app2')");
      LoadSchema(3);
      Assert.False(TableExists("mysql_membership"));
      Assert.False(TableExists("mysql_roles"));
      Assert.False(TableExists("mysql_usersinroles"));
    }

    [Fact]
    public void CheckAppsUpgrade()
    {
      LoadData();

      DataTable apps = FillTable("SELECT * FROM my_aspnet_applications");
      Assert.Equal(2, apps.Rows.Count);
      Assert.Equal(1, apps.Rows[0]["id"]);
      Assert.Equal("app1", apps.Rows[0]["name"]);
      Assert.Equal(2, apps.Rows[1]["id"]);
      Assert.Equal("app2", apps.Rows[1]["name"]);
    }

    //    [Fact]
    //    public void CheckUsersUpgrade()
    //    {
    //      LoadData();

    //      DataTable dt = FillTable("SELECT * FROM my_aspnet_users");
    //      Assert.Equal(4, dt.Rows.Count);
    //      Assert.Equal(1, dt.Rows[0]["id"]);
    //      Assert.Equal(1, dt.Rows[0]["applicationId"]);
    //      Assert.Equal("user1", dt.Rows[0]["name"]);
    //      Assert.Equal(2, dt.Rows[1]["id"]);
    //      Assert.Equal(1, dt.Rows[1]["applicationId"]);
    //      Assert.Equal("user2", dt.Rows[1]["name"]);
    //      Assert.Equal(3, dt.Rows[2]["id"]);
    //      Assert.Equal(2, dt.Rows[2]["applicationId"]);
    //      Assert.Equal("user1", dt.Rows[2]["name"]);
    //      Assert.Equal(4, dt.Rows[3]["id"]);
    //      Assert.Equal(2, dt.Rows[3]["applicationId"]);
    //      Assert.Equal("user2", dt.Rows[3]["name"]);
    //    }

    //    [Fact]
    //    public void CheckRolesUpgrade()
    //    {
    //      LoadData();

    //      DataTable dt = FillTable("SELECT * FROM my_aspnet_roles");
    //      Assert.Equal(4, dt.Rows.Count);
    //      Assert.Equal(1, dt.Rows[0]["id"]);
    //      Assert.Equal(1, dt.Rows[0]["applicationId"]);
    //      Assert.Equal("role1", dt.Rows[0]["name"]);
    //      Assert.Equal(2, dt.Rows[1]["id"]);
    //      Assert.Equal(1, dt.Rows[1]["applicationId"]);
    //      Assert.Equal("role2", dt.Rows[1]["name"]);
    //      Assert.Equal(3, dt.Rows[2]["id"]);
    //      Assert.Equal(2, dt.Rows[2]["applicationId"]);
    //      Assert.Equal("role1", dt.Rows[2]["name"]);
    //      Assert.Equal(4, dt.Rows[3]["id"]);
    //      Assert.Equal(2, dt.Rows[3]["applicationId"]);
    //      Assert.Equal("role2", dt.Rows[3]["name"]);
    //    }

    //    [Fact]
    //    public void CheckMembershipUpgrade()
    //    {
    //      LoadData();

    //      DataTable dt = FillTable("SELECT * FROM my_aspnet_membership");
    //      Assert.Equal(4, dt.Rows.Count);
    //      Assert.Equal(1, dt.Rows[0]["userid"]);
    //      Assert.Equal(2, dt.Rows[1]["userid"]);
    //      Assert.Equal(3, dt.Rows[2]["userid"]);
    //      Assert.Equal(4, dt.Rows[3]["userid"]);
    //    }

    //    [Fact]
    //    public void CheckUsersInRolesUpgrade()
    //    {
    //      LoadData();

    //      DataTable dt = FillTable("SELECT * FROM my_aspnet_usersinroles");
    //      Assert.Equal(4, dt.Rows.Count);
    //      Assert.Equal(1, dt.Rows[0]["userid"]);
    //      Assert.Equal(1, dt.Rows[0]["roleid"]);
    //      Assert.Equal(2, dt.Rows[1]["userid"]);
    //      Assert.Equal(2, dt.Rows[1]["roleid"]);
    //      Assert.Equal(3, dt.Rows[2]["userid"]);
    //      Assert.Equal(3, dt.Rows[2]["roleid"]);
    //      Assert.Equal(4, dt.Rows[3]["userid"]);
    //      Assert.Equal(4, dt.Rows[3]["roleid"]);
    //    }

    /// <summary>
    /// Bug #39072 Web provider does not work
    /// </summary>
    [Fact]
    public void AutoGenerateSchema()
    {
      MySQLMembershipProvider provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("ConnectionStringName", "LocalMySqlServer");
      config.Add("autogenerateschema", "true");
      config.Add("applicationName", "/");
      config.Add("passwordFormat", "Clear");

      provider.Initialize(null, config);

      MembershipCreateStatus status;
      MembershipUser user = provider.CreateUser("boo", "password", "email@email.com",
          "question", "answer", true, null, out status);
    }

    //    [Fact]
    //    public void SchemaTablesUseSameEngine()
    //    {
    //      DropAllTables();

    //      for (int x = 1; x <= SchemaManager.Version; x++)
    //        LoadSchema(x);

    //      string query = string.Format("SELECT TABLE_NAME, ENGINE FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}'", st.conn.Database);
    //      MySqlCommand cmd = new MySqlCommand(query, st.conn);
    //      string lastEngine = null;
    //      string currentEngine;

    //      using (MySqlDataReader reader = cmd.ExecuteReader())
    //      {
    //        while (reader.Read())
    //        {
    //          currentEngine = reader.GetString("ENGINE");

    //          if (string.IsNullOrEmpty(lastEngine))
    //          {
    //            lastEngine = currentEngine;
    //          }

    //          Assert.Equal(lastEngine, currentEngine);
    //        }
    //      }
    //    }

    //    [Fact]    
    //    public void InitializeInvalidConnStringThrowsArgumentException()
    //    {
    //      Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    //      string connStr = configFile.ConnectionStrings.ConnectionStrings["LocalMySqlServer"].ConnectionString;
    //      string fakeConnectionString = connStr.Replace("database", "fooKey");     
    //      configFile.ConnectionStrings.ConnectionStrings["LocalMySqlServer"].ConnectionString = fakeConnectionString;
    //      configFile.Save();
    //      ConfigurationManager.RefreshSection("connectionStrings");

    //      MySQLMembershipProvider provider = new MySQLMembershipProvider();
    //      NameValueCollection config = new NameValueCollection();
    //      config.Add("connectionStringName", "LocalMySqlServer");

    //      Exception ex = Assert.Throws<ArgumentException>(() => provider.Initialize(null, config));
    //      Assert.Equal(ex.Message, "Keyword not supported.\r\nParameter name: fookey");

    //      configFile.ConnectionStrings.ConnectionStrings["LocalMySqlServer"].ConnectionString = connStr;
    //      configFile.Save();
    //      ConfigurationManager.RefreshSection("connectionStrings");

    //    }

    /// <summary>
    /// Checking fix for http://bugs.mysql.com/bug.php?id=65144 / http://clustra.no.oracle.com/orabugs/14495292
    /// (Net Connector 6.4.4 Asp.Net Membership Database fails on MySql Db of UTF32).
    /// </summary>
    [Fact]
    public void AttemptLatestSchemaVersion()
    {
      execSQL(string.Format("alter database `{0}` character set = 'utf32' collate = 'utf32_general_ci'", Connection.Database));
      for (int i = 1; i <= 4; i++)
      {
        LoadSchema(i);
      }
      MySQLRoleProvider roleProvider = new MySQLRoleProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("autogenerateschema", "true");
      roleProvider.Initialize(null, config);
    }
  }
}
