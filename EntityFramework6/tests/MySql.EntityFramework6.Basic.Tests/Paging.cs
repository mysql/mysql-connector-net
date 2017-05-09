// Copyright � 2014 Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class Paging : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public Paging(DefaultFixture fixture)
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
    public void Take()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var q = ctx.Books.Take(2);
        var sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Id`, `Name`, `PubDate`, `Pages`, `Author_Id` FROM `Books` LIMIT 2");
      }
    }

    [Fact]
    public void Skip()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var q = ctx.Books.OrderBy(b=>b.Pages).Skip(3);
        var sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, `Extent1`.`Author_Id`
            FROM `Books` AS `Extent1` ORDER BY `Extent1`.`Pages` ASC LIMIT 3,18446744073709551615");
      }
    }

    [Fact]
    public void SkipAndTakeSimple()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var q = ctx.Books.OrderBy(b => b.Pages).Skip(3).Take(4);
        var sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, `Extent1`.`Author_Id`
            FROM `Books` AS `Extent1` ORDER BY `Extent1`.`Pages` ASC LIMIT 3,4");
      }
    }

    // <summary>
    // Tests fix for bug #64749 - Entity Framework - Take().Count() fails with EntityCommandCompilationException.
    // </summary>
    [Fact]
    public void TakeWithCount()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        int cnt = ctx.Products.Take(2).Count();
        Assert.Equal(2, cnt);
      }
    }
  }
}