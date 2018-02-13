// Copyright (c) 2013, 2017, Oracle and/or its affiliates. All rights reserved.
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


using System.Data;
using System.Linq;
using Xunit;

namespace MySql.Data.EntityFramework.Tests
{
  public class OrderingAndGrouping : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public OrderingAndGrouping(DefaultFixture fixture)
    {
      st = fixture;
      st.Setup(this.GetType());
    }

    [Fact]
    public void OrderBySimple()
    {
      st.TestESql<Book>(
        "SELECT VALUE b FROM Books AS b ORDER BY b.Pages",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
          `Extent1`.`Author_Id` FROM `Books` AS `Extent1` ORDER BY `Extent1`.`Pages` ASC");
    }

    [Fact]
    public void OrderByWithPredicate()
    {
      st.TestESql<Book>(
        "SELECT VALUE b FROM Books AS b WHERE b.Pages > 200 ORDER BY b.Pages",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, 
        `Extent1`.`Author_Id` FROM `Books` AS `Extent1` WHERE `Extent1`.`Pages` > 200
        ORDER BY `Extent1`.`Pages` ASC");
    }

    [Fact]
    public void CanGroupBySingleColumn()
    {
      using (DefaultContext ctx = st.GetDefaultContext())
      {
        var authors = from a in ctx.Authors
                      group a by a.Age into cgroup
                      select new
                      {
                        Name = cgroup.Key,
                        Count = cgroup.Count()
                      };
        string sql = authors.ToString();
        st.CheckSql(sql,
          @"SELECT `GroupBy1`.`K1` AS `Age`,  `GroupBy1`.`A1` AS `C1` FROM (SELECT
          `Extent1`.`Age` AS `K1`, COUNT(1) AS `A1` FROM `Authors` AS `Extent1`
          GROUP BY `Extent1`.`Age`) AS `GroupBy1`");
      }
    }

    [Fact]
    public void CanGroupByMultipleColumns()
    {
      using (DefaultContext ctx = st.GetDefaultContext())
      {
        var authors = from a in ctx.Authors
                        group a by new { a.Age, a.Name } into cgroup
                        select new
                        {
                          Name = cgroup.Key.Name,
                          Count = cgroup.Count()
                        };

        string sql = authors.ToString();
        st.CheckSql(sql,
          @"SELECT `GroupBy1`.`K2` AS `Age`, `GroupBy1`.`K1` AS `Name`, `GroupBy1`.`A1` AS `C1`
            FROM (SELECT `Extent1`.`Name` AS `K1`, `Extent1`.`Age` AS `K2`, COUNT(1) AS `A1`
            FROM `Authors` AS `Extent1` GROUP BY `Extent1`.`Name`, `Extent1`.`Age`) AS `GroupBy1`");
      }
    }

    [Fact(Skip ="Fix Me")]
    public void OrdersTableDoesNotProvokeSyntaxError()
    {
      //using (model2Entities context = new model2Entities())
      //{
      //  var customers = from c in context.customer
      //                  select c;
      //  customers.ToList().ForEach(c => c.order.Load());
      //}
    }
  }
}