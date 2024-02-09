// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Data.EntityFramework.Tests
{
  public class InsertTests : DefaultFixture
  {
    [Test]
    public void InsertSingleRow()
    {
      using (DefaultContext ctx = new DefaultContext(ConnectionString))
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

        Assert.AreEqual(beforeCnt + 1, ctx.Companies.Count());

        Company d = ctx.Companies.Find(c.Id);
        d.Id = c.Id;
        Assert.AreEqual(c, d);
      }
    }

    [Ignore("Fix Me")]
    public void CanInsertRowWithDefaultTimeStamp()
    {
      //  using (var context = GetContext())
      //  {
      //    // The default timestamp is in the CreatedDate column.
      //    Company c = new Company();
      //    c.Nameproduct.Name = "Coca Cola";

      //    context.AddToProducts(product);
      //    context.SaveChanges();

      //    Assert.AreEqual(DateTime.Today.Day, product.CreatedDate.Day);
      //  }
    }

    [Test]
    public async Task ExecuteNonQueryAndScalarAsyncAwait()
    {
      ExecSQL("CREATE TABLE NonQueryAndScalarAsyncAwaitTest (id int)");
      ExecSQL("CREATE PROCEDURE NonQueryAndScalarAsyncAwaitSpTest() BEGIN SET @x=0; REPEAT INSERT INTO NonQueryAndScalarAsyncAwaitTest VALUES(@x); " +
        "SET @x=@x+1; UNTIL @x = 100 END REPEAT; END");

      EFMySqlCommand proc = new EFMySqlCommand() { CommandText = "NonQueryAndScalarAsyncAwaitSpTest", Connection = Connection };
      proc.CommandType = CommandType.StoredProcedure;
      int result = await proc.ExecuteNonQueryAsync();

      Assert.AreNotEqual(-1, result);

      EFMySqlCommand cmd = new EFMySqlCommand() { CommandText = "SELECT COUNT(*) FROM NonQueryAndScalarAsyncAwaitTest;", Connection = Connection };
      cmd.CommandType = CommandType.Text;
      object cnt = await cmd.ExecuteScalarAsync();
      Assert.AreEqual(100, Convert.ToInt32(cnt));
    }

    [Test]
    public async Task PrepareAsyncAwait()
    {
      ExecSQL("CREATE TABLE PrepareAsyncAwaitTest (val1 varchar(20), numbercol int, numbername varchar(50));");
      EFMySqlCommand cmd = new EFMySqlCommand() { CommandText = "INSERT INTO PrepareAsyncAwaitTest VALUES(NULL, @number, @text)", Connection = Connection };
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
    [Ignore("Fix Me")]
    public void NullReferenceWhenInsertingPk()
    {
      //using (testEntities1 ctx = new testEntities1())
      //{
      //  gamingplatform gp = new gamingplatform() { Name = "PlayStation2" };
      //  ctx.AddTogamingplatform(gp);
      //  ctx.SaveChanges();
      //}
    }

    /// <summary>
    /// Bug#22564126 [SERVER SQL_MODE REPLACED WITH ANSI ONLY BY THE CONNECTOR]
    /// </summary>
    [Test]
    public void SqlModeReplacedByANSI()
    {
      using (DefaultContext ctx = new DefaultContext(ConnectionString))
      {
        ctx.Configuration.ValidateOnSaveEnabled = false;

        LongDataTest longData = new LongDataTest();
        longData.Data = "This does fit.";
        ctx.LongDataTests.Add(longData);
        Assert.IsTrue(ctx.SaveChanges() == 1);

        // Try to insert a value that is larger than the max lenght of the column
        longData.Data = "This does not fit in the column!!!";
        ctx.LongDataTests.Add(longData);
        var ex = Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());
        Assert.That(ex.InnerException.InnerException is MySqlException);
        StringAssert.AreEqualIgnoringCase("Data too long for column 'Data' at row 1", ex.InnerException.InnerException.Message);
      }
    }
  }
}
