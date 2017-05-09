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

using System.Data.Entity.Core.Objects;
using System.Text.RegularExpressions;
using System.Data.Entity.Infrastructure;
using Xunit;
using System;
using MySql.Data.MySqlClient;

namespace MySql.Data.Entity.Tests
{
  public class DefaultFixture : IDisposable
  {
    public string host { get; set; }
    public string user { get; set; }
    public string password { get; set; }
    public int port { get; set; }
    public string database { get; set; }
    public Version version { get; set; }
    public MySqlConnection Connection { get; set; }
    public string ConnectionString { get; set; }
    public bool NeedSetup { get; set; }

    public DefaultFixture()
    {
      NeedSetup = true;
    }

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

    public virtual bool Setup(Type t)
    {
      if (!NeedSetup) return false;
      NeedSetup = false;

      database = "db-" + t.Name.ToLower();
      if (database.Length > 32)
        database = database.Substring(0, 32);

      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder();
      sb.Server = "localhost";
      sb.Port = 3306;
      sb.UserID = "root";
      sb.Pooling = false;
      sb.AllowUserVariables = true;
      sb.Database = database;
      ConnectionString = sb.ToString();

      using (DefaultContext ctx = new DefaultContext(ConnectionString))
      {
        if (ctx.Database.Exists())
          ctx.Database.Delete();
        ctx.Database.Create();
      }

      Connection = new MySqlConnection(ConnectionString);
      Connection.Open();
      return true;
    }

    public void execSQL(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.ExecuteNonQuery();
    }

    public virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        MySqlConnection.ClearAllPools();
        execSQL($"DROP DATABASE IF EXISTS `{Connection.Database}`");
        Connection.Close();
      }
    }

    public virtual void Dispose()
    {
      Dispose(true);
    }

    public void CheckSql(string actual, string expected)
    {
      var exp = Regex.Replace(expected, @"\s", string.Empty);
      var act = Regex.Replace(actual, @"\s", string.Empty);
      Assert.Equal(act, exp);
    }

    public void CheckSqlContains(string actual, string expected)
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
