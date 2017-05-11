// Copyright � 2013, 2017 Oracle and/or its affiliates. All rights reserved.
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

using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class SetOperators : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public SetOperators(DefaultFixture fixture)
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
    public void Any()
    {
      // find all authors that are in our db with no books
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var q = from a in ctx.Authors where !a.Books.Any() select a;
        string sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`Age`, `Extent1`.`Address_City`, `Extent1`.`Address_Street`, 
          `Extent1`.`Address_State`, `Extent1`.`Address_ZipCode` FROM `Authors` AS `Extent1` WHERE NOT EXISTS(SELECT
          1 AS `C1` FROM `Books` AS `Extent2` WHERE `Extent1`.`Id` = `Extent2`.`Author_Id`)");
      }
    }

    [Fact]
    public void FirstSimple()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM products", st.Connection);
      int id = (int)cmd.ExecuteScalar();

      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var q = from p in ctx.Products
                select p;
        Product product = q.First() as Product;

        Assert.Equal(id, product.Id);
      }
    }

    [Fact]
    public void FirstPredicate()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM products WHERE minage > 8", st.Connection);
      int id = (int)cmd.ExecuteScalar();

      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var q = from p in ctx.Products
                where p.MinAge > 8
                select p;
        Product product = q.First() as Product;
        Assert.Equal(id, product.Id);
      }
    }
  }
}