// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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

namespace MySqlX.Data.Tests.RelationalTests
{
  public class SqlTests : BaseTest
  {
    [Fact]
    public void ReturnSimpleScalar()
    {
      ExecuteSQL("CREATE TABLE test(id INT)");
      ExecuteSQL("INSERT INTO test VALUES (1)");
      SqlResult r = GetNodeSession().SQL("SELECT * FROM test").Execute();
      Assert.True(r.Next());
      Assert.Equal(1, r[0]);
      Assert.False(r.NextResult());
    }

    [Fact]
    public void ExecuteStoredProcedure()
    {
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; END");

      NodeSession session = GetNodeSession();
      var result = session.SQL("CALL my_proc()").Execute();
      Assert.True(result.HasData);
      var row = result.FetchOne();
      Assert.NotNull(row);
      Assert.Equal((sbyte)5, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());
      Assert.False(result.NextResult());
    }

    [Fact]
    public void ExecuteStoredProcedureMultipleResults()
    {
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; SELECT 'A'; SELECT 5 * 2; END");

      NodeSession session = GetNodeSession();
      var result = session.SQL("CALL my_proc()").Execute();
      Assert.True(result.HasData);
      var row = result.FetchOne();
      Assert.NotNull(row);
      Assert.Equal((sbyte)5, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.True(result.NextResult());
      row = result.FetchOne();
      Assert.NotNull(row);
      Assert.Equal("A", row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.True(result.NextResult());
      row = result.FetchOne();
      Assert.NotNull(row);
      Assert.Equal((sbyte)10, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.False(result.NextResult());
    }

    [Fact]
    public void Bind()
    {
      ExecuteSQL("CREATE TABLE test(id INT, letter varchar(1))");
      for (int i = 1; i <= 10; i++)
        GetNodeSession().SQL("INSERT INTO test VALUES (?, ?), (?, ?)")
          .Bind(i, ((char)('@' + i)).ToString())
          .Bind(++i, ((char)('@' + i)).ToString())
          .Execute();

      SqlResult result = GetNodeSession().SQL("select * from test where id=?").Bind(5).Execute();
      Assert.True(result.Next());
      Assert.Equal(1, result.Rows.Count);
      Assert.Equal(5, result[0]);
      Assert.Equal("E", result[1]);
    }

    [Fact]
    public void BindNull()
    {
      ExecuteSQL("CREATE TABLE test(id INT, letter varchar(1))");

      var nodeSession = GetNodeSession();
      var result = nodeSession.SQL("INSERT INTO test VALUES(1, ?), (2, 'B');").Bind(null).Execute();
      Assert.Equal(2ul, result.RecordsAffected);

      var sqlResult = nodeSession.SQL("SELECT * FROM test WHERE letter is ?").Bind(null).Execute().FetchAll();
      Assert.Equal(1, sqlResult.Count);
      Assert.Equal(1, sqlResult[0][0]);
      Assert.Null(sqlResult[0][1]);
    }
  }
}
