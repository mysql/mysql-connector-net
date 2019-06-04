// Copyright (c) 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using System.Collections.Generic;
using System.Collections;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Represents a collection of parameters relevant to a <see cref="MySqlCommand"/> as well as their respective mappings to columns in a <see cref="System.Data.DataSet"/>. This class cannot be inherited.
  /// </summary>
  /// <include file='docs/MySqlParameterCollection.xml' path='MyDocs/MyMembers[@name="Class"]/*'/>
  public sealed partial class MySqlParameterCollection : DbParameterCollection
  {
    readonly List<MySqlParameter> _items = new List<MySqlParameter>();
    private readonly Dictionary<string, int> _indexHashCs;
    private readonly Dictionary<string, int> _indexHashCi;
    //turns to true if any parameter is unnamed
    internal bool containsUnnamedParameters;

    internal MySqlParameterCollection(MySqlCommand cmd)
    {
      _indexHashCs = new Dictionary<string, int>();
      _indexHashCi = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
      containsUnnamedParameters = false;
      Clear();
    }

    /// <summary>
    /// Gets the number of MySqlParameter objects in the collection.
    /// </summary>
    public override int Count => _items.Count;

    #region Public Methods

    /// <summary>
    /// Gets the <see cref="MySqlParameter"/> at the specified index.
    /// </summary>
    /// <overloads>Gets the <see cref="MySqlParameter"/> with a specified attribute.
    /// [C#] In C#, this property is the indexer for the <see cref="MySqlParameterCollection"/> class.
    /// </overloads>
    public new MySqlParameter this[int index]
    {
      get { return InternalGetParameter(index); }
      set { InternalSetParameter(index, value); }
    }

    /// <summary>
    /// Gets the <see cref="MySqlParameter"/> with the specified name.
    /// </summary>
    public new MySqlParameter this[string name]
    {
      get { return InternalGetParameter(name); }
      set { InternalSetParameter(name, value); }
    }

    /// <summary>
    /// Adds a <see cref="MySqlParameter"/> to the <see cref="MySqlParameterCollection"/> with the parameter name, the data type, the column length, and the source column name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">One of the <see cref="MySqlDbType"/> values. </param>
    /// <param name="size">The length of the column.</param>
    /// <param name="sourceColumn">The name of the source column.</param>
    /// <returns>The newly added <see cref="MySqlParameter"/> object.</returns>
    public MySqlParameter Add(string parameterName, MySqlDbType dbType, int size, string sourceColumn)
    {
      return Add(new MySqlParameter(parameterName, dbType, size, sourceColumn));
    }

    /// <summary>
    /// Adds the specified <see cref="MySqlParameter"/> object to the <see cref="MySqlParameterCollection"/>.
    /// </summary>
    /// <param name="value">The <see cref="MySqlParameter"/> to add to the collection.</param>
    /// <returns>The newly added <see cref="MySqlParameter"/> object.</returns>
    public MySqlParameter Add(MySqlParameter value)
    {
      return InternalAdd(value, -1);
    }

    /// <summary>
    /// Adds a parameter and its value.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>A <see cref="MySqlParameter"/> object representing the provided values.</returns>
    public MySqlParameter AddWithValue(string parameterName, object value)
    {
      return Add(new MySqlParameter(parameterName, value));
    }

    /// <summary>
    /// Adds a <see cref="MySqlParameter"/> to the <see cref="MySqlParameterCollection"/> given the parameter name and the data type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">One of the <see cref="MySqlDbType"/> values. </param>
    /// <returns>The newly added <see cref="MySqlParameter"/> object.</returns>
    public MySqlParameter Add(string parameterName, MySqlDbType dbType)
    {
      return Add(new MySqlParameter(parameterName, dbType));
    }

    /// <summary>
    /// Adds a <see cref="MySqlParameter"/> to the <see cref="MySqlParameterCollection"/> with the parameter name, the data type, and the column length.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">One of the <see cref="MySqlDbType"/> values. </param>
    /// <param name="size">The length of the column.</param>
    /// <returns>The newly added <see cref="MySqlParameter"/> object.</returns>
    public MySqlParameter Add(string parameterName, MySqlDbType dbType, int size)
    {
      return Add(new MySqlParameter(parameterName, dbType, size));
    }

    #endregion

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public override void Clear()
    {
      foreach (MySqlParameter p in _items)
        p.Collection = null;
      _items.Clear();
      _indexHashCs.Clear();
      _indexHashCi.Clear();
    }

    void CheckIndex(int index)
    {
      if (index < 0 || index >= Count)
        throw new IndexOutOfRangeException("Parameter index is out of range.");
    }

    private MySqlParameter InternalGetParameter(int index)
    {
      CheckIndex(index);
      return _items[index];
    }

    private MySqlParameter InternalGetParameter(string parameterName)
    {
      int index = IndexOf(parameterName);
      if (index < 0)
      {
        // check to see if the user has added the parameter without a
        // parameter marker.  If so, kindly tell them what they did.
        if (parameterName.StartsWith("@", StringComparison.Ordinal) ||
                    parameterName.StartsWith("?", StringComparison.Ordinal))
        {
          string newParameterName = parameterName.Substring(1);
          index = IndexOf(newParameterName);
          if (index != -1)
            return _items[index];
        }
        throw new ArgumentException("Parameter '" + parameterName + "' not found in the collection.");
      }
      return _items[index];
    }

    private void InternalSetParameter(string parameterName, MySqlParameter value)
    {
      int index = IndexOf(parameterName);
      if (index < 0)
        throw new ArgumentException("Parameter '" + parameterName + "' not found in the collection.");
      InternalSetParameter(index, value);
    }

    private void InternalSetParameter(int index, MySqlParameter value)
    {
      MySqlParameter newParameter = value;
      if (newParameter == null)
        throw new ArgumentException(Resources.NewValueShouldBeMySqlParameter);

      CheckIndex(index);
      MySqlParameter p = _items[index];

      // first we remove the old parameter from our hashes
      _indexHashCs.Remove(p.ParameterName);
      _indexHashCi.Remove(p.ParameterName);

      // then we add in the new parameter
      _items[index] = newParameter;
      _indexHashCs.Add(value.ParameterName, index);
      _indexHashCi.Add(value.ParameterName, index);
    }

    /// <summary>
    /// Gets the location of the <see cref="MySqlParameter"/> in the collection with a specific parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the <see cref="MySqlParameter"/> object to retrieve. </param>
    /// <returns>The zero-based location of the <see cref="MySqlParameter"/> in the collection.</returns>
    public override int IndexOf(string parameterName)
    {
      int i = -1;
      if (!_indexHashCs.TryGetValue(parameterName, out i) &&
        !_indexHashCi.TryGetValue(parameterName, out i))
        return -1;
      return i;
    }

    /// <summary>
    /// Gets the location of a <see cref="MySqlParameter"/> in the collection.
    /// </summary>
    /// <param name="value">The <see cref="MySqlParameter"/> object to locate. </param>
    /// <returns>The zero-based location of the <see cref="MySqlParameter"/> in the collection.</returns>
    /// <overloads>Gets the location of a <see cref="MySqlParameter"/> in the collection.</overloads>
    public override int IndexOf(object value)
    {
      MySqlParameter parameter = value as MySqlParameter;
      if (null == parameter)
        throw new ArgumentException("Argument must be of type DbParameter", "value");
      return _items.IndexOf(parameter);
    }

    internal void ParameterNameChanged(MySqlParameter p, string oldName, string newName)
    {
      int index = IndexOf(oldName);
      _indexHashCs.Remove(oldName);
      _indexHashCi.Remove(oldName);

      _indexHashCs.Add(newName, index);
      _indexHashCi.Add(newName, index);
    }

    private MySqlParameter InternalAdd(MySqlParameter value, int index)
    {
      if (value == null)
        throw new ArgumentException("The MySqlParameterCollection only accepts non-null MySqlParameter type objects.", "value");

      // if the parameter is unnamed, then assign a default name
      if (String.IsNullOrEmpty(value.ParameterName))
        value.ParameterName = String.Format("Parameter{0}", GetNextIndex());

      // make sure we don't already have a parameter with this name
      if (IndexOf(value.ParameterName) >= 0)
      {
        throw new MySqlException(
            String.Format(Resources.ParameterAlreadyDefined, value.ParameterName));
      }
      else
      {
        string inComingName = value.ParameterName;
        if (inComingName[0] == '@' || inComingName[0] == '?')
          inComingName = inComingName.Substring(1, inComingName.Length - 1);
        if (IndexOf(inComingName) >= 0)
          throw new MySqlException(
              String.Format(Resources.ParameterAlreadyDefined, value.ParameterName));
      }

      if (index == -1)
      {
        _items.Add(value);
        index = _items.Count - 1;
      }
      else
      {
        _items.Insert(index, value);
        AdjustHashes(index, true);
      }

      _indexHashCs.Add(value.ParameterName, index);
      _indexHashCi.Add(value.ParameterName, index);

      value.Collection = this;
      return value;
    }

    private int GetNextIndex()
    {
      int index = Count + 1;

      while (true)
      {
        string name = "Parameter" + index.ToString();
        if (!_indexHashCi.ContainsKey(name)) break;
        index++;
      }
      return index;
    }

    private static void AdjustHash(Dictionary<string, int> hash, string parameterName, int keyIndex, bool addEntry)
    {
      if (!hash.ContainsKey(parameterName)) return;
      int index = hash[parameterName];
      if (index < keyIndex) return;
      hash[parameterName] = addEntry ? ++index : --index;
    }

    /// <summary>
    /// This method will update all the items in the index hashes when
    /// we insert a parameter somewhere in the middle
    /// </summary>
    /// <param name="keyIndex"></param>
    /// <param name="addEntry"></param>
    private void AdjustHashes(int keyIndex, bool addEntry)
    {
      for (int i = 0; i < Count; i++)
      {
        string name = _items[i].ParameterName;
        AdjustHash(_indexHashCi, name, keyIndex, addEntry);
        AdjustHash(_indexHashCs, name, keyIndex, addEntry);
      }
    }

    private MySqlParameter GetParameterFlexibleInternal(string baseName)
    {
      int index = IndexOf(baseName);
      if (-1 == index)
        index = IndexOf("?" + baseName);
      if (-1 == index)
        index = IndexOf("@" + baseName);
      if (-1 != index)
        return this[index];
      return null;
    }

    internal MySqlParameter GetParameterFlexible(string parameterName, bool throwOnNotFound)
    {
      string baseName = parameterName;
      MySqlParameter p = GetParameterFlexibleInternal(baseName);
      if (p != null) return p;

      if (parameterName.StartsWith("@", StringComparison.Ordinal) || parameterName.StartsWith("?", StringComparison.Ordinal))
        baseName = parameterName.Substring(1);
      p = GetParameterFlexibleInternal(baseName);
      if (p != null) return p;

      if (throwOnNotFound)
        throw new ArgumentException("Parameter '" + parameterName + "' not found in the collection.");
      return null;
    }

    #region DbParameterCollection Implementation

    /// <summary>
    /// Adds an array of values to the end of the <see cref="MySqlParameterCollection"/>. 
    /// </summary>
    /// <param name="values"></param>
    public override void AddRange(Array values)
    {
      foreach (DbParameter p in values)
        Add(p);
    }

    /// <summary>
    /// Retrieve the parameter with the given name.
    /// </summary>
    /// <param name="parameterName"></param>
    /// <returns></returns>
    protected override DbParameter GetParameter(string parameterName)
    {
      return InternalGetParameter(parameterName);
    }

    protected override DbParameter GetParameter(int index)
    {
      return InternalGetParameter(index);
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
      InternalSetParameter(parameterName, value as MySqlParameter);
    }

    protected override void SetParameter(int index, DbParameter value)
    {
      InternalSetParameter(index, value as MySqlParameter);
    }

    /// <summary>
    /// Adds the specified <see cref="MySqlParameter"/> object to the <see cref="MySqlParameterCollection"/>.
    /// </summary>
    /// <param name="value">The <see cref="MySqlParameter"/> to add to the collection.</param>
    /// <returns>The index of the new <see cref="MySqlParameter"/> object.</returns>
    public override int Add(object value)
    {
      MySqlParameter parameter = value as MySqlParameter;
      if (parameter == null)
        throw new MySqlException("Only MySqlParameter objects may be stored");

      parameter = Add(parameter);
      return IndexOf(parameter);
    }

    /// <summary>
    /// Gets a value indicating whether a <see cref="MySqlParameter"/> with the specified parameter name exists in the collection.
    /// </summary>
    /// <param name="parameterName">The name of the <see cref="MySqlParameter"/> object to find.</param>
    /// <returns>true if the collection contains the parameter; otherwise, false.</returns>
    public override bool Contains(string parameterName)
    {
      return IndexOf(parameterName) != -1;
    }

    /// <summary>
    /// Gets a value indicating whether a MySqlParameter exists in the collection.
    /// </summary>
    /// <param name="value">The value of the <see cref="MySqlParameter"/> object to find. </param>
    /// <returns>true if the collection contains the <see cref="MySqlParameter"/> object; otherwise, false.</returns>
    /// <overloads>Gets a value indicating whether a <see cref="MySqlParameter"/> exists in the collection.</overloads>
    public override bool Contains(object value)
    {
      MySqlParameter parameter = value as MySqlParameter;
      if (null == parameter)
        throw new ArgumentException("Argument must be of type DbParameter", nameof(value));
      return _items.Contains(parameter);
    }

    /// <summary>
    /// Copies MySqlParameter objects from the MySqlParameterCollection to the specified array.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="index"></param>
    public override void CopyTo(Array array, int index)
    {
      _items.ToArray().CopyTo(array, index);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="MySqlParameterCollection"/>. 
    /// </summary>
    /// <returns></returns>
    public override IEnumerator GetEnumerator()
    {
      return _items.GetEnumerator();
    }

    /// <summary>
    /// Inserts a MySqlParameter into the collection at the specified index.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    public override void Insert(int index, object value)
    {
      MySqlParameter parameter = value as MySqlParameter;
      if (parameter == null)
        throw new MySqlException("Only MySqlParameter objects may be stored");
      InternalAdd(parameter, index);
    }

    /// <summary>
    /// Removes the specified MySqlParameter from the collection.
    /// </summary>
    /// <param name="value"></param>
    public override void Remove(object value)
    {
      MySqlParameter p = (value as MySqlParameter);
      p.Collection = null;
      int index = IndexOf(p);
      _items.Remove(p);

      _indexHashCs.Remove(p.ParameterName);
      _indexHashCi.Remove(p.ParameterName);
      AdjustHashes(index, false);
    }

    /// <summary>
    /// Removes the specified <see cref="MySqlParameter"/> from the collection using the parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the <see cref="MySqlParameter"/> object to retrieve. </param>
    public override void RemoveAt(string parameterName)
    {
      DbParameter p = GetParameter(parameterName);
      Remove(p);
    }

    /// <summary>
    /// Removes the specified <see cref="MySqlParameter"/> from the collection using a specific index.
    /// </summary>
    /// <param name="index">The zero-based index of the parameter. </param>
    /// <overloads>Removes the specified <see cref="MySqlParameter"/> from the collection.</overloads>
    public override void RemoveAt(int index)
    {
      object o = _items[index];
      Remove(o);
    }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the 
    /// <see cref="MySqlParameterCollection"/>. 
    /// </summary>
    public override object SyncRoot => (_items as IList).SyncRoot;

    #endregion

  }
}
