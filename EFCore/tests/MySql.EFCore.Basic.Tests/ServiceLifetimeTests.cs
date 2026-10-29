// Copyright © 2026, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Update;
using NUnit.Framework;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class ServiceLifetimeTests
  {
    private class ServiceLifetimeContext : DbContext
    {
      public ServiceLifetimeContext(DbContextOptions<ServiceLifetimeContext> options)
        : base(options)
      {
      }

      public DbSet<Person> People => Set<Person>();
    }

    // ASP.NET Core validates scopes in development, as do EF Core's own test fixtures.
    private static ServiceProvider BuildValidatingProvider(IServiceCollection services)
      => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

    [Test]
    public void UpdateSqlGeneratorResolvesFromAScopeValidatingProvider()
    {
      using var serviceProvider = BuildValidatingProvider(new ServiceCollection().AddEntityFrameworkMySQL());
      using var scope = serviceProvider.CreateScope();

      var generator = scope.ServiceProvider.GetRequiredService<IUpdateSqlGenerator>();

      Assert.That(generator, Is.InstanceOf<MySQLUpdateSqlGenerator>());
      Assert.That(serviceProvider.GetRequiredService<IUpdateSqlGenerator>(), Is.SameAs(generator));
    }

    [Test]
    public void ProviderSpecificUpdateSqlGeneratorIsASingletonLikeIUpdateSqlGenerator()
    {
      var services = new ServiceCollection().AddEntityFrameworkMySQL();

      Assert.That(services.Single(d => d.ServiceType == typeof(IUpdateSqlGenerator)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
      Assert.That(services.Single(d => d.ServiceType == typeof(IMySQLUpdateSqlGenerator)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void SaveChangesWorksWithAScopeValidatingInternalServiceProvider()
    {
      var services = new ServiceCollection().AddEntityFrameworkMySQL();
      services.AddDbContext<ServiceLifetimeContext>((sp, options) => options
        .UseMySQL(MySQLTestStore.GetContextConnectionString<ServiceLifetimeContext>())
        .UseInternalServiceProvider(sp));
      using var serviceProvider = BuildValidatingProvider(services);

      using (var scope = serviceProvider.CreateScope())
      {
        var context = scope.ServiceProvider.GetRequiredService<ServiceLifetimeContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.People.Add(new Person { Name = "John Doe", PersonNumber = "1", PhoneNumber = "2" });
        context.SaveChanges();
      }

      using (var scope = serviceProvider.CreateScope())
      {
        var context = scope.ServiceProvider.GetRequiredService<ServiceLifetimeContext>();
        Assert.That(context.People.Single().Name, Is.EqualTo("John Doe"));
        context.Database.EnsureDeleted();
      }
    }
  }
}
