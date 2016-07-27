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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Globalization;
using Xunit;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using MySql.Data.Entity.Tests.v4.x;

namespace MySql.Data.Entity.Tests
{
    public class ProviderServicesTests : IUseFixture<SetUpEntityTests>, IDisposable
    {
        private SetUpEntityTests st;

        public void SetFixture(SetUpEntityTests data)
        {
            st = data;
        }
        private CultureInfo originalCulture;

        public ProviderServicesTests()
            : base()
        {
            originalCulture = Thread.CurrentThread.CurrentCulture;
        }

#if CLR4
    [Fact]
    public void CreateDatabase()
    {
      st.suExecSQL("GRANT ALL ON `modeldb`.* to 'test'@'localhost'");
      st.suExecSQL("FLUSH PRIVILEGES");

      using (Model1Container ctx = new Model1Container())
      {
        Assert.False(ctx.DatabaseExists());
        ctx.CreateDatabase();
        Assert.True(ctx.DatabaseExists());
      }
    }

    [Fact]
    public void CreateDatabaseScript()
    {
      using (testEntities ctx = new testEntities())
      {        
        string s = ctx.CreateDatabaseScript();
      }
    }

    [Fact]
    public void DeleteDatabase()
    {
      st.suExecSQL("GRANT ALL ON `modeldb`.* to 'test'@'localhost'");
      st.suExecSQL("FLUSH PRIVILEGES");

      using (Model1Container ctx = new Model1Container())
      {
        Assert.False(ctx.DatabaseExists());
        ctx.CreateDatabase();
        Assert.True(ctx.DatabaseExists());
        ctx.DeleteDatabase();
        Assert.False(ctx.DatabaseExists());
      }
    }

    [Fact]
    public void DatabaseExists()
    {
      st.suExecSQL("GRANT ALL ON `modeldb`.* to 'test'@'localhost'");
      st.suExecSQL("FLUSH PRIVILEGES");

      using (Model1Container ctx = new Model1Container())
      {
        Assert.False(ctx.DatabaseExists());
        ctx.CreateDatabase();
        Assert.True(ctx.DatabaseExists());
        ctx.DeleteDatabase();
        Assert.False(ctx.DatabaseExists());
      }
    }
#endif

    [Fact(Skip = "EF 5 have an known issue that is happening when the CreateDatabaseScript is called in this test and is suppose to be fixed in EF 6 but need a lot of changes incopatibles with the current architecture")]
        public void CheckReservedWordColumnName()
        {
            using (ReservedWordColumnNameContainer ctx = new ReservedWordColumnNameContainer())
            {
                var ddl = ((IObjectContextAdapter)ctx).ObjectContext.CreateDatabaseScript();
                Assert.Contains("ALTER TABLE `table_name` ADD PRIMARY KEY (`key`);", ddl);
            }
        }

        [Fact]
        public void GetDbProviderManifestTokenDoesNotThrowWhenLocalized()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-CA");

            using (MySqlConnection connection = new MySqlConnection(st.GetConnectionString(true)))
            {
                MySqlProviderServices providerServices = new MySqlProviderServices();
                string token = null;

        Assert.DoesNotThrow(delegate () { token = providerServices.GetProviderManifestToken(connection); });
                Assert.NotNull(token);
            }
        }

        [Fact]
        public void GetDbProviderManifestTokenDoesNotThrowWhenMissingPersistSecurityInfo()
        {
            var conn = new MySqlConnection(st.rootConn.ConnectionString);
            conn.Open();
            MySqlProviderServices providerServices = new MySqlProviderServices();
            string token = null;

      Assert.DoesNotThrow(delegate () { token = providerServices.GetProviderManifestToken(conn); });
            Assert.NotNull(token);
            conn.Close();
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            st.Dispose();
        }
    }
}
