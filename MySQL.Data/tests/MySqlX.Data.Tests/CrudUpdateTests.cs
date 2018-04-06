// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.Serialization;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
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

      result = coll.Modify("_id = 1").Set("pages", "20").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
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
    public void RemoveItemInSingleDocument()
    {
      Collection coll = CreateCollection("test");
      Result result = coll.Add(new { _id = 1, name = "Book 1", pages = 20 }).Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);

      result = coll.Modify("_id = 1").Unset("pages").Execute();
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
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
  }
}
