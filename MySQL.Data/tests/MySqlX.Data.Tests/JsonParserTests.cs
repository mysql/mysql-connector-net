// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using System.Collections.Generic;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class JsonParserTests : BaseTest
  {
    [Fact]
    public void ParseBooleanValue()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""_id"": 1, ""isDocument"": true }");
      Result result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.GetOne(1);
      Assert.True(document.values.ContainsKey("isDocument"));
      Assert.True((bool) document.values["isDocument"]);

      document = new DbDoc(new { _id=2, isDocument=false });
      result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.GetOne(2);
      Assert.True(document.values.ContainsKey("isDocument"));
      Assert.False((bool) document.values["isDocument"]);

      Assert.True(collection.Find("isDocument = false").Execute().FetchAll().Count > 0);
    }

    [Fact]
    public void ParseNullValue()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""isDocument"": null }");
      Result result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.Find().Execute().FetchOne();
      Assert.True(document.values.ContainsKey("isDocument"));
      Assert.Equal(null, document.values["isDocument"]);
    }

    [Fact]
    public void ParseNumberArray()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,2,3] }");
      Result result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.Find().Execute().FetchOne();
      Assert.True(document.values.ContainsKey("list"));
      Assert.Equal(new object[] { 1, 2, 3 }, document.values["list"]);
    }

    [Fact]
    public void ParsObjectArray()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a""] }");
      //DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a"",true,null] }");
      Result result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.Find().Execute().FetchOne();
      Assert.True(document.values.ContainsKey("list"));
      Assert.Equal(new object[] { 1, "a" }, document.values["list"]);
      //Assert.Equal(new object[] { 1, "a", true, null }, document.values["list"]);
    }

    [Fact]
    public void ParseGroupsEmbededInArrays()
    {
      DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a"", { ""b"": 1 } ] }");
      //DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a"",true,null] }");
      Collection collection = CreateCollection("test");
      Result result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.Find().Execute().FetchOne();
      Assert.True(document.values.ContainsKey("list"));
      var dictionary = new Dictionary<string,object>();
      dictionary.Add("b",1);
      Assert.Equal(new object[] { 1, "a", dictionary }, document.values["list"]);
      //Assert.Equal(new object[] { 1, "a", true, null, dictionary }, document.values["list"]);
    }
  }
}
