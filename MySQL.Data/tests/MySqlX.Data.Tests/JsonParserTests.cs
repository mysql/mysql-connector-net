// Copyright © 2017, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MySqlX.Data.Tests
{
  public class JsonParserTests : BaseTest
  {
    [Test]
    public void ParseBooleanValue()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""_id"": 1, ""isDocument"": true }");
      Result result = ExecuteAddStatement(collection.Add(document));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      document = collection.GetOne(1);
      Assert.That(document.values.ContainsKey("isDocument"));
      Assert.That((bool) document.values["isDocument"]);

      document = new DbDoc(new { _id=2, isDocument=false });
      result = ExecuteAddStatement(collection.Add(document));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      document = collection.GetOne(2);
      Assert.That(document.values.ContainsKey("isDocument"));
      Assert.That((bool) document.values["isDocument"], Is.False);

      Assert.That(ExecuteFindStatement(collection.Find("isDocument = false")).FetchAll().Count > 0);
    }

    [Test]
    public void ParseNullValue()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""isDocument"": null }");
      Result result = ExecuteAddStatement(collection.Add(document));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.values.ContainsKey("isDocument"));
      Assert.That(document.values["isDocument"], Is.Null);
    }

    [Test]
    public void ParseNumberArray()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,2,3] }");
      Result result = ExecuteAddStatement(collection.Add(document));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.values.ContainsKey("list"));
      Assert.That(document.values["list"], Is.EqualTo(new object[] { 1, 2, 3 }));
    }

    [Test]
    public void ParsObjectArray()
    {
      Collection collection = CreateCollection("test");

      DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a""] }");
      //DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a"",true,null] }");
      Result result = ExecuteAddStatement(collection.Add(document));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.values.ContainsKey("list"));
      Assert.That(document.values["list"], Is.EqualTo(new object[] { 1, "a" }));
      //Assert.AreEqual(new object[] { 1, "a", true, null }, document.values["list"]);
    }

    [Test]
    public void ParseGroupsEmbededInArrays()
    {
      DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a"", { ""b"": 1 } ] }");
      //DbDoc document = new DbDoc(@"{ ""id"": 1, ""list"": [1,""a"",true,null] }");
      Collection collection = CreateCollection("test");
      Result result = ExecuteAddStatement(collection.Add(document));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.That(document.values.ContainsKey("list"));
      var dictionary = new Dictionary<string,object>();
      dictionary.Add("b",1);
      Assert.That(document.values["list"], Is.EqualTo(new object[] { 1, "a", dictionary }));
      //Assert.AreEqual(new object[] { 1, "a", true, null, dictionary }, document.values["list"]);
    }

    [Test]
    public void ParseWithEscapedQuotes()
    {
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add("{ \"_id\": \"123\", \"email\": [\"alice@ora.com\"], \"dates\": [\"4/1/2017\"] }"));
      var expr = new
      {
        email = new MySqlExpression("UPPER($.email)")
      };
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("email", expr));
      var expr2 = new
      {
        email = new MySqlExpression("UPPER($.email[0])")
      };
      ExecuteModifyStatement(collection.Modify("true").ArrayAppend("email", expr2));

      var document = collection.GetOne("123");
      var innerEmail = ((document["email"] as object[])[1]) as Dictionary<string, object>;
      Assert.That(innerEmail["email"], Is.EqualTo("[\\\"ALICE@ORA.COM\\\"]"));
      innerEmail = ((document["email"] as object[])[2]) as Dictionary<string, object>;
      Assert.That(innerEmail["email"], Is.EqualTo("ALICE@ORA.COM"));

      ExecuteAddStatement(collection.Add("{ \"_id\": \"124\", \"email\": \"\\\"\"  }"));
      document = collection.GetOne("124");
      Assert.That(document["email"], Is.EqualTo("\\\""));

      var ex = Assert.Throws<Exception>(() => ExecuteAddStatement(collection.Add("{ \"_id\": \"124\", \"email\": \"\"\"  }")));
      Assert.That(ex.Message, Is.EqualTo("The value provided is not a valid JSON document. Expected token ','"));

      ex = Assert.Throws<Exception>(() => ExecuteAddStatement(collection.Add("{ \"_id\": \"124\", \"email\": \"\\\"  }")));
      Assert.That(ex.Message, Is.EqualTo("The value provided is not a valid JSON document. Failed to find ending '\"' while reading stream."));
    }
  }
}
