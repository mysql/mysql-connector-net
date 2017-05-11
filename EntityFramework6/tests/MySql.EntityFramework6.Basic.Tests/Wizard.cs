// Copyright � 2014, 2017 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Entity.Core.Common;
using System.Xml;
using Xunit;


namespace MySql.Data.Entity.Tests
{
  // This test unit covers the tests that the wizard runs when generating a model
  // from an existing database
  public class WizardTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public WizardTests(DefaultFixture data)
    {
      st = data;
      st.Setup(this.GetType());
    }

    [Fact]
    public void GetDbProviderManifestTokenReturnsCorrectSchemaVersion()
    {
      MySqlProviderServices services = new MySqlProviderServices();
      string token = services.GetProviderManifestToken(st.Connection);

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
      MySqlProviderManifest manifest = new MySqlProviderManifest(st.Version.Major + "." + st.Version.Minor);
      using (XmlReader reader = manifest.GetInformation(DbXmlEnabledProviderManifest.StoreSchemaDefinition))
      {
        Assert.NotNull(reader);
      }
    }
  }
}