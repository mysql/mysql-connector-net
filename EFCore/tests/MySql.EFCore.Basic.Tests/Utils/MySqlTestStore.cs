// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Data;

namespace MySql.EntityFrameworkCore.Basic.Tests.Utils
{
  public class MyTestContext : DbContext
  {
    public MyTestContext()
    {
    }

    public MyTestContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      // get the class name of the caller to get a unique name for the database
      if (!optionsBuilder.IsConfigured
        || optionsBuilder.Options.FindExtension<MySQLOptionsExtension>() == null)
      {
        optionsBuilder.UseMySQL(MySQLTestStore.GetContextConnectionString(this.GetType()));
      }
    }
  }

  public class MySQLTestStore : RelationalTestStore
  {
    public static bool SslMode { get; set; }
    private static string SslString
    {
      get { return SslMode ? "sslmode=Required;" : string.Empty; }
    }

    public static string BaseConnectionString
    {
      get { return $"server=localhost;user id=root;password=;port={Port()};{SslString}pooling=false;defaultcommandtimeout=50;"; }
    }

    public static string RootConnectionString
    {
      get { return $"server=localhost;user id=root;password=;port={Port()};{SslString}pooling=false;defaultcommandtimeout=50;"; }
    }

    internal static string GetContextConnectionString<T>()
      where T : DbContext
    {
      return GetContextConnectionString(typeof(T));
    }

    internal static string GetContextConnectionString(Type type)
    {
      string name = $"db-{type.Name.ToLowerInvariant()}";
      return MySQLTestStore.RootConnectionString + $";database={name};";
    }

    public static string Port()
    {
      var port = Environment.GetEnvironmentVariable("MYSQL_PORT");
      return port ?? "3306";
    }

    public static void CreateDatabase(string databaseName, bool deleteifExists = false, string? script = null)
    {
      if (script != null)
      {
        if (deleteifExists)
          script = "Drop database if exists [database0];" + script;

        script = script.Replace("[database0]", databaseName);
        //execute
        using (var cnn = new MySqlConnection(RootConnectionString))
        {
          cnn.Open();
          MySqlScript s = new MySqlScript(cnn, script);
          s.Execute();
        }
      }
      else
      {
        using (var cnn = new MySqlConnection(RootConnectionString))
        {
          cnn.Open();
          var cmd = new MySqlCommand(string.Format("Drop database {0}; Create Database {0};", databaseName), cnn);
          cmd.ExecuteNonQuery();
        }
      }
    }

    public static void Execute(string sql)
    {
      using (var cnn = new MySqlConnection(RootConnectionString))
      {
        cnn.Open();
        var cmd = new MySqlCommand(sql, cnn);
        cmd.ExecuteNonQuery();
      }
    }

    public static void ExecuteScript(string sql)
    {
      using (var cnn = new MySqlConnection(RootConnectionString))
      {
        cnn.Open();
        var scr = new MySqlScript(cnn, sql);
        scr.Execute();
      }
    }

    public int ExecuteNonQuery(string sql, params object[] parameters)
    {
      using (var cnn = new MySqlConnection(RootConnectionString))
      {
        cnn.Open();
        var scr = new MySqlScript(cnn, sql);
        ExecuteNonQuery(cnn, sql, parameters);
        return scr.Execute();
      }
    }

    private static int ExecuteNonQuery(MySqlConnection connection, string sql, object[]? parameters = null)
        => Execute(connection, command => command.ExecuteNonQuery(), sql, false, parameters);

    private static T Execute<T>(
                MySqlConnection connection, Func<MySqlCommand, T> execute, string sql,
                bool useTransaction = false, object[]? parameters = null)
                => ExecuteCommand(connection, execute, sql, useTransaction, parameters!);

    private static T ExecuteCommand<T>(
                MySqlConnection connection, Func<MySqlCommand, T> execute, string sql, bool useTransaction, object[] parameters)
    {
      if (connection.State != ConnectionState.Open)
      {
        connection.Close();
      }

      connection.Open();

      try
      {
        using (var transaction = useTransaction
            ? connection.BeginTransaction()
            : null)
        {
          T result;
          var command = (MySqlCommand)connection.CreateCommand();
          command.CommandText = sql;

          command.Transaction = transaction;
          result = execute(command);

          transaction?.Commit();

          return result;
        }
      }
      finally
      {
        if (connection.State != ConnectionState.Closed)
        {
          connection.Close();
        }
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
      csb.SslMode = MySqlSslMode.Disabled;

      return csb.ConnectionString;
    }

    public static void DeleteDatabase(string name)
    {
      using (var cnn = new MySqlConnection(RootConnectionString))
      {
        cnn.Open();
        var cmd = new MySqlCommand(string.Format("DROP DATABASE IF EXISTS {0}", name), cnn);
        cmd.ExecuteNonQuery();
      }
    }

    public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
      => builder.UseMySQL(GetContextConnectionString<MyTestContext>());

    public override void Clean(DbContext context)
      => context.Database.EnsureDeleted();

    public static MySQLTestStore Create(string name)
      => new MySQLTestStore(name);

    public static MySQLTestStore GetOrCreate(string name)
      => new MySQLTestStore(name);

    private MySQLTestStore(string name)
    : base(name, true)
    {
      SslMode = true;
      Connection = new MySqlConnection(RootConnectionString);
    }
  }

  public class MySQLTestStoreFactory : RelationalTestStoreFactory
  {
    public static MySQLTestStoreFactory Instance { get; } = new MySQLTestStoreFactory();

    protected MySQLTestStoreFactory()
    {
    }

    public override TestStore Create(string storeName)
        => MySQLTestStore.Create(storeName);

    public override TestStore GetOrCreate(string storeName)
        => MySQLTestStore.GetOrCreate(storeName);

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkMySQL();
  }
}
