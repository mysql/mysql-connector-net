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

using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Data.EntityFrameworkCore.Scaffolding.Internal
{
  internal static class MySQLTableSelectionSetExtensions
  {
    public static bool Allows(this TableSelectionSet _tableSelectionSet, string schemaName, string tableName)
    {
      if (_tableSelectionSet == null
          || (_tableSelectionSet.Schemas.Count == 0
          && _tableSelectionSet.Tables.Count == 0))
      {
        return true;
      }

      var result = false;

      if (_tableSelectionSet.Schemas.Count == 0)
        result = true;

      foreach (var schemaSelection in _tableSelectionSet.Schemas)
      {
        if (schemaSelection.Text.Equals(schemaName))
        {
          schemaSelection.IsMatched = true;
          result = true;
        }
      }

      if (_tableSelectionSet.Tables.Count > 0 && result)
      {
        result = false;
        foreach (var tableSelection in _tableSelectionSet.Tables)
        {
          var components = tableSelection.Text.Split('.');
          if (components.Length == 1
              ? components[0].Equals(tableName)
              : components[0].Equals(schemaName) && components[1].Equals(tableName))
          {
            tableSelection.IsMatched = true;
            result = true;
          }
        }
      }

      return result;
    }
  }
}
