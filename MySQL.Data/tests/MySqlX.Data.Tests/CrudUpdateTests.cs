// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.//
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
using MySqlX.Serialization;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using System.Collections.Generic;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class CrudUpdateTests : BaseTest
  {
    [Fact]
    public void SetItemInSingleDocument()
    {
      Collection coll = CreateCollection("test");
      Result result = coll.Add(new { _id = 1, name = "Book 1" }).Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);

      // Set integer value.
      result = coll.Modify("_id = 1").Set("pages", 20).Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      Assert.Equal(20, coll.GetOne(1)["pages"]);

      // Set null value.
      result = coll.Modify("_id = 1").Set("pages", null).Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      Assert.Equal(null, coll.GetOne(1)["pages"]);

      // Set existing field.
      result = coll.Modify("_id = 1").Set("name", "Book 2").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      Assert.Equal("Book 2", coll.GetOne(1)["name"]);

      // Set alphanumeric field.
      var document = new DbDoc();
      document.SetValue("_id", 2);
      document.SetValue("1a", "other");
      result = coll.Add(document).Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      var insertedDocument = coll.GetOne(2);

      //result = coll.Modify("_id = 1").Set("1a", "other").Execute();
    }

    [Fact]
    public void ChangeItemInSingleDocument()
    {
      Collection coll = CreateCollection("test");
      Result result = coll.Add(new { _id = 1, name = "Book 1", pages = 20 }).Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);

      result = coll.Modify("_id = 1").Change("name", "Book 2").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
    }

    [Fact]
    public void RemoveItemInSingleDocumentUsingUnset()
    {
      Collection coll = CreateCollection("test");
      Result result = coll
                .Add(new { _id = 1, name = "Book 1", pages = 20 })
                .Add(new { _id = 2, name = "Book 2", pages = 30 })
                .Add(new { _id = 3, name = "Book 3", pages = 40, author = "John", author2 = "Mary" })
                .Execute();
      Assert.Equal<ulong>(3, result.AffectedItemsCount);

      // Unset 1 field.
      result = coll.Modify("_id = 1").Unset("pages").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      var document = coll.Find("_id = 1").Execute().FetchOne();
      Assert.Equal(2, document.values.Count);

      // Unset multiple fields.
      result = coll.Modify("_id = 2").Unset("name", "pages").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      document = coll.Find("_id = 2").Execute().FetchOne();
      Assert.Equal(1, document.values.Count);
      result = coll.Modify("_id = 3").Unset(null, "author", "author2").Execute();
      document = coll.Find("_id = 3").Execute().FetchOne();
      Assert.Equal(3, document.values.Count);

      // Unsetting nonexistent fields doesn't raise an error.
      result = coll.Modify("_id = 2").Unset("otherfield").Execute();
      Assert.Equal(0ul, result.AffectedItemsCount);

      // Unsetting null items combined with valid values are ignored.
      result = coll.Modify("_id = 3").Unset(null).Unset("name").Execute();
      Assert.Equal(1ul, result.AffectedItemsCount);
      document = coll.Find("_id = 3").Execute().FetchOne();
      Assert.Equal(2, document.values.Count);

      // Unsetting single null items raises an error
      var ex = Assert.Throws<MySqlException>(() => coll.Modify("_id = 3").Unset(null).Execute());
      Assert.Equal("Invalid update expression list", ex.Message);

      // Unsetting empty strings raises an error.
      ex = Assert.Throws<MySqlException>(() => coll.Modify("_id = 2").Unset("").Execute());
      Assert.Equal("The path expression '$' is not allowed in this context.", ex.Message);
      ex = Assert.Throws<MySqlException>(() => coll.Modify("_id = 2").Unset(string.Empty).Execute());
      Assert.Equal("The path expression '$' is not allowed in this context.", ex.Message);

      // Unset with special chars.
      var ex2 = Assert.Throws<ArgumentException>(() => coll.Modify("_id = 3").Unset(null).Unset("@*%#ç").Execute());
      Assert.Equal("The path expression '$' is not allowed in this context.", ex.Message);
      ex2 = Assert.Throws<ArgumentException>(() => coll.Modify("_id = 3").Unset(null).Unset("******").Execute());
      Assert.Equal("The path expression '$' is not allowed in this context.", ex.Message);
    }

    [Fact]
    public void SetItemAndBind()
    {
      Collection coll = CreateCollection("test");
      Result result = coll.Add(new { _id = 1, name = "Book 1" })
        .Add(new { _id = 2, name = "Book 2" }).Execute();
      Assert.Equal<ulong>(2, result.AffectedItemsCount);

      var stmt = coll.Modify("_id = :ID");
      result = stmt.Bind("Id", 2).Set("pages", "20").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      result = stmt.Bind("Id", 1).Set("pages", "10").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);

      var docs = coll.Find().Execute().FetchAll();
      Assert.Equal(new DbDoc("{ \"_id\": 1, \"name\": \"Book 1\", \"pages\": 10 }").ToString(), docs[0].ToString());
      Assert.Equal(new DbDoc("{ \"_id\": 2, \"name\": \"Book 2\", \"pages\": 20 }").ToString(), docs[1].ToString());
    }

    [Fact]
    public void ModifyAll()
    {
      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(2, result.AffectedItemsCount);

      // Condition can't be null or empty.
      string errorMessage = "Parameter can't be null or empty.\r\nParameter name: condition";
      Exception ex = Assert.Throws<ArgumentNullException>(() => collection.Modify(string.Empty).Execute());
      Assert.Equal(ex.Message,errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => collection.Modify("").Execute());
      Assert.Equal(ex.Message,errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => collection.Modify(" ").Execute());
      Assert.Equal(ex.Message,errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => collection.Modify("   ").Execute());
      Assert.Equal(ex.Message,errorMessage);
      ex = Assert.Throws<ArgumentNullException>(() => collection.Modify(null).Execute());
      Assert.Equal(ex.Message,errorMessage);

      // Sending an expression that evaluates to true applies changes on all documents.
      result = collection.Modify("true").Set("pages","10").Execute();
      Assert.Equal<ulong>(2, result.AffectedItemsCount);
    }

    [Fact]
    public void ModifyWithLimit()
    {
      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new {  _id = 1, title = "Book 1", pages = 20 },
        new {  _id = 2, title = "Book 2", pages = 30 },
      };
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(2, result.AffectedItemsCount);

      collection.Modify("true").Set("title", "Book X").Limit(1).Execute();
      Assert.Equal(1, collection.Find("title = \"Book X\"").Execute().FetchAll().Count);

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => collection.Modify("true").Set("pages", 10).Limit(0).Execute());
      Assert.Throws<ArgumentOutOfRangeException>(() => collection.Modify("true").Set("pages", 10).Limit(-10).Execute());
    }

    [Fact]
    public void ModifyWithInOperator()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new DbDoc("{ \"a\": 1, \"b\": \"foo\", \"c\": { \"d\": true, \"e\": [1,2,3] }, \"f\": [ {\"x\":5}, {\"x\":7 } ] }"),
        new DbDoc("{ \"a\": 2, \"b\": \"foo2\", \"c\": { \"d\": true, \"e\": [4,5,6] }, \"f\": [ {\"x\":5}, {\"x\":8 } ] }"),
        new DbDoc("{ \"a\": 1, \"b\": \"foo3\", \"c\": { \"d\": true, \"e\": [1,4,3] }, \"f\": [ {\"x\":6}, {\"x\":9 } ] }"),
      };
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(3, result.AffectedItemsCount);

      Assert.Equal<ulong>(3, collection.Modify("a IN (1,2)").Set("a", 3).Execute().AffectedItemsCount);
      Assert.Equal(3, collection.Find().Where("a = 3").Execute().FetchAll().Count);

      Assert.Equal<ulong>(3, collection.Modify("a IN [3]").Set("a", 1).Execute().AffectedItemsCount);
      Assert.Equal(3, collection.Find().Where("a = 1").Execute().FetchAll().Count);

      Assert.Equal<ulong>(2, collection.Modify("1 IN c.e").Set("c.e", "newValue").Execute().AffectedItemsCount);
      Assert.Equal(2, collection.Find().Where("c.e = \"newValue\"").Execute().FetchAll().Count);
    }

    [Fact]
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
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(4, result.AffectedItemsCount);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne(null, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne("", docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne(string.Empty, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.ReplaceOne("1", null));

      // Replace using a numeric identifier.
      Assert.Equal<ulong>(1, collection.ReplaceOne(1, docs[1]).AffectedItemsCount);
      DbDoc document = collection.GetOne(1);
      Assert.Equal(1, Convert.ToInt32(document.Id));
      Assert.Equal("Book 2", document["title"]);
      Assert.Equal(30, Convert.ToInt32(document["pages"]));

      // Replace using a string identifier.
      Assert.Equal<ulong>(1, collection.ReplaceOne("2", new DbDoc("{ \"name\": \"John\", \"lastName\": \"Smith\" }")).AffectedItemsCount);
      document = collection.GetOne(2);
      Assert.Equal(2, Convert.ToInt32(document.Id));
      Assert.Equal("John", document["name"]);
      Assert.Equal("Smith", document["lastName"]);

      // Replace a non-existing document.
      Assert.Equal<ulong>(0, collection.ReplaceOne(5, docs[1]).AffectedItemsCount);
      Assert.True(collection.GetOne(5) == null);
    }

    [Fact]
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
      Assert.Equal<ulong>(4, collection.Add(docs).Execute().AffectedItemsCount);

      DbDoc d_new = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,""title"" : ""Book 1"", ""person"": { ""name"": ""Fred"", ""age"": 45 ,""State"" : ""Ohio""} }");
      Assert.Equal<ulong>(1, collection.ReplaceOne(1, d_new).AffectedItemsCount);
      DbDoc document = collection.GetOne(1);
      Assert.Equal("Ohio", (document.values["person"] as Dictionary<string,object>)["State"]);

      d_new = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,""title"" : ""Book 1"", ""person"": { ""name"": ""Fred"", ""age"": 45 ,""State"" : ""Ohio"", ""newProp"": { ""a"":33 } } }");
      Assert.Equal<ulong>(1, collection.ReplaceOne(1, d_new).AffectedItemsCount);
      document = collection.GetOne(1);
      Assert.Equal(33, ((document.values["person"] as Dictionary<string,object>)["newProp"] as Dictionary<string,object>)["a"] );
    }

    [Fact]
    public void ArrayInsert()
    {
      Collection collection = CreateCollection("test");
      collection.Add("{ \"x\":[1,2] }").Execute();

      // x[1]=43, x[2]=2. 
      collection.Modify("true").ArrayInsert("x[1]", 43).Execute();

      // x[3]=44.
      collection.Modify("true").ArrayInsert("x[3]", 44).Execute();

      // Since array only contains 4 items the value 46 is assigned to x[4].
      collection.Modify("true").ArrayInsert("x[5]", 46).Execute();

      // Since array only contains 5 items the value 50 is assigned to x[5].
      collection.Modify("true").ArrayInsert("x[20]", 50).Execute();

      // Assign an item from different data type.
      collection.Modify("true").ArrayInsert("x[6]", "string").Execute();

      // Assign a document.
      collection.Modify("true").ArrayInsert("x[7]", "{ \"name\":\"Mike\" }").Execute();

      var result = collection.Find().Execute();
      var document = result.FetchOne();
      var x = (object[])document.values["x"];

      Assert.Equal(8, x.Length);
      Assert.Equal(1, (int)x[0]);
      Assert.Equal(43, (int)x[1]);
      Assert.Equal(2, (int)x[2]);
      Assert.Equal(44, (int)x[3]);
      Assert.Equal(46, (int)x[4]);
      Assert.Equal(50, (int)x[5]);
      Assert.Equal("string", x[6]);
      Assert.True(new DbDoc(x[7]) is DbDoc);

      // No value is inserted if the array doesn't exist.
      collection.Modify("true").ArrayInsert("y[0]", 1).Execute();

      result = collection.Find().Execute();
      document = result.FetchOne();
      Assert.False(document.values.ContainsKey("y"));

      collection.Modify("true").ArrayInsert("x[0]", null).Execute();
      collection.Modify("true").ArrayInsert("x[1]", " ").Execute();

      result = collection.Find().Execute();
      document = result.FetchOne();
      x = (object[])document.values["x"];
      Assert.Equal(null, x[0]);
      Assert.Equal(" ", x[1]);

      // Insert an empty string fails
      var ex = Assert.Throws<ArgumentException>(() => collection.Modify("true").ArrayInsert("x[0]", "").Execute());
      Assert.Contains("String can't be empty.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => collection.Modify("true").ArrayInsert("x[0]", string.Empty).Execute());
      Assert.Contains("String can't be empty.", ex.Message);
    }

    [Fact]
    public void ArrayAppendWithMySqlExpression()
    {
      Collection collection = CreateCollection("test");

      // Use inline expression.
      collection.Add("{ \"_id\":\"123\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }").Execute();
      collection.Modify("true").ArrayAppend("email", "UPPER($.name)").Execute();
      var document = collection.GetOne("123");
      Assert.Equal("ALICE", (document["email"] as object[])[1]);

      // Use MySqlExpression.
      collection.Add("{ \"_id\":\"124\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }").Execute();
      collection.Modify("_id = \"124\"").ArrayAppend("email", new MySqlExpression("UPPER($.name)")).Execute();
      document = collection.GetOne("124");
      Assert.Equal("ALICE", (document["email"] as object[])[1]);

      // Use embedded MySqlExpression.
      collection.Add("{ \"_id\":\"125\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }").Execute();
      collection.Modify("_id = \"125\"").ArrayAppend("email", new { other = new MySqlExpression("UPPER($.name)") }).Execute();
      document = collection.GetOne("125");
      var item = ((document["email"] as object[])[1] as Dictionary<string, object>);
      Assert.Equal("ALICE", item["other"]);
    }

    [Fact]
    public void ArrayAppendUsesCorrectDataTypes()
    {
      Collection collection = CreateCollection("test");
      collection.Add("{ \"_id\":\"123\", \"email\":[ \"alice@ora.com\"], \"dates\":\"4/1/2017\" }").Execute();
      collection.Modify("true").ArrayAppend("dates", "\"1\"").Execute();
      collection.Modify("true").ArrayAppend("dates", 1).Execute();
      var document = collection.GetOne("123");
      var dates = document["dates"] as object[];
      Assert.True(dates[1] is string);
      Assert.True(dates[2] is int);
    }

    [Fact]
    public void ArrayAppend()
    {
      Collection collection = CreateCollection("test");
      collection.Add("{ \"x\":[1,2] }").Execute();

      // Append values of different types, null and spaces.
      collection.Modify("true").ArrayAppend("x", 43).Execute();
      collection.Modify("true").ArrayAppend("x", "string").Execute();
      collection.Modify("true").ArrayAppend("x", true).Execute();
      collection.Modify("true").ArrayAppend("x", null).Execute();
      collection.Modify("true").ArrayAppend("x", " ").Execute();

      DocResult result = collection.Find().Execute();
      DbDoc document = result.FetchOne();
      var x = (object[]) document.values["x"];

      Assert.Equal(7, x.Length);
      Assert.Equal(1, (int) x[0]);
      Assert.Equal(2, (int) x[1]);
      Assert.Equal(43, (int) x[2]);
      Assert.Equal("string", x[3]);
      Assert.Equal(true, x[4]);
      Assert.Equal(null, x[5]);
      Assert.Equal(" ", x[6]);

      // No value is appended if the array doesn't exist.
      collection.Modify("true").ArrayAppend("y", 45).Execute();

      result = collection.Find().Execute();
      document = result.FetchOne();
      Assert.False(document.values.ContainsKey("y"));

      var ex = Assert.Throws<ArgumentException>(() => collection.Modify("true").ArrayAppend("x", "").Execute());
      Assert.Contains("String can't be empty.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => collection.Modify("true").ArrayAppend("x", string.Empty).Execute());
      Assert.Contains("String can't be empty.", ex.Message);
    }

    [Fact]
    public void ArrayInsertWithMySqlExpression()
    {
      Collection collection = CreateCollection("test");

      // Use inline expression.
      collection.Add("{ \"_id\":\"123\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }").Execute();
      collection.Modify("true").ArrayInsert("email[0]", "UPPER($.name)").Execute();
      var document = collection.GetOne("123");
      Assert.Equal("ALICE", (document["email"] as object[])[0]);

      // Use MySqlExpression.
      collection.Add("{ \"_id\":\"124\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }").Execute();
      collection.Modify("_id = \"124\"").ArrayInsert("email[0]", new MySqlExpression("UPPER($.name)")).Execute();
      document = collection.GetOne("124");
      Assert.Equal("ALICE", (document["email"] as object[])[0]);

      // Use embedded MySqlExpression.
      collection.Add("{ \"_id\":\"125\", \"name\":\"alice\", \"email\":[ \"alice@ora.com\" ], \"dates\":\"4/1/2017\" }").Execute();
      collection.Modify("_id = \"125\"").ArrayInsert("email[0]", new { other = new MySqlExpression("UPPER($.name)") }).Execute();
      document = collection.GetOne("125");
      var item = ((document["email"] as object[])[0] as Dictionary<string, object>);
      Assert.Equal("ALICE", item["other"]);
    }
  }
}
