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
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using NUnit.Framework;
using System;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class BasicGuidTests
  {
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using (var context = new ContextGUID())
        context.Database.EnsureDeleted();
    }

    /// <summary>
    /// Bug #32173133	CAN'T FILTER WITH CONTAINS IN ENTITYFRAMEWORKCORE FOR GUID OR BYTE[] FIELDS
    /// </summary>

    [Test]
    public void TestEmptyGUID()
    {
      MySQLTestStore.Execute("drop database if exists DbContextGuid; Create database DbContextGuid; ");
      using (var context = new ContextGUID())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var filter = new[] { Guid.Empty };
        var resultFilter = context.Guidtable.Where(t => filter.Contains(t.Uuid)).ToArray();
        Assert.IsNotNull(resultFilter);
        Random rnd = new Random();
        var guid = Guid.NewGuid();
        var record = new GuidTable { Id = rnd.Next(100), Uuid = guid };
        context.Guidtable.Add(record);
        context.SaveChanges();
        var rows = context.Guidtable.Count();
        Assert.AreEqual(1, rows);
        filter[0] = guid;
        var resultFilter2 = context.Guidtable.Where(t => filter.Contains(t.Uuid)).ToArray();
        Assert.AreEqual(1, resultFilter2.Count());
      }
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
  }
}
