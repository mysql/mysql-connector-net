// Copyright © 2013, 2016 Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;
using System.Threading;
using System.Globalization;
using Xunit;
using System.Data;

namespace MySql.Data.Entity.Tests
{
  public class ProviderServicesTests : IClassFixture<DefaultFixture>, IDisposable
  {
    private DefaultFixture st;
    private CultureInfo originalCulture;

    public ProviderServicesTests(DefaultFixture fixture)
    {
      st = fixture;
      originalCulture = Thread.CurrentThread.CurrentCulture;
      st.Setup(this.GetType());
    }

    [Fact]
    public void CreateAndDeleteDatabase()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        Assert.False(ctx.Database.Exists());
        ctx.Database.Create();
        Assert.True(ctx.Database.Exists());
        ctx.Database.Delete();
        Assert.False(ctx.Database.Exists());
      }
    }

    [Fact]
    public void DatabaseExists()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        ctx.Database.Create();
        DataTable dt = st.Connection.GetSchema("DATABASES", new string[] { st.Connection.Database });
        Assert.Equal(1, dt.Rows.Count);
      }
    }

    [Fact(Skip = "EF 5 have an known issue that is happening when the CreateDatabaseScript is called in this test and is suppose to be fixed in EF 6 but need a lot of changes incopatibles with the current architecture")]
    public void CheckReservedWordColumnName()
    {
      //using (ReservedWordColumnNameContainer ctx = new ReservedWordColumnNameContainer())
      //{
      //  var ddl = ((IObjectContextAdapter)ctx).ObjectContext.CreateDatabaseScript();
      //  Assert.Contains("ALTER TABLE `table_name` ADD PRIMARY KEY (`key`);", ddl);
      //}
    }

    [Fact]
    public void GetDbProviderManifestTokenDoesNotThrowWhenLocalized()
    {
      Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-CA");

      using (MySqlConnection connection = new MySqlConnection(st.ConnectionString))
      {
        MySqlProviderServices providerServices = new MySqlProviderServices();
        var token = providerServices.GetProviderManifestToken(connection);
        Assert.NotNull(token);
      }
    }

    [Fact]
    public void GetDbProviderManifestTokenDoesNotThrowWhenMissingPersistSecurityInfo()
    {
      var conn = new MySqlConnection(st.ConnectionString);
      conn.Open();
      MySqlProviderServices providerServices = new MySqlProviderServices();
      var token = providerServices.GetProviderManifestToken(conn);
      Assert.NotNull(token);
      conn.Close();
    }

    public void Dispose()
    {
      Thread.CurrentThread.CurrentCulture = originalCulture;
      st.execSQL($"DROP DATABASE IF EXISTS `{st.Connection.Database}`");
    }
  }
}
