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
using System.Data;
#if EF6
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
#else
using System.Data.EntityClient;
using System.Data.Objects;
#endif


namespace MySql.Data.Entity.Tests
{
  public class DeleteTests : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }    

    [Fact]
    public void SimpleDeleteAllRows()
    {
      //Make sure the table exists
      var createTableSql = "CREATE TABLE IF NOT EXISTS Toys ( `Id` INT NOT NULL AUTO_INCREMENT, `SupplierId` INT NOT NULL, `Name` varchar(100) NOT NULL,`MinAge` int NOT NULL, CONSTRAINT PK_Toys PRIMARY KEY (Id) ) ENGINE=InnoDB;";
      if (st.conn.State != ConnectionState.Open)
        st.conn.Open();

      MySqlHelper.ExecuteNonQuery(st.conn, createTableSql);
      MySqlHelper.ExecuteNonQuery(st.conn, "DELETE FROM Toys");
      MySqlHelper.ExecuteNonQuery(st.conn, "INSERT INTO Toys VALUES (1, 3, 'Slinky', 2), (2, 2, 'Rubiks Cube', 5), (3, 1, 'Lincoln Logs', 3), (4, 4, 'Legos', 4)");
      
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
        Assert.Equal(0, dt.Rows.Count);
      }
    }

    [Fact]
    public void SimpleDeleteRowByParameter()
    {
      //Make sure the table exists
      var createTableSql = "CREATE TABLE IF NOT EXISTS Toys ( `Id` INT NOT NULL AUTO_INCREMENT, `SupplierId` INT NOT NULL, `Name` varchar(100) NOT NULL,`MinAge` int NOT NULL, CONSTRAINT PK_Toys PRIMARY KEY (Id)) ENGINE=InnoDB;";
      if (st.conn.State != ConnectionState.Open)
        st.conn.Open();

      MySqlHelper.ExecuteNonQuery(st.conn, createTableSql);
      MySqlHelper.ExecuteNonQuery(st.conn, "DELETE FROM Toys");
      MySqlHelper.ExecuteNonQuery(st.conn, "INSERT INTO Toys VALUES (1, 3, 'Slinky', 2), (2, 2, 'Rubiks Cube', 5), (3, 1, 'Lincoln Logs', 3), (4, 4, 'Legos', 4)");
      
      
      using (testEntities context = new testEntities())
      {                
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM toys WHERE minage=3", st.conn);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.True(dt.Rows.Count > 0);

        ObjectQuery<Toy> toys = context.Toys.Where("it.MinAge = @age", new ObjectParameter("age", 3));
        foreach (Toy t in toys)
          context.DeleteObject(t);
        context.SaveChanges();

        dt.Clear();
        da.Fill(dt);
        Assert.Equal(0, dt.Rows.Count);
      }
    }

    /// <summary>
    /// Fix for bug Cascading delete using CreateDatabase in Entity Framework
    /// (http://bugs.mysql.com/bug.php?id=64779) using ModelFirst.
    /// </summary>
    [Fact]
    public void XOnDeleteCascade()
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
        Assert.Equal("Einstein, Albert", s.Name);
        Kardex k = s.Kardexes.First();
        Assert.Equal(9.0, k.Score);
        ctx.DeleteObject(s);
        ctx.SaveChanges();
      }

      using (ModelFirstModel1Container ctx = new ModelFirstModel1Container())
      {
        var q = from st in ctx.Students select st;
        Assert.Equal(0, q.Count());
        var q2 = from k in ctx.Kardexes select k;
        Assert.Equal(0, q2.Count());
      }
    }
  }
}
