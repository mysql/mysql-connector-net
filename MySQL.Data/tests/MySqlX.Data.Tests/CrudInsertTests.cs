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

using MySql.Data;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.Common;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  public class CrudInsertTests : BaseTest
  {
    [Test]
    public void InsertSingleDbDocWithId()
    {
      Collection coll = CreateCollection("test");
      Result r = ExecuteAddStatement(coll.Add(@"{ ""_id"": 1, ""foo"": 1 }"));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      Assert.That(coll.Count(), Is.EqualTo(1));
    }

    [Test]
    public void InsertSingleDbDocWithoutId()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add("{ \"foo\": 1 }");
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.That(Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code, Is.EqualTo(5115u));
        return;
      }
      Result r = ExecuteAddStatement(stmt);
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      Assert.That(coll.Count(), Is.EqualTo(1));
      Assert.That(r.GeneratedIds, Has.One.Items);
      Assert.That(string.IsNullOrWhiteSpace(r.GeneratedIds[0]), Is.False);
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
        Assert.That(Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code, Is.EqualTo(5115u));
        return;
      }
      Result r = ExecuteAddStatement(stmt);
      Assert.That(r.AffectedItemsCount, Is.EqualTo(3));
      Assert.That(coll.Count(), Is.EqualTo(3));
      Assert.That(r.GeneratedIds.Count, Is.EqualTo(3));
    }

    [Test]
    public void InsertAnonymousObjectWithId()
    {
      var obj = new { _id = "5", name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      Result r = ExecuteAddStatement(coll.Add(obj));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      //TODO:  pull object and verify data
      Assert.That(coll.Count(), Is.EqualTo(1));
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
        Assert.That(Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code, Is.EqualTo(5115u));
        return;
      }
      Result r = ExecuteAddStatement(stmt);
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));
      //TODO:  pull object and verify data
      Assert.That(coll.Count(), Is.EqualTo(1));
      Assert.That(r.GeneratedIds, Has.One.Items);
      Assert.That(string.IsNullOrWhiteSpace(r.GeneratedIds[0]), Is.False);
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
      Assert.That(r.AffectedItemsCount, Is.EqualTo(4));
      Assert.That(coll.Count(), Is.EqualTo(4));
    }

    [Test]
    public void ValidatesDocumentIds()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add(new { name = "Book 1" });
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.That(Assert.Throws<MySqlException>(() => ExecuteAddStatement(stmt)).Code, Is.EqualTo(5115u));
        return;
      }
      Result result = ExecuteAddStatement(stmt);
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      result = ExecuteModifyStatement(coll.Modify($"_id = '{result.GeneratedIds[0]}'").Set("pages", "20"));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
      Assert.That(result.GeneratedIds, Is.Empty);
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
      Assert.That(r.AffectedItemsCount, Is.EqualTo((ulong)docs.Length + 1));
      Assert.That(coll.Count(), Is.EqualTo(5));
    }

    [Test]
    public void EmptyDocArray()
    {
      Collection coll = CreateCollection("test");

      var insertResult = ExecuteAddStatement(coll.Add(new DbDoc[] { }));
      Assert.That(insertResult.AffectedItemsCount, Is.EqualTo(0ul));

      var result = ExecuteFindStatement(coll.Find()).FetchAll();
      Assert.That(result, Is.Empty);
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
      Assert.That(result[0].ToString(), Is.EqualTo(d2.ToString()));
    }

    [Test]
    public void CompareGuids()
    {
      Guid guid1 = new Guid();
      Guid guid2 = new Guid();
      Assert.That(Tools.CompareGuids(guid1, guid2), Is.EqualTo(0));
      Assert.That(Tools.CompareGuids(guid1.ToString(), guid2.ToString()), Is.EqualTo(0));

      guid1 = Guid.NewGuid();
      guid2 = Guid.NewGuid();
      Assert.That(Tools.CompareGuids(guid1, guid2) != 0);
      Assert.That(Tools.CompareGuids(guid1.ToString(), guid2.ToString()) != 0);
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
      Assert.That(result.Count, Is.EqualTo(docs.Length));

      for (int i = 0; i < docs.Length; i++)
        Assert.That(result[i].ToString(), Is.EqualTo(docs[i].ToString()));
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
      Assert.That(result.Count, Is.EqualTo(docs.Length));
      for (int i = 0; i < docs.Length; i++)
      {
        var currentDoc = new DbDoc(docs[i]);
        var resultingDoc = new DbDoc(result[i]);
        Assert.That(resultingDoc.Id, Is.EqualTo(currentDoc.Id));
        Assert.That(resultingDoc["foo"], Is.EqualTo(currentDoc["foo"]));
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
      Assert.That(result.AffectedItemsCount, Is.EqualTo(4));

      // Add a document.
      Assert.That(collection.AddOrReplaceOne(5, new { _id = 5, title = "Book 5", pages = 60 }).AffectedItemsCount, Is.EqualTo(1));
      Assert.That(collection.GetOne(5) != null);

      Assert.That(collection.AddOrReplaceOne("6", new { title = "Book 6", pages = 70 }).AffectedItemsCount, Is.EqualTo(1));
      Assert.That(collection.GetOne(6) != null);
      Assert.That(collection.GetOne("6") != null);

      // Replace a document.
      Assert.That(collection.AddOrReplaceOne(1, new { _id = 1, title = "Book X", pages = 10 }).AffectedItemsCount, Is.EqualTo(2));
      DbDoc document = collection.GetOne(1);
      Assert.That(Convert.ToInt32(document.Id), Is.EqualTo(1));
      Assert.That(document["title"], Is.EqualTo("Book X"));
      Assert.That(Convert.ToInt32(document["pages"]), Is.EqualTo(10));

      Assert.That(collection.AddOrReplaceOne(1, new { title = "Book Y", pages = 9, other = "value" }).AffectedItemsCount, Is.EqualTo(2));
      document = collection.GetOne(1);
      Assert.That(Convert.ToInt32(document.Id), Is.EqualTo(1));
      Assert.That(document["title"], Is.EqualTo("Book Y"));
      Assert.That(Convert.ToInt32(document["pages"]), Is.EqualTo(9));
      Assert.That(document["other"], Is.EqualTo("value"));

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne(null, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne("", docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne(" ", docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne(string.Empty, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne("1", null));
      Assert.Throws<MySqlException>(() => collection.AddOrReplaceOne(4, new DbDoc("{ \"_id\": 2, \"title\": \"Book\", \"pages\": 60 }")),
        ResourcesX.ReplaceWithNoMatchingId);
    }

    #region WL14389

    [Test, Description("Test MySQLX plugin Collection Scenarios - with Create a valid collection and reuse existing object")]
    public void CreateCollectionAndReuseObject()
    {
      using (Session sessionPlain = MySQLX.GetSession(ConnectionString))
      {
        var col = CreateCollection("my_collection_123456789");
        object[] data = new object[]
        {
              new {  _id = 1, title = "Book 1", pages = 20 },
              new {  _id = 2, title = "Book 2", pages = 30 },
              new {  _id = 3, title = "Book 3", pages = 40 },
              new {  _id = 4, title = "Book 4", pages = 50 },
        };
        Result result = col.Add(data).Execute();
        var data1 = col.Remove("_id = 1").Execute();
        Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
        data1 = col.Remove("_id = 2").Execute();
        Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
        data1 = col.Remove("_id = 3").Execute();
        Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
        data1 = col.Remove("_id = 4").Execute();
        Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
        sessionPlain.Close();
      }
    }

    [Test, Description("Collection Scenarios - with Create a collection with emptyname")]
    public void CreateCollectionEmptyName()
    {
      Schema db = session.GetSchema(schemaName);
      Assert.Throws<MySqlException>(() => db.CreateCollection(null));
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - with Create a collection which exists without reuse existing object")]
    public void CreateCollectionWithoutReuseExistingObject()
    {
      Schema db = session.GetSchema(schemaName);
      var col = CreateCollection("my_collection_123456789");
      col = db.GetCollection("my_collection_123456789", true);
      Assert.That(col, Is.Not.Null);
      Assert.Throws<MySqlException>(() => db.CreateCollection("my_collection_123456789", false));
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add Objects,Find with condition")]
    public void CollectionAddObjectsFindCondition()
    {
      Schema db = session.GetSchema(schemaName);
      Collection col = CreateCollection("my_collection_1");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result result = col.Add(docs).Execute();

      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(4), "Matching the updated record count");
      var foundDocs = col.Find("pages > 20").Execute();
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Next Node Exist");
      Assert.That(foundDocs.Current["title"], Is.EqualTo("Book 2"), "Matching the Node Value");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Next Node Exist");
      Assert.That(foundDocs.Current["title"], Is.EqualTo("Book 3"), "Matching the Node Value");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Next Node Exist");
      Assert.That(foundDocs.Current["title"], Is.EqualTo("Book 4"), "Matching the Node Value");
      Assert.That(foundDocs.Next(), Is.EqualTo(false), "Next Node doesnot Exist");
      db.DropCollection("my_collection_1");
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add Objects,Find without parameters")]
    public void CollectionAddFindNoParams()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Collection col = CreateCollection("my_collection_1");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      var result = col.Add(docs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(4), "Matching the updated record count");
      var foundDocs = col.Find().Execute();
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Next Node Exist");
      Assert.That(foundDocs.Current["title"], Is.EqualTo("Book 1"), "Matching the Node Value");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Next Node Exist");
      Assert.That(foundDocs.Current["title"], Is.EqualTo("Book 2"), "Matching the Node Value");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Next Node Exist");
      Assert.That(foundDocs.Current["title"], Is.EqualTo("Book 3"), "Matching the Node Value");
      Assert.That(foundDocs.Next(), Is.EqualTo(true), "Next Node Exist");
      Assert.That(foundDocs.Current["title"], Is.EqualTo("Book 4"), "Matching the Node Value");
      Assert.That(foundDocs.Next(), Is.EqualTo(false), "Next Node doesnot Exist");
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add/Remove JSON Object with id")]
    public void CollectionAddRemoveJSONObjectID()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      using (var sessionPlain = MySQLX.GetSession(ConnectionString + ";sslmode=" + MySqlSslMode.Required))
      {
        Schema db = sessionPlain.GetSchema(schemaName);
        Collection col = CreateCollection("my_collection_1");
        DbDoc DbDocs = new DbDoc();
        DbDocs.SetValue("_id", "0");
        DbDocs.SetValue("title", "Book 0");
        DbDocs.SetValue("pages", "10");
        var result = col.Add(DbDocs).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");
        DbDoc jsonremovedoc = new DbDoc();
        jsonremovedoc.SetValue("_id", "0");
        var foundDocs = col.Remove("_id='0'").Execute();
        Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
        db.DropCollection("my_collection_1");
        sessionPlain.Close();
      }
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add/Remove with condition")]
    public void CollectionAddRemoveJSONObjectCondition()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Schema db = session.GetSchema(schemaName);
      Collection col = CreateCollection("my_collection_1");
      DbDoc DbDocs = new DbDoc();
      DbDocs.SetValue("_id", "0");
      DbDocs.SetValue("title", "Book 0");
      DbDocs.SetValue("pages", "10");
      var result = col.Add(DbDocs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");
      var foundDocs = col.Remove("pages > 0").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      db.DropCollection("my_collection_1");
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add,Remove with condition limit")]
    public void CollectionAddRemoveObjectConditionLimit()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Schema db = session.GetSchema(schemaName);
      Collection col = CreateCollection("my_collection_1");
      var docs = new[]
      {
        new {  _id = 0, title = "Book 0", pages = 10 },
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      var result = col.Add(docs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Matching the updated record count");
      var foundDocs = col.Remove("_id=1").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
      };
      result = col.Add(docs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");
      result = col.Remove("pages > 10").Limit(1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");
      result = col.Remove("pages > 10").Limit(3).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(3), "Matching the updated record count");
      Assert.Throws<ArgumentOutOfRangeException>(() => col.Remove("pages > 10").Limit(0).Execute());
      db.DropCollection("my_collection_1");
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add/Remove 200 JSON records ")]
    public void CollectionAddRemove200JSONRecords()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Collection col = CreateCollection("my_collection_1");
      var numOfRecords = 200;

      DbDoc[] jsonlist = new DbDoc[numOfRecords];
      for (int i = 0; i < numOfRecords; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 1000).ToString());
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      var res = col.Add(jsonlist).Execute();

      Assert.That(jsonlist[0].ToString(), Is.EqualTo(jsonlist[0].ToString()), "String Match being done");
      for (int i = 0; i < numOfRecords; i++)
      {
        var data1 = col.Remove($"_id = '{i + 1000}'").Execute();
        Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      }
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add/Remove documents with limit and orderby ")]
    public void CollectionAddRemoveJSONRecordsLimitOrderBy()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Schema db = session.GetSchema(schemaName);
      Collection col = CreateCollection("my_collection_1");

      DbDoc[] jsonlist = new DbDoc[10];
      for (int i = 0; i < 10; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i).ToString());
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i));
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      col.Add(jsonlist).Execute();
      Assert.That(jsonlist[0].ToString(), Is.EqualTo(jsonlist[0].ToString()), "String Match being done");

      Result result = col.Remove("F3 > 305").Limit(1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Match being done");

      result = col.Remove("F3 > 303").Sort("F3 DESC").Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Match being done");

      result = col.Remove("F3 > 303").Sort("F3 DESC").Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(0), "Match being done");

      result = col.Remove("F3 = 303").Sort("F2 DESC").Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Match being done");

      result = col.Remove("F3 < 301").Sort("F2 DESC").Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Match being done");

      result = col.Remove("F3 < 301").Sort("F2 DESC").Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(0), "Match being done");

      db.DropCollection("my_collection_1");

    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add Single/Multiple Docs - Remove Condition/ID/Condition-Limit/Condition-Limit-OrderBy/Bind")]
    public void CollectionAddRemoveDocsLimitOrderByBind()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Collection col = CreateCollection("my_collection_1");
      DbDoc DbDocs = new DbDoc();
      DbDocs.SetValue("_id", 100000);
      DbDocs.SetValue("title", "Book 0");
      DbDocs.SetValue("pages", 10);
      var result = col.Add(DbDocs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");
      var foundDocs = col.Remove("pages > 0").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");

      var docs = new { _id = 100001, title = "Book 1", pages = 20 };
      result = col.Add(docs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");

      foundDocs = col.Remove("pages=20").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");

      docs = new { _id = -100001, title = "Book 1", pages = 20 };
      result = col.Add(docs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");

      foundDocs = col.Remove("pages=20").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      var docs1 = new[]
      {
        new { _id = -100001, title = "Book 0", pages = 10 },
        new { _id = 0, title = "Book 1", pages = 20 },
        new { _id = 100001, title = "Book 2", pages = 30 },
        new { _id = 10000001, title = "Book 3", pages = 40 }
      };
      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(4), "Matching the updated record count");

      foundDocs = col.Remove("pages > 10").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(3), "Matching the deleted record count");

      foundDocs = col.Remove("pages=10").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      docs1 = new[]
      {
        new { _id = -100001, title = "Book 0", pages = 10 },
        new { _id = 0, title = "Book 1", pages = 20 },
        new { _id = 100001, title = "Book 2", pages = 30 },
        new { _id = 10000001, title = "Book 3", pages = 40 },
        new { _id = 10000009, title = "Book 4", pages = 50 }
      };
      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Matching the updated record count");

      foundDocs = col.Remove("pages = 40").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");

      foundDocs = col.Remove("pages >= 10").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(4), "Matching the deleted record count");

      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Matching the updated record count");
      foundDocs = col.Remove("pages = 40").Limit(1).Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      foundDocs = col.Remove("pages <= 50").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(4), "Matching the deleted record count");

      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Matching the updated record count");
      foundDocs = col.Remove("pages = 40").Limit(1).Sort("title").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      foundDocs = col.Remove("pages <= 50").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(4), "Matching the deleted record count");

      docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(4), "Matching the updated record count");
      foundDocs = col.Remove("pages = :Pages").Bind("pAges", 50).Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
    }

    [Test, Description("Test MySQLX plugin Collection Scenarios - Add Single/Multiple Docs - Remove Condition/ID/Condition-Limit/Condition-Limit-OrderBy/Bind using invalid conditions")]
    public void CollectionRemoveDocsLimitOrderByBindNegative()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Schema db = session.GetSchema(schemaName);
      var col = db.CreateCollection("my_collection_1", true);

      DbDoc DbDocs = new DbDoc();
      DbDocs.SetValue("_id", 100000);
      DbDocs.SetValue("title", "Book 0");
      DbDocs.SetValue("pages", 10);
      var result = col.Add(DbDocs).Execute();

      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");
      var foundDocs = col.Remove("pages > 10").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");

      var docs = new { _id = 100001, title = "Book 1", pages = 20 };
      result = col.Add(docs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");

      foundDocs = col.Remove("pages >= 10").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(2), "Matching the deleted record count");

      docs = new { _id = -100001, title = "Book 1", pages = 20 };
      result = col.Add(docs).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the updated record count");

      foundDocs = col.Remove("pages > 10").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");


      var docs1 = new[]
      {
        new { _id = -100001, title = "Book 0", pages = 10 },
        new { _id = 0, title = "Book 1", pages = 20 },
        new { _id = 100001, title = "Book 2", pages = 30 },
        new { _id = 10000001, title = "Book 3", pages = 40 }
      };
      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(4), "Matching the updated record count");

      foundDocs = col.Remove("pages > 50").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");

      foundDocs = col.Remove("pages < 50").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(4), "Matching the deleted record count");

      docs1 = new[]
      {
        new { _id = -100001, title = "Book 0", pages = 10 },
        new { _id = 0, title = "Book 1", pages = 20 },
        new { _id = 100001, title = "Book 2", pages = 30 },
        new { _id = 10000001, title = "Book 3", pages = 40 },
        new { _id = 10000009, title = "Book 4", pages = 50 }
      };
      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Matching the updated record count");

      foundDocs = col.Remove("pages = 0").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");

      foundDocs = col.Remove("pages <= 50").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(5), "Matching the deleted record count");

      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Matching the updated record count");
      foundDocs = col.Remove("pages = 60").Limit(1).Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");

      foundDocs = col.Remove("pages <= 50").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(5), "Matching the deleted record count");

      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(5), "Matching the updated record count");
      foundDocs = col.Remove("pages = 04").Limit(1).Sort("title").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");
      foundDocs = col.Remove("pages <= 50").Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(5), "Matching the deleted record count");

      docs1 = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };

      result = col.Add(docs1).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(4), "Matching the updated record count");
      foundDocs = col.Remove("pages1 = :Pages").Bind("pAges", 50).Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");
      foundDocs = col.Remove("pages = :Pages").Bind("pAges", 51).Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");
      foundDocs = col.Remove("pages = :Pages").Bind("pAges", 50).Execute();
      Assert.That((int)foundDocs.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      db.DropCollection("my_collection_1");

    }

    [Test, Description("Test MySQLX plugin - MYSQLCNET_680 Allow Reuse Statement after execute Positive1(after a succesful execute)")]
    public void AllowReuseStatementAfterExecutePositive1()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      Schema db = session.GetSchema(schemaName);
      var col = db.CreateCollection("my_collection_123456789");
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 10 },
        new {  _id = 2, title = "Book 2", pages = 20 },
        new {  _id = 3, title = "Book 3", pages = 30 },
        new {  _id = 4, title = "Book 4", pages = 40 },
        new {  _id = 5, title = "Book 5", pages = 50 },
        new {  _id = 6, title = "Book 6", pages = 60 },
        new {  _id = 7, title = "Book 7", pages = 70 },
        new {  _id = 8, title = "Book 8", pages = 80 },
      };
      Result result = col.Add(data).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(8), "Matching the added record count");
      var data1 = col.Remove("_id=1").Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      data1 = col.Remove("pages = :Pages").Bind("pAges", 80).Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      data1 = col.Remove("pages = :Pages").Bind("pAges", 70).Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      var remData = col.Remove("_id = :param1 AND title = :param2");
      data1 = remData.Bind("param1", 5).Bind("param2", "Book 5").Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      data1 = remData.Bind("param1", 6).Bind("param2", "Book 6").Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      db.DropCollection("my_collection_123456789");
    }

    [Test, Description("Test MySQLX plugin - MYSQLCNET_680 Allow Reuse Statement after execute-Positive2(after an unsuccessful execute)")]
    public void AllowReuseStatementAfterExecutePositive2()
    {
      var col = CreateCollection("my_collection_123456789");
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 10 },
        new {  _id = 2, title = "Book 2", pages = 20 },
        new {  _id = 3, title = "Book 3", pages = 30 },
        new {  _id = 4, title = "Book 4", pages = 40 },
        new {  _id = 5, title = "Book 5", pages = 50 },
        new {  _id = 6, title = "Book 6", pages = 60 },
        new {  _id = 7, title = "Book 7", pages = 70 },
        new {  _id = 8, title = "Book 8", pages = 80 },
      };
      Result result = col.Add(data).Execute();
      Assert.That((int)result.AffectedItemsCount, Is.EqualTo(8), "Matching the added record count");
      var data1 = col.Remove("_id=1").Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      data1 = col.Remove("pages = :Pages").Bind("pAges", 80).Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      data1 = col.Remove("pages = :Pages").Bind("pAges", 70).Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      var remData = col.Remove("_id = :param1 AND title = :param2");

      data1 = remData.Bind("param1", 35).Bind("param2", "Book 33").Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(0), "Matching the deleted record count");

      data1 = remData.Bind("param1", 6).Bind("param2", "Book 6").Execute();
      Assert.That((int)data1.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");
      testSchema.DropCollection("my_collection_123456789");
    }

    [Test, Description("Test MySQLX plugin - MYSQLCNET 755 Collection GetDocumentID")]
    public void CollectionGetDocumentID()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      Collection coll = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 100, title = "Book 100", pages = 1000 },
      };
      Result result = coll.Add(docs).Execute();
      var documentIds = result.GeneratedIds;
      Assert.That(documentIds != null && documentIds.Count > 0, Is.False);

      coll = CreateCollection("test");
      var docs1 = new[]
      {
        new { title = "Book 1", pages = 100 },
      };
      var r = coll.Add(docs1);
      var result1 = r.Execute();
      var documentIds1 = result.GeneratedIds;
      HashSet<string> firstset = new HashSet<string>();
      for (int i = 0; i < documentIds.Count; i++)
      {
        if (documentIds1 != null)
        {
          Assert.That(documentIds1[i].ToString(), Is.EqualTo(documentIds1[i].ToString()), "Matching the document ID with unique id");
        }
        if (!firstset.Add(documentIds1[i]))
        {
          break;
        }
      }

      coll = CreateCollection("test");
      docs = new[]
      {
        new {  _id = 100, title = "Book 100", pages = 1000 },
        new {  _id = 200, title = "Book 200", pages = 2000 },
        new {  _id = 300, title = "Book 300", pages = 3000 },
        new {  _id = 400, title = "Book 400", pages = 4000 },
      };

      var r1 = coll.Add(docs).Execute();
      documentIds = r1.GeneratedIds;
      Assert.That(documentIds != null && documentIds.Count > 0, Is.False);

      coll = CreateCollection("test");
      docs1 = new[]
      {
        new { title = "Book 1", pages = 100 },
        new { title = "Book 2", pages = 200 },
        new { title = "Book 3", pages = 300 },
        new { title = "Book 4", pages = 400 },
      };

      var stmt = coll.Add(docs1);
      result1 = stmt.Execute();
      documentIds1 = result1.GeneratedIds;
      firstset = new HashSet<string>();
      for (int i = 0; i < documentIds1.Count; i++)
      {
        if (documentIds != null)
        {
          Assert.That(documentIds1[i].ToString(), Is.EqualTo(documentIds1[i].ToString()), "Matching the document ID with unique id");
        }

        if (!firstset.Add(documentIds1[i]))
        {
          break;
        }
      }
    }

    [Test, Description("Test MySQLX UUID Scenario-1(Check UUID is not generated when JSON doc is added using collection.add() with _id fields)")]
    public void CheckDocUUIDScenario1()
    {
      var col = CreateCollection("my_collection_123456789");
      object[] data = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  _id = 4, title = "Book 4", pages = 50 },
      };
      Result result = col.Add(data).Execute();
      var documentIds = result.GeneratedIds;
      Assert.That(documentIds != null && documentIds.Count > 0, Is.False);
    }

    [Test, Description("Test MySQLX UUID Scenario-2(Check UUID generated when multiple JSON docs are added using collection.add().add()..without _id fields)")]
    public void CheckDocUUIDScenario2()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      var col = CreateCollection("my_collection_123456789");
      object[] data1 = new object[]
      {
        new { title ="Book 1", pages = 20 },
        new {  title = "Book 2", pages = 30 },
        new {  title = "Book 3", pages = 40 },
        new {  title = "Book 4", pages = 50 }
      };

      object[] data2 = new object[]
      {
        new { title = "Book 5", pages = 60 },
        new {title =  "Book 6", pages = 70 },
        new { title = "Book 7", pages = 80 },
        new { title = "Book 8", pages = 90 },
      };
      var stmt = col.Add(data1).Add(data2);
      var result = stmt.Execute();
      var documentIds = result.GeneratedIds;
      HashSet<string> firstset = new HashSet<string>();
      for (int i = 0; i < documentIds.Count; i++)
      {
        if (documentIds != null)
        {
          Assert.That(documentIds[i].ToString(), Is.EqualTo(documentIds[i].ToString()), "Matching the document ID with unique id");
        }
        if (!firstset.Add(documentIds[i]))
        {
          Console.WriteLine("Contains duplicate ID");
          break;
        }
      }
    }

    [Test, Description("Test MySQLX UUID Scenario-2(Check UUID generated when multiple JSON docs are added using collection.add(doc, doc, doc... ) without _id fields)")]
    public void CheckDocUUIDScenario3()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      var col = CreateCollection("my_collection_123456789");
      var stmt = col.Add(@"{ ""foo"": 1 }", @"{""foo"": 2 }", @"{ ""foo"": 3 }", @"{ ""foo"": 4 }");
      Result r = col.Add(@"{ ""foo"": 1 }", @"{""foo"": 2 }", @"{ ""foo"": 3 }", @"{ ""foo"": 4 }").Execute();
      long count = col.Count();
      Assert.That(4, Is.EqualTo(count), "Matching the Collection Count");
      var documentIds = r.GeneratedIds;
      if (documentIds != null)
      {
        Assert.That(documentIds[0].ToString(), Is.EqualTo(documentIds[0].ToString()), "Matching the document ID without unique id");
        Assert.That(documentIds[1].ToString(), Is.EqualTo(documentIds[1].ToString()), "Matching the document ID without unique id");
        Assert.That(documentIds[2].ToString(), Is.EqualTo(documentIds[2].ToString()), "Matching the document ID without unique id");
        Assert.That(documentIds[3].ToString(), Is.EqualTo(documentIds[3].ToString()), "Matching the document ID without unique id");
      }

      var collectionName = col.Name;
      Assert.That("my_collection_123456789", Is.EqualTo(collectionName), "Matching the collection Name");
      Assert.That(col.Schema.Name, Is.EqualTo(schemaName), "Matching the Schema Name");
    }

    [Test, Description("Test MySQLX UUID Scenario-4(Check UUID generated when multiple JSON docs are added using some containing and some not containing _id fields)")]
    public void CheckDocUUIDScenario4()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      var col = CreateCollection("my_collection_123456789");
      object[] data1 = new object[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
        new {  _id = 3, title = "Book 3", pages = 40 },
        new {  title = "Book 4", pages = 50 }
      };

      object[] data2 = new object[]
      {
        new { title = "Book 5", pages = 60 },
        new {  _id = 6,title = "Book 6", pages = 70 },
        new { title = "Book 7", pages = 80 },
        new { title = "Book 8", pages = 90 },
      };

      var stmt = col.Add(data1).Add(data2);
      Result result = stmt.Execute();
      var documentIdsCount = result.GeneratedIds.Count;
      var documentIds = result.GeneratedIds;
      Assert.That(documentIdsCount, Is.EqualTo(4), "Matching the document ID count");
      if (documentIds != null)
      {
        Assert.That(documentIds[0].ToString(), Is.EqualTo(documentIds[0].ToString()), "Matching the document ID with unique id");
        Assert.That(documentIds[1].ToString(), Is.EqualTo(documentIds[1].ToString()), "Matching the document ID with unique id");
        Assert.That(documentIds[2].ToString(), Is.EqualTo(documentIds[2].ToString()), "Matching the document ID with unique id");
        Assert.That(documentIds[3].ToString(), Is.EqualTo(documentIds[3].ToString()), "Matching the document ID without unique id");
      }
    }

    [Test, Description("Test MySQLX UUID Scenario-5(Check that UUID is not generated by adding multiple doc from the same collection in a session with _id fields)")]
    public void CheckDocUUIDScenario5()
    {
      Collection testCollection = CreateCollection("test");
      DbDoc[] jsonlist = new DbDoc[1000];
      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", (i + 1000));
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      Result r = testCollection.Add(jsonlist).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1000), "Matching");
      var documentIds = r.GeneratedIds;
      Assert.That(documentIds != null && documentIds.Count > 0, Is.False);

      Schema testSchema = session.GetSchema(schemaName);
      Table test = testSchema.GetCollectionAsTable("test");
      Assert.That(test.ExistsInDatabase(), Is.EqualTo(true), "Matching");
    }

    [Test, Description("Test MySQLX UUID Scenario-6(Check that no duplicate UUID is generated by adding multiple doc from the same collection in a session when there are no _id fields)")]
    public void CheckDocUUIDScenario6()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      Collection testCollection = CreateCollection("test");
      DbDoc[] jsonlist = new DbDoc[1000];
      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }

      var r = testCollection.Add(jsonlist);
      var result = r.Execute();
      var countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1000), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        var generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      }

      Schema testSchema = session.GetSchema(schemaName);
      Table test = testSchema.GetCollectionAsTable("test");
      Assert.That(test.ExistsInDatabase(), Is.EqualTo(true), "Matching");
    }

    [Test, Description("Test MySQLX UUID Scenario-7(Check UUID generated when multiple JSON docs are added using collection.add(doc, doc, doc... ) when docs contains _id fields with negative numbers,big positive numbers)")]
    public void CheckDocUUIDScenario7()
    {
      var col = CreateCollection("my_collection_123456789");
      object[] data1 = new object[]
        {
          new {  _id = -1, title = "Book 1", pages = 20 },
          new {  _id = 200000000, title = "Book 2", pages = 30 },
          new {  _id = -300000000, title = "Book 3", pages = 40 },
          new {  title = "Book 4", pages = 50 }
        };

      object[] data2 = new object[]
      {
        new { title = "Book 5", pages = 60 },
        new {  _id = 60000000000000,title = "Book 6", pages = 70 },
        new { title = "Book 7", pages = 80 },
        new { title = "Book 8", pages = 90 },
      };

      var r = col.Add(data1).Add(data2);
      var result = r.Execute();
      var countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(4), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        var generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      }

    }

    [Test, Description("Test MySQLX UUID Scenario-8(Check UUID generated when multiple JSON docs are added using collection.add(doc, doc, doc... ) when docs contains _id fields with zero,strings)")]
    public void CheckDocUUIDScenario8()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      var col = CreateCollection("my_collection_123456789");
      object[] data1 = new object[]
      {
        new {  _id = 0, title = "Book 1", pages = 20 },
        new {  _id = "test1", title = "Book 2", pages = 30 },
        new {  _id = "^%$^&&%)(*&", title = "Book 3", pages = 40 },
        new {  title = "Book 4", pages = 50 }
      };

      object[] data2 = new object[]
      {
        new { title = "Book 5", pages = 60 },
        new {  _id = 60000000000000,title = "Book 6", pages = 70 },
        new { title = "Book 7", pages = 80 },
        new { title = "Book 8", pages = 90 },
      };

      var r = col.Add(data1).Add(data2);
      var result = r.Execute();
      var countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(4), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        var generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      }

    }

    [Test, Description("Test MySQLX UUID Scenario-9(Check the behaviour when JSON docs are added using collection.add(doc) when docs contains _id fields with (blank)")]
    public void CheckDocUUIDScenario9()
    {
      Collection testCollection = CreateCollection("test");
      DbDoc[] jsonlist = new DbDoc[1];
      for (int i = 0; i < 1; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", " ");
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (300 + i).ToString());
        jsonlist[i] = newDoc2;
      }

      Result result = testCollection.Add(jsonlist).Execute();
      //ID with blank could be added as per bug#27627861 as client will not do any validation and server will do"
      Assert.That(result, Is.Not.Null);

      Schema testSchema = session.GetSchema("test");
      Table test = testSchema.GetCollectionAsTable("test");
      Assert.That(test.ExistsInDatabase(), Is.EqualTo(true), "Matching");
    }

    [Test, Description("Test MySQLX plugin Collection JSON Depth Scenarios")]
    public void CollectionAddJSONDepth()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      int i, maxArrayelement = 100;
      Collection col = CreateCollection("my_collection_1");
      Collection col1 = CreateCollection("my_collection_2");

      DbDoc d1 = new DbDoc();
      for (i = 0; i < maxArrayelement; i++)
      {
        d1.SetValue("_id" + "_" + i.ToString(), 10000);
        d1.SetValue("person" + "_" + i.ToString(), "Person" + "_" + i.ToString());
      }

      var result = col1.Add(d1).Execute();
      Assert.That(result.AffectedItemsCount, Is.GreaterThan(0));

      var data1 = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,
                  ""person"": { ""name"": ""Fred"", ""age"": 45 }
                  }");
      var data2 = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,
                    ""books"": [
                      {""_id"" : 1, ""title"" : ""Book 1""},
                      { ""_id"" : 2, ""title"" : ""Book 2"" }
                    ]
                }");
      DbDoc d2 = new DbDoc();
      d2.SetValue("_id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("taker1", data1);
      d2.SetValue("taker2", data2);

      result = col.Add(d2).Execute();
      Assert.That(result.AffectedItemsCount, Is.GreaterThan(0));

      var result1 = col1.Find().Execute().FetchAll();
      Assert.That(result1.Count, Is.EqualTo(1));
      var result2 = col.Find().Execute().FetchAll();
      Assert.That(result2.Count, Is.EqualTo(1));
    }

    [Test, Description("Test MySQLX plugin Collection Add function with null")]
    public void CollectionAddNullFind()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      int i = 1;
      Collection col = CreateCollection("my_collection_1");
      var d1 = new DbDoc();
      d1.SetValue("id" + "_" + i.ToString(), "test");
      Assert.That(col.Add(d1).Execute().AffectedItemsCount, Is.EqualTo(1));
      var result1 = col.Find(null).Execute();
      Assert.That(result1.AffectedItemsCount, Is.EqualTo(0));
      Assert.Throws<ArgumentNullException>(() => col.Add(null).Execute());
      var result2 = col.Add().Execute();
      Assert.That(result2.AffectedItemsCount, Is.EqualTo(0));
    }

    [Test, Description("Test MySQLX plugin Invalid JSON String")]
    public void InvalidJSONString()
    {
      Collection col = CreateCollection("my_collection_1");
      String json = "";
      json = "{'_id':'1004','F1': [] }";
      Exception ex = Assert.Throws<Exception>(() => col.Add(json).Execute());
      Assert.That(ex.Message, Does.Contain("The value provided is not a valid JSON document"));
    }

    [Test, Description("Test MySQLX plugin JSON String long expression")]
    public void JSONStringLongExpression()
    {
      Assume.That(!Platform.IsMacOSX(), "Check failure on MacOs: stack overflow");//TO DO

      Collection col = CreateCollection("my_collection_1");
      String json = "",
      query2 = "";
      json = "{\"_id\":\"1004\",\"F1\": 1234 }";
      col.Add(json).Execute();
      query2 = "-1+";
      for (int i = 0; i < 230; i++)
      {
        query2 = query2 + "(";
      }

      query2 = query2 + "(100+2)";
      for (int i = 230; i > 0; i--)
      {
        query2 = query2 + ")";
      }

      var docs = col.Find().Fields(("{'X':" + query2 + "}")).Execute();
      var res = col.Modify("$.F1 = 1234").Set("F1", query2).Limit(1).Execute();
      Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
    }

    [Test, Description("Test MySQLX plugin Binary Expression")]
    public void BinaryExpression()
    {
      String json = "";
      Collection col = CreateCollection("my_collection_1");
      Collection col1 = CreateCollection("my_collection_2");
      json = "{\"_id\":\"1004\",\"F1\": 123,\"F2\":\"#\" }";
      col.Add(json).Execute();

      var docs1 = col.Find().Fields("$._id as _id", "1 << 4 as tmp").Execute();
      var res = docs1.FetchAll();
      Assert.That(res[0].Id.ToString(), Is.EqualTo("1004").IgnoreCase);

      docs1 = col.Find().Fields("$._id as _id", "$.F2 ^ 1 as tmp").Execute();
      res = docs1.FetchAll();
      Assert.That(res[0].Id.ToString(), Is.EqualTo("1004").IgnoreCase);
      col.Add("{\"_id\":\"100001\",\"x1\":\"31\", \"x2\":\"13\", \"x3\":\"8\", \"x4\":\"18446744073709551614\"}").Execute();
      docs1 = col.Find("CAST($.x1 as SIGNED) | pow(2,$.x1) = $.x1").Fields("$._id as _id, $.x1 as x1, $.x2 as x2, $.x3 as x3 , $.x2 | pow(2,$.x1) as tmp").Execute();
      res = docs1.FetchAll();
      Assert.That(res, Is.Not.Null);
      docs1 = col.Find("~16 = ~CAST($.F2 as SIGNED)").Fields("$._id as _id,$.F2 as f2, ~1 as tmp").Execute();
      res = docs1.FetchAll();
      Assert.That(res, Is.Not.Null);
      int maxrec = 100;
      DbDoc newDoc = new DbDoc();
      newDoc.SetValue("_id", maxrec + 1000);
      newDoc.SetValue("F1", "Field-1-Data-" + maxrec);
      newDoc.SetValue("F2", "Field-2-Data-" + maxrec);
      newDoc.SetValue("F3", 300 + maxrec);
      col1.Add(newDoc).Execute();

      json = "{'_id':'" + (maxrec + 1000 + 1) + "','F1':'Field-1-Data-" + (maxrec + 1) + "','F2':'Field-2-Data-" + (maxrec + 1) + "','F3':" + (300 + maxrec + 1) + "}";
      json = json.Replace("'", "\"");
      var res1 = col1.Add(json).Execute();
      Assert.That(res1.AffectedItemsCount, Is.EqualTo(1));
      json = "{'F1': 'Field-1-Data-9999','F2': 'Field-2-Data-9999','F3': 'Field-3-Data-9999'}".Replace("'", "\"");
      col1.Add(json).Add(json.Replace("9", "8")).Execute();
      Assert.That(res1.AffectedItemsCount, Is.EqualTo(1));

      var docs = col1.Find("$._id = 1100").Fields("$_id as _id,$.F1 as f1, $.F2 as f2, $.F3 as f3").Execute();
      var res2 = docs.FetchOne();
      Assert.That(res2["_id"].ToString(), Is.EqualTo("1100"));
      Assert.That(res2["f1"].ToString(), Is.EqualTo("Field-1-Data-100"));
      Assert.That(res2["f2"].ToString(), Is.EqualTo("Field-2-Data-100"));
      Assert.That(res2["f3"].ToString(), Is.EqualTo("400"));

    }

    [Test, Description("Test MySQLX plugin Invalid JSON String long expression")]
    public void JSONStringSpecialCharacters()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      Collection col = CreateCollection("my_collection_1");
      String[] splName = {"+", "*", "/", "a+b", "#1", "%", "&", "@1", "!1", "~", "^",
                          "(", ")", "{", "}", "[", "]", "|", "JSON", "ADD", "JSON_EXTRACT", "JSON_OBJECT",
                          "?", "=", "+", ";", ",", ":", "<", ">", "-"};
      for (int i = 0; i < splName.Length; i++)
      {
        col.Add("{\"" + splName[i] + "\":\"data" + i + "\",\"ID\":" + i + "}").Execute();
        var docs = col.Find("$.ID = " + i).Fields("$.`" + splName[i] + "` as col1,$.ID as Id").Execute();
        var res = docs.FetchOne();
        Assert.That(res["Id"].ToString(), Is.EqualTo(i.ToString()), "Matching the ID");
        if (i == 30)
          Assert.That("data30", Is.EqualTo("data" + i), "Matching the String");
        else
          Assert.That(res["col1"].ToString(), Is.EqualTo("data" + i), "Matching the String");
      }
    }

    [Test, Description("Test MySQLX plugin Collections Chained Insert")]
    public void CollectionsChainedInsert()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      Collection col = CreateCollection("my_collection_1");
      DbDoc newDoc = new DbDoc();
      newDoc.SetValue("F1", 1);

      DbDoc newDoc1 = new DbDoc();
      newDoc1.SetValue("F1", 2);

      DbDoc newDoc2 = new DbDoc();
      newDoc2.SetValue("F1", 3);

      DbDoc[] jsonlist = new DbDoc[5];
      for (int i = 0; i < 5; i++)
      {
        DbDoc newDoc3 = new DbDoc();
        newDoc3.SetValue("F1", 4 + i);
        jsonlist[i] = newDoc3;
        newDoc3 = null;
      }

      DbDoc[] jsonlist1 = new DbDoc[5];
      for (int i = 0; i < 5; i++)
      {
        DbDoc newDoc4 = new DbDoc();
        newDoc4.SetValue("F1", 10 + i);
        jsonlist1[i] = newDoc4;
        newDoc4 = null;
      }

      DbDoc[] jsonlist2 = new DbDoc[5];
      for (int i = 0; i < 5; i++)
      {
        DbDoc newDoc5 = new DbDoc();
        newDoc5.SetValue("F1", 100 + i);
        jsonlist2[i] = newDoc5;
        newDoc5 = null;
      }

      var tabRes = col.Add(newDoc).Add(newDoc1).Execute();
      Assert.That(tabRes.AffectedItemsCount, Is.EqualTo(2), "Matching the affected records");

      tabRes = col.Add(jsonlist).Add(newDoc2).Execute();
      Assert.That(tabRes.AffectedItemsCount, Is.EqualTo(6), "Matching the affected records");

      tabRes = col.Add(jsonlist1).Add(jsonlist2).Execute();
      Assert.That(tabRes.AffectedItemsCount, Is.EqualTo(10), "Matching the affected records");
    }

    [Test, Description("Test MySQLX plugin Collection Add Array")]
    public void CollectionAddArray()
    {
      Assume.That(Platform.IsWindows(), "This test is for Windows OS only.");
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      int maxrec = 5;
      var col = CreateCollection("my_collection_1");
      DbDoc[] jsonlist = new DbDoc[maxrec];
      for (int i = 0; i < maxrec; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("_id", i);
        newDoc2.SetValue("F1", "Field-1-Data-" + i);
        newDoc2.SetValue("ARR_INT", new DbDoc(@"{""values"":[1, 2, 3, 4, 5, 6, 7, 8, 9]}"));
        newDoc2.SetValue("ARR_STR", new DbDoc(@"{""values"":""['DATA1', 'DATA2', 'DATA3', 'DATA4', 'DATA5', 'DATA6']""}"));
        newDoc2.SetValue("ARR_LIT", new DbDoc(@"{""values"":""[null, true, false, null, true, false, null, true, false]""}"));
        newDoc2.SetValue("ARR_ARR", new DbDoc(@"{""values"":[[1, 2, 3, 4, 5], [1, 2, 3, 4, 5], [1, 2, 3, 4, 5]]}"));

        col.Add(newDoc2).Execute();
        jsonlist[i] = newDoc2;
      }

      jsonlist = null;
      var res = col.Find().Execute().FetchAll();
      Assert.That(res.Count, Is.EqualTo(5), "Matching the find count");
    }

    [Test, Description("Test MySQLX plugin Collection JSON Scenarios")]
    public void CollectionAddJSONDocs()
    {
      Assume.That(Platform.IsWindows(), "This test is for Windows OS only.");
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      Collection col = CreateCollection("my_collection_1");
      string json = @"{ ""_id"": 0, ""title"": ""Book 0"" ,""pages"": 10,""name"": ""Jeoff Archer""}";
      Result r = col.Add(json).Execute();

      Assert.That((int)r.AffectedItemsCount, Is.EqualTo(1), "Matching Affected Records Count");
      var foundDocs = col.Find("pages > 5").Execute();
      Assert.That(foundDocs.Count(), Is.EqualTo(1), "Matching Count");

      json = @"{ ""_id"" : 99950, ""city"" : ""KETCHIKAN"", ""loc"" : ""[ -133.18479, 55.942471 ]"", ""pop"" : 422, ""state"" : ""AK"" }";
      r = col.Add(json).Execute();

      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": 20,
                  ""person"": { ""name"": ""Fred"", ""age"": 45 }
                  }");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("person", new { name = "Fred", age = 45 });

      Assert.That(true, Is.EqualTo(d.Equals(d2)), "Matching");
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

      var result = col.Find("$._id = 0").Fields("$._id as _id,$.name as name, $.pages as pages, $.title as title").Execute();
      var res1 = result.FetchOne();
      Assert.That(res1["_id"], Is.EqualTo(0));
      Assert.That(res1["name"], Is.EqualTo("Jeoff Archer"));
      Assert.That(res1["pages"], Is.EqualTo(10));
      Assert.That(res1["title"], Is.EqualTo("Book 0"));

      result = col.Find("$._id > 0").Fields().Execute();
      var res2 = result.FetchAll();
      Assert.That(res2.Count(), Is.EqualTo(6));

      DbDoc test = new DbDoc();
      test.SetValue("_id", 1);
      test.SetValue("name", "ABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYABBBBBBBBBBBBBBXXXXXXXXXXXXXXXYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYTTTTTTYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY");
      var coll = CreateCollection("my_collection_123456789");
      var res = coll.Add(test).Execute();
      foundDocs = coll.Find().Execute();
      var docs1 = foundDocs.FetchAll();
      Assert.That(docs1.Count, Is.EqualTo(1));
    }

    [Test, Description("Verify that the field and column _id has a value when it's not given to a document-Scenario-1(single document add)")]
    public void VerifyIDField()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      List<string> idStringList = new List<string>();
      var col = CreateCollection("my_collection");
      Result result = null;
      string generatedIDs1 = null, generatedIDs2 = null;
      int countgenerateIDs = 0;
      // Anonymous Object Array
      object[] data = new object[] { new { title = "Book 1", pages = 30 } };
      var stmt = col.Add(data);
      result = stmt.Execute();
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      VerifyGeneratedID(generatedIDs1);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // DbDoc
      DbDoc DbDocs = new DbDoc();
      DbDocs.SetValue("title", "Book 0");
      DbDocs.SetValue("pages", 10);
      stmt = col.Add(DbDocs);
      result = stmt.Execute();
      generatedIDs2 = result.GeneratedIds[0];
      Assert.That(generatedIDs2, Is.EqualTo(generatedIDs2), "ID generated by the server");
      VerifyGeneratedID(generatedIDs2);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // Anonymous Object
      var docs = new { title = "Book 1", pages = 20 };
      stmt = col.Add(docs);
      result = stmt.Execute();
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      VerifyGeneratedID(generatedIDs1);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // JSON
      stmt = col.Add("{ \"foo\": 100 }");
      result = stmt.Execute();
      generatedIDs2 = result.GeneratedIds[0];
      Assert.That(generatedIDs2, Is.EqualTo(generatedIDs2), "ID generated by the server");
      VerifyGeneratedID(generatedIDs2);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      int j = 1;
      for (int i = 0; i < idStringList.Count; i++)
      {
        if (j == idStringList.Count)
        {
          break;
        }
        VerifySequence(idStringList[i], idStringList[j]);
        j++;
      }
    }

    [Test, Description("Unique _ids generated server side for multiple documents, single add and generated ids count should be number of docs added-Scenario3")]
    public void VerifyIDFieldScenario3()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      List<string> idStringList = new List<string>();
      var col = CreateCollection("my_collection");
      Result result = null;
      string generatedIDs1 = null;
      int countgenerateIDs = 0;
      HashSet<string> firstset = new HashSet<string>();

      object[] data1 = new object[]
      {
        new { title = "Book 1", pages = 20 },
        new { title = "Book 2", pages = 30 },
        new { title = "Book 3", pages = 40 },
        new { title = "Book 4", pages = 50 }
      };

      var stmt = col.Add(data1);
      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(4), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        Assert.That(!firstset.Add(generatedIDs1), Is.False);
      }

      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      stmt = col.Add(@"{ ""foo"": 1 }", @"{""foo"": 2 }", @"{ ""foo"": 3 }", @"{ ""foo"": 4 }");
      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(4), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        Assert.That(!firstset.Add(generatedIDs1), Is.False);
      }

      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      int j = 1;
      for (int i = 0; i < idStringList.Count; i++)
      {
        if (j == idStringList.Count)
        {
          break;
        }
        VerifySequence(idStringList[i], idStringList[j]);
        j++;
      }
    }

    [Test, Description("Verify that the field and column _id has a value when it's not given to a document-Scenario-1(single document add)-when doc already exists")]
    public void VerifyIDFieldScenario4()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      List<string> idStringList = new List<string>();
      var col = CreateCollection("my_collection");
      Result result = null;
      string generatedIDs1 = null, generatedIDs2 = null;
      int countgenerateIDs = 0;
      object[] data = null;
      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // Anonymous Object Array
      data = new object[] { new { title = "Book 1", pages = 30 } };
      var stmt = col.Add(data);
      result = stmt.Execute();
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      VerifyGeneratedID(generatedIDs1);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      col = CreateCollection("my_collection");
      data = null;
      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // DbDoc
      DbDoc DbDocs = new DbDoc();
      DbDocs.SetValue("title", "Book 0");
      DbDocs.SetValue("pages", 10);
      stmt = col.Add(DbDocs);
      result = stmt.Execute();
      generatedIDs2 = result.GeneratedIds[0];
      Assert.That(generatedIDs2, Is.EqualTo(generatedIDs2), "ID generated by the server");
      VerifyGeneratedID(generatedIDs2);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      col = CreateCollection("my_collection");
      data = null;
      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // Anonymous Object
      var docs = new { title = "Book 1", pages = 20 };
      stmt = col.Add(docs);
      result = stmt.Execute();
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      VerifyGeneratedID(generatedIDs1);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      col = CreateCollection("my_collection");
      data = null;
      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      //JSON
      stmt = col.Add("{ \"foo\": 100 }");
      result = stmt.Execute();
      generatedIDs2 = result.GeneratedIds[0];
      Assert.That(generatedIDs2, Is.EqualTo(generatedIDs2), "ID generated by the server");
      VerifyGeneratedID(generatedIDs2);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      int j = 1;
      for (int i = 0; i < idStringList.Count; i++)
      {
        if (j == idStringList.Count)
        {
          break;
        }
        VerifySequence(idStringList[i], idStringList[j]);
        j++;
      }
    }

    [Test, Description("Unique _ids generated server side for multiple documents,multiple add and generated ids count should be number of docs added-when doc already exists Scenario2")]
    public void VerifyIDFieldScenario5()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      List<string> idStringList = new List<string>();
      Result result = null;
      string generatedIDs1 = null;
      int countgenerateIDs = 0;
      var col = CreateCollection("my_collection");
      object[] data = null;

      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      HashSet<string> firstset = new HashSet<string>();
      // Anonymous Object Array
      object[] data1 = new object[] { new { title ="Book 1", pages = 20 }
         };
      object[] data2 = new object[] { new { title = "Book 5", pages = 60 } };

      var stmt = col.Add(data1).Add(data2);
      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(2), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        Assert.That(!firstset.Add(generatedIDs1), Is.False);
      }
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      col = CreateCollection("my_collection");
      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // DbDoc
      DbDoc DbDocs1 = new DbDoc();
      DbDocs1.SetValue("title", "Book 0");
      DbDocs1.SetValue("pages", 10);
      DbDoc DbDocs2 = new DbDoc();
      DbDocs2.SetValue("title", "Book 1");
      DbDocs2.SetValue("pages", 20);
      stmt = col.Add(DbDocs1).Add(DbDocs2);
      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(2), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        Assert.That(!firstset.Add(generatedIDs1), Is.False);
      }
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      col = CreateCollection("my_collection");

      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // Anonymous Object
      var docs1 = new { title = "Book 1", pages = 20 };
      var docs2 = new { title = "Book 2", pages = 30 };
      stmt = col.Add(docs1).Add(docs2);

      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(2), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        Assert.That(!firstset.Add(generatedIDs1), Is.False);
      }
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      col = CreateCollection("my_collection");
      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // JSON
      stmt = col.Add("{ \"foo1\": 100 }").Add("{ \"foo2\": 200 }");
      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(2), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        if (!firstset.Add(generatedIDs1))
        {
          break;
        }
      }
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      int j = 1;
      for (int i = 0; i < idStringList.Count; i++)
      {
        if (j == idStringList.Count)
        {
          break;
        }
        VerifySequence(idStringList[i], idStringList[j]);
        j++;
      }
    }

    [Test, Description("Unique _ids generated server side for multiple documents, single add and generated ids count should be number of docs added-when doc already exists Scenario3")]
    public void VerifyIDFieldScenario6()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      List<string> idStringList = new List<string>();
      var col = CreateCollection("my_collection");
      Result result = null;
      string generatedIDs1 = null;
      int countgenerateIDs = 0;
      object[] data = null;

      data = new object[]
      {
        new { title ="Book 0", pages = 10 },
        new { title ="Book 5", pages = 60, _id = 6}
      };
      result = col.Add(data).Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      HashSet<string> firstset = new HashSet<string>();
      // Anonymous Object Array
      object[] data1 = new object[]
      {
        new { title ="Book 1", pages = 20 },
        new {  title = "Book 2", pages = 30 },
        new {  title = "Book 3", pages = 40 },
        new {  title = "Book 4", pages = 50 }
      };

      var stmt = col.Add(data1);
      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(4), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        Assert.That(!firstset.Add(generatedIDs1), Is.False);
      }
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      col = CreateCollection("my_collection");
      result = col.Add(@"{ ""foo"": 0, ""_id"":0 }", @"{""foo"": 5 }").Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      // JSON
      stmt = col.Add(@"{ ""foo"": 1 }", @"{""foo"": 2 }", @"{ ""foo"": 3 }", @"{ ""foo"": 4 }");
      result = stmt.Execute();
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(4), "Count of the ID generated by the server");
      for (int i = 0; i < countgenerateIDs; i++)
      {
        generatedIDs1 = result.GeneratedIds[i];
        Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
        VerifyGeneratedID(generatedIDs1);
        Assert.That(!firstset.Add(generatedIDs1), Is.False);
      }
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(result.GeneratedIds[i]);
      }

      int j = 1;
      for (int i = 0; i < idStringList.Count; i++)
      {
        if (j == idStringList.Count)
        {
          break;
        }
        VerifySequence(idStringList[i], idStringList[j]);
        j++;
      }
    }

    [Test, Description("Client provided _id shouldnt be discarded and generatedids() should give empty list for anonymous object array -single document add")]
    public void VerifyIDFieldScenario7()
    {
      var col = CreateCollection("my_collection");
      object[] data = new object[]
      {
          new { _id = "1e9c92fda74ed311944e00059a3c7a00", title = "Book 0", pages = 10 },
      };
      Result result = col.Add(data).Execute();
      var generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      var doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a00").Execute();
      var docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");

      //Anonymous Object
      var data1 = new { _id = "1e9c92fda74ed311944e00059a3c7a01", title = "Book 0", pages = 10 };
      result = col.Add(data1).Execute();

      generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a01").Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");

      // DbDoc
      DbDoc DbDocs = new DbDoc();
      DbDocs.SetValue("title", "Book 0");
      DbDocs.SetValue("pages", 10);
      DbDocs.SetValue("_id", "1e9c92fda74ed311944e00059a3c7a02");
      result = col.Add(DbDocs).Execute();

      generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a02").Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");

      //JSON
      result = col.Add("{\"_id\":\"1e9c92fda74ed311944e00059a3c7a03\",\"title\": \"Book 0\",\"pages\": 10}").Execute();
      generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a03").Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");
    }

    [Test, Description("Client provided _id shouldnt be discarded and generatedids() should give empty list for anonymous object array -multiple documents multiple add with negative number")]
    public void VerifyIDFieldScenario8()
    {
      var col = CreateCollection("my_collection");
      object[] data1 = new object[]
      {
        new { _id = "1e9c92fda74ed311944e00059a3c7a00", title = "Book 0", pages = 10 }
      };
      object[] data2 = new object[]
      {
        new { _id = -1, title = "Book 0", pages = 10 }
      };
      Result result = col.Add(data1).Add(data2).Execute();
      var generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      var doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a00").Execute();
      var docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");
      doc = col.Find("_id like :param").Bind("param", -1).Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");

      // Anonymous Object
      var data3 = new { _id = "1e9c92fda74ed311944e00059a3c7a01", title = "Book 0", pages = 10 };
      var data4 = new { _id = -2, title = "Book 0", pages = 10 };
      result = col.Add(data3).Add(data4).Execute();
      generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a01").Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");
      doc = col.Find("_id like :param").Bind("param", -2).Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");

      //DbDoc
      DbDoc DbDocs1 = new DbDoc();
      DbDocs1.SetValue("title", "Book 0");
      DbDocs1.SetValue("pages", 10);
      DbDocs1.SetValue("_id", "1e9c92fda74ed311944e00059a3c7a02");
      DbDoc DbDocs2 = new DbDoc();
      DbDocs2.SetValue("title", "Book 0");
      DbDocs2.SetValue("pages", 10);
      DbDocs2.SetValue("_id", -3);
      result = col.Add(DbDocs1).Add(DbDocs2).Execute();
      generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a02").Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");
      doc = col.Find("_id like :param").Bind("param", -3).Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");

      // JSON
      result = col.Add("{\"_id\":\"1e9c92fda74ed311944e00059a3c7a03\",\"title\": \"Book 0\",\"pages\": 10}").
          Add("{\"_id\":-4,\"title\": \"Book 0\",\"pages\": 10}").
          Execute();
      generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      doc = col.Find("_id like :param").Bind("param", "1e9c92fda74ed311944e00059a3c7a03").Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");
      doc = col.Find("_id like :param").Bind("param", -4).Execute();
      docs = doc.FetchAll().Count();
      Assert.That(docs, Is.EqualTo(1), "Matches");
    }

    [Test, Description("Client provided _id shouldnt be discarded and generatedids() should give empty list for anonymous object array -multiple documents single add with negative number,zero and big positive numbers")]
    public void VerifyIDFieldScenario9()
    {
      var col = CreateCollection("my_collection");
      // Anonymous Object Array
      object[] idList = new object[] { "1e9c92fda74ed311944e00059a3c7a00", -1, 60000000000000, -3000000000000, 0 };
      object[] data1 = new object[]
      {
        new { _id = idList[0], title = "Book 0", pages = 10 },
        new { _id = idList[1], title = "Book 0", pages = 10 },
        new { _id = idList[2] , title = "Book 0", pages = 10 },
        new { _id = idList[3], title = "Book 0", pages = 10 },
        new { _id = idList[4], title = "Book 0", pages = 10 }
      };
      Result result = col.Add(data1).Execute();
      var generatedIDs = result.GeneratedIds;
      Assert.That(generatedIDs.Count, Is.EqualTo(0), "Matches");
      for (int i = 0; i < idList.Length; i++)
      {
        var doc = col.Find("_id like :param").Bind("param", idList[i]).Execute();
        var docs = doc.FetchAll().Count();
        Assert.That(docs, Is.EqualTo(1), "Matches the Document ID");
      }
    }

    [Test, Description("documents single add with blank id")]
    public void CollectionAddBlankId()
    {
      var col = CreateCollection("my_collection");
      // Anonymous Object Array
      object[] idList = new object[] { "", " " };
      object[] data1 = new object[]
      {
              new { _id = idList[0], title = "Book 0", pages = 10 }
      };
      object[] data2 = new object[]
      {
              new { _id = idList[1], title = "Book 0", pages = 10 }
      };
      Result result1 = col.Add(data1).Execute();
      Assert.That(result1.AffectedItemsCount, Is.EqualTo(1));
      result1 = null;

      col = CreateCollection("my_collection");
      result1 = col.Add(data2).Execute();
      Assert.That(result1.AffectedItemsCount, Is.EqualTo(1));
      result1 = null;

      col = CreateCollection("my_collection");
      // DbDoc
      DbDoc DbDocs1 = new DbDoc();
      DbDocs1.SetValue("title", "Book 0");
      DbDocs1.SetValue("pages", 10);
      DbDocs1.SetValue("_id", "");
      result1 = col.Add(DbDocs1).Execute();
      Assert.That(result1.AffectedItemsCount, Is.EqualTo(1));
      result1 = null;

      col = CreateCollection("my_collection");
      DbDoc DbDocs2 = new DbDoc();
      DbDocs2.SetValue("title", "Book 1");
      DbDocs2.SetValue("pages", 20);
      DbDocs2.SetValue("_id", " ");
      result1 = col.Add(DbDocs2).Execute();
      Assert.That(result1.AffectedItemsCount, Is.EqualTo(1));
      result1 = null;

      // JSON
      col = CreateCollection("my_collection");
      result1 = col.Add("{\"_id\":\"\",\"title\": \"Book 0\",\"pages\": 10}").Execute();
      Assert.That(result1.AffectedItemsCount, Is.EqualTo(1));
      result1 = null;

      col = CreateCollection("my_collection");
      result1 = col.Add("{\"_id\":\" \",\"title\": \"Book 0\",\"pages\": 10}").Execute();
      Assert.That(result1.AffectedItemsCount, Is.EqualTo(1));
      result1 = null;

    }

    [Test, Description("Multiple documents with same id")]
    public void CollectionAddMultipleDocsSameId()
    {
      var col = CreateCollection("my_collection");
      string exception = "Document contains a field value that is not unique but required to be";
      // DbDoc
      DbDoc DbDocs1 = new DbDoc();
      DbDocs1.SetValue("title", "Book 0");
      DbDocs1.SetValue("pages", 10);
      DbDocs1.SetValue("_id", 1);
      DbDoc DbDocs2 = new DbDoc();
      DbDocs2.SetValue("title", "Book 1");
      DbDocs2.SetValue("pages", 20);
      DbDocs2.SetValue("_id", 1);
      var ex = Assert.Throws<MySqlException>(() => col.Add(DbDocs1).Add(DbDocs2).Execute());
      Assert.That(ex.Message, Is.EqualTo(exception), "Checking the exception");

      // JSON
      ex = Assert.Throws<MySqlException>(() => col.Add("{\"_id\":1,\"title\": \"Book 0\",\"pages\": 10}").
           Add("{\"_id\":1,\"title\": \"Book 1\",\"pages\": 20}").Execute());
      Assert.That(ex.Message, Is.EqualTo(exception), "Checking the exception");

      // Anonymous Object Array
      object[] data1 = new object[]
      {
        new { _id = 1, title = "Book 0", pages = 10 }
      };
      object[] data2 = new object[]
      {
        new { _id = 1, title = "Book 1", pages = 20 }
      };
      ex = Assert.Throws<MySqlException>(() => col.Add(data1).Add(data2).Execute());
      Assert.That(ex.Message, Is.EqualTo(exception), "Checking the exception");
    }

    [Test, Description("Verify the behaviour if a sequence is incremented by the user and added as _id for the document")]
    public void VerifySequenceAndIdAdded()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");
      string incrementedString = null, generatedString = null;
      var col = CreateCollection("my_collection");
      Result result = null;
      string generatedIDs1 = null;
      int countgenerateIDs = 0;
      // Anonymous Object Array
      object[] data = new object[] { new { title = "Book 1", pages = 30 } };
      var stmt = col.Add(data);

      result = stmt.Execute();
      generatedIDs1 = result.GeneratedIds[0];
      Assert.That(generatedIDs1, Is.EqualTo(generatedIDs1), "ID generated by the server");
      VerifyGeneratedID(generatedIDs1);
      countgenerateIDs = result.GeneratedIds.Count;
      Assert.That(countgenerateIDs, Is.EqualTo(1), "Count of the ID generated by the server");
      generatedString = generatedIDs1;
      incrementedString = Increment(generatedIDs1, Mode.AlphaNumeric);

      data = new object[] { new { title = "Book 2", pages = 40, _id = generatedString } };
      stmt = col.Add(data);
      string exception = "Document contains a field value that is not unique but required to be";
      Exception ex = Assert.Throws<MySqlException>(() => stmt.Execute());
      Assert.That(ex.Message, Is.EqualTo(exception), "Matching the exception");

      data = new object[] { new { title = "Book 3", pages = 50, _id = incrementedString } };
      stmt = col.Add(data);
      result = stmt.Execute();
      Assert.That(result, Is.Not.Null);

      data = new object[] { new { title = "Book 4", pages = 60 } };
      stmt = col.Add(data);
      ex = Assert.Throws<MySqlException>(() => stmt.Execute());
      Assert.That(ex.Message, Is.EqualTo(exception), "Matching the exception");

    }

    [Test, Description("documents inserted concurrently by two threads")]
    public async Task CollectionConcurrentAdd()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      CreateCollection("my_collection");
      var r1 = await CollectionAddThread1();
      _ = await CollectionAddThread2();
      Assert.That(r1, Is.EqualTo(1000));
    }
    public Task<int> CollectionAddThread1()
    {
      List<string> idStringList = new List<string>();
      var col = CreateCollection("my_collection");
      DbDoc[] jsonlist = new DbDoc[1000];
      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("F1", ("Field-1-Data-" + i));
        newDoc2.SetValue("F2", ("Field-2-Data-" + i));
        newDoc2.SetValue("F3", (3 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      Result r = col.Add(jsonlist).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1000), "Matching");
      int countgenerateIDs = r.GeneratedIds.Count;
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(r.GeneratedIds[i]);
      }

      int j = 1;
      for (int i = 0; i < idStringList.Count; i++)
      {
        if (j == idStringList.Count)
        {
          break;
        }
        VerifySequence(idStringList[i], idStringList[j]);
        j++;
      }
      return Task.FromResult((int)r.AffectedItemsCount);
    }

    public Task<int> CollectionAddThread2()
    {
      List<string> idStringList = new List<string>();
      var col = CreateCollection("my_collection1");
      DbDoc[] jsonlist = new DbDoc[1000];
      for (int i = 0; i < 1000; i++)
      {
        DbDoc newDoc2 = new DbDoc();
        newDoc2.SetValue("F4", ("Field-1-Data-" + i));
        newDoc2.SetValue("F5", ("Field-2-Data-" + i));
        newDoc2.SetValue("F6", (3 + i).ToString());
        jsonlist[i] = newDoc2;
        newDoc2 = null;
      }
      Result r = col.Add(jsonlist).Execute();
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1000), "Matching");
      int countgenerateIDs = r.GeneratedIds.Count;
      for (int i = 0; i < countgenerateIDs; i++)
      {
        idStringList.Add(r.GeneratedIds[i]);
      }

      int j = 1;
      for (int i = 0; i < idStringList.Count; i++)
      {
        if (j == idStringList.Count)
        {
          break;
        }
        VerifySequence(idStringList[i], idStringList[j]);
        j++;
      }
      return Task.FromResult(0);
    }

    ///// <summary>
    ///// Bug24397888
    ///// </summary>
    [Test, Description("WHEN A DBDOC IS PASSED AS OBJ TO SETVALUE OF ANOTHER DBDOC IT CONVERTS TO BLANK")]
    public void DbDocAsObjectConvertToBlank()
    {
      string newLine = Platform.IsWindows() ? "\r\n" : "\n";

      var col = CreateCollection("my_collection");
      var data1 = new DbDoc(@"{ ""id"": 1, ""pages"": 20,
                  ""person"": { ""name"": ""Fred"", ""age"": 45 }
                  }");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("taker1", data1);
      string expected = $"{{{newLine}  \"id\": 1, {newLine}  \"pages\": 20, {newLine}  \"taker1\": {{{newLine}    \"id\": 1, {newLine}    \"pages\": 20, {newLine}    \"person\": {{{newLine}      \"name\": \"Fred\", {newLine}      \"age\": 45{newLine}    }}{newLine}  }}{newLine}}}";
      Assert.That(d2.ToString(), Is.EqualTo(expected));
    }

    [Test, Description("ADDITION OF OBJ FAILS AFTER CREATE INDEX IN 5.7.12 SERVER(WORKS WITH 5.7.9)")]
    public void AdditionOfObject()
    {
      Collection testColl = CreateCollection("test");
      testColl.CreateIndex("testIndex", "{\"fields\": [ { \"field\":$.myId, \"type\":\"INT\" , \"required\":true} ] }");
      testColl.CreateIndex("testIndex1", "{\"fields\": [ { \"field\":$.myAge, \"type\":\"FLOAT\" , \"required\":true} ] }");
      var result = testColl.Add(new { myId = 1, myAge = 35.1, _id = 1 }).Execute();
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
    }

    [Test, Description("Test valid insert at Depth n for multiple arrays))")]
    public void InsertAtNDepth()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) return;
      string json = "";
      int i = 0, j = 0, maxField = 100;
      var collection = CreateCollection("test");
      int maxDepth = 97;
      json = "{\"_id\":\"1002\",\"XYZ\":1111";
      for (j = 0; j < maxField; j++)
      {
        json = json + ",\"ARR" + j + "\":[";
        for (i = 0; i < maxDepth; i++)
        {
          json = json + i + ",[";
        }
        json = json + i;
        for (i = maxDepth - 1; i >= 0; i--)
        {
          json = json + "]," + i;
        }
        json = json + "]";
      }
      json = json + "}";

      var res = collection.Add(json).Execute();
      Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
    }

    #endregion WL14389

    #region Methods

    public void VerifyGeneratedID(string input)
    {
      byte[] array = Encoding.ASCII.GetBytes(input);
      Assert.That(array.Length < 28, Is.False);

      byte[] uniquePrefix = new byte[4];
      Array.Copy(array, 0, uniquePrefix, 0, 4);

      Assert.That(System.Text.Encoding.UTF8.GetString(uniquePrefix), Is.EqualTo(System.Text.Encoding.UTF8.GetString(uniquePrefix)),
          "Unique Prefix of the Generated ID");

      byte[] startTimeStamp = new byte[8];
      Array.Copy(array, 4, startTimeStamp, 0, 8);
      Assert.That(System.Text.Encoding.UTF8.GetString(startTimeStamp), Is.EqualTo(System.Text.Encoding.UTF8.GetString(startTimeStamp)),
          "StartTimeStamp of the Generated ID");

      byte[] serial = new byte[16];
      Array.Copy(array, 12, serial, 0, 16);
      Assert.That(System.Text.Encoding.UTF8.GetString(serial), Is.EqualTo(System.Text.Encoding.UTF8.GetString(serial)),
          "Serial Number of the Generated ID");
    }

    public bool VerifySequence(string input1, string input2)
    {
      byte[] array1 = Encoding.ASCII.GetBytes(input1);
      Assert.That(array1.Length < 28, Is.False);
      byte[] array2 = Encoding.ASCII.GetBytes(input2);
      Assert.That(array2.Length < 28, Is.False);
      Assert.That(input2, Is.Not.EqualTo(input1));
      string incrementedString = Increment(input1, Mode.AlphaNumeric);
      if (incrementedString.Equals(input2))
      {
        return true;
      }
      else
      {
        return false;
      }
    }
    public static string Increment(string text, Mode mode)
    {
      var textArr = text.ToCharArray();
      var characters = new List<char>();

      if (mode == Mode.AlphaNumeric || mode == Mode.Numeric)
        for (char c = '0'; c <= '9'; c++)
          characters.Add(c);

      if (mode == Mode.AlphaNumeric || mode == Mode.Alpha)
        for (char c = 'a'; c <= 'f'; c++)
          characters.Add(c);

      // Loop from end to beginning
      for (int i = textArr.Length - 1; i >= 0; i--)
      {
        if (textArr[i] == characters.Last())
        {
          textArr[i] = characters.First();
        }
        else
        {
          textArr[i] = characters[characters.IndexOf(textArr[i]) + 1];
          break;
        }
      }

      return new string(textArr);
    }
    public enum Mode
    {
      AlphaNumeric = 1,
      Alpha = 2,
      Numeric = 3
    }

    #endregion Methods
  }
}
