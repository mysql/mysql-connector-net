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
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace MySqlX.Data.Tests
{
  public class CrudRemoveTests : BaseTest
  {
    [Test]
    public void RemoveSingleDocumentById()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]{
        new { _id = 12, title = "Book 1", pages = 20 },
        new { _id = 34, title = "Book 2", pages = 30 },
        new { _id = 56, title = "Book 3", pages = 40 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.AreEqual(3, r.AffectedItemsCount);

      // Remove with condition.
      r = ExecuteRemoveStatement(coll.Remove("_id = 12"));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Remove by ID.
      r = coll.RemoveOne(34);
      Assert.AreEqual(1, r.AffectedItemsCount);

      var ex = Assert.Throws<ArgumentNullException>(() => coll.Remove(""));
#if (NETCOREAPP3_1 || NET5_0 || NET6_0)
      Assert.AreEqual("Parameter can't be null or empty. (Parameter 'condition')", ex.Message);
#else
      Assert.AreEqual("Parameter can't be null or empty.\r\nParameter name: condition", ex.Message);
#endif
    }

    [Test]
    public void RemoveMultipleDocuments()
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

      r = ExecuteRemoveStatement(coll.Remove("pages > 20"));
      Assert.AreEqual(3, r.AffectedItemsCount);
    }

    [Test]
    public void RemoveMultipleDocumentsWithLimit()
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

      r = ExecuteRemoveStatement(coll.Remove("pages > 20").Limit(1));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("True").Limit(0)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("True").Limit(-2)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("pages > 10").Limit(0)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("pages > 20").Limit(-3)));
    }

    [Test]
    public void RemoveMultipleDocumentsWithLimitAndOrder()
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

      r = ExecuteRemoveStatement(coll.Remove("pages > 20").Limit(1));
      Assert.AreEqual(1, r.AffectedItemsCount);
    }

    [Test]
    public void RemovingDocWithNoIdThrowsException()
    {
      Collection coll = CreateCollection("test");
      DbDoc doc = new DbDoc();
      Exception ex = Assert.Throws<KeyNotFoundException>(() => ExecuteRemoveStatement(coll.Remove("_id = :id").Bind("id", doc.Id)));
    }

    [Test]
    public void RemoveBind()
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

      r = ExecuteRemoveStatement(coll.Remove("pages = :Pages").Bind("pAges", 50));
      Assert.AreEqual(1, r.AffectedItemsCount);

      var jsonParams = new { pages1 = 30, pages2 = 40 };
      var res = coll.Remove("pages = :Pages1 || pages = :Pages2").Bind(jsonParams).Execute();
      Assert.AreEqual(2, res.AffectedItemsCount);

      DbDoc docParams = new DbDoc(new { pages1 = 10, pages2 = 20 });
      coll.Remove("pages = :Pages1 || pages = :Pages2").Bind(docParams).Execute();
      Assert.True(res.AffectedItemsCount > 0);
    }

    [Test]
    public void RemoveAll()
    {
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

      // Condition can't be null or empty.
      string errorMessage = string.Empty;
#if (NETCOREAPP3_1 || NET5_0 || NET6_0)
      errorMessage = "Parameter can't be null or empty. (Parameter 'condition')";
#else
      errorMessage = "Parameter can't be null or empty.\r\nParameter name: condition";
#endif
      Exception ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove(string.Empty)));
      Assert.AreEqual(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove("")));
      Assert.AreEqual(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove(" ")));
      Assert.AreEqual(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove("  ")));
      Assert.AreEqual(errorMessage, ex.Message);

      // Sending an expression that evaluates to true applies changes on all documents.
      result = ExecuteRemoveStatement(collection.Remove("true"));
      Assert.AreEqual(4, result.AffectedItemsCount);
    }

    [Test]
    public void RemoveWithInOperator()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new DbDoc("{ \"a\": 1, \"b\": \"foo\", \"c\": { \"d\": true, \"e\": [1,2,3] }, \"f\": [ {\"x\":5}, {\"x\":7 } ] }"),
        new DbDoc("{ \"a\": 2, \"b\": \"foo2\", \"c\": { \"d\": true, \"e\": [4,5,6] }, \"f\": [ {\"x\":5}, {\"x\":8 } ] }"),
        new DbDoc("{ \"a\": 1, \"b\": \"foo3\", \"c\": { \"d\": true, \"e\": [1,4,3] }, \"f\": [ {\"x\":6}, {\"x\":9 } ] }"),
      };
      Result result = ExecuteAddStatement(collection.Add(docs));
      Assert.AreEqual(3, result.AffectedItemsCount);

      Assert.AreEqual(1, ExecuteRemoveStatement(collection.Remove("a IN (2,3)")).AffectedItemsCount);
      Assert.AreEqual(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      Assert.AreEqual(0, ExecuteRemoveStatement(collection.Remove("a IN [3]")).AffectedItemsCount);
      Assert.AreEqual(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      Assert.AreEqual(2, ExecuteRemoveStatement(collection.Remove("1 IN c.e")).AffectedItemsCount);
      CollectionAssert.IsEmpty(ExecuteFindStatement(collection.Find()).FetchAll());
    }

    [Test]
    public void RemoveOne()
    {
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

      ExecuteAddStatement(collection.Add(new { _id = 5, title = "Book 5", pages = 60 }));
      Assert.AreEqual(5, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove sending numeric parameter.
      Assert.AreEqual(1, collection.RemoveOne(1).AffectedItemsCount);
      Assert.AreEqual(4, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove sending string parameter.
      Assert.AreEqual(1, collection.RemoveOne("3").AffectedItemsCount);
      Assert.AreEqual(3, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove an auto-generated id.
      DbDoc document = ExecuteFindStatement(collection.Find("pages = 60")).FetchOne();
      Assert.AreEqual(1, collection.RemoveOne(document.Id).AffectedItemsCount);
      Assert.AreEqual(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove a non-existing document.
      Assert.AreEqual(0, collection.RemoveOne(5).AffectedItemsCount);
      Assert.AreEqual(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(null));
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(""));
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(string.Empty));
    }

    #region WL14389

    [Test, Description("MySQLX CNET Forbid remove() with no condition-Scenario-2")]
    public void ForbidRemoveWithNoCondition()
    {
      Collection collection = CreateCollection("test");
      var docs = new[]
      {
                    new {  _id = 1, title = "Book 1", pages = 20 },
                    new {  _id = 2, title = "Book 2", pages = 30 },
                    new {  _id = 3, title = "Book 3", pages = 40 },
                    new {  _id = 4, title = "Book 4", pages = 50 },
                    };
      Result result = collection.Add(docs).Execute();
      Assert.AreEqual(4, result.AffectedItemsCount);

      result = collection.Remove("_id = 1").Execute();
      Assert.AreEqual(1, result.AffectedItemsCount);
      result = collection.Remove("_id = 10").Execute();
      Assert.AreEqual(0, result.AffectedItemsCount);
      result = collection.Remove("_id = 2").Execute();
      Assert.AreEqual(1, result.AffectedItemsCount);
      result = collection.Remove("_id = 10").Execute();
      Assert.AreEqual(0, result.AffectedItemsCount);
      Assert.Throws<ArgumentNullException>(() => collection.Remove(""));
    }

    [Test, Description("Test MySQLX plugin MySQL Net 846 - Collection Unset Multiple")]
    public void CollectionUnsetMultiple()
    {
      Collection col = CreateCollection("my_collection_1");

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
      //Unset with multiple variables not supported
      col.Modify("_id = 1").Unset(new string[] { "count", "books" }).Execute();
      col.Modify("_id = 1").Set("count", 10).Set("books", "test1").Execute();

    }

    [Test, Description("Test MySQLX plugin RemovingItemUsingDbDoc")]
    public void RemovingItemUsingDbDoc()
    {
      Collection coll = CreateCollection("test");
      DbDoc doc = new DbDoc(new { _id = 1, title = "Book 1", pages = 20 });
      Result r = coll.Add(doc).Execute();
      Assert.AreEqual(1, (int)r.AffectedItemsCount, "Match being done");
      r = coll.Remove("_id=1").Execute();
      Assert.AreEqual(1, (int)r.AffectedItemsCount, "Match being done");
    }

    #endregion WL14389
  }
}
