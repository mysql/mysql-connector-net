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
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Data;
using MySql.Data.Entity.Tests.Properties;
#if EF6
using System.Data.Entity.Core.Objects;
#else
using System.Data.Objects;
#endif


namespace MySql.Data.Entity.Tests
{
  public class AggregateOperators : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    } 

    [Fact]
    public void CountSimple()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT COUNT(*) FROM Toys", st.conn);
      object trueCount = trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE Count(t.Id) FROM Toys AS t";
        ObjectQuery<Int32> q = context.CreateQuery<Int32>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.CountSimple);

        foreach (int count in q)
          Assert.Equal(Convert.ToInt32(trueCount), count);
      }
    }

    [Fact]
    public void BigCountSimple()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT COUNT(*) FROM Toys", st.conn);
      object trueCount = trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE BigCount(t.Id) FROM Toys AS t";
#if EF6
        ObjectQuery<long> q = context.CreateQuery<long>(eSql);
#else
        ObjectQuery<Int32> q = context.CreateQuery<Int32>(eSql);
#endif

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.BigCountSimple);

        foreach (int count in q)
          Assert.Equal(Convert.ToInt32(trueCount), count);
      }
    }

    [Fact]
    public void CountWithPredicate()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT COUNT(*) FROM Toys AS t WHERE t.MinAge > 3", st.conn);
      object trueCount = trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE Count(t.Id) FROM Toys AS t WHERE t.MinAge > 3";
        ObjectQuery<Int32> q = context.CreateQuery<Int32>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.CountWithPredicate);

        foreach (int count in q)
          Assert.Equal(Convert.ToInt32(trueCount), count);
      }
    }

    [Fact]
    public void MinSimple()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT MIN(minage) FROM Toys", st.conn);
      int trueMin = (int)trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE MIN(t.MinAge) FROM Toys AS t";
        ObjectQuery<Int32> q = context.CreateQuery<Int32>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MinSimple);

        foreach (int age in q)
          Assert.Equal(trueMin, age);
      }
    }

    [Fact]
    public void MinWithPredicate()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT MIN(Freight) FROM Orders WHERE shopId=2", st.conn);
      object freight = trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT Min(o.Freight) FROM Orders AS o WHERE o.Shop.Id = 2";
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MinWithPredicate);

        foreach (DbDataRecord r in q)
        {
          Assert.Equal(Convert.ToDouble(freight), r.GetDouble(0));
        }
      }
    }

    [Fact]
    public void MinWithGrouping()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT MIN(Freight) FROM Orders GROUP BY shopId", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE Min(o.Freight) FROM Orders AS o GROUP BY o.Shop.Id";
        ObjectQuery<Double> q = context.CreateQuery<Double>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MinWithGrouping);

        int i = 0;
        foreach (double freight in q)
          Assert.Equal(Convert.ToInt32(dt.Rows[i++][0]), Convert.ToInt32(freight));
      }
    }

    [Fact]
    public void MaxSimple()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT MAX(minage) FROM Toys", st.conn);
      int trueMax = (int)trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE MAX(t.MinAge) FROM Toys AS t";
        ObjectQuery<Int32> q = context.CreateQuery<Int32>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MaxSimple);

        foreach (int max in q)
          Assert.Equal(trueMax, max);
      }
    }

    [Fact]
    public void MaxWithPredicate()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT MAX(Freight) FROM Orders WHERE shopId=1", st.conn);
      object freight = trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT MAX(o.Freight) FROM Orders AS o WHERE o.Shop.Id = 1";
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MaxWithPredicate);

        foreach (DbDataRecord r in q)
          Assert.Equal(Convert.ToDouble(freight), r.GetDouble(0));
      }
    }

    [Fact]
    public void MaxWithGrouping()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT MAX(Freight) FROM Orders GROUP BY ShopId", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE MAX(o.Freight) FROM Orders AS o GROUP BY o.Shop.Id";
        ObjectQuery<Double> q = context.CreateQuery<Double>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MaxWithGrouping);

        int i = 0;
        foreach (double freight in q)
          Assert.Equal(Convert.ToInt32(dt.Rows[i++][0]), Convert.ToInt32(freight));
      }
    }

    [Fact]
    public void AverageSimple()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT AVG(minAge) FROM Toys", st.conn);
//#if EF6
//      int avgAge = (int)trueCmd.ExecuteScalar();
//#else
      Decimal avgAge = (Decimal)trueCmd.ExecuteScalar();
//#endif

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE Avg(t.MinAge) FROM Toys AS t";
//#if EF6
//        ObjectQuery<int> q = context.CreateQuery<int>(eSql);
//#else
        ObjectQuery<Decimal> q = context.CreateQuery<Decimal>(eSql);
//#endif

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.AverageSimple);

//#if EF6
//        foreach (int r in q)
//#else
        foreach (Decimal r in q)
//#endif
        Assert.Equal(avgAge, r);
      }
    }

    [Fact]
    public void AverageWithPredicate()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT AVG(Freight) FROM Orders WHERE shopId=3", st.conn);
      Double freight = (Double)trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE AVG(o.Freight) FROM Orders AS o WHERE o.Shop.Id = 3";
        ObjectQuery<Double> q = context.CreateQuery<Double>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.AverageWithPredicate);

        foreach (Double r in q)
          Assert.Equal(Convert.ToInt32(freight), Convert.ToInt32(r));
      }
    }

    [Fact]
    public void AverageWithGrouping()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT AVG(Freight) FROM Orders GROUP BY ShopId", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT AVG(o.Freight) FROM Orders AS o GROUP BY o.Shop.Id";
        ObjectQuery<DbDataRecord> q = context.CreateQuery<DbDataRecord>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.AverageWithGrouping);

        foreach (object x in q)
        {
          string s = x.GetType().ToString();
        }
        int i = 0;
        foreach (var freight in q)
        {
          //   Assert.Equal(Convert.ToInt32(dt.Rows[i++][0]), Convert.ToInt32(freight));
        }
      }
    }

    [Fact]
    public void SumSimple()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT SUM(minage) FROM Toys", st.conn);
      int sumAge = Convert.ToInt32(trueCmd.ExecuteScalar());

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE Sum(t.MinAge) FROM Toys AS t";
        ObjectQuery<Int32> q = context.CreateQuery<Int32>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.SumSimple);

        foreach (int r in q)
          Assert.Equal(sumAge, r);
      }
    }

    [Fact]
    public void SumWithPredicate()
    {
      MySqlCommand trueCmd = new MySqlCommand("SELECT SUM(Freight) FROM Orders WHERE shopId=2", st.conn);
      object freight = trueCmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE SUM(o.Freight) FROM Orders AS o WHERE o.Shop.Id = 2";
        ObjectQuery<Double> q = context.CreateQuery<Double>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.SumWithPredicate);

        foreach (Double r in q)
          Assert.Equal(freight, r);
      }
    }

    [Fact]
    public void SumWithGrouping()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT SUM(Freight) FROM Orders GROUP BY ShopId", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE SUM(o.Freight) FROM Orders AS o GROUP BY o.Shop.Id";
        ObjectQuery<Double> q = context.CreateQuery<Double>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.SumWithGrouping);

        int i = 0;
        foreach (double freight in q)
          Assert.Equal(Convert.ToInt32(dt.Rows[i++][0]), Convert.ToInt32(freight));
      }
    }

    [Fact]
    public void MaxInSubQuery1()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT s.* FROM Shops AS s WHERE s.id=(SELECT MAX(o.shopId) FROM Orders AS o)", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = @"SELECT VALUE s FROM Shops AS s WHERE s.Id = 
                                MAX(SELECT VALUE o.Shop.Id FROM Orders As o)";
        ObjectQuery<Shop> q = context.CreateQuery<Shop>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MaxInSubQuery1);

        int i = 0;
        foreach (Shop s in q)
          Assert.Equal(dt.Rows[i++]["id"], s.Id);
      }
    }

    [Fact]
    public void MaxInSubQuery2()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT s.* FROM Shops AS s WHERE s.id=(SELECT MAX(o.shopId) FROM Orders AS o)", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = @"SELECT VALUE s FROM Shops AS s WHERE s.Id = 
                                ANYELEMENT(SELECT VALUE MAX(o.Shop.Id) FROM Orders As o)";
        ObjectQuery<Shop> q = context.CreateQuery<Shop>(eSql);

        string sql = q.ToTraceString();
        st.CheckSql(sql, SQLSyntax.MaxInSubQuery2);

        int i = 0;
        foreach (Shop s in q)
          Assert.Equal(dt.Rows[i++]["id"], s.Id);
      }
    }

    /// <summary>
    /// This test the fix for bug 67377.
    /// </summary>
    [Fact]
    public void FirstOrDefaultNested()
    {
      using (testEntities ctx = new testEntities())
      {
        var q = ctx.Authors.Where(p => p.Id == p.Id).Select(p => new { AuthorId = p.Id, FirstBook = (int?)p.Books.FirstOrDefault().Id });

        string sql = q.ToTraceString();
        int?[,] input = { { 1, 1 }, { 2, 2 }, { 3, 3 }, { 4, null }, { 5, null } };
        int i = 0;
        foreach (var r in q)
        {
          Assert.Equal(input[i, 0], r.AuthorId);
          Assert.Equal(input[i, 1], r.FirstBook);
          i++;
        }
      }
    }
  }
}
