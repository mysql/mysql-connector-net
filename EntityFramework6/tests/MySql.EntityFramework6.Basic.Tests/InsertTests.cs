// Copyright � 2013, 2017 Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;

namespace MySql.Data.Entity.Tests
{
  public class InsertTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public InsertTests(DefaultFixture fixture)
    {
      st = fixture;
      st.Setup(this.GetType());
    }

    [Fact]
    public void InsertSingleRow()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        int beforeCnt = ctx.Companies.Count();

        Company c = new Company();
        c.Name = "Yoyo";
        c.NumEmployees = 486;
        c.DateBegan = DateTime.Now;
        c.Address = new Address();
        c.Address.Street = "212 My Street.";
        c.Address.City = "Helena";
        c.Address.State = "MT";
        c.Address.ZipCode = "44558";

        ctx.Companies.Add(c);
        int result = ctx.SaveChanges();

        Assert.Equal(beforeCnt + 1, ctx.Companies.Count());

        Company d = ctx.Companies.Find(c.Id);
        d.Id = c.Id;
        Assert.Equal(c, d);
      }
    }

    [Fact(Skip ="Fix Me")]
    public void CanInsertRowWithDefaultTimeStamp()
    {
      //  using (var context = GetContext())
      //  {
      //    // The default timestamp is in the CreatedDate column.
      //    Company c = new Company();
      //    c.Nameproduct.Name = "Coca Cola";

      //    context.AddToProducts(product);
      //    context.SaveChanges();

      //    Assert.Equal(DateTime.Today.Day, product.CreatedDate.Day);
      //  }
    }

    [Fact]
    public async Task ExecuteNonQueryAndScalarAsyncAwait()
    {
      st.execSQL("CREATE TABLE NonQueryAndScalarAsyncAwaitTest (id int)");
      st.execSQL("CREATE PROCEDURE NonQueryAndScalarAsyncAwaitSpTest() BEGIN SET @x=0; REPEAT INSERT INTO NonQueryAndScalarAsyncAwaitTest VALUES(@x); " +
        "SET @x=@x+1; UNTIL @x = 100 END REPEAT; END");

      EFMySqlCommand proc = new EFMySqlCommand() { CommandText = "NonQueryAndScalarAsyncAwaitSpTest", Connection = st.Connection };
      proc.CommandType = CommandType.StoredProcedure;
      int result = await proc.ExecuteNonQueryAsync();

      Assert.NotEqual(-1, result);

      EFMySqlCommand cmd = new EFMySqlCommand() { CommandText = "SELECT COUNT(*) FROM NonQueryAndScalarAsyncAwaitTest;", Connection = st.Connection };
      cmd.CommandType = CommandType.Text;
      object cnt = await cmd.ExecuteScalarAsync();
      Assert.Equal(100, Convert.ToInt32(cnt));
    }

    [Fact]
    public async Task PrepareAsyncAwait()
    {
      st.execSQL("CREATE TABLE PrepareAsyncAwaitTest (val1 varchar(20), numbercol int, numbername varchar(50));");
      EFMySqlCommand cmd = new EFMySqlCommand() { CommandText = "INSERT INTO PrepareAsyncAwaitTest VALUES(NULL, @number, @text)", Connection = st.Connection };
      //TODO: Fix me
//      await cmd.PrepareAsync();

      cmd.Parameters.Add(new MySqlParameter("@number", 1));
      cmd.Parameters.Add(new MySqlParameter("@text", "One"));

      for (int i = 1; i <= 100; i++)
      {
        cmd.Parameters["@number"].Value = i;
        cmd.Parameters["@text"].Value = "A string value";
        cmd.ExecuteNonQuery();
      }
    }

    ///// <summary>
    ///// Test for fix for "NullReferenceException when try to save entity with TINYINY or BIGINT as PK" (MySql bug #70888, Oracle bug #17866076).
    ///// </summary>
    [Fact(Skip ="Fix Me")]
    public void NullReferenceWhenInsertingPk()
    {
      //using (testEntities1 ctx = new testEntities1())
      //{
      //  gamingplatform gp = new gamingplatform() { Name = "PlayStation2" };
      //  ctx.AddTogamingplatform(gp);
      //  ctx.SaveChanges();
      //}
    }

  }
}