// Copyright © 2011, 2013, Oracle and/or its affiliates. All rights reserved.
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

using System;
using Xunit;
using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data;

namespace MySql.Data.Entity.Tests
{
  public class Widget
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime DateCreated { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime DateTimeWithPrecision { get; set; }
  }

  public class TestContext : DbContext
  {
    public TestContext(string connStr) : base(connStr)
    {
      Database.SetInitializer<TestContext>(null);
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Entity<Widget>()
                      .Property(e => e.DateTimeWithPrecision)
                      .HasPrecision(6);
    }

    public DbSet<Widget> Widgets { get; set; }
  }

  public class DatesTypesTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public DatesTypesTests(DefaultFixture fixture)
    {
      st = fixture;
      st.Setup(this.GetType());
    }

    [Fact]
    public void CanCreateDBScriptWithDateTimePrecision()
    {
      if (st.Version < new Version(5, 6, 5)) return;

      using (var ctx = new TestContext(st.ConnectionString))
      {
        var script = new MySqlScript(st.Connection);
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        script.Query = context.CreateDatabaseScript();
        script.Execute();

        DataTable schema = st.Connection.GetSchema("COLUMNS", new string[] { null, st.Connection.Database, "widgets" });

        DataRow row = schema.Rows[3];
        Assert.Equal("datetime", row.Field<string>("DATA_TYPE"));
        Assert.Equal((ulong)6, row.Field<UInt64>("DATETIME_PRECISION"));
        Assert.Equal("NO", row.Field<string>("IS_NULLABLE"));
      }
    }
  }
}
