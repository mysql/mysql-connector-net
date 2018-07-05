// Copyright © 2016, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class EntityTestsFixtureClass : IDisposable
  {
    // A trace listener to use during testing.
    private AssertFailTraceListener asertFailListener = new AssertFailTraceListener();    
    private readonly IServiceProvider _serviceProvider;
   

    public EntityTestsFixtureClass()      
    {
      //TODO check if we still need this listener
      // Replace existing listeners with listener for testing.
      Trace.Listeners.Clear();
      Trace.Listeners.Add(this.asertFailListener);

      var serviceCollection = new ServiceCollection();

      serviceCollection.AddDbContext<TestsContext>();

      _serviceProvider = serviceCollection.BuildServiceProvider();

    }

    //public void CreateContext(MySQLTestStore testStore)
    //{
    //  var optionsBuilder = new DbContextOptionsBuilder();
    //  optionsBuilder.UseMySQL(MySQLTestStore.CreateConnectionString(_databaseName));

    //  using (var context = new TestsContext(optionsBuilder.Options))
    //  {
    //    context.Database.EnsureDeleted();
    //    context.Database.EnsureCreated();
    //  }
    //}

    public void Dispose()
    {
      // ensure database deletion
      using (var cnn = new MySqlConnection(MySQLTestStore.baseConnectionString))
      {
        cnn.Open();
        var cmd = new MySqlCommand("DROP DATABASE IF EXISTS test", cnn);
        cmd.ExecuteNonQuery();
      }
    }
   

    protected internal void CheckSql(string sql, string refSql)
    {
      StringBuilder str1 = new StringBuilder();
      StringBuilder str2 = new StringBuilder();
      foreach (char c in sql)
        if (!Char.IsWhiteSpace(c))
          str1.Append(c);
      foreach (char c in refSql)
        if (!Char.IsWhiteSpace(c))
          str2.Append(c);
      Assert.Equal(0, String.Compare(str1.ToString(), str2.ToString(), true));
    }


    private class AssertFailTraceListener : DefaultTraceListener
    {
      public override void Fail(string message)
      {
        //Assert.Fail("Assertion failure: " + message);
      }

      public override void Fail(string message, string detailMessage)
      {
        //Assert.Fail("Assertion failure: " + detailMessage);
      }
    }
  }

  public class SakilaLiteFixture : IDisposable
  {
    public SakilaLiteFixture()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        context.InitContext();
      }
    }

    private void DeleteDatabase<TDbContext>() where TDbContext : MyTestContext, new()
    {
      using(TDbContext context = new TDbContext())
      {
        context.Database.EnsureDeleted();
      }
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          DeleteDatabase<SakilaLiteContext>();
          DeleteDatabase<SakilaLiteTableSplittingContext>();
          DeleteDatabase<SakilaLiteUpdateContext>();
        }

        disposedValue = true;
      }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
    }
    #endregion

  }
}
