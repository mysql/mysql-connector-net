// Copyright � 2013, 2017 Oracle and/or its affiliates. All rights reserved.
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

using System.Diagnostics;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class UpdateTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public UpdateTests(DefaultFixture data)
    {
      st = data;
      st.Setup(this.GetType());
    }

    /// <summary>
    /// Fix for "Connector/Net Generates Incorrect SELECT Clause after UPDATE" (MySql bug #62134, Oracle bug #13491689).
    /// </summary>
    [Fact]
    public void UpdateSimple()
    {
      var sb = new MySqlConnectionStringBuilder(st.ConnectionString);
      sb.Logging = true;
      using (DefaultContext ctx = new DefaultContext(sb.ToString()))
      {
        MySqlTrace.Listeners.Clear();
        MySqlTrace.Switch.Level = SourceLevels.All;
        GenericListener listener = new GenericListener();
        MySqlTrace.Listeners.Add(listener);

        Product p = new Product() { Name = "Acme" };
        ctx.Products.Add(p);
        ctx.SaveChanges();

        p.Name = "Acme 2";
        ctx.SaveChanges();

        Regex rx = new Regex(@"Query Opened: (?<item>UPDATE .*)", RegexOptions.Compiled | RegexOptions.Singleline);
        foreach (string s in listener.Strings)
        {
          Match m = rx.Match(s);
          if (m.Success)
          {
            st.CheckSqlContains(m.Groups["item"].Value,
              @"UPDATE `Products` SET `Name`='Acme 2' WHERE `Id` = 1;
                SELECT `CreatedDate` FROM `Products` WHERE  row_count() > 0 and `Id` = 1");
          }
        }
      }
    }
  }
}