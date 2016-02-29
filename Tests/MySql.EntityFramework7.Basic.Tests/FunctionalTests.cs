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
using MySql.Data.Entity.Tests.DbContextClasses;
using System;
using System.Linq;
using Xunit;
using MySQL.Data.Entity;
using MySQL.Data.Entity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Entity.Infrastructure;
using MySql.Data.MySqlClient;
using Microsoft.Data.Entity.Internal;

namespace MySql.Data.Entity.Tests
{
  public class FunctionalTests : IDisposable
  {

    [Fact]
    public void CanConnectWithConnectionOnConfiguring()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<ConnectionStringInOnConfiguringTestContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConnectionStringInOnConfiguringTestContext>())
      {
        Assert.True(context.Posts.Any());
      }

    }


    [Fact]
    public void CanThrowExceptionWhenNoConfiguration()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<NoConfigurationContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<NoConfigurationContext>())
      {
        Assert.Equal(CoreStrings.NoProviderConfigured, Assert.Throws<InvalidOperationException>(() => context.Blogs.Any()).Message);
      }
    }


    [Fact]
    public void CreatedDb()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<TestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<TestsContext>())
      {
        context.Database.EnsureCreated();        
        using (var cnn = new MySqlConnection(MySqlTestStore.baseConnectionString + string.Format(";database={0}", context.Database.GetDbConnection().Database)))
        {
          cnn.Open();
          var cmd = new MySqlCommand(string.Format("SHOW DATABASES LIKE '{0}'", context.Database.GetDbConnection().Database), cnn);
          var reader = cmd.ExecuteReader();
          while (reader.Read())
          {
            Assert.True(string.Equals("test", reader.GetString(0), StringComparison.CurrentCultureIgnoreCase), "Database was not created");
          }
        }
        context.Database.EnsureDeleted();
      }
    }


    [Fact]
    public void EnsureRelationalPatterns()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<TestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<TestsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySqlTestStore.baseConnectionString + string.Format(";database={0}", context.Database.GetDbConnection().Database)))
        {
          cnn.Open();
          var cmd = new MySqlCommand(string.Format("SHOW DATABASES LIKE '{0}'", context.Database.GetDbConnection().Database), cnn);
          var reader = cmd.ExecuteReader();
          while (reader.Read())
          {
            Assert.True(string.Equals("test", reader.GetString(0), StringComparison.CurrentCultureIgnoreCase), "Database was not created");
          }
        }
        context.Database.EnsureDeleted();
      }
    }


    [Fact]
    public void CanUseIgnoreEntity()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<SimpleContextWithIgnore>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<SimpleContextWithIgnore>())
      {
        context.Database.EnsureCreated();
        Assert.True(context.Model.GetEntityTypes().Count() == 2, "Wrong model generation");
        Assert.True(context.Blogs.ToList().Count == 0, "");
        // TODO check why this is not valid
        //Assert.True(context.Posts.ToList().Count == 0, "");
        context.Database.EnsureDeleted();
      }
    }



    [Fact]
    public void CanUseOptionsInDbContextCtor()
    {      
      using (var context = new OptionsContext(new DbContextOptions<OptionsContext>(),
                                              new MySqlConnection(MySqlTestStore.CreateConnectionString("test"))))
      {
        Assert.True(context.Blogs.Any());
      }

    }


    public void Dispose()
    {

    }


    #region ContextClasses

    protected class OptionsContext : SimpleContext
    {
      private readonly MySqlConnection _connection;
      private readonly DbContextOptions _options;

      public OptionsContext(DbContextOptions options, MySqlConnection connection)
          : base(options)
      {
        _options = options;
        _connection = connection;
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        Assert.Same(_options, optionsBuilder.Options);

        optionsBuilder.UseMySQL(_connection);

        Assert.NotSame(_options, optionsBuilder.Options);
      }

      public override void Dispose()
      {
        _connection.Dispose();
        base.Dispose();
      }
    }

    private class ConnectionStringInOnConfiguringTestContext : TestsContext
    {
      public ConnectionStringInOnConfiguringTestContext(IServiceProvider serviceProvider, DbContextOptions options) 
        : base(serviceProvider, options)
      {

      }
      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        optionsBuilder.UseMySQL(MySqlTestStore.CreateConnectionString("test"));
      }
    }


    private class UseConnectionInOnConfiguring : TestsContext
    {
      private MySqlConnection _connection;

      public UseConnectionInOnConfiguring(MySqlConnection connection)
      {
        _connection = connection;
      }      

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        optionsBuilder.UseMySQL(_connection);
      }

      public override void Dispose()
      {
        _connection.Dispose();
        base.Dispose();
      }
    }


    #endregion
  }
}
