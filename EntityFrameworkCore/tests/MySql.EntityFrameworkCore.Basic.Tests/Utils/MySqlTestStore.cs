// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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


using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Reflection;

namespace MySql.Data.EntityFrameworkCore.Tests
{

  public class MyTestContext : DbContext
  {
    public MyTestContext()
    {
    }
  
    public MyTestContext(DbContextOptions options): base(options)
    {
    }
        
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      // get the class name of the caller to get a unique name for the database      
      string name = $"db-{this.GetType().Name.ToLowerInvariant()}";
      optionsBuilder.UseMySQL(MySQLTestStore.rootConnectionString + ";database=" + name  + ";");
    }
  }

  public class MySQLTestStore : IDisposable
  {
    public static string baseConnectionString
    {
        get { return $"server=localhost;user id=root;password=;port={Port()};sslmode=Required;pooling=false;"; }
    }

    public static string rootConnectionString
    {
        get { return $"server=localhost;user id=root;password=;port={Port()};sslmode=Required;pooling=false;"; }
    }

    private static string Port()
    {
      var port = Environment.GetEnvironmentVariable("MYSQL_PORT");
      return port == null ? "3306" : port;        
    }

    public static void CreateDatabase(string databaseName, bool deleteifExists = false, string script = null)
    {
        if (script != null)
        {
            if (deleteifExists)
                script = "Drop database if exists [database0];"  + script;

            script = script.Replace("[database0]", databaseName);
            //execute
            using (var cnn = new MySqlConnection(rootConnectionString))
            {
                cnn.Open();
                MySqlScript s = new MySqlScript(cnn, script);
                s.Execute();
            }
        }
        else
        {
            using (var cnn = new MySqlConnection(rootConnectionString))
            {
                cnn.Open();                    
                var cmd = new MySqlCommand(string.Format("Drop database {0}; Create Database {0};", databaseName), cnn);
                cmd.ExecuteNonQuery();                    
            }
        }
    }

    public static void Execute(string sql)
    {
        using (var cnn = new MySqlConnection(rootConnectionString))
        {
            cnn.Open();
            var cmd = new MySqlCommand(sql, cnn);
            cmd.ExecuteNonQuery();
        }
    }

    public static void ExecuteScript(string sql)
    {
        using (var cnn = new MySqlConnection(rootConnectionString))
        {
            cnn.Open();
            var scr = new MySqlScript(cnn, sql);
            scr.Execute();
        }
     }

    public static string CreateConnectionString(string databasename)
    {
      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
      csb.Database = databasename;
      csb.Port = Convert.ToUInt32(Port());
      csb.UserID = "root";
      csb.Password = "";
      csb.Server = "localhost";
      csb.Pooling = false;
      csb.SslMode = MySqlSslMode.None;

      return csb.ConnectionString;
    }

    public static void DeleteDatabase(string name)
    {
      using (var cnn = new MySqlConnection(rootConnectionString))
      {
        cnn.Open();
        var cmd =  new MySqlCommand(string.Format("DROP DATABASE IF EXISTS {0}", name), cnn);
        cmd.ExecuteNonQuery();
      }
    }

    public void Dispose()
    {
     //nothing to do yet
    }

  }
}

