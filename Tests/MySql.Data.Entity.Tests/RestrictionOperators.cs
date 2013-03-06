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
using System.Collections.Generic;
using MySql.Data.Entity.Tests.Properties;

namespace MySql.Data.Entity.Tests
{
  [TestFixture]
  public class RestrictionOperators : BaseEdmTest
  {
    [Test]
    public void SimpleSelect()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Toys", conn);
      DataTable toys = new DataTable();
      da.Fill(toys);
      int i = 0;

      using (testEntities context = new testEntities())
      {
        var query = context.CreateQuery<Toy>("SELECT VALUE c FROM Toys AS c");
        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.SimpleSelect);

        foreach (Toy t in query)
        {
          Assert.AreEqual(toys.Rows[i++]["name"], t.Name);
        }
      }
    }

    [Test]
    public void SimpleSelectWithFilter()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Toys WHERE minage=4", conn);
      DataTable toys = new DataTable();
      da.Fill(toys);
      int i = 0;

      using (testEntities context = new testEntities())
      {
        var query = context.CreateQuery<Toy>("SELECT VALUE t FROM Toys AS t WHERE t.MinAge=4");
        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.SimpleSelectWithFilter);

        foreach (Toy t in query)
          Assert.AreEqual(toys.Rows[i++]["name"], t.Name);
      }
    }

    [Test]
    public void SimpleSelectWithParam()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Toys WHERE minage>3", conn);
      DataTable toys = new DataTable();
      da.Fill(toys);
      int i = 0;

      using (testEntities context = new testEntities())
      {
        var query = context.CreateQuery<Toy>("SELECT VALUE t FROM Toys AS t WHERE t.MinAge>@age");
        query.Parameters.Add(new ObjectParameter("age", 3));
        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.SimpleSelectWithParam);

        foreach (Toy t in query)
        {
          Assert.AreEqual(toys.Rows[i++]["name"], t.Name);
        }
      }
    }

    [Test]
    public void WhereLiteralOnRelation()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT id FROM Companies WHERE city = 'Dallas'", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE c FROM Companies AS c WHERE c.Address.City = 'Dallas'";
        ObjectQuery<Company> query = context.CreateQuery<Company>(eSql);

        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.WhereLiteralOnRelation);

        int i = 0;
        foreach (Company c in query)
          Assert.AreEqual(dt.Rows[i++]["id"], c.Id);
      }
    }

    [Test]
    public void SelectWithComplexType()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT c.LastName FROM Employees AS c WHERE c.Age > 20", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = @"SELECT c.LastName FROM Employees AS c WHERE c.Age > 20";
        ObjectQuery<DbDataRecord> query = context.CreateQuery<DbDataRecord>(eSql);

        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.SelectWithComplexType);

        int i = 0;
        foreach (DbDataRecord s in query)
          Assert.AreEqual(dt.Rows[i++][0], s.GetString(0));
      }
    }

    [Test]
    public void WhereWithRelatedEntities1()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          "SELECT c.* FROM Toys t LEFT JOIN Companies c ON c.id=t.SupplierId WHERE c.State='TX'", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = "SELECT VALUE t FROM Toys AS t WHERE t.Supplier.Address.State = 'TX'";
        ObjectQuery<Toy> query = context.CreateQuery<Toy>(eSql);

        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.WhereWithRelatedEntities1);

        int i = 0;
        foreach (Toy t in query)
        {
          Assert.AreEqual(dt.Rows[i++]["id"], t.Id);
        }
      }
    }

    [Test]
    public void WhereWithRelatedEntities2()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          @"SELECT c.* FROM Toys t LEFT JOIN Companies c ON c.Id=t.SupplierId 
                    WHERE c.State<>'TX' AND c.State<>'AZ'", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = @"SELECT VALUE t FROM Toys AS t 
                    WHERE t.Supplier.Address.State<>'TX' AND t.Supplier.Address.State <> 'AZ'";
        ObjectQuery<Toy> query = context.CreateQuery<Toy>(eSql);

        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.WhereWithRelatedEntities2);

        int i = 0;
        foreach (Toy t in query)
        {
          Assert.AreEqual(dt.Rows[i++]["id"], t.Id);
        }
      }
    }

    [Test]
    public void Exists()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
          @"SELECT c.* FROM Companies c WHERE EXISTS 
                    (SELECT * FROM Toys t WHERE t.SupplierId=c.Id && t.MinAge < 4)", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      using (testEntities context = new testEntities())
      {
        string eSql = @"SELECT VALUE c FROM Companies AS c WHERE EXISTS(
                    SELECT p FROM c.Toys AS p WHERE p.MinAge < 4)";
        ObjectQuery<Company> query = context.CreateQuery<Company>(eSql);

        string sql = query.ToTraceString();
        CheckSql(sql, SQLSyntax.Exists);

        int i = 0;
        foreach (Company c in query)
          Assert.AreEqual(dt.Rows[i++]["id"], c.Id);
      }
    }
  }
}