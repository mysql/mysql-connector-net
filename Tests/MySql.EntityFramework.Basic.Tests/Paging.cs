// Copyright © 2014 Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Tests;
using System.Linq;
#if !EF6
using System.Data.EntityClient;
using System.Data.Objects; 
#else
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
#endif
using System.Data.Common;
using MySql.Data.Entity.Tests.Properties;
using Xunit;

namespace MySql.Data.Entity.Tests
{  
  public class Paging : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }
  
    [Fact]
    public void Top()
    {
      using (testEntities context = new testEntities())
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Companies LIMIT 2", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);

        int i = 0;
        var query = context.Companies.Top("2");
        string sql = query.ToTraceString();
        st.CheckSql(sql, SQLSyntax.Top);

        foreach (Company c in query)
        {
          Assert.Equal(dt.Rows[i++]["id"], c.Id);
        }
      }
    }

    [Fact]
    public void Skip()
    {
      using (testEntities context = new testEntities())
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Companies LIMIT 3,20", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);

        int i = 0;
        var query = context.Companies.Skip("it.Id", "3");
        string sql = query.ToTraceString();
        st.CheckSql(sql, SQLSyntax.Skip);

        foreach (Company c in query)
        {
          Assert.Equal(dt.Rows[i++]["id"], c.Id);
        }
      }
    }

    [Fact]
    public void SkipAndTakeSimple()
    {
      using (testEntities context = new testEntities())
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Companies LIMIT 2,2", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);

        int i = 0;
        var query = context.Companies.Skip("it.Id", "2").Top("2");
        string sql = query.ToTraceString();
        st.CheckSql(sql, SQLSyntax.SkipAndTakeSimple);

        foreach (Company c in query)
        {
          Assert.Equal(dt.Rows[i++]["id"], c.Id);
        }
        Assert.Equal(2, i);
      }
    }

    /// <summary>
    /// Bug #45723 Entity Framework DbSortExpression not processed when using Skip & Take  
    /// </summary>
    [Fact]
    public void SkipAndTakeWithOrdering()
    {
      using (testEntities context = new testEntities())
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Companies ORDER BY Name DESC LIMIT 2,2", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);

        int i = 0;
        var query = context.Companies.OrderByDescending(q => q.Name).Skip(2).Take(2);
        string sql = query.ToTraceString();
        st.CheckSql(sql, SQLSyntax.SkipAndTakeWithOrdering);
        foreach (Company c in query)
          Assert.Equal(dt.Rows[i++]["Name"], c.Name);
      }
    }

    /// <summary>
    /// Tests fix for bug #64749 - Entity Framework - Take().Count() fails with EntityCommandCompilationException.
    /// </summary>
    [Fact]
    public void TakeWithCount()
    {
      using (testEntities context = new testEntities())
      {
        int cnt = context.Companies.Take(2).Count();
        Assert.Equal(2, cnt);
      }
    }

    /// <summary>
    /// Fix for EF SQL Generator, Union Syntax (Concat operator) is missing required parentheses 
    /// (may cause semantic changes when combined with Limit clause (Take operator)). 
    /// (MySql bug #70828, Oracle bug #18049691).
    /// </summary>
    [Fact]
    public void TakeWithUnion()
    {
      int[] ids = new int[] { 1, 2, 3, 4 };
      string[] names = new string[] { "Slinky", "Rubiks Cube", "Lincoln Logs", "Legos" };
      using (testEntities ctx = new testEntities())
      {
        var q = ctx.Toys;
        var q2 = ctx.Toys.Take(0).Concat(q);
        var q3 = q.Concat(q.Take(0));
        int i = 0;
        string sql = q2.ToTraceString();
        st.CheckSql(sql, SQLSyntax.UnionWithLimit2);
        foreach (var row in q2)
        {
          Assert.Equal<int>(ids[i], row.Id);
          Assert.Equal<string>(names[i], row.Name);
          i++; 
        }
        i = 0;
        sql = q3.ToTraceString();
        st.CheckSql(sql, SQLSyntax.UnionWithLimit);
        foreach (var row in q)
        {
          Assert.Equal<int>(ids[i], row.Id);
          Assert.Equal<string>( names[ i ], row.Name );
          i++; 
        }
      }
    }
  }
}