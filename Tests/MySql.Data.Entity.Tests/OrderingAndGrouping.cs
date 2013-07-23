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
using MySql.Data.Entity.Tests.Properties;
using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class OrderingAndGrouping : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

    [Fact]
    public void OrderBySimple()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT id FROM Companies c ORDER BY c.Name", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE c FROM Companies AS c ORDER BY c.Name";
        ObjectQuery<Company> query = context.CreateQuery<Company>(eSql);

        string sql = query.ToTraceString();
        st.CheckSql(sql, SQLSyntax.OrderBySimple);

        int i = 0;
        foreach (Company c in query)
          Assert.Equal(dt.Rows[i++][0], c.Id);
      }
    }

    [Fact]
    public void OrderByWithPredicate()
    {
      using (testEntities context = new testEntities())
      {
        using (EntityConnection ec = context.Connection as EntityConnection)
        {
          ec.Open();
          MySqlDataAdapter da = new MySqlDataAdapter(
              "SELECT id FROM Companies c WHERE c.NumEmployees > 100 ORDER BY c.Name", st.conn);
          DataTable dt = new DataTable();
          da.Fill(dt);

          string eSql = "SELECT VALUE c FROM Companies AS c WHERE c.NumEmployees > 100 ORDER BY c.Name";
          ObjectQuery<Company> query = context.CreateQuery<Company>(eSql);

          string sql = query.ToTraceString();
          st.CheckSql(sql, SQLSyntax.OrderByWithPredicate);

          int i = 0;
          foreach (Company c in query)
            Assert.Equal(dt.Rows[i++][0], c.Id);
        }
      }
    }

    [Fact]
    public void CanGroupBySingleColumn()
    {
      MySqlDataAdapter adapter = new MySqlDataAdapter(
          "SELECT Name, COUNT(Id) as Count FROM Companies GROUP BY Name", st.conn);
      DataTable table = new DataTable();
      adapter.Fill(table);

      using (testEntities context = new testEntities())
      {
        var companies = from c in context.Companies
                        group c by c.Name into cgroup
                        select new
                        {
                          Name = cgroup.Key,
                          Count = cgroup.Count()
                        };
        string sql = companies.ToTraceString();
        st.CheckSql(sql, SQLSyntax.CanGroupBySingleColumn);

        int i = 0;
        foreach (var company in companies)
        {
          Assert.Equal(table.Rows[i][0], company.Name);
          Assert.Equal(Convert.ToInt32(table.Rows[i][1]), Convert.ToInt32(company.Count));
          i++;
        }
      }
    }

    [Fact]
    public void CanGroupByMultipleColumns()
    {
      MySqlDataAdapter adapter = new MySqlDataAdapter(
          "SELECT Name, COUNT(Id) as Count FROM Companies GROUP BY Name, NumEmployees, DateBegan", st.conn);
      DataTable table = new DataTable();
      adapter.Fill(table);

      using (testEntities context = new testEntities())
      {
        var companies = from c in context.Companies
                        group c by new { c.Name, c.NumEmployees, c.DateBegan } into cgroup
                        select new
                        {
                          Name = cgroup.Key.Name,
                          Count = cgroup.Count()
                        };

        string sql = companies.ToTraceString();
        st.CheckSql(sql, SQLSyntax.CanGroupByMultipleColumns);

        int i = 0;
        foreach (var company in companies)
        {
          Assert.Equal(table.Rows[i][0],company.Name);
          Assert.Equal(Convert.ToInt32(table.Rows[i][1]),Convert.ToInt32(company.Count));
          i++;
        }
      }
    }

    [Fact]
    public void OrdersTableDoesNotProvokeSyntaxError()
    {
      using (model2Entities context = new model2Entities())
      {
        var customers = from c in context.customer
                        select c;

        Assert.DoesNotThrow(delegate { customers.ToList().ForEach(c => c.order.Load()); });
      }
    }
  }
}