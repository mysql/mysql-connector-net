// Copyright (c) 2013, 2019, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
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


using System;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Globalization;
using Xunit;
using System.Data;

namespace MySql.Data.EntityFramework.Tests
{
  public class ProviderServicesTests : IClassFixture<DefaultFixture>, IDisposable
  {
    private DefaultFixture st;
    private CultureInfo originalCulture;

    public ProviderServicesTests(DefaultFixture fixture)
    {
      st = fixture;
      originalCulture = Thread.CurrentThread.CurrentCulture;
      st.NeedSetup = true;
      st.Setup(this.GetType());
    }

    [Fact]
    public void CreateAndDeleteDatabase()
    {
      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        if (ctx.Database.Exists())
          ctx.Database.Delete();
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
        DataTable dt = st.Connection.GetSchema("DATABASES", new string[] { st.Connection.Database });
        Assert.Equal(1, dt.Rows.Count);
        ctx.Database.Delete();
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
      using (var conn = new MySqlConnection(st.ConnectionString))
      {
      conn.Open();
      MySqlProviderServices providerServices = new MySqlProviderServices();
      var token = providerServices.GetProviderManifestToken(conn);
      Assert.NotNull(token);
    }
    }

    public void Dispose()
    {
      Thread.CurrentThread.CurrentCulture = originalCulture;
      st.execSQL($"DROP DATABASE IF EXISTS `{st.Connection.Database}`");
    }
  }
}
