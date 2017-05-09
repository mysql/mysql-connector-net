// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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

using System.Linq;
using Xunit;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace MySql.Data.Entity.Tests
{

  public class DeleteTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public DeleteTests(DefaultFixture fixture)
    {
      st = fixture;
      if (st.Setup(this.GetType()))
        LoadData();
    }

    void LoadData()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        ctx.Products.Add(new Product() { Name = "Garbage Truck", MinAge = 8 });
        ctx.Products.Add(new Product() { Name = "Fire Truck", MinAge = 12 });
        ctx.Products.Add(new Product() { Name = "Hula Hoop", MinAge = 18 });
        ctx.SaveChanges();
      }
    }

    [Fact]
    public void SimpleDeleteAllRows()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        Assert.True(ctx.Products.Count() > 0);

        foreach (Product p in ctx.Products)
          ctx.Products.Remove(p);
        ctx.SaveChanges();

        Assert.Equal(0, ctx.Products.Count());
      }
      // set the flag that will cause the setup to happen again
      // since we just blew away a table
      st.NeedSetup = true;
    }

    [Fact]
    public void SimpleDeleteRowByParameter()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        int total = ctx.Products.Count();
        int cntLeft = ctx.Products.Where(b => b.MinAge >= 18).Count();
        // make sure the test is valid
        Assert.True(total > cntLeft);

        foreach (Product p in ctx.Products.Where(b => b.MinAge < 18).ToList())
          ctx.Products.Remove(p);
        ctx.SaveChanges();
        Assert.Equal(cntLeft, ctx.Products.Count());
        st.NeedSetup = true;
      }
    }


    public class Widget
    {
      public int Id { get; set;  }
      public WidgetDetail Detail { get; set; }
    }

    public class WidgetDetail
    {
      public int Id { get; set; }
      public Widget Widget { get; set; }
    }

    public class WidgetContext : DbContext
    {
      public WidgetContext(string connStr) : base(connStr)
      {
        Database.SetInitializer<WidgetContext>(null);
      }

      protected override void OnModelCreating(DbModelBuilder modelBuilder)
      {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<WidgetDetail>()
          .HasRequired(b => b.Widget)
          .WithOptional(a => a.Detail)
          .WillCascadeOnDelete(true);
      }
    }

    /// <summary>
    /// Fix for bug Cascading delete using CreateDatabase in Entity Framework
    /// (http://bugs.mysql.com/bug.php?id=64779) using ModelFirst.
    /// </summary>
    [Fact]
    public void XOnDeleteCascade()
    {
      using (WidgetContext ctx = new WidgetContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        var sql = context.CreateDatabaseScript();
        st.CheckSqlContains(sql,
          @"ALTER TABLE `WidgetDetails` ADD CONSTRAINT WidgetDetail_Widget
	          FOREIGN KEY (Id)	REFERENCES `Widgets` (Id) ON DELETE Cascade ON UPDATE NO ACTION;");
      }
    }
  }
}
