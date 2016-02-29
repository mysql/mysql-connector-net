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
using System;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class DbDocTests : BaseTest
  {
    [Fact]
    public void SimpleConverstionToJson()
    {
      DbDoc d = new DbDoc();
      d.SetValue("_id", 1);
      d.SetValue("pages", 20);
      string s = d.ToString();
      Assert.Equal(@"{""_id"":1, ""pages"":20}", d.ToString());
    }

    [Fact]
    public void SimpleParse()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": 20}");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      Assert.True(d.Equals(d2));
    }

    [Fact]
    public void NestedParse()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": 20, 
          ""person"": { ""name"": ""Fred"", ""age"": 45 }
      }");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("person", new { name = "Fred", age = 45 });
      Assert.True(d.Equals(d2));
    }

    [Fact]
    public void ParseWithArray()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": 20, 
          ""books"": [ 
            { ""_id"" : 1, ""title"" : ""Book 1"" },
            { ""_id"" : 2, ""title"" : ""Book 2"" }
          ]
      }");

      var docs = new[]
      {
        new {  _id = 1, title = "Book 1" },
        new {  _id = 2, title = "Book 2" },
      };
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", 20);
      d2.SetValue("books", docs);
      Assert.True(d.Equals(d2));
    }

    [Fact]
    public void ParseLongValues()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pages"": " + ((long)int.MaxValue + 1) + "}");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pages", (long)int.MaxValue + 1);
      Assert.True(d.Equals(d2));
    }

    [Fact]
    public void ParseFloatValues()
    {
      DbDoc d = new DbDoc(@"{ ""id"": 1, ""pi"": 3.14159 }");
      DbDoc d2 = new DbDoc();
      d2.SetValue("id", 1);
      d2.SetValue("pi", 3.14159);
      Assert.True(d.Equals(d2));
    }
  }
}
