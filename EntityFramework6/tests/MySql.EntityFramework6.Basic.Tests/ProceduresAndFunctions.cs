// Copyright � 2013 Oracle and/or its affiliates. All rights reserved.
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

using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
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