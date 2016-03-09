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
      Assert.Equal("{\"_id\":1, \"name\":\"Book 1\", \"pages\":10}", docs[0].ToString());
      Assert.Equal("{\"_id\":2, \"name\":\"Book 2\", \"pages\":20}", docs[1].ToString());
    }
  }
}
