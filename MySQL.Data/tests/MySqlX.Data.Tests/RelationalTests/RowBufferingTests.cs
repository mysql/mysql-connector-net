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

using MySqlX.XDevAPI.Relational;
using System.Linq;
using Xunit;

namespace MySqlX.Data.Tests.RelationalTests
{
  class RowBufferingTests : BaseTest
  {
    [Fact]
    public void SmartBuffering()
    {
      ExecuteSQL("CREATE TABLE test1(id INT)");
      ExecuteSQL("INSERT INTO test1 VALUES (1),(2),(3),(4)");
      ExecuteSQL("CREATE TABLE test2(id INT, val INT)");
      ExecuteSQL("INSERT INTO test2 VALUES (1,0)");

      var rowResult = testSchema.GetTable("test1").Select("id").Execute();
      foreach (var row in rowResult)
      {
        var result = testSchema.GetTable("test2").Update().Where("id=1").Set("val", row["id"]).Execute();
        Assert.Equal<ulong>(1, result.RecordsAffected);
      }

      Row valRow = testSchema.GetTable("test2").Select("val").Execute().FetchOne();
      Assert.Equal(4, valRow[0]);
    }
  }
}
