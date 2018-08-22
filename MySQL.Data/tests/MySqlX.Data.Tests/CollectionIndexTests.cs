// Copyright (c) 2017, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class CollectionIndexTests : BaseTest
  {  
    [Fact]
    public void IncorrectlyFormatedIndexDefinition()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"type\": \"INDEX\" }"));
      Assert.Equal("Field 'fields' is mandatory.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT\" } ], \"unexpectedField\" : false }"));
      Assert.Equal("Field name 'unexpectedField' is not allowed.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"fields\":$.myField, \"types\":\"TEXT\" } ] }"));
      Assert.Equal("Field 'field' is mandatory.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"types\":\"TEXT\" } ] }"));
      Assert.Equal("Field 'type' is mandatory.", ex.Message);

      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT\", \"unexpectedField\" : false } ] }"));
      Assert.Equal("Field name 'unexpectedField' is not allowed.", ex.Message);
    }

    [Fact]
    public void CreateIndexOnSingleFieldWithDefaultOptions()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT(10)\" } ] }");
      ValidateIndex("myIndex", "test", "t10", false, false, false, 1, 10);
    }

    [Fact]
    public void CreateIndexOnSingleFieldWithAllOptions()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\", \"required\": true } ] }");
      ValidateIndex("myIndex", "test", "i", false, true, false, 1);
    }

    [Fact]
    public void CreateIndexOnMultipleFields()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" }, { \"field\":$.myField2, \"type\":\"INT\" }, { \"field\":$.myField3, \"type\":\"INT\" } ] }");
      ValidateIndex("myIndex", "test", "i", false, false, false, 1);
      ValidateIndex("myIndex", "test", "i", false, false, false, 2);
      ValidateIndex("myIndex", "test", "i", false, false, false, 3);
    }

    [Fact]
    public void CreateIndexOnMultipleFieldsWithAllOptions()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" }, { \"field\":$.myField2, \"type\":\"INT\", \"required\":true }, { \"field\":$.myField3, \"type\":\"INT UNSIGNED\", \"required\":false } ] }");
      ValidateIndex("myIndex", "test", "i", false, false, false, 1);
      ValidateIndex("myIndex", "test", "i", false, true, false, 2);
      ValidateIndex("myIndex", "test", "i_u", false, false, true, 3);
    }

    [Fact]
    public void CreateTypeSpecificIndexesCaseInsensitive()
    {
      var collection = CreateCollection("test");

      // Datetime index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"daTEtiME\" } ] }");
      ValidateIndex("myIndex", "test", "dd", false, false, false, 1);

      // Timestamp index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TIMESTAMP\" } ] }");
      ValidateIndex("myIndex", "test", "ds", false, true, false, 1);

      // Time index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"time\" } ] }");
      ValidateIndex("myIndex", "test", "dt", false, false, false, 1);

      // Date index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"DATE\" } ] }");
      ValidateIndex("myIndex", "test", "d", false, false, false, 1);

      // Numeric index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"NUMERIC UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "xn_u", false, false, true, 1);

      // Decimal index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"DECIMAL\" } ] }");
      ValidateIndex("myIndex", "test", "xd", false, false, false, 1);

      // Double index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"double UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "fd_u", false, false, true, 1);

      // Float index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"FLOAT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "f_u", false, false, true, 1);

      // Real index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"REAL UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "fr_u", false, false, true, 1);

      // Bigint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"BIGINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "ib_u", false, false, true, 1);

      // Int index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INTEGER unsigned\" } ] }");
      ValidateIndex("myIndex", "test", "i_u", false, false, true, 1);

      // Mediumint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"MEDIUMINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "im_u", false, false, true, 1);

      // Smallint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"SMALLINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "is_u", false, false, true, 1);

      // Tinyint index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TINYINT UNSIGNED\" } ] }");
      ValidateIndex("myIndex", "test", "it_u", false, false, true, 1);

      // Geojson index.
      collection.DropIndex("myIndex");
      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"required\":true, \"options\":2, \"srid\":4326 } ], \"type\":\"SPATIAL\" }");
      ValidateIndex("myIndex", "test", "gj", false, true, false, 1);
    }

    [Fact]
    public void DropIndex()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT(64)\" }, { \"field\":$.myField2, \"type\":\"TEXT(10)\", \"required\":true }, { \"field\":$.myField3, \"type\":\"INT UNSIGNED\", \"required\":false } ] }");
      ValidateIndex("myIndex", "test", "t64", false, false, false, 1, 64);
      ValidateIndex("myIndex", "test", "t10", false, true, false, 2, 10);
      ValidateIndex("myIndex", "test", "i_u", false, false, true, 3);

      collection.DropIndex("myIndex");
      Exception ex = Assert.Throws<Exception>(() => ValidateIndex("myIndex", "test", "t10", false, false, false, 1, 10));
      Assert.Equal("Index not found.", ex.Message);
      ex = Assert.Throws<Exception>(() => ValidateIndex("myIndex", "test", "t10", false, true, false, 2, 10));
      Assert.Equal("Index not found.", ex.Message);
      ex = Assert.Throws<Exception>(() => ValidateIndex("myIndex", "test", "i_u", false, false, true, 3));
      Assert.Equal("Index not found.", ex.Message);
    }

    [Fact]
    public void CreateIndexWithDuplicateName()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("myUniqueIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ] }");
      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myUniqueIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ] }"));
      Assert.Equal("Duplicate key name 'myUniqueIndex'", ex.Message);
    }

    [Fact]
    public void CreateIndexWithInvalidJsonDocumentDefinition()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<Exception>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\" = $.myField, \"type\" = \"TEXT\" } ] }"));
      Assert.Equal("The value provided is not a valid JSON document. Expected token ':'", ex.Message);
      ex = Assert.Throws<Exception>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"TEXT\" ] }"));
      Assert.Equal("The value provided is not a valid JSON document. Expected token ','", ex.Message);
      ex = Assert.Throws<Exception>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, type:\"TEXT\" } }"));
      Assert.Equal("The value provided is not a valid JSON document. Expected token '\"'", ex.Message);
    }

    [Fact]
    public void CreateIndexWithInvalidJsonDocumentStructure()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\", \"myCustomField\":\"myCustomValue\" } ] }"));
      Assert.Equal("Field name 'myCustomField' is not allowed.", ex.Message);
      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"mytype\":\"INT\" } ] }"));
      Assert.Equal("Field 'type' is mandatory.", ex.Message);
      ex = Assert.Throws<FormatException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"myfield\":$.myField, \"type\":\"INT\" } ] }"));
      Assert.Equal("Field 'field' is mandatory.", ex.Message);
    }

    [Fact]
    public void CreateIndexWithTypeNotIndexOrSpatial()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ], \"type\":\"indexa\" }"));
      Assert.Equal("Argument value 'indexa' for index type is invalid", ex.Message);
      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"INT\" } ], \"type\":\"OTheR\" }"));
      Assert.Equal("Argument value 'OTheR' for index type is invalid", ex.Message);
    }

    [Fact]
    public void CreateSpatialIndexWithRequiredSetToFalse()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"required\":false, \"options\":2, \"srid\":4326 } ], \"type\":\"SPATIAL\" }"));
      Assert.Equal("GEOJSON index requires 'constraint.required: TRUE", ex.Message);
    }

    [Fact]
    public void CreateIndexWithInvalidType()
    {
      var collection = CreateCollection("test");

      // Missing key length for text type.
      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"Text\" } ] }"));
      Assert.EndsWith("used in key specification without a key length", ex.Message);

      // Invalid index types.
      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"Texta\" } ] }"));
      Assert.EndsWith("Invalid or unsupported type specification 'Texta'", ex.Message);
      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"INTO\" } ] }"));
      Assert.EndsWith("Invalid or unsupported type specification 'INTO'", ex.Message);
    }

    [Fact]
    public void CreateIndexWithInvalidGeojsonOptions()
    {
      var collection = CreateCollection("test");

      Exception ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"INT\", \"options\":2, \"srid\":4326 } ] }"));
      Assert.Equal("Unsupported argumet specification for '$.myField'", ex.Message);

      ex = Assert.Throws<MySqlException>(() => collection.CreateIndex("myIndex", "{\"fields\": [ { \"field\":$.myField, \"type\":\"GEOJSON\", \"options\":2, \"srid\":4326 } ], \"type\":\"SPATIAL\" }"));
      Assert.Equal("GEOJSON index requires 'constraint.required: TRUE", ex.Message);
    }

    [Fact]
    public void ValidIndexNames()
    {
      var collection = CreateCollection("test");

      collection.CreateIndex("01myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"TEXT(64)\" } ] }");
      ValidateIndex("01myIndex", "test", "t64", false, false, false, 1, 64);
      collection.DropIndex("01myIndex");

      collection.CreateIndex("!myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"TEXT(64)\" } ] }");
      ValidateIndex("!myIndex", "test", "t64", false, false, false, 1, 64);
      collection.DropIndex("!myIndex");

      collection.CreateIndex("-myIndex", "{ \"fields\": [ { \"field\":\"$.myField\", \"type\":\"TEXT(64)\" } ] }");
      ValidateIndex("-myIndex", "test", "t64", false, false, false, 1, 64);
      collection.DropIndex("-myIndex");
    }

    private void ValidateIndex(string fieldName, string collectionName, string dataType, bool unique, bool required, bool isUnsigned, int sequence, int? length = null)
    {
      bool indexFound = false;
      using (var connection = new MySqlConnection(ConnectionString.Replace("33060", "3306")))
      {
        connection.Open();
        var command = new MySqlCommand("SHOW INDEX FROM test.test", connection);
        var reader = command.ExecuteReader();
        if (!reader.HasRows)
          throw new Exception("No indexes found.");

        while (reader.Read())
        {
          if (fieldName == reader["Key_name"].ToString())
          {
            if (sequence != Convert.ToInt32(reader["Seq_in_index"]))
              continue;

            indexFound = true;
            Assert.Equal(collectionName, reader["Table"]);
            Assert.Equal(unique ? 0 : 1, Convert.ToInt16(reader["Non_unique"]));
            var columnNameTokens = reader["Column_name"].ToString().Split('_');
            Assert.Equal(dataType, isUnsigned ? string.Format("{0}_{1}", columnNameTokens[1], columnNameTokens[2]) : columnNameTokens[1]);
            Assert.Equal(required ? "" : "YES", reader["Null"]);
            if (length != null)
              Assert.Equal(length, Convert.ToInt32(reader["Sub_part"]));
            break;
          }
        }

        if (!indexFound)
          throw new Exception("Index not found.");

        connection.Close();
      }
    }
  }
}
