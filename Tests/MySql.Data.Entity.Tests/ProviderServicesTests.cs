// Copyright © 2011, Oracle and/or its affiliates. All rights reserved.
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
using NUnit.Framework;
using System.Threading;
using System.Globalization;

namespace MySql.Data.Entity.Tests
{
  [TestFixture]
  public class ProviderServicesTests : BaseEdmTest
  {
    private CultureInfo originalCulture;

    public override void Setup()
    {
      originalCulture = Thread.CurrentThread.CurrentCulture;
      base.Setup();
    }

#if CLR4
    [Test]
    public void CreateDatabase()
    {
      suExecSQL("GRANT ALL ON `modeldb`.* to 'test'@'localhost'");
      suExecSQL("FLUSH PRIVILEGES");

      using (Model1Container ctx = new Model1Container())
      {
        Assert.IsFalse(ctx.DatabaseExists());
        ctx.CreateDatabase();
        Assert.IsTrue(ctx.DatabaseExists());
      }
    }

    [Test]
    public void CreateDatabaseScript()
    {
      using (testEntities ctx = new testEntities())
      {
        string s = ctx.CreateDatabaseScript();
      }
    }

    [Test]
    public void DeleteDatabase()
    {
      suExecSQL("GRANT ALL ON `modeldb`.* to 'test'@'localhost'");
      suExecSQL("FLUSH PRIVILEGES");

      using (Model1Container ctx = new Model1Container())
      {
        Assert.IsFalse(ctx.DatabaseExists());
        ctx.CreateDatabase();
        Assert.IsTrue(ctx.DatabaseExists());
        ctx.DeleteDatabase();
        Assert.IsFalse(ctx.DatabaseExists());
      }
    }

    [Test]
    public void DatabaseExists()
    {
      suExecSQL("GRANT ALL ON `modeldb`.* to 'test'@'localhost'");
      suExecSQL("FLUSH PRIVILEGES");

      using (Model1Container ctx = new Model1Container())
      {
        Assert.IsFalse(ctx.DatabaseExists());
        ctx.CreateDatabase();
        Assert.IsTrue(ctx.DatabaseExists());
        ctx.DeleteDatabase();
        Assert.IsFalse(ctx.DatabaseExists());
      }
    }
#endif

    [Test]
    public void GetDbProviderManifestTokenDoesNotThrowWhenLocalized()
    {
      Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-CA");

      using (MySqlConnection connection = new MySqlConnection(GetConnectionString(true)))
      {
        MySqlProviderServices providerServices = new MySqlProviderServices();
        string token = null;

        Assert.DoesNotThrow(delegate() { token = providerServices.GetProviderManifestToken(connection); });
        Assert.IsNotNull(token);
      }
    }

    [Test]
    public void GetDbProviderManifestTokenDoesNotThrowWhenMissingPersistSecurityInfo()
    {
      using (MySqlConnection connection = new MySqlConnection(GetConnectionString(this.user, this.password, false, true)))
      {
        MySqlProviderServices providerServices = new MySqlProviderServices();
        string token = null;
        connection.Open();
        Assert.DoesNotThrow(delegate() { token = providerServices.GetProviderManifestToken(connection); });
        Assert.IsNotNull(token);
        connection.Close();
      }
    }

    public override void Teardown()
    {
      Thread.CurrentThread.CurrentCulture = originalCulture;
      base.Teardown();
    }
  }
}
