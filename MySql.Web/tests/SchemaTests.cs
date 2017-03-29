// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using MySql.Web.Common;
using MySql.Web.Security;
using System.Collections.Specialized;
using System.Configuration.Provider;
using MySql.Data.MySqlClient;
using System.Web.Security;
using System.Configuration;

namespace MySql.Web.Tests
{
  public class SchemaTests : IUseFixture<SetUpWeb>, IDisposable
  {
    private SetUpWeb st;

    public void SetFixture(SetUpWeb data)
    {
      st = data;
      DropAllTables();
    }

    private void DropAllTables()
    {
      DataTable dt = st.conn.GetSchema("Tables");
      foreach (DataRow row in dt.Rows)
        st.execSQL(String.Format("DROP TABLE IF EXISTS {0}", row["TABLE_NAME"]));
    }

    public void Dispose()
    {
      //Nothing to clean
    }

    /// <summary>
    /// Bug #37469 autogenerateschema optimizing
    /// </summary>
    [Fact]
    public void SchemaCheck()
    {
      for (int i = 0; i <= SchemaManager.Version; i++)
      {
        DropAllTables();
        MySQLMembershipProvider provider = new MySQLMembershipProvider();
        NameValueCollection config = new NameValueCollection();
        config.Add("connectionStringName", "LocalMySqlServer");
        config.Add("applicationName", "/");
        config.Add("passwordFormat", "Clear");

        if (i > 0)
          for (int x = 1; x <= i; x++)
            st.LoadSchema(x);

       try
        {
          provider.Initialize(null, config);
          Assert.False(i < SchemaManager.Version, "This should have failed");
        }
        catch (ProviderException)
        {
          Assert.False(i == SchemaManager.Version,"This should not have failed");
        }
      }
    }

    /// <summary>
    /// Bug #36444 'autogenerateschema' produces tables with 'random' collations 
    /// </summary>
    [Fact]
    public void CurrentSchema()
    {
      st.execSQL("set character_set_database=utf8");

      st.LoadSchema(1);
      st.LoadSchema(2);
      st.LoadSchema(3);
      st.LoadSchema(4);

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM my_aspnet_schemaversion", st.conn);
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
      st.LoadSchema(1);

      MySqlCommand cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createTable = reader.GetString(1);
        int index = createTable.IndexOf("COMMENT='1'");
        Assert.NotEqual(-1, index);
      }

      st.LoadSchema(2);
      cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", st.conn);
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
      st.LoadSchema(1);
      st.LoadSchema(2);
      st.execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('1', 'user1', '', 'app1', '2007-01-01')");
      st.execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('2', 'user2', '', 'app1', '2007-01-01')");
      st.execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('3', 'user1', '', 'app2', '2007-01-01')");
      st.execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('4', 'user2', '', 'app2', '2007-01-01')");
      st.execSQL(@"INSERT INTO mysql_roles VALUES ('role1', 'app1')");
      st.execSQL(@"INSERT INTO mysql_roles VALUES ('role2', 'app1')");
      st.execSQL(@"INSERT INTO mysql_roles VALUES ('role1', 'app2')");
      st.execSQL(@"INSERT INTO mysql_roles VALUES ('role2', 'app2')");
      st.execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user1', 'role1', 'app1')");
      st.execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user2', 'role2', 'app1')");
      st.execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user1', 'role1', 'app2')");
      st.execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user2', 'role2', 'app2')");
      st.LoadSchema(3);
      Assert.False(st.TableExists("mysql_membership"));
      Assert.False(st.TableExists("mysql_roles"));
      Assert.False(st.TableExists("mysql_usersinroles"));
    }

    [Fact]
    public void CheckAppsUpgrade()
    {
      LoadData();

      DataTable apps = st.FillTable("SELECT * FROM my_aspnet_applications");
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

//      DataTable dt = st.FillTable("SELECT * FROM my_aspnet_users");
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

//      DataTable dt = st.FillTable("SELECT * FROM my_aspnet_roles");
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

//      DataTable dt = st.FillTable("SELECT * FROM my_aspnet_membership");
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

//      DataTable dt = st.FillTable("SELECT * FROM my_aspnet_usersinroles");
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
      config.Add("connectionStringName", "LocalMySqlServer");
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
//        st.LoadSchema(x);

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
      // UTF32 is only supported 
      if (st.Version.Minor >= 5)
      {
        st.execSQL(string.Format("alter database `{0}` character set = 'utf32' collate = 'utf32_general_ci'", st.database0));
        for (int i = 1; i <= 4; i++)
        {
          st.LoadSchema(i);
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
}
