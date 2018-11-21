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
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class PreparedStatementsTests : BaseTest
  {
    private const string _collectionName = "Books";
    private string _tableName = "BookTable";

    private static object[] _docs = new[]
    {
      new {  _id = 1, title = "Book 1", pages = 20 },
      new {  _id = 2, title = "Book 2", pages = 30 },
      new {  _id = 3, title = "Book 3", pages = 40 },
      new {  _id = 4, title = "Book 4", pages = 50 },
    };

    private static object[][] _allRows = {
      new object[] { 1, "jonh doe", 38 },
      new object[] { 2, "milton green", 45 },
      new object[] { 3, "larry smith", 24}
    };

    private void InitCollection()
    {
      Collection coll = CreateCollection(_collectionName);
      Result r = ExecuteAddStatement(coll.Add(_docs));
      Assert.Equal<ulong>(4, r.AffectedItemsCount);
    }

    public void InitTable()
    {
      ExecuteSQL($"CREATE TABLE {_tableName} (id INT, name VARCHAR(45), age INT)");
      TableInsertStatement stmt = testSchema.GetTable(_tableName).Insert();
      for (int i = 0; i < _allRows.Length; i++)
      {
        stmt.Values(_allRows[i]);
      }
      Result result = ExecuteInsertStatement(stmt);
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

    private Table GetTable() => GetSession()
      .GetSchema(schemaName)
      .GetTable(_tableName);


    [Fact]
    public void Find()
    {
      InitCollection();
      Collection coll = GetCollection();
      var findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
      DocResult doc = ExecuteFindStatement(findStmt);
      Assert.Equal("Book 1", doc.FetchAll()[0]["title"].ToString());
      Assert.False(findStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 1; i <= _docs.Length; i++)
      {
        doc = ExecuteFindStatement(findStmt.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
        Assert.Equal($"Book {i}", doc.FetchAll()[0]["title"].ToString());
        Assert.True(findStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 4,
        $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_EXTRACT(doc,'$._id') = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT 1");
    }

    [Fact]
    public void FindWithChanges()
    {
      InitCollection();
      Collection coll = GetCollection();
      var findStmt = coll.Find().Where("_id = 1");

      DocResult foundDoc = ExecuteFindStatement(findStmt);
      Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
      Assert.False(findStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      foundDoc = ExecuteFindStatement(findStmt.Limit(1));
      Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
      Assert.True(findStmt._isPrepared);

      ValidatePreparedStatements(1, 1,
        $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_EXTRACT(doc,'$._id') = 1) LIMIT 1");

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
      InitCollection();
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

        foundDoc = ExecuteFindStatement(findStmt.Limit(1));
        Assert.Equal("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
        Assert.True(findStmt._isPrepared);

        ValidatePreparedStatements(1, 1,
          $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_EXTRACT(doc,'$._id') = 1) LIMIT 1",
          threadId);

        mySession.Close();
        ValidatePreparedStatements(0, 0, null, threadId);
      }
    }

    [Fact]
    public void Select()
    {
      InitTable();
      Table table = GetTable();
      var selectStmt = table.Select().Where("id = :id").Bind("id", 1);
      RowResult row = ExecuteSelectStatement(selectStmt);
      Assert.Equal(_allRows[0][1], row.FetchAll()[0]["name"].ToString());
      Assert.False(selectStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 0; i < _allRows.Length; i++)
      {
        row = ExecuteSelectStatement(selectStmt.Bind("id", i + 1).Limit(1));
        Assert.Equal(_allRows[i][1], row.FetchAll()[0]["name"].ToString());
        Assert.True(selectStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 3,
        $"SELECT * FROM `{schemaName}`.`{_tableName}` WHERE (`id` = ?) LIMIT 1");
    }

    [Fact]
    public void SelectWithChanges()
    {
      InitTable();
      Table table = GetTable();
      var selectStmt = table.Select().Where("id = 1");

      RowResult row = ExecuteSelectStatement(selectStmt);
      Assert.Equal(_allRows[0][1], row.FetchAll()[0]["name"].ToString());
      Assert.False(selectStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      row = ExecuteSelectStatement(selectStmt.Limit(1));
      Assert.Equal(_allRows[0][1], row.FetchAll()[0]["name"].ToString());
      Assert.True(selectStmt._isPrepared);

      ValidatePreparedStatements(1, 1,
        $"SELECT * FROM `{schemaName}`.`{_tableName}` WHERE (`id` = 1) LIMIT 1");

      for (int i = 2; i <= _allRows.Length; i++)
      {
        row = ExecuteSelectStatement(selectStmt.Where($"id = {i}"));
        Assert.Equal(_allRows[i - 1][1], row.FetchAll()[0]["name"].ToString());
        Assert.False(selectStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Fact]
    public void Modify()
    {
      InitCollection();
      Collection coll = GetCollection();
      var modifyStmt = coll.Modify("_id = :id").Set("title", "Magazine").Bind("id", 1);
      Result result = ExecuteModifyStatement(modifyStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(modifyStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _docs.Length; i++)
      {
        result = ExecuteModifyStatement(modifyStmt.Bind("id", i).Limit(1));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.True(modifyStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 3,
        $"UPDATE `{schemaName}`.`{_collectionName}` SET doc=JSON_SET(JSON_SET(doc,'$.title','Magazine'),'$._id',JSON_EXTRACT(`doc`,'$._id')) WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT 1");
    }

    [Fact]
    public void ModifyWithChanges()
    {
      InitCollection();
      Collection coll = GetCollection();
      var modifyStmt = coll.Modify("_id = :id").Set("title", "Magazine 1").Bind("id", 1);
      Result result = ExecuteModifyStatement(modifyStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(modifyStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteModifyStatement(modifyStmt.Bind("id", 1).Limit(1));
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.True(modifyStmt._isPrepared);
      ValidatePreparedStatements(1, 1,
        $"UPDATE `{schemaName}`.`{_collectionName}` SET doc=JSON_SET(JSON_SET(doc,'$.title','Magazine 1'),'$._id',JSON_EXTRACT(`doc`,'$._id')) WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT 1");

      for (int i = 2; i <= _docs.Length; i++)
      {
        result = ExecuteModifyStatement(modifyStmt.Set("title", $"Magazine {i}").Bind("id", i));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.False(modifyStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Fact]
    public void Update()
    {
      InitTable();
      Table table = GetTable();
      var updateStmt = table.Update().Where("id = :id").Set("name", "Magazine").Bind("id", 1);
      Result result = ExecuteUpdateStatement(updateStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(updateStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _allRows.Length; i++)
      {
        result = ExecuteUpdateStatement(updateStmt.Bind("id", i).Limit(1));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.True(updateStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 2,
        $"UPDATE `{schemaName}`.`{_tableName}` SET `name`='Magazine' WHERE (`id` = ?) LIMIT 1");
    }

    [Fact]
    public void UpdateWithChanges()
    {
      InitTable();
      Table table = GetTable();
      var updateStmt = table.Update().Where("id = :id").Set("name", "Magazine 1").Bind("id", 1);
      Result result = ExecuteUpdateStatement(updateStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(updateStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteUpdateStatement(updateStmt.Bind("id", 1).Limit(1));
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.True(updateStmt._isPrepared);
      ValidatePreparedStatements(1, 1,
        $"UPDATE `{schemaName}`.`{_tableName}` SET `name`='Magazine 1' WHERE (`id` = ?) LIMIT 1");

      for (int i = 2; i <= _allRows.Length; i++)
      {
        result = ExecuteUpdateStatement(updateStmt.Set("name", $"Magazine {i}").Bind("id", i));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.False(updateStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Fact]
    public void Remove()
    {
      InitCollection();
      Collection coll = GetCollection();
      var removeStmt = coll.Remove("_id = :id").Bind("id", 1);
      Result result = ExecuteRemoveStatement(removeStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(removeStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _docs.Length; i++)
      {
        result = ExecuteRemoveStatement(removeStmt.Bind("id", i).Limit(1));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.True(removeStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 3,
        $"DELETE FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT 1");
    }

    [Fact]
    public void RemoveWithChanges()
    {
      InitCollection();
      Collection coll = GetCollection();
      var removeStmt = coll.Remove("_id = :id").Bind("id", 1);
      Result result = ExecuteRemoveStatement(removeStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(removeStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteRemoveStatement(removeStmt.Bind("id", 2).Limit(1));
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.True(removeStmt._isPrepared);
      ValidatePreparedStatements(1, 1,
        $"DELETE FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT 1");

      for (int i = 3; i <= _docs.Length; i++)
      {
        result = ExecuteRemoveStatement(removeStmt.Where($"_id = {i}"));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.False(removeStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Fact]
    public void Delete()
    {
      InitTable();
      Table table = GetTable();
      var deleteStmt = table.Delete().Where("id = :id").Bind("id", 1);
      Result result = ExecuteDeleteStatement(deleteStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(deleteStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _allRows.Length; i++)
      {
        result = ExecuteDeleteStatement(deleteStmt.Bind("id", i).Limit(1));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.True(deleteStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 2,
        $"DELETE FROM `{schemaName}`.`{_tableName}` WHERE (`id` = ?) LIMIT 1");
    }

    [Fact]
    public void DeleteWithChanges()
    {
      InitTable();
      Table table = GetTable();
      var deleteStmt = table.Delete().Where("id = :id").Bind("id", 1);
      Result result = ExecuteDeleteStatement(deleteStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(deleteStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteDeleteStatement(deleteStmt.Bind("id", 2).Limit(1));
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.True(deleteStmt._isPrepared);
      ValidatePreparedStatements(1, 1,
        $"DELETE FROM `{schemaName}`.`{_tableName}` WHERE (`id` = ?) LIMIT 1");

      for (int i = 3; i <= _allRows.Length; i++)
      {
        result = ExecuteDeleteStatement(deleteStmt.Where($"id = {i}"));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.False(deleteStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Fact]
    public void Insert()
    {
      InitTable();
      Table table = GetTable();
      var insertStmt = table.Insert().Values(8, "name 8", 40);
      Result result = ExecuteInsertStatement(insertStmt);
      Assert.Equal(1ul, result.AffectedItemsCount);
      Assert.False(insertStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 9; i <= 12; i++)
      {
        result = ExecuteInsertStatement(insertStmt.Values(i, $"name {i}", i * 5));
        Assert.Equal(1ul, result.AffectedItemsCount);
        Assert.True(insertStmt._isPrepared);
        var row = ExecuteSelectStatement(table.Select().Where($"id={i}")).FetchAll();
        Assert.Equal(i, row[0]["id"]);
        Assert.Equal($"name {i}", row[0]["name"]);
        Assert.Equal(i * 5, row[0]["age"]);
      }

      ValidatePreparedStatements(1, 4,
        $"INSERT INTO `{schemaName}`.`{_tableName}` VALUES (?,?,?)");
    }

    [Fact]
    public void InsertChainedValues()
    {
      InitTable();
      Table table = GetTable();
      var insertStmt = table.Insert().Values(7, "name 7", 35).Values(8, "name 8", 40);
      Result result = ExecuteInsertStatement(insertStmt);
      Assert.Equal(2ul, result.AffectedItemsCount);
      Assert.False(insertStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);
      var row = ExecuteSelectStatement(table.Select().Where($"id in (7,8)")).FetchAll();
      Assert.Equal(7, row[0]["id"]);
      Assert.Equal($"name 7", row[0]["name"]);
      Assert.Equal(7 * 5, row[0]["age"]);
      Assert.Equal(8, row[1]["id"]);
      Assert.Equal($"name 8", row[1]["name"]);
      Assert.Equal(8 * 5, row[1]["age"]);

      for (int i = 9; i <= 20; i += 2)
      {
        result = ExecuteInsertStatement(insertStmt.Values(i, $"name {i}", i * 5).Values(i + 1, $"name {i + 1}", (i + 1) * 5));
        Assert.Equal(2ul, result.AffectedItemsCount);
        Assert.True(insertStmt._isPrepared);
        row = ExecuteSelectStatement(table.Select().Where($"id in ({i},{i + 1})")).FetchAll();
        Assert.Equal(i, row[0]["id"]);
        Assert.Equal($"name {i}", row[0]["name"]);
        Assert.Equal(i * 5, row[0]["age"]);
        Assert.Equal(i + 1, row[1]["id"]);
        Assert.Equal($"name {i + 1}", row[1]["name"]);
        Assert.Equal((i + 1) * 5, row[1]["age"]);
      }

      ValidatePreparedStatements(1, 6,
        $"INSERT INTO `{schemaName}`.`{_tableName}` VALUES (?,?,?),(?,?,?)");
    }

    [Fact]
    public void SqlStatement()
    {
      var sqlStmt = GetSession().SQL("SELECT ? as number, ? as letter").Bind(1, "A");
      var row = ExecuteSQLStatement(sqlStmt).FetchAll();
      Assert.Equal((sbyte)1, row[0][0]);
      Assert.Equal("A", row[0][1]);
      Assert.False(sqlStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i < 10; i++)
      {
        row = ExecuteSQLStatement(sqlStmt.Bind(i, ((char)('@' + i)).ToString())).FetchAll();
        Assert.Equal((long)i, row[0][0]);
        Assert.Equal(((char)('@' + i)).ToString(), row[0][1]);
        Assert.True(sqlStmt._isPrepared);
      }

      ValidatePreparedStatements(1, 8, "SELECT ? as number, ? as letter");
    }
  }
}
