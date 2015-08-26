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

using MySql.XDevAPI;
using Xunit;

namespace PortableConnectorNetTests
{
  public class CrudInsertTests : BaseTest
  {
    [Fact]
    public void InsertSingleJSONDocWithId()
    {
      Collection coll = CreateCollection("test");
      Result r = coll.Add("{ \"_id\": 1, \"foo\": 1 }").Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      coll.Drop();
    }

    [Fact]
    public void InsertSingleJSONDocWithoutId()
    {
      Collection coll = CreateCollection("test");
      Result r = coll.Add("{ \"foo\": 1 }").Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      /// TODO:  retrieve doc and complete foo column
      coll.Drop();
    }

    [Fact]
    public void InsertAnonymousObjectWithId()
    {
      var obj = new { _id = "5", name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      Result r = coll.Add(obj).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      ///TODO:  pull object and verify data
      coll.Drop();
    }

    [Fact]
    public void InsertAnonymousObjectWithNoId()
    {
      var obj = new { name = "Sakila", age = 15 };

      Collection coll = CreateCollection("test");
      Result r = coll.Add(obj).Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);
      ///TODO:  pull object and verify data
      coll.Drop();
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
      coll.Drop();
    }
  }
}
