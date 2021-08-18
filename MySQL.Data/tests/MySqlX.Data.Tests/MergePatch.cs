// Copyright (c) 2017, 2021, Oracle and/or its affiliates.
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
    private string documentAsJsonString2 =
                          "{ " +
                          // Document 2.
                          "\"_id\": \"123456789asdferdfghhghjh12334\", " +
                          "\"title\": \"CASTAWAY\", " +
                          "\"description\": \"A FedEx executive must transform himself physically and  " +
                          "emotionally to survive a crash landing on a deserted island\", " +
                          "\"releaseyear\": 2000, " +
                          "\"language\": \"English\", " +
                          "\"duration\": 150, " +
                          "\"rating\": \"PG\", " +
                          "\"genre\": \"Drama\", " +
                          "\"actors\": [ " +
                          "{ \"name\": \"TOM HANKS\", \"country\": \"USA\", \"birthdate\": \"9 Jul 1956\" }, " +
                          "{ \"name\": \"Paul Sanchez\", \"country\": \"Spain\", \"birthdate\": \"26 Oct 1975\" }, " +
                          "{ \"name\": \"Lari White\", \"country\": \"France\", \"birthdate\": \"13 May 1965\" }" +
                          "], " +
                          "\"additionalinfo\": { " +
                          "\"director\": { " +
                          "\"name\": \"Steven Spielberg\", " +
                          "\"birthplace\": " +
                          "{ \"country\": {\"countryActual\": \"USA\", \"city\": \"Florida\"} }, " +
                          "\"age\":67, " +
                          "\"awards\": [ " +
                          "{ \"award\": \"Best Movie\", \"movie\": \"American Beauty\", \"year\": 1999}, " +
                          "{ \"award\": \"Best Director\", \"movie\": \"Saving Private Ryan\", \"year\": 1998 }" +
                          "] " +
                          "}, " +
                          "\"writers\": [\"Tom Sizemore\", \"Barry Pepper\", \"Giovanni Ribisi\"], " +
                          "\"productioncompanies\": [\"Amblin Entertainment\", \"Mutual Film Company\"] " +
                          "} " +
                          "}";

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
      for (int i = 0; i < documentsAsJsonStrings.Length; i++)
        documentsAsDbDocs[i] = new DbDoc(documentsAsJsonStrings[i]);
    }

    #region General

    [Test]
    public void PatchExpectedExceptions()
    {
      Collection collection = CreateCollection("test");

      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3))
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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", \"startDate\": \"4/1/2017\" }"));
      Assert.AreEqual(1, r.AffectedItemsCount);

      var patch = new
      {
        email = new MySqlExpression("UPPER($.email)")
      };

      ExecuteModifyStatement(collection.Modify("true").Patch(patch));
      DbDoc document = collection.GetOne("123");
      Assert.AreEqual("ALICE@ORA.COM", document.values["email"]);
    }

    [Test]
    public void CRUD()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      var translations = (object[])document.values["translations"];
      Assert.AreEqual("Spanish", (string)translations[0]);
      Assert.AreEqual("Italian", (string)translations[1]);

      // Remove field.
      ExecuteModifyStatement(collection.Modify("language = :lang").Patch("{ \"translations\": null }").Bind("lang", "English"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.False(document.values.ContainsKey("translations"));

      // Add field.
      Assert.False(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": \"Sakila D\" } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));
      Assert.AreEqual("The Sakila", ((Dictionary<string, object>)document.values["additionalinfo"])["musicby"]);

      // Remove field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"musicby\": null } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("additionalinfo.director.name = :director").Bind("director", "Sharice Legaspi")).FetchOne();
      Assert.False(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));

      // Add field.
      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"France\" } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));

      // Update field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"Canada\" } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("language = :lang").Bind("lang", "English")).FetchOne();
      Assert.True(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));
      Assert.AreEqual("Canada", ((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["country"]);

      // Remove field.
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": null } } }").Bind("director", "Sharice Legaspi"));
      document = ExecuteFindStatement(collection.Find("additionalinfo.director.name = :director").Bind("director", "Sharice Legaspi")).FetchOne();
      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));
    }

    #endregion

    #region Nested operations

    [Test]
    public void ReplaceUpdateInDifferentNestingLevels()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("AFRICAN EGG", document["title"]);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": \"The African Egg\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("The African Egg", document["title"]);

      Assert.AreEqual(57, ((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["age"]);
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"age\": 67 } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual(67, ((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["age"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"director\": { \"age\": 77 } } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual(77, ((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])["age"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"title\": { \"movie\": \"The African Egg\"} }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("The African Egg", ((Dictionary<string, object>)document.values["title"])["movie"]);

      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": \"No data available\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("No data available", document.values["additionalinfo"]);
    }

    [Test]
    public void AddRemoveFieldInDifferentNestingLevels()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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

      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": \"France\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"country\": null } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]).ContainsKey("country"));

      Assert.False(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": null } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("musicby"));
    }

    #endregion

    #region Multiple fields

    [Test]
    public void CRUDMultipleFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      Assert.AreEqual("one", (((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 1"]);
      Assert.AreEqual("two", (((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 2"]);
      Assert.AreEqual("three", (((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 3"]);

      // Update/Replace fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": \"ONE\", \"field 2\": \"TWO\", \"field 3\": \"THREE\" }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": \"ONE\", \"field 2\": \"TWO\", \"field 3\": \"THREE\" } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("ONE", document.values["field 1"]);
      Assert.AreEqual("TWO", document.values["field 2"]);
      Assert.AreEqual("THREE", document.values["field 3"]);
      Assert.AreEqual("ONE", (((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 1"]);
      Assert.AreEqual("TWO", (((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 2"]);
      Assert.AreEqual("THREE", (((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"]))["field 3"]);

      // Remove fields.
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"field 1\": null, \"field 2\": null, \"field 3\": null }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      ExecuteModifyStatement(collection.Modify("additionalinfo.director.name = :director").Patch("{ \"additionalinfo\": { \"director\": { \"field 1\": null, \"field 2\": null, \"field 3\": null } } }").Bind("director", "Sharice Legaspi"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(document.values.ContainsKey("field 1"));
      Assert.False(document.values.ContainsKey("field 2"));
      Assert.False(document.values.ContainsKey("field 3"));
      Assert.False((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])).ContainsKey("field 1"));
      Assert.False((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])).ContainsKey("field 2"));
      Assert.False((((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["director"])).ContainsKey("field 3"));
    }

    #endregion

    #region Using expressions

    [Test]
    public void AddNewFieldUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.False(((Dictionary<string, object>)((object[])document.values["actors"])[0]).ContainsKey("age"));
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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("_id"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": replace(UUID(), '-', '') } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("_id"));
    }

    [Test]
    public void AddNullFieldUsingExpressions()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>)document.values["additionalinfo"]).ContainsKey("releasedate"));
      Exception ex = Assert.Throws<MySqlException>(() => ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"releasedate\": DATE_ADD('2006-04-00',INTERVAL 1 DAY) }").Bind("id", "a6f4b93e1a264a108393524f29546a8c")));
      Assert.AreEqual("Invalid data for update operation on document collection table", ex.Message);
    }

    [Test]
    public void ReplaceUpdateNullFieldReturnedFromExpression()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("Science fiction", document.values["genre"]);
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"genre\": JSON_OBJECT('name', 'Science Fiction') }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("Science Fiction", ((Dictionary<string, object>)document.values["genre"])["name"]);
    }

    #endregion

    #region Using _id

    [Test]
    public void ReplaceUpdateId()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsDbDocs));
      Assert.AreEqual(1, r.AffectedItemsCount);

      // Add id to nested document is allowed.
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      var field = (Dictionary<string, object>)document.values["additionalinfo"];
      Assert.AreEqual(3, field.Count);
      Assert.False(field.ContainsKey("_id"));
      ExecuteModifyStatement(collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": \"b5f4b93e1a264a108393524f29546a9d\" } }").Bind("id", "a6f4b93e1a264a108393524f29546a8c"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      field = (Dictionary<string, object>)document.values["additionalinfo"];
      Assert.AreEqual(4, field.Count);
      Assert.True(field.ContainsKey("_id"));
      Assert.AreEqual("b5f4b93e1a264a108393524f29546a9d", field["_id"]);
    }

    [Test]
    public void SetNullToId()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

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
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.AreEqual(1, r.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": NULL }"));
      Assert.False(ExecuteFindStatement(collection.Find()).FetchOne().values.ContainsKey("nullfield"));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": [NULL, NULL] }"));
      DbDoc document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.True(document.values.ContainsKey("nullfield"));
      var nullArray = (object[])document.values["nullfield"];
      Assert.Null(nullArray[0]);
      Assert.Null(nullArray[1]);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": NULL } }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      Assert.True(ExecuteFindStatement(collection.Find()).FetchOne().values.ContainsKey("nullfield"));
      Assert.True(((Dictionary<string, object>)document.values["nullfield"]).Count == 0);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": [NULL, NULL] } }"));
      document = ExecuteFindStatement(collection.Find()).FetchOne();
      var nestedNullArray = (object[])((Dictionary<string, object>)document.values["nullfield"])["nested"];
      Assert.Null(nestedNullArray[0]);
      Assert.Null(nestedNullArray[1]);
    }

    [Test]
    public void AddNestedNullFields()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      Collection collection = CreateCollection("test");
      Result r = ExecuteAddStatement(collection.Add(documentsAsJsonStrings));
      Assert.AreEqual(1, r.AffectedItemsCount);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": NULL } }"));
      Assert.False(((Dictionary<string, object>)ExecuteFindStatement(collection.Find()).FetchOne().values["additionalinfo"]).ContainsKey("nullfield"));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": [NULL, NULL] } }"));
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      var nestedNullArray = (object[])((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"];
      Assert.Null(nestedNullArray[0]);
      Assert.Null(nestedNullArray[1]);

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": NULL } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.False(((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"]).ContainsKey("nullfield"));

      ExecuteModifyStatement(collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": [NULL, NULL] } } }"));
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      nestedNullArray = (object[])((Dictionary<string, object>)((Dictionary<string, object>)document.values["additionalinfo"])["nullfield"])["nested"];
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

    #region WL14389

    [Test, Description("Test valid modify.patch on condition matching multiple records))")]
    public void ModifyPatchMultipleRecords_S1()
    {

      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = "12"},
        new {_id = 2, title = "Book 2", pages = 30,age = "18"},
        new {_id = 3, title = "Book 3", pages = 40,age = "34"},
        new {_id = 4, title = "Book 4", pages = 50,age = "12"}
      };
      Result r = collection.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);
      var document = collection.GetOne("1");
      var jsonParams = new { title = "Book 100" };
      r = collection.Modify("age = :age").Patch(jsonParams).
          Bind("age", "12").Execute();//Multiple Records
      Assert.AreEqual(2, r.AffectedItemsCount);
      string jsonParams1 = "{ \"title\": \"Book 400\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      document = collection.GetOne("1");
      Assert.AreEqual("Book 100", document["title"]);
      document = collection.GetOne("4");
      Assert.AreEqual("Book 100", document["title"]);
      document = collection.GetOne("2");
      Assert.AreEqual("Book 400", document["title"]);

      string splName = "A*b";
      jsonParams1 = "{\"data1\":\"" + splName + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      document = collection.GetOne("2");
      Assert.AreEqual(splName, document["data1"]);

      splName = "A/b";
      jsonParams1 = "{\"data1\":\"" + splName + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      document = collection.GetOne("2");
      Assert.AreEqual(splName, document["data1"]);

      splName = "A&b!c@d#e$f%g^h&i(j)k-l+m=0_p~q`r}s{t][.,?/><";
      jsonParams1 = "{\"data1\":\"" + splName + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      document = collection.GetOne("2");
      Assert.AreEqual(splName, document["data1"]);

      //Large Key Length
      string myString = new string('*', 65535);
      jsonParams1 = "{\"data1\":\"" + myString + "\"}";
      r = collection.Modify("age = :age").Patch(jsonParams1).
          Bind("age", "18").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      document = collection.GetOne("2");
      Assert.AreEqual(myString, document["data1"]);

      collection = CreateCollection("test");
      r = collection.Add(documentsAsJsonStrings).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(documentAsJsonString2).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      collection.Modify("_id = :id").Patch("{ \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      collection.Modify("additionalinfo.director.name = :director").
           Patch("{ \"additionalinfo\": { \"director\": { \"field1 11\": " +
                 "\"one\", \"field2 12\": \"two\", \"field3 13\": \"three\" } } }").
           Bind("director", "Sharice Legaspi").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      collection.Modify("_id = :id").Patch("{ \"field 1\": \"one\", \"field 2\": \"two\", \"field 3\": \"three\" }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      collection.Modify("additionalinfo.director.name = :director").
         Patch("{ \"additionalinfo\": { \"director\": { \"field1 11\": " +
                "\"one\", \"field2 12\": \"two\", \"field3 13\": \"three\" } } }").
         Bind("director", "Steven Spielberg").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("one", document["field 1"]);
      Assert.AreEqual("two", document["field 2"]);
      Assert.AreEqual("three", document["field 3"]);
      Assert.AreEqual("one",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field1 11"]);
      Assert.AreEqual("two",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field2 12"]);
      Assert.AreEqual("three",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field3 13"]);

      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.AreEqual("one", document["field 1"]);
      Assert.AreEqual("two", document["field 2"]);
      Assert.AreEqual("three", document["field 3"]);
      Assert.AreEqual("one",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field1 11"]);
      Assert.AreEqual("two",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field2 12"]);
      Assert.AreEqual("three",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["field3 13"]);

      collection.Modify("language = :language").
          Patch("{ \"additionalinfo\": { \"director\": { \"test1\": " +
                "\"one\", \"test2\": \"two\", \"test3\": \"three\" } } }").
          Bind("language", "English").Execute();
      collection.Modify("language = :language").
          Patch("{ \"field 1\": \"check1\", \"field 2\": \"check2\", \"field 3\": \"check3\" }").
          Bind("language", "English").Execute();
      //Multiple Records
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("check1", document["field 1"]);
      Assert.AreEqual("check2", document["field 2"]);
      Assert.AreEqual("check3", document["field 3"]);
      Assert.AreEqual("one",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test1"]);
      Assert.AreEqual("two",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test2"]);
      Assert.AreEqual("three",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test3"]);
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.AreEqual("check1", document["field 1"]);
      Assert.AreEqual("check2", document["field 2"]);
      Assert.AreEqual("check3", document["field 3"]);
      Assert.AreEqual("one",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test1"]);
      Assert.AreEqual("two",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test2"]);
      Assert.AreEqual("three",
          (((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"]))["test3"]);

      collection = CreateCollection("test");
      var data2 = new DbDoc(@"{ ""_id"": -1, ""pages"": 20,
                          ""books"": [
                            {""_id"" : 10, ""title"" : ""Book 10""},
                            { ""_id"" : 20, ""title"" : ""Book 20"" }
                          ]
                      }");
      r = collection.Add(data2).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      document = collection.GetOne("-1");
      Assert.AreEqual("20", document["pages"].ToString());
      collection.Modify("_id = :id").Patch("{ \"pages\": \"200\" }").
          Bind("id", -1).Execute();
      document = collection.GetOne("-1");
      Assert.AreEqual("200", document["pages"]);
      data2 = new DbDoc(@"{ ""_id"": 1, ""pages"": 20,
                          ""books"": [
                            {""_id"" : 10, ""title"" : ""Book 10""},
                            { ""_id"" : 20, ""title"" : ""Book 20"" }
                          ]
                      }");
      r = collection.Add(data2).Execute();
      collection.Modify("_id = :id").
          Patch("{ \"books\": [{ \"_id\": \"11\",\"title\":\"Ganges\"}]}").
          Bind("id", "1").Execute();
      document = collection.GetOne("1");
      object[] books = document["books"] as object[];
      Assert.AreEqual(1, books.Length);
      Dictionary<string, object> book1 = books[0] as Dictionary<string, object>;
      Assert.AreEqual("11", book1["_id"]);
      Assert.AreEqual("Ganges", book1["title"]);
      var t1 =
          "{\"_id\": \"1\", \"name\": \"Alice\", \"address\": [{\"zip\": \"12345\", \"city\": \"Los Angeles\", \"street\": \"32 Main str\"}]}";
      var t2 =
          "{\"_id\": \"2\", \"name\": \"Bob\", \"address\": [{\"zip\": \"325226\", \"city\": \"San Francisco\", \"street\": \"42 2nd str\"}]}";
      var t3 =
          "{\"_id\": \"3\", \"name\": \"Bob\", \"address\": [{\"zip1\": \"325226\", \"city1\": \"San Francisco\", \"street1\": \"42 2nd str\"}," +
          "{\"zip2\": \"325226\", \"city2\": \"San Francisco\", \"street2\": \"42 2nd str\"}]}";

      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(t2).Execute();
      //update the name and zip code of match
      collection.Modify("_id = :id").Patch("{\"name\": \"Joe\", \"address\": [{\"zip\":\"91234\"}]}").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("Joe", document["name"]);
      object[] address = document["address"] as object[];
      Assert.AreEqual(1, address.Length);
      Dictionary<string, object> address1 = address[0] as Dictionary<string, object>;
      Assert.AreEqual("91234", address1["zip"]);

      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(t2).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(t3).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("_id = :id").Patch("{\"name\": \"Joe\", \"address\": [{\"zip1\":\"91234\"},{\"zip2\":\"10000\"}]}").
          Bind("id", "3").Execute();
      document = collection.GetOne("3");
      Assert.AreEqual("Joe", document["name"]);
      address = document["address"] as object[];
      Assert.AreEqual(2, address.Length);
      address1 = address[0] as Dictionary<string, object>;
      Assert.AreEqual("91234", address1["zip1"]);
      address1 = address[1] as Dictionary<string, object>;
      Assert.AreEqual("10000", address1["zip2"]);

      collection = CreateCollection("test");
      var t = "{\"_id\": \"id1004\", \"age\": 1, \"misc\": 1.2, \"name\": { \"last\": \"ABCDEF3\", \"first\": \"ABCDEF1\", \"middle\": { \"middle1\": \"ABCDEF21\", \"middle2\": \"ABCDEF22\"}}}";
      r = collection.Add(t).Execute();
      collection.Modify("_id = :id").Patch("{\"name\":{\"middle\":{\"middle1\": {\"middle11\" : \"ABCDEF211\", \"middle12\" : \"ABCDEF212\", \"middle13\" : \"ABCDEF213\"}}}}").Bind("id", "id1004").Execute();
      document = collection.GetOne("id1004");
      Assert.AreEqual("ABCDEF211", document["name.middle.middle1.middle11"]);
      Assert.AreEqual("ABCDEF212", document["name.middle.middle1.middle12"]);
      Assert.AreEqual("ABCDEF213", document["name.middle.middle1.middle13"]);

      t1 = "{\"_id\": \"1001\", \"ARR\":[1,2,3], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      t2 = "{\"_id\": \"1002\", \"ARR\":[1,1,2], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      t3 = "{\"_id\": \"1003\", \"ARR\":[1,4,5], \"ARR1\":[\"name1\",\"name2\", \"name3\"]}";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(t2).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(t3).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("_id = :_id").
          Patch("{\"ARR\":[6,8,3],\"ARR1\":[\"changed name1\",\"changed name2\", \"changed name3\"]}").
          Bind("_id", "1001").Execute();
      document = collection.GetOne("1001");

      object[] arr = document["ARR1"] as object[];
      Assert.AreEqual(3, arr.Length);
      int j = 1;
      for (int i = 0; i < arr.Length; i++)
      {
        Assert.AreEqual("changed name" + j, arr[i]);
        j++;
      }

      t1 =
          "{\"_id\": \"1\", \"name\": \"Alice\", \"address\": {\"zip\": \"12345\", \"city\": \"Los Angeles\", \"street\": \"32 Main str\"}}";
      t2 =
          "{\"_id\": \"2\", \"name\": \"Bob\", \"address\": [{\"zip\": \"325226\", \"city\": \"San Francisco\", \"street\": \"42 2nd str\"}]}";
      t3 =
          "{\"_id\": \"3\", \"name\": \"Bob\", \"address\": [{\"zip1\": \"325226\", \"city1\": \"San Francisco\", \"street1\": \"42 2nd str\"}," +
          "{\"zip2\": \"325226\", \"city2\": \"San Francisco\", \"street2\": \"42 2nd str\"}]}";

      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(t2).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      r = collection.Add(t3).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      collection.Modify("_id = :id").
          Patch("{\"address\": null, \"zip\": $.address.zip, \"street\": $.address.street, \"city\": upper($.address.city)}").Bind("id", "1").
          Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("Alice", document["name"]);
      Assert.AreEqual("32 Main str", document["street"]);
      Assert.AreEqual("LOS ANGELES", document["city"]);
      Assert.AreEqual("12345", document["zip"]);

      t1 =
          "{\"_id\": \"1\", \"name\": \"Alice\", \"address\": {\"test\":{\"zip\": \"12345\", " +
          "\"city\": \"Los Angeles\", \"street\": \"32 Main str\"}}}";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("_id = :id").
          Patch("{\"address\":{\"test\": null,\"zip\":$.address.test.zip,\"city\": lower($.address.test.city)}}").Bind("id", "1").
          Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("Alice", document["name"]);
      Assert.AreEqual("los angeles", document["address.city"]);
      Assert.AreEqual("12345", document["address.zip"]);

    }

    [Test, Description("Test valid modify.patch to change element at Depth 5 for multiple arrays))")]
    public void ModifyInNestedObjects()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      string json = "";
      int i = 0, j = 0, k = 0, l = 0, m = 0, n = 0, maxFld = 10;
      var collection = CreateCollection("test");
      int maxdepth = 5;
      json = "{\"_id\":\"1002\",\"XYZ\":1111";
      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"ARR" + j + "\":[";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + i + ",[";
        }
        json = json + i;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "]," + i;
        }
        json = json + "]";
      }
      json = json + "}";

      collection.Add(json).Execute();

      json = "{\"_id\":\"1003\",\"XYZ\":2222";
      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"DATAX" + j + "\":";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + "{\"D" + i + "\":";
        }
        json = json + maxdepth;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "}";
        }
      }
      json = json + "}";

      collection.Add(json).Execute();

      json = "{\"_id\":\"1001\",\"XYZ\":3333";
      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"ARR" + j + "\":[";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + i + ",[";
        }
        json = json + i;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "]," + i;
        }
        json = json + "]";
      }

      for (j = 0; j < maxFld; j++)
      {
        json = json + ",\"DATAX" + j + "\":";
        for (i = 0; i < maxdepth; i++)
        {
          json = json + "{\"D" + i + "\":";
        }
        json = json + maxdepth;
        for (i = maxdepth - 1; i >= 0; i--)
        {
          json = json + "}";
        }
      }
      json = json + "}";

      collection.Add(json).Execute();

      //Update each array one by one
      for (n = 1; n < 2; n++)
      {
        collection.Modify("_id = :_id").
        Patch("{\"ARR" + i + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]}").
        Bind("_id", "1002").Execute();
        var document1 = collection.GetOne("1002");
        var test = "ARR" + i;
        object[] arr1 = document1[test] as object[];
        Assert.AreEqual(3, arr1.Length);
        i = 0;
        j = 0;
        for (i = 0; i < arr1.Length; i++)
        {

          if (i == 1)
          {
            object[] arr2 = arr1[1] as object[];
            Assert.AreEqual(3, arr2.Length);
            for (j = 0; j < arr2.Length; j++)
            {

              if (j == 1)
              {
                object[] arr3 = arr2[1] as object[];
                Assert.AreEqual(3, arr3.Length);
                for (k = 0; k < arr3.Length; k++)
                {

                  if (k == 1)
                  {
                    object[] arr4 = arr3[1] as object[];
                    Assert.AreEqual(3, arr4.Length);
                    for (l = 0; l < arr4.Length; l++)
                    {

                      if (l == 1)
                      {
                        object[] arr5 = arr4[1] as object[];
                        Assert.AreEqual(3, arr5.Length);
                        for (m = 0; m < arr5.Length; m++)
                        {

                          if (m == 1)
                          {
                            object[] arr6 = arr5[1] as object[];
                            Assert.AreEqual(1, arr6.Length);
                            if (arr6.Length == 1)
                              Assert.AreEqual(4, arr6[0]);
                          }
                          else
                            Assert.AreEqual(5, arr5[m]);
                          if (m == 2)
                            break;
                        }
                      }
                      else
                        Assert.AreEqual(6, arr4[l]);
                      if (l == 2)
                        break;

                    }
                  }
                  else
                    Assert.AreEqual(7, arr3[k]);
                  if (k == 2)
                    break;
                }
              }
              if (j == 0 || j == 2)
                Assert.AreEqual(8, arr2[j]);
              if (j == 2)
                break;
            }
          }
          else
            Assert.AreEqual(9, arr1[i]);
          if (i == 2)
            break;
        }
      }
      //Update each array at one shot
      string updatedPatch = "{\"ARR" + 0 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 1 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 2 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 3 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 4 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 5 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 6 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 7 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 8 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]," +
                "\"ARR" + 9 + "\":[9,[8,[7,[6,[5,[4],5],6],7],8],9]}";

      collection.Modify("_id = :_id").
         Patch(updatedPatch).
         Bind("_id", "1002").Execute();
      var document = collection.GetOne("1002");
      for (n = 0; n < 10; n++)
      {
        var test = "ARR" + i;
        object[] arr1 = document[test] as object[];
        Assert.AreEqual(3, arr1.Length);
        i = 0;
        j = 0;
        for (i = 0; i < arr1.Length; i++)
        {

          if (i == 1)
          {
            object[] arr2 = arr1[1] as object[];
            Assert.AreEqual(3, arr2.Length);
            for (j = 0; j < arr2.Length; j++)
            {

              if (j == 1)
              {
                object[] arr3 = arr2[1] as object[];
                Assert.AreEqual(3, arr3.Length);
                for (k = 0; k < arr3.Length; k++)
                {

                  if (k == 1)
                  {
                    object[] arr4 = arr3[1] as object[];
                    Assert.AreEqual(3, arr4.Length);
                    for (l = 0; l < arr4.Length; l++)
                    {

                      if (l == 1)
                      {
                        object[] arr5 = arr4[1] as object[];
                        Assert.AreEqual(3, arr5.Length);
                        for (m = 0; m < arr5.Length; m++)
                        {

                          if (m == 1)
                          {
                            object[] arr6 = arr5[1] as object[];
                            Assert.AreEqual(1, arr6.Length);
                            if (arr6.Length == 1)
                              Assert.AreEqual(4, arr6[0]);
                          }
                          else
                            Assert.AreEqual(5, arr5[m]);
                          if (m == 2)
                            break;
                        }
                      }
                      else
                        Assert.AreEqual(6, arr4[l]);
                      if (l == 2)
                        break;

                    }
                  }
                  else
                    Assert.AreEqual(7, arr3[k]);
                  if (k == 2)
                    break;
                }
              }
              if (j == 0 || j == 2)
                Assert.AreEqual(8, arr2[j]);
              if (j == 2)
                break;
            }
          }
          else
            Assert.AreEqual(9, arr1[i]);
          if (i == 2)
            break;
        }
      }

      // Replace/Update in different Nesting Depths
      List<DbDoc> listDocs = new List<DbDoc>();
      listDocs.Add(documentsAsDbDocs[0]);
      listDocs.Add(new DbDoc(documentAsJsonString2));
      collection = CreateCollection("test");
      Result r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("AFRICAN EGG", document["title"]);
      // Modify Using ID and Patch Title
      collection.Modify("_id = :id").Patch("{ \"title\": \"The African Egg\" }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("The African Egg", document["title"]);

      Assert.AreEqual(57, ((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"])["age"]);
      // Modify Using additionalinfo.director.name = :director and Patch Age 67
      collection.Modify("additionalinfo.director.name = :director").
          Patch("{ \"additionalinfo\": { \"director\": { \"age\": 67 } } }").
          Bind("director", "Sharice Legaspi").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual(67, ((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"])["age"]);

      // Modify Using _id=:id and Patch Age 77
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"age\": 77 } } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual(77, ((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["director"])["age"]);

      // Modify Using _id=:id and Patch contains movie title
      collection.Modify("_id = :id").
          Patch("{ \"title\": { \"movie\": \"The African Egg\"} }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("The African Egg", ((Dictionary<string, object>)document["title"])["movie"]);

      // Modify Using _id=:id and Patch additionalinfo Birthplace director country countryActual
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"birthplace\": { \"country\": { \"countryActual\": \"India\" } } } } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.AreEqual("India", document["additionalinfo.director.birthplace.country.countryActual"]);

      // Modify Using _id=:id and Patch additionalinfo Birthplace director country countryActual"
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"birthplace\": { \"country\": { \"city\": \"NewDelhi\",\"countryActual\": \"India\" } } } } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.AreEqual("India", document["additionalinfo.director.birthplace.country.countryActual"]);
      Assert.AreEqual("NewDelhi", document["additionalinfo.director.birthplace.country.city"]);

      // Modify Using _id=:id and Patch additionalinfo Birthplace director country to replace an array
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"director\": { \"birthplace\": { \"country\": \"India\" } } } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.AreEqual("India", document["additionalinfo.director.birthplace.country"]);

      // Modify Using _id=:id and Patch additionalinfo writers to replace an array
      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"writers\": \"Jeoff Archer\" } }").
          Bind("id", "123456789asdferdfghhghjh12334").Execute();
      document = collection.GetOne("123456789asdferdfghhghjh12334");
      Assert.AreEqual("Jeoff Archer", document["additionalinfo.writers"]);

      //Reset the docs
      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      // Modify Using _id=:id and Patch contains blank
      collection.Modify("_id = :id").Patch("{ \"additionalinfo\": \"\" }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");


    }

    [Test, Description("Test valid modify.patch to add/remove fields at Depth for multiple arrays.))")]
    public void ModifyPatchAtDepthMultipleArrays_S1()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      Collection collection = CreateCollection("test");
      List<DbDoc> listDocs = new List<DbDoc>();
      listDocs.Add(documentsAsDbDocs[0]);
      listDocs.Add(new DbDoc(documentAsJsonString2));
      Result r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);
      object[] path = null;
      DbDoc document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path = document["translations"] as object[]);

      collection.Modify("_id = :id").
          Patch("{ \"translations\": [\"Spanish\"] }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      path = document["translations"] as object[];
      Assert.AreEqual("Spanish", path[0],
          "Verify transalations field is present or not");
      collection.Modify("_id = :id").
          Patch("{ \"translations\": null }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path = document["translations"] as object[]);
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.mobile"]);

      collection.Modify("additionalinfo.director.age = :age").
          Patch("{ \"additionalinfo\": { \"director\": { \"mobile\": \"9876543210\" } } }").
          Bind("age", 57).Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("9876543210", document["additionalinfo.director.mobile"],
          "Verify mobile field is present or not");
      collection.Modify("additionalinfo.director.name = :director").
          Patch("{ \"additionalinfo\": { \"director\": { \"mobile\": null } } }").
          Bind("director", "Sharice Legaspi").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.mobile"]);
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.musicby"]);

      collection.Modify("_id = :id").
          Patch("{ \"additionalinfo\": { \"musicby\": \"The Sakila\" } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("The Sakila", document["additionalinfo.musicby"],
       "Verify musicby field is present or not");
      collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"musicby\": null } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.musicby"]);
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.awards.genre"]);

      collection.Modify("_id = :id").
          Patch("{\"additionalinfo\": { \"director\": { \"awards\": { \"genre\": \"Action\" } } } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("Action", document["additionalinfo.director.awards.genre"],
       "Verify musicby field is present or not");
      collection.Modify("_id = :id").
          Patch("{\"additionalinfo\": { \"director\": { \"awards\": { \"genre\": null } } } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => path[0] = document["additionalinfo.director.awards.genre"]);

    }

    [Test, Description("Test valid modify.patch to use expr to change existing keys in document))")]
    public void ModifyPatchAtDepthMultipleArrays_S2()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      DbDoc document = null;
      Collection collection = CreateCollection("test");
      Result r = collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", " +
          "\"startDate\": \"4/1/2017\" }").Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      var patch1 = new
      {
        email = new MySqlExpression("UPPER($.email)")
      };
      var patch2 = new
      {
        email = new MySqlExpression("LOWER($.email)")
      };
      var patch3 = new
      {
        email = new MySqlExpression("CONCAT('No', 'S', 'QL')")
      };
      var patch4 = new
      {
        email = new MySqlExpression("CHAR(77, 121, 83, 81, '76')")
      };
      var patch5 = new
      {
        email = new MySqlExpression("CONCAT('My', NULL, 'QL')")
      };
      var patch6 = new
      {
        email = new MySqlExpression("ELT(4, 'ej', 'Heja', 'hej', 'foo')")
      };
      var patch7 = new
      {
        email = new MySqlExpression("REPEAT('MySQL', 3)")
      };
      var patch8 = new
      {
        email = new MySqlExpression("REVERSE('abc')")
      };
      var patch9 = new
      {
        email = new MySqlExpression("RIGHT('foobarbar', 4)")
      };
      var patch10 = new
      {
        email = new MySqlExpression(" REPLACE('www.mysql.com', 'w', 'Ww')")
      };
      var patch11 = new
      {
        email = new MySqlExpression(" HEX('abc')")
      };
      var patch12 = new
      {
        email = new MySqlExpression(" BIN(12)")
      };

      collection.Modify("true").Patch(patch1).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("ALICE@ORA.COM", document["email"]);
      collection.Modify("true").Patch(patch2).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("alice@ora.com", document["email"]);
      collection.Modify("true").Patch(patch3).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("NoSQL", document["email"]);
      collection.Modify("true").Patch(patch4).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("base64:type15:TXlTUUw=", document["email"]);
      collection.Modify("true").Patch(patch5).Execute();
      document = collection.GetOne("123");
      DbDoc test = null;
      Assert.Throws<InvalidOperationException>(()=> test = (DbDoc)document["email"]);

      collection = CreateCollection("test");
      r = collection.Add("{ \"_id\": \"123\", \"email\": \"alice@ora.com\", " +
          "\"startDate\": \"4/1/2017\" }").Execute();
      collection.Modify("true").Patch(patch6).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("foo", document["email"]);
      collection.Modify("true").Patch(patch7).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("MySQLMySQLMySQL", document["email"]);
      collection.Modify("true").Patch(patch8).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("cba", document["email"]);
      collection.Modify("true").Patch(patch9).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("rbar", document["email"]);
      collection.Modify("true").Patch(patch10).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("WwWwWw.mysql.com", document["email"]);
      collection.Modify("true").Patch(patch11).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("616263", document["email"]);
      collection.Modify("true").Patch(patch12).Execute();
      document = collection.GetOne("123");
      Assert.AreEqual("1100", document["email"]);

      string t1 = "{\"_id\": \"1\", \"name\": \"Alice\" }";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": YEAR('2000-01-01') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(2000, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MONTH('2008-02-03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(2, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": WEEK('2008-02-20') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(7, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DAY('2008-02-20') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(20, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": HOUR('10:05:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(10, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MINUTE('2008-02-03 10:05:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(5, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": SECOND('10:05:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(3, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": MICROSECOND('12:00:00.123456') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(123456, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": QUARTER('2008-04-01') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual(2, document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": TIME('2003-12-31 01:02:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("01:02:03.000000", document["dateAndTimeValue"]);

      collection.Modify("_id = :id").Patch("{ \"dateAndTimeValue\": DATE('2003-12-31 01:02:03') }").Bind("id", "1").Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("2003-12-31", document["dateAndTimeValue"]);

      List<DbDoc> listDocs = new List<DbDoc>();
      listDocs.Add(documentsAsDbDocs[0]);
      listDocs.Add(new DbDoc(documentAsJsonString2));
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      object[] actors = document["actors"] as object[];
      Assert.AreEqual(3, actors.Length);
      Dictionary<string, object> actor0 = actors[0] as Dictionary<string, object>;

      Assert.Throws<KeyNotFoundException>(() => test = (DbDoc)actor0["age"]);

      t1 =
     "{\"_id\": \"1\", \"name\": \"Alice\", \"details\": {\"personal\":{\"mobile\": \"9876543210\", " +
     "\"DOB\": \"19 Mar 1982\", \"citizen\": \"USA\"}}}";
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"yearofbirth\":" +
          "CAST(SUBSTRING_INDEX($.details.personal.DOB, ' ', -1) AS DECIMAL)}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("Alice", document["name"]);
      Assert.AreEqual("9876543210", document["details.mobile"]);
      Assert.AreEqual("1982", document["details.yearofbirth"].ToString());
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"currentyear\":" +
          "CURDATE()}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("Alice", document["name"]);
      Assert.AreEqual("9876543210", document["details.mobile"]);
      Assert.AreEqual(document["details.currentyear"].ToString(), document["details.currentyear"].ToString(), "Matching the current date");
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"currentyear\":" +
          "Year(CURDATE())}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("Alice", document["name"]);
      Assert.AreEqual("9876543210", document["details.mobile"]);
      Assert.AreEqual(document["details.currentyear"].ToString(), document["details.currentyear"].ToString());
      collection = CreateCollection("test");
      r = collection.Add(t1).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);
      collection.Modify("_id = :id").
          Patch("{\"details\":{\"personal\": null,\"mobile\":$.details.personal.mobile,\"currentage\":" +
          "Year(CURDATE()) - CAST(SUBSTRING_INDEX($.details.personal.DOB, ' ', -1) AS DECIMAL)}}").Bind("id", "1").
         Execute();
      document = collection.GetOne("1");
      Assert.AreEqual("Alice", document["name"]);
      Assert.AreEqual("9876543210", document["details.mobile"]);
      Assert.AreEqual(document["details.currentage"].ToString(), document["details.currentage"].ToString());

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      // Get root array.
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["audio"]);

      collection.Modify("true").
          Patch("{ \"audio\": CONCAT($.language, ', no subtitles') }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("English, no subtitles", document["audio"]);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["audio"]);

      collection.Modify("true").Patch("{ \"audio\": CONCAT(UPPER($.language), ', No Subtitles') }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("ENGLISH, No Subtitles", document["audio"]);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      collection.Modify("_id = :id").Patch("{ \"_id\": replace(UUID(), '-', '') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("a6f4b93e1a264a108393524f29546a8c", document.Id);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["additionalinfo._id"]);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      collection.Modify("_id = :id").Patch("{ \"additionalinfo\": { \"_id\": replace(UUID(), '-', '') } }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.True(((Dictionary<string, object>)document["additionalinfo"])["_id"] != null);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["additionalinfo.releasedate"]);

      try
      {
        collection.Modify("_id = :id").
        Patch("{ \"releasedate\": DATE_ADD('2006-04-00',INTERVAL 1 DAY) }").
        Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      }
      catch (MySqlException ex)
      {
        Assert.AreEqual("Invalid data for update operation on document collection table", ex.Message);
      }

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("AFRICAN EGG", document["title"]);
      collection.Modify("_id = :id").Patch("{ \"title\": concat('my ', NULL, ' title') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["title"]);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.Throws<InvalidOperationException>(() => test = (DbDoc)document["docfield"]);

      collection.Modify("_id = :id").Patch("{ \"docfield\": JSON_OBJECT('field 1', 1, 'field 2', 'two') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      object docField = document["docfield"] as object;

      Assert.True(docField != null);

      collection = CreateCollection("test");
      r = collection.Add(listDocs.ToArray()).Execute();
      Assert.AreEqual(2, r.AffectedItemsCount);

      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("Science fiction", document["genre"]);

      collection.Modify("_id = :id").Patch("{ \"genre\": JSON_OBJECT('name', 'Science Fiction') }").
          Bind("id", "a6f4b93e1a264a108393524f29546a8c").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      Assert.AreEqual("Science Fiction", ((Dictionary<string, object>)document["genre"])["name"]);

    }

    [Test, Description("Test invalid modify.patch to attempt to use invalid string (non JSON string/empty/NULL) as patch string.))")]
    public void ModifyPatchInvalidString()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher.");
      var collection = CreateCollection("test");
      var docs = new[]
      {
        new {_id = 1, title = "Book 1", pages = 20, age = 12},
        new {_id = 2, title = "Book 2", pages = 30,age = 18},
        new {_id = 3, title = "Book 3", pages = 40,age = 34},
        new {_id = 4, title = "Book 4", pages = 50,age = 12}
      };
      Result r = collection.Add(docs).Execute();
      Assert.AreEqual(4, r.AffectedItemsCount);
      var document = collection.GetOne("1");
      var jsonParams = "invalidJsonString";
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("age = :age").Patch(jsonParams).Bind("age", "12")));

      jsonParams = "{invalidJsonString}";
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("age = :age").Patch(jsonParams).Bind("age", "12")));

      jsonParams = "{\"_id\":\"1004\",\"F1\": ] }";
      Assert.Throws<Exception>(() => ExecuteModifyStatement(collection.Modify("age = :age").Patch(jsonParams).Bind("age", "12")));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch(null)));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch("")));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch(" ")));

      Assert.Throws<ArgumentNullException>(() => ExecuteModifyStatement(collection.Modify("true").Patch(string.Empty)));


      r = collection.Add(documentsAsJsonStrings).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      collection.Modify("true").Patch("{ \"_id\": NULL }").Execute();
      Assert.AreEqual("1", collection.Find().Execute().FetchOne().Id.ToString());

      collection = CreateCollection("test");
      r = collection.Add(documentsAsJsonStrings).Execute();
      Assert.AreEqual(1, r.AffectedItemsCount);

      collection.Modify("true").Patch("{ \"nullfield\": NULL }").Execute();
      DbDoc test = null;
      Assert.Throws<InvalidOperationException> (() => test = (DbDoc)collection.Find().Execute().FetchOne()["nullfield"]);

      collection.Modify("true").Patch("{ \"nullfield\": [NULL, NULL] }").Execute();
      document = collection.Find().Execute().FetchOne();
      var nullArray = (object[])document["nullfield"];
      Assert.AreEqual(null, nullArray[0]);
      Assert.AreEqual(null, nullArray[1]);

      collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": NULL } }").Execute();
      document = collection.Find().Execute().FetchOne();
      Assert.AreEqual(true, ((Dictionary<string, object>)document["nullfield"]).Count == 0);

      collection.Modify("true").Patch("{ \"nullfield\": { \"nested\": [NULL, NULL] } }").Execute();
      document = collection.Find().Execute().FetchOne();
      var nestedNullArray = (object[])((Dictionary<string, object>)document["nullfield"])["nested"];
      Assert.AreEqual(null, nestedNullArray[0]);
      Assert.AreEqual(null, nestedNullArray[1]);

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": NULL } }").Execute();
      Dictionary<string, object> test2 = null;
      Assert.Throws<InvalidOperationException>(() => test2 = ((Dictionary<string, object>)collection.Find().Execute().FetchOne()["additionalinfo.nullfield"]));

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": [NULL, NULL] } }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      nestedNullArray = (object[])((Dictionary<string, object>)document["additionalinfo"])["nullfield"];
      Assert.AreEqual(null, nestedNullArray[0]);
      Assert.AreEqual(null, nestedNullArray[1]);

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": [NULL, NULL] } } }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");

      nestedNullArray = (object[])((((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo"])["nullfield"]))["nested"]);
      Assert.AreEqual(null, nestedNullArray[0]);
      Assert.AreEqual(null, nestedNullArray[1]);

      collection.Modify("true").Patch("{ \"additionalinfo\": { \"nullfield\": { \"nested\": JSON_OBJECT('field', null) } } }").Execute();
      document = collection.GetOne("a6f4b93e1a264a108393524f29546a8c");
      var nestedObject = (Dictionary<string, object>)((Dictionary<string, object>)((Dictionary<string, object>)document["additionalinfo.nullfield.nested"]));
      Assert.AreEqual(0, nestedObject.Count);
    }

    #endregion WL14389

  }
}
