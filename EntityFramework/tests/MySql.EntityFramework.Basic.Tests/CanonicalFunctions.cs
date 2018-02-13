// Copyright © 2013, 2017, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Linq;
using Xunit;
using System.Data.Common;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace MySql.Data.EntityFramework.Tests
{
  public class CanonicalFunctions : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public CanonicalFunctions(DefaultFixture fixture)
    {
      st = fixture;
      if (st.Setup(this.GetType()))
        LoadData();
    }

    void LoadData()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        ctx.Products.Add(new Product() { Id = 1, Name = "Garbage Truck", Weight = 8.865f });
        ctx.Products.Add(new Product() { Id = 2, Name = "Fire Truck", Weight = 12.623f });
        ctx.Products.Add(new Product() { Id = 3, Name = "Hula Hoop", Weight = 2.687f });
        ctx.SaveChanges();
      }
    }

    //private EntityConnection GetEntityConnection()
    //{
    //  string connectionString = String.Format(
    //      "metadata=TestDB.csdl|TestDB.msl|TestDB.ssdl;provider=MySql.Data.MySqlClient; provider connection string=\"{0}\"", st.GetConnectionString(true));
    //  EntityConnection connection = new EntityConnection(connectionString);
    //  return connection;
    //}

    [Fact]
    public void Bitwise()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<Int32> q = context.CreateQuery<Int32>("BitwiseAnd(255,15)");
        foreach (int i in q)
          Assert.Equal(15, i);
        q = context.CreateQuery<Int32>("BitwiseOr(240,31)");
        foreach (int i in q)
          Assert.Equal(255, i);
        q = context.CreateQuery<Int32>("BitwiseXor(255,15)");
        foreach (int i in q)
          Assert.Equal(240, i);
      }
    }

    [Fact]
    public void CurrentDateTime()
    {
      DateTime current = DateTime.Now;

      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<DateTime> q = context.CreateQuery<DateTime>("CurrentDateTime()");
        foreach (DateTime dt in q)
        {
          Assert.Equal(current.Year, dt.Year);
          Assert.Equal(current.Month, dt.Month);
          Assert.Equal(current.Day, dt.Day);
          // we don't check time as that will be always be different
        }
      }
    }

    [Fact]
    public void YearMonthDay()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(
            @"SELECT c.DateBegan, Year(c.DateBegan), Month(c.DateBegan), Day(c.DateBegan)
                        FROM Companies AS c WHERE c.Id=1");
        foreach (DbDataRecord record in q)
        {
          Assert.Equal(1996, record[1]);
          Assert.Equal(11, record[2]);
          Assert.Equal(15, record[3]);
        }
      }
    }

    [Fact]
    public void HourMinuteSecond()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(
            @"SELECT c.DateBegan, Hour(c.DateBegan), Minute(c.DateBegan), Second(c.DateBegan)
                        FROM Companies AS c WHERE c.Id=1");
        foreach (DbDataRecord record in q)
        {
          Assert.Equal(5, record[1]);
          Assert.Equal(18, record[2]);
          Assert.Equal(23, record[3]);
        }
      }
    }

    [Fact]
    public void IndexOf()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<Int32> q = context.CreateQuery<Int32>(@"IndexOf('needle', 'haystackneedle')");
        foreach (int index in q)
          Assert.Equal(9, index);

        q = context.CreateQuery<Int32>(@"IndexOf('haystack', 'needle')");
        foreach (int index in q)
          Assert.Equal(0, index);
      }
    }

    [Fact]
    public void LeftRight()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        string entitySQL = "CONCAT(LEFT('foo',3),RIGHT('bar',3))";
        ObjectQuery<String> query = context.CreateQuery<String>(entitySQL);
        foreach (string s in query)
          Assert.Equal("foobar", s);

        entitySQL = "CONCAT(LEFT('foobar',3),RIGHT('barfoo',3))";
        query = context.CreateQuery<String>(entitySQL);
        foreach (string s in query)
          Assert.Equal("foofoo", s);

        entitySQL = "CONCAT(LEFT('foobar',8),RIGHT('barfoo',8))";
        query = context.CreateQuery<String>(entitySQL);
        foreach (string s in query)
          Assert.Equal("foobarbarfoo", s);
      }
    }

    [Fact]
    public void Length()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        string entitySQL = "Length('abc')";
        ObjectQuery<Int32> query = context.CreateQuery<Int32>(entitySQL);
        foreach (int len in query)
          Assert.Equal(3, len);
      }
    }

    [Fact]
    public void Trims()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<string> query = context.CreateQuery<string>("LTrim('   text   ')");
        foreach (string s in query)
          Assert.Equal("text   ", s);
        query = context.CreateQuery<string>("RTrim('   text   ')");
        foreach (string s in query)
          Assert.Equal("   text", s);
        query = context.CreateQuery<string>("Trim('   text   ')");
        foreach (string s in query)
          Assert.Equal("text", s);
      }
    }

    [Fact]
    public void Round()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(@"
                    SELECT p.Id, p.Weight, 
                    Round(p.Weight) AS [Rounded Weight],
                    Floor(p.Weight) AS [Floor of Weight], 
                    Ceiling(p.Weight) AS [Ceiling of Weight] 
                    FROM Products AS p WHERE p.Id=1");
        foreach (DbDataRecord r in q)
        {
          Assert.Equal(1, r[0]);
          Assert.Equal(8.865f, (float)r[1]);
          Assert.Equal(9, Convert.ToInt32(r[2]));
          Assert.Equal(8, Convert.ToInt32(r[3]));
          Assert.Equal(9, Convert.ToInt32(r[4]));
        }
      }
    }

    [Fact]
    public void Substring()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<string> query = context.CreateQuery<string>("SUBSTRING('foobarfoo',4,3)");
        query = context.CreateQuery<string>("SUBSTRING('foobarfoo',4,30)");
        foreach (string s in query)
          Assert.Equal("barfoo", s);
      }
    }

    [Fact]
    public void ToUpperToLowerReverse()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(
            @"SELECT ToUpper(c.Name),ToLower(c.Name),
                    Reverse(c.Name) FROM Companies AS c WHERE c.Id=1");
        foreach (DbDataRecord r in q)
        {
          Assert.Equal("HASBRO", r[0]);
          Assert.Equal("hasbro", r[1]);
          Assert.Equal("orbsaH", r[2]);
        }
      }
    }

    [Fact]
    public void Replace()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        ObjectQuery<string> q = context.CreateQuery<string>(
            @"Replace('abcdefghi', 'def', 'zzz')");
        foreach (string s in q)
          Assert.Equal("abczzzghi", s);
      }
    }

    [Fact]
    public void CanRoundToNonZeroDigits()
    {

      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        DbDataRecord product = context.CreateQuery<DbDataRecord>(@"
                                        SELECT p.Id, p.Weight, 
                                        Round(p.Weight, 2) AS [Rounded Weight]
                                        FROM Products AS p WHERE p.Id=1").First();

        Assert.Equal((float)8.865, (float)product[1]);
        Assert.Equal((double)8.86, (double)product[2]);
      }
    }

    /// <summary>
    /// Fix for bug "Using List.Contains in Linq to EF generates many ORs instead of more efficient IN"
    /// (http://bugs.mysql.com/bug.php?id=64934 / http://www.google.com ).
    /// </summary>
    [Fact]
    public void ListContains2In()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        int[] Ages = new int[] { 8, 9, 10 };
        var q = from e in ctx.Products
                where Ages.Contains(e.MinAge)
                orderby e.Name
                select e;
        var sql = q.ToString();
        st.CheckSql(sql, 
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`MinAge`, `Extent1`.`Weight`, 
          `Extent1`.`CreatedDate` FROM `Products` AS `Extent1` WHERE `Extent1`.`MinAge` IN ( 8,9,10 )
          ORDER BY `Extent1`.`Name` ASC");
      }
    }

    [Fact]
    public void ComplexListIn()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        int[] Ages = new int[] { 8, 9, 10 };
        var q = from e in ctx.Products
                where (Ages.Contains(e.MinAge) && e.Name.Contains("Hoop")) ||
                       !Ages.Contains(e.MinAge)
                orderby e.Name
                select e;
        var sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`MinAge`, `Extent1`.`Weight`, `Extent1`.`CreatedDate`
            FROM `Products` AS `Extent1` WHERE ((`Extent1`.`MinAge` IN ( 8,9,10 )) AND 
            (`Extent1`.`Name` LIKE @gp1)) OR (`Extent1`.`MinAge` NOT  IN ( 8,9,10 )) ORDER BY 
            `Extent1`.`Name` ASC");
      }
    }

    [Fact]
    public void MultipleOrs()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        // 3rd test, using only ||'s
        var q = from e in ctx.Products
             where e.MinAge == 37 || e.MinAge == 38 || e.MinAge == 39 ||
             e.MinAge == 40 || e.MinAge == 40 || e.MinAge == 41 || 
             e.MinAge == 42 || e.MinAge == 43
            orderby e.Name
            select e;
        var sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`MinAge`, `Extent1`.`Weight`, `Extent1`.`CreatedDate`
          FROM `Products` AS `Extent1` WHERE (((((((37 = `Extent1`.`MinAge`) OR (38 = `Extent1`.`MinAge`)) OR 
          (39 = `Extent1`.`MinAge`)) OR (40 = `Extent1`.`MinAge`)) OR (40 = `Extent1`.`MinAge`)) OR 
          (41 = `Extent1`.`MinAge`)) OR (42 = `Extent1`.`MinAge`)) OR (43 = `Extent1`.`MinAge`)
          ORDER BY `Extent1`.`Name` ASC");
      }
    }

    /// <summary>
    /// Fix for bug LINQ to SQL's StartsWith() and Contains() generate slow LOCATE() 
    /// instead of LIKE (bug http://bugs.mysql.com/bug.php?id=64935 / http://clustra.no.oracle.com/orabugs/14009363).
    /// </summary>
    [Fact]
    public void ConversionToLike()
    {
      // Generates queries for each LIKE + wildcards case and checks SQL generated.
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        // Like 'pattern%'
        var q = from c in ctx.Products
                where c.Name.StartsWith("B")
                orderby c.Name
                select c;
        var sql = q.ToString();
        st.CheckSql(sql, 
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`MinAge`, `Extent1`.`Weight`, 
            `Extent1`.`CreatedDate` FROM `Products` AS `Extent1` WHERE `Extent1`.`Name` LIKE @gp1
            ORDER BY `Extent1`.`Name` ASC");

        // Like '%pattern%'
        q = from c in ctx.Products
            where c.Name.Contains("r")
            orderby c.Name
            select c;
        sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`MinAge`, `Extent1`.`Weight`, 
          `Extent1`.`CreatedDate` FROM `Products` AS `Extent1` WHERE `Extent1`.`Name` LIKE @gp1
          ORDER BY `Extent1`.`Name` ASC");

        // Like '%pattern'
        q = from c in ctx.Products
            where c.Name.EndsWith("y")
            orderby c.Name
            select c;
        sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`MinAge`, `Extent1`.`Weight`, `Extent1`.`CreatedDate`
          FROM `Products` AS `Extent1` WHERE `Extent1`.`Name` LIKE @gp1 ORDER BY `Extent1`.`Name` ASC");
      }
    }

    ///// <summary>
    ///// Tests fix for bug http://bugs.mysql.com/bug.php?id=69409, Entity Framework Syntax Error in Where clause.
    ///// </summary>
    [Fact]
    public void EFSyntaxErrorApostrophe()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        var q = from c in ctx.Products
                where c.Name.EndsWith("y'")
                orderby c.Name
                select c;
        var sql = q.ToString();
        st.CheckSql(sql,
          @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`MinAge`, `Extent1`.`Weight`, `Extent1`.`CreatedDate`
            FROM `Products` AS `Extent1` WHERE `Extent1`.`Name` LIKE @gp1 ORDER BY `Extent1`.`Name` ASC");
      }
    }
  }
}
