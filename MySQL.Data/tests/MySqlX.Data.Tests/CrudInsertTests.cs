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
using MySqlX.Common;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using System;
using NUnit.Framework;

namespace MySqlX.Data.Tests
{
  public class CrudInsertTests : BaseTest
  {
    [Test]
    public void InsertSingleDbDocWithId()
    {
      Collection coll = CreateCollection("test");
      Result r = ExecuteAddStatement(coll.Add(@"{ ""_id"": 1, ""foo"": 1 }"));
      Assert.AreEqual(1, r.AffectedItemsCount);
      Assert.AreEqual(1, coll.Count());
    }

    [Test]
    public void InsertSingleDbDocWithoutId()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add("{ \"foo\": 1 }");
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.AreEqual(5115u, Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code);
        return;
      }
      Result r = ExecuteAddStatement(stmt);
      Assert.AreEqual(1, r.AffectedItemsCount);
      Assert.AreEqual(1, coll.Count());
      Assert.That(r.GeneratedIds, Has.One.Items);
      Assert.False(string.IsNullOrWhiteSpace(r.GeneratedIds[0]));
    }

    [Test]
    public void InsertMultipleDbDocWithoutId()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add("{ \"foo\": 1 }")
        .Add("{ \"amber\": 2 }")
        .Add("{ \"any\": 3 }");
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.AreEqual(5115u, Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code);
        return;
      }
      Result r = ExecuteAddStatement(stmt);
      Assert.AreEqual(3, r.AffectedItemsCount);
      Assert.AreEqual(3, coll.Count());
      Assert.AreEqual(3, r.GeneratedIds.Count);
    }

    [Test]
    public void InsertAnonymousObjectWithId()
    {
      var obj = new { _id = "5", name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      Result r = ExecuteAddStatement(coll.Add(obj));
      Assert.AreEqual(1, r.AffectedItemsCount);
      //TODO:  pull object and verify data
      Assert.AreEqual(1, coll.Count());
    }

    [Test]
    public void InsertAnonymousObjectWithNoId()
    {
      var obj = new { name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      var stmt = coll.Add(obj);
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.AreEqual(5115u, Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code);
        return;
      }
      Result r = ExecuteAddStatement(stmt);
      Assert.AreEqual(1, r.AffectedItemsCount);
      //TODO:  pull object and verify data
      Assert.AreEqual(1, coll.Count());
      Assert.That(r.GeneratedIds, Has.One.Items);
      Assert.False(string.IsNullOrWhiteSpace(r.GeneratedIds[0]));
    }

    [Test]
    public void InsertMultipleAnonymousObjectsWithId()
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
      Assert.AreEqual(4, coll.Count());
    }

    [Test]
    public void ValidatesDocumentIds()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add(new { name = "Book 1" });
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.AreEqual(5115u, Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code);
        return;
      }
      Result result = ExecuteAddStatement(stmt);
      Assert.AreEqual(1, result.AffectedItemsCount);

      result = ExecuteModifyStatement(coll.Modify($"_id = '{result.GeneratedIds[0]}'").Set("pages", "20"));
      Assert.AreEqual(1, result.AffectedItemsCount);
      CollectionAssert.IsEmpty(result.GeneratedIds);
    }

    [Test]
    public void ReuseStatement()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      var stmt = coll.Add(new { _id = 0 });
      foreach (var doc in docs)
      {
        stmt.Add(doc);
      }
      Result r = ExecuteAddStatement(stmt);
      Assert.AreEqual((ulong)docs.Length + 1, r.AffectedItemsCount);
      Assert.AreEqual(5, coll.Count());
    }

    [Test]
    public void EmptyDocArray()
    {
      Collection coll = CreateCollection("test");

      var insertResult = ExecuteAddStatement(coll.Add(new DbDoc[] { }));
      Assert.AreEqual(0ul, insertResult.AffectedItemsCount);

      var result = ExecuteFindStatement(coll.Find()).FetchAll();
      CollectionAssert.IsEmpty(result);
    }

    [Test]
    public void NullParameter()
    {
      Collection coll = CreateCollection("test");

      Assert.Throws<ArgumentNullException>(() => ExecuteAddStatement(coll.Add(null)));
    }

    [Test]
    public void InsertingArray()
    {
      var docs = new[]
      {
        new {  id = 1, title = "Book 1" },
        new {  id = 2, title = "Book 2" },
      };
      DbDoc d2 = new DbDoc();
      d2.SetValue("_id", 1);
      d2.SetValue("books", docs);
      d2.SetValue("pages", 20);

      Collection coll = CreateCollection("test");
      ExecuteAddStatement(coll.Add(d2));
      var result = ExecuteFindStatement(coll.Find()).FetchAll();
      Assert.That(result, Has.One.Items);
      Assert.AreEqual(d2.ToString(), result[0].ToString());
    }

    [Test]
    public void CompareGuids()
    {
      Guid guid1 = new Guid();
      Guid guid2 = new Guid();
      Assert.AreEqual(0, Tools.CompareGuids(guid1, guid2));
      Assert.AreEqual(0, Tools.CompareGuids(guid1.ToString(), guid2.ToString()));

      guid1 = Guid.NewGuid();
      guid2 = Guid.NewGuid();
      Assert.True(Tools.CompareGuids(guid1, guid2) != 0);
      Assert.True(Tools.CompareGuids(guid1.ToString(), guid2.ToString()) != 0);
    }

    [Test]
    public void InsertNullValuesAsDbDoc()
    {
      Collection collection = CreateCollection("test");

      var nullValues = new String[] { null, "null", "NULL" };
      var docs = new DbDoc[3];
      for (int i = 0; i < docs.Length; i++)
      {
        docs[i] = new DbDoc();
        docs[i].SetValue("a", nullValues[i]);
        docs[i].SetValue("_id", (i + 1));
      }

      ExecuteAddStatement(collection.Add(docs));
      var result = ExecuteFindStatement(collection.Find()).FetchAll();
      Assert.AreEqual(docs.Length, result.Count);

      for (int i = 0; i < docs.Length; i++)
        Assert.AreEqual(docs[i].ToString(), result[i].ToString());
    }

    [Test]
    public void InsertNullValuesAsJson()
    {
      var docs = new[]
      {
        @"{ ""_id"": 1, ""foo"": null}",
        @"{ ""_id"": 2, ""foo"": null }",
        @"{ ""_id"": 3, ""foo"": ""null"" }",
        @"{ ""_id"": 4, ""foo"": ""NULL"" }",
      };

      Collection collection = CreateCollection("test");
      ExecuteAddStatement(collection.Add(docs));
      var result = ExecuteFindStatement(collection.Find()).FetchAll();
      Assert.AreEqual(docs.Length, result.Count);
      for (int i = 0; i < docs.Length; i++)
      {
        var currentDoc = new DbDoc(docs[i]);
        var resultingDoc = new DbDoc(result[i]);
        Assert.AreEqual(currentDoc.Id, resultingDoc.Id);
        Assert.AreEqual(currentDoc["foo"], resultingDoc["foo"]);
      }
    }

    [Test]
    public void AddOrReplaceOne()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) return;

      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result result = ExecuteAddStatement(collection.Add(docs));
      Assert.AreEqual(4, result.AffectedItemsCount);

      // Add a document.
      Assert.AreEqual(1, collection.AddOrReplaceOne(5, new { _id = 5, title = "Book 5", pages = 60 }).AffectedItemsCount);
      Assert.True(collection.GetOne(5) != null);

      Assert.AreEqual(1, collection.AddOrReplaceOne("6", new { title = "Book 6", pages = 70 }).AffectedItemsCount);
      Assert.True(collection.GetOne(6) == null);
      Assert.True(collection.GetOne("6") != null);

      // Replace a document.
      Assert.AreEqual(2, collection.AddOrReplaceOne(1, new { _id = 1, title = "Book X", pages = 10 }).AffectedItemsCount);
      DbDoc document = collection.GetOne(1);
      Assert.AreEqual(1, Convert.ToInt32(document.Id));
      Assert.AreEqual("Book X", document["title"]);
      Assert.AreEqual(10, Convert.ToInt32(document["pages"]));

      Assert.AreEqual(2, collection.AddOrReplaceOne(1, new { title = "Book Y", pages = 9, other = "value" }).AffectedItemsCount);
      document = collection.GetOne(1);
      Assert.AreEqual(1, Convert.ToInt32(document.Id));
      Assert.AreEqual("Book Y", document["title"]);
      Assert.AreEqual(9, Convert.ToInt32(document["pages"]));
      Assert.AreEqual("value", document["other"]);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne(null, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne("", docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne(string.Empty, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne("1", null));
    }
  }
}
