// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI.Relational;
using System.Linq;
using Xunit;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class RowBufferingTests : BaseTest
  {
    [Fact]
    public void SmartBuffering()
    {
      ExecuteSQL("CREATE TABLE test1(id INT)");
      ExecuteSQL("INSERT INTO test1 VALUES (1),(2),(3),(4)");
      ExecuteSQL("CREATE TABLE test2(id INT, val INT)");
      ExecuteSQL("INSERT INTO test2 VALUES (1,0)");

      var rowResult = ExecuteSelectStatement(testSchema.GetTable("test1").Select("id"));
      Assert.Equal(0, rowResult.IndexOf("id"));
      foreach (var row in rowResult)
      {
        var result = ExecuteUpdateStatement(testSchema.GetTable("test2").Update().Where("id=1").Set("val", row["id"]));
        Assert.Equal<ulong>(1, result.AffectedItemsCount);
      }

      Row valRow = ExecuteSelectStatement(testSchema.GetTable("test2").Select("val")).FetchOne();
      Assert.Equal(4, valRow[0]);
    }
  }
}
