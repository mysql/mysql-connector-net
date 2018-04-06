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

using System;
using MySqlX.XDevAPI;
using Xunit;
using MySqlX.XDevAPI.Common;

namespace MySqlX.Data.Tests
{
  public class CrudRemoveTests : BaseTest
  {
    [Fact]
    public void RemoveSingleDocumentById()
    {
      Collection coll = CreateCollection("test");
      var docs = new { _id = 12, title = "Book 1", pages = 20 };
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(1, r.AffectedItemsCount);

      r = coll.Remove(12).Execute();
      Assert.Equal<ulong>(1, r.AffectedItemsCount);
    }

    [Fact]
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
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = coll.Remove("pages > 20").Execute();
      Assert.Equal<ulong>(3, r.AffectedItemsCount);
    }

    [Fact]
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
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = coll.Remove("pages > 20").Limit(1).Execute();
      Assert.Equal<ulong>(1, r.AffectedItemsCount);

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => coll.Remove("True").Limit(0).Execute());
      Assert.Throws<ArgumentOutOfRangeException>(() => coll.Remove("True").Limit(-2).Execute());
      Assert.Throws<ArgumentOutOfRangeException>(() => coll.Remove("pages > 10").Limit(0).Execute());
      Assert.Throws<ArgumentOutOfRangeException>(() => coll.Remove("pages > 20").Limit(-3).Execute());
    }

    [Fact]
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
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = coll.Remove("pages > 20").Limit(1).Execute();
      Assert.Equal<ulong>(1, r.AffectedItemsCount);
    }

    [Fact]
    public void RemovingDocWithNoIdThrowsException()
    {
      Collection coll = CreateCollection("test");
      DbDoc doc = new DbDoc();
      Exception ex = Assert.Throws<InvalidOperationException>(() => coll.Remove(doc));
    }

    [Fact]
    public void RemovingItemUsingDbDoc()
    {
      Collection coll = CreateCollection("test");
      DbDoc doc = new DbDoc(new { _id = 1, title = "Book 1", pages = 20 });
      Result r = coll.Add(doc).Execute();
      Assert.Equal<ulong>(1, r.AffectedItemsCount);

      r = coll.Remove(doc).Execute();
      Assert.Equal<ulong>(1, r.AffectedItemsCount);
    }

    [Fact]
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
      Result r = coll.Add(docs).Execute();
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = coll.Remove("pages = :Pages").Bind("pAges", 50).Execute();
      Assert.Equal<ulong>(1, r.AffectedItemsCount);
    }

    [Fact]
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
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(4, result.AffectedItemsCount);

      // Condition can't be null or empty.
      string errorMessage = "Parameter can't be null or empty.\r\nParameter name: condition";
      Exception ex = Assert.Throws<ArgumentNullException>(() => collection.Remove(string.Empty).Execute());
      Assert.Equal(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => collection.Remove("").Execute());
      Assert.Equal(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => collection.Remove(" ").Execute());
      Assert.Equal(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => collection.Remove("  ").Execute());
      Assert.Equal(errorMessage, ex.Message);

      // Sending an expression that evaluates to true applies changes on all documents.
      result = collection.Remove("true").Execute();
      Assert.Equal<ulong>(4, result.AffectedItemsCount);
    }

    [Fact]
    public void RemoveWithInOperator()
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

      Assert.Equal<ulong>(1, collection.Remove("a IN (2,3)").Execute().AffectedItemsCount);
      Assert.Equal(2, collection.Find().Execute().FetchAll().Count);

      Assert.Equal<ulong>(0, collection.Remove("a IN [3]").Execute().AffectedItemsCount);
      Assert.Equal(2, collection.Find().Execute().FetchAll().Count);

      Assert.Equal<ulong>(2, collection.Remove("1 IN c.e").Execute().AffectedItemsCount);
      Assert.Equal(0, collection.Find().Execute().FetchAll().Count);
    }

    [Fact]
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
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(4, result.AffectedItemsCount);

      collection.Add(new { title = "Book 5", pages = 60 }).Execute();
      Assert.Equal(5, collection.Find().Execute().FetchAll().Count);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(null));
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(""));
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(string.Empty));

      // Remove sending numeric parameter.
      Assert.Equal<ulong>(1, collection.RemoveOne(1).AffectedItemsCount);
      Assert.Equal(4, collection.Find().Execute().FetchAll().Count);

      // Remove sending string parameter.
      Assert.Equal<ulong>(1, collection.RemoveOne("3").AffectedItemsCount);
      Assert.Equal(3, collection.Find().Execute().FetchAll().Count);

      // Remove an auto-generated id.
      DbDoc document = collection.Find("pages = 60").Execute().FetchOne();
      Assert.Equal<ulong>(1, collection.RemoveOne(document.Id).AffectedItemsCount);
      Assert.Equal(2, collection.Find().Execute().FetchAll().Count);

      // Remove a non-existing document.
      Assert.Equal<ulong>(0, collection.RemoveOne(5).AffectedItemsCount);
      Assert.Equal(2, collection.Find().Execute().FetchAll().Count);
    }
  }
}
