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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MySql.Serialization
{
  public class JsonSerializer
  {
    public static List<string> ToJson(object[] values)
    {
      List<string> jsonValues = new List<string>();
      foreach (object val in values)
        jsonValues.Add(ToJson(val));
      return jsonValues;
    }

    public static string ToJson(object value)
    {
      ///TODO:  see if we can improve on this
      string json = JsonConvert.SerializeObject(value);
      return EnsureId(json);
    }

    public static List<string> EnsureId(string[] jsonValues)
    {
      List<string> newValues = new List<string>();
      foreach (string jsonValue in jsonValues)
        newValues.Add(EnsureId(jsonValue));
      return newValues;
    }

    public static string EnsureId(string json)
    {
      Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
      if (!dict.ContainsKey("_id"))
        dict["_id"] = Guid.NewGuid().ToString("N");
      return JsonConvert.SerializeObject(dict);
    }
  }
}
