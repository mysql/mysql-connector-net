﻿// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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


using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class SqlServerMode : TestBase
  {
    public SqlServerMode(TestFixture fixture) : base(fixture)
    {
    }

    public override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.SqlServerMode = true;
    }

    [Fact]
    public void Simple()
    {
      executeSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      executeSQL("INSERT INTO Test VALUES (1, 'A')");

      MySqlCommand cmd = new MySqlCommand("SELECT [id], [name] FROM [Test]", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("A", reader.GetString(1));
      }
    }

  }
}
