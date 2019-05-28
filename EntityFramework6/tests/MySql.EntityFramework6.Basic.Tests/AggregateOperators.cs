// Copyright (c) 2013, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Data.Common;
using System.Data;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class AggregateOperators : IClassFixture<DefaultFixture>
  {
    private DefaultFixture _fixture;

    public AggregateOperators(DefaultFixture fixture)
    {
      _fixture = fixture;
      _fixture.Setup(this.GetType());
    }

    [Fact]
    public void CountSimple()
    {
      _fixture.TestESql<Int32>(
        "SELECT VALUE Count(b.Id) FROM Books as b",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM(SELECT COUNT(`Extent1`.`Id`) AS `A1`
                         FROM `Books` AS `Extent1`) AS `GroupBy1`");
    }

    [Fact]
    public void BigCountSimple()
    {
      _fixture.TestESql<long>(
        "SELECT VALUE BigCount(b.Id) FROM Books as b",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT COUNT(`Extent1`.`Id`) AS `A1`
                            FROM `Books` AS `Extent1`) AS `GroupBy1`");
    }

    [Fact]
    public void CountWithPredicate()
    {
      _fixture.TestESql<Int32>("SELECT VALUE Count(b.Id) FROM Books AS b WHERE b.Pages > 3",
                  @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT COUNT(`Extent1`.`Id`) AS `A1` 
                            FROM `Books` AS `Extent1` WHERE `Extent1`.`Pages` > 3) AS `GroupBy1`");
    }

    [Fact]
    public void MinSimple()
    {
      _fixture.TestESql<Int32>(
        "SELECT VALUE MIN(b.Pages) FROM Books AS b",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT MIN(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1`) AS `GroupBy1`");
    }

    [Fact]
    public void MinWithPredicate()
    {
      _fixture.TestESql<DbDataRecord>(
        "SELECT Min(b.Pages) FROM Books AS b WHERE b.Author.Age > 50",
        @"SELECT 1 AS `C1`, `GroupBy1`.`A1` AS `C2` FROM(SELECT MIN(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1` INNER JOIN `Authors` AS `Extent2` ON `Extent1`.`Author_Id` = `Extent2`.`Id`
          WHERE `Extent2`.`Age` > 50) AS `GroupBy1`");
    }

    [Fact]
    public void MinWithGrouping()
    {
      _fixture.TestESql<Int32>(
        "SELECT VALUE Min(b.Pages) FROM Books AS b GROUP BY b.Author.Id",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT `Extent1`.`Author_Id` AS `K1`, 
          MIN(`Extent1`.`Pages`) AS `A1` FROM `Books` AS `Extent1`  GROUP BY 
          `Extent1`.`Author_Id`) AS `GroupBy1`"
        );
    }

    [Fact]
    public void MaxSimple()
    {
      _fixture.TestESql<Int32>(
        "SELECT VALUE MAX(b.Pages) FROM Books AS b",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT MAX(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1`) AS `GroupBy1`");
    }

    [Fact]
    public void MaxWithPredicate()
    {
      _fixture.TestESql<DbDataRecord>(
        "SELECT MAX(b.Pages) FROM Books AS b WHERE b.Author.Id=2",
        @"SELECT 1 AS `C1`, `GroupBy1`.`A1` AS `C2` FROM (SELECT MAX(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1` WHERE `Extent1`.`Author_Id` = 2) AS `GroupBy1`");
    }

    [Fact]
    public void MaxWithGrouping()
    {
      _fixture.TestESql<Int32>(
        "SELECT VALUE MAX(b.Pages) FROM Books AS b GROUP BY b.Author.Id",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT `Extent1`.`Author_Id` AS `K1`,  MAX(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1` GROUP BY `Extent1`.`Author_Id`) AS `GroupBy1`");
    }

    [Fact]
    public void AverageSimple()
    {
      _fixture.TestESql<Decimal>(
        "SELECT VALUE Avg(b.Pages) FROM Books AS b",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT AVG(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1`) AS `GroupBy1`");
    }

    [Fact]
    public void AverageWithPredicate()
    {
      _fixture.TestESql<Decimal>(
        "SELECT VALUE AVG(b.Pages) FROM Books AS b WHERE b.Author.Id = 3",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT AVG(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1` WHERE `Extent1`.`Author_Id` = 3) AS `GroupBy1`");
    }

    [Fact]
    public void AverageWithGrouping()
    {
      _fixture.TestESql<DbDataRecord>(
        "SELECT AVG(b.Pages) FROM Books AS b GROUP BY b.Author.Id",
        @"SELECT 1 AS `C1`, `GroupBy1`.`A1` AS `C2` FROM (SELECT `Extent1`.`Author_Id` AS `K1`, 
          AVG(`Extent1`.`Pages`) AS `A1` FROM `Books` AS `Extent1` GROUP BY `Extent1`.`Author_Id`) AS `GroupBy1`");
    }

    [Fact]
    public void SumSimple()
    {
      _fixture.TestESql<Int32>(
        "SELECT VALUE SUM(b.Pages) FROM Books AS b",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT SUM(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1`) AS `GroupBy1`");
    }

    [Fact]
    public void SumWithPredicate()
    {
      _fixture.TestESql<Double>(
        "SELECT VALUE SUM(b.Pages) FROM Books AS b WHERE b.Author.Id = 3",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT SUM(`Extent1`.`Pages`) AS `A1`
          FROM `Books` AS `Extent1` WHERE `Extent1`.`Author_Id` = 3) AS `GroupBy1`");
    }

    [Fact]
    public void SumWithGrouping()
    {
      _fixture.TestESql<Double>(
        "SELECT VALUE SUM(b.Pages) FROM Books AS b GROUP BY b.Author.Id",
        @"SELECT `GroupBy1`.`A1` AS `C1` FROM (SELECT `Extent1`.`Author_Id` AS `K1`, 
          SUM(`Extent1`.`Pages`) AS `A1` FROM `Books` AS `Extent1`  GROUP BY 
          `Extent1`.`Author_Id`) AS `GroupBy1`");
    }

    [Fact]
    public void MaxInSubQuery1()
    {
      _fixture.TestESql<Book>(
        "SELECT VALUE a FROM Authors AS a WHERE a.Id = MAX(SELECT VALUE b.Author.Id FROM Books AS b)",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`Age`, `Extent1`.`Address_City`, `Extent1`.`Address_Street`, 
          `Extent1`.`Address_State`, `Extent1`.`Address_ZipCode` FROM `Authors` AS `Extent1` INNER JOIN (SELECT
          MAX(`Extent2`.`Author_Id`) AS `A1` FROM `Books` AS `Extent2`) AS `GroupBy1` ON `Extent1`.`Id` = `GroupBy1`.`A1`");
    }

    [Fact]
    public void MaxInSubQuery2()
    {
      _fixture.TestESql<Book>(
        "SELECT VALUE a FROM Authors AS a WHERE a.Id = ANYELEMENT(SELECT VALUE MAX(b.Author.Id) FROM Books AS b)",
        @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`Age`, `Extent1`.`Address_City`, `Extent1`.`Address_Street`, 
          `Extent1`.`Address_State`, `Extent1`.`Address_ZipCode` FROM `Authors` AS `Extent1` INNER JOIN (SELECT
          MAX(`Extent2`.`Author_Id`) AS `A1` FROM `Books` AS `Extent2`) AS `GroupBy1` ON `Extent1`.`Id` = `GroupBy1`.`A1`");
    }

    /// <summary>
    /// This test the fix for bug 67377.
    /// </summary>
    [Fact]
    public void FirstOrDefaultNested()
    {
      using (DefaultContext ctx = _fixture.GetDefaultContext())
      {
        var q = ctx.Authors.Where(p => p.Id == 1).Select(p => new { AuthorId = p.Id, FirstBook = (int?)p.Books.FirstOrDefault().Id });
        var s = q.ToString();
        _fixture.CheckSql(q.ToString(),
          @"SELECT `Extent1`.`Id`, (SELECT `Extent2`.`Id` FROM `Books` AS `Extent2` WHERE `Extent1`.`Id` = `Extent2`.`Author_Id` LIMIT 1) AS `C1`
            FROM `Authors` AS `Extent1` WHERE 1 = `Extent1`.`Id`");
      }
    }
  }
}
