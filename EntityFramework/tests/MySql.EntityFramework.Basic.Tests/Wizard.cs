// Copyright © 2014, 2024, Oracle and/or its affiliates.
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

using System;
using MySql.Data.MySqlClient;
using System.Xml;
using NUnit.Framework;
using System.Data.Entity.Core.Common;

namespace MySql.Data.EntityFramework.Tests
{
  // This test unit covers the tests that the wizard runs when generating a model
  // from an existing database
  public class WizardTests : DefaultFixture
  {
    [Test]
    public void GetDbProviderManifestTokenReturnsCorrectSchemaVersion()
    {
      MySqlProviderServices services = new MySqlProviderServices();
      string token = services.GetProviderManifestToken(Connection);

      if (Version < new Version(5, 1))
        Assert.AreEqual("5.0", token);
      else if (Version < new Version(5, 5))
        Assert.AreEqual("5.1", token);
      else if (Version < new Version(5, 6))
        Assert.AreEqual("5.5", token);
      else if (Version < new Version(5, 7))
        Assert.AreEqual("5.6", token);
      else if (Version < new Version(8, 0))
        Assert.AreEqual("5.7", token);
      else
        Assert.AreEqual("8.0", token);
    }

    [Test]
    public void GetStoreSchemaDescriptionDoesNotThrowForServer50OrGreater()
    {
      MySqlProviderManifest manifest = new MySqlProviderManifest(Version.Major + "." + Version.Minor);
      using (XmlReader reader = manifest.GetInformation(DbXmlEnabledProviderManifest.StoreSchemaDefinition))
      {
        Assert.NotNull(reader);
      }
    }
  }
}
