// Copyright (c) 2015, 2020, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using NUnit.Framework;

namespace MySqlX.Data.Tests
{
  public class BasicFindTests : BaseTest
  {
    [Test]
    public void SimpleFind()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      DocResult foundDocs = ExecuteFindStatement(coll.Find("pages > 20"));
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 2");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 3");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 4");
      Assert.False(foundDocs.Next());
    }

    [Test]
    public void SimpleFindWithSort()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      DocResult foundDocs = ExecuteFindStatement(coll.Find("pages > 20").Sort("pages DESC"));
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 4");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 3");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 2");
      Assert.False(foundDocs.Next());
    }

    [Test]
    public void SimpleFindWithLimitAndOffset()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      DocResult foundDocs = ExecuteFindStatement(coll.Find("pages > 20").Limit(1));
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 2");
      Assert.False(foundDocs.Next());

      var resultDocs = ExecuteFindStatement(coll.Find("pages > 20").Offset(1).Limit(2)).FetchAll();
      Assert.AreEqual(40, resultDocs[0]["pages"]);
      Assert.AreEqual(50, resultDocs[1]["pages"]);

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteFindStatement(coll.Find().Limit(0)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteFindStatement(coll.Find().Limit(-1)));
    }

    [Test]
    public void FindConditional()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      DocResult foundDocs = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40));
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"].ToString() == "Book 3");
      Assert.False(foundDocs.Next());
    }

    [Test]
    public void BindDbDoc()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      DbDoc docParams = new DbDoc(new { pages1 = 30, pages2 = 40 });
      DocResult foundDocs = ExecuteFindStatement(coll.Find("pages = :Pages1 || pages = :Pages2").Bind(docParams));
      Assert.True(foundDocs.Next());
      Assert.AreEqual("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.AreEqual("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Test]
    public void BindJsonAsAnonymous()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      var jsonParams = new { pages1 = 30, pages2 = 40 };
      DocResult foundDocs = ExecuteFindStatement(coll.Find("pages = :Pages1 || pages = :Pages2").Bind(jsonParams));
      Assert.True(foundDocs.Next());
      Assert.AreEqual("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.AreEqual("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Test]
    public void BindJsonAsString()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      var jsonParams = "{ \"pages1\" : 30, \"pages2\" : 40 }";
      DocResult foundDocs = ExecuteFindStatement(coll.Find("pages = :Pages1 || pages = :Pages2").Bind(jsonParams));
      Assert.True(foundDocs.Next());
      Assert.AreEqual("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.AreEqual("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Test]
    public void RowLockingNotSupportedInOlderVersions()
    {
      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      Collection coll = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll.Find().LockShared()));
      Assert.AreEqual("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);

      ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll.Find().LockExclusive()));
      Assert.AreEqual("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);
    }

    [Test]
    public void SimpleSharedLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        var docs = new[]
        {
          new {  _id = 1, a = 1 },
          new {  _id = 2, a = 1 },
          new {  _id = 3, a = 1 },
        };
        ExecuteAddStatement(coll.Add(docs));
        Collection coll2 = session2.GetSchema("test").GetCollection("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        DocResult docResult = ExecuteFindStatement(coll.Find("_id = 1").LockShared());
        Assert.That(docResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since document isn't locked.
        docResult = ExecuteFindStatement(coll2.Find("_id = 2").LockShared());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        // Should return immediately due to LockShared() allows reading by other sessions.
        docResult = ExecuteFindStatement(coll2.Find("_id = 1").LockShared());
        Assert.That(docResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void SimpleExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
        var docs = new[]
        {
          new {  _id = 1, a = 1 },
          new {  _id = 2, a = 1 },
          new {  _id = 3, a = 1 },
        };
        ExecuteAddStatement(coll.Add(docs));
        Collection coll2 = session2.GetSchema("test").GetCollection("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        DocResult docResult = ExecuteFindStatement(coll.Find("_id = 1").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since document isn't locked.
        docResult = ExecuteFindStatement(coll2.Find("_id = 2").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive()));
        Assert.AreEqual("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void SharedLockForbidsToModifyDocuments()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        var docs = new[]
        {
          new {  _id = 1, a = 1 },
          new {  _id = 2, a = 1 },
          new {  _id = 3, a = 1 },
        };
        ExecuteAddStatement(coll.Add(docs));
        Collection coll2 = session2.GetSchema("test").GetCollection("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        DocResult docResult = ExecuteFindStatement(coll.Find("_id = 1").LockShared());
        Assert.That(docResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Reading the same document is allowed with LockShared().
        docResult = ExecuteFindStatement(coll2.Find("_id = 1"));
        Assert.That(docResult.FetchAll(), Has.One.Items);

        // Modify() is allowed for non-locked documents.
        Result result = ExecuteModifyStatement(coll2.Modify("_id = 2").Set("a", 2));
        Assert.AreEqual(1, result.AffectedItemsCount);
        // Session1 blocks, Modify() is not allowed for locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("_id = 1").Set("a", 2)));
        Assert.AreEqual("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Modify() is allowed since document isn't locked anymore.
        ExecuteModifyStatement(coll2.Modify("_id = 1").Set("a", 2));
        ExecuteSQLStatement(session2.SQL("COMMIT"));
      }
    }

    [Test]
    public void ExclusiveLockForbidsToModifyDocuments()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        var docs = new[]
        {
          new {  _id = 1, a = 1 },
          new {  _id = 2, a = 1 },
          new {  _id = 3, a = 1 },
        };
        ExecuteAddStatement(coll.Add(docs));
        Collection coll2 = session2.GetSchema("test").GetCollection("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        DocResult docResult = ExecuteFindStatement(coll.Find("_id = 1").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));

        // Modify() is allowed for non-locked documents.
        Result result = ExecuteModifyStatement(coll2.Modify("_id = 2").Set("a", 2));
        Assert.AreEqual(1, result.AffectedItemsCount);
        // Session1 blocks, Modify() is not allowed for locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("_id = 1").Set("a", 2)));
        Assert.AreEqual("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Modify() is allowed since document isn't locked anymore.
        ExecuteModifyStatement(coll2.Modify("_id = 1").Set("a", 2));
        ExecuteSQLStatement(session2.SQL("COMMIT"));
      }
    }

    [Test]
    public void SharedLockAfterExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
        var docs = new[]
        {
          new {  _id = 1, a = 1 },
          new {  _id = 2, a = 1 },
          new {  _id = 3, a = 1 },
        };
        ExecuteAddStatement(coll.Add(docs));
        Collection coll2 = session2.GetSchema("test").GetCollection("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        DocResult docResult = ExecuteFindStatement(coll.Find("_id = 1").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since document isn't locked.
        docResult = ExecuteFindStatement(coll2.Find("_id = 2").LockShared());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        // Session2 blocks due to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockShared()));
        Assert.AreEqual("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks documents.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Document can now be recovered.
        docResult = ExecuteFindStatement(coll2.Find("_id = 1").LockShared());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void ExclusiveLockAfterSharedLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
        var docs = new[]
        {
          new {  _id = 1, a = 1 },
          new {  _id = 2, a = 1 },
          new {  _id = 3, a = 1 },
        };
        ExecuteAddStatement(coll.Add(docs));
        Collection coll2 = session2.GetSchema("test").GetCollection("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        DocResult docResult = ExecuteFindStatement(coll.Find("_id in (1, 3)").LockShared());
        Assert.AreEqual(2, docResult.FetchAll().Count);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since document isn't locked.
        docResult = ExecuteFindStatement(coll2.Find("_id = 2").LockExclusive());
        // Should return immediately due to LockShared() allows reading by other sessions.
        docResult = ExecuteFindStatement(coll2.Find("_id = 2").LockShared());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive()));
        Assert.AreEqual("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks documents.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        docResult = ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void ExclusiveLockAfterExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
        var docs = new[]
        {
          new {  _id = 1, a = 1 },
          new {  _id = 2, a = 1 },
          new {  _id = 3, a = 1 }
        };
        ExecuteAddStatement(coll.Add(docs));
        Collection coll2 = session2.GetSchema("test").GetCollection("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        DocResult docResult = ExecuteFindStatement(coll.Find("_id = 1").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since document isn't locked.
        docResult = ExecuteFindStatement(coll2.Find("_id = 2").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive()));
        Assert.AreEqual("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks documents.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        docResult = ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive());
        Assert.That(docResult.FetchAll(), Has.One.Items);
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void InOperatorWithListOfValues()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      // Validates the IN operator allows expressions of the type
      // ( compExpr ["NOT"] "IN" "(" argsList ")" ) | ( compExpr ["NOT"] "IN" "[" argsList "]" )
      Collection coll = CreateCollection("test");
      ExecuteAddStatement(coll.Add(new DbDoc("{ \"a\": 1, \"b\": [ 1, \"value\" ], \"d\":\"\" }")));

      Assert.That(ExecuteFindStatement(coll.Find("a IN (1,2,3)")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("a not in (0,2,3)")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("b[0] in (1,2,3)")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("b[1] in (\"a\", \"b\", \"value\")")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("b[0] NOT IN (0,2,3)")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("b[1] not in (\"a\", \"b\", \"c\")")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("a in [1,2,3]")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("a in [2,3,4]")).FetchAll());
      Assert.That(ExecuteFindStatement(coll.Find("a NOT in [0,2,3]")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("b not IN [1,2,3]")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("b[0] not IN [1,2,3]")).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("c NOT IN [1,2,3]")).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("a IN ('', ' ')")).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("'' IN (1,2,3)")).FetchAll());
      Assert.That(ExecuteFindStatement(coll.Find("d IN ('')")).FetchAll(), Has.One.Items);

      Collection movies = CreateCollection("movies");
      var docString = "{ \"_id\" : \"a6f4b93e1a264a108393524f29546a8c\", \"title\" : \"AFRICAN EGG\", \"description\" : \"A Fast-Paced Documentary of a Pastry Chef And a Dentist who must Pursue a Forensic Psychologist in The Gulf of Mexico\", \"releaseyear\" : 2006, \"language\" : \"English\", \"duration\" : 130, \"rating\" : \"G\", \"genre\" : \"Science fiction\", \"actors\" : [{ \"name\" : \"MILLA PECK\", \"country\" : \"Mexico\", \"birthdate\": \"12 Jan 1984\"}, { \"name\" : \"VAL BOLGER\", \"country\" : \"Botswana\", \"birthdate\": \"26 Jul 1975\" }, { \"name\" : \"SCARLETT BENING\", \"country\" : \"Syria\", \"birthdate\": \"16 Mar 1978\" }], \"additionalinfo\" : { \"director\" : \"Sharice Legaspi\", \"writers\" : [\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"], \"productioncompanies\" : [\"Qvodrill\", \"Indigoholdings\"] } }";
      ExecuteAddStatement(movies.Add(new DbDoc(docString)));

      Assert.That(ExecuteFindStatement(movies.Find("(1>5) in (true, false)")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("(1+5) in (1, 2, 3, 4, 5)")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("('a'>'b') in (true, false)")).FetchAll(), Has.One.Items);
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(movies.Find("(1>5) in [true, false]")).FetchAll());
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(movies.Find("(1+5) in [1, 2, 3, 4, 5]")).FetchAll());
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(movies.Find("('a'>'b') in [true, false]")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("true IN [(1>5), !(false), (true || false), (false && true)]")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find("true IN ((1>5), !(false), (true || false), (false && true))")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("{\"field\":true} IN (\"mystring\", 124, myvar, othervar.jsonobj)")).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("actor.name IN ['a name', null, (1<5-4), myvar.jsonobj.name]")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("!false && true IN [true]")).FetchAll(), Has.One.Items);
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(movies.Find("1-5/2*2 > 3-2/1*2 IN [true, false]")).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("true IN [1-5/2*2 > 3-2/1*2]")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find(" 'African Egg' IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find(" 1 IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find(" [0,1,2] IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find(" { 'title' : 'Atomic Firefighter' } IN ('African Egg', 1, true, NULL, [0,1,2], { 'title' : 'Atomic Firefighter' }) ")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("title IN ('African Egg', 'The Witcher', 'Jurassic Perk')")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("releaseyear IN (2006, 2010, 2017)")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find("1 IN [1,2,3]")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("0 IN [1,2,3]")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("0 NOT IN [1,2,3]")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("1 NOT IN [1,2,3]")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("releaseyear IN [2006, 2007, 2008]")).FetchAll(), Has.One.Items);
    }

    [Test]
    public void InOperatorWithCompExpr()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      // Validates the IN operator allows expressions of the type: compExpr ["NOT"] "IN" compExpr
      Collection coll = CreateCollection("test");
      var docString = "{ \"a\": 1, \"b\": \"foo\", \"c\": { \"d\": true, \"e\": [1,2,3] }, \"f\": [ {\"x\":5}, {\"x\":7 } ] }";
      ExecuteAddStatement(coll.Add(new DbDoc(docString)));

      Assert.That(ExecuteFindStatement(coll.Find("a in [1,2,3]")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("c.e[0] in [1,2,3]")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("5 in f[*].x")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("3 in c.e")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("5 in c.e")).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("\"foo\" in " + docString)).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("\"a\" in " + docString)).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("a in " + docString)).FetchAll());
      Assert.That(ExecuteFindStatement(coll.Find("{\"a\":1} in " + docString)).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(coll.Find("\"foo\" in b")).FetchAll(), Has.One.Items);

      Collection movies = CreateCollection("movies");
      docString = "{ \"_id\" : \"a6f4b93e1a264a108393524f29546a8c\", \"title\" : \"AFRICAN EGG\", \"description\" : \"A Fast-Paced Documentary of a Pastry Chef And a Dentist who must Pursue a Forensic Psychologist in The Gulf of Mexico\", \"releaseyear\" : 2006, \"language\" : \"English\", \"duration\" : 130, \"rating\" : \"G\", \"genre\" : \"Science fiction\", \"actors\" : [{ \"name\" : \"MILLA PECK\", \"country\" : \"Mexico\", \"birthdate\": \"12 Jan 1984\"}, { \"name\" : \"VAL BOLGER\", \"country\" : \"Botswana\", \"birthdate\": \"26 Jul 1975\" }, { \"name\" : \"SCARLETT BENING\", \"country\" : \"Syria\", \"birthdate\": \"16 Mar 1978\" }], \"additionalinfo\" : { \"director\" : \"Sharice Legaspi\", \"writers\" : [\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"], \"productioncompanies\" : [\"Qvodrill\", \"Indigoholdings\"] } }";
      ExecuteAddStatement(movies.Add(new DbDoc(docString)));

      Assert.That(ExecuteFindStatement(movies.Find("{ \"name\" : \"MILLA PECK\" } IN actors")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("'African Egg' in movietitle")).FetchAll());
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(movies.Find("(1 = NULL) IN title")).FetchAll());
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(movies.Find("NOT NULL IN title")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("[\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"] IN additionalinfo.writers")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find("{ \"name\" : \"MILLA PECK\", \"country\" : \"Mexico\", \"birthdate\": \"12 Jan 1984\"} IN actors")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("true IN title")).FetchAll());
      CollectionAssert.IsEmpty(ExecuteFindStatement(movies.Find("false IN genre")).FetchAll());
      Assert.That(ExecuteFindStatement(movies.Find("'Sharice Legaspi' IN additionalinfo.director")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find("'Mexico' IN actors[*].country")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteFindStatement(movies.Find("'Angelic Orduno' IN additionalinfo.writers")).FetchAll(), Has.One.Items);
    }

    [Test]
    public void InOperatorWithJsonArrays()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      Collection coll = CreateCollection("test");
      var docString = "{ \"_id\": \"1001\", \"ARR\":[1,2,3], \"ARR1\":[\"name\", \"name2\", \"name3\"]}";
      ExecuteAddStatement(coll.Add(new DbDoc(docString)));

      Assert.That(ExecuteFindStatement(coll.Find("\"1001\" in $._id")).FetchAll(), Has.One.Items);
      CollectionAssert.IsEmpty(ExecuteFindStatement(coll.Find("\"1002\" in $._id")).FetchAll());
      Assert.That(ExecuteFindStatement(coll.Find("(1+2) in (1, 2, 3)")).FetchAll(), Has.One.Items);
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll.Find("(1+2) in [1, 2, 3]")).FetchAll());
      Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll.Find("(1+2) in $.ARR")).FetchAll());
    }

    [Test]
    public void GetOne()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4, r.AffectedItemsCount);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => coll.GetOne(null));
      Assert.Throws<ArgumentNullException>(() => coll.GetOne(""));
      Assert.Throws<ArgumentNullException>(() => coll.GetOne(string.Empty));

      // Get document using numeric parameter.
      DbDoc document = coll.GetOne(1);
      Assert.AreEqual(1, document.Id);
      Assert.AreEqual("Book 1", document["title"]);
      Assert.AreEqual(20, Convert.ToInt32(document["pages"]));

      // Get document using string parameter.
      document = coll.GetOne("3");
      Assert.AreEqual(3, document.Id);
      Assert.AreEqual("Book 3", document["title"]);
      Assert.AreEqual(40, Convert.ToInt32(document["pages"]));

      // Get a non-existing document.
      document = coll.GetOne(5);
      Assert.Null(document);
    }

    public enum LockMode { Exclusive, Shared }

    [TestCase(LockContention.Default, LockMode.Exclusive)]
    [TestCase(LockContention.NoWait, LockMode.Exclusive)]
    [TestCase(LockContention.SkipLocked, LockMode.Exclusive)]
    [TestCase(LockContention.Default, LockMode.Shared)]
    [TestCase(LockContention.NoWait, LockMode.Shared)]
    [TestCase(LockContention.SkipLocked, LockMode.Shared)]
    public void LockExclusiveAndSharedWithWaitingOptions(LockContention lockOption, LockMode lockMode)
    {
      if (!session.XSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      string collectionName = "test";
      var coll = CreateCollection(collectionName);
      ExecuteAddStatement(coll.Add(new { _id = 1, name = "Jonh" }));

      // first session locks the row
      using (Session s1 = MySQLX.GetSession(ConnectionString))
      {
        var coll1 = s1.GetSchema(schemaName).GetCollection(collectionName);
        s1.StartTransaction();
        DocResult r1 = ExecuteFindStatement(coll1.Find("_id = :id").Bind("id", 1).LockExclusive());
        Assert.That(r1.FetchAll(), Has.One.Items);

        // second session tries to read the locked row
        using (Session s2 = MySQLX.GetSession(ConnectionString))
        {
          var coll2 = s2.GetSchema(schemaName).GetCollection(collectionName);
          ExecuteSQLStatement(s2.SQL("SET innodb_lock_wait_timeout = 1"));
          s2.StartTransaction();
          var stmt2 = coll2.Find("_id = :id").Bind("id", 1);
          if (lockMode == LockMode.Exclusive)
            stmt2.LockExclusive(lockOption);
          else
            stmt2.LockShared(lockOption);

          switch (lockOption)
          {
            case LockContention.Default:
              // error 1205 Lock wait timeout exceeded; try restarting transaction
              Assert.AreEqual(1205u, Assert.Throws<MySqlException>(() => ExecuteFindStatement(stmt2).FetchAll()).Code);
              break;
            case LockContention.NoWait:
              // error 1205 Lock wait timeout exceeded; try restarting transaction
              uint expectedError = 1205;
              if (session.XSession.GetServerVersion().isAtLeast(8, 0, 5))
                // error 3572 Statement aborted because lock(s) could not be acquired immediately and NOWAIT is set
                expectedError = 3572;
              Assert.AreEqual(expectedError, Assert.Throws<MySqlException>(() => ExecuteFindStatement(stmt2).FetchAll()).Code);
              break;
            case LockContention.SkipLocked:
              if (!session.XSession.GetServerVersion().isAtLeast(8, 0, 5))
              {
                // error 1205 Lock wait timeout exceeded; try restarting transaction
                Assert.AreEqual(1205u, Assert.Throws<MySqlException>(() => ExecuteFindStatement(stmt2).FetchAll()).Code);
                break;
              }
              CollectionAssert.IsEmpty(ExecuteFindStatement(stmt2).FetchAll());
              break;
            default:
              throw new NotImplementedException(lockOption.ToString());
          }
        }
        // first session frees the lock
        s1.Commit();
      }
    }

    [Test]
    public void Grouping()
    {
      ExecuteSQLStatement(GetSession().SQL("SET GLOBAL sql_mode=(SELECT REPLACE(@@sql_mode, 'ONLY_FULL_GROUP_BY', '')); "));
      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new { _id = 1, name = "jonh doe", age = 38 },
        new { _id = 2, name = "milton green", age = 45 },
        new { _id = 3, name = "larry smith", age = 24},
        new { _id = 4, name = "mary weinstein", age = 24 },
        new { _id = 5, name = "jerry pratt", age = 45 },
        new { _id = 6, name = "hugh jackman", age = 20},
        new { _id = 7, name = "elizabeth olsen", age = 31}
      };
      Result r = ExecuteAddStatement(collection.Add(docs));
      Assert.AreEqual(7, r.AffectedItemsCount);

      // GroupBy operation.
      // GroupBy returns 5 rows since age 45 and 24 is repeated.
      var result = ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy("age"));
      Assert.AreEqual(5, result.FetchAll().Count);

      // GroupBy with null.
      result = ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy(null));
      Assert.AreEqual(7, result.FetchAll().Count);
      result = ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy(null, null));
      Assert.AreEqual(7, result.FetchAll().Count);
      result = ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy(null, "age"));
      Assert.AreEqual(5, result.FetchAll().Count);

      // Having operation.
      // Having reduces the original 5 rows to 3 since 2 rows have a cnt=2, due to the repeated names.
      result = ExecuteFindStatement(collection.Find().Fields("_id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("cnt = 1"));
      Assert.AreEqual(3, result.FetchAll().Count);

      // Having with null.
      result = ExecuteFindStatement(collection.Find().Fields("_id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(null));
      Assert.AreEqual(5, result.FetchAll().Count);

      // GroupBy with invalid field name.
      var ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy("none")));
      Assert.AreEqual("Unknown column 'none' in 'group statement'", ex.Message);

      // GroupBy with empty strings.
      var ex2 = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy("")));
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex2.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy(" ")));
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex2.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy(string.Empty)));
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex2.Message);

      // Having with invalid field name.
      ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("none = 1")));
      Assert.AreEqual("Invalid expression in grouping criteria", ex.Message);

      // Having with empty strings.
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("")));
      Assert.AreEqual("Unable to parse query ''", ex2.Message);
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex2.InnerException.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(" ")));
      Assert.AreEqual("Unable to parse query ' '", ex2.Message);
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex2.InnerException.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(collection.Find().Fields("_id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(string.Empty)));
      Assert.AreEqual("Unable to parse query ''", ex2.Message);
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex2.InnerException.Message);
    }

    [Test]
    public void Fields()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(4ul, r.AffectedItemsCount);

      // Single field.
      var result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields("title"));
      var document = result.FetchOne();
      Assert.That(document.values, Has.One.Items);
      Assert.AreEqual("Book 3", document["title"]);

      // Null values are ignored.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields(null));
      document = result.FetchOne();
      Assert.AreEqual(3, document.values.Count);

      // Null values are ignored.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields("title", null));
      document = result.FetchOne();
      Assert.That(document.values, Has.One.Items);

      // Single field in array.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields(new string[] { "title" }));
      document = result.FetchOne();
      Assert.That(document.values, Has.One.Items);
      Assert.AreEqual("Book 3", document["title"]);

      // Single field with alias.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pages", 20).Fields("title as title2"));
      document = result.FetchOne();
      Assert.That(document.values, Has.One.Items);
      Assert.AreEqual("Book 1", document["title2"]);
      Assert.False(document.values.ContainsKey("title"));

      // Unexistent field returns null.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pages", 20).Fields("book"));
      document = result.FetchOne();
      Assert.That(document.values, Has.One.Items);
      Assert.Null(document["book"]);

      // Unexistent field with alias returns null.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pages", 20).Fields("book as book1"));
      document = result.FetchOne();
      Assert.That(document.values, Has.One.Items);
      Assert.Null(document["book1"]);

      // Multiple fields.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields("title", "pages", "other"));
      document = result.FetchOne();
      Assert.AreEqual(3, document.values.Count);
      Assert.AreEqual("Book 3", document["title"]);
      Assert.AreEqual(40, document["pages"]);
      Assert.Null(document["other"]);

      // Multiple fields in array.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields(new string[] { "title", "pages" }));
      document = result.FetchOne();
      Assert.AreEqual(2, document.values.Count);
      Assert.AreEqual("Book 3", document["title"]);
      Assert.AreEqual(40, document["pages"]);

      // Sending a document doesn't generate an error.
      result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pages", 20).Fields("{\"_id\":\"1004\",\"F1\": 1234 }"));
      document = result.FetchOne();

      // Empty string and white space raises error.
      var ex = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields("")));
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields("  ")));
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields(string.Empty)));
      Assert.AreEqual("No more tokens when expecting one at token pos 0", ex.Message);

      // Multiple word field name raises error.
      ex = Assert.Throws<ArgumentException>(() => result = ExecuteFindStatement(coll.Find("pages = :Pages").Bind("pAges", 40).Fields("Book 1")));
      Assert.AreEqual("Expression has unexpected token '1' at position 1.", ex.Message);
    }

    [TestCase("", "")]
    [TestCase("'", "'")]
    [TestCase("", "'")]
    [TestCase("'", "")]
    public void FindIdAsString(string prefix, string suffix)
    {
      Collection coll = CreateCollection("test");
      Result r = null;
      var docs = new[]
      {
        new {  _id = $"{prefix}1{suffix}", title = $"{prefix}Book 1{suffix}", pages = 20 },
        new {  _id = $"{prefix}2{suffix}", title = $"{prefix}Book 2{suffix}", pages = 30 },
        new {  _id = $"{prefix}3{suffix}", title = $"{prefix}Book 3{suffix}", pages = 40 },
        new {  _id = $"{prefix}4{suffix}", title = $"{prefix}Book 4{suffix}", pages = 50 },
      };
      r = coll.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);

      var findStmt = coll.Find("_id = :id and pages = :pages").Bind("id", $"{prefix}3{suffix}").Bind("pages", 40);
      DocResult doc = ExecuteFindStatement(findStmt);
      var books = doc.FetchAll();
      Assert.That(books, Has.One.Items);
      Assert.AreEqual($"{prefix}3{suffix}", books[0]["_id"]);

      findStmt = coll.Find("_id = :id and pages = :pages").Bind("Id", $"{prefix}2{suffix}").Bind("Pages", 30);
      doc = ExecuteFindStatement(findStmt);
      books = doc.FetchAll();
      Assert.That(books, Has.One.Items);
      Assert.AreEqual($"{prefix}2{suffix}", books[0]["_id"]);

      findStmt = coll.Find("title = :title").Bind("Title", $"{prefix}Book 4{suffix}");
      doc = ExecuteFindStatement(findStmt);
      books = doc.FetchAll();
      Assert.That(books, Has.One.Items);
      Assert.AreEqual($"{prefix}4{suffix}", books[0]["_id"]);
      Assert.AreEqual(50, books[0]["pages"]);
    }

    [TestCase(":hobbies IN $.additionalinfo.hobbies", "hobbies", "painting", 4)]
    [TestCase(":hobbies IN $.additionalinfo.hobbies", "hobbies", "[\"playing\", \"listening\"]", 0)]
    [TestCase("[\"playing\", \"listening\"] IN $.additionalinfo.hobbies", null, null, 3)]
    public void InOperatorBindingJson(string condition, string bind, string value, int id)
    {
      Collection coll = CreateCollection("test");
      Result r = null;
      var docs = new[]
      {
        new { _id = 1, title = $"Book 1", pages = 20, additionalinfo = new DbDoc("{\"company\":\"xyz\",\"vehicle\":\"bike\",\"hobbies\":\"reading\"}") },
        new { _id = 2, title = $"Book 2", pages = 30, additionalinfo = new DbDoc("{\"company\":\"abc\",\"vehicle\":\"car\",\"hobbies\":\"boxing\"}") },
        new { _id = 3, title = $"Book 3", pages = 40, additionalinfo = new DbDoc("{\"company\":\"qwe\",\"vehicle\":\"airplane\",\"hobbies\":[\"playing\", \"listening\"]}") },
        new { _id = 4, title = $"Book 4", pages = 50, additionalinfo = new DbDoc("{\"company\":\"zxc\",\"vehicle\":\"boat\",\"hobbies\":\"painting\"}") },
      };
      r = coll.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);

      var findStmt = coll.Find(condition);
      if (bind != null) findStmt.Bind(bind, value);
      var result = findStmt.Execute().FetchAll();
      Assert.AreEqual(id == 0 ? 0 : 1, result.Count);
      if (id > 0)
      {
        Assert.AreEqual(id, result[0]["_id"]);
      }
    }

    [Test]
    public void FindUsingOverlaps()
    {
      Collection coll = CreateCollection("test");

      coll.Add("{ \"_id\":1, \"title\": \"Book 1\", \"list\":[1, 4]}").Execute();
      coll.Add("{ \"_id\":2, \"title\": \"Book 2\", \"list\":[5, 7]}").Execute();
      coll.Add("{ \"_id\":3, \"title\": \"Book 3\", \"list\":[4, 9]}").Execute();
      coll.Add("{ \"_id\":4, \"title\": \"Book 4\", \"list\":[2]}").Execute();
      coll.Add("{ \"_id\":5, \"title\": \"Book 5\",\"list\":[\"\"]}").Execute();
      coll.Add("{ \"_id\":6, \"title\": \"Book 6\",\"list\":[\" \"]}").Execute();

      var result = ExecuteFindStatement(coll.Find("[7] OVERLAPS $.list")).FetchAll();
      Assert.AreEqual("Book 2", result[0]["title"]);
      result = ExecuteFindStatement(coll.Find("[8] overlaps $.list")).FetchAll();
      CollectionAssert.IsEmpty(result);
      result = ExecuteFindStatement(coll.Find("[1, 4] OVERLAPS $.list")).FetchAll();
      CollectionAssert.IsNotEmpty(result);
      Assert.AreEqual("Book 1", result[0]["title"]);
      Assert.AreEqual("Book 3", result[1]["title"]);
      result = ExecuteFindStatement(coll.Find("$.list OVERLAPS [1, 2]")).FetchAll();
      CollectionAssert.IsNotEmpty(result);
      Assert.AreEqual("Book 1", result[0]["title"]);
      Assert.AreEqual("Book 4", result[1]["title"]);
      result = ExecuteFindStatement(coll.Find("'Book 1' NOT OVERLAPS $.title").Fields("_id")).FetchAll();
      Assert.AreEqual(5, result.Count);
      Assert.AreEqual(3, result[1].Id);
      result = ExecuteFindStatement(coll.Find(":title NOT OVERLAPS $.title").Bind("title", "Book 1").Fields("_id")).FetchAll();
      Assert.AreEqual(5, result.Count);
      Assert.AreEqual(4, result[2].Id);
      result = ExecuteFindStatement(coll.Find("$.list OVERLAPS :list").Bind("list", 9)).FetchAll();
      Assert.AreEqual("Book 3", result[0]["title"]);
      var jsonParams = new { list = 4 };
      result = ExecuteFindStatement(coll.Find("$.list OVERLAPS :list").Bind(jsonParams).Fields("count(_id) as ID", "title as Title", "list as List").
        GroupBy("title", "list").Having("ID > 0")).FetchAll();
      Assert.AreEqual(2, result.Count);
      Assert.AreEqual("Book 1", result[0]["Title"]);
      result = ExecuteFindStatement(coll.Find("[''] OVERLAPS $.list")).FetchAll();
      Assert.That(result, Has.One.Items);
      Assert.AreEqual(5, result[0].Id);
      result = ExecuteFindStatement(coll.Find("[' '] OVERLAPS $.list")).FetchAll();
      Assert.That(result, Has.One.Items);
      Assert.AreEqual(6, result[0].Id);

      Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("$.list OVERLAPS -")).FetchAll());
      Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("[2, 9] OVERLAPS")).FetchAll());
      Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("[2, 9] OVERPS $.list")).FetchAll());
    }
  }
}

