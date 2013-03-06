// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
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

//  This code was contributed by Sean Wright (srwright@alcor.concordia.ca) on 2007-01-12
//  The copyright was assigned and transferred under the terms of
//  the MySQL Contributor License Agreement (CLA)

using NUnit.Framework;
using MySql.Web.Security;
using System.Collections.Specialized;
using MySql.Data.MySqlClient;
using System.Resources;
using System.Data;
using System;
using System.IO;
using System.Configuration.Provider;
using System.Web.Security;
using MySql.Web.Common;
using System.Configuration;

namespace MySql.Web.Tests
{
  [TestFixture]
  public class SchemaTests : BaseWebTest
  {
    [SetUp]
    public override void Setup()
    {
      base.Setup();
      DropAllTables();
    }

    private void DropAllTables()
    {
      DataTable dt = conn.GetSchema("Tables");
      foreach (DataRow row in dt.Rows)
        execSQL(String.Format("DROP TABLE IF EXISTS {0}", row["TABLE_NAME"]));
    }

    /// <summary>
    /// Bug #37469 autogenerateschema optimizing
    /// </summary>
    [Test]
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
            LoadSchema(x);

        try
        {
          provider.Initialize(null, config);
          if (i < SchemaManager.Version)
            Assert.Fail("Should have failed");
        }
        catch (ProviderException)
        {
          if (i == SchemaManager.Version)
            Assert.Fail("This should not have failed");
        }
      }
    }

    /// <summary>
    /// Bug #36444 'autogenerateschema' produces tables with 'random' collations 
    /// </summary>
    [Test]
    public void CurrentSchema()
    {
      execSQL("set character_set_database=utf8");

      LoadSchema(1);
      LoadSchema(2);
      LoadSchema(3);
      LoadSchema(4);

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM my_aspnet_schemaversion", conn);
      object ver = cmd.ExecuteScalar();
      Assert.AreEqual(4, ver);

      cmd.CommandText = "SHOW CREATE TABLE my_aspnet_membership";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createSql = reader.GetString(1);
        Assert.IsTrue(createSql.IndexOf("CHARSET=utf8") != -1);
      }
    }

    [Test]
    public void UpgradeV1ToV2()
    {
      LoadSchema(1);

      MySqlCommand cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createTable = reader.GetString(1);
        int index = createTable.IndexOf("COMMENT='1'");
        Assert.AreNotEqual(-1, index);
      }

      LoadSchema(2);
      cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createTable = reader.GetString(1);
        int index = createTable.IndexOf("COMMENT='2'");
        Assert.AreNotEqual(-1, index);
      }
    }

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
      Assert.IsFalse(TableExists("mysql_membership"));
      Assert.IsFalse(TableExists("mysql_roles"));
      Assert.IsFalse(TableExists("mysql_usersinroles"));
    }

    [Test]
    public void CheckAppsUpgrade()
    {
      LoadData();

      DataTable apps = FillTable("SELECT * FROM my_aspnet_applications");
      Assert.AreEqual(2, apps.Rows.Count);
      Assert.AreEqual(1, apps.Rows[0]["id"]);
      Assert.AreEqual("app1", apps.Rows[0]["name"]);
      Assert.AreEqual(2, apps.Rows[1]["id"]);
      Assert.AreEqual("app2", apps.Rows[1]["name"]);
    }

    [Test]
    public void CheckUsersUpgrade()
    {
      LoadData();

      DataTable dt = FillTable("SELECT * FROM my_aspnet_users");
      Assert.AreEqual(4, dt.Rows.Count);
      Assert.AreEqual(1, dt.Rows[0]["id"]);
      Assert.AreEqual(1, dt.Rows[0]["applicationId"]);
      Assert.AreEqual("user1", dt.Rows[0]["name"]);
      Assert.AreEqual(2, dt.Rows[1]["id"]);
      Assert.AreEqual(1, dt.Rows[1]["applicationId"]);
      Assert.AreEqual("user2", dt.Rows[1]["name"]);
      Assert.AreEqual(3, dt.Rows[2]["id"]);
      Assert.AreEqual(2, dt.Rows[2]["applicationId"]);
      Assert.AreEqual("user1", dt.Rows[2]["name"]);
      Assert.AreEqual(4, dt.Rows[3]["id"]);
      Assert.AreEqual(2, dt.Rows[3]["applicationId"]);
      Assert.AreEqual("user2", dt.Rows[3]["name"]);
    }

    [Test]
    public void CheckRolesUpgrade()
    {
      LoadData();

      DataTable dt = FillTable("SELECT * FROM my_aspnet_roles");
      Assert.AreEqual(4, dt.Rows.Count);
      Assert.AreEqual(1, dt.Rows[0]["id"]);
      Assert.AreEqual(1, dt.Rows[0]["applicationId"]);
      Assert.AreEqual("role1", dt.Rows[0]["name"]);
      Assert.AreEqual(2, dt.Rows[1]["id"]);
      Assert.AreEqual(1, dt.Rows[1]["applicationId"]);
      Assert.AreEqual("role2", dt.Rows[1]["name"]);
      Assert.AreEqual(3, dt.Rows[2]["id"]);
      Assert.AreEqual(2, dt.Rows[2]["applicationId"]);
      Assert.AreEqual("role1", dt.Rows[2]["name"]);
      Assert.AreEqual(4, dt.Rows[3]["id"]);
      Assert.AreEqual(2, dt.Rows[3]["applicationId"]);
      Assert.AreEqual("role2", dt.Rows[3]["name"]);
    }

    [Test]
    public void CheckMembershipUpgrade()
    {
      LoadData();

      DataTable dt = FillTable("SELECT * FROM my_aspnet_membership");
      Assert.AreEqual(4, dt.Rows.Count);
      Assert.AreEqual(1, dt.Rows[0]["userid"]);
      Assert.AreEqual(2, dt.Rows[1]["userid"]);
      Assert.AreEqual(3, dt.Rows[2]["userid"]);
      Assert.AreEqual(4, dt.Rows[3]["userid"]);
    }

    [Test]
    public void CheckUsersInRolesUpgrade()
    {
      LoadData();

      DataTable dt = FillTable("SELECT * FROM my_aspnet_usersinroles");
      Assert.AreEqual(4, dt.Rows.Count);
      Assert.AreEqual(1, dt.Rows[0]["userid"]);
      Assert.AreEqual(1, dt.Rows[0]["roleid"]);
      Assert.AreEqual(2, dt.Rows[1]["userid"]);
      Assert.AreEqual(2, dt.Rows[1]["roleid"]);
      Assert.AreEqual(3, dt.Rows[2]["userid"]);
      Assert.AreEqual(3, dt.Rows[2]["roleid"]);
      Assert.AreEqual(4, dt.Rows[3]["userid"]);
      Assert.AreEqual(4, dt.Rows[3]["roleid"]);
    }

    /// <summary>
    /// Bug #39072 Web provider does not work
    /// </summary>
    [Test]
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

    [Test]
    public void SchemaTablesUseSameEngine()
    {
      DropAllTables();

      for (int x = 1; x <= SchemaManager.Version; x++)
        LoadSchema(x);

      string query = string.Format("SELECT TABLE_NAME, ENGINE FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}'", conn.Database);
      MySqlCommand cmd = new MySqlCommand(query, conn);
      string lastEngine = null;
      string currentEngine;

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          currentEngine = reader.GetString("ENGINE");

          if (string.IsNullOrEmpty(lastEngine))
          {
            lastEngine = currentEngine;
          }

          Assert.AreEqual(lastEngine, currentEngine);
        }
      }
    }

    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void InitializeInvalidConnStringThrowsArgumentException()
    {
      Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      string connStr = configFile.ConnectionStrings.ConnectionStrings["LocalMySqlServer"].ConnectionString;
      string fakeConnectionString = connStr.Replace("database", "fooKey");
      try
      {
        configFile.ConnectionStrings.ConnectionStrings["LocalMySqlServer"].ConnectionString = fakeConnectionString;
        configFile.Save();
        ConfigurationManager.RefreshSection("connectionStrings");

        MySQLMembershipProvider provider = new MySQLMembershipProvider();
        NameValueCollection config = new NameValueCollection();
        config.Add("connectionStringName", "LocalMySqlServer");

        provider.Initialize(null, config);
      }
      finally
      {
        configFile.ConnectionStrings.ConnectionStrings["LocalMySqlServer"].ConnectionString = connStr;
        configFile.Save();
        ConfigurationManager.RefreshSection("connectionStrings");
      }
    }

    /// <summary>
    /// Checking fix for http://bugs.mysql.com/bug.php?id=65144 / http://clustra.no.oracle.com/orabugs/14495292
    /// (Net Connector 6.4.4 Asp.Net Membership Database fails on MySql Db of UTF32).
    /// </summary>
    [Test]
    public void AttemptLatestSchemaVersion()
    {
      // UTF32 is only supported 
      if (Version.Minor >= 5)
      {
        execSQL(string.Format("alter database `{0}` character set = 'utf32' collate = 'utf32_general_ci'", database0));
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
}
