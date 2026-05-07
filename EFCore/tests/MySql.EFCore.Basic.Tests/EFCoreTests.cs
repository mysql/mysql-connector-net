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
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class EFCoreTests : SakilaLiteFixture
  {
    // Explicitly compiled query
    private static Func<SakilaLiteContext, int, Customer> _customerById =
      EF.CompileQuery((SakilaLiteContext context, int id) =>
        context.Customer.Single(p => p.CustomerId == id));

    [Test]
    public void ExplicitlyCompiledQueries()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        var customer = _customerById(context, 9);
        Assert.That(customer.CustomerId, Is.EqualTo(9));
        Assert.That(customer.LastName, Is.EqualTo("MOORE"));
      }
    }

    [Test]
    public void GraphNewAndExistingEntities()
    {
      using (SakilaLiteUpdateContext context = new SakilaLiteUpdateContext())
      {
        context.InitContext();

        Actor actorNoId, actor;
        context.Attach(actorNoId = new Actor { FirstName = "PENELOPE", LastName = "GUINESS" });
        context.Attach(actor = new Actor { ActorId = 21, FirstName = "KIRSTEN", LastName = "PALTROW" });

        var changes = context.ChangeTracker.Entries();
        Assert.That(changes.Count(), Is.EqualTo(2));
        Assert.That(changes.Select(c => c.State), Has.Exactly(1).Matches<EntityState>(state => state.Equals(EntityState.Added)));
        Assert.That(changes.Select(c => c.State), Has.Exactly(1).Matches<EntityState>(state => state.Equals(EntityState.Unchanged)));

        context.SaveChanges();

        var list = context.Actor.ToList();
        Assert.That(list.Count, Is.EqualTo(1));
      }
    }

    [Test]
    public void StringInterpolationInSqlCommands()
    {
      using (SakilaLiteUpdateContext context = new SakilaLiteUpdateContext())
      {
        context.InitContext();

        int id = 1;
        string firstName = "PENELOPE";
        string lastName = "GUINESS";
        DateTime lastUpdate = DateTime.Parse("2006-02-15 04:34:33");
        context.Database.ExecuteSqlInterpolated($"INSERT INTO actor(actor_id, first_name, last_name, last_update) VALUES ({id}, {firstName}, {lastName}, {lastUpdate:u})");
        Actor actor = context.Set<Actor>().FromSqlInterpolated($"SELECT * FROM actor WHERE actor_id={id} and last_name={lastName}").First();

        Assert.That(actor.ActorId, Is.EqualTo(id));
        Assert.That(actor.FirstName, Is.EqualTo(firstName));
      }
    }

    [Test]
    public void TransactionScopeTest()
    {
      using (var context = new SakilaLiteUpdateContext())
      {
        context.InitContext(false);
      }

      using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
      {
        using (MySqlConnection connection = new MySqlConnection(MySQLTestStore.GetContextConnectionString<SakilaLiteUpdateContext>()))
        {
          connection.Open();

          MySqlCommand command = connection.CreateCommand();
          command.CommandText = "DELETE FROM actor";
          command.ExecuteNonQuery();

          var options = new DbContextOptionsBuilder<SakilaLiteUpdateContext>()
            .UseMySQL(connection)
            .Options;

          using (TransactionScope innerScope = new TransactionScope(TransactionScopeOption.Required))
          {
            using (var context = new SakilaLiteUpdateContext(options))
            {
              context.Actor.Add(new Actor
              {
                FirstName = "PENELOPE",
                LastName = "GUINESS"
              });
              context.SaveChanges();
              innerScope.Complete();
            }
          }

          // Commit transaction if all commands succeed, transaction will auto-rollback
          // when disposed if either commands fails
          scope.Complete();
        }
      }
    }

    /// <summary>
    /// Bug #103436	SqlNullabilityProcessor error when using EF and filter by Date/Time
    /// </summary>
    [Test]
    public void QueryWithDate()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        var scenario1 = context.Actor.Where(
        w => w.FirstName == "GARY"
        && w.LastName == "PENN"
        && w.LastUpdate >= DateTime.Today.AddDays(-15));

        var res = scenario1.Count();
        Assert.That(res, Is.EqualTo(0));

        var dt = DateTime.Today.AddDays(-15);
        var scenario2 = context.Actor.Where(
        w => w.FirstName == "GARY"
        && w.LastName == "PENN"
        && w.LastUpdate >= dt);

        var res2 = scenario2.Count();
        Assert.That(res2, Is.EqualTo(0));

        var scenario3 = context.Actor.Where(
        w => w.FirstName == "GARY"
        && w.LastName == "PENN"
        && w.LastUpdate >= DateTime.Today.AddDays(3));

        var res3 = scenario3.Count();
        Assert.That(res3, Is.EqualTo(0));
      }
    }

    /// <summary>
    /// Bug#34317220 [.NET 6 - equality check on Datetime.Date throws ArgumentOutOfRangeException]
    /// </summary>
    [Test]
    public void DateTimeEqualityCheck()
    {
      using (BugContext context = new BugContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.Add(new Bug { Id = 1, CreatedDate = DateTime.Today });
        context.SaveChanges();

        var test = context.Bugs.Where(x => x.CreatedDate == DateTime.Today).ToList();
        Assert.That(test.Count, Is.EqualTo(1));
        test = context.Bugs.Where(x => x.CreatedDate.Date > DateTime.Today).ToList();
        Assert.That(test.Count, Is.EqualTo(0));
        test = context.Bugs.Where(x => x.CreatedDate.Date < DateTime.Today).ToList();
        Assert.That(test.Count, Is.EqualTo(0));
        test = context.Bugs.Where(x => x.CreatedDate.Date == DateTime.Today).ToList();
        Assert.That(test.Count, Is.EqualTo(1));
      }
    }

    /// <summary>
    /// Bug #39328452 - MySql.EntityFrameworkCore error when using Linq with .Contains in fails
    /// Verifies string array collection parameters used with Contains are translated correctly.
    /// </summary>
    [Test]
    public void StringCollectionParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        string[] lastNames = { "LOLLOBRIGIDA", "NICHOLSON" };

        var result = context.Actor
          .Where(x => lastNames.Contains(x.LastName!))
          .ToList();

        AssertLastNames(result, lastNames);
      }
    }

    /// <summary>
    /// Bug #39328452 - MySql.EntityFrameworkCore error when using Linq with .Contains in fails
    /// Verifies string list collection parameters used with Contains are translated correctly.
    /// </summary>
    [Test]
    public void StringListParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        List<string> lastNames = new List<string> { "LOLLOBRIGIDA", "NICHOLSON" };

        var result = context.Actor
          .Where(x => lastNames.Contains(x.LastName!))
          .ToList();

        AssertLastNames(result, lastNames);
      }
    }

    /// <summary>
    /// Bug #39328452 - MySql.EntityFrameworkCore error when using Linq with .Contains in fails
    /// Verifies string enumerable collection parameters used with Contains are translated correctly.
    /// </summary>
    [Test]
    public void StringEnumerableParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        IEnumerable<string> lastNames = new[] { "LOLLOBRIGIDA", "NICHOLSON" };

        var result = context.Actor
          .Where(x => lastNames.Contains(x.LastName!))
          .ToList();

        AssertLastNames(result, lastNames);
      }
    }

    /// <summary>
    /// Bug #39328452 - MySql.EntityFrameworkCore error when using Linq with .Contains in fails
    /// Verifies large string collection parameters used with Contains are not constrained by the element column size.
    /// </summary>
    [Test]
    public void LargeStringCollectionParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        string[] lastNames = Enumerable.Range(0, 40)
          .Select(i => $"NOT_MATCHING_{i:D2}")
          .Concat(new[] { "LOLLOBRIGIDA", "NICHOLSON" })
          .ToArray();

        var result = context.Actor
          .Where(x => lastNames.Contains(x.LastName!))
          .ToList();

        AssertLastNames(result, new[] { "LOLLOBRIGIDA", "NICHOLSON" });
      }
    }

    /// <summary>
    /// Bug #39328452 - MySql.EntityFrameworkCore error when using Linq with .Contains in fails
    /// Verifies numeric collection parameters used with Contains are translated correctly.
    /// </summary>
    [Test]
    public void NumericCollectionParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        short[] actorIds = { 1, 2 };

        var result = context.Actor
          .Where(x => actorIds.Contains(x.ActorId))
          .ToList();

        Assert.That(result.Select(x => x.ActorId), Is.EquivalentTo(actorIds));
      }
    }

    /// <summary>
    /// Bug #39328452 - MySql.EntityFrameworkCore error when using Linq with .Contains in fails
    /// Verifies empty collection parameters used with Contains are translated correctly.
    /// </summary>
    [Test]
    public void EmptyCollectionParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        string[] lastNames = Array.Empty<string>();

        var result = context.Actor
          .Where(x => lastNames.Contains(x.LastName!))
          .ToList();

        Assert.That(result, Is.Empty);
      }
    }

    /// <summary>
    /// Bug #39328452 - MySql.EntityFrameworkCore error when using Linq with .Contains in fails
    /// Verifies nullable collection parameters used with Contains are translated correctly.
    /// </summary>
    [Test]
    public void NullableCollectionParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        byte?[] languageIds = { null, 1 };

        var result = context.Film
          .Where(x => languageIds.Contains(x.OriginalLanguageId))
          .ToList();

        AssertContainsOnly(result.Select(x => x.OriginalLanguageId), languageIds);
      }
    }

    /// <summary>
    /// LINQ Contains with a byte array collection parameter in a predicate.
    /// This documents that byte arrays remain excluded from the generic collection mapping path
    /// while still working as byte collection parameters against byte columns.
    /// </summary>
    [Test]
    public void ByteArrayCollectionParameterContainsInPredicateSucceeds()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        byte[] languageIds = { 1 };

        var result = context.Film
          .Where(x => languageIds.Contains(x.LanguageId))
          .ToList();

        AssertContainsOnly(result.Select(x => x.LanguageId), languageIds);
      }
    }

    /// <summary>
    /// LINQ Contains with collection parameters preserves enum and value-converted enum mappings.
    /// </summary>
    [Test]
    public void EnumCollectionParameterContainsInPredicateSucceeds()
    {
      using (CollectionParameterContext context = CreateCollectionParameterContext(nameof(EnumCollectionParameterContainsInPredicateSucceeds)))
      {
        CollectionParameterStatus[] enabledStatuses = { CollectionParameterStatus.Enabled };
        CollectionParameterStatus[] disabledStatuses = { CollectionParameterStatus.Disabled };

        var enumResult = context.Entities
          .Where(x => enabledStatuses.Contains(x.Status))
          .ToList();

        var convertedResult = context.Entities
          .Where(x => disabledStatuses.Contains(x.ConvertedStatus))
          .ToList();

        Assert.That(enumResult.Select(x => x.Status), Is.EquivalentTo(enabledStatuses));
        Assert.That(convertedResult.Select(x => x.ConvertedStatus), Is.EquivalentTo(disabledStatuses));
      }
    }

    /// <summary>
    /// Bug #39328452 - LINQ collection parameter mapping should not enable primitive collection properties.
    /// </summary>
    [Test]
    public void PrimitiveCollectionPropertyDoesNotUseCollectionParameterMapping()
    {
      using (PrimitiveCollectionPropertyContext context = new PrimitiveCollectionPropertyContext())
      {
        var exception = Assert.Throws<InvalidOperationException>(() => _ = context.Model);

        Assert.That(exception!.Message, Does.Contain(nameof(PrimitiveCollectionEntity.Tags)));
      }
    }

    /// <summary>
    /// Verifies that the actor query returned exactly the expected last names.
    /// </summary>
    private static void AssertLastNames(IEnumerable<Actor> actors, IEnumerable<string> expectedLastNames)
    {
      Assert.That(actors.Select(x => x.LastName), Is.EquivalentTo(expectedLastNames));
    }

    /// <summary>
    /// Verifies that a non-empty result contains only values from the expected collection.
    /// </summary>
    private static void AssertContainsOnly<T>(IEnumerable<T> actualValues, IEnumerable<T> expectedValues)
    {
      T[] values = actualValues.ToArray();
      Assert.That(values, Is.Not.Empty);
      Assert.That(values, Is.All.Matches<T>(x => expectedValues.Contains(x)));
    }

    /// <summary>
    /// Entity used to verify that collection parameter mapping does not enable primitive collection properties.
    /// </summary>
    public class PrimitiveCollectionEntity
    {
      /// <summary>
      /// Gets or sets the entity identifier.
      /// </summary>
      public int Id { get; set; }

      /// <summary>
      /// Gets or sets the primitive collection property.
      /// </summary>
      public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Context used to verify primitive collection property mapping behavior.
    /// </summary>
    public class PrimitiveCollectionPropertyContext : DbContext
    {
      /// <summary>
      /// Gets or sets the entities with primitive collection properties.
      /// </summary>
      public DbSet<PrimitiveCollectionEntity> Entities { get; set; }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        if (!optionsBuilder.IsConfigured
          || optionsBuilder.Options.FindExtension<MySQLOptionsExtension>() == null)
        {
          optionsBuilder.UseMySQL(MySQLTestStore.GetContextConnectionStringWithName("PrimitiveCollectionProperty"));
        }
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        modelBuilder.Entity<PrimitiveCollectionEntity>()
          .Property(e => e.Tags);
      }
    }

    /// <summary>
    /// Creates a seeded context for collection parameter mapping tests.
    /// </summary>
    private static CollectionParameterContext CreateCollectionParameterContext(string databaseName)
    {
      CollectionParameterContext context = new CollectionParameterContext(databaseName);
      context.Database.EnsureDeleted();
      context.Database.EnsureCreated();
      context.Entities.AddRange(
        new CollectionParameterEntity
        {
          Id = 1,
          Status = CollectionParameterStatus.Enabled,
          ConvertedStatus = CollectionParameterStatus.Enabled
        },
        new CollectionParameterEntity
        {
          Id = 2,
          Status = CollectionParameterStatus.Disabled,
          ConvertedStatus = CollectionParameterStatus.Disabled
        });
      context.SaveChanges();

      return context;
    }

    /// <summary>
    /// Status values used by collection parameter mapping tests.
    /// </summary>
    public enum CollectionParameterStatus
    {
      Enabled = 1,
      Disabled = 2
    }

    /// <summary>
    /// Entity used by collection parameter mapping tests.
    /// </summary>
    public class CollectionParameterEntity
    {
      /// <summary>
      /// Gets or sets the entity identifier.
      /// </summary>
      public int Id { get; set; }

      /// <summary>
      /// Gets or sets the enum status.
      /// </summary>
      public CollectionParameterStatus Status { get; set; }

      /// <summary>
      /// Gets or sets the value-converted enum status.
      /// </summary>
      public CollectionParameterStatus ConvertedStatus { get; set; }
    }

    /// <summary>
    /// Context used by collection parameter mapping tests.
    /// </summary>
    public class CollectionParameterContext : DbContext
    {
      private readonly string _databaseName;

      /// <summary>
      /// Initializes a new instance of the <see cref="CollectionParameterContext"/> class.
      /// </summary>
      /// <param name="databaseName">The database name for the test context.</param>
      public CollectionParameterContext(string databaseName)
      {
        _databaseName = databaseName;
      }

      /// <summary>
      /// Gets or sets the collection parameter test entities.
      /// </summary>
      public DbSet<CollectionParameterEntity> Entities { get; set; }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        if (!optionsBuilder.IsConfigured
          || optionsBuilder.Options.FindExtension<MySQLOptionsExtension>() == null)
        {
          optionsBuilder.UseMySQL(MySQLTestStore.GetContextConnectionStringWithName(_databaseName));
        }
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        modelBuilder.Entity<CollectionParameterEntity>()
          .Property(e => e.ConvertedStatus)
          .HasConversion<string>()
          .HasColumnType("varchar(16)");
      }
    }

    /// <summary>
    /// Bug#35392218 [bugfix schema name (specially usefull when use multiple schema..]
    /// </summary>
    [Test]
    public void MultipleSchemaContext()
    {
      try
      {
        using (Bug35392218Context context = new Bug35392218Context())
        {
          context.Database.EnsureDeleted();
          context.Database.EnsureCreated();

          context.Add(new Bug35392218_1 { Id = 1, Name = "John Doe" });
          context.Add(new Bug35392218_2 { Id = 1, Name = "John Doe" });
          context.SaveChanges();

          using (MySqlConnection connection = new MySqlConnection(MySQLTestStore.BaseConnectionString))
          {
            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SHOW DATABASES LIKE '%bug35392218%';";

            MySqlDataReader reader = command.ExecuteReader();
            int rowCount = 0;
            while (reader.Read())
            {
              rowCount++;
            }

            //Checking if 2 databases related to the bug number are created in the server;
            Assert.That(rowCount, Is.EqualTo(2));
            reader.Close();
          }

          using (MySqlConnection connection = new MySqlConnection(MySQLTestStore.BaseConnectionString + "database=schema_bug35392218_1;"))
          {
            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM bug35392218_table1";

            MySqlDataReader reader = command.ExecuteReader();
            int rowCount = 0;
            while (reader.Read())
            {
              rowCount++;
            }

            //Checking if the data is available in 'schemaBug35392218_1' as its the default database definded in the context configuration.
            Assert.That(rowCount, Is.EqualTo(1));
            Assert.That(reader.GetString(1), Is.EqualTo("John Doe"));

            reader.Close();
          }

          using (MySqlConnection connection = new MySqlConnection(MySQLTestStore.BaseConnectionString + "database=schema_bug35392218_2;"))
          {
            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM bug35392218_table2";

            MySqlDataReader reader = command.ExecuteReader();
            int rowCount = 0;
            while (reader.Read())
            {
              rowCount++;
            }

            //Checking if the same data is also inserted into 'schemaBug35392218_2' which is the other database defined in model creation.
            Assert.That(rowCount, Is.EqualTo(1));
            Assert.That(reader.GetString(1), Is.EqualTo("John Doe"));

            reader.Close();
          }
        }
      }
      finally
      {
        using (MySqlConnection connection = new MySqlConnection(MySQLTestStore.BaseConnectionString))
        {
          connection.Open();

          MySqlCommand command = connection.CreateCommand();
          command.CommandText = "DROP DATABASE IF EXISTS schema_bug35392218_2;DROP DATABASE IF EXISTS schema_bug35392218_1;";
          command.ExecuteNonQuery();
        }
      }
    }
  }
}
