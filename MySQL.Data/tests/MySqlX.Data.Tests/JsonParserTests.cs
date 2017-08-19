// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class JsonParserTests : BaseTest
  {
    [Fact(Skip="Fix This")]
    public void ParseBooleanValue()
    {
      // TODO: Boolean values are currently stored as strings.
      // Fix is required in JsonParser.ReadValue().
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""isDocument"": true }");     
      Result result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.Find().Execute().FetchOne();
      Assert.True(document.values.ContainsKey("isDocument"));
      Assert.True((bool) document.values["isDocument"]);

      document = new DbDoc(new { isDocument=true });
      result = collection.Add(document).Execute();
      Assert.Equal<ulong>(1, result.RecordsAffected);

      document = collection.Find().Execute().FetchOne();
      Assert.True(document.values.ContainsKey("isDocument"));
      Assert.True((bool) document.values["isDocument"]);
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
