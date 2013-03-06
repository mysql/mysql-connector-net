// Copyright © 2008, 2011, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.EntityClient;
using System.Data.Common;
using NUnit.Framework;
using System.Data.Objects;
using MySql.Data.Entity.Tests.Properties;
using System.Linq;

namespace MySql.Data.Entity.Tests
{
  [TestFixture]
  public class OrderingAndGrouping : BaseEdmTest
  {
    [Test]
    public void OrderBySimple()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT id FROM Companies c ORDER BY c.Name", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE c FROM Companies AS c ORDER BY c.Name";
        ObjectQuery<Company> query = context.CreateQuery<Company>(eSql);

        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.OrderBySimple);

        int i = 0;
        foreach (Company c in query)
          Assert.AreEqual(dt.Rows[i++][0], c.Id);
      }
    }

    [Test]
    public void OrderByWithPredicate()
    {
      using (testEntities context = new testEntities())
      {
        using (EntityConnection ec = context.Connection as EntityConnection)
        {
          ec.Open();
          MySqlDataAdapter da = new MySqlDataAdapter(
              "SELECT id FROM Companies c WHERE c.NumEmployees > 100 ORDER BY c.Name", conn);
          DataTable dt = new DataTable();
          da.Fill(dt);

          string eSql = "SELECT VALUE c FROM Companies AS c WHERE c.NumEmployees > 100 ORDER BY c.Name";
          ObjectQuery<Company> query = context.CreateQuery<Company>(eSql);

          string sql = query.ToTraceString();
          CheckSql(sql, SQLSyntax.OrderByWithPredicate);

          int i = 0;
          foreach (Company c in query)
            Assert.AreEqual(dt.Rows[i++][0], c.Id);
        }
      }
    }

    [Test]
    public void CanGroupBySingleColumn()
    {
      MySqlDataAdapter adapter = new MySqlDataAdapter(
          "SELECT Name, COUNT(Id) as Count FROM Companies GROUP BY Name", conn);
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
        CheckSql(sql, SQLSyntax.CanGroupBySingleColumn);

        int i = 0;
        foreach (var company in companies)
        {
          Assert.AreEqual(table.Rows[i][0], company.Name);
          Assert.AreEqual(table.Rows[i][1], company.Count);
          i++;
        }
      }
    }

    [Test]
    public void CanGroupByMultipleColumns()
    {
      MySqlDataAdapter adapter = new MySqlDataAdapter(
          "SELECT Name, COUNT(Id) as Count FROM Companies GROUP BY Name, NumEmployees, DateBegan", conn);
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
        CheckSql(sql, SQLSyntax.CanGroupByMultipleColumns);

        int i = 0;
        foreach (var company in companies)
        {
          Assert.AreEqual(table.Rows[i][0], company.Name);
          Assert.AreEqual(table.Rows[i][1], company.Count);
          i++;
        }
      }
    }

    [Test]
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