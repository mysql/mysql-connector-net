// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  public class CollectionAsyncTests : BaseTest
  {
    [Test]
    public void CollectionInsert()
    {
      var coll = CreateCollection("test");
      List<Task<Result>> tasksList = new List<Task<Result>>();

      for (int i = 1; i <= 200; i++)
      {
        tasksList.Add(coll.Add(string.Format(@"{{ ""_id"": {0}, ""foo"": {0} }}", i)).ExecuteAsync());
      }

      Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2));

      var count = session.SQL("SELECT COUNT(*) FROM test.test").Execute().FetchOne()[0];
      Assert.AreEqual(count, coll.Count());
    }

    [Test]
    public void MultipleFindAsync()
    {
      var coll = CreateCollection("test");
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
      Assert.AreEqual(docs, validator.Count);
    }

    #region WL14389

    [Test, Description("Create valid index using a document field type of array and setting array to true/with single key on all possible datatypes concurrently)")]
    public async Task IndexArrayMultiThreading()
    {
      var coll = CreateCollection("test");
      // For server not supporting array indexes, array option will be ignored and and old-style index will be created.
      if (!session.Version.isAtLeast(8, 0, 17))
      {
        coll.CreateIndex("myIndex", "{\"fields\": [{\"field\": $.myField, \"type\":\"DATE\", \"array\": true}]}");
        return;
      }
      int v1 = await IndexArrayMultiThreading_T1();
      int v2 = await IndexArrayMultiThreading_T2();

      Assert.AreEqual(2, v1 + v2);
    }

    private async Task<int> IndexArrayMultiThreading_T1()
    {
      Schema test = session.GetSchema(schemaName);
      var coll = test.GetCollection("test");
      Thread.Sleep(100);
      coll.CreateIndex("multiArrayIndex1", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"INT\", \"array\": false}," +
                                                        "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}," +
                                                        "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL\", \"array\": true}," +
                                                        "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}," +
                                                        "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}," +
                                                        "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}]}");
      return 1;
    }

    private async Task<int> IndexArrayMultiThreading_T2()
    {
      Schema test = session.GetSchema(schemaName);
      var coll = test.GetCollection("test");
      coll.CreateIndex("multiArrayIndex2", "{\"fields\": [{\"field\": \"$.intField\", \"type\": \"INT\", \"array\": false}," +
                                                        "{\"field\": \"$.charField\", \"type\": \"CHAR(255)\", \"array\": false}," +
                                                        "{\"field\": \"$.decimalField\", \"type\": \"DECIMAL\", \"array\": true}," +
                                                        "{\"field\": \"$.dateField\", \"type\": \"DATE\", \"array\": false}," +
                                                        "{\"field\": \"$.timeField\", \"type\": \"TIME\", \"array\": false}," +
                                                        "{\"field\": \"$.datetimeField\", \"type\": \"DATETIME\", \"array\": false}]}");
      return 1;
    }

    #endregion WL14389

  }
}
