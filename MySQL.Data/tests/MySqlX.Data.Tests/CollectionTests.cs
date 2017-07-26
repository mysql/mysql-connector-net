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
      var result = testColl.CreateIndex("testIndex", true).Field("$.myId", "INT", true).Execute();
      result = testColl.Add(new { myId = 1 }).Add(new { myId = 2 }).Execute();
      Assert.Throws<MySqlException>(() => testColl.Add(new { myId = 1 }).Execute());
      testColl.DropIndex("testIndex");
      result = testColl.Add(new { myId = 1 }).Execute();
    }

    [Fact]
    public void DropCollectionIndex()
    {
      Session session = GetSession();
      Schema test = session.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      testColl.CreateIndex("testIndex", true).Field("$.myId", "INT", true).Execute();

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
