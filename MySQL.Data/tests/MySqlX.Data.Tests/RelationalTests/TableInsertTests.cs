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

using System.Linq;
using MySqlX.XDevAPI.Relational;
using Xunit;
using MySql.Data.MySqlClient;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableInsertTests : BaseTest
  {
    public TableInsertTests()
    {
    }

    [Fact]
    public void InsertMultipleValues()
    {
      ExecuteSQL("CREATE TABLE test.test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var result = table.Insert("name", "age")
        .Values("Henry", "22")
        .Values("Patric", 30)
        .Execute();
      Assert.Equal<ulong>(2, result.RecordsAffected);

      var selectResult = table.Select().Execute();
      while (selectResult.Next()) ;
      Assert.Equal(2, selectResult.Rows.Count);
      Assert.Equal("Henry", selectResult.Rows.ToArray()[0][0]);
      Assert.Equal(22, selectResult.Rows.ToArray()[0][1]);
      Assert.Equal("Patric", selectResult.Rows.ToArray()[1][0]);
      Assert.Equal(30, selectResult.Rows.ToArray()[1][1]);

      Assert.Equal(2, table.Count());
    }

    [Fact]
    public void InsertExpressions()
    {
      ExecuteSQL("CREATE TABLE test.test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var result = table.Insert("name", "age")
        .Values("upper('mark')", "50-16")
        .Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      var selectResult = table.Select().Execute();
      while (selectResult.Next()) ;
      Assert.Equal(1, selectResult.Rows.Count);
      Assert.Equal("MARK", selectResult.Rows.ToArray()[0][0]);
      Assert.Equal(34, selectResult.Rows.ToArray()[0][1]);
    }

    [Fact]
    public void ReuseStatement()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var stmt = table.Insert("name", "age");
      var result = stmt.Values("upper('mark')", "50-16").Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);
      Assert.Throws<MySqlException>(() => result = stmt.Values("George", 34, 1).Execute());
      result = stmt.Values("George", 34).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);
      Assert.Equal(2, table.Select().Execute().FetchAll().Count);
    }
  }
}
