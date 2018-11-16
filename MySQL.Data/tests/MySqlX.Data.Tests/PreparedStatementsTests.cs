// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class PreparedStatementsTests : BaseTest
  {
    private const string _collectionName = "Books";

    private static object[] _docs = new[]
    {
      new {  _id = 1, title = "Book 1", pages = 20 },
      new {  _id = 2, title = "Book 2", pages = 30 },
      new {  _id = 3, title = "Book 3", pages = 40 },
      new {  _id = 4, title = "Book 4", pages = 50 },
    };

    public void Init()
    {
      Collection coll = CreateCollection(_collectionName);
      Result r = ExecuteAddStatement(coll.Add(_docs));
      Assert.Equal<ulong>(4, r.AffectedItemsCount);
    }

    private void ValidatePreparedStatements(int count, ulong executions, string sqlText, string threadId = null)
    {
      string condition;
      if (threadId == null)
        condition = "t.processlist_id = @@pseudo_thread_id";
      else
        condition = $"t.thread_id = {threadId}";

      string sql = $"SELECT SQL_TEXT, COUNT_EXECUTE " +
        $"FROM performance_schema.prepared_statements_instances AS p " +
        $"JOIN performance_schema.threads AS t " +
        $"ON p.owner_thread_id = t.thread_id " +
        $"WHERE {condition}";
      var preparedStatements = ExecuteSQL(sql).FetchAll();

      Assert.Equal(count, preparedStatements.Count);
      if (count > 0)
      {
        Assert.Equal(sqlText, preparedStatements[0]["SQL_TEXT"].ToString(), true, true, true);
        Assert.Equal(executions, preparedStatements[0]["COUNT_EXECUTE"]);
      }
    }

    private Collection GetCollection() => GetSession()
      .GetSchema(schemaName)
      .GetCollection(_collectionName);

    [Fact]
    public void Find()
    {
      Init();
      Collection coll = GetCollection();
      var findStmt = coll.Find("_id = :id").Bind("id", 1);
      DocResult foundDoc = ExecuteFindStatement(findStmt);
      Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
      Assert.False(findStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _docs.Length; i++)
      {
        DocResult foundDoc2 = ExecuteFindStatement(findStmt.Bind("id", i));
        Assert.Equal($"Book {i}", foundDoc2.FetchAll()[0]["title"].ToString());
        Assert.True(findStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 3,
        "SELECT doc FROM `test`.`Books` WHERE (JSON_EXTRACT(doc,'$._id') = ?)");
    }

    [Fact]
    public void FindWithChanges()
    {
      Init();
      Collection coll = GetCollection();
      var findStmt = coll.Find().Where($"_id = 1");

      DocResult foundDoc = ExecuteFindStatement(findStmt);
      Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
      Assert.False(findStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      foundDoc = ExecuteFindStatement(findStmt);
      Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
      Assert.True(findStmt._isPrepared);

      ValidatePreparedStatements(1, 1,
        "SELECT doc FROM `test`.`Books` WHERE (JSON_EXTRACT(doc,'$._id') = 1)");

      for (int i = 1; i <= _docs.Length; i++)
      {
        DocResult foundDoc2 = ExecuteFindStatement(findStmt.Where($"_id = {i}"));
        Assert.Equal($"Book {i}", foundDoc2.FetchAll()[0]["title"].ToString());
        Assert.False(findStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Fact]
    public void DeallocatePreparedStatmentsWhenClosingSession()
    {
      Init();
      string threadId;
      using (Session mySession = MySQLX.GetSession(ConnectionString))
      {
        mySession.SetCurrentSchema(schemaName);
        threadId = mySession.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        Collection coll = mySession.GetSchema(schemaName).GetCollection(_collectionName);

        var findStmt = coll.Find().Where($"_id = 1");

        DocResult foundDoc = ExecuteFindStatement(findStmt);
        Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
        Assert.False(findStmt._isPrepared);
        ValidatePreparedStatements(0, 0, null, threadId);

        foundDoc = ExecuteFindStatement(findStmt);
        Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
        Assert.True(findStmt._isPrepared);

        ValidatePreparedStatements(1, 1,
          "SELECT doc FROM `test`.`Books` WHERE (JSON_EXTRACT(doc,'$._id') = 1)",
          threadId);
      }
      ValidatePreparedStatements(0, 0, null, threadId);
    }
  }
}
