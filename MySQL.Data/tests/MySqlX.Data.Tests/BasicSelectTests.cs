// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class BasicSelectTests : BaseTest
  {
    [Fact]
    public void SimpleSelect()
    {
      CreateBooksTable();
      Table books = GetTable("test", "books");

      RowResult result = books.Select("name", "pages").Execute();
      var rows = result.FetchAll();
      Assert.True(result.Columns.Count == 2);
      Assert.True(rows.Count == 2);
    }

    [Fact]
    public void SimpleSelectWithWhere()
    {
      CreateBooksTable();
      Table books = GetTable("test", "books");

      RowResult result = books.Select("name", "pages").Where("pages > 250").Execute();
      var rows = result.FetchAll();
      Assert.True(result.Columns.Count == 2);
      Assert.True(rows.Count == 1);
    }

    private void CreateBooksTable()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test.books");
      ExecuteSQL("CREATE TABLE test.books(id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(100), pages INT)");
      ExecuteSQL("INSERT INTO test.books VALUES (NULL, 'Moby Dick', 500)");
      ExecuteSQL("INSERT INTO test.books VALUES (NULL, 'A Tale of Two Cities', 250)");
    }
  }
}
