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
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class ProceduresAndFunctions : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

    public ProceduresAndFunctions()
      : base()
    {
    }

    [Fact]
    public void Insert()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Authors", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      int count = dt.Rows.Count;

      using (testEntities context = new testEntities())
      {
        Author a = new Author();
        a.Id = 23;
        a.Name = "Test name";
        a.Age = 44;
        context.AddToAuthors(a);
        context.SaveChanges();
      }

      dt.Clear();
      da.Fill(dt);
      Assert.Equal(count + 1, dt.Rows.Count);
      Assert.Equal(23, dt.Rows[count]["id"]);
    }

    [Fact]
    public void Update()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Authors", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      int count = dt.Rows.Count;

      using (testEntities context = new testEntities())
      {
        var q = from a in context.Authors
                where a.Name == "Don Box"
                select a;
        foreach (Author a in q)
          a.Name = "Dummy";
        context.SaveChanges();
      }

      da.SelectCommand.CommandText = "SELECT * FROM Authors WHERE name='Dummy'";
      dt.Clear();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
    }

    [Fact]
    public void Delete()
    {
      using (testEntities context = new testEntities())
      {
        foreach (Book b in context.Books)
          context.DeleteObject(b);
        context.SaveChanges();
      }

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Books", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(0, dt.Rows.Count);
    }

    /// <summary>
    /// Bug #45277	Calling User Defined Function using eSql causes NullReferenceException
    /// </summary>
    [Fact]
    public void UserDefinedFunction()
    {
      using (EntityConnection conn = new EntityConnection("name=testEntities"))
      {
        conn.Open();

        string query = @"SELECT e.FirstName AS Name FROM testEntities.Employees AS e 
                    WHERE testModel.Store.spFunc(e.Id, '') = 6";
        using (EntityCommand cmd = new EntityCommand(query, conn))
        {
          EntityDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
          Assert.True(reader.Read());
          Assert.Equal("Scooby", reader[0]);
        }
      }
    }

    /// <summary>
    /// Bug #56806	Default Command Timeout has no effect in connection string
    /// </summary>
    [Fact]
    public void CommandTimeout()
    {
      string connectionString = String.Format(
          "metadata=res://*/TestModel.csdl|res://*/TestModel.ssdl|res://*/TestModel.msl;provider=MySql.Data.MySqlClient; provider connection string=\"{0};default command timeout=5\"", st.GetConnectionString(true));
      EntityConnection connection = new EntityConnection(connectionString);

      using (testEntities context = new testEntities(connection))
      {
        Author a = new Author();
        a.Id = 66;  // special value to indicate the routine should take 30 seconds
        a.Name = "Test name";
        a.Age = 44;
        context.AddToAuthors(a);
        try
        {
          context.SaveChanges();
          //Assert.Fail("This should have timed out");
        }
        catch (Exception ex)
        {
          string s = ex.Message;
        }
      }
    }

  }
}