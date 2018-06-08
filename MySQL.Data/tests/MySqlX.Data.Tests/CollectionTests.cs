// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System.Collections.Generic;
using MySqlX.XDevAPI;
using Xunit;
using MySql.Data.MySqlClient;
using System;

namespace MySqlX.Data.Tests
{
  public class CollectionTests : BaseTest
  {
    [Fact]
    public void GetAllCollections()
    {
      Collection book = CreateCollection("book");
      List<Collection> collections = book.Schema.GetCollections();
      Assert.True(collections.Count == 1);
      Assert.True(collections[0].Name == "book");
    }

    [Fact]
    public void CreateAndDropCollection()
    {
      Session s = GetSession();
      Schema test = s.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      Assert.True(testColl.ExistsInDatabase());

      // Drop existing collection.
      test.DropCollection("test");
      Assert.False(testColl.ExistsInDatabase());

      // Drop non-existing collection.
      test.DropCollection("test");
      Assert.False(testColl.ExistsInDatabase());

      // Empty, whitespace and null schema name.
      Assert.Throws<ArgumentNullException>(() => test.DropCollection(string.Empty));
      Assert.Throws<ArgumentNullException>(() => test.DropCollection(" "));
      Assert.Throws<ArgumentNullException>(() => test.DropCollection("  "));
      Assert.Throws<ArgumentNullException>(() => test.DropCollection(null));
    }

    [Fact]
    public void CreateCollectionIndex()
    {
      Session session = GetSession();
      Schema test = session.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      Assert.True(testColl.ExistsInDatabase(), "ExistsInDatabase failed");
      testColl.CreateIndex("testIndex", "{ \"fields\": [ { \"field\":$.myId, \"type\":\"INT\", \"required\":true } ] }");
      var result = testColl.Add(new { myId = 1 }).Add(new { myId = 2 }).Execute();
      Assert.Equal<ulong>(result.AffectedItemsCount, 2);
    }

    [Fact]
    public void DropCollectionIndex()
    {
      Session session = GetSession();
      Schema test = session.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      testColl.CreateIndex("testIndex", "{ \"fields\": [ { \"field\":$.myId, \"type\":\"INT\", \"required\":true } ] }");

      // Drop existing index.
      testColl.DropIndex("testIndex");

      // Drop non-existing index.
      testColl.DropIndex("testIndex");

      // Empty, whitespace and null schema name.
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(string.Empty));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(" "));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex("  "));
      Assert.Throws<ArgumentNullException>(() => testColl.DropIndex(null));
    }
  }
}
