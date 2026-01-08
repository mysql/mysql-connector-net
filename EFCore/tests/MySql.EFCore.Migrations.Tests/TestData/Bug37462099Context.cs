// Copyright © 2025, 2026, Oracle and/or its affiliates.
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
using System;
using System.ComponentModel.DataAnnotations;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;

namespace MySql.EntityFrameworkCore.Migrations.Tests.TestData
{
  public class Bug37462099Context : DbContext
  {
    public DbSet<Bug37462099> Entity { get; set; }

    public Bug37462099Context() : base()
    {
    }

    public Bug37462099Context(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Bug37462099>(builder =>
      {
        builder.ToTable("Bug37462099");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name);

        builder.Property(p => p.Created);
      });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    => options.UseMySQL(MySQLTestStore.RootConnectionString + "database=test;");
  }

  public class Bug37462099
  {
    [Key]
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset Created { get; set; }

  }
}
