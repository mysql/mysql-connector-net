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

using MySql.Properties;
using MySql.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MySql.XDevAPI
{
  public class JsonDoc
  {
    private Dictionary<string, object> values = new Dictionary<string, object>();

    public JsonDoc(object val = null)
    {
      if (val != null)
      {
        if (val is string)
          values = JsonParser.Parse(val as string);
        else
          values = ParseObject(val);
      }
    }

    public string this[string path]
    {
      get { return GetValue(path); }
    }

    public object Id
    {
      get { return values["_id"];  }
    }
    public bool HasId
    {
      get { return values.ContainsKey("_id"); }
    }

    public void EnsureId()
    {
      if (!HasId)
        SetValue("_id", Guid.NewGuid().ToString("N"));
    }

    private string GetValue(string path)
    {
      if (!values.ContainsKey(path))
        throw new InvalidOperationException(
          String.Format(Resources.PathNotFound, path));
      ///TODO:  implement full path here.  This is currently only one level deep
      return values[path].ToString();
    }

    public void SetValue(string key, object val)
    {
      Type t = val.GetType();
      IList e = val as IList;

      if (e != null)
        values[key] = GetArrayValues(e);
      else if (t.Namespace != "System")
          values[key] = ParseObject(val);
      else
          values[key] = val;
    }

    private Dictionary<string,object>[] GetArrayValues(IEnumerable value)
    {
      List<Dictionary<string, object>> values = new List<Dictionary<string, object>>();
      foreach (object o in value)
        values.Add(ParseObject(o));
      return values.ToArray();
    }

    public override string ToString()
    {
      return DictToString(values);
    }

    private string DictToString(Dictionary<string, object> vals)
    {
      StringBuilder json = new StringBuilder("{");
      string delimiter = "";
      foreach (string key in vals.Keys)
      {
        json.AppendFormat("{2}\"{0}\":{1}", key, GetValue(vals[key]), delimiter);
        delimiter = ", ";
      }
      json.Append("}");
      return json.ToString();
    }

    private string GetValue(object val)
    {
      if (val is Dictionary<string, object>)
        return DictToString(val as Dictionary<string, object>);
      return "\"" + val.ToString() + "\"";
    }

    private bool CompareDictionaries<TKey, TValue>(Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
    {
      IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;
      if (dict1.Count != dict2.Count) return false;
      foreach (TKey key in dict1.Keys)
      {
        if (!dict2.ContainsKey(key)) return false;
        object val = dict1[key];
        object val2 = dict2[key];
        if (val.GetType() != val2.GetType()) return false;
        if (val is Dictionary<TKey, TValue>)
          return CompareDictionaries<TKey, TValue>((Dictionary<TKey, TValue>)val, (Dictionary<TKey, TValue>)val2);
      }
      return true;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is JsonDoc))
        throw new InvalidOperationException("JsonDoc can only be compared with another JsonDoc");
      JsonDoc toCompare = obj as JsonDoc;
      return CompareDictionaries<string, object>(values, toCompare.values);
    }

    private Dictionary<string, object> ParseObject(object val)
    {
      Type t = val.GetType();
      bool allProps = t.Name.Contains("Anonymous");
      Dictionary<string, object> vals = new Dictionary<string, object>();

      PropertyInfo[] props = allProps ? t.GetProperties() : t.GetProperties(BindingFlags.Public);
      foreach (PropertyInfo prop in props)
        vals.Add(prop.Name, prop.GetValue(val));
      return vals;
    }
  }
}
