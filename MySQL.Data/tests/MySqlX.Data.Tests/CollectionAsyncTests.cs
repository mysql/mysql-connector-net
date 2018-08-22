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

using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class CollectionAsyncTests : BaseTest
  {
    [Fact]
    public void CollectionInsert()
    {
      var coll = CreateCollection("test");
      List<Task<Result>> tasksList = new List<Task<Result>>();

      for (int i = 1; i <= 200; i++)
      {
        tasksList.Add(coll.Add(string.Format(@"{{ ""_id"": {0}, ""foo"": {0} }}", i)).ExecuteAsync());
      }

      Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void MultipleFindAsync()
    {
      var coll = testSchema.CreateCollection("test");
      int docs = 100;
      HashSet<string> validator = new HashSet<string>();
      var addStatement = coll.Add(new { id = 1, age = 1 });

      for (int i = 2; i <= docs; i++)
      {
        addStatement.Add(new { id = i, age = i });
      }
      var result = ExecuteAddStatement(addStatement);

      List<Task<DocResult>> tasksList = new List<Task<DocResult>>();

      for (int i = 1; i <= docs; i++)
      {
        tasksList.Add(coll.Find("age = :age").Bind("AgE", i).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      foreach (Task<DocResult> task in tasksList)
      {
        var doc = task.Result.FetchOne();
        string value = task.Result.Current["age"].ToString();
        Assert.False(validator.Contains(value), value + " value exists");
        validator.Add(value);
      }
      Assert.Equal(docs, validator.Count);
    }
  }
}
