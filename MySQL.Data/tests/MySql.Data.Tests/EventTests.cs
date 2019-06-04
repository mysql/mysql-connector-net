// Copyright (c) 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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
    [Trait("Category", "Security")]
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
      Assert.Single(args.errors);
    }

    [Fact]
    [Trait("Category", "Security")]
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
