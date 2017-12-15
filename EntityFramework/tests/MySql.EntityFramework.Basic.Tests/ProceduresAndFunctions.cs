// Copyright (c) 2013, 2017, Oracle and/or its affiliates. All rights reserved.
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

using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using Xunit;

namespace MySql.Data.EntityFramework.Tests
{
  public class ProceduresAndFunctions : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public ProceduresAndFunctions(DefaultFixture data)
    {
      st = data;
      st.Setup(this.GetType());
    }

    /// <summary>
    /// Validates a stored procedure call using Code First
    /// Bug #14008699
    [Fact]
    public void CallStoredProcedure()
    {
      MySqlCommand cmd = new MySqlCommand("CREATE PROCEDURE CallStoredProcedure() BEGIN SELECT 5; END", st.Connection);
      cmd.ExecuteNonQuery();

      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        long count = ctx.Database.SqlQuery<long>("CallStoredProcedure").First();
        Assert.Equal(5, count);
      }
    }

    /// <summary>
    /// Bug #45277	Calling User Defined Function using eSql causes NullReferenceException
    /// </summary>
    [Fact]
    public void UserDefinedFunction()
    {
      MySqlCommand cmd = new MySqlCommand("CREATE FUNCTION spFunc() RETURNS INT BEGIN RETURN 3; END", st.Connection);
      cmd.ExecuteNonQuery();

      using (DefaultContext ctx = new DefaultContext(st.ConnectionString))
      {
        int val = ctx.Database.SqlQuery<int>(@"SELECT spFunc()").Single();
        Assert.Equal(3, val);
      }
      st.NeedSetup = true;
    }

    /// <summary>
    /// Bug #56806	Default Command Timeout has no effect in connection string
    /// </summary>
    [Fact]
    public void CommandTimeout()
    {
      MySqlCommand cmd = new MySqlCommand("CREATE FUNCTION spFunc() RETURNS INT BEGIN DO SLEEP(5); RETURN 4; END", st.Connection);
      cmd.ExecuteNonQuery();

      var sb = new MySqlConnectionStringBuilder(st.ConnectionString);
      sb.DefaultCommandTimeout = 3;
      sb.UseDefaultCommandTimeoutForEF = true;
      using (DefaultContext ctx = new DefaultContext(sb.ToString()))
      {
        var exception = Record.Exception(() =>
        {
          int val = ctx.Database.SqlQuery<int>(@"SELECT spFunc()").Single();
        });

        Assert.NotNull(exception);
      }
      st.NeedSetup = true;
    }
  }
}