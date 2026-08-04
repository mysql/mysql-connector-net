// Copyright © 2021, 2026, Oracle and/or its affiliates.
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
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  /// <summary>
  /// Tests GUID mappings and collection query translation.
  /// </summary>
  public class GuidTests
  {
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using (var context = new ContextGUID())
        context.Database.EnsureDeleted();

      using (var context = new ContextGUIDChar())
        context.Database.EnsureDeleted();
    }

    /// <summary>
    /// Bug #32173133	CAN'T FILTER WITH CONTAINS IN ENTITYFRAMEWORKCORE FOR GUID OR BYTE[] FIELDS
    /// </summary>

    [Test]
    public void TestEmptyGUID()
    {
      MySQLTestStore.Execute("DROP DATABASE IF EXISTS DbContextGuid; CREATE DATABASE DbContextGuid;");
      using (var context = new ContextGUID())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var filter = new[] { Guid.Empty };
        var resultFilter = context.Guidtable.Where(t => filter.Contains(t.Uuid)).ToArray();
        Assert.That(resultFilter, Is.Not.Null);
        Random rnd = new Random();
        var guid = Guid.NewGuid();
        var record = new GuidTable { Id = rnd.Next(100), Uuid = guid };
        context.Guidtable.Add(record);
        context.SaveChanges();
        var rows = context.Guidtable.Count();
        Assert.That(rows, Is.EqualTo(1));
        filter[0] = guid;
        var resultFilter2 = context.Guidtable.Where(t => filter.Contains(t.Uuid)).ToArray();
        Assert.That(resultFilter2.Count(), Is.EqualTo(1));
      }
    }

    /// <summary>
    /// Guid collection parameters preserve the provider's char GUID mapping.
    /// </summary>
    [Test]
    public void GuidArrayParameterContainsWithDefaultGuidFormat()
    {
      MySQLTestStore.Execute("DROP DATABASE IF EXISTS DbContextGuidChar; CREATE DATABASE DbContextGuidChar;");
      using (var context = new ContextGUIDChar())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var guid = Guid.NewGuid();
        var filter = new[] { guid };
        var record = new GuidTable { Id = 1, Uuid = guid };
        context.Guidtable.Add(record);
        context.SaveChanges();

        var resultFilter = context.Guidtable.Where(t => filter.Contains(t.Uuid)).ToArray();

        Assert.That(resultFilter.Count(), Is.EqualTo(1));
      }
    }

    /// <summary>
    /// Bug#39825453 [Bug #119461 fix incomplete: .Contains() on a Guid[] array still throws NullReferenceException]
    /// Verifies that Guid arrays use a collection parameter mapping when generating SQL.
    /// </summary>
    [Test]
    public void GuidArrayContainsGeneratesSql()
    {
      var guids = new[] { Guid.NewGuid(), Guid.NewGuid() };

      AssertContainsGeneratesSql(context => context.Entities.Where(entity => guids.Contains(entity.ExternalId)), guids);
    }

    /// <summary>
    /// Bug#39825453 [Bug #119461 fix incomplete: .Contains() on a Guid[] array still throws NullReferenceException]
    /// Verifies that Guid arrays wrapped by EF.Constant use a collection parameter mapping when generating SQL.
    /// </summary>
    [Test]
    public void GuidArrayConstantContainsGeneratesSql()
    {
      var guids = new[] { Guid.NewGuid(), Guid.NewGuid() };

      AssertContainsGeneratesSql(context => context.Entities.Where(entity => EF.Constant(guids).Contains(entity.ExternalId)), guids);
    }

    /// <summary>
    /// Verifies that GUID lists use a collection parameter mapping when generating SQL.
    /// </summary>
    [Test]
    public void GuidListContainsGeneratesSql()
    {
      var guids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

      AssertContainsGeneratesSql(context => context.Entities.Where(entity => guids.Contains(entity.ExternalId)), guids);
    }

    /// <summary>
    /// Verifies that GUID hash sets use a collection parameter mapping when generating SQL.
    /// </summary>
    [Test]
    public void GuidHashSetContainsGeneratesSql()
    {
      var guids = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid() };

      AssertContainsGeneratesSql(context => context.Entities.Where(entity => guids.Contains(entity.ExternalId)), guids);
    }

    /// <summary>
    /// Generates SQL for a collection parameter query and verifies the WHERE clause uses an IN predicate containing the expected GUIDs.
    /// </summary>
    /// <param name="queryFactory">Creates the query to validate.</param>
    /// <param name="guids">The GUIDs expected in the generated SQL.</param>
    private static void AssertContainsGeneratesSql(Func<CollectionParameterContext, IQueryable<CollectionParameterEntity>> queryFactory, IEnumerable<Guid> guids)
    {
      using (var context = new CollectionParameterContext())
      {
        var sql = queryFactory(context).ToQueryString();

        Assert.That(sql, Does.Match(@"(?s)\bWHERE\b.*?\bIN\s*\("));
        foreach (var guid in guids)
          Assert.That(sql, Does.Contain(guid.ToString()));
      }
    }

    /// <summary>
    /// Defines the minimal model used to test collection query translation.
    /// </summary>
    private sealed class CollectionParameterContext : DbContext
    {
      /// <summary>
      /// Gets the entity set used by the test.
      /// </summary>
      public DbSet<CollectionParameterEntity> Entities => Set<CollectionParameterEntity>();

      /// <summary>
      /// Configures the MySQL provider without opening a database connection.
      /// </summary>
      /// <param name="optionsBuilder">The context options builder.</param>
      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySQL("Server=localhost;Database=collection_parameter;Uid=test;Pwd=test;");
    }

    /// <summary>
    /// Represents an entity with collection-query properties.
    /// </summary>
    private sealed class CollectionParameterEntity
    {
      /// <summary>
      /// Gets or sets the primary key.
      /// </summary>
      public int Id { get; set; }

      /// <summary>
      /// Gets or sets the external identifier.
      /// </summary>
      public Guid ExternalId { get; set; }
    }

    public class GuidTable
    {
      public int Id { get; set; }
      public Guid Uuid { get; set; }
    }

    public class ContextGUID : DbContext
    {
      public DbSet<GuidTable> Guidtable { get; set; }
      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        // get the class name of the caller to get a unique name for the database
        if (!optionsBuilder.IsConfigured
          || optionsBuilder.Options.FindExtension<MySQLOptionsExtension>() == null)
        {
          optionsBuilder.UseMySQL($"server=localhost;user id=root;password=;port={MySQLTestStore.Port()};OldGuids=true;pooling = false;database=DbContextGuid;defaultcommandtimeout=50;");
        }
      }
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        modelBuilder.Entity<GuidTable>(entity =>
        {
          entity.HasKey(e => e.Id);

          entity.ToTable("GuidTable");

          entity.Property(e => e.Id)
              .HasColumnName("id")
              .HasColumnType("int");

          entity.Property(e => e.Uuid)
              .HasColumnName("uuid")
              .HasColumnType("binary(16)");
        });
      }
    }

    /// <summary>
    /// Context used to verify GUID values mapped with the default char GUID format.
    /// </summary>
    public class ContextGUIDChar : DbContext
    {
      /// <summary>
      /// Gets or sets the GUID table mapped with the default char GUID format.
      /// </summary>
      public DbSet<GuidTable> Guidtable { get; set; }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        // get the class name of the caller to get a unique name for the database
        if (!optionsBuilder.IsConfigured
          || optionsBuilder.Options.FindExtension<MySQLOptionsExtension>() == null)
        {
          optionsBuilder.UseMySQL($"server=localhost;user id=root;password=;port={MySQLTestStore.Port()};pooling = false;database=DbContextGuidChar;defaultcommandtimeout=50;");
        }
      }
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        modelBuilder.Entity<GuidTable>(entity =>
        {
          entity.HasKey(e => e.Id);

          entity.ToTable("GuidTable");

          entity.Property(e => e.Id)
              .HasColumnName("id")
              .HasColumnType("int");

          entity.Property(e => e.Uuid)
              .HasColumnName("uuid")
              .HasColumnType("char(36)");
        });
      }
    }
  }
}
