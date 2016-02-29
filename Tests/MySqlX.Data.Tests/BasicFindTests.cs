// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class BasicFindTests : BaseTest
  {
    [Fact]
    public void SimpleFind()
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

      DocResult foundDocs = coll.Find("pages > 20").Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 2");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 3");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 4");
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void SimpleFindWithSort()
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

      DocResult foundDocs = coll.Find("pages > 20").OrderBy("pages DESC").Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 4");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 3");
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 2");
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void SimpleFindWithLimit()
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

      DocResult foundDocs = coll.Find("pages > 20").Limit(1).Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 2");
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void FindConditional()
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

      DocResult foundDocs = coll.Find("pages = :Pages").Bind("pAges", 40).Execute();
      Assert.True(foundDocs.Next());
      Assert.True(foundDocs.Current["title"] == "Book 3");
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void BindDbDoc()
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

      DbDoc docParams = new DbDoc(new { pages1 = 30, pages2 = 40 });
      DocResult foundDocs = coll.Find("pages = :Pages1 || pages = :Pages2").Bind(docParams).Execute();
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void BindJsonAsAnonymous()
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

      var jsonParams = new { pages1 = 30, pages2 = 40 };
      DocResult foundDocs = coll.Find("pages = :Pages1 || pages = :Pages2").Bind(jsonParams).Execute();
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }

    [Fact]
    public void BindJsonAsString()
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

      var jsonParams = "{ \"pages1\" : 30, \"pages2\" : 40 }";
      DocResult foundDocs = coll.Find("pages = :Pages1 || pages = :Pages2").Bind(jsonParams).Execute();
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 2", foundDocs.Current["title"]);
      Assert.True(foundDocs.Next());
      Assert.Equal("Book 3", foundDocs.Current["title"]);
      Assert.False(foundDocs.Next());
    }
  }
}

