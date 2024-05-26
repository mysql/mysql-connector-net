// Copyright © 2015, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;

namespace MySqlX.Data.Tests
{
  public class TransactionTests : BaseTest
  {
    private string collName = "collSp";

    [TearDown]
    public void TearDown() => session.Schema.DropCollection(collName);

    [Test]
    public void Commit()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      // start the transaction
      coll.Session.StartTransaction();

      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(4));

      // now roll it back
      coll.Session.Commit();

      var foundDocs = ExecuteFindStatement(coll.Find());
      Assert.That(foundDocs.Next());
      Assert.That(foundDocs.Next());
      Assert.That(foundDocs.Next());
      Assert.That(foundDocs.Next());
      Assert.That(foundDocs.Next(), Is.False);
    }

    [Test]
    public void Rollback()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      // start the transaction
      coll.Session.StartTransaction();

      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(4));

      // now roll it back
      coll.Session.Rollback();

      var foundDocs = ExecuteFindStatement(coll.Find());
      Assert.That(foundDocs.Next(), Is.False);
    }

    #region Savepoints

    [Test]
    public void CreateUnnamedSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        session.StartTransaction();

        string spName = session.SetSavepoint();
        Assert.That(string.IsNullOrWhiteSpace(spName), Is.False);

        session.Rollback();
      }
    }

    [Test]
    public void RollbackToSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        var schema = session.GetSchema("test");
        schema.DropCollection("collSP");
        var coll = schema.CreateCollection("collSP");

        session.StartTransaction();

        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));
        var sp = session.SetSavepoint();
        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll().Count, Is.EqualTo(2));
        session.RollbackTo(sp);
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll(), Has.One.Items);

        session.Rollback();
      }
    }

    [Test]
    public void ReleaseSavepoint()
    {
      using (var sessionTest = MySQLX.GetSession(ConnectionString))
      {
        var coll = CreateCollection("test");
        sessionTest.StartTransaction();
        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));
        var sp = sessionTest.SetSavepoint();
        ExecuteAddStatement(coll.Add("{ \"test2\": \"test2\" }"));
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll().Count, Is.EqualTo(2));
        sessionTest.ReleaseSavepoint(sp);
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll().Count, Is.EqualTo(2));
        sessionTest.Rollback();
      }
    }

    [Test]
    public void CreateNamedSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        session.StartTransaction();

        string spName = session.SetSavepoint("mySavedPoint");
        Assert.That(string.IsNullOrWhiteSpace(spName), Is.False);

        session.Rollback();
      }
    }

    [Test]
    public void RollbackToNamedSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        var schema = session.GetSchema("test");
        schema.DropCollection("collSP");
        var coll = schema.CreateCollection("collSP");
        session.StartTransaction();
        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));
        var sp = session.SetSavepoint("mySavedPoint");
        ExecuteAddStatement(coll.Add("{ \"test2\": \"test2\" }"));
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll().Count, Is.EqualTo(2));
        session.RollbackTo(sp);
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll(), Has.One.Items);
        session.Rollback();
      }
    }

    [Test]
    public void ReleaseNamedSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        var schema = session.GetSchema("test");
        schema.DropCollection("test");
        var coll = schema.CreateCollection("test");
        session.StartTransaction();
        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));
        var sp = session.SetSavepoint("mySavedPoint");
        ExecuteAddStatement(coll.Add("{ \"test2\": \"test2\" }"));
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll().Count, Is.EqualTo(2));
        session.ReleaseSavepoint(sp);
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll().Count, Is.EqualTo(2));
        session.Rollback();
      }
    }

    [Test]
    public void NonExistentSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        session.StartTransaction();

        Exception exception = Assert.Throws<MySqlException>(() => session.RollbackTo("nonExistentSavePoint"));
        Assert.That(exception.Message, Is.EqualTo("SAVEPOINT nonExistentSavePoint does not exist"));

        exception = Assert.Throws<MySqlException>(() => session.ReleaseSavepoint("nonExistentSavePoint"));
        Assert.That(exception.Message, Is.EqualTo("SAVEPOINT nonExistentSavePoint does not exist"));

        session.Rollback();
      }
    }

    [Test]
    public void CreateSavepointWithWeirdNames()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        string errorMessage = "You have an error in your SQL syntax";
        session.StartTransaction();

        Exception ex = Assert.Throws<MySqlException>(() => session.SetSavepoint(""));
        Assert.That(ex.Message, Does.StartWith(errorMessage));
        ex = Assert.Throws<MySqlException>(() => session.SetSavepoint(" "));
        Assert.That(ex.Message, Does.StartWith(errorMessage));
        ex = Assert.Throws<MySqlException>(() => session.SetSavepoint(null));
        Assert.That(ex.Message, Does.StartWith(errorMessage));
        ex = Assert.Throws<MySqlException>(() => session.SetSavepoint("-"));
        Assert.That(ex.Message, Does.StartWith(errorMessage));
        ex = Assert.Throws<MySqlException>(() => session.SetSavepoint("mysp+"));
        Assert.That(ex.Message, Does.StartWith(errorMessage));
        ex = Assert.Throws<MySqlException>(() => session.SetSavepoint("3306"));
        Assert.That(ex.Message, Does.StartWith(errorMessage));

        var sp = session.SetSavepoint("_");
        session.RollbackTo(sp);
        sp = session.SetSavepoint("mysql3306");
        session.RollbackTo(sp);

        session.Rollback();
      }
    }

    [Test]
    public void OverwriteSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        var schema = session.GetSchema("test");
        var coll = schema.CreateCollection(collName);

        session.StartTransaction();

        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));
        var sp = session.SetSavepoint("mySP");
        ExecuteAddStatement(coll.Add("{ \"test2\": \"test2\" }"));
        sp = session.SetSavepoint("mySP");
        ExecuteAddStatement(coll.Add("{ \"test3\": \"test3\" }"));
        sp = session.SetSavepoint("mySP");
        ExecuteAddStatement(coll.Add("{ \"test4\": \"test4\" }"));
        sp = session.SetSavepoint("mySP");
        session.RollbackTo(sp);
        Assert.That(ExecuteFindStatement(coll.Find()).FetchAll().Count, Is.EqualTo(4));

        session.Rollback();
      }
    }

    [Test]
    public void MultipleReleasesForSavepoint()
    {
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        session.StartTransaction();

        var sp = session.SetSavepoint("mySP");
        session.ReleaseSavepoint(sp);
        Exception exception = Assert.Throws<MySqlException>(() => session.ReleaseSavepoint(sp));
        Assert.That(exception.Message, Is.EqualTo(string.Format("SAVEPOINT {0} does not exist", sp)));

        session.Rollback();
      }
    }

    [Test]
    public void RollbackAndReleaseAfterTransactionCommit()
    {
      using (var sessionTest = MySQLX.GetSession(ConnectionString))
      {
        var coll = CreateCollection("collSP");
        sessionTest.StartTransaction();
        var sp = sessionTest.SetSavepoint("mySP");
        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));
        sessionTest.Commit();
        Exception exception = Assert.Throws<MySqlException>(() => sessionTest.RollbackTo(sp));
        Assert.That(exception.Message, Is.EqualTo(string.Format("SAVEPOINT {0} does not exist", sp)));
        exception = Assert.Throws<MySqlException>(() => sessionTest.ReleaseSavepoint(sp));
        Assert.That(exception.Message, Is.EqualTo(string.Format("SAVEPOINT {0} does not exist", sp)));
      }
    }

    [Test]
    public void RollbackAndReleaseAfterTransactionRollback()
    {
      using (var sessionTest = MySQLX.GetSession(ConnectionString))
      {
        var coll = CreateCollection("collSP");
        sessionTest.StartTransaction();

        var sp = sessionTest.SetSavepoint("mySP");
        ExecuteAddStatement(coll.Add("{ \"test\": \"test\" }"));

        sessionTest.Rollback();

        Exception exception = Assert.Throws<MySqlException>(() => sessionTest.RollbackTo(sp));
        Assert.That(exception.Message, Is.EqualTo(string.Format("SAVEPOINT {0} does not exist", sp)));

        exception = Assert.Throws<MySqlException>(() => sessionTest.ReleaseSavepoint(sp));
        Assert.That(exception.Message, Is.EqualTo(string.Format("SAVEPOINT {0} does not exist", sp)));
      }
    }

    #endregion

    #region WL14389

    [Test, Description("Session Close Transaction")]
    public void SessionCloseTransaction()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      using (Session sessionTest = MySQLX.GetSession(ConnectionString))
      {
        Schema db = null;
        db = sessionTest.GetSchema("test");
        sessionTest.StartTransaction();

        if (db.GetCollection("my_collection_1").ExistsInDatabase())
        {
          db.DropCollection("my_collection_1");
        }
        Collection col = db.CreateCollection("my_collection_1");
        sessionTest.Close();
        Assert.Throws<ObjectDisposedException>(() => sessionTest.Rollback());
      }
    }

    [Test, Description("Valid Commit and Check Warning ")]
    public void CommitValidWarning()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      using (Session sessionPlain = MySQLX.GetSession(ConnectionString))
      {
        sessionPlain.SetCurrentSchema("test");
        sessionPlain.SQL("drop table if exists temp").Execute();
        sessionPlain.SQL("CREATE TABLE temp(id INT primary key)").Execute();
        Table table = sessionPlain.Schema.GetTable("temp");
        sessionPlain.StartTransaction();
        table.Insert().Values(5).Execute();
        sessionPlain.Commit();
        Assert.That(table.Count(), Is.EqualTo(1));
      }
    }

    /// <summary>
    ///   Bug 23542005
    /// </summary>
    [Test, Description("Invalid Commit and Check Warning ")]
    public void CommitInvalidWarning()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      using (Session sessionPlain = MySQLX.GetSession(ConnectionString))
      {
        sessionPlain.SetCurrentSchema("test");
        sessionPlain.SQL("CREATE TABLE temp(v VARCHAR(4))").Execute();
        Table table = sessionPlain.Schema.GetTable("temp");
        sessionPlain.StartTransaction();
        Assert.Throws<MySqlException>(() => sessionPlain.GetSchema("test").GetTable("temp").Insert().Values("abcdef").Execute());
        sessionPlain.Commit();
        Assert.That(table.Count(), Is.EqualTo(0));
        var warnings = sessionPlain.SQL("DROP TABLE IF EXISTS temp1").Execute().Warnings;
        Assert.That(warnings.Count > 0);
        sessionPlain.Commit();
      }
    }


    [Test, Description("Valid Rollback and Check Warning ")]
    public void RollbackValidWarning()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      using (Session sessionPlain = MySQLX.GetSession(ConnectionString))
      {
        sessionPlain.SetCurrentSchema("test");
        sessionPlain.SQL("DROP TABLE IF EXISTS temp").Execute();
        sessionPlain.SQL("CREATE TABLE temp(id INT primary key)").Execute();
        Table table = sessionPlain.Schema.GetTable("temp");
        sessionPlain.StartTransaction();
        var res = table.Insert().Values(5).Execute();
        sessionPlain.Rollback();
        Assert.That(table.Count(), Is.EqualTo(0));
        Assert.That(res.Warnings.Count, Is.EqualTo(0));
      }
    }

    /// <summary>
    ///   Bug 23542005
    /// </summary>
    [Test, Description("Invalid Rollback and Check Warning ")]
    public void RollbackInvalidWarning()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      using (Session sessionPlain = MySQLX.GetSession(ConnectionString))
      {
        sessionPlain.SetCurrentSchema("test");
        sessionPlain.SQL("DROP TABLE IF EXISTS temp").Execute();
        sessionPlain.SQL("CREATE TABLE temp(v VARCHAR(4))").Execute();
        Table table = sessionPlain.Schema.GetTable("temp");
        sessionPlain.StartTransaction();
        Assert.Throws<MySqlException>(() => sessionPlain.GetSchema("test").GetTable("temp").Insert().Values("abcdef").Execute());
        sessionPlain.Rollback();
        Assert.That(table.Count(), Is.EqualTo(0));
        var res = sessionPlain.SQL("DROP TABLE IF EXISTS temp1").Execute();
        Assert.That(res.Warnings.Count > 0);
      }
    }

    /// <summary>
    ///   Bug 23542005
    /// </summary>
    [Test, Description("Commit Rollback Invalid Warning")]
    public void CommitRollbackInvalidWarning()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      Collection coll = CreateCollection("test");
      var docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };

      var docs2 = new[]
      {
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      var docs3 = new[]

      {
        new {  _id = 5, title = "Book 5", pages = 60 },
        new {  _id = 6, title = "Book 6", pages = 70 },
      };

      // start the transaction
      coll.Session.StartTransaction();

      Result r = coll.Add(docs1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      r = coll.Add(docs2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2));

      r = coll.Add(docs3).Execute();
      Assert.Throws<MySqlException>(() => coll.Add(docs3).Execute());
      coll.Session.Commit();
      Assert.That(coll.Count(), Is.EqualTo(6));
      // now Rollback
      coll.Session.Rollback();
      Assert.That(coll.Count(), Is.EqualTo(6));

      var foundDocs = coll.Find().Execute();
      Assert.That(foundDocs, Is.Not.Null);
    }

    [Test, Description("MySQLX plugin Warnings")]
    public void Warnings()
    {
      if (!session.Version.isAtLeast(8, 0, 0)) return;
      using (Session sessionTest = MySQLX.GetSession(ConnectionString))
      {
        Schema schema = sessionTest.GetSchema("test");
        Collection coll = CreateCollection("test");

        Result r = coll.Add("{ \"foo\": 1 }").Execute();
        Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
        Assert.That(coll.Count(), Is.EqualTo(1));
        r = coll.Add("{ \"fool\": 2 }").Execute();
        Assert.That(r.Warnings.Count, Is.EqualTo(0));
        r = coll.Add("{ \"fool\": 10000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 }").Execute();
        Assert.That(r.Warnings.Count, Is.EqualTo(0));

        sessionTest.SQL("use test").Execute();
        sessionTest.SQL("CREATE TABLE nontrac(id INT primary key) ENGINE=MyISAM;").Execute();
        Table table = schema.GetTable("nontrac");
        sessionTest.StartTransaction();
        table.Insert().Values(5).Execute();

        schema = sessionTest.GetSchema(schemaName);
        sessionTest.StartTransaction();

        var res = sessionTest.SQL("drop table if exists t1, t2").Execute();
        Assert.That(res.Warnings.Count, Is.EqualTo(2));
        Assert.That(res.Warnings[0].Code, Is.EqualTo(1051));

        Assert.That(res.Warnings[1].Code, Is.EqualTo(1051));

        res = sessionTest.SQL("create table t1 (a int) engine=innodb").Execute();
        Assert.That(res.Warnings.Count, Is.EqualTo(0));
        res = sessionTest.SQL("create table t2 (a int) engine=myisam").Execute();
        Assert.That(res.Warnings.Count, Is.EqualTo(0));
        res = sessionTest.SQL("insert into t1 values(1)").Execute();
        Assert.That(res.Warnings.Count, Is.EqualTo(0));
        res = sessionTest.SQL("insert into t2 select * from t1").Execute();
        Assert.That(res.Warnings.Count, Is.EqualTo(1));
        sessionTest.Commit();

      }
    }

    //Savepoints
    [Test, Description("Rollback to same savepoint multiple times")]
    public void RollbackToSameSavepoint()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      var col = CreateCollection("my_collection");
      session.StartTransaction();
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 30 },
        new {  _id = 2, title = "Book 2", pages = 50 },
      };
      Result result = col.Add(data).Execute();
      var sp = session.SetSavepoint("SavePoint1");
      data = new object[]
      {
        new {  _id = 3, title = "Book 3", pages = 30 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      result = col.Add(data).Execute();
      var sp1 = session.SetSavepoint("SavePoint2");

      session.RollbackTo(sp);
      data = new object[]
      {
        new {  _id = 5, title = "Book 5", pages = 30 },
        new {  _id = 6, title = "Book 6", pages = 50 },
      };
      result = col.Add(data).Execute();
      session.RollbackTo(sp);
      var doc = col.Find().Execute();
      var docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(2));
    }


    [Test, Description("Releasing a savepoint multiple times")]
    public void ReleaseSavepointMoreThanOnce()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      using (Session sessionTest = MySQLX.GetSession(ConnectionString))
      {
        var col = CreateCollection("my_collection");
        sessionTest.StartTransaction();
        object[] data = new object[]
        {
          new {  _id = 1, title = "Book 1", pages = 30 },
          new {  _id = 2, title = "Book 2", pages = 50 },
        };
        Result result = col.Add(data).Execute();
        var sp = sessionTest.SetSavepoint("SavePoint1");
        data = new object[]
        {
          new {  _id = 3, title = "Book 3", pages = 30 },
          new {  _id = 4, title = "Book 4", pages = 50 },
        };
        result = col.Add(data).Execute();
        sessionTest.ReleaseSavepoint(sp);
        data = new object[]
        {
          new {  _id = 5, title = "Book 5", pages = 30 },
          new {  _id = 6, title = "Book 6", pages = 50 },
        };
        result = col.Add(data).Execute();
        var ex = Assert.Throws<MySqlException>(() => sessionTest.ReleaseSavepoint(sp));
        sessionTest.Rollback();
      }
    }

    [TestCase("Savepoint1", "Savepoint2")]
    [TestCase("", "")]
    [Description("Creating multiple savepoints with SetSavepoint([name]) and rolling back to a specific one")]
    public void MultipleSavepointsAndRollback(string savePoint1, string savePoint2)
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      var col = CreateCollection("my_collection");
      session.StartTransaction();
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 30 },
        new {  _id = 2, title = "Book 2", pages = 50 },
      };
      Result result = col.Add(data).Execute();
      var sp = string.IsNullOrEmpty(savePoint1) ? session.SetSavepoint() : session.SetSavepoint(savePoint1);
      data = new object[]
      {
        new {  _id = 3, title = "Book 3", pages = 30 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      result = col.Add(data).Execute();
      var sp2 = string.IsNullOrEmpty(savePoint2) ? session.SetSavepoint() : session.SetSavepoint(savePoint2);
      session.RollbackTo(sp);
      var doc = col.Find().Execute();
      var docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(2));
    }

    [Test, Description("Test creating a savepoint without starting a transaction")]
    public void SavepointWithoutTransaction()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      var col = CreateCollection("my_collection");
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 30 },
        new {  _id = 2, title = "Book 2", pages = 50 },
      };
      Result result = col.Add(data).Execute();
      var sp = "";
      sp = session.SetSavepoint("SP");
      data = new object[]
      {
        new { _id =3, title = "Book 3", pages = 30 },
        new { _id =4, title = "Book 4", pages = 50 },
      };
      result = col.Add(data).Execute();
      var sp1 = session.SetSavepoint("SP");
      Assert.Throws<MySqlException>(() => session.ReleaseSavepoint(sp1));
    }

    [Test, Description("Validate that further savepoints get released once you release a preceding savepoint")]
    public void ValidateSavepointsReleased()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      var col = CreateCollection("my_collection");
      session.StartTransaction();

      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 30 },
        new {  _id = 2, title = "Book 2", pages = 50 },
      };
      Result result = col.Add(data).Execute();
      var sp = session.SetSavepoint("SavePoint1");
      data = new object[]
      {
        new {  _id = 3, title = "Book 3", pages = 30 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      result = col.Add(data).Execute();
      var sp1 = session.SetSavepoint("SavePoint2");
      session.ReleaseSavepoint(sp);
      Assert.Throws<MySqlException>(() => session.ReleaseSavepoint(sp1));

      var doc = col.Find().Execute();
      var docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(4));

    }


    [Test, Description("Validate Nested-transactions with multiple savepoints")]
    public void NestedTransactions()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      var col = CreateCollection("my_collection");
      session.StartTransaction();
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 30 },
        new {  _id = 2, title = "Book 2", pages = 50 },
      };
      Result result = col.Add(data).Execute();
      var sp = session.SetSavepoint("Savepoint1");
      session.StartTransaction();

      data = new object[]
      {
        new {  _id = 3, title = "Book 3", pages = 30 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      result = col.Add(data).Execute();
      var sp1 = session.SetSavepoint("Savepoint2");
      session.Rollback();
      var doc = col.Find().Execute();
      var docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(2));
    }

    [Test, Description("Test the behaviour of Savepoints created immediately after one another")]
    public void SavepointsCreatedImmediately()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      var col = CreateCollection("my_collection");
      session.StartTransaction();
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 30 },
        new {  _id = 2, title = "Book 2", pages = 50 },
      };
      Result result = col.Add(data).Execute();
      var sp = session.SetSavepoint("Savepoint1");
      var sp1 = session.SetSavepoint("Savepoint2");
      session.RollbackTo(sp);
      var doc = col.Find().Execute();
      var docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(2));
    }

    [Test, Description("Test MySQLX plugin Commit After Commit")]
    public void CommitAfterCommit()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Collection coll = CreateCollection("test");
      var docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };

      var docs2 = new[]
      {
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      var docs3 = new[]
      {
        new {  _id = 5, title = "Book 5", pages = 60 },
        new {  _id = 6, title = "Book 6", pages = 70 },
      };

      var docs4 = new[]
      {
        new {  _id = 7, title = "Book 7", pages = 80 },
        new {  _id = 8, title = "Book 8", pages = 90 },
      };

      // start the transaction
      coll.Session.StartTransaction();

      Result r = coll.Add(docs1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      // now Commit
      coll.Session.Commit();
      // start the transaction
      coll.Session.StartTransaction();
      r = coll.Add(docs4).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");
      // now Commit Again
      coll.Session.Commit();

      var foundDocs = coll.Find().Execute();
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(false), "Matching");

    }

    [Test, Description("Test MySQLX plugin Rollback Multiple")]
    public void RollBackMultiple()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Collection coll = CreateCollection("test");
      var docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };

      var docs2 = new[]
      {
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      var docs3 = new[]
      {
        new {  _id = 5, title = "Book 5", pages = 60 },
        new {  _id = 6, title = "Book 6", pages = 70 },
      };

      // start the transaction
      coll.Session.StartTransaction();

      Result r = coll.Add(docs1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      // now Rollback
      coll.Session.Rollback();

      var foundDocs = coll.Find().Execute();
      Assert.That(foundDocs.Next(), Is.EqualTo(false), "Matching");

    }

    [Test, Description("Test MySQLX plugin Rollback after RollBack")]
    public void RollBackAfterRollBack()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      Collection coll = CreateCollection("test");
      var docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };

      var docs2 = new[]
      {
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      var docs3 = new[]
      {
        new {  _id = 5, title = "Book 5", pages = 60 },
        new {  _id = 6, title = "Book 6", pages = 70 },
      };

      var docs4 = new[]
      {
        new {  _id = 7, title = "Book 7", pages = 80 },
        new {  _id = 8, title = "Book 8", pages = 90 },
      };

      // start the transaction
      coll.Session.StartTransaction();

      Result r = coll.Add(docs1).Execute();
      //WL11843-Core API v1 alignment Changes
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      // now Rollback
      coll.Session.Rollback();

      // start the transaction
      coll.Session.StartTransaction();

      r = coll.Add(docs4).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      // now Rollback Again
      coll.Session.Rollback();

      var foundDocs = coll.Find().Execute();
      Assert.That(foundDocs.Next(), Is.EqualTo(false), "Matching");

    }

    [Test, Description("Test MySQLX plugin Commit Rollback")]
    public void CommitRollBack()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Collection coll = CreateCollection("test");
      var docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };

      var docs2 = new[]
      {
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      var docs3 = new[]
      {
        new {  _id = 5, title = "Book 5", pages = 60 },
        new {  _id = 6, title = "Book 6", pages = 70 },
      };

      // start the transaction
      coll.Session.StartTransaction();

      Result r = coll.Add(docs1).Execute();
      //WL11843-Core API v1 alignment Changes
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      r = coll.Add(docs3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      //now Commit
      coll.Session.Commit();
      // now Rollback
      coll.Session.Rollback();

      var foundDocs = coll.Find().Execute();
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(false), "Matching");

    }

    [Test, Description("Test MySQLX plugin RollBack Commit")]
    public void RollBackCommit()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Collection coll = CreateCollection("test");
      var docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };

      var docs2 = new[]
      {
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      var docs3 = new[]
      {
        new {  _id = 5, title = "Book 5", pages = 60 },
        new {  _id = 6, title = "Book 6", pages = 70 },
      };

      // start the transaction
      coll.Session.StartTransaction();
      Result r = coll.Add(docs1).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");

      // now Rollback
      coll.Session.Rollback();
      r = coll.Add(docs2).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");
      r = coll.Add(docs3).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(2), "Matching");
      //now Commit
      coll.Session.Commit();
      var foundDocs = coll.Find().Execute();
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Matching");
      Assert.That(foundDocs.Next(), Is.EqualTo(false), "Matching");

    }

    #endregion

  }
}
