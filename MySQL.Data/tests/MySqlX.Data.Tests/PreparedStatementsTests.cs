// Copyright (c) 2018, 2023, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    [SetUp]
    public void SetUp() => session.Reset();

    private void InitCollection()
    {
      Collection coll = CreateCollection(_collectionName);
      Result r = ExecuteAddStatement(coll.Add(_docs));
      Assert.AreEqual(4, r.AffectedItemsCount);
    }

    public void InitTable()
    {
      ExecuteSQL($"DROP TABLE IF EXISTS {_tableName}");
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
      if (!GetSession().SupportsPreparedStatements)
      {
        Console.Error.WriteLine("Prepared statements not supported.");
        return;
      }
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

      Assert.AreEqual(count, preparedStatements.Count);
      if (count > 0)
      {
        StringAssert.AreEqualIgnoringCase(sqlText, preparedStatements[0]["SQL_TEXT"].ToString());
        Assert.AreEqual(executions, preparedStatements[0]["COUNT_EXECUTE"]);
      }
    }

    private Collection GetCollection() => GetSession()
      .GetSchema(schemaName)
      .GetCollection(_collectionName);

    private Table GetTable() => GetSession()
      .GetSchema(schemaName)
      .GetTable(_tableName);

    [Test]
    public void Find()
    {
      InitCollection();
      Collection coll = GetCollection();
      var findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
      var doc = ExecuteFindStatement(findStmt);
      Assert.AreEqual("Book 1", doc.FetchAll()[0]["title"].ToString());
      Assert.False(findStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 1; i <= _docs.Length; i++)
      {
        doc = ExecuteFindStatement(findStmt.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
        Assert.AreEqual($"Book {i}", doc.FetchAll()[0]["title"].ToString());
        Assert.True(findStmt._isPrepared || !findStmt.Session.SupportsPreparedStatements);
      }

      ValidatePreparedStatements(1, 4,
        $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?");
    }

    [Test]
    public void FindWithChanges()
    {
      InitCollection();
      Collection coll = GetCollection();
      var findStmt = coll.Find("_id = 1");

      var foundDoc = ExecuteFindStatement(findStmt);
      Assert.AreEqual("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
      Assert.False(findStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      foundDoc = ExecuteFindStatement(findStmt.Limit(1));
      Assert.AreEqual("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
      Assert.True(findStmt._isPrepared || !findStmt.Session.SupportsPreparedStatements);

      ValidatePreparedStatements(1, 1,
        $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = 1) LIMIT ?, ?");

      for (int i = 1; i <= _docs.Length; i++)
      {
        var foundDoc2 = ExecuteFindStatement(findStmt.Where($"_id = {i}"));
        Assert.AreEqual($"Book {i}", foundDoc2.FetchAll()[0]["title"].ToString());
        Assert.False(findStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Test]
    public void DeallocatePreparedStatmentsWhenClosingSession()
    {
      InitCollection();
      string threadId;
      using (Session mySession = MySQLX.GetSession(ConnectionString))
      {
        mySession.SetCurrentSchema(schemaName);
        threadId = mySession.SQL("SELECT THREAD_ID FROM performance_schema.threads WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
        Collection coll = mySession.GetSchema(schemaName).GetCollection(_collectionName);

        var findStmt = coll.Find($"_id = 1");

        var foundDoc = ExecuteFindStatement(findStmt);
        Assert.AreEqual("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
        Assert.False(findStmt._isPrepared);
        ValidatePreparedStatements(0, 0, null, threadId);

        foundDoc = ExecuteFindStatement(findStmt.Limit(1));
        Assert.AreEqual("Book 1", foundDoc.FetchAll()[0]["title"].ToString());
        Assert.True(findStmt._isPrepared || !findStmt.Session.SupportsPreparedStatements);

        if (findStmt.Session.SupportsPreparedStatements)
        {
          ValidatePreparedStatements(1, 1,
            $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = 1) LIMIT ?, ?",
            threadId);
        }

      }
      ValidatePreparedStatements(0, 0, null, threadId);
    }

    [Test]
    public void Select()
    {
      InitTable();
      Table table = GetTable();
      var selectStmt = table.Select().Where("id = :id").Bind("id", 1);
      RowResult row = ExecuteSelectStatement(selectStmt);
      Assert.AreEqual(_allRows[0][1], row.FetchAll()[0]["name"].ToString());
      Assert.False(selectStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 0; i < _allRows.Length; i++)
      {
        row = ExecuteSelectStatement(selectStmt.Bind("id", i + 1).Limit(1));
        Assert.AreEqual(_allRows[i][1], row.FetchAll()[0]["name"].ToString());
        Assert.True(selectStmt._isPrepared || !selectStmt.Session.SupportsPreparedStatements);
      }

      ValidatePreparedStatements(1, 3,
        $"SELECT * FROM `{schemaName}`.`{_tableName}` WHERE (`id` = ?) LIMIT ?, ?");
    }

    [Test]
    public void SelectWithChanges()
    {
      InitTable();
      Table table = GetTable();
      var selectStmt = table.Select().Where("id = 1");

      RowResult row = ExecuteSelectStatement(selectStmt);
      Assert.AreEqual(_allRows[0][1], row.FetchAll()[0]["name"].ToString());
      Assert.False(selectStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      row = ExecuteSelectStatement(selectStmt.Limit(1));
      Assert.AreEqual(_allRows[0][1], row.FetchAll()[0]["name"].ToString());
      Assert.True(selectStmt._isPrepared || !selectStmt.Session.SupportsPreparedStatements);

      ValidatePreparedStatements(1, 1,
        $"SELECT * FROM `{schemaName}`.`{_tableName}` WHERE (`id` = 1) LIMIT ?, ?");

      for (int i = 2; i <= _allRows.Length; i++)
      {
        row = ExecuteSelectStatement(selectStmt.Where($"id = {i}"));
        Assert.AreEqual(_allRows[i - 1][1], row.FetchAll()[0]["name"].ToString());
        Assert.False(selectStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Test]
    public void Modify()
    {
      InitCollection();
      Collection coll = GetCollection();
      var modifyStmt = coll.Modify("_id = :id").Set("title", "Magazine 1").Bind("id", 1);
      Result result = ExecuteModifyStatement(modifyStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(modifyStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _docs.Length; i++)
      {
        result = ExecuteModifyStatement(modifyStmt.Bind("id", i).Limit(1));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.True(modifyStmt._isPrepared || !modifyStmt.Session.SupportsPreparedStatements);
      }

      ValidatePreparedStatements(1, 3,
        $"UPDATE `{schemaName}`.`{_collectionName}` SET doc=JSON_SET(JSON_SET(doc,'$.title','Magazine 1'),'$._id',JSON_EXTRACT(`doc`,'$._id')) WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT ?");
    }

    [Test]
    public void ModifyWithChanges()
    {
      InitCollection();
      Collection coll = GetCollection();
      var modifyStmt = coll.Modify("_id = :id").Set("title", "CONCAT('Magazine ', id)").Bind("id", 1);
      Result result = ExecuteModifyStatement(modifyStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(modifyStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteModifyStatement(modifyStmt.Bind("id", 2).Limit(1));
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.True(modifyStmt._isPrepared || !modifyStmt.Session.SupportsPreparedStatements);
      ValidatePreparedStatements(1, 1,
        $"UPDATE `{schemaName}`.`{_collectionName}` SET doc=JSON_SET(JSON_SET(doc,'$.title','CONCAT(\\'Magazine \\', id)'),'$._id',JSON_EXTRACT(`doc`,'$._id')) WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT ?");

      for (int i = 3; i <= _docs.Length; i++)
      {
        result = ExecuteModifyStatement(modifyStmt.Set("title", $"Magazine {i}").Bind("id", i));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.False(modifyStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Test]
    public void Update()
    {
      InitTable();
      Table table = GetTable();
      var updateStmt = table.Update().Where("id = :id").Set("name", "Magazine").Bind("id", 1);
      Result result = ExecuteUpdateStatement(updateStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(updateStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _allRows.Length; i++)
      {
        result = ExecuteUpdateStatement(updateStmt.Bind("id", i).Limit(1));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.True(updateStmt._isPrepared || !updateStmt.Session.SupportsPreparedStatements);
      }

      ValidatePreparedStatements(1, 2,
        $"UPDATE `{schemaName}`.`{_tableName}` SET `name`='Magazine' WHERE (`id` = ?) LIMIT ?");
    }

    [Test]
    public void UpdateWithChanges()
    {
      InitTable();
      Table table = GetTable();
      var updateStmt = table.Update().Where("id = :id").Set("name", "CONCAT('Magazine ', id)").Bind("id", 1);
      Result result = ExecuteUpdateStatement(updateStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(updateStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteUpdateStatement(updateStmt.Bind("id", 2).Limit(1));
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.True(updateStmt._isPrepared || !updateStmt.Session.SupportsPreparedStatements);
      ValidatePreparedStatements(1, 1,
        $"UPDATE `{schemaName}`.`{_tableName}` SET `name`=CONCAT('Magazine ',`id`) WHERE (`id` = ?) LIMIT ?");

      for (int i = 3; i <= _allRows.Length; i++)
      {
        result = ExecuteUpdateStatement(updateStmt.Set("name", $"Magazine {i}").Bind("id", i));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.False(updateStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Test]
    public void Remove()
    {
      InitCollection();
      Collection coll = GetCollection();
      var removeStmt = coll.Remove("_id = :id").Bind("id", 1);
      Result result = ExecuteRemoveStatement(removeStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(removeStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _docs.Length; i++)
      {
        result = ExecuteRemoveStatement(removeStmt.Bind("id", i).Limit(1));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.True(removeStmt._isPrepared || !removeStmt.Session.SupportsPreparedStatements);
      }

      ValidatePreparedStatements(1, 3,
        $"DELETE FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT ?");
    }

    [Test]
    public void RemoveWithChanges()
    {
      InitCollection();
      Collection coll = GetCollection();
      var removeStmt = coll.Remove("_id = :id").Bind("id", 1);
      Result result = ExecuteRemoveStatement(removeStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(removeStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteRemoveStatement(removeStmt.Bind("id", 2).Limit(1));
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.True(removeStmt._isPrepared || !removeStmt.Session.SupportsPreparedStatements);
      ValidatePreparedStatements(1, 1,
        $"DELETE FROM `{schemaName}`.`{_collectionName}` WHERE (JSON_EXTRACT(doc,'$._id') = ?) LIMIT ?");

      for (int i = 3; i <= _docs.Length; i++)
      {
        result = ExecuteRemoveStatement(removeStmt.Where($"_id = {i}"));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.False(removeStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Test]
    public void Delete()
    {
      InitTable();
      Table table = GetTable();
      var deleteStmt = table.Delete().Where("id = :id").Bind("id", 1);
      Result result = ExecuteDeleteStatement(deleteStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(deleteStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      for (int i = 2; i <= _allRows.Length; i++)
      {
        result = ExecuteDeleteStatement(deleteStmt.Bind("id", i).Limit(1));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.True(deleteStmt._isPrepared || !deleteStmt.Session.SupportsPreparedStatements);
      }

      ValidatePreparedStatements(1, 2,
        $"DELETE FROM `{schemaName}`.`{_tableName}` WHERE (`id` = ?) LIMIT ?");
    }

    [Test]
    public void DeleteWithChanges()
    {
      InitTable();
      Table table = GetTable();
      var deleteStmt = table.Delete().Where("id = :id").Bind("id", 1);
      Result result = ExecuteDeleteStatement(deleteStmt);
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.False(deleteStmt._isPrepared);
      ValidatePreparedStatements(0, 0, null);

      result = ExecuteDeleteStatement(deleteStmt.Bind("id", 2).Limit(1));
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      Assert.True(deleteStmt._isPrepared || !deleteStmt.Session.SupportsPreparedStatements);
      ValidatePreparedStatements(1, 1,
        $"DELETE FROM `{schemaName}`.`{_tableName}` WHERE (`id` = ?) LIMIT ?");

      for (int i = 3; i <= _allRows.Length; i++)
      {
        result = ExecuteDeleteStatement(deleteStmt.Where($"id = {i}"));
        Assert.AreEqual(1ul, result.AffectedItemsCount);
        Assert.False(deleteStmt._isPrepared);
      }

      ValidatePreparedStatements(0, 0, null);
    }

    [Test]
    public void MaxPreparedStmtCount()
    {
      InitCollection();
      Collection coll = GetCollection();
      try
      {
        ((Session)coll.Session).SQL("SET GLOBAL max_prepared_stmt_count=0").Execute();
        var findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
        var doc = ExecuteFindStatement(findStmt);
        Assert.AreEqual("Book 1", doc.FetchAll()[0]["title"].ToString());
        Assert.False(findStmt._isPrepared);
        Assert.True(findStmt.Session.SupportsPreparedStatements);
        ValidatePreparedStatements(0, 0, null);

        doc = ExecuteFindStatement(findStmt.Bind("id", 2).Bind("pages", 30).Limit(1));
        Assert.AreEqual($"Book 2", doc.FetchAll()[0]["title"].ToString());
        Assert.False(findStmt._isPrepared);
        Assert.False(findStmt.Session.SupportsPreparedStatements);

        doc = ExecuteFindStatement(findStmt.Bind("id", 3).Bind("pages", 40).Limit(1));
        Assert.AreEqual($"Book 3", doc.FetchAll()[0]["title"].ToString());
        Assert.False(findStmt._isPrepared);
        Assert.False(findStmt.Session.SupportsPreparedStatements);
      }
      finally
      {
        ((Session)coll.Session).SQL("SET GLOBAL max_prepared_stmt_count=16382").Execute();
      }
    }

    [Test]
    public void LimitAndOffset()
    {
      InitCollection();
      Collection coll = GetCollection();

      // first execution (normal)
      var findStmt = coll.Find("pages >= :lower AND pages <= :upper").Bind("lower", 20).Bind("upper", 20);
      var result = ExecuteFindStatement(findStmt).FetchAll();
      Assert.That(result, Has.One.Items);
      Assert.AreEqual("Book 1", result[0]["title"].ToString());
      Assert.False(findStmt._isPrepared);

      ValidatePreparedStatements(0, 0, null);

      // second execution adding limit (prepared statement)
      result = ExecuteFindStatement(findStmt.Bind("lower", 0).Bind("upper", 100).Limit(1)).FetchAll();
      Assert.That(result, Has.One.Items);
      Assert.AreEqual($"Book 1", result[0]["title"].ToString());
      Assert.True(findStmt._isPrepared);

      ValidatePreparedStatements(1, 1,
        $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_EXTRACT(doc,'$.pages') >= ?) AND (JSON_EXTRACT(doc,'$.pages') <= ?)) LIMIT ?, ?");

      // new execution using a different limit and offset (prepared statement)
      result = ExecuteFindStatement(findStmt.Bind("lower", 0).Bind("upper", 100).Limit(2).Offset(1)).FetchAll();
      Assert.AreEqual(2, result.Count);
      Assert.AreEqual($"Book 2", result[0]["title"].ToString());
      Assert.AreEqual($"Book 3", result[1]["title"].ToString());
      Assert.True(findStmt._isPrepared || !findStmt.Session.SupportsPreparedStatements);

      ValidatePreparedStatements(1, 2,
        $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_EXTRACT(doc,'$.pages') >= ?) AND (JSON_EXTRACT(doc,'$.pages') <= ?)) LIMIT ?, ?");

      // execution without limit and offset but persisting previous values (prepared statement)
      result = ExecuteFindStatement(findStmt.Bind("lower", 0).Bind("upper", 100)).FetchAll();
      Assert.AreEqual(2, result.Count);
      Assert.True(findStmt._isPrepared);

      ValidatePreparedStatements(1, 3,
        $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_EXTRACT(doc,'$.pages') >= ?) AND (JSON_EXTRACT(doc,'$.pages') <= ?)) LIMIT ?, ?");
    }

    #region WL14389

    [Test, Description("Validate prepared statements for connection pooling")]
    public void ConnectionPoolingTest()
    {
      string connectionID1 = null;
      Session session1, session2 = null;
      Client client1 = null;
      using (client1 = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { maxSize = 1, queueTimeout = 2000 } }))
      {
        session1 = client1.GetSession();
        session1.DropSchema(schemaName);
        session1.CreateSchema(schemaName);
        InitCollection();
        var s = session1.GetSchema(schemaName);
        var col = GetCollection();

        var res0 = session1.SQL("SELECT CONNECTION_ID()").Execute();
        if (res0.HasData)
        {
          var row = res0.FetchOne();
          connectionID1 = row[0].ToString();
          Assert.IsNotEmpty(connectionID1);
        }
        var findStmt = col.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
        var doc = ExecuteFindStatement(findStmt);
        Assert.AreEqual("Book 1", doc.FetchAll()[0]["title"].ToString());
        ValidatePreparedStatements(0, 0, null);
        for (int i = 1; i < _docs.Length; i++)
        {
          doc = ExecuteFindStatement(findStmt.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
          Assert.AreEqual($"Book {i}", doc.FetchAll()[0]["title"].ToString());
        }
        ValidatePreparedStatements(1, 3,
          $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?");
        session1.Close();

        session2 = client1.GetSession();
        res0 = session2.SQL("SELECT CONNECTION_ID()").Execute();
        if (res0.HasData)
        {
          var row = res0.FetchOne();
          connectionID1 = row[0].ToString();
          Assert.IsNotEmpty(connectionID1);
        }
        s = session2.GetSchema(schemaName);
        col = s.GetCollection(_collectionName);
        findStmt = col.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
        doc = ExecuteFindStatement(findStmt);
        Assert.AreEqual("Book 1", doc.FetchAll()[0]["title"].ToString());
        ValidatePreparedStatements(0, 0, null, connectionID1);
        for (int i = 1; i < _docs.Length; i++)
        {
          doc = ExecuteFindStatement(findStmt.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
          Assert.AreEqual($"Book {i}", doc.FetchAll()[0]["title"].ToString());
        }
        ValidatePreparedStatements(1, 3,
          $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?");
        session2.Close();
        Assert.Throws<MySqlException>(() => doc = ExecuteFindStatement(findStmt));
        session2 = client1.GetSession();
        res0 = session2.SQL("SELECT CONNECTION_ID()").Execute();
        if (res0.HasData)
        {
          var row = res0.FetchOne();
          connectionID1 = row[0].ToString();
          Assert.IsNotEmpty(connectionID1);
        }
        s = session2.GetSchema(schemaName);
        col = s.GetCollection(_collectionName);
        findStmt = col.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
        doc = ExecuteFindStatement(findStmt);
        Assert.AreEqual("Book 1", doc.FetchAll()[0]["title"].ToString());
        ValidatePreparedStatements(0, 0, null, connectionID1);
        for (int i = 1; i < _docs.Length; i++)
        {
          doc = ExecuteFindStatement(findStmt.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
          Assert.AreEqual($"Book {i}", doc.FetchAll()[0]["title"].ToString());
        }
        ValidatePreparedStatements(1, 3,
          $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?");
        session2.Close();
        Assert.Throws<MySqlException>(() => doc = ExecuteFindStatement(findStmt));
      }
    }

    [Test, Description("Deallocate PreparedStatments When Closing Session Load-100 times")]
    public void DeallocatePreparedStatmentsWhenClosingSessionLoad()
    {
      if (!Platform.IsWindows()) Assert.Ignore("Check for Linux OS");

      InitCollection();
      string threadId;
      for (int k = 0; k < 100; k++)
      {
        using (Session mySession = MySQLX.GetSession(ConnectionString))
        {
          mySession.SetCurrentSchema(schemaName);
          threadId = mySession.SQL("SELECT THREAD_ID FROM performance_schema.threads " +
              "WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
          Collection coll = mySession.GetSchema(schemaName).GetCollection(_collectionName);
          var findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
          var doc = ExecuteFindStatement(findStmt);
          Assert.AreEqual("Book 1", doc.FetchAll()[0]["title"].ToString());
          ValidatePreparedStatements(0, 0, null, threadId);

          for (int i = 1; i <= _docs.Length; i++)
          {
            doc = ExecuteFindStatement(findStmt.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
            Assert.AreEqual($"Book {i}", doc.FetchAll()[0]["title"].ToString());
          }
          ValidatePreparedStatements(1, 4,
            $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?", threadId);
        }
        ValidatePreparedStatements(0, 0, null, threadId);
      }
    }

    [Test, Description("Deallocate PreparedStatments When Closing Session Parallel")]
    public void DeallocatePreparedStatmentsWhenClosingSessionParallel()
    {
      InitCollection();
      string threadId1, threadId2;

      Session mySession1 = MySQLX.GetSession(ConnectionString);
      Session mySession2 = MySQLX.GetSession(ConnectionString);
      mySession1.SetCurrentSchema(schemaName);
      mySession2.SetCurrentSchema(schemaName);
      threadId1 = mySession1.SQL("SELECT THREAD_ID FROM performance_schema.threads " +
          "WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
      threadId2 = mySession2.SQL("SELECT THREAD_ID FROM performance_schema.threads " +
          "WHERE PROCESSLIST_ID=CONNECTION_ID()").Execute().FetchOne()[0].ToString();
      Collection coll1 = mySession1.GetSchema(schemaName).GetCollection(_collectionName);
      Collection coll2 = mySession2.GetSchema(schemaName).GetCollection(_collectionName);
      var findStmt1 = coll1.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
      var findStmt2 = coll2.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
      var doc1 = ExecuteFindStatement(findStmt1);
      Assert.AreEqual("Book 1", doc1.FetchAll()[0]["title"].ToString());
      ValidatePreparedStatements(0, 0, null, threadId1);

      var doc2 = ExecuteFindStatement(findStmt2);
      Assert.AreEqual("Book 1", doc2.FetchAll()[0]["title"].ToString());
      ValidatePreparedStatements(0, 0, null, threadId2);

      for (int i = 1; i <= _docs.Length; i++)
      {
        doc1 = ExecuteFindStatement(findStmt1.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
        Assert.AreEqual($"Book {i}", doc1.FetchAll()[0]["title"].ToString());
      }

      for (int i = 1; i <= _docs.Length; i++)
      {
        doc2 = ExecuteFindStatement(findStmt2.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
        Assert.AreEqual($"Book {i}", doc2.FetchAll()[0]["title"].ToString());
      }

      ValidatePreparedStatements(1, 4,
          $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?", threadId1);
      ValidatePreparedStatements(1, 4,
          $"SELECT doc FROM `{schemaName}`.`{_collectionName}` WHERE ((JSON_UNQUOTE(JSON_EXTRACT(doc,'$._id')) = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?", threadId2);

      mySession1.Close();
      mySession2.Close();
      ValidatePreparedStatements(0, 0, null, threadId1);
      ValidatePreparedStatements(0, 0, null, threadId2);
    }

    [Test, Description("Max PreparedStatment Count set to one")]
    public void MaxPreparedStmtCountAsOne()
    {
      Collection coll = CreateCollection("testGlobal");
      Result r = ExecuteAddStatement(coll.Add(_docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      try
      {
        ((Session)coll.Session).SQL("SET GLOBAL max_prepared_stmt_count=1").Execute();
        var findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
        var doc = ExecuteFindStatement(findStmt);
        Assert.AreEqual("Book 1", doc.FetchAll()[0]["title"].ToString());

        ValidatePreparedStatements(0, 0, null);

        for (int i = 1; i <= _docs.Length; i++)
        {
          doc = ExecuteFindStatement(findStmt.Bind("id", i).Bind("pages", i * 10 + 10).Limit(1));
          Assert.AreEqual($"Book {i}", doc.FetchAll()[0]["title"].ToString());
        }
        ValidatePreparedStatements(1, 4,
            $"SELECT doc FROM `{schemaName}`.`testGlobal` WHERE ((JSON_EXTRACT(doc,'$._id') = ?) AND (JSON_EXTRACT(doc,'$.pages') = ?)) LIMIT ?, ?");

        findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", 1).Bind("pages", 20);
        ExecuteFindStatement(findStmt).FetchAll();
        ExecuteFindStatement(findStmt).FetchAll();
        ValidatePreparedStatements(0, 0, null);
      }
      finally
      {
        ((Session)coll.Session).SQL("SET GLOBAL max_prepared_stmt_count=16382").Execute();
      }
    }

    [Test, Description("PS with Async")]
    public void PSMultipleFindAsync()
    {
      var coll = CreateCollection("test");
      int docs = 100;
      HashSet<string> validator = new HashSet<string>();
      var addStatement = coll.Add(new { id = 1, age = 1, _id = 1 });

      for (int i = 2; i <= docs; i++)
      {
        addStatement.Add(new
        {
          id = i,
          age = i,
          _id = i
        });
      }
      var result = ExecuteAddStatement(addStatement);

      List<Task> tasksList = new List<Task>();
      var findStmt = coll.Find("age = 1");
      tasksList.Add(findStmt.ExecuteAsync().ContinueWith((findResult) =>
      {
        Assert.AreEqual(1, findResult.Result.FetchAll()[0]["age"]);
      }));
      tasksList.Add(findStmt.ExecuteAsync().ContinueWith((findResult) =>
      {
        Assert.AreEqual(1, findResult.Result.FetchAll()[0]["age"]);
      }));
      Assert.AreEqual(true, Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      ValidatePreparedStatements(1, 1, $"SELECT doc FROM `test`.`test` WHERE (JSON_EXTRACT(doc,'$.age') = 1)");

    }

    /// <summary>
    ///   Bug 29304767
    /// </summary>
    [Test, Description("BIND WITH FIND/REMOVE DOESN'T WORK WHEN STRING IS PASSED-WL#12174")]
    public void FindRemoveWithString()
    {
      InitCollection();
      Collection coll = GetCollection();
      Assert.AreEqual(4, coll.Count());
      var findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", "1").Bind("pages", 20);
      var doc = ExecuteFindStatement(findStmt);
      findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", "2").Bind("pages", 30);
      doc = ExecuteFindStatement(findStmt);
      var remStatement = coll.Remove("$._id == :id").Sort().Limit(2);
      for (int i = 1; i < 4; i++)
      {
        var r1 = remStatement.Bind("id", i.ToString()).Execute();
      }
      findStmt = coll.Find("_id = :id").Bind("id", "4");
      var docCount = ExecuteFindStatement(findStmt).FetchAll();
      Assert.AreEqual(1, docCount.Count, "There should be a record");
      Assert.AreEqual("Book 4", docCount[0]["title"].ToString());
      findStmt = coll.Find("_id = :id").Bind("id", "1");
      docCount = ExecuteFindStatement(findStmt).FetchAll();
      Assert.AreEqual(0, docCount.Count, "There should not be any records");
    }

    /// <summary>
    ///   Bug 29311658
    /// </summary>
    [Test, Description("STATEMENT WITH OFFSET FAILS WHEN EXECUTED FOR THE SECOND TIME-WL#12174")]
    public void StatementWithOffset()
    {

      var coll = CreateCollection("test");
      ExecuteSQLStatement(session.SQL("SET sql_mode='';"));
      var docs1 = new[]
      {
        new { _id = 11, name = "jonh doe", age =38,profit = 100 },
        new { _id = 12, name = "milton green", age =45,profit = 200 },
        new { _id = 13, name = "larry smith", age =24,profit = 300},
        new { _id = 14, name = "mary weinstein", age= 24 ,profit = 100},
        new { _id = 15, name = "jerry pratt", age =45 ,profit = 400 },
        new { _id = 16, name = "hugh jackman", age =20,profit = 500},
        new { _id = 117, name = "elizabeth olsen",age = 31,profit = 300 },
        new { _id = 8, name = "tommy h", age =31,profit = 3000}
      };
      var r = coll.Add(docs1).Execute();
      Assert.AreEqual((ulong)8, r.AffectedItemsCount);
      var findStmt = coll.Find().Fields("_id as ID", "name as Name",
          "age as Age", "profit as Profit").GroupBy("age").
          GroupBy("profit").Sort("profit ASC").Limit(3);
      ExecuteFindStatement(findStmt).FetchAll();
      ValidatePreparedStatements(0, 0, null);
      ExecuteFindStatement(findStmt).FetchAll();
      ExecuteFindStatement(findStmt).FetchAll();
      ValidatePreparedStatements(1, 2, "SELECT JSON_OBJECT('ID', `_DERIVED_TABLE_`.`ID`,'Name', `_DERIVED_TABLE_`.`Name`,'Age', `_DERIVED_TABLE_`.`Age`," +
          "'Profit', `_DERIVED_TABLE_`.`Profit`) AS doc FROM (SELECT JSON_EXTRACT(doc,'$._id') AS `ID`,JSON_EXTRACT(doc,'$.name') " +
          "AS `Name`,JSON_EXTRACT(doc,'$.age') AS `Age`,JSON_EXTRACT(doc,'$.profit') AS `Profit` FROM `test`.`Books` GROUP BY `profit` " +
          "ORDER BY JSON_EXTRACT(doc,'$.profit') LIMIT ?, ?) AS `_DERIVED_TABLE_`");

      findStmt = coll.Find().Fields("_id as ID", "name as Name",
          "age as Age", "profit as Profit").GroupBy("age").
          GroupBy("profit").Sort("profit ASC").Offset(0);
      ExecuteFindStatement(findStmt).FetchAll();
      ValidatePreparedStatements(0, 0, null);
      ExecuteFindStatement(findStmt).FetchAll();
      ExecuteFindStatement(findStmt).FetchAll();
      ValidatePreparedStatements(1, 2, "SELECT JSON_OBJECT('ID', `_DERIVED_TABLE_`.`ID`,'Name', `_DERIVED_TABLE_`.`Name`,'Age', `_DERIVED_TABLE_`.`Age`," +
          "'Profit', `_DERIVED_TABLE_`.`Profit`) AS doc FROM (SELECT JSON_EXTRACT(doc,'$._id') AS `ID`,JSON_EXTRACT(doc,'$.name') " +
          "AS `Name`,JSON_EXTRACT(doc,'$.age') AS `Age`,JSON_EXTRACT(doc,'$.profit') AS `Profit` FROM `test`.`Books` GROUP BY `profit` " +
          "ORDER BY JSON_EXTRACT(doc,'$.profit')) AS `_DERIVED_TABLE_`");


      findStmt = coll.Find().Fields("_id as ID", "name as Name",
          "age as Age", "profit as Profit").GroupBy("age").
          GroupBy("profit").Sort("profit ASC").Limit(3).Offset(2);
      ExecuteFindStatement(findStmt).FetchAll();
      ValidatePreparedStatements(0, 0, null);

      ExecuteFindStatement(findStmt).FetchAll();
      ExecuteFindStatement(findStmt.Offset(0)).FetchAll();
      ValidatePreparedStatements(1, 2, "SELECT JSON_OBJECT('ID', `_DERIVED_TABLE_`.`ID`,'Name', `_DERIVED_TABLE_`.`Name`,'Age', " +
          "`_DERIVED_TABLE_`.`Age`,'Profit', `_DERIVED_TABLE_`.`Profit`) AS doc FROM (SELECT JSON_EXTRACT(doc,'$._id') AS `ID`," +
          "JSON_EXTRACT(doc,'$.name') AS `Name`,JSON_EXTRACT(doc,'$.age') " +
          "AS `Age`,JSON_EXTRACT(doc,'$.profit') AS `Profit` FROM `test`.`Books` GROUP BY `profit` ORDER BY JSON_EXTRACT(doc,'$.profit') LIMIT ?, ?) AS `_DERIVED_TABLE_`");

      ExecuteSQLStatement(session.SQL("set sql_mode='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';"));

    }

    /// <summary>
    ///  Bug 29249857
    /// </summary>
    [Test, Description("SESSION.SQL STATEMENT EXECUTION FAILS WHEN EXECUTED FOR SECOND TIME WITH BIND-WL#12174")]
    public void SessionSqlStatementFail()
    {
      InitTable();
      var sqlStmt = session.SQL(@"INSERT INTO test.BookTable VALUES(8, 'name 8', 40)");
      for (int i = 0; i < 100; i++)
      {
        sqlStmt.Execute();
      }
      ValidatePreparedStatements(0, 0, null);
      ExecuteSQL("DROP TABLE if EXISTS test.BookTable");
      InitTable();
      sqlStmt = session.SQL(@"INSERT INTO test.BookTable VALUES(8, 'name 8', 40)");
      sqlStmt.Execute();
      sqlStmt.Execute();
      ExecuteSQL("DROP TABLE if EXISTS test.test1");
      ExecuteSQL("CREATE TABLE test.test1(id INT, letter varchar(1))");
      var statment = session.SQL("INSERT INTO test.test1 VALUES(1, ?), (2, 'B');").Bind(1);
      statment.Execute();
      sqlStmt.Execute();
      ValidatePreparedStatements(0, 0, null);
    }

    /// <summary>
    ///   Bug 29347028
    /// </summary>
    [Test, Description("DELETE WHERE THROWS PARSE EXCEPTION WITH IN OPERATOR FOR ARRAY")]
    public void DeleteParseException()
    {
      if (!session.Version.isAtLeast(8, 0, 16)) Assert.Ignore("This test is for MySql 8.0.16 or higher.");
      var tableName = "newtable";
      Session session1 = null;
      Client client1 = null;
      using (client1 = MySQLX.GetClient(ConnectionStringUri, new { pooling = new { maxSize = 1, queueTimeout = 2000 } }))
      {
        session1 = client1.GetSession();
        session1.DropSchema(schemaName);
        session1.CreateSchema(schemaName);
        var s = session1.GetSchema(schemaName);

        session1.SQL($"create table `{schemaName}`.`{tableName}`(id JSON, n JSON, a JSON, info JSON)").Execute();
        Table tabNew = s.GetTable(tableName);
        tabNew.Insert().
            Values("{\"_id\":101}", "{\"name\":\"joy\"}", "{\"age\":21}", "{\"additionalinfo\":{\"company\":\"xyz\",\"vehicle\":\"bike\",\"hobbies\":\"reading\"}}").
            Values("{\"_id\":102}", "{\"name\":\"happy\"}", "{\"age\":24}", "{\"additionalinfo\":{\"company\":\"abc\",\"vehicle\":\"car\",\"hobbies\":[\"playing\",\"painting\",\"boxing\"]}}").
            Execute();

        var tDelete = tabNew.Delete().Where("[\"playing\", \"painting\",\"boxing\"] IN info->$.additionalinfo.hobbies");
        tDelete.Execute();

        tableName = "newtable1";
        session1.SQL($"create table `{schemaName}`.`{tableName}`(c1 varchar(256), c2 JSON)").Execute();
        tabNew = s.GetTable(tableName);
        tabNew.Insert("c1", "c2").Values("12345", "{ \"name\": \"abc\", \"age\": 1 , \"misc\": 1.2}").Execute();
        tabNew.Insert("c1", "c2").Values("123456", "{ \"name\": \"abc\", \"age\": 2 , \"misc\": 1.3}").Execute();
        tabNew.Insert("c1", "c2").Values("1234567", "{ \"name\": \"abc\", \"age\": 3 , \"misc\": 1.4}").Execute();

        tDelete = tabNew.Delete().Where(":C2 in c2->$.name and :C1 = c1");
        tDelete.Bind("C1", "123456");
        tDelete.Bind("C2", "abc");
        tDelete.Execute();
        session1.Close();
      }

    }

    /// <summary>
    ///  Bug 29346856
    /// </summary>
    [Test, Description("SECOND FIND/SELECT FAILS WITH IN OPERATOR-WL#12174-TS1")]
    public void SecondFindFails()
    {
      var collectionName = "newcollection";
      var t1 = "{\"_id\": \"1001\", \"ARR\":[1,2,3], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      var t2 = "{\"_id\": \"1002\", \"ARR\":[1,1,2], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      var t3 = "{\"_id\": \"1003\", \"ARR\":[1,4,5], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      var testCollection = CreateCollection(collectionName);
      testCollection.Add(t1).Execute();
      testCollection.Add(t2).Execute();
      testCollection.Add(t3).Execute();
      var findStatement = testCollection.Find("(1+2) in (1, 2, 3)");
      findStatement.Execute().FetchAll();
      findStatement.Execute().FetchAll();

      var table1 = testSchema.GetCollectionAsTable(collectionName);
      var selectStatement = table1.Select().Where("(1+2) in (1, 2, 3)");
      selectStatement.Execute().FetchAll();
      selectStatement.Execute().FetchAll();
    }

    #endregion

  }
}
