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
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class EventTests : TestBase
  {
    public EventTests(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void Warnings()
    {
      executeSQL("CREATE TABLE Test (name VARCHAR(10))");

      using (var connection = Fixture.GetConnection(false))
      {
        MySqlCommand cmd = new MySqlCommand("SET SQL_MODE=''", connection);
        cmd.ExecuteNonQuery();

        connection.InfoMessage += new MySqlInfoMessageEventHandler(WarningsInfoMessage);

        cmd.CommandText = "INSERT INTO Test VALUES ('12345678901')";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
        }
      }
    }

    private void WarningsInfoMessage(object sender, MySqlInfoMessageEventArgs args)
    {
      Assert.Equal(1, args.errors.Length);
    }

    [Fact]
    public void StateChange()
    {
      using (var connection = Fixture.GetConnection(false))
      {
        connection.StateChange += new StateChangeEventHandler(StateChangeHandler);
        connection.Close();
      }
    }

    private void StateChangeHandler(object sender, StateChangeEventArgs e)
    {
    }
  }
}
