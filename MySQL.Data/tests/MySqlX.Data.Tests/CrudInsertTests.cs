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

using MySql.Data.MySqlClient;
using MySqlX.Common;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using System;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class CrudInsertTests : BaseTest
  {
    [Fact]
    public void InsertSingleDbDocWithId()
    {
      Collection coll = CreateCollection("test");
      Result r = coll.Add(@"{ ""_id"": 1, ""foo"": 1 }").Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      Assert.Equal(1, coll.Count());
    }

    [Fact]
    public void InsertSingleDbDocWithoutId()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add("{ \"foo\": 1 }");
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.Equal(5115u, Assert.ThrowsAny<MySqlException>(() => stmt.Execute()).Code);
        return;
      }
      Result r = stmt.Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      Assert.Equal(1, coll.Count());
      Assert.Equal(1, r.GeneratedIds.Count);
      Assert.False(string.IsNullOrWhiteSpace(r.GeneratedIds[0]));
    }

    [Fact]
    public void InsertMultipleDbDocWithoutId()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add("{ \"foo\": 1 }")
        .Add("{ \"amber\": 2 }")
        .Add("{ \"any\": 3 }");
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.Equal(5115u, Assert.ThrowsAny<MySqlException>(() => stmt.Execute()).Code);
        return;
      }
      Result r = stmt.Execute();
      Assert.Equal<ulong>(3, r.RecordsAffected);
      Assert.Equal(3, coll.Count());
      Assert.Equal(3, r.GeneratedIds.Count);
    }

    [Fact]
    public void InsertAnonymousObjectWithId()
    {
      var obj = new { _id = "5", name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      Result r = coll.Add(obj).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      //TODO:  pull object and verify data
      Assert.Equal(1, coll.Count());
    }

    [Fact]
    public void InsertAnonymousObjectWithNoId()
    {
      var obj = new { name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      var stmt = coll.Add(obj);
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.Equal(5115u, Assert.ThrowsAny<MySqlException>(() => stmt.Execute()).Code);
        return;
      }
      Result r = stmt.Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      //TODO:  pull object and verify data
      Assert.Equal(1, coll.Count());
      Assert.Equal(1, r.GeneratedIds.Count);
      Assert.False(string.IsNullOrWhiteSpace(r.GeneratedIds[0]));
    }

    [Fact]
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
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.RecordsAffected);
      Assert.Equal(4, coll.Count());
    }

    [Fact]
    public void ValidatesDocumentIds()
    {
      Collection coll = CreateCollection("test");
      var stmt = coll.Add(new { name = "Book 1" });
      if (!session.Version.isAtLeast(8, 0, 5))
      {
        // Code 5115 Document is missing a required field
        Assert.Equal(5115u, Assert.ThrowsAny<MySqlException>(() => stmt.Execute()).Code);
        return;
      }
      Result result = stmt.Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      result = coll.Modify($"_id = '{result.GeneratedIds[0]}'").Set("pages", "20").Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);
      Assert.Equal(0, result.GeneratedIds.Count);
    }

    [Fact]
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
      stmt.Execute();
      foreach (var doc in docs)
      {
        Result r = stmt.Add(doc).Execute();
        Assert.Equal<ulong>(1, r.RecordsAffected);
      }
      Assert.Equal(5, coll.Count());
    }

    [Fact]
    public void EmptyDocArray()
    {
      Collection coll = CreateCollection("test");

      var insertResult = coll.Add(new DbDoc[] { }).Execute();
      Assert.Equal(0ul, insertResult.RecordsAffected);

      var result = coll.Find().Execute().FetchAll();
      Assert.Equal(0, result.Count);
    }

    [Fact]
    public void NullParameter()
    {
      Collection coll = CreateCollection("test");

      Assert.Throws<ArgumentNullException>(() => coll.Add(null).Execute());
    }

    [Fact]
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
      coll.Add(d2).Execute();
      var result = coll.Find().Execute().FetchAll();
      Assert.Equal(1, result.Count);
      Assert.Equal(d2.ToString(), result[0].ToString());
    }

    [Fact]
    public void CompareGuids()
    {
      Guid guid1 = new Guid();
      Guid guid2 = new Guid();
      Assert.Equal(0, Tools.CompareGuids(guid1, guid2));
      Assert.Equal(0, Tools.CompareGuids(guid1.ToString(), guid2.ToString()));

      guid1 = Guid.NewGuid();
      guid2 = Guid.NewGuid();
      Assert.True(Tools.CompareGuids(guid1, guid2) != 0);
      Assert.True(Tools.CompareGuids(guid1.ToString(), guid2.ToString()) != 0);
    }

    [Fact]
    public void InsertNullValuesAsDbDoc()
    {
      Collection collection = CreateCollection("test");

      var nullValues = new String[] { null, "null", "NULL" };
      var docs = new DbDoc[3];
      for (int i = 0; i < docs.Length; i++)
      {
        docs[i] = new DbDoc();
        docs[i].SetValue("a", nullValues[i]);
        docs[i].SetValue("_id", (i+1));
      }

      collection.Add(docs).Execute();
      var result = collection.Find().Execute().FetchAll();
      Assert.Equal(docs.Length, result.Count);

      for (int i = 0; i < docs.Length; i++)
        Assert.Equal(docs[i].ToString(), result[i].ToString());
    }

    [Fact]
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
      collection.Add(docs).Execute();
      var result = collection.Find().Execute().FetchAll();
      Assert.Equal(docs.Length, result.Count);
      for (int i=0; i < docs.Length; i++)
      {
        var currentDoc = new DbDoc(docs[i]);
        var resultingDoc = new DbDoc(result[i]);
        Assert.Equal(currentDoc.Id, resultingDoc.Id);
        Assert.Equal(currentDoc["foo"], resultingDoc["foo"]);
      }
    }

    [Fact]
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
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(4, result.RecordsAffected);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne(null, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne("", docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne(string.Empty, docs[1]));
      Assert.Throws<ArgumentNullException>(() => collection.AddOrReplaceOne("1", null));

      // Add a document.
      Assert.Equal<ulong>(1, collection.AddOrReplaceOne(5, new { _id = 5, title = "Book 5", pages = 60 }).RecordsAffected);
      Assert.True(collection.GetOne(5) != null);

      Assert.Equal<ulong>(1, collection.AddOrReplaceOne("6", new { title = "Book 6", pages = 70 }).RecordsAffected);
      Assert.True(collection.GetOne(6) == null);
      Assert.True(collection.GetOne("6") != null);

      // Replace a document.
      Assert.Equal<ulong>(2, collection.AddOrReplaceOne(1, new { _id = 1, title = "Book X", pages = 10 }).RecordsAffected);
      DbDoc document = collection.GetOne(1);
      Assert.Equal(1, Convert.ToInt32(document.Id));
      Assert.Equal("Book X", document["title"]);
      Assert.Equal(10, Convert.ToInt32(document["pages"]));

      Assert.Equal<ulong>(2, collection.AddOrReplaceOne(1, new { title = "Book Y", pages = 9, other = "value" }).RecordsAffected);
      document = collection.GetOne(1);
      Assert.Equal(1, Convert.ToInt32(document.Id));
      Assert.Equal("Book Y", document["title"]);
      Assert.Equal(9, Convert.ToInt32(document["pages"]));
      Assert.Equal("value", document["other"]);
    }
  }
}
