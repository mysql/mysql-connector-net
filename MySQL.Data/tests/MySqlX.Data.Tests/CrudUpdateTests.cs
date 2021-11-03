// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
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

using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace MySqlX.Data.Tests
{
  public class CrudUpdateTests : BaseTest
  {
    [Test]
    public void SetItemInSingleDocument()
    {
      Collection coll = CreateCollection("test");
      Result result = ExecuteAddStatement(coll.Add(new { _id = 1, name = "Book 1" }));
      Assert.AreEqual(1, result.AffectedItemsCount);

      // Set integer value.
      result = ExecuteModifyStatement(coll.Modify("_id = 1").Set("pages", "20"));
      Assert.AreEqual(1, result.AffectedItemsCount);
      Assert.AreEqual("20", coll.GetOne(1)["pages"]);

      // Set null value.
      result = ExecuteModifyStatement(coll.Modify("_id = 1").Set("pages", null));
      Assert.AreEqual(1, result.AffectedItemsCount);
      Assert.Null(coll.GetOne(1)["pages"]);

      // Set existing field.
      result = ExecuteModifyStatement(coll.Modify("_id = 1").Set("name", "Book 2"));
      Assert.AreEqual(1, result.AffectedItemsCount);
      Assert.AreEqual("Book 2", coll.GetOne(1)["name"]);

      // Set alphanumeric field.
      var document = new DbDoc();
      document.SetValue("_id", 2);
      document.SetValue("1a", "other");
      result = ExecuteAddStatement(coll.Add(document));
      Assert.AreEqual(1, result.AffectedItemsCount);
      var insertedDocument = coll.GetOne(2);

      //result = coll.Modify("_id = 1").Set("1a", "other"));
    }

    [Test]
    public void ChangeItemInSingleDocument()
    {
      Collection coll = CreateCollection("test");
      Result result = ExecuteAddStatement(coll.Add(new { _id = 1, name = "Book 1", pages = 20 }));
      Assert.AreEqual(1, result.AffectedItemsCount);

      result = ExecuteModifyStatement(coll.Modify("_id = 1").Change("name", "Book 2"));
      Assert.AreEqual(1, result.AffectedItemsCount);
    }

    [Test]
    public void RemoveItemInSingleDocumentUsingUnset()
    {
      Collection coll = CreateCollection("test");
      Result result = ExecuteAddStatement(coll
                .Add(new { _id = 1, name = "Book 1", pages = 20 })
                .Add(new { _id = 2, name = "Book 2", pages = 30 })
                .Add(new { _id = 3, name = "Book 3", pages = 40, author = "John", author2 = "Mary" })
                );
      Assert.AreEqual(3, result.AffectedItemsCount);

      // Unset 1 field.
      result = ExecuteModifyStatement(coll.Modify("_id = 1").Unset("pages"));
      Assert.AreEqual(1, result.AffectedItemsCount);
      var document = ExecuteFindStatement(coll.Find("_id = 1")).FetchOne();
      Assert.AreEqual(2, document.values.Count);

      // Unset multiple fields.
      result = ExecuteModifyStatement(coll.Modify("_id = 2").Unset("name", "pages"));
      Assert.AreEqual(1, result.AffectedItemsCount);
      document = ExecuteFindStatement(coll.Find("_id = 2")).FetchOne();
      Assert.That(document.values, Has.One.Items);
      result = ExecuteModifyStatement(coll.Modify("_id = 3").Unset(null, "author", "author2"));
      document = ExecuteFindStatement(coll.Find("_id = 3")).FetchOne();
      Assert.AreEqual(3, document.values.Count);

      // Unsetting nonexistent fields doesn't raise an error.
      result = ExecuteModifyStatement(coll.Modify("_id = 2").Unset("otherfield"));
      Assert.AreEqual(0ul, result.AffectedItemsCount);

      // Unsetting null items combined with valid values are ignored.
      result = ExecuteModifyStatement(coll.Modify("_id = 3").Unset(null).Unset("name"));
      Assert.AreEqual(1ul, result.AffectedItemsCount);
      document = ExecuteFindStatement(coll.Find("_id = 3")).FetchOne();
      Assert.AreEqual(2, document.values.Count);

      // Unsetting single null items raises an error
      var ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll.Modify("_id = 3").Unset(null)));
      Assert.AreEqual("Invalid update expression list", ex.Message);

      // Unsetting empty strings raises an error.
      ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll.Modify("_id = 2").Unset("")));
      Assert.AreEqual("The path expression '$' is not allowed in this context.", ex.Message);
      ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(coll.Modify("_id = 2").Unset(string.Empty)));
      Assert.AreEqual("The path expression '$' is not allowed in this context.", ex.Message);

      // Unset with special chars.
      var ex2 = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(coll.Modify("_id = 3").Unset(null).Unset("@*%#ç")));
      Assert.AreEqual("The path expression '$' is not allowed in this context.", ex.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(coll.Modify("_id = 3").Unset(null).Unset("******")));
      Assert.AreEqual("The path expression '$' is not allowed in this context.", ex.Message);
    }

    [Test]
    public void SetItemAndBind()
    {
      Collection coll = CreateCollection("test");
      Result result = ExecuteAddStatement(coll.Add(new { _id = 1, name = "Book 1" })
        .Add(new { _id = 2, name = "Book 2" }));
      Assert.AreEqual(2, result.AffectedItemsCount);

      var stmt = coll.Modify("_id = :ID");
      result = ExecuteModifyStatement(stmt.Bind("Id", 2).Set("pages", "20"));
      Assert.AreEqual(1, result.AffectedItemsCount);
      result = ExecuteModifyStatement(stmt.Bind("Id", 1).Set("pages", 10)); Assert.AreEqual(1, result.AffectedItemsCount);

      var docs = ExecuteFindStatement(coll.Find()).FetchAll();
      Assert.AreEqual(new DbDoc("{ \"_id\": 1, \"name\": \"Book 1\", \"pages\": 10 }").ToString(), docs[0].ToString());
      Assert.AreEqual(new DbDoc("{ \"_id\": 2, \"name\": \"Book 2\", \"pages\": \"20\" }").ToString(), docs[1].ToString());
    }

    [Test]
    public void ModifyAll()
    {
      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };
      Result result = ExecuteAddStatement(collection.Add(docs));
      Assert.AreEqual(2, result.AffectedItemsCount);

      // Condition can't be null or empty.
      string errorMessage = string.Empty;
#if (NETCOREAPP3_1 || NET5_0 || NET6_0)
      errorMessage = "Parameter can't be null or empty. (Parameter 'condition')";
#else
      errorMessage = "Parameter can't be null or empty.\r\nParameter name: condition";
#endif
      Exception ex = Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify(string.Empty)));
      Assert.AreEqual(ex.Message, errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("")));
      Assert.AreEqual(ex.Message, errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify(" ")));
      Assert.AreEqual(ex.Message, errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("   ")));
      Assert.AreEqual(ex.Message, errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify(null)));
      Assert.AreEqual(ex.Message, errorMessage);

      // Sending an expression that evaluates to true applies changes on all documents.
      result = ExecuteModifyStatement(collection.Modify("true").Set("pages", "10"));
      Assert.AreEqual(2, result.AffectedItemsCount);
    }

    [Test]
    public void ModifyWithLimit()
    {
      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };
      Result result = ExecuteAddStatement(collection.Add(docs));
      Assert.AreEqual(2, result.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("true").Set("title", "Book X").Limit(1));
      Assert.That(ExecuteFindStatement(collection.Find("title = \"Book X\"")).FetchAll(), Has.One.Items);

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteModifyStatement(collection.Modify("true").Set("pages", 10).Limit(0)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteModifyStatement(collection.Modify("true").Set("pages", 10).Limit(-10)));
    }

    [Test]
    public void ModifyWithInOperator()
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

      Assert.AreEqual(3, ExecuteModifyStatement(collection.Modify("a IN (1,2)").Set("a", 3)).AffectedItemsCount);
      Assert.AreEqual(3, ExecuteFindStatement(collection.Find().Where("a = 3")).FetchAll().Count);

      Assert.AreEqual(3, ExecuteModifyStatement(collection.Modify("a IN [3]").Set("a", 1)).AffectedItemsCount);
      Assert.AreEqual(3, ExecuteFindStatement(collection.Find().Where("a = 1")).FetchAll().Count);

      Assert.AreEqual(2, ExecuteModifyStatement(collection.Modify("1 IN c.e").Set("c.e", "newValue")).AffectedItemsCount);
      Assert.AreEqual(2, ExecuteFindStatement(collection.Find().Where("c.e = \"newValue\"")).FetchAll().Count);
    }

    [Test]
    public void ReplaceOne()
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

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne(null, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne("", docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne(string.Empty, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne("1", null));
      // Replace using no matching id
      Assert.Throws<MySqlException>(() => collection.ReplaceOne(3, new DbDoc("{ \"_id\": 2, \"name\": \"John\", \"lastName\": \"Smith\" }")),
        ResourcesX.ReplaceWithNoMatchingId);

      var newDoc = new { _id = 1, title = "Book 11", pages = 311 };

      // Replace using a numeric identifier.
      Assert.AreEqual(1, collection.ReplaceOne(1, newDoc).AffectedItemsCount);
      DbDoc document = collection.GetOne(1);
      Assert.AreEqual(1, Convert.ToInt32(document.Id));
      Assert.AreEqual("Book 11", document["title"]);
      Assert.AreEqual(311, Convert.ToInt32(document["pages"]));

      // Replace using a string identifier.
      Assert.AreEqual(1, collection.ReplaceOne("2", new DbDoc("{ \"name\": \"John\", \"lastName\": \"Smith\" }")).AffectedItemsCount);
      document = collection.GetOne(2);
      Assert.AreEqual(2, Convert.ToInt32(document.Id));
      Assert.AreEqual("John", document["name"]);
      Assert.AreEqual("Smith", document["lastName"]);

      // Replace a non-existing document.
      Assert.AreEqual(0, collection.ReplaceOne(5, docs[1]).AffectedItemsCount);
      Assert.True(collection.GetOne(5) == null);
    }

    [Test]
    public void ReplaceNestedDocument()
    {
      var collection = CreateCollection("test");
      var docs = new DbDoc[]
      {
        new DbDoc(@"{ ""_id"":1, ""pages"":20, ""title"":""Book 1"", ""person"": { ""name"": ""Fred"", ""age"":45 } }" ),
        new DbDoc(@"{ ""_id"": 2, ""pages"": 30,""title"" : ""Book 2"", ""person"": { ""name"": ""Peter"", ""age"": 38 } }"),
        new DbDoc(@"{ ""_id"": 3, ""pages"": 40,""title"" : ""Book 3"", ""person"": { ""name"": ""Andy"", ""age"": 25 } }"),
        new DbDoc(@"{ ""_id"": 4, ""pages"": 50,""title"" : ""Book 4"", ""person"": { ""name"": ""John"", ""age"": 34 } }")
      };
      Assert.AreEqual(4, ExecuteAddStatement(collection.Add(docs)).AffectedItemsCount);

      DbDoc d_new = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,""title"" : ""Book 1"", ""person"": { ""name"": ""Fred"", ""age"": 45 ,""State"" : ""Ohio""} }");
      Assert.AreEqual(1, collection.ReplaceOne(1, d_new).AffectedItemsCount);
      DbDoc document = collection.GetOne(1);
      Assert.AreEqual("Ohio", (document.values["person"] as Dictionary<string, object>)["State"]);

      d_new = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,""title"" : ""Book 1"", ""person"": { ""name"": ""Fred"", ""age"": 45 ,""State"" : ""Ohio"", ""newProp"": { ""a"":33 } } }");
      Assert.AreEqual(1, collection.ReplaceOne(1, d_new).AffectedItemsCount);
      document = collection.GetOne(1);
      Assert.AreEqual(33, ((document.values["person"] as Dictionary<string, object>)["newProp"] as Dictionary<string, object>)["a"]);
    }

    [Test]
    public void ArrayInsert()
    {
      Collection collection = CreateCollection("test");
      ExecuteAddStatement(collection.Add("{ \"x\":[1,2] }"));

      // x[1]=43, x[2]=2. 
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[1]", 43));
      // x[3]=44.
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[3]", 44));
      // Since array only contains 4 items the value 46 is assigned to x[4].
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[5]", 46));
      // Since array only contains 5 items the value 50 is assigned to x[5].
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[20]", 50));
      // Assign an item from different data type.
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[6]", "string"));
      // Assign a document.
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[7]", "{ \"name\":\"Mike\" }"));

      var result = ExecuteFindStatement(collection.Find());
      var document = result.FetchOne();
      var x = (object[])document.values["x"];

      Assert.AreEqual(8, x.Length);
      Assert.AreEqual(1, (int)x[0]);
      Assert.AreEqual(43, (int)x[1]);
      Assert.AreEqual(2, (int)x[2]);
      Assert.AreEqual(44, (int)x[3]);
      Assert.AreEqual(46, (int)x[4]);
      Assert.AreEqual(50, (int)x[5]);
      Assert.AreEqual("string", x[6]);
      Assert.True(new DbDoc(x[7]) is DbDoc);

      // No value is inserted if the array doesn't exist.
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("y[0]", 1));

      result = ExecuteFindStatement(collection.Find());
      document = result.FetchOne();
      Assert.False(document.values.ContainsKey("y"));

      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[0]", null));
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[1]", " "));

      result = ExecuteFindStatement(collection.Find());
      document = result.FetchOne();
      x = (object[])document.values["x"];
      Assert.Null(x[0]);
      Assert.AreEqual(" ", x[1]);

      // Insert an empty string fails.
      var ex = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[0]", "")));
      StringAssert.Contains("String can't be empty.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("true").ArrayInsert("x[0]", string.Empty)));
      StringAssert.Contains("String can't be empty.", ex.Message);

      // Not specifying an index raises an error.
      var ex2 = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(collection.Modify("true").ArrayInsert("dates", "5/1/2018")));
      Assert.AreEqual("A path expression is not a path to a cell in an array.", ex2.Message);

      var col = CreateCollection("my_collection");
      var t1 = "{\"_id\": \"1001\", \"ARR\":[1,2,3], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      col.Add(t1).Execute();
      col.Modify("true").ArrayInsert("ARR[0]", 4).Execute();
      col.Modify("true").ArrayInsert("ARR1[0]", "name4").Execute();
      col.Modify("true").ArrayInsert("ARR[0]", "name5").Execute();
      col.Modify("true").ArrayInsert("ARR1[0]", 5).Execute();
      col.Modify("true").ArrayInsert("ARR[0]", 6).ArrayInsert("ARR1[0]", "name6").ArrayInsert("ARR[0]", 7).ArrayInsert("ARR[0]", 8).Execute();
      col.Modify("true").ArrayInsert("ARR1[0]", null).Execute();
      col.Modify("true").ArrayInsert("ARR1[0]", " ").Execute();
      col.Modify("true").ArrayInsert("ARR1[0]", "****").Execute();
      result = ExecuteFindStatement(col.Find());
      document = result.FetchOne();
      var x2 = (object[])document.values["ARR"];
      Assert.AreEqual(8, x2.Length);
      Assert.AreEqual(8, (int)x2[0]);
      x2 = (object[])document.values["ARR1"];
      Assert.AreEqual("****", x2[0]);
      Assert.AreEqual("name3", x2[8]);
    }

    [Test]
    public void ArrayAppendWithMySqlExpression()
    {
      Collection collection = CreateCollection("test");

      // String containing an expression is not evaluted.
      ExecuteAddStatement(collection.Add("{ \"_id\":\"123\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("email", "UPPER($.name)"));
      var document = collection.GetOne("123");
      Assert.AreEqual("UPPER($.name)", (document["email"] as object[])[1]);

      // Use MySqlExpression.
      ExecuteAddStatement(collection.Add("{ \"_id\":\"124\", \"name\":\"alice\", \"value\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("_id = \"124\"").ArrayAppend("value", new MySqlExpression("UPPER($.name)")));
      document = collection.GetOne("124");
      Assert.AreEqual("ALICE", (document["value"] as object[])[1]);

      // Use embedded MySqlExpression.
      ExecuteAddStatement(collection.Add("{ \"_id\":\"125\", \"name\":\"alice\", \"value\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("_id = \"125\"").ArrayAppend("value", new { expression = new MySqlExpression("UPPER($.name)") }));
      document = collection.GetOne("125");
      var item = ((document["value"] as object[])[1] as Dictionary<string, object>);
      Assert.AreEqual("ALICE", item["expression"]);

      ExecuteAddStatement(collection.Add("{ \"_id\":\"126\", \"name\":\"alice\", \"value\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("_id = \"126\"").ArrayAppend("value", new { expression = new MySqlExpression("UPPER($.name)"), literal = "UPPER($.name)" }));
      document = collection.GetOne("126");
      item = ((document["value"] as object[])[1] as Dictionary<string, object>);
      Assert.AreEqual("ALICE", item["expression"]);
      Assert.AreEqual("UPPER($.name)", item["literal"]);
    }

    [Test]
    public void ArrayAppendUsesCorrectDataTypes()
    {
      Collection collection = CreateCollection("test");
      ExecuteAddStatement(collection.Add("{ \"_id\":\"123\", \"email\":[ \"alice@ora.com\"], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("dates", "1"));
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("dates", 1));
      var document = collection.GetOne("123");
      var dates = document["dates"] as object[];
      Assert.True(dates[1] is string);
      Assert.True(dates[2] is int);
    }

    [Test]
    public void ArrayAppend()
    {
      Collection collection = CreateCollection("test");
      ExecuteAddStatement(collection.Add("{ \"x\":[1,2] }"));

      // Append values of different types, null and spaces.
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("x", 43));
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("x", "string"));
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("x", true));
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("x", null));
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("x", " "));
      DocResult result = ExecuteFindStatement(collection.Find());
      DbDoc document = result.FetchOne();
      var x = (object[])document.values["x"];

      Assert.AreEqual(7, x.Length);
      Assert.AreEqual(1, (int)x[0]);
      Assert.AreEqual(2, (int)x[1]);
      Assert.AreEqual(43, (int)x[2]);
      Assert.AreEqual("string", x[3]);
      Assert.AreEqual(true, x[4]);
      Assert.Null(x[5]);
      Assert.AreEqual(" ", x[6]);

      // No value is appended if the array doesn't exist.
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("y", 45));

      result = ExecuteFindStatement(collection.Find());
      document = result.FetchOne();
      Assert.False(document.values.ContainsKey("y"));

      var ex = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("true").ArrayAppend("x", "")));
      StringAssert.Contains("String can't be empty.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("true").ArrayAppend("x", string.Empty)));
      StringAssert.Contains("String can't be empty.", ex.Message);

      var col = CreateCollection("my_collection");
      var t1 = "{\"_id\": \"1001\", \"ARR\":[1,2,3], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      col.Add(t1).Execute();
      col.Modify("true").ArrayAppend("ARR", 4).Execute();
      col.Modify("true").ArrayAppend("ARR1", "name4").Execute();
      col.Modify("true").ArrayAppend("ARR", "name5").Execute();
      col.Modify("true").ArrayAppend("ARR1", 5).Execute();
      col.Modify("true").ArrayAppend("ARR", 6).ArrayAppend("ARR1", "name6").ArrayAppend("ARR", 7).ArrayAppend("ARR", 8).Execute();

      Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(col.Modify("true").ArrayAppend("ARR1", "")));
    }

    [Test]
    public void ArrayInsertWithMySqlExpression()
    {
      Collection collection = CreateCollection("test");
      ExecuteAddStatement(collection.Add("{ \"x\":[1,2] }"));

      // String containing an expression is not evaluted.
      ExecuteAddStatement(collection.Add("{ \"_id\":\"123\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("email[0]", "UPPER($.name)"));
      var document = collection.GetOne("123");
      Assert.AreEqual("UPPER($.name)", (document["email"] as object[])[0]);

      // Use MySqlExpression.
      ExecuteAddStatement(collection.Add("{ \"_id\":\"124\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("_id = \"124\"").ArrayInsert("email[0]", new MySqlExpression("UPPER($.name)")));
      document = collection.GetOne("124");
      Assert.AreEqual("ALICE", (document["email"] as object[])[0]);

      // Use embedded MySqlExpression.
      ExecuteAddStatement(collection.Add("{ \"_id\":\"125\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("_id = \"125\"").ArrayInsert("email[0]", new { other = new MySqlExpression("UPPER($.name)") }));
      document = collection.GetOne("125");
      var item = ((document["email"] as object[])[0] as Dictionary<string, object>);
      Assert.AreEqual("ALICE", item["other"]);

      ExecuteAddStatement(collection.Add("{ \"_id\":\"126\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }"));
      ExecuteModifyStatement(collection.Modify("_id = \"126\"").ArrayInsert("email[0]", new { other = new MySqlExpression("UPPER($.name)"), literal = "UPPER($.name)" }));
      document = collection.GetOne("126");
      item = ((document["email"] as object[])[0] as Dictionary<string, object>);
      Assert.AreEqual("ALICE", item["other"]);
      Assert.AreEqual("UPPER($.name)", item["literal"]);
    }

    [Test]
    public void ArrayOperationsKeepDateValue()
    {
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add("{ \"_id\": \"123\", \"email\":[\"alice@ora.com\"], \"dates\": \"5/1/2018\" }"));
      Assert.AreEqual(1ul, r.AffectedItemsCount);

      // No items are affected since dates isn't an array.
      r = ExecuteModifyStatement(collection.Modify("true").ArrayInsert("dates[0]", "4/1/2018"));
      Assert.AreEqual(0ul, r.AffectedItemsCount);

      // Converts a non array to an array by appending a value.
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("dates", "6/1/2018"));

      // Array insert at specified index is now succesful since dates is an array.
      ExecuteModifyStatement(collection.Modify("true").ArrayInsert("dates[0]", "4/1/2018"));

      DbDoc document = collection.GetOne("123");
      object[] dates = document["dates"] as object[];
      Assert.AreEqual(3, dates.Length);
      Assert.AreEqual("4/1/2018", dates[0]);
      Assert.AreEqual("5/1/2018", dates[1]);
      Assert.AreEqual("6/1/2018", dates[2]);
    }

    [Test]
    public void Alphanumeric()
    {
      Collection collection = CreateCollection("test");
      var document = new DbDoc();

      for (int i = 0; i < 30; i++)
      {
        document.SetValue("_id", i);
        document.SetValue("books", "test" + i);
        document.SetValue("pages", i + 10);
        document.SetValue("reviewers", "reviewers" + i);
        document.SetValue("person", new
        {
          name = "Fred" + i,
          age = i
        });
        document.SetValue("1address", "street" + i);
        ExecuteAddStatement(collection.Add(document));
      }

      var crudresult = collection.Find("pages=10").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 before Unset of pages for _id=0.");
      var result = collection.Modify("_id = 0").Unset("pages").Execute();
      Assert.AreEqual(1, result.AffectedItemsCount, "Affected Items Count when modify unset is used");
      crudresult = collection.Find("pages=10").Execute().FetchAll();
      Assert.AreEqual(0, crudresult.Count, "Count should be 0 after Unset of pages for _id=0");
      crudresult = collection.Find("books='test0'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 0 after Unset of pages for _id=0");

      result = collection.Modify("_id = 21").Unset("1address").Execute();
      Assert.AreEqual(1, result.AffectedItemsCount, "Affected Items Count when modify unset(multiple docs) is used");
    }

    [Test]
    public void UnsetVariations()
    {
      Collection collection = CreateCollection("test");
      var document = new DbDoc("{ \"_id\":1, \"pages\":1, \"pages2\":2, \"pages3\":3, \"pages4\":{ \"internalPages\":4 } }");
      ExecuteAddStatement(collection.Add(document));

      // Whitespace is ignored.
      ExecuteModifyStatement(collection.Modify("_id = 1").Unset("pages "));
      Assert.False(collection.GetOne(1).values.ContainsKey("pages"));
      ExecuteModifyStatement(collection.Modify("_id = 1").Unset(" pages2 "));
      Assert.False(collection.GetOne(1).values.ContainsKey("pages2"));
      ExecuteModifyStatement(collection.Modify("_id = 1").Unset(" pages3"));
      Assert.False(collection.GetOne(1).values.ContainsKey("pages3"));
      ExecuteModifyStatement(collection.Modify("_id = 1").Unset("  pages4.internalPages  "));
      Assert.True(collection.GetOne(1).values.ContainsKey("pages4"));
      Assert.False(collection.GetOne(1).values.ContainsKey("pages4.internalPages"));

      // Error is raised with incorrect document path.
      var ex = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("_id = 1").Unset("pages*")));
      Assert.AreEqual("Invalid document path.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("_id = 1").Unset("pages!")));
      Assert.AreEqual("Invalid document path.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("_id = 1").Unset("pages*data")));
      Assert.AreEqual("Invalid document path.", ex.Message);
    }

    #region WL14389

    [Test, Description("Collection.Modify(condition).Unset() to accept a list of elements instead of just one.")]
    public void CollectionModifyUnset()
    {
      List<string> idStringList = new List<string>();
      var col = CreateCollection("my_collection");
      var d1 = new DbDoc();
      for (int i = 0; i < 30; i++)
      {
        d1.SetValue("_id", i);
        d1.SetValue("books", "test" + i);
        d1.SetValue("pages", i + 10);
        d1.SetValue("reviewers", "reviewers" + i);
        d1.SetValue("person", new { name = "Fred" + i, age = i });
        d1.SetValue("1address", "street" + i);
        col.Add(d1).Execute();
      }

      var crudresult = col.Find("pages=10").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 before Unset of pages for _id=0.");
      var result = col.Modify("_id = 0").Unset("pages").Execute();
      Assert.AreEqual(1, result.AffectedItemsCount, "Affected Items Count when modify unset is used");
      crudresult = col.Find("pages=10").Execute().FetchAll();
      Assert.AreEqual(0, crudresult.Count, "Count should be 0 after Unset of pages for _id=0");
      crudresult = col.Find("books='test0'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 0 after Unset of pages for _id=0");

      crudresult = col.Find("pages=11").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count of pages=11 should be 1 before Unset of pages for _id=1.");
      crudresult = col.Find("reviewers='reviewers1'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count of reviewers1 should be 1 before Unset of pages for _id=1.");
      result = col.Modify("_id = 1").Unset("pages").Unset("reviewers").Execute();
      Assert.AreEqual(1, result.AffectedItemsCount, "Affected Items Count when modify multiple unset is used");
      crudresult = col.Find("pages=11").Execute().FetchAll();
      Assert.AreEqual(0, crudresult.Count, "Count should be 0 after Unset of pages for _id=1");
      crudresult = col.Find("reviewers='reviewers1'").Execute().FetchAll();
      Assert.AreEqual(0, crudresult.Count, "Count should be 0 after Unset of pages for _id=1");

      crudresult = col.Find("pages=21").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count of pages=21 should be 1 before Unset of pages for _id=11.");
      crudresult = col.Find("reviewers='reviewers11'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count of reviewers11 should be 1 before Unset of pages for _id=11.");
      result = col.Modify("_id = 11").Unset(new string[] { "pages", "reviewers" }).Execute();
      Assert.AreEqual(1, result.AffectedItemsCount, "Affected Items Count when modify unset(multiple docs) is used");
      crudresult = col.Find("pages=21").Execute().FetchAll();
      Assert.AreEqual(0, crudresult.Count, "Count should be 0 after Unset of pages for _id=11");
      crudresult = col.Find("reviewers='reviewers11'").Execute().FetchAll();
      Assert.AreEqual(0, crudresult.Count, "Count should be 0 after Unset of pages for _id=11");

      crudresult = col.Find("pages=31").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count of pages=31 should be 1 before Unset of pages for _id=21.");
      crudresult = col.Find("reviewers='reviewers21'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count of reviewers21 should be 1 before Unset of pages for _id=21.");

      result = col.Modify("_id = 21").Unset(" pages ").Execute();
      Assert.AreEqual(1, result.AffectedItemsCount, "Affected Items Count when modify unset(multiple docs) is used");
      //Should have failed when unset is used for fields with special characters
      Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(col.Modify("_id = 22").Unset("pages*")));
      //Should have failed when unset is used for non-existent fields
      Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(col.Modify("_id = 21").Unset("1")));
      //Should have failed when unset is used for special characters
      Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(col.Modify("_id = 21").Unset("@*%#^)(-+!~<>?/")));
      //Should have failed when unset is used for special characters
      Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(col.Modify("_id = 21").Unset("*******")));

      crudresult = col.Find("pages=31").Execute().FetchAll();
      Assert.AreEqual(0, crudresult.Count, "Count should be 0 after Unset of pages for _id=21");

      result = col.Modify("_id = 12").Unset(new string[] { " pages1", "reviewers1" }).Execute();
      Assert.AreEqual(0, result.AffectedItemsCount, "Affected Items Count when modify unset(invalid docs) is used");
      crudresult = col.Find("pages=22").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with invalid  of pages for _id=12");
      crudresult = col.Find("reviewers='reviewers12'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with invalid of pages for _id=12");
      //Testcase should have failed when unset is used with null
      Assert.Throws<MySqlException>(() => ExecuteModifyStatement(col.Modify("_id = 12").Unset(null)));

      crudresult = col.Find("pages=22").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with null  of pages for _id=12");
      crudresult = col.Find("reviewers='reviewers12'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with null of pages for _id=12");

      Assert.Throws<MySqlException>(() => ExecuteModifyStatement(col.Modify("_id = 12").Unset("")));

      crudresult = col.Find("pages=22").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with blank  of pages for _id=12");
      crudresult = col.Find("reviewers='reviewers12'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with blank of pages for _id=12");

      //Testcase should have failed when unset is used with blank and space
      Assert.Throws<MySqlException>(() => ExecuteModifyStatement(col.Modify("_id = 12").Unset("")));

      crudresult = col.Find("pages=22").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with blank with space  of pages for _id=12");
      crudresult = col.Find("reviewers='reviewers12'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count, "Count should be 1 after Unset with blank with space  of pages for _id=12");

      //Testcase should have failed when unset is used with blank and space
      Assert.Throws<MySqlException>(() => ExecuteModifyStatement(col.Modify("_id = 12").Unset(new string[] { "", " ", "pages" })));

      crudresult = col.Find("pages=22").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count);
      crudresult = col.Find("reviewers='reviewers12'").Execute().FetchAll();
      Assert.AreEqual(1, crudresult.Count);
    }

    [Test, Description("All Bug Fixes")]
    public void ValidateValuesAfterAppendAndInserts()
    {
      DbDoc document = null;
      Collection collection = CreateCollection("test");
      Result r = collection.Add("{ \"_id\": \"123\", \"email\": [\"alice@ora.com\"], " +
          "\"dates\": \"4/1/2017\" }").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      collection.Modify("true").ArrayAppend("dates", "5/1/2018").Execute();
      document = collection.GetOne("123");
      object[] dates = document["dates"] as object[];
      Assert.AreEqual(2, dates.Length);
      Assert.AreEqual("4/1/2017", dates[0], "Existing Date");
      Assert.AreEqual("5/1/2018", dates[1], "Appended Date");
      collection.Modify("true").ArrayInsert("dates[0]", "5/1/2059").Execute();
      document = collection.GetOne("123");
      dates = document["dates"] as object[];
      Assert.AreEqual("5/1/2059", dates[0], "Inserted  Date");

      collection = CreateCollection("test");
      r = collection.Add("{ \"_id\": \"123\", \"email\": [\"alice@ora.com\"], " +
         "\"dates\": [\"4/1/2017\"] }").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      collection = CreateCollection("test");
      r = collection.Add("{ \"_id\": \"123\", \"email\": [\"alice@ora.com\"], " +
          "\"dates\": \"4/1/2017\" }").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("true").ArrayAppend("dates", "1").Execute();
      collection.Modify("true").ArrayAppend("dates", 1).Execute();
      collection.Modify("true").ArrayAppend("dates", "3.1").Execute();
      collection.Modify("true").ArrayAppend("dates", 3.1).Execute();
      document = collection.GetOne("123");
      dates = document["dates"] as object[];
      Assert.AreEqual(5, dates.Length);
      Assert.AreEqual("4/1/2017", dates[0], "Existing Date");
      Assert.AreEqual("1", dates[1], "Appended Date");
      Assert.AreEqual(1, dates[2], "Appended Date");
      Assert.AreEqual("3.1", dates[3], "Appended Date");
      Assert.AreEqual(3.1, dates[4], "Appended Date");

      collection.Modify("true").ArrayInsert("dates[0]", "10").Execute();
      collection.Modify("true").ArrayInsert("dates[0]", 1000).Execute();
      collection.Modify("true").ArrayInsert("dates[0]", "3.1").Execute();
      collection.Modify("true").ArrayInsert("dates[0]", 22.7).Execute();
      document = collection.GetOne("123");
      dates = document["dates"] as object[];
      Assert.AreEqual("10", dates[3], "Inserted Date");
      Assert.AreEqual(1000, dates[2], "Inserted Date");
      Assert.AreEqual("3.1", dates[1], "Inserted Date");
      Assert.AreEqual(22.7, dates[0], "Inserted Date");

      var d1 = new DbDoc();
      for (int i = 0; i < 30; i++)
      {
        d1.SetValue("_id", i);
        d1.SetValue("books", "test" + i);
        d1.SetValue("pages", i + 10);
        d1.SetValue("reviewers", "reviewers" + i);
        d1.SetValue("person", new
        {
          name = "Fred" + i,
          age = i
        });
        d1.SetValue("1address", "street" + i);
        collection.Add(d1).Execute();
      }
      Assert.Throws<ArgumentException>(() => ExecuteModifyStatement(collection.Modify("_id = 21").Unset("pages*")));

      var docs = new[]
      {
        new {  _id = 100, title = "Book 1", pages = 20 },
        new {  _id = 200, title = "Book 2", pages = 30 },
        new {  _id = 300, title = "Book 3", pages = 40 },
        new {  _id = 400, title = "Book 4", pages = 50 },
      };
      r = collection.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount, "Matching the records affected");
      var test1 = collection.Find("pages = :Pages").Bind("pAges", 90).Fields("{\"_id\":100,\"pages\": 20 }").Execute();
      Assert.IsNotNull(test1);
    }

    [Test, Description("Collection.modify(condition).arrayAppend(CollectionField, ExprOrLiteral)")]
    public void CollectionModifyArrayAppend()
    {

      string currentYear = DateTime.Now.Year.ToString();
      DbDoc document = null;
      string t1 = "{\"_id\": \"1\", \"name\": \"Alice\" }";
      var collection = CreateCollection("test");
      Result r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      object[] expressions1 = new object[] { "YEAR('2000-01-01')", "MONTH('2008-02-03')", "WEEK('2008-02-20')", "DAY('2008-02-20')", "HOUR('10:05:03')",
                "MINUTE('2008-02-03 10:05:03')","SECOND('10:05:03')","MICROSECOND('12:00:00.123456')","QUARTER('2008-04-01')","TIME('2003-12-31 01:02:03')","DATE('2003-12-31 01:02:03')",
                "Year(CURDATE())"};

      object[] expressions2 = new object[] { "5/1/2018",2012,"2012",-22.7,22.7,"22.7", "'large'",
                    "'838:59:59'" ,true,-100000000,"-10000000000","6/6/2018","9999-12-31 23:59:59","0000-00-00 00:00:00","[a,b,c]","[]"};
      object[] compare_expressions1 = new object[] { 2000, 2, 7, 20, 10, 5, 3, 123456, 2, "01:02:03.000000", "2003-12-31", currentYear };
      object[] compare_expressions2 = new object[] { "5/1/2018",2012, "2012", -22.7, 22.7, "22.7",
                     "'large'", "'838:59:59'", true, -100000000, "-10000000000","6/6/2018" ,"9999-12-31 23:59:59","0000-00-00 00:00:00","[a,b,c]","[]"};
      for (int k = 0; k < expressions1.Length; k++)
      {
        collection.Modify("true").ArrayAppend("name", "{ \"dateAndTimeValue\":  " + expressions1[k] + " }").Execute();
      }
      for (int k = 0; k < expressions2.Length; k++)
      {
        collection.Modify("true").ArrayAppend("name", "{ \"dateAndTimeValue\":  \"" + expressions2[k] + "\" }").Execute();
      }
      object[] actors = null;
      object test = null;
      Dictionary<string, object> actor0 = null;
      int l = 1;
      for (int k = 0; k < compare_expressions1.Length; k++)
      {

        document = collection.GetOne("1");
        actors = document["name"] as object[];
        actor0 = actors[l] as Dictionary<string, object>;
        test = actor0["dateAndTimeValue"];
        Assert.AreEqual(compare_expressions1[k].ToString(), test.ToString());
        l++;
      }
      for (int k = 0; k < compare_expressions2.Length; k++)
      {

        document = collection.GetOne("1");
        actors = document["name"] as object[];
        actor0 = actors[l] as Dictionary<string, object>;
        test = actor0["dateAndTimeValue"];
        Assert.AreEqual(compare_expressions2[k].ToString(), test.ToString());
        l++;
      }

      collection = CreateCollection("test");
      r = collection.Add("{ \"_id\": \"123\", \"email\": [\"alice@ora.com\"], " +
         "\"dates\": [\"4/1/2017\"] }").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      var reg_expression = new
      {
        test1 = new MySqlExpression("UPPER($.email)"),
        test2 = new MySqlExpression("LOWER($.email)"),
        test3 = new MySqlExpression("CONCAT('No', 'S', 'QL')"),
        test4 = new MySqlExpression("CHAR(77, 121, 83, 81, '76')"),
        test5 = new MySqlExpression("CONCAT('My', NULL, 'QL')"),
        test6 = new MySqlExpression("ELT(4, 'ej', 'Heja', 'hej', 'foo')"),
        test7 = new MySqlExpression("REPEAT('MySQL', 3)"),
        test8 = new MySqlExpression("REVERSE('abc')"),
        test9 = new MySqlExpression("RIGHT('foobarbar', 4)"),
        test10 = new MySqlExpression("REPLACE('www.mysql.com', 'w', 'Ww')"),
        test11 = new MySqlExpression(" HEX('abc')"),
        test12 = new MySqlExpression(" BIN(12)"),
      };
      object[] compare_expressions = null;
      if (session.Version.isAtLeast(8, 0, 0))
      {
        compare_expressions = new object[] { "[\\\"ALICE@ORA.COM\\\"]", "[\\\"alice@ora.com\\\", \\\"[\\\\\\\"alice@ora.com\\\\\\\"]\\\"]",
                    "NoSQL","base64:type253:TXlTUUw=",null,"foo","MySQLMySQLMySQL","cba","rbar","WwWwWw.mysql.com","616263","1100" };
      }
      else
      {
        compare_expressions = new object[] { "[\\\"ALICE@ORA.COM\\\"]", "[\\\"alice@ora.com\\\", \\\"[\\\\\\\"alice@ora.com\\\\\\\"]\\\"]",
                    "NoSQL","base64:type15:TXlTUUw=",null,"foo","MySQLMySQLMySQL","cba","rbar","WwWwWw.mysql.com","616263","1100" };
      }

      var items = new List<object>();
      items.Add(reg_expression.test1);
      items.Add(reg_expression.test2);
      items.Add(reg_expression.test3);
      items.Add(reg_expression.test4);
      items.Add(reg_expression.test5);
      items.Add(reg_expression.test6);
      items.Add(reg_expression.test7);
      items.Add(reg_expression.test8);
      items.Add(reg_expression.test9);
      items.Add(reg_expression.test10);
      items.Add(reg_expression.test11);
      items.Add(reg_expression.test12);
      int m = 1, n = 0;
      foreach (var obj in items)
      {
        collection.Modify("true").ArrayAppend("email", obj).Execute();
        document = collection.GetOne("123");
        actors = document["email"] as object[];
        if (n == 3)
        { }
        else
        {
          Assert.AreEqual(actors[m], compare_expressions[n]);
        }

        m++; n++;
      }

      string json = "";
      int i = 0, j = 0, maxField = 40;
      collection = CreateCollection("test");
      int maxDepth = 2;
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

      collection.Add(json).Execute();
      r = collection.Modify("true").ArrayAppend("ARR10", 1).ArrayAppend("ARR20", 2).ArrayAppend("ARR30", 3).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Modify("true").ArrayAppend("ARR0", null).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Modify("true").ArrayAppend("ARR39", null).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

    }

    [Test, Description("MySQLX CNET Forbid modify() with no condition-Scenario-1")]
    public void ForbidModifyWithNoCondition_S1()
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

      // Condition can't be null or empty.
      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify(string.Empty)));
      Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove("")));
      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify(null)));

      // Sending an expression that evaluates to true applies changes on all documents.
      result = collection.Modify("true").Set("pages", "10").Execute();
      Assert.AreEqual(4, result.AffectedItemsCount);

    }

    [Test, Description("MySQLX CNET Forbid modify() with no condition-Scenario-2")]
    public void ForbidModifyWithNoCondition_S2()
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

      // Sending an expression that evaluates to true applies changes on all documents.
      //Deprecated Modify().Where() in 8.0.17
      result = collection.Modify("true").Where("false").Set("pages", "10").Execute();
      Assert.AreEqual(0, result.AffectedItemsCount);

      result = collection.Modify("true").Where("true").Set("pages", "10").Execute();
      Assert.AreEqual(4, result.AffectedItemsCount);
      result = collection.Modify("false").Where("true").Set("pages", "40").Execute();
      Assert.AreEqual(4, result.AffectedItemsCount);
      result = collection.Modify("false").Where("false").Set("pages", "40").Execute();
      Assert.AreEqual(0, result.AffectedItemsCount);

      // Condition can't be null or empty.
      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify(" ")));
    }

    [Test, Description("Test valid modify.patch to change element at Depth n for multiple arrays#Bug))")]
    public void ModifyPatchNDepth()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      string json = "";
      int i = 0, j = 0, maxField = 100;
      var collection = CreateCollection("test");
      int maxDepth = 46;
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

      var r = collection.Modify("age = :age").Patch(json).
          Bind("age", "18").Execute();
      Assert.IsNotNull(r);
    }

    [Test, Description("Test valid modify.patch with condition/limit/OrderBy")]
    public void ModifyPatch()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = 12},
        new {_id = 2, title = "Book 2", pages = 30,age = 18},
        new {_id = 3, title = "Book 3", pages = 40,age = 34},
        new {_id = 4, title = "Book 4", pages = 50,age = 15}
      };
      var r = collection.Add(docs).Execute();
      Assert.AreEqual(4, (int)r.AffectedItemsCount, "Matching the updated record count");

      var jsonParams = new { title = "Book 100" };
      var foundDocs = collection.Modify("age==18").Patch(jsonParams).Execute();
      Assert.AreEqual(1, (int)foundDocs.AffectedItemsCount, "Matching the record count");

      var document = collection.GetOne("2");
      Assert.AreEqual("Book 100", document["title"]);

      jsonParams = new { title = "Book 300" };
      r = collection.Modify("age<18").Patch(jsonParams).Limit(1).Execute();
      Assert.AreEqual(1, (int)r.AffectedItemsCount, "Matching the record count");

      document = collection.GetOne(1);
      Assert.AreEqual("Book 300", document["title"]);

      var jsonParams1 = new { title = "Book 10", pages = 1000 };
      r = collection.Modify("age>30").Patch(jsonParams1).Sort("age ASC").Execute();
      Assert.AreEqual(1, (int)r.AffectedItemsCount, "Matching the record count");
    }

    [Test, Description("Test valid modify.patch with set/unset")]
    public void ModifyPatchWithSetUnset()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = "12"},
        new {_id = 2, title = "Book 2", pages = 30,age = "18"},
        new {_id = 3, title = "Book 3", pages = 40,age = "34"},
        new {_id = 4, title = "Book 4", pages = 50,age = "12"}
      };
      Result r = collection.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);

      var jsonParams = new { title = "Book 500" };
      r = collection.Modify("age = :age").Patch(jsonParams).Bind("age", "18").
          Set("pages", "5000").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      var document = collection.GetOne("2");
      Assert.AreEqual("5000", document["pages"].ToString());
      Assert.AreEqual("Book 500", document["title"].ToString());

      var jsonParams1 = new { title = "Book 50000", pages = 5000 };
      r = collection.Modify("age = :age").Patch(jsonParams1).Bind("age", "18").
          Unset("pages").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount, "Match being done");
      document = collection.GetOne("2");
      DbDoc test = null;
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["pages"]);
      Assert.AreEqual("Book 50000", document["title"]);
    }

    [Test, Description("Test invalid modify.patch to attempt to change _id using modify.patch")]
    public void ModifyPatchChangeId()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = 12},
        new {_id = 2, title = "Book 2", pages = 30,age = 18},
        new {_id = 3, title = "Book 3", pages = 40,age = 34},
        new {_id = 4, title = "Book 4", pages = 50,age = 12}
      };
      Result r = collection.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);
      var document = collection.GetOne("1");
      var jsonParams = new { _id = 123 };

      r = collection.Modify("age = :age").Patch(jsonParams).
          Bind("age", 18).Execute();
      Assert.AreEqual(0, r.AffectedItemsCount);

      var jsonParams2 = new { _id = 123, title = "Book 4000" };

      r = collection.Modify("age = :age").Patch(jsonParams2).
          Bind("age", 18).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      string jsonParams1 = "{ \"_id\": \"123\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", 18).Execute();
      Assert.AreEqual(0, r.AffectedItemsCount);

      jsonParams1 = "{ \"_id\": \"123\",\"title\": \"Book 400\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", 18).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
    }

    [Test, Description("Test modify.patch where the key to be modified has array, dbDoc and normal constant value and condition is matched for all.")]
    public void ModifyPatchKeyWithArray()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs1 = new[]
      {
         new {_id = 1, title = "Book 1", pages = 20, age = 12,name = "Morgan"},
      };
      var docs2 =
          "{\"_id\": \"2\",\"age\": 12,\"name\": \"Alice\", " +
          "\"address\": {\"zip\": \"12345\", \"city\": \"Los Angeles\", \"street\": \"32 Main str\"}}";

      var docs3 = "{\"_id\": \"3\", \"age\": 12,\"name\":[\"Cynthia\"], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      Result r = collection.Add(docs1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(docs2).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(docs3).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      var jsonParams = new { name = "Changed" };
      r = collection.Modify("age = :age").Patch(jsonParams).Bind("age", 12).Execute();
      Assert.AreEqual(3, r.AffectedItemsCount);
    }

    [Test, Description("Test that documents not matching conditions are not modified.")]
    public void ModifyPatchNotMatchingConditions()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = 12},
        new {_id = 2, title = "Book 2", pages = 30,age = 18},
        new {_id = 3, title = "Book 3", pages = 40,age = 34},
        new {_id = 4, title = "Book 4", pages = 50,age = 12}
      };
      Result r = collection.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);
      var document = collection.GetOne("1");
      var jsonParams = new { title = "Book 100" };
      r = collection.Modify("age = :age").Patch(jsonParams).Bind("age", "19").Execute();
      Assert.AreEqual(0, r.AffectedItemsCount);
      string jsonParams1 = "{ \"title\": \"Book 100\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).Bind("age", "28").Execute();
      Assert.AreEqual(0, r.AffectedItemsCount);
      jsonParams1 = "{ \"unknownvalues\": null}";
      r = collection.Modify("age = :age").Patch(jsonParams1).Bind("age", "28").Execute();
      Assert.AreEqual(0, r.AffectedItemsCount);
    }

    [Test, Description("Test modify.patch with different types of records(anonymous object,Json String,DbDoc) with same key and try to replace using a patch")]
    public void ModifyPatchDifferentTypesSameKey()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs1 = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = 12,name = "Morgan"},
      };
      var docs2 =
          "{\"_id\": \"2\",\"age\": \"12\",\"name\": \"Alice\", " +
          "\"address\": {\"zip\": \"12345\", \"city\": \"Los Angeles\", \"street\": \"32 Main str\"}}";

      var docs3 = new DbDoc(@"{ ""_id"": 3, ""pages"": 20, ""age"":12,""name"":""Cynthiaa"",
                          ""books"": [
                            {""_id"" : 10, ""title"" : ""Book 10""},
                            { ""_id"" : 20, ""title"" : ""Book 20"" }
                          ]
                      }");
      Result r = collection.Add(docs1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(docs2).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(docs3).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      var jsonParams = new { name = "Changed" };
      r = collection.Modify("age = :age").Patch(jsonParams).
              Bind("age", 12).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);//docs2 age is the string
    }

    [Test, Description("GetOne with ExistingID and NewID with doc(Verify the Immutable feature also))")]
    public void GetOneAndRemoveOne()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      var coll = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20},
        new {_id = 2, title = "Book 2", pages = 30},
        new {_id = 3, title = "Book 3", pages = 40},
        new {_id = 4, title = "Book 4", pages = 50}
      };
      var r = coll.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => coll.GetOne(null));
      Assert.Throws<ArgumentNullException>(() => coll.GetOne(""));
      Assert.Throws<ArgumentNullException>(() => coll.GetOne(string.Empty));
      Assert.Throws<ArgumentNullException>(() => coll.GetOne(" "));

      // Get document using numeric parameter.
      var document = coll.GetOne(1);
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
      Assert.AreEqual(null, document);

      coll.Add(new { _id = 5, title = "Book 5", pages = 60 }).Execute();
      Assert.AreEqual(5, coll.Find().Execute().FetchAll().Count);
      // Remove sending numeric parameter.
      //WL11843-Core API v1 alignment Changes
      Assert.AreEqual(1, coll.RemoveOne(1).AffectedItemsCount);
      Assert.AreEqual(4, coll.Find().Execute().FetchAll().Count);

      // Remove sending string parameter.
      Assert.AreEqual(1, coll.RemoveOne("3").AffectedItemsCount);
      Assert.AreEqual(3, coll.Find().Execute().FetchAll().Count);

      // Remove an auto-generated id.
      document = coll.Find("pages = 60").Execute().FetchOne();
      Assert.AreEqual(1, coll.RemoveOne(document.Id).AffectedItemsCount);
      Assert.AreEqual(2, coll.Find().Execute().FetchAll().Count);

      // Remove a non-existing document.
      Assert.AreEqual(0, coll.RemoveOne(5).AffectedItemsCount);
      Assert.AreEqual(2, coll.Find().Execute().FetchAll().Count);

    }

    [Test, Description("AddReplaceOne with unique id generated by SQL")]
    public void AddReplaceOneUniqueId()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, name = "foo"},
        new {_id = 2, name = "bar"}
      };
      var result = collection.Add(docs).Execute();
      Assert.AreEqual(2, result.AffectedItemsCount);

      // Add unique index.
      session.SQL(
              "ALTER TABLE test.test ADD COLUMN name VARCHAR(3) GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(doc, '$.name'))) VIRTUAL UNIQUE KEY NOT NULL")
          .Execute();
      Assert.Throws<MySqlException>(() => collection.AddOrReplaceOne(1, new { name = "bar" }));
      Assert.Throws<MySqlException>(() => collection.AddOrReplaceOne(1, new { _id = 3, name = "bar", age = "55" }));
    }

    #endregion WL14389

  }
}
