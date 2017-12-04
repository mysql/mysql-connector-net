// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;

namespace MySqlX.XDevAPI.CRUD
{
  internal class CreateIndexParams
  {
    private string _indexName;
    private bool _unique;
    private string _type;
    private List<IndexField> _fields = new List<IndexField>();

    public CreateIndexParams(string indexName, DbDoc indexDefinition)
    {
      this._indexName = indexName;

      if (indexDefinition.values.ContainsKey("unique"))
        this._unique = Convert.ToBoolean(indexDefinition.values["unique"]);

      if (indexDefinition.values.ContainsKey("type"))
        this._type = indexDefinition.values["type"].ToString();

      // Read fields from the indexDefinition object.
      foreach (var item in indexDefinition.values["fields"] as Object[])
      {
        var field = item as Dictionary<string, object>;
        if (field == null) continue;

        var indexField = new IndexField()
        {
          Field = field["field"].ToString(),
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
      private string _field;
      private string _type;
      private bool? _required;
      private uint? _options;
      private uint? _srid;

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

    public bool IsUnique
    {
      get { return this._unique; }
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
