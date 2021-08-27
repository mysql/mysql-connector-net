// Copyright (c) 2013, 2021, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using System.Data;
using System.Configuration;
using System.Reflection;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity;
using MySql.Data.EntityFramework.Tests;
using MySql.EntityFramework.CodeFirst.Tests;

namespace MySql.Data.EntityFramework.CodeFirst.Tests
{
  public class CodeFirstFixture : DefaultFixture
  {
    // A trace listener to use during testing.
    private AssertFailTraceListener asertFailListener = new AssertFailTraceListener();

    public CodeFirstFixture()
    {
      // Initilizes MySql EF configuration
      MySqlEFConfiguration.SetConfiguration(new MySqlEFConfiguration());
    }

    [OneTimeSetUp]
    public new void OneTimeSetup()
    {
      // Override sql_mode so it converts automatically from varchar(65535) to text
      MySqlCommand cmd = new MySqlCommand("SET GLOBAL SQL_MODE=``", Connection);
      cmd.ExecuteNonQuery();

      // Replace existing listeners with listener for testing.
      Trace.Listeners.Clear();
      Trace.Listeners.Add(this.asertFailListener);

      DataSet dataSet = ConfigurationManager.GetSection("system.data") as DataSet;
      if (dataSet != null)
      {
        DataView vi = dataSet.Tables[0].DefaultView;
        vi.Sort = "Name";
        int idx = -1;
        if (((idx = vi.Find("MySql")) != -1) || ((idx = vi.Find("MySQL Data Provider")) != -1))
        {
          DataRow row = vi[idx].Row;
          dataSet.Tables[0].Rows.Remove(row);
        }
        dataSet.Tables[0].Rows.Add("MySql"
          , "MySql.Data.MySqlClient"
          , "MySql.Data.MySqlClient"
          ,
          typeof(MySqlClientFactory).AssemblyQualifiedName);
      }

      cmd = new MySqlCommand("SELECT COUNT(SCHEMA_NAME) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'sakila'", Connection);

      if (Convert.ToInt32(cmd.ExecuteScalar() ?? 0) == 0)
      {
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        using (var stream = executingAssembly.GetManifestResourceStream("MySql.EntityFramework.CodeFirst.Tests.Properties.sakila-schema.sql"))
        {
          using (StreamReader sr = new StreamReader(stream))
          {
            string sql = sr.ReadToEnd();
            MySqlScript s = new MySqlScript(Connection, sql);
            s.Execute();
          }
        }

        using (var stream = executingAssembly.GetManifestResourceStream("MySql.EntityFramework.CodeFirst.Tests.Properties.sakila-data.sql"))
        {
          using (StreamReader sr = new StreamReader(stream))
          {
            string sql = sr.ReadToEnd();
            MySqlScript s = new MySqlScript(Connection, sql);
            s.Execute();
          }
        }
      }
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
      DeleteContext<AutoIncrementBugContext>();
      DeleteContext<MovieDBContext>();
      DeleteContext<SakilaDb>();
      DeleteContext<DinosauriaDBContext>();
      DeleteContext<MovieCodedBasedConfigDBContext>();
      DeleteContext<EnumTestSupportContext>();
      DeleteContext<JourneyContext>();
      DeleteContext<EntityAndComplexTypeContext>();
      DeleteContext<PromotionsDB>();
      DeleteContext<ShipContext>();
      DeleteContext<SiteDbContext>();
      DeleteContext<VehicleDbContext>();
      DeleteContext<VehicleDbContext2>();
      DeleteContext<VehicleDbContext3>();
      DeleteContext<ProductsDbContext>();
      DeleteContext<ShortDbContext>();
      DeleteContext<UsingUnionContext>();
      DeleteContext<BlogContext>();
      DeleteContext<ContextForString>();
      DeleteContext<ContextForNormalFk>();
      DeleteContext<ContextForLongFk>();
      DeleteContext<ContextForTinyPk>();
      DeleteContext<ContextForBigIntPk>();
    }

    public static string GetEFConnectionString<T>(string database = null) where T : DbContext
    {
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder();
      sb.Server = "localhost";
      string port = Environment.GetEnvironmentVariable("MYSQL_PORT");
      sb.Port = string.IsNullOrEmpty(port) ? 3306 : uint.Parse(port);
      sb.UserID = "root";
      sb.Pooling = false;
      sb.AllowUserVariables = true;
      sb.Database = database ?? typeof(T).Name;

      return sb.ToString();
    }

    private void DeleteContext<T>() where T : DbContext, new()
    {
      using (var context = new T())
      {
        context.Database.Delete();
      }
    }

    private EntityConnection GetEntityConnection()
    {
      return null;
    }

    internal protected static new void CheckSql(string sql, string refSql)
    {
      StringBuilder str1 = new StringBuilder();
      StringBuilder str2 = new StringBuilder();
      foreach (char c in sql)
        if (!Char.IsWhiteSpace(c))
          str1.Append(c);
      foreach (char c in refSql)
        if (!Char.IsWhiteSpace(c))
          str2.Append(c);
      Assert.AreEqual(0, String.Compare(str1.ToString(), str2.ToString(), true));
    }

    private class AssertFailTraceListener : DefaultTraceListener
    {
      public override void Fail(string message)
      {
        Assert.True(message == String.Empty, "Failure: " + message);
      }

      public override void Fail(string message, string detailMessage)
      {
        Assert.True(message == String.Empty, "Failure: " + message);
      }
    }
  }
}