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

using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Extensions;
using NUnit.Framework;
using System;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class MySQLDatabaseFacadeTest
  {
    [Test]
    public void IsMySQLWhenUsingOnConfiguring()
    {
      using (var context = new MySQLOnConfiguringContext())
      {
        Assert.True(context.Database.IsMySql());
      }
    }

    [Test]
    public void IsMySQLInOnModelCreatingWhenUsingOnConfiguring()
    {
      using (var context = new MySQLOnModelContext())
      {
        var _ = context.Model; // Trigger context initialization
        Assert.True(context.IsMySQLSet);
      }
    }

    [Test]
    public void IsMySQLInConstructorWhenUsingOnConfiguring()
    {
      using (var context = new MySQLConstructorContext())
      {
        var _ = context.Model; // Trigger context initialization
        Assert.True(context.IsMySQLSet);
      }
    }

    [Test]
    public void CannotUseIsMySQLInOnConfiguring()
    {
      using (var context = new MySQLUseInOnConfiguringContext())
      {
        Assert.AreEqual(
            CoreStrings.RecursiveOnConfiguring,
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                  var _ = context.Model; // Trigger context initialization
                })!.Message);
      }
    }

    [Test]
    public void IsMySQLWhenUsingConstructor()
    {
      using (var context = new ProviderContext(
          new DbContextOptionsBuilder()
              .UseInternalServiceProvider(MySQLFixture.DefaultServiceProvider)
              .UseMySQL("Database=Maltesers").Options))
      {
        Assert.True(context.Database.IsMySql());
      }
    }

    [Test]
    public void IsMySQLInOnModelCreatingWhenUsingConstructor()
    {
      using (var context = new ProviderOnModelContext(
          new DbContextOptionsBuilder()
              .UseInternalServiceProvider(MySQLFixture.DefaultServiceProvider)
              .UseMySQL("Database=Maltesers").Options))
      {
        var _ = context.Model; // Trigger context initialization
        Assert.True(context.IsMySQLSet);
      }
    }

    [Test]
    public void IsMySQLInConstructorWhenUsingConstructor()
    {
      using (var context = new ProviderConstructorContext(
          new DbContextOptionsBuilder()
              .UseInternalServiceProvider(MySQLFixture.DefaultServiceProvider)
              .UseMySQL("Database=Maltesers").Options))
      {
        var _ = context.Model; // Trigger context initialization
        Assert.True(context.IsMySQLSet);
      }
    }

    [Test]
    public void CannotUseIsMySQLInOnConfiguringWithConstructor()
    {
      using (var context = new ProviderUseInOnConfiguringContext(
          new DbContextOptionsBuilder()
              .UseInternalServiceProvider(MySQLFixture.DefaultServiceProvider)
              .UseMySQL("Database=Maltesers").Options))
      {
        Assert.AreEqual(
            CoreStrings.RecursiveOnConfiguring,
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                  var _ = context.Model; // Trigger context initialization
                })!.Message);
      }
    }

    private class ProviderContext : DbContext
    {
      protected ProviderContext()
      {
      }

      public ProviderContext(DbContextOptions options)
          : base(options)
      {
      }

      public bool? IsMySQLSet { get; protected set; }
    }

    private class MySQLFixture : ServiceProviderFixtureBase
    {
      public static IServiceProvider DefaultServiceProvider { get; }
          = new ServiceCollection().AddEntityFrameworkMySQL().BuildServiceProvider();

      public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();
      protected override ITestStoreFactory TestStoreFactory => MySQLTestStoreFactory.Instance;
    }

    private class MySQLOnConfiguringContext : ProviderContext
    {
      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
          => optionsBuilder
              .UseInternalServiceProvider(MySQLFixture.DefaultServiceProvider)
              .UseMySQL("Database=Maltesers");
    }

    private class MySQLOnModelContext : MySQLOnConfiguringContext
    {
      protected override void OnModelCreating(ModelBuilder modelBuilder)
          => IsMySQLSet = Database.IsMySql();
    }

    private class MySQLConstructorContext : MySQLOnConfiguringContext
    {
      public MySQLConstructorContext()
          => IsMySQLSet = Database.IsMySql();
    }

    private class MySQLUseInOnConfiguringContext : MySQLOnConfiguringContext
    {
      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        base.OnConfiguring(optionsBuilder);

        IsMySQLSet = Database.IsMySql();
      }
    }

    private class ProviderOnModelContext : ProviderContext
    {
      public ProviderOnModelContext(DbContextOptions options)
          : base(options)
      {
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
          => IsMySQLSet = Database.IsMySql();
    }

    private class ProviderConstructorContext : ProviderContext
    {
      public ProviderConstructorContext(DbContextOptions options)
          : base(options)
          => IsMySQLSet = Database.IsMySql();
    }

    private class ProviderUseInOnConfiguringContext : ProviderContext
    {
      public ProviderUseInOnConfiguringContext(DbContextOptions options)
          : base(options)
      {
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
          => IsMySQLSet = Database.IsMySql();
    }
  }
}
