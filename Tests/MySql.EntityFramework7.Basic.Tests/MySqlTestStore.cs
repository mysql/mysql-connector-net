// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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



using Microsoft.Data.Entity;
using MySql.Data.MySqlClient;
using MySQL.Data.Entity.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.Entity.Tests
{

  public class MyTestContext : DbContext
  {
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseMySQL(MySqlTestStore.baseConnectionString + ";database=test;");
    }
  }

  public class MySqlTestStore : IDisposable
  {
    public const string baseConnectionString = "server=localhost;userid=test;password=test;port=3305;";
    public const string rootConnectionString = "server=localhost;userid=root;password='';port=3305;";    

    public static void CreateDatabase(string databaseName, string script = null)
    {      
      if (script != null)
      {
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        Stream stream = executingAssembly.GetManifestResourceStream("MySql.Data.Entity.Tests.Properties.DatabaseSetup.sql");
        StreamReader sr = new StreamReader(stream);
        string sql = sr.ReadToEnd();
        sr.Close();
        sql = sql.Replace("[database0]", databaseName);

        //execute
        using (var cnn = new MySqlConnection(rootConnectionString))
        {
          cnn.Open();
          MySqlScript s = new MySqlScript(cnn, sql);
          s.Execute();
        }
      }
    }

    public static string CreateConnectionString(string databasename)
    {
      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
      csb.Database = databasename;
      csb.Port = 3305;
      csb.UserID = "test";
      csb.Password = "test";
      csb.Server = "localhost";

      return csb.ConnectionString;
    }

    private void DeleteDatabase(string name)
    {
      using (var cnn = new MySqlConnection(rootConnectionString))
      {
        cnn.Open();
        var cmd =  new MySqlCommand(string.Format("DROP database {0}", name), cnn);
        cmd.ExecuteNonQuery();
      }

    }

    public void Dispose()
    {
     //nothing to do yet
    }

  }
}

