// Copyright (c) 2017, 2020, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MySqlX.Data.Tests
{
  public class MergePatch : BaseTest
  {
    private static string[] documentsAsJsonStrings;
    private static DbDoc[] documentsAsDbDocs;

    static MergePatch()
    {
      documentsAsJsonStrings = new string[] {
        "{ " +
          // Document 1.
          "\"_id\": \"a6f4b93e1a264a108393524f29546a8c\", " +
          "\"title\": \"AFRICAN EGG\", " +
          "\"description\": \"A Fast-Paced Documentary of a Pastry Chef And a Dentist who must Pursue a Forensic Psychologist in The Gulf of Mexico\", " +
          "\"releaseyear\": 2006, " +
          "\"language\": \"English\", " +
          "\"duration\": 130, " +
          "\"rating\": \"G\", " +
          "\"genre\": \"Science fiction\", " +
          "\"actors\": [ " +
            "{ \"name\": \"MILLA PECK\", \"country\": \"Mexico\", \"birthdate\": \"12 Jan 1984\" }, " +
            "{ \"name\": \"VAL BOLGER\", \"country\": \"Botswana\", \"birthdate\": \"26 Jul 1975\" }, " +
            "{ \"name\": \"SCARLETT BENING\", \"country\": \"Syria\", \"birthdate\": \"16 Mar 1978\" }" +
          "], " +
          "\"additionalinfo\": { " +
            "\"director\": { " +
              "\"name\": \"Sharice Legaspi\", " +
              "\"age\":57, " +
              "\"birthplace\": " +
                "{ \"country\": \"Italy\", \"city\": \"Rome\" }, " +
              "\"awards\": [ " +
                "{ \"award\": \"Best Movie\", \"movie\": \"THE EGG\", \"year\": 2002}, " +
                "{ \"award\": \"Best Special Effects\", \"movie\": \"AFRICAN EGG\", \"year\": 2006 }" +
              "] " +
            "}, " +
            "\"writers\": [\"Rusty Couturier\", \"Angelic Orduno\", \"Carin Postell\"], " +
            "\"productioncompanies\": [\"Qvodrill\", \"Indigoholdings\"] " +
          "} " +
        "}"
      };

      documentsAsDbDocs = new DbDoc[documentsAsJsonStrings.Length];
      for (int i=0; i < documentsAsJsonStrings.Length; i++)
        documentsAsDbDocs[i] = new DbDoc(documentsAsJsonStrings[i]);
    }

    #region General

    [Test]
    public void PatchExpectedExceptions()
    {
      Collection collection = CreateCollection("test");

      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3))
        Assert.Throws<MySqlException>(() => collection.Modify("true").Patch(null));
      else
      {
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch(null));
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch("  "));
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch(""));
        Assert.Throws<ArgumentNullException>(() => collection.Modify("true").Patch(string.Empty));
      }
    }

    [Test]
    public void SimplePatch()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", \"startDate\": \"4/1/2017\" }"));
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = ExecuteAddStatement(collection.Add("{ \"_id\": \"124\", \"email\": \"jose@ora.com\", \"startDate\": \"4/1/2017\" }"));
      Assert.AreEqual(1, r.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("email = \"alice@ora.com\"").Patch("{ \"_id\": \"123\", \"email\":\"bob@ora.com\", \"startDate\":null }"));

      DbDoc document = collection.GetOne("123");
      Assert.AreEqual("123", document.Id);
      Assert.AreEqual("bob@ora.com", document.values["email"]);
      Assert.True(!document.values.ContainsKey("startDate"));

      document = collection.GetOne("124");
      Assert.AreEqual("124", document.Id);
      Assert.AreEqual("jose@ora.com", document.values["email"]);
      Assert.True(document.values.ContainsKey("startDate"));
    }

    [Test]
    public void SimplePatchUsingMySqlExpressionClass()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", \"startDate\": \"4/1/2017\" }"));
      Assert.AreEqual(1, r.AffectedItemsCount);

      var patch = new {
        email = new MySqlExpression("UPPER($.email)")
      };

      ExecuteModifyStatement(collection.Modify("true").Patch(patch));
      DbDoc document = collection.GetOne("123");
      Assert.AreEqual("ALICE@ORA.COM", document.values["email"]);
    }

    [Test]
    public void CRUD()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Add field.
      ExecuteModifyStatement(collection.Modify("language = :lang").Patch("{ \"translations\": [\"Spanish\"] }").Bind("lang", "English"));
      var document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(document.values.ContainsKey("translations"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("language = :lang").Patch("{ \"translations\": [\"Spanish\", \"Italian\"] }").Bind("lang", "English"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(document.values.ContainsKey("translations"));
      var translations = (object[]) document.values["translations"];
      Assert.AreEqual("Spanish", (string)translations[0]);
      Assert.AreEqual("Italian", (string)translations[1]);

      // Remove field.
      ExecuteModifyStatement(collection.Modify("language = :lang").Patch("{ \"translations\": null }").Bind("lang", "English"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.False(document.values.ContainsKey("translations"));

      // Add field.
      Assert.False(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("musicby"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": \"Sakila D\" } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("musicby"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string,object>) document.values["additionalinfo"]).ContainsKey("musicby"));
      Assert.AreEqual("The Sakila", ((Dictionary<string,object>) document.values["additionalinfo"])["musicby"]);

      // Remove field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": null } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("additionalinfo.director.name = :director").Bind("director", "Sharice Legaspi")).FetchOne();
      Assert.False(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("musicby"));

      // Add field.
      Assert.False(((Dictionary<string, object>)((Dictionary<string,object>) document.values["additionalinfo"])["director"]).ContainsKey("country"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"France\" } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]).ContainsKey("country"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"Canada\" } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string, object>)((Dictionary<string,object>) document.values["additionalinfo"])["director"]).ContainsKey("country"));
      Assert.AreEqual("Canada", ((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["country"]);

      // Remove field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": null } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("additionalinfo.director.name = :director").Bind("director", "Sharice Legaspi")).FetchOne();
      Assert.False(((Dictionary<string, object>)((Dictionary<string,object>) document.values["additionalinfo"])["director"]).ContainsKey("country"));
    }

    #endregion

    #region Nested operations

    [Test]
    public void ReplaceUpdateInDifferentNestingLevels()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("AFRICAN EGG", document["title"]);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": \"The African Egg\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("The African Egg", document["title"]);

      Assert.AreEqual(57, ((Dictionary<string, object>) ((Dictionary<string, object>) document.values["additionalinfo"])["director"])["age"]);
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"age\": 67 } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual(67, ((Dictionary<string, object>) ((Dictionary<string, object>) document.values["additionalinfo"])["director"])["age"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"director\": { \"age\": 77 } } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual(77, ((Dictionary<string, object>) ((Dictionary<string, object>) document.values["additionalinfo"])["director"])["age"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": { \"movie\": \"The African Egg\"} }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("The African Egg", ((Dictionary<string, object>) document.values["title"])["movie"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": \"No data available\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("No data available", document.values["additionalinfo"]);
    }

    [Test]
    public void AddRemoveFieldInDifferentNestingLevels()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(document.values.ContainsKey("translations"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"translations\": [\"Spanish\"] }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(document.values.ContainsKey("translations"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"translations\": null }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(document.values.ContainsKey("translations"));

      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]).ContainsKey("country"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"France\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]).ContainsKey("country"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": null } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]).ContainsKey("country"));

      Assert.False(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("musicby"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("musicby"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": null } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("musicby"));
    }

    #endregion

    #region Multiple fields

    [Test]
    public void CRUDMultipleFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");

      // Add fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("one", document.values["field 1"]);
      Assert.AreEqual("two", document.values["field 2"]);
      Assert.AreEqual("three", document.values["field 3"]);
      Assert.AreEqual("one", (((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]))["field 1"]);
      Assert.AreEqual("two", (((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]))["field 2"]);
      Assert.AreEqual("three", (((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]))["field 3"]);

      // Update/Replace fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": \"ONE\", \"field 2\": \"TWO\", \"field 3\": \"THREE\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": \"ONE\", \"field 2\": \"TWO\", \"field 3\": \"THREE\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("ONE", document.values["field 1"]);
      Assert.AreEqual("TWO", document.values["field 2"]);
      Assert.AreEqual("THREE", document.values["field 3"]);
      Assert.AreEqual("ONE", (((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]))["field 1"]);
      Assert.AreEqual("TWO", (((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]))["field 2"]);
      Assert.AreEqual("THREE", (((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"]))["field 3"]);

      // Remove fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": null, \"field 2\": null, \"field 3\": null }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": null, \"field 2\": null, \"field 3\": null } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(document.values.ContainsKey("field 1"));
      Assert.False(document.values.ContainsKey("field 2"));
      Assert.False(document.values.ContainsKey("field 3"));
      Assert.False((((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"])).ContainsKey("field 1"));
      Assert.False((((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"])).ContainsKey("field 2"));
      Assert.False((((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["director"])).ContainsKey("field 3"));
    }

    #endregion

    #region Using expressions

    [Test]
    public void AddNewFieldUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.False(((Dictionary<string, object>)((object[]) document.values["actors"])[0]).ContainsKey("age"));
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("true").Patch("{ \"actors\": { \"age\": Year(CURDATE()) - CAST(SUBSTRING_INDEX(actors.birthdate, ' ', - 1) AS DECIMAL)) } }")));

      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.False(document.values.ContainsKey("audio"));
      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"audio\": CONCAT($.language, ', no subtitles') }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.AreEqual("English, no subtitles", document.values["audio"]);
    }

    [Test]
    public void ReplaceUpdateUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");

      Assert.False(document.values.ContainsKey("audio"));
      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"audio\": CONCAT(UPPER($.language), ', No Subtitles') }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("ENGLISH, No Subtitles", document.values["audio"]);
    }

    [Test]
    public void ReplaceUpdateIdUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Changes to the _id field are ignored.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"_id\": replace(UUID(), '-', '') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.AreEqual("a6f4b93e1a264a108393524f29546a8c", document.Id);
    }

    [Test]
    public void AddIdToNestedDocumentUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("_id"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": replace(UUID(), '-', '') } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("_id"));
    }

    [Test]
    public void AddNullFieldUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>) document.values["additionalinfo"]).ContainsKey("releasedate"));
      Exception ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"releasedate\": DATE_ADD('2006-04-00',INTERVAL 1 DAY) }").Bind("id", "a6f4b93e1a264a108393524f29546a8c")));
      Assert.AreEqual("Invalid data for update operation on document collection table", ex.Message);
    }

    [Test]
    public void ReplaceUpdateNullFieldReturnedFromExpression()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("AFRICAN EGG", document.values["title"]);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": concat('my ', NULL, ' title') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(document.values.ContainsKey("title"));
    }

    [Test]
    public void AddNewFieldReturnedFromExpression()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(document.values.ContainsKey("docfield"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"docfield\": JSON_OBJECT('field 1', 1, 'field 2', 'two') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(document.values.ContainsKey("docfield"));
    }

    [Test]
    public void ReplaceUpdateFieldWithDocumentReturnedFromExpression()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("Science fiction", document.values["genre"]);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"genre\": JSON_OBJECT('name', 'Science Fiction') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("Science Fiction", ((Dictionary<string, object>) document.values["genre"])["name"]);
    }

    #endregion

    #region Using _id

    [Test]
    public void ReplaceUpdateId()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Changes to the _id field are ignored.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"_id\": \"b5f4b93e1a264a108393524f29546a9d\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.AreEqual("a6f4b93e1a264a108393524f29546a8c", document.Id);
    }

    [Test]
    public void AddIdToNestedDocument()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Add id to nested document is allowed.
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      var field = (Dictionary<string, object>) document.values["additionalinfo"];
      Assert.AreEqual(3, field.Count);
      Assert.False(field.ContainsKey("_id"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": \"b5f4b93e1a264a108393524f29546a9d\" } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      field = (Dictionary<string, object>) document.values["additionalinfo"];
      Assert.AreEqual(4, field.Count);
      Assert.True(field.ContainsKey("_id"));
      Assert.AreEqual("b5f4b93e1a264a108393524f29546a9d", field["_id"]);
    }

    [Test]
    public void SetNullToId()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Changes to the _id field are ignored.
      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"_id\": NULL }"));
      Assert.AreEqual("a6f4b93e1a264a108393524f29546a8c", ExecuteFindStatement(collection.Find()).FetchOne().Id);
    }

    [Test]
    public void AddNullFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.AreEqual(1, r.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": NULL }"));
      Assert.False(ExecuteFindStatement(collection.Find()).FetchOne().values.ContainsKey("nullfield"));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": [NULL, NULL] }"));
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.True(document.values.ContainsKey("nullfield"));
      var nullArray = (object[]) document.values["nullfield"];
      Assert.Null(nullArray[0]);
      Assert.Null(nullArray[1]);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": NULL } }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.True(ExecuteFindStatement(collection.Find()).FetchOne().values.ContainsKey("nullfield"));
      Assert.True(((Dictionary<string, object>)document.values["nullfield"]).Count == 0);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": [NULL, NULL] } }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      var nestedNullArray = (object[])((Dictionary<string, object>) document.values["nullfield"])["nested"];
      Assert.Null(nestedNullArray[0]);
      Assert.Null(nestedNullArray[1]);
    }

    [Test]
    public void AddNestedNullFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.AreEqual(1, r.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": NULL } }"));
      Assert.False(((Dictionary<string, object>) ExecuteFindStatement(collection.Find()).FetchOne().values["additionalinfo"]).ContainsKey("nullfield"));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": [NULL, NULL] } }"));
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      var nestedNullArray = (object[]) ((Dictionary<string, object>) document.values["additionalinfo"])["nullfield"];
      Assert.Null(nestedNullArray[0]);
      Assert.Null(nestedNullArray[1]);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": NULL } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["nullfield"]).ContainsKey("nullfield"));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": [NULL, NULL] } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      nestedNullArray = (object[]) ((Dictionary<string, object>)((Dictionary<string, object>) document.values["additionalinfo"])["nullfield"])["nested"];
      Assert.Null(nestedNullArray[0]);
      Assert.Null(nestedNullArray[1]);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": JSON_OBJECT('field', null) } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      var nestedObject = (Dictionary<string, object>)((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"])["nested"];
      CollectionAssert.IsEmpty(nestedObject);
    }

    #endregion

    [Test]
    public void GetDocumentProperties()
    {
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Get root string properties.
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("a6f4b93e1a264a108393524f29546a8c", document.Id);
      Assert.AreEqual("AFRICAN EGG", document["title"]);
      Assert.AreEqual("G", document["rating"]);

      // Get root numeric properties.
      Assert.AreEqual(2006, document["releaseyear"]);
      Assert.AreEqual(130, document["duration"]);

      // Get root array.
      object[] actors = document["actors"] as object[];
      Assert.True(actors.Length == 3);
      Dictionary<string, object> actor1 = actors[1] as Dictionary<string, object>;
      Assert.AreEqual("VAL BOLGER", actor1["name"]);

      // Get nested string properies.
      Assert.AreEqual("Sharice Legaspi", document["additionalinfo.director.name"]);
      Assert.AreEqual(57, document["additionalinfo.director.age"]);
      Assert.AreEqual("Italy", document["additionalinfo.director.birthplace.country"]);

      // Get nested array.
      object[] awards = document["additionalinfo.director.awards"] as object[];
      Assert.True(awards.Length == 2);
    }

    [Test]
    public void PatchUsingDateAndTimeFunctions()
    {
      string t1 = "{\"_id\": \"1\", \"name\": \"Alice\" }";
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(t1));
      Assert.AreEqual(1, r.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": YEAR('2000-01-01') }").Bind("id", "1"));
      DbDoc document = collection.GetOne("1");
      Assert.AreEqual(2000, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MONTH('2008-02-03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(2, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": WEEK('2008-02-20') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(7, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DAY('2008-02-20') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(20, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": HOUR('10:05:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(10, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MINUTE('2008-02-03 10:05:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(5, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": SECOND('10:05:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(3, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MICROSECOND('12:00:00.123456') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(123456, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": QUARTER('2008-04-01') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual(2, document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": TIME('2003-12-31 01:02:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual("01:02:03.000000", document["dateAndTimeValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DATE('2003-12-31 01:02:03') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual("2003-12-31", document["dateAndTimeValue"]);
    }

    [Test]
    public void PatchUsingOtherKnownFunctions()
    {
      string t1 = "{\"_id\": \"1\", \"name\": \"Alice\" }";
      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(t1));
      Assert.AreEqual(1, r.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"otherValue\": CHAR(77, 121, 83, 81, '76') }").Bind("id", "1"));
      DbDoc document = collection.GetOne("1");
      Assert.AreEqual("base64:type15:TXlTUUw=", document["otherValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"otherValue\": HEX('abc') }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual("616263", document["otherValue"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"otherValue\": BIN(12) }").Bind("id", "1"));
      document = collection.GetOne("1");
      Assert.AreEqual("1100", document["otherValue"]);
    }
  }
}
