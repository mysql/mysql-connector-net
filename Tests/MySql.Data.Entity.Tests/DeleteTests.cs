// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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
using NUnit.Framework;
using MySql.Data.MySqlClient.Tests;
using System.Data.EntityClient;
using System.Data.Common;
using System.Data.Objects;
using System.Linq;

namespace MySql.Data.Entity.Tests
{
  [TestFixture]
  public class DeleteTests : BaseEdmTest
  {
    [Test]
    public void SimpleDeleteAllRows()
    {
      using (testEntities context = new testEntities())
      {
        foreach (Toy t in context.Toys)
          context.DeleteObject(t);
        context.SaveChanges();

        EntityConnection ec = context.Connection as EntityConnection;
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM toys",
            (MySqlConnection)ec.StoreConnection);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.AreEqual(0, dt.Rows.Count);
      }
    }

    [Test]
    public void SimpleDeleteRowByParameter()
    {
      using (testEntities context = new testEntities())
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM toys WHERE minage=3", conn);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.IsTrue(dt.Rows.Count > 0);

        ObjectQuery<Toy> toys = context.Toys.Where("it.MinAge = @age", new ObjectParameter("age", 3));
        foreach (Toy t in toys)
          context.DeleteObject(t);
        context.SaveChanges();

        dt.Clear();
        da.Fill(dt);
        Assert.AreEqual(0, dt.Rows.Count);
      }
    }

    /// <summary>
    /// Fix for bug Cascading delete using CreateDatabase in Entity Framework
    /// (http://bugs.mysql.com/bug.php?id=64779) using ModelFirst.
    /// </summary>
    [Test]
    public void OnDeleteCascade()
    {
#if CLR4
      using (ModelFirstModel1Container ctx = new ModelFirstModel1Container())
      {
        if (ctx.DatabaseExists())
          ctx.DeleteDatabase();
        ctx.CreateDatabase();
        ctx.SaveChanges();
      }
#endif
      using (ModelFirstModel1Container ctx = new ModelFirstModel1Container())
      {
        Student s = new Student();
        s.Name = "Einstein, Albert";
        s.Kardexes.Add(new Kardex() { Score = 9.0 });
        ctx.AddToStudents(s);
        ctx.SaveChanges();
      }

      using (ModelFirstModel1Container ctx = new ModelFirstModel1Container())
      {
        var a = from st in ctx.Students select st;
        Student s = a.First();
        s.Kardexes.Load();
        Assert.AreEqual( "Einstein, Albert", s.Name );
        Kardex k = s.Kardexes.First();
        Assert.AreEqual(9.0, k.Score);
        ctx.DeleteObject( s );
        ctx.SaveChanges();
      }

      using (ModelFirstModel1Container ctx = new ModelFirstModel1Container())
      {
        var q = from st in ctx.Students select st;
        Assert.AreEqual(0, q.Count());
        var q2 = from k in ctx.Kardexes select k;
        Assert.AreEqual(0, q2.Count());
      }
    }
  }
}