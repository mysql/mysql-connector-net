// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
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
      Assert.Equal<ulong>(1, r.RecordsAffected);

      r = coll.Remove(12).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
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
      Assert.Equal<ulong>(4, r.RecordsAffected);

      r = coll.Remove("pages > 20").Execute();
      Assert.Equal<ulong>(3, r.RecordsAffected);
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
      Assert.Equal<ulong>(4, r.RecordsAffected);

      r = coll.Remove("pages > 20").Limit(1).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);

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
      Assert.Equal<ulong>(4, r.RecordsAffected);

      r = coll.Remove("pages > 20").Limit(1).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
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
      Assert.Equal<ulong>(1, r.RecordsAffected);

      r = coll.Remove(doc).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
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
      Assert.Equal<ulong>(4, r.RecordsAffected);

      r = coll.Remove("pages = :Pages").Bind("pAges", 50).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
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
      Assert.Equal<ulong>(4, result.RecordsAffected);

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
      Assert.Equal<ulong>(4, result.RecordsAffected);
    }

    [Fact]
    public void RemoveWithInOperator()
    {
      Collection collection = CreateCollection("test");
      var docs = new[]
      {
        new DbDoc("{ \"a\": 1, \"b\": \"foo\", \"c\": { \"d\": true, \"e\": [1,2,3] }, \"f\": [ {\"x\":5}, {\"x\":7 } ] }"),
        new DbDoc("{ \"a\": 2, \"b\": \"foo2\", \"c\": { \"d\": true, \"e\": [4,5,6] }, \"f\": [ {\"x\":5}, {\"x\":8 } ] }"),
        new DbDoc("{ \"a\": 1, \"b\": \"foo3\", \"c\": { \"d\": true, \"e\": [1,4,3] }, \"f\": [ {\"x\":6}, {\"x\":9 } ] }"),
      };
      Result result = collection.Add(docs).Execute();
      Assert.Equal<ulong>(3, result.RecordsAffected);

      Assert.Equal<ulong>(1, collection.Remove("a IN (2,3)").Execute().RecordsAffected);
      Assert.Equal(2, collection.Find().Execute().FetchAll().Count);

      Assert.Equal<ulong>(0, collection.Remove("a IN [3]").Execute().RecordsAffected);
      Assert.Equal(2, collection.Find().Execute().FetchAll().Count);

      Assert.Equal<ulong>(2, collection.Remove("1 IN c.e").Execute().RecordsAffected);
      Assert.Equal(0, collection.Find().Execute().FetchAll().Count);
    }
  }
}
