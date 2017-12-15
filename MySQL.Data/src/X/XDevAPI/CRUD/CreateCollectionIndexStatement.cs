// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Implementation class for CRUD statements with collections using an index.
  /// </summary> 
  public class CreateCollectionIndexStatement : CrudStatement<Result>
  {
    internal CreateIndexParams createIndexParams;

    // Fields allowed at the root level.
    private readonly string[] allowedFields = new string[]{ "fields", "type" };

    // Fields allowed for embedded documents.
    private readonly string[] allowedInternalFields = new string[] { "field", "type", "required", "options", "srid" };

    private readonly string[] allowedIndexTypes = new string[] { "INDEX", "SPATIAL" };

    private readonly string[] allowedFieldTypes = new string[] {
      "INT", "TINYINT", "SMALLINT", "MEDIUMINT", "INTEGER", "BIGINT",
      "REAL", "FLOAT", "DOUBLE", "DECIMAL", "NUMERIC",
      "INT UNSIGNED", "TINYINT UNSIGNED", "SMALLINT UNSIGNED", "MEDIUMINT UNSIGNED", "INTEGER UNSIGNED", "BIGINT UNSIGNED",
      "REAL UNSIGNED", "FLOAT UNSIGNED", "DOUBLE UNSIGNED", "DECIMAL UNSIGNED", "NUMERIC UNSIGNED",
      "DATE", "TIME", "TIMESTAMP", "DATETIME", "TEXT", "GEOJSON" };

    internal CreateCollectionIndexStatement(Collection collection, string indexName, DbDoc indexDefinition) : base(collection)
    {
      // Validate the index follows the allowed format.
      if (!indexDefinition.values.ContainsKey(allowedFields[0]))
        throw new FormatException(string.Format(ResourcesX.MandatoryFieldNotFound, allowedFields[0]));

      // Validate that fields on the root level are allowed.
      foreach(var field in indexDefinition.values)
      {
        if (!allowedFields.Contains(field.Key))
          throw new FormatException(string.Format(ResourcesX.UnexpectedField, field.Key));
      }

      // Validate the index type.
      if (indexDefinition.values.ContainsKey(allowedFields[1]))
      {
        string indexType = indexDefinition.values[allowedFields[1]].ToString();
        if (!allowedIndexTypes.Contains(indexType))
          throw new FormatException(string.Format(ResourcesX.InvalidIndexType, indexType));
      }

      // Validate that embedded fields are allowed.
      foreach (var item in indexDefinition.values[allowedFields[0]] as Object[])
      {
        var field = item as Dictionary<string, object>;
        if (!field.ContainsKey(allowedInternalFields[0]))
          throw new FormatException(string.Format(ResourcesX.MandatoryFieldNotFound, allowedInternalFields[0]));

        if (!field.ContainsKey(allowedInternalFields[1]))
          throw new FormatException(string.Format(ResourcesX.MandatoryFieldNotFound, allowedInternalFields[1]));

        foreach(var internalField in field)
        {
          if (!allowedInternalFields.Contains(internalField.Key))
            throw new FormatException(string.Format(ResourcesX.UnexpectedField, internalField.Key));
        }

        // Validate field type.
        if (field.ContainsKey(allowedInternalFields[1]))
        {
          string fieldType = field[allowedInternalFields[1]].ToString();
          if (!IsValidFieldType(fieldType))
            throw new FormatException(string.Format(ResourcesX.InvalidFieldType, fieldType));
        }
      }

      createIndexParams = new CreateIndexParams(indexName, indexDefinition);
    }

    /// <summary>
    /// Executes this statement.
    /// </summary>
    /// <returns>A <see cref="Result"/> object containing the results of the execution.</returns>
    public override Result Execute()
    {
      return Session.XSession.CreateCollectionIndex(this);
    }

    private bool IsValidFieldType(string fieldType)
    {
      if (fieldType.StartsWith("TEXT("))
        return true;
      else
        return allowedFieldTypes.Contains(fieldType);
    }
  }
}
