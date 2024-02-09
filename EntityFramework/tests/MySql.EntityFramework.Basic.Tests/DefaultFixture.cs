// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using System.Data.Entity.Core.Objects;
using System.Text.RegularExpressions;
using System.Data.Entity.Infrastructure;
using NUnit.Framework;
using System;
using MySql.Data.MySqlClient;

namespace MySql.Data.EntityFramework.Tests
{
  public class DefaultFixture
  {
    public string host { get; set; }
    public string user { get; set; }
    public string password { get; set; }
    public uint Port { get; set; }
    public string database { get; set; }
    public Version version { get; set; }
    public MySqlConnection Connection { get; set; }
    public string ConnectionString { get; set; }
    public bool NeedSetup { get; set; }

    public DefaultFixture()
    {
      NeedSetup = true;
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
      if (NeedSetup)
      {
        NeedSetup = false;

        database = "db-" + this.GetType().Name.ToLower();
        if (database.Length > 32)
          database = database.Substring(0, 32);

        MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder();
        sb.Server = "localhost";
        string port = Environment.GetEnvironmentVariable("MYSQL_PORT");
        sb.Port = Port = string.IsNullOrEmpty(port) ? 3306 : uint.Parse(port);
        sb.UserID = "root";
        sb.Pooling = false;
        sb.AllowUserVariables = true;
        sb.Database = database;
        ConnectionString = sb.ToString();

        using (DefaultContext ctx = new DefaultContext(ConnectionString))
        {
          if (ctx.Database.Exists())
            ctx.Database.Delete();
          var context = ((IObjectContextAdapter)ctx).ObjectContext;
          context.CreateDatabase();
        }

        Connection = new MySqlConnection(ConnectionString);
        Connection.Open();
        LoadData();
      }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      MySqlConnection.ClearAllPools();
      ExecSQL($"DROP DATABASE IF EXISTS `{Connection.Database}`");
      Connection.Close();
    }

    [SetUp]
    public virtual void SetUp()
    {
      if (NeedSetup)
      {
        NeedSetup = false;

        using (DefaultContext ctx = new DefaultContext(ConnectionString))
        {
          if (ctx.Database.Exists())
            ctx.Database.Delete();
          var context = ((IObjectContextAdapter)ctx).ObjectContext;
          context.CreateDatabase();
        }

        LoadData();
      }
    }

    [TearDown]
    public virtual void TearDown() { }

    public virtual void LoadData() { }

    public Version Version
    {
      get
      {
        if (version == null)
        {
          string versionString = Connection.ServerVersion;
          int i = 0;
          while (i < versionString.Length &&
              (Char.IsDigit(versionString[i]) || versionString[i] == '.'))
            i++;

          version = new Version(versionString.Substring(0, i));
        }
        return version;
      }
    }

    public void ExecSQL(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.ExecuteNonQuery();
    }

    public static void CheckSql(string actual, string expected)
    {
      var exp = Regex.Replace(expected, @"\s", string.Empty);
      var act = Regex.Replace(actual, @"\s", string.Empty);
      Assert.AreEqual(act, exp);
    }

    public static void CheckSqlContains(string actual, string expected)
    {
      var exp = Regex.Replace(expected, @"\s", string.Empty);
      var act = Regex.Replace(actual, @"\s", string.Empty);
      Assert.True(act.Contains(exp));
    }

    public DefaultContext GetDefaultContext()
    {
      return new DefaultContext(ConnectionString);
    }

    public void TestESql<T>(string eSql, string expected, params ObjectParameter[] parms)
    {
      using (DefaultContext ctx = GetDefaultContext())
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<T> q = context.CreateQuery<T>(eSql);
        foreach (var p in parms)
          q.Parameters.Add(p);

        string sql = q.ToTraceString();
        CheckSql(sql, expected);
      }
    }
  }
}
