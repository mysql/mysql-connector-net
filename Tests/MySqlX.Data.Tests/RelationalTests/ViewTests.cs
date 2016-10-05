// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class ViewTests : BaseTest
  {
    [Fact]
    public void TryUpdatingView()
    {
      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view1 AS select *, 1 from test");

      List<Table> tables = testSchema.GetTables();
      Assert.Equal(2, tables.Count);

      Table view = tables.First(i => i.IsView);
      Assert.Equal("view1", view.Name);
      MySqlException ex = Assert.Throws<MySqlException>(() => view.Insert().Values(1).Execute());
      Assert.Equal("Column '1' is not updatable", ex.Message);
    }

    [Fact]
    public void GetView()
    {
      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view1 AS select *, 1 from test");

      Table table = testSchema.GetTable("test");
      Table view = testSchema.GetTable("view1");
      Assert.True(view.IsView);
      Assert.False(table.IsView);
    }

    [Fact]
    public void NonExistingView()
    {
      Assert.Throws<MySqlException>(() => testSchema.GetTable("no_exists").IsView);
    }
  }
}
