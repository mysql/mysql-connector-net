// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;

namespace MySqlX.XDevAPI.CRUD
{
  internal class CreateIndexParams
  {
    private string _indexName;
    private string _type;
    private List<IndexField> _fields = new List<IndexField>();

    public CreateIndexParams(string indexName, DbDoc indexDefinition)
    {
      this._indexName = indexName;

      if (indexDefinition.values.ContainsKey("type"))
        this._type = indexDefinition.values["type"].ToString();

      // Read fields from the indexDefinition object.
      foreach (var item in indexDefinition.values["fields"] as Object[])
      {
        var field = item as Dictionary<string, object>;
        if (field == null) continue;

        var fieldValue = field["field"] is MySqlExpression ? ((MySqlExpression) field["field"]).value : field["field"].ToString();
        var indexField = new IndexField()
        {
          Field = fieldValue
        };
        if (field.ContainsKey("type"))
          indexField.Type = field["type"].ToString();

        if (field.ContainsKey("required"))
          indexField.Required = Convert.ToBoolean(field["required"]);

        if (field.ContainsKey("options"))
          indexField.Options = Convert.ToUInt32(field["options"]);

        if (field.ContainsKey("srid"))
          indexField.Srid = Convert.ToUInt32(field["srid"]);

        _fields.Add(indexField);
      }
    }

    internal class IndexField
    {
      internal string Field { get; set; }
      internal string Type { get; set; }
      internal bool? Required { get; set; }
      internal uint? Options { get; set; }
      internal uint? Srid { get; set; }
    }

    public string IndexName
    {
      get { return this._indexName; }
    }

    public string Type
    {
      get { return this._type; }
    }

    public List<IndexField> Fields
    {
      get { return this._fields; }
    }
  }
}
