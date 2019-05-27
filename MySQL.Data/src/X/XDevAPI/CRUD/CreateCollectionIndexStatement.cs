// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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

namespace MySqlX.XDevAPI.CRUD
{
  /// <summary>
  /// Implementation class for CRUD statements with collections using an index.
  /// </summary> 
  public class CreateCollectionIndexStatement : CrudStatement<Result>
  {
    internal CreateIndexParams createIndexParams;

    internal CreateCollectionIndexStatement(Collection collection, string indexName, DbDoc indexDefinition) : base(collection)
    {
      // Fields allowed at the root level.
      var allowedFields = new string[]{ "fields", "type" };

      // Fields allowed for embedded documents.
      var allowedInternalFields = new string[] { "field", "type", "required", "options", "srid", "array" };

      // Validate the index follows the allowed format.
      if (!indexDefinition.values.ContainsKey(allowedFields[0]))
        throw new FormatException(string.Format(ResourcesX.MandatoryFieldNotFound, allowedFields[0]));

      // Validate that fields on the root level are allowed.
      foreach(var field in indexDefinition.values)
      {
        if (!allowedFields.Contains(field.Key))
          throw new FormatException(string.Format(ResourcesX.UnexpectedField, field.Key));
      }

      // Validate embedded documents.
      foreach (var item in indexDefinition.values[allowedFields[0]] as Object[])
      {
        var field = item as Dictionary<string, object>;

        // Validate embedded documents have the field field.
        if (!field.ContainsKey(allowedInternalFields[0]))
          throw new FormatException(string.Format(ResourcesX.MandatoryFieldNotFound, allowedInternalFields[0]));

        // Validate embedded documents have the type field.
        if (!field.ContainsKey(allowedInternalFields[1]))
          throw new FormatException(string.Format(ResourcesX.MandatoryFieldNotFound, allowedInternalFields[1]));

        foreach(var internalField in field)
        {
          if (!allowedInternalFields.Contains(internalField.Key))
            throw new FormatException(string.Format(ResourcesX.UnexpectedField, internalField.Key));
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
      ValidateOpenSession();
      return Session.XSession.CreateCollectionIndex(this);
    }
  }
}
