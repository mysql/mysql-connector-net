// Copyright © 2014 Oracle and/or its affiliates. All rights reserved.
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
using System.Threading;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Tests;
#if !EF6
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.Entity.Design;
#else
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.ModelConfiguration.Design;
#endif
using System.Data.Common;
using System.Linq;
using Store;
using System.Configuration;
using System.Xml;
using Xunit;


namespace MySql.Data.Entity.Tests
{
  // This test unit covers the tests that the wizard runs when generating a model
  // from an existing database
  public class WizardTests : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

#if !EF6
    private EntityConnection GetConnection()
    {
      return EntityStoreSchemaGenerator.CreateStoreSchemaConnection(
          "MySql.Data.MySqlClient", string.Format(@"server=localhost;uid=root;database=test;pooling=false; port={0}", st.port));
    }

    [Fact]
    public void SelectAllTables()
    {
      st.execSQL("CREATE TABLE IF NOT EXISTS test (id int)");

      System.Data.DataTable dt = st.conn.GetSchema("Tables");

      using (EntityConnection ec = GetConnection())
      {
        using (SchemaInformation si = new SchemaInformation(ec))
        {
          int i = 0;
          var q = si.Tables.Select("it.CatalogName, it.SchemaName, it.Name").OrderBy("it.Name, it.SchemaName");
          foreach (DbDataRecord t in q)
            Assert.Equal(dt.Rows[i++]["TABLE_NAME"], t.GetString(2));
        }
      }
    } 


    [Fact]
    public void SelectAllViews()
    {
      st.execSQL("CREATE TABLE IF NOT EXISTS test(id int)");
      st.execSQL("CREATE VIEW view1 as SELECT * FROM test");

      System.Data.DataTable dt = st.conn.GetSchema("Views");

      using (EntityConnection ec = GetConnection())
      {
        using (SchemaInformation si = new SchemaInformation(ec))
        {
          int i = 0;
          var q = si.Views.Select("it.CatalogName, it.SchemaName, it.Name").OrderBy("it.Name, it.SchemaName");
          foreach (DbDataRecord t in q)
            Assert.Equal(dt.Rows[i++]["TABLE_NAME"], t.GetString(2));
        }
      }
    }
#endif

    [Fact]
    public void GetDbProviderManifestTokenReturnsCorrectSchemaVersion()
    {
      if (st.Version < new Version(5, 0)) return;

      MySqlProviderServices services = new MySqlProviderServices();
      string token = services.GetProviderManifestToken(st.conn);

      if (st.Version < new Version(5, 1))
        Assert.Equal("5.0", token);
      else if (st.Version < new Version(5, 5))
        Assert.Equal("5.1", token);
      else if (st.Version < new Version(5, 6))
        Assert.Equal("5.5", token);
      else if (st.Version < new Version(5, 7))
        Assert.Equal("5.6", token);
      else
        Assert.Equal("5.7", token);
    }

    [Fact]
    public void GetStoreSchemaDescriptionDoesNotThrowForServer50OrGreater()
    {
      if (st.Version < new Version(5, 0)) return;

      MySqlProviderManifest manifest = new MySqlProviderManifest(st.Version.Major + "." + st.Version.Minor);
#if !EF6
      using (XmlReader reader = manifest.GetInformation(DbProviderManifest.StoreSchemaDefinition))
#else
      using (XmlReader reader = manifest.GetInformation(DbXmlEnabledProviderManifest.StoreSchemaDefinition))
#endif
      {
        Assert.NotNull(reader);
      }
    }
  }
}