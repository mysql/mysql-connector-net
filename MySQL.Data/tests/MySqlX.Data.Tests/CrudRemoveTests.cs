// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;

namespace MySqlX.Data.Tests
{
  public class CrudRemoveTests : BaseTest
  {
    [Fact]
    public void RemoveSingleDocumentById()
    {
      Collection coll = CreateCollection("test");
      var docs = new[]{
        new { _id = 12, title = "Book 1", pages = 20 },
        new { _id = 34, title = "Book 2", pages = 30 },
        new { _id = 56, title = "Book 3", pages = 40 },
      };
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.Equal<ulong>(3, r.AffectedItemsCount);

      // Remove with condition.
      r = ExecuteRemoveStatement(coll.Remove("_id = 12"));
      Assert.Equal<ulong>(1, r.AffectedItemsCount);

      // Remove by ID.
      r = coll.RemoveOne(34);
      Assert.Equal<ulong>(1, r.AffectedItemsCount);

      var ex = Assert.Throws<ArgumentNullException>(() => coll.Remove(""));
#if NETCOREAPP3_0
      Assert.Equal("Parameter can't be null or empty. (Parameter 'condition')", ex.Message);
#else
      Assert.Equal("Parameter can't be null or empty.\r\nParameter name: condition", ex.Message);
#endif
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
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = ExecuteRemoveStatement(coll.Remove("pages > 20"));
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
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = ExecuteRemoveStatement(coll.Remove("pages > 20").Limit(1));
      Assert.Equal<ulong>(1, r.AffectedItemsCount);

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("True").Limit(0)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("True").Limit(-2)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("pages > 10").Limit(0)));
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteRemoveStatement(coll.Remove("pages > 20").Limit(-3)));
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
      Result r = ExecuteAddStatement(coll.Add(docs));
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = ExecuteRemoveStatement(coll.Remove("pages > 20").Limit(1));
      Assert.Equal<ulong>(1, r.AffectedItemsCount);
    }

    [Fact]
    public void RemovingDocWithNoIdThrowsException()
    {
      Collection coll = CreateCollection("test");
      DbDoc doc = new DbDoc();
      Exception ex = Assert.Throws<KeyNotFoundException>(() => ExecuteRemoveStatement(coll.Remove("_id = :id").Bind("id", doc.Id)));
    }

    [Fact]    public void RemoveBind()
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
      Assert.Equal<ulong>(4, r.AffectedItemsCount);

      r = ExecuteRemoveStatement(coll.Remove("pages = :Pages").Bind("pAges", 50));
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
      Result result = ExecuteAddStatement(collection.Add(docs));
      Assert.Equal<ulong>(4, result.AffectedItemsCount);

      // Condition can't be null or empty.
      string errorMessage = string.Empty;
#if NETCOREAPP3_0
      errorMessage = "Parameter can't be null or empty. (Parameter 'condition')";
#else
      errorMessage = "Parameter can't be null or empty.\r\nParameter name: condition";
#endif
      Exception ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove(string.Empty)));
      Assert.Equal(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove("")));
      Assert.Equal(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove(" ")));
      Assert.Equal(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentNullException>(() => ExecuteRemoveStatement(collection.Remove("  ")));
      Assert.Equal(errorMessage, ex.Message);

      // Sending an expression that evaluates to true applies changes on all documents.
      result = ExecuteRemoveStatement(collection.Remove("true"));
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
      Result result = ExecuteAddStatement(collection.Add(docs));
      Assert.Equal<ulong>(3, result.AffectedItemsCount);

      Assert.Equal<ulong>(1, ExecuteRemoveStatement(collection.Remove("a IN (2,3)")).AffectedItemsCount);
      Assert.Equal(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      Assert.Equal<ulong>(0, ExecuteRemoveStatement(collection.Remove("a IN [3]")).AffectedItemsCount);
      Assert.Equal(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      Assert.Equal<ulong>(2, ExecuteRemoveStatement(collection.Remove("1 IN c.e")).AffectedItemsCount);
      Assert.Empty(ExecuteFindStatement(collection.Find()).FetchAll());
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
      Result result = ExecuteAddStatement(collection.Add(docs));
      Assert.Equal<ulong>(4, result.AffectedItemsCount);

      ExecuteAddStatement(collection.Add(new { title = "Book 5", pages = 60 }));
      Assert.Equal(5, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove sending numeric parameter.
      Assert.Equal<ulong>(1, collection.RemoveOne(1).AffectedItemsCount);
      Assert.Equal(4, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove sending string parameter.
      Assert.Equal<ulong>(1, collection.RemoveOne("3").AffectedItemsCount);
      Assert.Equal(3, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove an auto-generated id.
      DbDoc document = ExecuteFindStatement(collection.Find("pages = 60")).FetchOne();
      Assert.Equal<ulong>(1, collection.RemoveOne(document.Id).AffectedItemsCount);
      Assert.Equal(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Remove a non-existing document.
      Assert.Equal<ulong>(0, collection.RemoveOne(5).AffectedItemsCount);
      Assert.Equal(2, ExecuteFindStatement(collection.Find()).FetchAll().Count);

      // Expected exceptions.
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(null));
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(""));
      Assert.Throws<ArgumentNullException>(() => collection.RemoveOne(string.Empty));
    }
  }
}
