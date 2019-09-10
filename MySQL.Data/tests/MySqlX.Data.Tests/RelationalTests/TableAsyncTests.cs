// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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
      var result = ExecuteInsertStatement(insert);

      List<Task<RowResult>> tasksList = new List<Task<RowResult>>();

      for(int i = 1; i <= rows; i++)
      {
        tasksList.Add(table.Select().Where("age = :age").Bind("aGe", i).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      foreach (Task<RowResult> task in tasksList)
      {
        Assert.Equal(2, task.Result.Columns.Count);
        Assert.Single(task.Result.Rows);
        int value = (int)task.Result.Rows[0][1];
        Assert.False(validator.Contains(value), value + " value exists");
        validator.Add(value);
      }
      Assert.Equal(rows, validator.Count);
    }
  }
}
