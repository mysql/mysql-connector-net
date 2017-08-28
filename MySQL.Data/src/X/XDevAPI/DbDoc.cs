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

using MySql.Data;
using MySqlX;
using MySqlX.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a generic document in JSON format.
  /// </summary>
  public class DbDoc
  {
    internal Dictionary<string, object> values = new Dictionary<string, object>();

    /// <summary>
    /// Initializes a new instance of the DbDoc class based on the object provided. The value can be a domain object, anonymous object, or JSON string.
    /// </summary>
    /// <param name="val">The value for this DbDoc.</param>
    public DbDoc(object val = null)
    {
      if (val != null)
      {
        if (val is string)
          values = JsonParser.Parse(val as string);
        else if (val is Dictionary<string, object>)
          values = JsonParser.Parse(DictToString(val as Dictionary<string, object>, 2));
        else if (val is DbDoc)
          values = JsonParser.Parse(DictToString((val as DbDoc).values, 2));
        else
          values = ParseObject(val);
      }
    }

    /// <summary>
    /// Gets the value of a document property.
    /// NOTE: currently this is only supported one level deep.
    /// </summary>
    /// <param name="path">The key path for the property.</param>
    /// <returns></returns>
    public string this[string path]
    {
      get { return GetValue(path); }
    }

    /// <summary>
    /// Gets the identifier of the document.
    /// </summary>
    public object Id
    {
      get { return values["_id"];  }
      internal set { SetValue("_id", value); }
    }

    /// <summary>
    /// Gets a value indicating if this document has an identifier (property named _id with a value).
    /// </summary>
    public bool HasId
    {
      get { return values.ContainsKey("_id"); }
    }

    internal void EnsureId()
    {
      if (!HasId)
      {
        char separatorChar = '-';
        string[] token = Guid.NewGuid().ToString().Split(separatorChar);
        string guid = string.Empty;
        for (int i = token.Length-1; i >= 0; i--)
        {
          guid += token[i];
          if (i != 0) guid += separatorChar;
        }

        SetValue("_id", guid.Replace(separatorChar.ToString(), string.Empty));
      }
    }

    private string GetValue(string path)
    {
      string[] levels = path.Split('.');
      Dictionary<string,object> dic = values;
      string returnValue = null;
      foreach(string level in levels)
      {
        if (!dic.ContainsKey(level))
          throw new InvalidOperationException(
            String.Format(ResourcesX.PathNotFound, path));
        if (dic[level] is Dictionary<string, object>)
        {
          dic = dic[level] as Dictionary<string, object>;
          returnValue = DictToString(dic, 2);
        }
        else if (dic[level] == null) return null;
        else if (dic[level].GetType().GetTypeInfo().IsGenericType)
        {
          dic = ParseObject(dic[level]);
          returnValue = DictToString(dic, 2);
        }
        else
          returnValue = dic[level].ToString();
      }

      return returnValue;
    }

    /// <summary>
    /// Sets a property on this document.
    /// </summary>
    /// <param name="key">The key of the property.</param>
    /// <param name="val">The new property value.</param>
    public void SetValue(string key, object val)
    {
      IList e = val as IList;

      if (e != null)
        values[key] = GetArrayValues(e);
      else if (val is DbDoc)
        values[key] = (val as DbDoc).values;
      else if (val is Dictionary<string, object>)
        values[key] = val;
      else if (val!=null && val.GetType().Namespace != "System")
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

    /// <summary>
    /// Returns this document in Json format.
    /// </summary>
    /// <returns>A Json formatted string.</returns>
    public override string ToString()
    {
      return DictToString(values, 2);
    }

    private string DictToString(Dictionary<string, object> vals, int ident)
    {
      StringBuilder json = new StringBuilder("{");
      string delimiter = "";
      foreach (string key in vals.Keys)
      {
        json.Append(delimiter);
        json.AppendLine();
        json.Append(' ', ident);
        json.AppendFormat("\"{0}\": {1}", key, GetValue(vals[key], ident) ?? "null");
        delimiter = ", ";
      }
      json.AppendLine();
      json.Append(' ', ident - 2);
      json.Append("}");
      return json.ToString();
    }

    private string GetValue(object val, int ident)
    {
      if (val==null) return null;

      if(val.GetType().IsArray)
      {

        StringBuilder values = new StringBuilder("[");
        string separator = string.Empty;
        foreach (var item in (Array)val)
        {
          values.Append(separator);
          values.AppendLine();
          values.Append(' ', ident + 2);
          values.Append(GetValue(item, ident + 2));
          separator = ", ";
        }
        values.AppendLine();
        values.Append(' ', ident);
        values.Append("]");
        return values.ToString();
      }
      if (val is Dictionary<string, object>)
        return DictToString(val as Dictionary<string, object>, ident + 2);
      string quoteChar = "";
      Type type = val.GetType();
      if (val is string || val is DateTime)
      {
        quoteChar = "\"";
      }
      return quoteChar + val.ToString() + quoteChar;
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
        if(val is Dictionary<TKey, TValue>[] && val2 is Dictionary<TKey, TValue>[])
        {
          Dictionary<TKey, TValue>[] valArray1 = (Dictionary<TKey, TValue>[])val;
          Dictionary<TKey, TValue>[] valArray2 = (Dictionary<TKey, TValue>[])val2;
          if (valArray1.Length != valArray2.Length) return false;
          for(int i = 0; i < valArray1.Length; i++)
          {
            if (!CompareDictionaries<TKey, TValue>(valArray1[i], valArray2[i])) return false;
          }
        }
        else if (val is Dictionary<TKey, TValue> && val2 is Dictionary<TKey, TValue>)
          return CompareDictionaries<TKey, TValue>((Dictionary<TKey, TValue>)val, (Dictionary<TKey, TValue>)val2);
        else if (!val.Equals(val2)) return false;
      }
      return true;
    }

    /// <summary>
    /// Compares this DbDoc with another one.
    /// </summary>
    /// <param name="obj">The DbDoc to compare to.</param>
    /// <returns>True if they are equal, false otherwise.</returns>
    public override bool Equals(object obj)
    {
      if (!(obj is DbDoc))
        throw new InvalidOperationException("DbDoc can only be compared with another DbDoc");
      DbDoc toCompare = obj as DbDoc;
      return CompareDictionaries<string, object>(values, toCompare.values);
    }

    /// <summary>
    /// Gets a value that serves as a hash function for a particular type.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    private Dictionary<string, object> ParseObject(object val)
    {
      Type t = val.GetType();
      bool allProps = t.Name.Contains("Anonymous");
      Dictionary<string, object> vals = new Dictionary<string, object>();

      PropertyInfo[] props = allProps ? t.GetProperties() : t.GetProperties(BindingFlags.Public);
      foreach (PropertyInfo prop in props)
        vals.Add(prop.Name, prop.GetValue(val, null));
      return vals;
    }
  }
}
