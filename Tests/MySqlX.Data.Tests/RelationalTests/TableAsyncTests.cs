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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using Xunit;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableAsyncTests : BaseTest
  {
    public TableAsyncTests()
    {
      ExecuteSQL("CREATE TABLE test.test(id INT, age INT)");
    }

    [Fact]
    public void MultipleTableInsertAsync()
    {
      Table table = testSchema.GetTable("test");
      List<Task<Result>> tasksList = new List<Task<Result>>();

      for (int i = 1; i <= 200; i++)
      {
        tasksList.Add(table.Insert().Values(i, i%250).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      Assert.Equal(200, table.Count());
    }

    [Fact]
    public void MultipleTableSelectAsync()
    {
      var table = testSchema.GetTable("test");
      int rows = 100;
      var insert = table.Insert();
      HashSet<int> validator = new HashSet<int>();

      for(int i = 1; i <= rows; i++)
      {
        insert.Values(i, i);
      }
      var result = insert.Execute();

      List<Task<RowResult>> tasksList = new List<Task<RowResult>>();

      for(int i = 1; i <= rows; i++)
      {
        tasksList.Add(table.Select().Where("age = :age").Bind("aGe", i).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      foreach (Task<RowResult> task in tasksList)
      {
        Assert.Equal(2, task.Result.Columns.Count);
        Assert.Equal(1, task.Result.Rows.Count);
        int value = (int)task.Result.Rows[0][1];
        Assert.False(validator.Contains(value), value + " value exists");
        validator.Add(value);
      }
      Assert.Equal(rows, validator.Count);
    }
  }
}
