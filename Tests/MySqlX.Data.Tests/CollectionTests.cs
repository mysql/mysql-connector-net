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

using System.Collections.Generic;
using MySqlX.XDevAPI;
using Xunit;
using MySql.Data.MySqlClient;

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
    public void CreateAndDeleteCollection()
    {
      XSession s = GetSession();
      Schema test = s.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      Assert.True(testColl.ExistsInDatabase());
      test.DropCollection("test");
      Assert.False(testColl.ExistsInDatabase());
    }

    [Fact]
    public void CreateCollectionIndex()
    {
      XSession session = GetSession();
      Schema test = session.GetSchema("test");
      Collection testColl = test.CreateCollection("test");
      Assert.True(testColl.ExistsInDatabase(), "ExistsInDatabase failed");
      var result = testColl.CreateIndex("testIndex", true).Field(".myId", "INT", true).Execute();
      result = testColl.Add(new { myId = 1 }).Add(new { myId = 2 }).Execute();
      Assert.Throws<MySqlException>(() => testColl.Add(new { myId = 1 }).Execute());
      result = testColl.DropIndex("testIndex");
      result = testColl.Add(new { myId = 1 }).Execute();
    }

  }
}
