// Copyright (c) 2013, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System.Diagnostics;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySql.Data.EntityFramework.Tests
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
    /// Fix for "Connector/NET Generates Incorrect SELECT Clause after UPDATE" (MySql bug #62134, Oracle bug #13491689).
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
                SELECT `CreatedDate` FROM `Products` WHERE  row_count() = 1 and (`Id` = 1)");
          }
        }
      }
    }
  }
}