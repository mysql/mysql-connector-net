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
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Tests;
#if !EF6
using System.Data.EntityClient;
using System.Data.Objects; 
#else
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
#endif
using System.Data.Common;
using System.Linq;
using MySql.Data.Entity.Tests.Properties;
using Xunit;
using MySql.Data.Entity.Tests;

namespace MySql.Data.Entity.Tests
{  
  public class SetOperators : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }
    [Fact]
    public void Any()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          @"SELECT a.id FROM authors a WHERE NOT EXISTS(SELECT * FROM books b WHERE b.author_id=a.id)", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      int i = 0;
      // find all authors that are in our db with no books
      using (testEntities context = new testEntities())
      {
        var authors = from a in context.Authors where !a.Books.Any() select a;

        string sql = authors.ToTraceString();
        st.CheckSql(sql, SQLSyntax.Any);

        foreach (Author a in authors)
          Assert.Equal(dt.Rows[i++]["id"], a.Id);
      }
    }

    [Fact]
    public void FirstSimple()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM orders", st.conn);
      int id = (int)cmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        var q = from o in context.Orders
                select o;
        Order order = q.First() as Order;

        Assert.Equal(id, order.Id);
      }
    }

    [Fact]
    public void FirstPredicate()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM orders WHERE freight > 100", st.conn);
      int id = (int)cmd.ExecuteScalar();

      using (testEntities context = new testEntities())
      {
        var q = from o in context.Orders
                where o.Freight > 100
                select o;
        Order order = q.First() as Order;
        Assert.Equal(id, order.Id);
      }
    }

    [Fact]
    public void Distinct()
    {
      using (testEntities context = new testEntities())
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Companies LIMIT 2", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);

        int i = 0;
        var query = context.Companies.Top("2");
        foreach (Company c in query)
        {
          Assert.Equal(dt.Rows[i++]["id"], c.Id);
        }
      }
    }
  }
}