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

using MySqlX.Serialization;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using System;
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
      Assert.Equal<ulong>(1, result.RecordsAffected);

      result = coll.Modify("_id = 1").Set("pages", "20").Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);
    }

    [Fact]
    public void ChangeItemInSingleDocument()
    {
      Collection coll = CreateCollection("test");
      Result result = coll.Add(new { _id = 1, name = "Book 1", pages = 20 }).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      result = coll.Modify("_id = 1").Change("name", "Book 2").Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);
    }

    [Fact]
    public void RemoveItemInSingleDocument()
    {
      Collection coll = CreateCollection("test");
      Result result = coll.Add(new { _id = 1, name = "Book 1", pages = 20 }).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      result = coll.Modify("_id = 1").Unset("pages").Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);
    }

    [Fact]
    public void SetItemAndBind()
    {
      Collection coll = CreateCollection("test");
      Result result = coll.Add(new { _id = 1, name = "Book 1" })
        .Add(new { _id = 2, name = "Book 2" }).Execute();
      Assert.Equal<ulong>(2, result.RecordsAffected);

      var stmt = coll.Modify("_id = :ID");
      result = stmt.Bind("Id", 2).Set("pages", "20").Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);
      result = stmt.Bind("Id", 1).Set("pages", "10").Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

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
      Assert.Equal<ulong>(2, result.RecordsAffected);

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
      Assert.Equal<ulong>(2, result.RecordsAffected);
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
      Assert.Equal<ulong>(2, result.RecordsAffected);

      collection.Modify("true").Set("title", "Book X").Limit(1).Execute();
      Assert.Equal(1, collection.Find("title = \"Book X\"").Execute().FetchAll().Count);

      // Limit out of range.
      Assert.Throws<ArgumentOutOfRangeException>(() => collection.Modify("true").Set("pages", 10).Limit(0).Execute());
      Assert.Throws<ArgumentOutOfRangeException>(() => collection.Modify("true").Set("pages", 10).Limit(-10).Execute());
    }

    [Fact]
    public void ModifyWithInOperator()
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

      Assert.Equal<ulong>(3, collection.Modify("a IN (1,2)").Set("a", 3).Execute().RecordsAffected);
      Assert.Equal(3, collection.Find().Where("a = 3").Execute().FetchAll().Count);

      Assert.Equal<ulong>(3, collection.Modify("a IN [3]").Set("a", 1).Execute().RecordsAffected);
      Assert.Equal(3, collection.Find().Where("a = 1").Execute().FetchAll().Count);

      Assert.Equal<ulong>(2, collection.Modify("1 IN c.e").Set("c.e", "newValue").Execute().RecordsAffected);
      Assert.Equal(2, collection.Find().Where("c.e = \"newValue\"").Execute().FetchAll().Count);
    }
  }
}
