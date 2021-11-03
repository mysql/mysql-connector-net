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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using NUnit.Framework;
using System;

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
      if (session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql lower than 8.0.3.");
      Collection coll = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll.Find().LockShared()));
      Assert.AreEqual("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);

      ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll.Find().LockExclusive()));
      Assert.AreEqual("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);
    }

    [Test]
    public void SimpleSharedLock()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
    public void SharedLockForbidsToModifyDocuments()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
        // Session2 blocks, Modify() is not allowed for locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("_id = 1").Set("a", 2)));
        Assert.AreEqual("Lock wait timeout exceeded; try restarting transaction", ex.Message);
        ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("_id = 1").Change("a", 12)));
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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");

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

    #region WL14389

    [Test, Description("Collection.Find(condition).GroupBy(SearchExprStr).Having(SearchConditionStr)")]
    public void FindGroupByHaving()
    {
      ExecuteSQLStatement(GetSession().SQL("set sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';"));

      Collection collection = CreateCollection("test");
      var docs1 = new[]
        {
          new { _id = 1, name = "jonh doe", age = 38,profit = 100,test = 1.92,date="21/11/2011" },
          new { _id = 2, name = "milton green", age = 45,profit = 200,test = 12.08,date="21/11/2012"  },
          new { _id = 3, name = "larry smith", age = 24,profit = 300,test = 12.1,date="21/11/2013" },
          new { _id = 4, name = "mary weinstein", age = 24 ,profit = 100,test = 12.0,date="21/11/2014" },
          new { _id = 5, name = "jerry pratt", age = 45 ,profit = 400,test = 20.87,date="21/11/2015" },
          new { _id = 6, name = "hugh jackman", age = 20,profit = 500,test = 20.65,date="21/11/2016"},
          new { _id = 7, name = "elizabeth olsen", age = 31,profit = 300,test = 20.45,date="21/11/2017" },
          new { _id = 8, name = "tommy h", age = 31,profit = 3000,test = 0.0,date="21/11/2018"}
        };

      Result r = collection.Add(docs1).Execute();
      Assert.AreEqual(8, r.AffectedItemsCount);

      // GroupBy operation.
      // GroupBy returns 5 rows since age 45 and 24 is repeated.
      var result = collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy("age").Execute();
      Assert.AreEqual(5, result.FetchAll().Count);
      result = collection.Find().Fields("_id as ID", "name as Name", "profit as Profit", "test as test").GroupBy("test").Having("test=1.92").Execute();
      Assert.AreEqual(1, result.FetchAll().Count);
      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>=31").
          Sort("profit DESC").Execute();
      Assert.AreEqual(result.FetchAll().Count, result.FetchAll().Count);
      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30 && max($.age)<32")
         .Execute();
      Assert.AreEqual(2, result.FetchAll().Count);
      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>300000000")
        .Execute();
      Assert.AreEqual(0, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32")
         .Execute();
      Assert.AreEqual(5, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
         Having("max($.age)<32").Limit(5)
        .Execute();
      Assert.AreEqual(5, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
         Having("max($.age)<32").Limit(3)
        .Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32").Limit(3).Offset(2)
         .Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      Assert.Throws<ArgumentException>(() => collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("date=21/11/2011").Execute());
    }

    [Test, Description("Collection.Find().Limit(x).Offset(y)")]
    public void FindLimitOffset()
    {
      ExecuteSQLStatement(GetSession().SQL("set sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';"));

      Collection collection = CreateCollection("test");
      var docs1 = new[]
      {
        new { _id = 1, name = "jonh doe", age = 38,profit = 100,test = 1.92,date="21/11/2011" },
        new { _id = 2, name = "milton green", age = 45,profit = 200,test = 12.08,date="21/11/2012"  },
        new { _id = 3, name = "larry smith", age = 24,profit = 300,test = 12.1,date="21/11/2013" },
        new { _id = 4, name = "mary weinstein", age = 24 ,profit = 100,test = 12.0,date="21/11/2014" },
        new { _id = 5, name = "jerry pratt", age = 45 ,profit = 400,test = 20.87,date="21/11/2015" },
        new { _id = 6, name = "hugh jackman", age = 20,profit = 500,test = 20.65,date="21/11/2016"},
        new { _id = 7, name = "elizabeth olsen", age = 31,profit = 300,test = 20.45,date="21/11/2017" },
        new { _id = 8, name = "tommy h", age = 31,profit = 3000,test = 0.0,date="21/11/2018"}
      };

      Result r = collection.Add(docs1).Execute();
      Assert.AreEqual(8, r.AffectedItemsCount);

      // GroupBy operation.
      // GroupBy returns 5 rows since age 45 and 24 is repeated.
      var result1 = collection.Find().Limit(2).Offset(-2378723).Execute();
      int k = result1.FetchAll().Count;
      result1 = collection.Find().Limit(2).Offset(-1).Execute();
      k = result1.FetchAll().Count;
      result1 = collection.Find().Limit(2).Offset(0).Execute();
      k = result1.FetchAll().Count;

      var result = collection.Find().Fields("_id as ID", "name as Name", "age as Age").GroupBy("age").Execute();
      Assert.AreEqual(5, result.FetchAll().Count);
      result = collection.Find().Fields("_id as ID", "name as Name", "profit as Profit", "test as test").GroupBy("test").Having("test=1.92").Execute();
      Assert.AreEqual(1, result.FetchAll().Count);
      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>=31").
          Sort("profit DESC").Execute();
      Assert.AreEqual(result.FetchAll().Count, result.FetchAll().Count);
      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30 && max($.age)<32")
          .Execute();
      Assert.AreEqual(2, result.FetchAll().Count);
      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>300000000")
        .Execute();
      Assert.AreEqual(0, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32")
          .Execute();
      Assert.AreEqual(5, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32").Limit(5)
        .Execute();
      Assert.AreEqual(5, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32").Limit(3)
        .Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32").Limit(3).Offset(2)
          .Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      result = collection.Find().Fields("$._id as _id", "$.test as test").GroupBy("$.test").Limit(5).Offset(1).Execute();
      Assert.AreEqual(5, result.FetchAll().Count);

      result = collection.Find().Fields("$._id as _id", "$.test as test").GroupBy("$.test").Limit(5).Offset(-1).Execute();
      Assert.AreEqual(5, result.FetchAll().Count);

      result = collection.Find().Fields("$._id as _id", "$.test as test").GroupBy("$.test").Limit(8).Execute();
      Assert.AreEqual(8, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
              Having("max($.age)<32").Limit(3).Offset(-1)
            .Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      Assert.Throws<ArgumentOutOfRangeException>(() => collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32").Limit(-1).Offset(0)
          .Execute());

      DbDoc[] jsonlist = new DbDoc[2000];
      for (int i = 0; i < 2000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 1000).ToString());
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      var res = collection.Add(jsonlist).Execute();

      result = collection.Find().Fields("$.F1 as F1", "$.F2 as F2").GroupBy("$.F1").Limit(3).Offset(1).Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      result = collection.Find().Fields("$.F1 as F1", "$.F2 as F2").GroupBy("$.F1").Limit(3).Offset(1844674407370955161).Execute();
      Assert.AreEqual(0, result.FetchAll().Count);
    }

    [Test, Description("Collection.Find().Limit(x,y)")]
    public void FindLimitOffsetDeprecated()
    {
      ExecuteSQLStatement(GetSession().SQL("set sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';"));
      Collection collection = CreateCollection("test");
      var docs1 = new[]
      {
        new { _id = 1, name = "jonh doe", age = 38,profit = 100,test = 1.92,date="21/11/2011" },
        new { _id = 2, name = "milton green", age = 45,profit = 200,test = 12.08,date="21/11/2012"  },
        new { _id = 3, name = "larry smith", age = 24,profit = 300,test = 12.1,date="21/11/2013" },
        new { _id = 4, name = "mary weinstein", age = 24 ,profit = 100,test = 12.0,date="21/11/2014" },
        new { _id = 5, name = "jerry pratt", age = 45 ,profit = 400,test = 20.87,date="21/11/2015" },
        new { _id = 6, name = "hugh jackman", age = 20,profit = 500,test = 20.65,date="21/11/2016"},
        new { _id = 7, name = "elizabeth olsen", age = 31,profit = 300,test = 20.45,date="21/11/2017" },
        new { _id = 8, name = "tommy h", age = 31,profit = 3000,test = 0.0,date="21/11/2018"}
      };

      Result r = collection.Add(docs1).Execute();
      Assert.AreEqual(8, r.AffectedItemsCount);

      // GroupBy operation.
      var result1 = collection.Find().Limit(2).Offset(-2378723).Execute();
      int k = result1.FetchAll().Count;
      result1 = collection.Find().Limit(2).Offset(-1).Execute();
      k = result1.FetchAll().Count;
      result1 = collection.Find().Limit(2).Offset(0).Execute();
      k = result1.FetchAll().Count;
      result1 = collection.Find().Limit(2).Offset(1000000000).Execute();
      k = result1.FetchAll().Count;
      collection.Remove("true").Limit(2).Execute();

      docs1 = new[]
      {
        new { _id = 1, name = "jonh doe", age = 38,profit = 100,test = 1.92,date="21/11/2011" },
        new { _id = 2, name = "milton green", age = 45,profit = 200,test = 12.08,date="21/11/2012"  },
      };

      r = collection.Add(docs1).Execute();
      Assert.Throws<MySqlException>(() => collection.Remove("true").Limit(2).Offset(1).Execute());

      collection.Remove("true").Limit(2).Offset(0).Execute();
      docs1 = new[]
      {
        new { _id = 1, name = "jonh doe", age = 38,profit = 100,test = 1.92,date="21/11/2011" },
        new { _id = 2, name = "milton green", age = 45,profit = 200,test = 12.08,date="21/11/2012"  },
      };

      r = collection.Add(docs1).Execute();
      Assert.Throws<MySqlException>(() => collection.Modify("_id = 1 || _id = 2").Set("age", 34).Limit(1).Offset(1).Execute());

      collection.Modify("_id = 1 || _id = 2").Set("age", 34).Limit(2).Offset(0).Execute();
      collection.Modify("_id = 1").Set("age", 38).Limit(2).Offset(0).Execute();
      collection.Modify("_id = 2").Set("age", 45).Limit(2).Offset(0).Execute();

      var result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
          Having("max($.age)<32").Limit(3).Offset(2)
          .Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      result = collection.Find().Fields("$._id as _id", "$.test as test").GroupBy("$.test").Limit(5).Offset(1).Execute();
      Assert.AreEqual(5, result.FetchAll().Count);
      result = collection.Find().Fields("$._id as _id", "$.test as test").GroupBy("$.test").Limit(5).Offset(-1).Execute();
      Assert.AreEqual(5, result.FetchAll().Count);

      result = collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
      Having("max($.age)<32").Limit(3).Offset(-1)
      .Execute();
      Assert.AreEqual(3, result.FetchAll().Count);

      Assert.Throws<ArgumentOutOfRangeException>(() => collection.Find().Fields("max($.age) as age", "max($.profit) as Profit", "max($.test) as test").GroupBy("$.test").Having("max($.age)>30").
      Having("max($.age)<32").Limit(-1).Offset(0)
      .Execute());
    }

    [Test, Description("Test MySQLX plugin Collection Dbdoc Scenarios")]
    public void CollectionDbdoc()
    {
      Collection col = CreateCollection("my_collection_1");
      Collection col1 = CreateCollection("my_collection_2");
      var d1 = new DbDoc();
      d1.SetValue("_id", 1);
      var docs1 = new[]
      {
        new {  _id = 1, title = "Book 1" },
        new {  _id = 2, title = "Book 2" },
      };

      d1.SetValue("books", docs1);
      d1.SetValue("pages", 20);

      var d2 = new DbDoc();
      d2.SetValue("_id", 2);
      var docs2 = new[]
      {
        new {  _id = 1, title = "Book 3" },
        new {  _id = 2, title = "Book 4" },
      };

      d2.SetValue("books", docs2);
      d2.SetValue("pages", 30);

      var d3 = new DbDoc();
      d3.SetValue("_id", 3);
      var docs3 = new[]
      {
        new {  _id = 1, title = "Book 3" },
        new {  _id = 2, title = "Book 4" },
      };

      d3.SetValue("books", docs3);
      d3.SetValue("pages", 40);
      col.Add(d1).Add(d2).Add(d3).Execute();
      string[] test1 = new string[] { "_id=1" };
      string[] test2 = new string[] { "_id=2" };
      var res = col.Find("$._id = 1").Execute().FetchAll();
      Assert.AreEqual(1, res.Count, "Matching the find count");
      Assert.AreEqual(d1.ToString(), res[0].ToString(), "Matching the string");
      res = col.Find("$._id = 2").Execute().FetchAll();
      Assert.AreEqual(1, res.Count, "Matching the find count");
      Assert.AreEqual(d2.ToString(), res[0].ToString(), "Matching the string");
      res = col.Find("$._id = 3").Execute().FetchAll();
      Assert.AreEqual(1, res.Count, "Matching the find count");
      Assert.AreEqual(d3.ToString(), res[0].ToString(), "Matching the string");

    }

    [Test, Description("Test MySQLX plugin Collection Dbdoc Scenarios - ID Present DbDoc Blank")]
    public void CollectionIDPresentDbdocBlank()
    {
      Collection col = CreateCollection("my_collection_1");
      Collection col1 = CreateCollection("my_collection_2");
      var d1 = new DbDoc();
      d1.SetValue("_id", 1);
      string[] a = { String.Empty };
      d1.SetValue("books", a);
      d1.SetValue("pages", 20);

      var d2 = new DbDoc();
      d2.SetValue("_id", 2);
      d2.SetValue("books", a);
      d2.SetValue("pages", 30);

      var d3 = new DbDoc();
      d3.SetValue("_id", 3);
      d3.SetValue("books", a);
      d3.SetValue("pages", 40);

      col.Add(d1).Add(d2).Add(d3).Execute();
      string[] test1 = new string[] { "_id=1" };
      string[] test2 = new string[] { "_id=2" };
      var res = col.Find("$._id = 1").Execute().FetchAll();
      Assert.AreEqual(1, res.Count, "Matching the find count");
      Assert.AreEqual(d1.ToString(), res[0].ToString(), "Matching the string");
      res = col.Find("$._id = 2").Execute().FetchAll();
      Assert.AreEqual(1, res.Count, "Matching the find count");
      Assert.AreEqual(d2.ToString(), res[0].ToString(), "Matching the string");
      res = col.Find("$._id = 3").Execute().FetchAll();
      Assert.AreEqual(1, res.Count, "Matching the find count");
      Assert.AreEqual(d3.ToString(), res[0].ToString(), "Matching the string");
    }

    /// <summary>
    /// Bug 23542055
    /// </summary>
    [Test, Description("Test MySQLX plugin Find with Many conditions")]
    [Ignore("Uncomment to execute")]
    public void FindWithManyConditions()
    {
      int i = 0;
      int Condition = 49;
      String query = "";
      var col = CreateCollection("my_collection_1");

      col.Add("{\"_id\":\"1002\",\"TEST1\":1111}").Execute();
      for (i = 0; i < Condition; i++)
      {
        if (i > 0)
          query = query + " OR ";
        query = query + "$.TEST1 > " + i;
      }
      var docs = col.Find(query).Execute();
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [Test, Description("Reading exclusively locked anonymous object array using LockShared without waiting option. ")]
    public void ExclusiveLocksAndCommit(int scenario)
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        ExecuteSQLStatement(session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)"));
        Result result1 = null;
        switch (scenario)
        {
          case 1:  // Anonymous Object
            var docs = new[]
              {
                  new {_id = 1, a = 1},
                  new {_id = 2, a = 2},
                  new {_id = 3, a = 3}
              };
            coll.Add(docs).Execute();
            break;
          case 2: // DbDoc
            for (int i = 0; i < 3; i++)
            {
              DbDoc DbDocs = new DbDoc();
              DbDocs.SetValue("_id", i);
              DbDocs.SetValue("a", i);
              result1 = coll.Add(DbDocs).Execute();
            }
            break;
          case 3: // Chained Add
            var docs1 = new { _id = 1, title = "Book 1", pages = 20, a = 1 };
            var docs2 = new { _id = 2, title = "Book 1", pages = 20, a = 2 };
            var docs3 = new { _id = 3, title = "Book 1", pages = 20, a = 3 };
            result1 = coll.Add(docs1).Add(docs2).Add(docs3).Execute();
            break;
          case 4: //Json string
            result1 = coll.Add(@"{ ""_id"": 1,""a"": 1 }", @"{""_id"": 2,""a"": 2 }", @"{""_id"": 3, ""a"": 3 }", @"{ ""_id"": 4,""a"": 4 }").Execute();
            break;
        }

        var coll2 = session2.GetSchema("test").GetCollection("test");
        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id = 1").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = :id").LockExclusive().Bind("id", 1)));

        Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("a = 1").Change("a", 10)));

        session.Commit();
        var result = coll2.Modify("a = 1").Set("a", 12).Execute();
        Assert.AreEqual(1, result.AffectedItemsCount);
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [TestCase(null)]
    [TestCase(LockMode.Shared)]
    [TestCase(LockMode.Exclusive)]
    [Test, Description("Reading a document which was locked using lock_shared without waiting option")]
    public void SharedLockAndCommit(LockMode? lockMode)
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher");
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
            new {_id = 1, a = 1},
            new {_id = 2, a = 2},
            new {_id = 3, a = 3}
        };

        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");

        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id = 1").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        switch (lockMode)
        {
          case LockMode.Shared:
            docResult = coll2.Find("_id = 2").LockShared().Execute();
            break;
          case LockMode.Exclusive:
            docResult = coll2.Find("_id = 2").LockExclusive().Execute();
            break;
          default:
            docResult = coll2.Find("_id = 2").Execute();
            break;
        }
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        // Session2 returns immediately.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        docResult = coll2.Find("_id = :id").Bind("id", 1).Execute();
        Assert.That(docResult.FetchAll(), Has.One.Items);

        // Session2 blocks due to to LockShared() not allowing to modify locked documents.
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("a = 1").Change("a", 10)));

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = coll2.Modify("a = 1").Set("a", 12).Execute();
        Assert.AreEqual(1, result.AffectedItemsCount);

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [TestCase(LockMode.Shared)]
    [TestCase(LockMode.Exclusive)]
    [Test, Description("Reading a document using lock_shared using SKIPLOCK and NOWAIT options with CRUD operations happening parallely.")]
    public void SharedAndExclusiveLockWithSkipAndNoWait(LockMode lockMode)
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher");
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        if (lockMode == LockMode.Shared)
          session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_m_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$.a'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex1`(`$ix_i_m_index`)").Execute();

        var docs = new[]
        {
            new {_id = 1, a = 1},
            new {_id = 2, a = 2},
            new {_id = 3, a = 3}
        };

        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");

        session.SQL("START TRANSACTION").Execute();
        var result1 = coll.Modify("a = 1").Set("a", 10).Execute();

        session2.SQL("START TRANSACTION").Execute();
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        DocResult docResult = null;
        switch (lockMode)
        {
          case LockMode.Exclusive:
            Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = :id").LockExclusive(LockContention.NoWait).Bind("id", 1)));
            docResult = coll2.Find("_id = :id").LockExclusive(LockContention.SkipLocked).Bind("id", 1).Execute();
            break;
          case LockMode.Shared:
            Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = :id").LockShared(LockContention.NoWait).Bind("id", 1)));
            docResult = coll2.Find("_id = :id").LockShared(LockContention.SkipLocked).Bind("id", 1).Execute();
            break;
        }
        Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the document ID");

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Multiple lock calls using NOWAIT and SKIPLOCK waiting option. ")]
    public void MultipleLocksWithNowaitAndSkiplock()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher");
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
            new {_id = 1, a = 1},
            new {_id = 2, a = 2},
            new {_id = 3, a = 3}
        };

        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");

        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id = 1").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();

        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = :id").LockExclusive(LockContention.SkipLocked).LockExclusive(LockContention.NoWait).Bind("id", 1)));

        docResult = coll2.Find("_id = :id").LockExclusive(LockContention.SkipLocked).LockShared(LockContention.NoWait).Bind("id", 1).Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        docResult = coll2.Find("_id = :id").LockExclusive(LockContention.SkipLocked).LockShared(LockContention.SkipLocked).Bind("id", 1).Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        docResult = coll2.Find("_id = :id").LockShared(LockContention.SkipLocked).LockExclusive(LockContention.SkipLocked).Bind("id", 1).Execute();
        Assert.AreEqual(0, docResult.FetchAll().Count, "Matching the document ID");

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading multiple rows in a locked document(lock_shared) using SKIPLOCK and NOWAIT ")]
    public void LockSharedMultipleReads()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher");
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
            new {_id = 1, a = 1},
            new {_id = 2, a = 2},
            new {_id = 3, a = 3}
        };

        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");

        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id >1").LockShared().Execute();
        Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();

        docResult = coll2.Find("_id < 3").LockShared(LockContention.SkipLocked).Execute();
        Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID");

        docResult = coll2.Find("_id < 3").LockShared(LockContention.NoWait).Execute();
        Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID");

        docResult = coll2.Find("_id < 3").LockExclusive(LockContention.SkipLocked).Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading multiple rows in an exclusively locked document using SKIPLOCK and NOWAIT ")]
    public void LockExclusiveMultipleReads()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher");
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_EXTRACT(doc, '$._id')) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
            new {_id = 1, a = 1},
            new {_id = 2, a = 2},
            new {_id = 3, a = 3}
        };

        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");

        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id >1").LockExclusive().Execute();
        Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();

        docResult = coll2.Find("_id < 3").LockShared(LockContention.SkipLocked).Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        docResult = coll2.Find("_id < 3").LockExclusive(LockContention.SkipLocked).Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Collection.Find() with shared lock and Collection.Modify() normal from two sessions. ")]
    public void LockSharedAndModify()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");
      ExecuteSQLStatement(session.SQL("SET autocommit = 0"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var docs = new[]
        {
            new {_id = 1, a = 1},
            new {_id = 2, a = 2},
            new {_id = 3, a = 3}
        };

        coll.Add(docs).Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");

        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id = 1").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        docResult = coll2.Find("_id = 1").Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Should allow to modify immediately since document isn't locked.
        var result = coll2.Modify("_id = 2").Set("a", 10).Execute();
        Assert.AreEqual(1, (int)result.AffectedItemsCount, "Match being done");
        Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("_id = 1").Change("a", 10)));

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }

      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Collection.Find() with shared lock from two sessions. ")]
    public void LockSharedTwoSessions()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");
      ExecuteSQLStatement(session.SQL("SET autocommit = 0"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        Collection coll = CreateCollection("test");
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
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        docResult = coll2.Find("_id = 2").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        // Should return immediately due to LockShared() allows reading by other sessions.
        docResult = coll2.Find("_id = 1").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }

      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Collection.Find() with exclusive lock and Collection.Find() with shared lock from two sessions. ")]
    public void LockExclusiveFindAndLockSharedFind()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");
      ExecuteSQLStatement(session.SQL("SET autocommit = 0"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
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
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        docResult = coll2.Find("_id = 2").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Session2 blocks due to LockExclusive() not allowing to read locked documents.
        Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockShared()));
        // Session unlocks documents.
        session.SQL("ROLLBACK").Execute();
        docResult = coll2.Find("_id = 1").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        docResult = coll.Find("_id = 1").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("ROLLBACK").Execute();
      }

      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Collection.Find() with shared lock and Collection.Find() with exclusive lock from two sessions. ")]
    public void LockSharedFindAndExclusiveLocks()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");
      ExecuteSQLStatement(session.SQL("SET autocommit = 0"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
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
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document IDs");
        docResult = coll.Find("_id = 3").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document IDs");
        session2.SQL("START TRANSACTION").Execute();

        // Should return immediately since document isn't locked.
        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        // Should return immediately due to LockShared() allows reading by other sessions.
        docResult = coll2.Find("_id = 2").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Session1 blocks due to LockExclusive() not allowing to read locked documents.
        coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive()));
        Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("_id = 1").Set("a", 100)));
        // Session unlocks documents.
        session2.SQL("ROLLBACK").Execute();
        session.SQL("ROLLBACK").Execute();
      }

      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Collection.Find() with exclusive lock and Collection.Find() with exclusive lock from two sessions. ")]
    public void LockExclusiveWithRollback()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");
      ExecuteSQLStatement(session.SQL("SET autocommit = 0"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
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
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document IDs");
        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately due to LockShared() allows reading by other sessions.
        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll.Find("_id = 2").LockExclusive()));
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Session1 blocks due to LockExclusive() not allowing to read locked documents.
        Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive()));
        // Session unlocks documents.
        session2.SQL("ROLLBACK").Execute();
        docResult = coll.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session.SQL("ROLLBACK").Execute();
        docResult = coll2.Find("_id = 1").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
      }

      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Collection.Find() with exclusive lock and Collection.Find() with exclusive lock from two sessions--Select multiple records ")]
    public void LockExclusiveWithINSelection()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher");
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Collection coll = CreateCollection("test");
        var docs = new[]
          {
              new {_id = 1, a = 1},
              new {_id = 2, a = 1},
              new {_id = 3, a = 1}
          };

        coll.Add(docs).Execute();
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
        var coll2 = session2.GetSchema("test").GetCollection("test");
        session.SQL("START TRANSACTION").Execute();
        var docResult = coll.Find("_id in (1,3)").LockExclusive().Execute();
        Assert.AreEqual(2, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        // Should return immediately due to LockShared() allows reading by other sessions.
        docResult = coll2.Find("_id = 2").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Session1 blocks due to LockExclusive() not allowing to read locked documents.
        coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        Assert.Throws<MySqlException>(() => ExecuteFindStatement(coll2.Find("_id = 1").LockExclusive()));
        Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll2.Modify("_id = 1").Set("a", 100)));
        // Session unlocks documents.
        session2.SQL("ROLLBACK").Execute();
        session.SQL("ROLLBACK").Execute();
      }

      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Collection.Find() with shared lock twice ")]
    public void LockSharedReadTwice()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");
      ExecuteSQLStatement(session.SQL("SET autocommit = 0"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        Collection coll = CreateCollection("test");
        session2.SQL("ALTER TABLE `test`.`test` ADD COLUMN `$ix_i_r_index` INT GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$._id'))) VIRTUAL NOT NULL, ADD UNIQUE INDEX `myIndex`(`$ix_i_r_index`)").Execute();
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
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        session2.SQL("START TRANSACTION").Execute();

        // Should return immediately since document isn't locked.
        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        docResult = coll2.Find("_id = 2").LockExclusive().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        docResult = coll2.Find("_id = 2").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        docResult = coll2.Find("_id = 2").LockShared().Execute();
        Assert.AreEqual(1, docResult.FetchAll().Count, "Matching the document ID");
        // Session unlocks documents.
        session2.SQL("ROLLBACK").Execute();
        session.SQL("ROLLBACK").Execute();
      }
      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Test MySQLX plugin Collection Array or Object contains operator Scenarios-1")]
    public void FindInJsonObjects()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");

      var col = CreateCollection("my_collection_1");
      string json = @"{ ""_id"": 0, ""title"": ""Book 0"" ,""pages"": 10,""name"": ""Jeoff Archer""}";
      Result r = col.Add(json).Execute();
      Assert.AreEqual(1, (int)r.AffectedItemsCount, "Matching Affected Records Count");
      var foundDocs = col.Find("1 in (1,2,3,6)").Execute();
      Assert.AreEqual(1, foundDocs.FetchAll().Count, "Matching Count");
      //var result = col.Find("0 in $._id").Fields("$.name as name, $.pages as pages, $.title as title").Execute();
      foundDocs = col.Find("'Book 0' in $.title").Execute();
      Assert.AreEqual(1, foundDocs.FetchAll().Count, "Matching Count");
      foundDocs = col.Find("100 in _id").Execute();
      Assert.AreEqual(0, foundDocs.FetchAll().Count, "Matching Count");
      foundDocs = col.Find("100 not in _id").Execute();
      Assert.AreEqual(1, foundDocs.FetchAll().Count, "Matching Count");

      json = @"{ ""_id"" : 99950, ""city"" : ""KETCHIKAN"", ""loc"" : ""[ -133.18479, 55.942471 ]"", ""pop"" : 422, ""state"" : ""AK"" }";
      r = col.Add(json).Execute();


      var d = new DbDoc(@"{ ""id"": 1, ""pages"": 20,
                     ""person"": { ""name"": ""Fred"", ""age"": 45 }
                     }");
      var d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("person", new { name = "Fred", age = 45 });

      Assert.AreEqual(d.Equals(d2), true, "Matching");
      col.Add(d).Execute();


      d = new DbDoc(@"{""id"":100,""FirstName"":""xyz"",""lastname"":""pqr"",
                         ""address"":
                         {""house"":44,""city"":""Delhi"",""country"":""india""}}");
      col.Add(d).Execute();

      d = new DbDoc(@"{""customerId"":100,""FirstName"":""xyz"",""lastname"":""pqr"",
                         ""address"":
                         {""house"":44,""city"":""Delhi"",""country"":""india""},
                         ""employer"":
                         {""cmpName"":""ABC"",""type"":""IT""}}");
      col.Add(d).Execute();

      d = new DbDoc(@"{ ""id"": 1, ""pages"": 20,
                       ""books"": [
                         {""_id"" : 1, ""title"" : ""Book 1""},
                         { ""_id"" : 2, ""title"" : ""Book 2"" }
                       ]
                   }");
      col.Add(d).Execute();

      var docs = new[] { new { _id = 1, title = "Book 1" }, new { _id = 2, title = "Book 2" } };
      d2 = new DbDoc();
      d2.SetValue("id", 100);
      d2.SetValue("pages", 20);
      d2.SetValue("books", docs);
      col.Add(d2).Execute();

      var result = col.Find("0 in $._id").Fields("$._id as _id,$.name as name, $.pages as pages, $.title as title").Execute();
      var res1 = result.FetchOne();
      Assert.AreEqual(0, res1["_id"]);
      Assert.AreEqual("Jeoff Archer", res1["name"]);
      Assert.AreEqual(10, res1["pages"]);
      Assert.AreEqual("Book 0", res1["title"]);

      result = col.Find("0 in $._id OR 1 in $._id").Fields("$._id as _id,$.name as name, $.pages as pages, $.title as title").Execute();
      res1 = result.FetchOne();
      Assert.AreEqual(0, res1["_id"]);
      Assert.AreEqual("Jeoff Archer", res1["name"]);
      Assert.AreEqual(10, res1["pages"]);
      Assert.AreEqual("Book 0", res1["title"]);

      result = col.Find("0 not in $._id").Fields("$._id as _id,$.name as name, $.pages as pages, $.title as title").Execute();
      var res2 = result.FetchAll();
      Assert.AreEqual(6, res2.Count, "Matching the find count");

      result = col.Find("'Jeoff Archer' in $.name").Execute();
      res1 = result.FetchOne();
      Assert.AreEqual(0, res1["_id"]);
      Assert.AreEqual("Jeoff Archer", res1["name"]);
      Assert.AreEqual(10, res1["pages"]);
      Assert.AreEqual("Book 0", res1["title"]);

      result = col.Find("0 not in $._id").Fields().Execute();
      res2 = result.FetchAll();
      Assert.IsNotNull(res2[0]["_id"]);

      var test = new DbDoc();
      test.SetValue("_id", 1);
      test.SetValue("age", 3488888888.9);
      test.SetValue("name",
          "ABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY");

      var coll = CreateCollection("my_collection_123456789");
      var res = coll.Add(test).Execute();
      var foundDocs2 = coll
          .Find(
              "'ABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY' in $.name")
          .Execute();
      var docs1 = foundDocs2.FetchAll();
      Assert.AreEqual(1, docs1.Count);
      foundDocs2 = coll.Find("3488888888.9 in $.age").Execute();
      docs1 = foundDocs2.FetchAll();
      Assert.AreEqual(1, docs1.Count);
    }

    [Test, Description("Test MySQLX plugin Collection Array or Object contains operator Scenarios-3")]
    public void FindAndCountJsonValues()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");

      var col = CreateCollection("my_collection_1");
      Result add;
      add = col.Add("{ \"name\": \"abcdefghijk\", \"age\": 1 , \"misc\": \"10-15-2017\"}")
          .Add("{ \"name\": \"xyz\", \"age\": 6 , \"misc\": \"19.5\"}").Execute();
      add = col.Add("{ \"name\": \"qwerty@#$%^&\", \"age\": 4 , \"misc\": \"11.9\"}").Execute();
      add = col.Add("{ \"name\": [\"name1\", \"name2\", \"name3\"], \"age\": 1 , \"misc\": \"1.2\"}").Execute();
      add = col.Add(
              "{ \"name\": {\"first\" : \"ABCDEF1\", \"middle\" : \"ABCDEF2\", \"last\" : \"ABCDEF3\"}, " +
              "\"age\": 1 , \"misc\": \"1.2\"}")
          .Execute();
      add = col.Add(
              "{ \"name\": {\"first\" : \"ABCDEF1\", \"middle\" : \"ABCDEF2\", \"last\" : \"ABCDEF3\"}, " +
              "\"age\": 2, \"misc\": \"1.2\"}")
          .Execute();
      var docs = col.Find().Execute();
      var result1 = col.Find("\"10-15-2017\" in $.misc").Execute().FetchAll().Count;
      Assert.AreEqual(1, result1);
      result1 = col.Find("\"10-15-2019\" in $.misc").Execute().FetchAll().Count;
      Assert.AreEqual(0, result1);
      var doc = docs.FetchOne();
      var result = col.Find("1 in $.age").Fields("$.name as name, $.age as age, $.misc as misc").Sort("name DESC").Execute();
      Assert.AreEqual(3, result.FetchAll().Count);
      var coll = CreateCollection("test");
      coll.Add(new DbDoc("{ \"a\": 1, \"b\": [ 1, \"value\" ], \"d\":\"\", \"ARR1\":[\"Field-1-Data-0\"] }")).Execute();
      result = coll.Find("JSON_TYPE($.ARR1) = 'ARRAY' AND \"Field-1-Data-0\" in $.ARR1").Execute();
      var count = result.FetchAll().Count;
      Assert.AreEqual(1, count);

    }

    [Test, Description("Test MySQLX plugin Collection Array or Object contains operator Scenarios-4")]
    public void CheckCountAfterSort()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("This test is for MySql 8.0.11 or higher.");
      var coll = CreateCollection("test");
      coll.Add("{ \"name\": \"abcdefghijk\", \"age\": 1 , \"misc\": 1.2}")
          .Add("{ \"name\": \"xyz\", \"age\": 6 , \"misc\": 19.59}").Execute();
      coll.Add("{ \"name\": \"qwerty@#$%^&\", \"age\": 4 , \"misc\": 11.9}").Execute();
      coll.Add("{ \"name\": \"name1\", \"age\": 4 , \"misc\": 11.9}").Execute();
      coll.Add("{ \"name\": [\"name1\", \"name2\", \"name3\"], \"age\": 1 , \"misc\": 1.2}").Execute();
      coll.Add("{ \"name\": {\"first\" : \"ABCDEF1\", \"middle\" : \"ABCDEF2\", \"last\" : \"ABCDEF3\"}, \"age\": 1 , \"misc\": 1.2}").Execute();
      var docs = coll.Find("4 in $.age").Execute().FetchAll().Count;
      Assert.True(docs > 0);
      var res3 = coll.Find("4 in $.age").Sort("name ASC").Execute().FetchAll();
      Assert.AreEqual(docs, res3.Count);
    }

    [Test, Description("Test MySQLX plugin Find with overlap Bugs")]
    public void FindUsingOverLapsBug()
    {
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");
      String json = "";
      String[] splName = { "+", "*", "/", "a+b", "#1", "%", "&", "@1", "!1", "~", "^",
                          "(", ")", "{", "}", "[", "]", "|", "JSON", "ADD", "JSON_EXTRACT", "JSON_OBJECT",
                          "?", "=", "+", ";", ",", ":", "<", ">", "-"};
      Collection coll = CreateCollection("test");
      for (int i = 0; i < splName.Length; i++)
      {
        coll.Add("{\"" + splName[i] + "\":\"data" + i + "\",\"ID\":" + i + "}").Execute();
        DocResult docs2 = coll.Find("$.ID OVERLAPS " + i).Fields("$.`" + splName[i] + "` as col1,$.ID as Id").Execute();
        var res2 = docs2.FetchOne();
        Assert.AreEqual(i.ToString(), res2["Id"].ToString(), "Matching the ID");
        if (i == 30)
          Assert.AreEqual("data" + i, "data30", "Matching the String");
        else
          Assert.AreEqual("data" + i, res2["col1"].ToString(), "Matching the String");
      }

      coll = CreateCollection("test");
      json = "{\"_id\":\"1005\",\"F1\": 123,\"F2\":\"ABCD\" }";
      coll.Add(json).Execute();
      json = "{\"_id\":\"1006\",\"F1\": 123,\"F2\":\"1234\" }";
      coll.Add(json).Execute();
      json = "{\"_id\":\"1007\",\"F1\": 123,\"F2\":\"S()R%^\" }";
      coll.Add(json).Execute();

      var docs1 = coll.Find().Fields("$._id as _id", "1 << 4 as tmp").Execute();
      var res = docs1.FetchAll();
      docs1 = coll.Find().Fields("$._id as _id", "$.F2 ^ 1 as tmp").Execute();
      res = docs1.FetchAll();
      coll.Add("{\"_id\":\"100001\",\"x1\":\"31\", \"x2\":\"13\", \"x3\":\"8\", \"x4\":\"18446744073709551614\"}").Execute();

      docs1 = coll.Find("CAST($.x1 as SIGNED) | pow(2,$.x1) = $.x1").Fields("$._id as _id, $.x1 as x1, $.x2 as x2, $.x3 as x3 , $.x2 | pow(2,$.x1) as tmp").Execute();
      res = docs1.FetchAll();

      docs1 = coll.Find("~16 = ~CAST($.F2 as SIGNED)").Fields("$._id as _id,$.F2 as f2, ~1 as tmp").Execute();
      res = docs1.FetchAll();
      int maxrec = 100;
      DbDoc newDoc = new DbDoc();
      newDoc.SetValue("_id", maxrec + 1000);
      newDoc.SetValue("F1", "Field-1-Data-" + maxrec);
      newDoc.SetValue("F2", "Field-2-Data-" + maxrec);
      newDoc.SetValue("F3", 300 + maxrec);
      coll.Add(newDoc).Execute();

      json = "{'_id':'" + (maxrec + 1000 + 1) + "','F1':'Field-1-Data-" + (maxrec + 1) + "','F2':'Field-2-Data-" + (maxrec + 1) + "','F3':" + (300 + maxrec + 1) + "}";
      json = json.Replace("'", "\"");
      coll.Add(json).Execute();
      json = "{'F1': 'Field-1-Data-9999','F2': 'Field-2-Data-9999','F3': 'Field-3-Data-9999'}".Replace("'", "\"");
      coll.Add(json).Add(json.Replace("9", "8")).Execute();

      var docs = coll.Find("$._id OVERLAPS 1100").Fields("$_id as _id,$.F1 as f1, $.F2 as f2, $.F3 as f3").Execute();
      var res1 = docs.FetchOne();
      Assert.AreEqual("1100", res1["_id"].ToString());
      Assert.AreEqual("Field-1-Data-100", res1["f1"].ToString());
      Assert.AreEqual("Field-2-Data-100", res1["f2"].ToString());
      Assert.AreEqual("400", res1["f3"].ToString());
      Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("$.F2 OVERLAPS #").Fields("$_id as _id,$.F1 as f1, $.F2 as f2, $.F3 as f3")));

      docs = coll.Find("$.F2 OVERLAPS 'ABCD'").Fields("$_id as _id,$.F1 as f1, $.F2 as f2, $.F3 as f3").Execute();
      res1 = docs.FetchOne();
      Assert.AreEqual("1005", res1["_id"]);
      Assert.AreEqual(123, res1["f1"]);
      Assert.AreEqual("ABCD", res1["f2"]);
      Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("$.F2 OVERLAPS [1234").Fields("$_id as _id,$.F1 as f1, $.F2 as f2, $.F3 as f3")));
      Assert.Throws<ArgumentException>(() => ExecuteFindStatement(coll.Find("$.F2 OVERLAPS S()R%^").Fields("$_id as _id,$.F1 as f1, $.F2 as f2, $.F3 as f3")));

      docs = coll.Find("$.F2 OVERLAPS 'S()R%^'").Fields("$_id as _id,$.F1 as f1, $.F2 as f2, $.F3 as f3").Execute();
      res1 = docs.FetchOne();
      Assert.AreEqual("1007", res1["_id"]);
      Assert.AreEqual(123, res1["f1"]);
      Assert.AreEqual("S()R%^", res1["f2"]);
    }

    [Test, Description("Test MySQLX plugin Find with overlap and Many conditions")]
    public void FindUsingOverLapsManyConditions()
    {
      if (!session.Version.isAtLeast(8, 0, 17)) Assert.Ignore("This test is for MySql 8.0.17 or higher.");
      int Condition = 45;
      int i, j = 0;
      String query = "";
      Collection coll = CreateCollection("test");
      for (i = 0; i < 50; i++)
      {
        coll.Add("{\"_id\":\"" + i + "\",\"TEST1\":" + (1000 + j) + "}").Execute();
        j++;
      }
      j = 0;
      for (i = 0; i < Condition; i++)
      {
        if (i > 0)
          query = query + " OR ";
        query = query + "$.TEST1 OVERLAPS " + (1000 + j);
        j++;
      }
      var docs = coll.Find(query).Execute();
      Assert.True(docs.FetchAll().Count > 0);
    }

    [Test, Description("Deprecated Find Where")]
    public void FindWhere()
    {
      Collection collection = CreateCollection("test");
      var docs = new[]
                {
                  new {  _id = 1, title = "Book 1", pages = 20 },
                  new {  _id = 2, title = "Book 2", pages = 30 },
                  new {  _id = 3, title = "Book 3", pages = 40 },
                  new {  _id = 4, title = "Book 4", pages = 50 },
                  };
      var result1 = collection.Add(docs).Execute();
      Assert.AreEqual(4, result1.AffectedItemsCount);

      //Deprecated Find().Where() in 8.0.17
      var result2 = collection.Find("$._id = 1").Where("true").Execute().FetchAll();
      Assert.AreEqual(4, result2.Count);
    }

    [Test, Description("Test MySQLX plugin Collection match count")]
    public void CollectionFindFieldMatchingCount()
    {
      Collection col = CreateCollection("my_collection_1");
      Collection col1 = CreateCollection("my_collection_2");

      var d1 = new DbDoc();
      d1.SetValue("_id", 1);
      d1.SetValue("books", "test1");
      d1.SetValue("count", 10);

      var d2 = new DbDoc();
      d2.SetValue("_id", 2);
      d2.SetValue("books", "test2");
      d2.SetValue("count", 20);

      var d3 = new DbDoc();
      d3.SetValue("_id", 3);
      d3.SetValue("books", "test3");
      d3.SetValue("count", 30);

      var d4 = new DbDoc();
      d4.SetValue("_id", 4);
      d4.SetValue("books", "test4");
      d4.SetValue("count", 40);

      var d5 = new DbDoc();
      d5.SetValue("_id", 5);
      d5.SetValue("books", "test5");
      d5.SetValue("count", 50);

      var d6 = new DbDoc();
      d6.SetValue("_id", 6);
      d6.SetValue("books", "test6");
      d6.SetValue("count", 0);

      var d7 = new DbDoc();
      d7.SetValue("_id", 0);
      d7.SetValue("books", "test7");
      d7.SetValue("count", 60);

      var final = col.Add(d1, d2).Add(d3).Execute();
      var res1 = col.Find().Fields("{\"_id\":\"1\",\"books\": \"test1\" }").Fields("{\"_id\":\"2\",\"books\": \"test2\" }").Fields("{\"_id\":\"3\",\"books\": \"test3\" }").Execute().FetchAll();
      res1 = col.Find().Fields(new string[] { "_id", "books", "count" }).Execute().FetchAll();
      Assert.AreEqual(3, res1.Count, "Matching the find count");
      Assert.AreEqual(d1.ToString(), res1[0].ToString(), "Matching the doc string 1");
      Assert.AreEqual(d2.ToString(), res1[1].ToString(), "Matching the doc string 2");
      Assert.AreEqual(d3.ToString(), res1[2].ToString(), "Matching the doc string 3");
      final = col.Add(new DbDoc[] { d4, d5 }).Execute();
      var res2 = col.Find().Fields("$._id as _id,$.books as books, $.count as count").Execute().FetchAll();
      Assert.AreEqual(5, res2.Count, "Matching the find count");
      Assert.AreEqual(d1.ToString(), res2[0].ToString(), "Matching the doc string 1");
      Assert.AreEqual(d2.ToString(), res2[1].ToString(), "Matching the doc string 2");
      Assert.AreEqual(d3.ToString(), res2[2].ToString(), "Matching the doc string 3");
      Assert.AreEqual(d4.ToString(), res2[3].ToString(), "Matching the doc string 4");
      Assert.AreEqual(d5.ToString(), res2[4].ToString(), "Matching the doc string 5");
      final = col.Add(d6, d7).Execute();
      var res3 = col.Find().Sort("count ASC").Execute().FetchAll();
      Assert.AreEqual(d6.ToString(), res3[0].ToString(), "Matching the doc string 7");
      Assert.AreEqual(d1.ToString(), res3[1].ToString(), "Matching the doc string 1");
      Assert.AreEqual(d2.ToString(), res3[2].ToString(), "Matching the doc string 2");
      Assert.AreEqual(d3.ToString(), res3[3].ToString(), "Matching the doc string 3");
      Assert.AreEqual(d4.ToString(), res3[4].ToString(), "Matching the doc string 4");
      Assert.AreEqual(d5.ToString(), res3[5].ToString(), "Matching the doc string 5");
      Assert.AreEqual(d7.ToString(), res3[6].ToString(), "Matching the doc string 6");
      var res4 = col.Find().Sort("count DESC").Execute().FetchAll();
      Assert.AreEqual(d7.ToString(), res4[0].ToString(), "Matching the doc string 6");
      Assert.AreEqual(d5.ToString(), res4[1].ToString(), "Matching the doc string 1");
      Assert.AreEqual(d4.ToString(), res4[2].ToString(), "Matching the doc string 2");
      Assert.AreEqual(d3.ToString(), res4[3].ToString(), "Matching the doc string 3");
      Assert.AreEqual(d2.ToString(), res4[4].ToString(), "Matching the doc string 4");
      Assert.AreEqual(d1.ToString(), res4[5].ToString(), "Matching the doc string 5");
      Assert.AreEqual(d6.ToString(), res4[6].ToString(), "Matching the doc string 7");
      col.Modify("_id = 1").Unset("count").Unset("books").Execute();
      col.Modify("_id = 1").Set("count", 10).Set("books", "test1").Execute();

    }

    [Test, Description("GetName,Schema and Count")]
    public void CollectionGetNameSchemaCount()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
      var col = CreateCollection("my_collection_123456789");

      Result r = col.Add(@"{ ""_id"": 1, ""foo"": 1 }").Execute();
      long count = col.Count();
      Assert.AreEqual(count, 1, "Matching the Collection Count");

      var collectionName = col.Name;
      Assert.AreEqual(collectionName, "my_collection_123456789", "Matching the collection Name");

      var schema = col.Schema.Name;
      Assert.AreEqual(schema, schemaName, "Matching the Schema Name");

      r = col.Add(@"{ ""_id"": 2, ""foo"": 2 }").Execute();
      count = col.Count();
      Assert.AreEqual(count, 2, "Matching the Collection Count");

      r = col.Remove("_id=2").Execute();
      count = col.Count();
      Assert.AreEqual(count, 1, "Matching the Collection Count");
      session.Schema.DropCollection("my_collection_123456789");
    }

    #endregion WL14389

  }
}

