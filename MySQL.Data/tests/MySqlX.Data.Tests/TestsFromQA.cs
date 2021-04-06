// Copyright (c) 2021, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using NUnit.Framework;
using System;
using System.Linq;

namespace MySqlX.Data.Tests
{
  public class TestsFromQA : BaseTest
  {
    protected string connStr = BaseTest.ConnectionString + ";ssl-mode=NONE;";

    [Test]
    public void QARegressionTestScenario1()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();

      var test = session.GetSchema("test");
      testSchema.CreateCollection("test");
      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(connStr))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var coll = session.GetSchema("test").GetCollection("test");

        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
                      new {_id = 1, a = 1},
                      new {_id = 2, a = 1},
                      new {_id = 3, a = 1}
                  };
        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");
        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id = 1").LockExclusive().Execute();
        var resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("START TRANSACTION").Execute();

        docResult = coll2.Find("_id = 2").LockShared().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();

        Assert.Throws<MySqlException>(() => coll2.Find("_id = 1").LockShared().Execute());
        //Should have thrown exception
        // Session unlocks documents.
        session.SQL("ROLLBACK").Execute();
        docResult = coll2.Find("_id = 1").LockShared().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
        docResult = coll.Find("_id = 1").LockShared().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("ROLLBACK").Execute();
      }

    }

    [Test]
    public void QARegressionTestScenario2()
    {
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();

      var test = session.GetSchema("test");
      testSchema.CreateCollection("test");
      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(connStr))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var coll = session.GetSchema("test").GetCollection("test");
        // coll.CreateIndex("myIndex", true).Field("$._id", "INT", true).Execute();
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
                      new {_id = 1, a = 1},
                      new {_id = 2, a = 1},
                      new {_id = 3, a = 1}
                  };
        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");
        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id = 1").LockShared().Execute();
        var resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document IDs");
        docResult = coll.Find("_id = 3").LockShared().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document IDs");
        session2.SQL("START TRANSACTION").Execute();

        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
        // Should return immediately due to LockShared() allows reading by other sessions.
        docResult = coll2.Find("_id = 2").LockShared().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Session1 blocks due to LockExclusive() not allowing to read locked documents.
        coll2.Find("_id = 2").LockExclusive().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");

        //Should throw exception
        Assert.Throws<MySqlException>(() => coll2.Find("_id = 1").LockExclusive().Execute());
        Assert.Throws<MySqlException>(() => coll2.Modify("_id = 1").Set("a", 100).Execute());


        // Session unlocks documents.
        session2.SQL("ROLLBACK").Execute();
        session.SQL("ROLLBACK").Execute();
      }

    }

    [Test]
    public void QARegressionTestScenario3()
    {
      if (session == null)
      {
        session = MySQLX.GetSession(connStr);
      }
      if (!(session.InternalSession.GetServerVersion().isAtLeast(8, 0, 23))) Assert.Ignore();

      var test = session.GetSchema("test");
      testSchema.CreateCollection("test");
      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(connStr))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var coll = session.GetSchema("test").GetCollection("test");

        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
                      new {_id = 1, a = 1},
                      new {_id = 2, a = 1},
                      new {_id = 3, a = 1}
                  };
        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");
        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id = 1").LockExclusive().Execute();
        var resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document IDs");

        session2.SQL("START TRANSACTION").Execute();

        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");

        session.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Assert.Throws<MySqlException>(() => coll.Find("_id = 2").LockExclusive().Execute());
        //TestSetup.Log_Failure("Should have thrown exception");

        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Session1 blocks due to LockExclusive() not allowing to read locked documents.
        Assert.Throws<MySqlException>(() => coll2.Find("_id = 1").LockExclusive().Execute());
        //TestSetup.Log_Failure("Should have thrown exception");

        // Session unlocks documents.
        session2.SQL("ROLLBACK").Execute();
        docResult = coll.Find("_id = 2").LockExclusive().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
        session.SQL("ROLLBACK").Execute();
        docResult = coll2.Find("_id = 1").LockExclusive().Execute();
        resCount = docResult.FetchAll().Count;
        Assert.AreEqual(1, resCount);
        //TestSetup.LogObj.DoCommand(1, docResult.FetchAll().Count, "Matching the document ID");
      }

    }

    [Test]
    public void QARegressionTestScenario4()
    {
      try
      {
        string connectionString = "server=localhost;user=test;port=33060;password=test;ssl-mode=none;database=test;";
        Session testsession = MySQLX.GetSession(connectionString);
        var db = testsession.GetSchema("test1");
        if (db.ExistsInDatabase())
        {
          testsession.DropSchema("test1");
          db = testsession.CreateSchema("test1");
        }
        else { db = testsession.CreateSchema("test1"); }
        var col = db.GetCollection("my_collection");
        if (col.ExistsInDatabase())
        {
          db.DropCollection("my_collection");
          col = db.CreateCollection("my_collection");
        }
        else { col = db.CreateCollection("my_collection"); }
        col = db.GetCollection("my_collection", true);
        testsession.StartTransaction();
        //col.Add(new { name = "Sakila", age = 16 }).Execute();
        object[] data = new object[]
        {
                new {  _id = 1, title = "Book 1", pages = 30 },
                new {  _id = 2, title = "Book 2", pages = 50 },};
        var result = col.Add(data).Execute();
        var sp = testsession.SetSavepoint("SavePoint1");
        data = new object[]
        {
                new {  _id = 3, title = "Book 3", pages = 30 },
                new {  _id = 4, title = "Book 4", pages = 50 },};
        result = col.Add(data).Execute();
        testsession.RollbackTo(sp);
        var doc = col.Find().Execute();
        var docs = doc.FetchAll().Count();
        testsession.Close();
        testsession.Dispose();
      }
      catch (MySqlException ex)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw;
      }
    }

  }
}
