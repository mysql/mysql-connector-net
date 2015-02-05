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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.Data.Common;
using MySql.Data.Entity.Tests.Properties;
using System.Diagnostics;
#if EF6
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
#else
using System.Data.EntityClient;
using System.Data.Objects;
#endif


namespace MySql.Data.Entity.Tests
{
  public class CanonicalFunctions : IUseFixture<SetUpEntityTests>, IDisposable
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

    public void Dispose()
    { }

    private EntityConnection GetEntityConnection()
    {
      string connectionString = String.Format(
          "metadata=TestDB.csdl|TestDB.msl|TestDB.ssdl;provider=MySql.Data.MySqlClient; provider connection string=\"{0}\"", st.GetConnectionString(true));
      EntityConnection connection = new EntityConnection(connectionString);
      return connection;
    }

    [Fact]
    public void Bitwise()
    {
      using (testEntities context = new testEntities())
      {
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

      using (testEntities context = new testEntities())
      {
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
      using (testEntities context = new testEntities())
      {
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
      using (testEntities context = new testEntities())
      {
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
      using (testEntities context = new testEntities())
      {
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
      using (testEntities context = new testEntities())
      {
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
      using (testEntities context = new testEntities())
      {
        string entitySQL = "Length('abc')";
        ObjectQuery<Int32> query = context.CreateQuery<Int32>(entitySQL);
        foreach (int len in query)
          Assert.Equal(3, len);
      }
    }

    [Fact]
    public void Trims()
    {
      using (testEntities context = new testEntities())
      {
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
      using (testEntities context = new testEntities())
      {
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(@"
                    SELECT o.Id, o.Freight, 
                    Round(o.Freight) AS [Rounded Freight],
                    Floor(o.Freight) AS [Floor of Freight], 
                    Ceiling(o.Freight) AS [Ceiling of Freight] 
                    FROM Orders AS o WHERE o.Id=1");
        foreach (DbDataRecord r in q)
        {
          Assert.Equal(1, r[0]);
          Assert.Equal(65.3, r[1]);
          Assert.Equal(65, Convert.ToInt32(r[2]));
          Assert.Equal(65, Convert.ToInt32(r[3]));
          Assert.Equal(66, Convert.ToInt32(r[4]));
        }
      }
    }

    [Fact]
    public void Substring()
    {
      using (testEntities context = new testEntities())
      {
        ObjectQuery<string> query = context.CreateQuery<string>("SUBSTRING('foobarfoo',4,3)");
        query = context.CreateQuery<string>("SUBSTRING('foobarfoo',4,30)");
        foreach (string s in query)
          Assert.Equal("barfoo", s);
      }
    }

    [Fact]
    public void ToUpperToLowerReverse()
    {
      using (testEntities context = new testEntities())
      {
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
      using (testEntities context = new testEntities())
      {
        ObjectQuery<string> q = context.CreateQuery<string>(
            @"Replace('abcdefghi', 'def', 'zzz')");
        foreach (string s in q)
          Assert.Equal("abczzzghi", s);
      }
    }

#if CLR4
    [Fact]
    public void CanRoundToNonZeroDigits()
    {

      using (testEntities context = new testEntities())
      {
        DbDataRecord order = context.CreateQuery<DbDataRecord>(@"
                                        SELECT o.Id, o.Freight, 
                                        Round(o.Freight, 2) AS [Rounded Freight]
                                        FROM Orders AS o WHERE o.Id=10").First();

        Assert.Equal(350.54721, order[1]);
        Assert.Equal(350.55, order[2]);
      }
    }
#endif


    /// <summary>
    /// Fix for bug "Using List.Contains in Linq to EF generates many ORs instead of more efficient IN"
    /// (http://bugs.mysql.com/bug.php?id=64934 / http://www.google.com ).
    /// </summary>
    [Fact]
    public void ListContains2In()
    {
      using (testEntities context = new testEntities())
      {
        int i = 0;

        // 1st test, only ORs
        List<int> Ages = new List<int>();
        Ages.AddRange(new int[] { 37, 38, 39, 40, 41, 42, 43 });
        var q = from e in context.Employees
                where Ages.Contains(e.Age.Value)
                orderby e.LastName, e.FirstName
                select e;
        string[,] data1 = new string[,] { { "Flintstone", "Fred" }, { "Flintstone", "Wilma" },
          { "Rubble", "Barney" } };
        string query = string.Empty;
#if NET_40_OR_GREATER
        query = q.ToTraceString();
        st.CheckSql(query, SQLSyntax.InExpressionSimple);
        Assert.Equal(3, q.Count());
        foreach (var e in q)
        {
          Assert.Equal(data1[i, 0], e.LastName);
          Assert.Equal(data1[i, 1], e.FirstName);
          i++;
        }

        // 2nd test, mix of ORs & ANDs
        Ages.Clear();
        Ages.AddRange(new int[] { 37, 38, 39 });
        List<int> Ages2 = new List<int>();
        Ages2.AddRange(new int[] { 40, 41, 42, 43 });
        q = from e in context.Employees
            where (Ages2.Contains(e.Age.Value) && (e.FirstName == "Flintstones")) ||
            (!Ages.Contains(e.Age.Value))
            orderby e.LastName, e.FirstName
            select e;
        query = q.ToTraceString();
        st.CheckSql(SQLSyntax.InExpressionComplex, query);
        Assert.Equal(6, q.Count());
        string[,] data2 = new string[,] { { "Doo", "Scooby" }, { "Flintstone", "Fred" }, 
          { "Rubble", "Barney" }, { "Rubble", "Betty" }, { "Slate", "S" }, 
          { "Underdog", "J" }
        };
        i = 0;
        foreach (var e in q)
        {
          Assert.Equal(data2[i, 0], e.LastName);
          Assert.Equal(data2[i, 1], e.FirstName);
          i++;
        }
#endif

        // 3rd test, using only ||'s
        q = from e in context.Employees
            where (e.Age.Value == 37) || (e.Age.Value == 38) || (e.Age.Value == 39) ||
            (e.Age.Value == 40) || (e.Age.Value == 41) || (e.Age.Value == 42) ||
            (e.Age.Value == 43)
            orderby e.LastName, e.FirstName
            select e;
        query = q.ToTraceString();
#if !EF6
        st.CheckSql(query, SQLSyntax.InExpressionSimple);
#else
        st.CheckSql(query, SQLSyntax.InExpressionSimpleEF6);
#endif
        Assert.Equal(3, q.Count());
        i = 0;
        foreach (var e in q)
        {
          Assert.Equal(data1[i, 0], e.LastName);
          Assert.Equal(data1[i, 1], e.FirstName);
          i++;
        }
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
      using (testEntities ctx = new testEntities())
      {
        // Like 'pattern%'
        var q = from c in ctx.Employees
                where c.FirstName.StartsWith("B")
                orderby c.FirstName
                select c;
        string query = q.ToTraceString();
        st.CheckSql(query, SQLSyntax.StartsWithTranslatedToLike);
        Assert.Equal(2, q.Count());
        Assert.Equal("Barney", q.First().FirstName);
        Assert.Equal("Betty", q.Skip(1).First().FirstName);
        // Like '%pattern%'
        q = from c in ctx.Employees
            where c.FirstName.Contains("r")
            orderby c.FirstName
            select c;
        query = q.ToTraceString();
        st.CheckSql(query, SQLSyntax.ContainsTranslatedToLike);
        Assert.Equal(2, q.Count());
        Assert.Equal("Barney", q.First().FirstName);
        Assert.Equal("Fred", q.Skip(1).First().FirstName);
        // Like '%pattern'
        q = from c in ctx.Employees
            where c.FirstName.EndsWith("y")
            orderby c.FirstName
            select c;
        query = q.ToTraceString();
        st.CheckSql(query, SQLSyntax.EndsWithTranslatedToLike);
        Assert.Equal(3, q.Count());
        Assert.Equal("Barney", q.First().FirstName);
        Assert.Equal("Betty", q.Skip(1).First().FirstName);
        Assert.Equal("Scooby", q.Skip(2).First().FirstName);
      }
    }

    /// <summary>
    /// Tests fix for bug http://bugs.mysql.com/bug.php?id=69409, Entity Framework Syntax Error in Where clause.
    /// </summary>
    [Fact]
    public void EFSyntaxErrorApostrophe()
    {
      using (testEntities ctx = new testEntities())
      {
        var q = from c in ctx.Employees
            where c.FirstName.EndsWith("y'")
            orderby c.FirstName
            select c;
        string query = q.ToTraceString();
        st.CheckSql(query, SQLSyntax.EndsWithTranslatedLikeApostrophes);
        foreach (var row in q)
        {
        }
      }
    }
  }
}
