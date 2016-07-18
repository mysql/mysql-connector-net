// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace MySql.Data.MySqlClient
{
#if !NETCORE10
  [Editor("MySql.Data.MySqlClient.Design.DBParametersEditor,MySql.Design", typeof(System.Drawing.Design.UITypeEditor))]
  [ListBindable(true)]
#endif
  public sealed partial class MySqlParameterCollection : DbParameterCollection
  {
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

#if !NETCORE10
    /// <summary>
    /// Gets a value that indicates whether the <see cref="MySqlParameterCollection"/>
    /// has a fixed size. 
    /// </summary>
    public override bool IsFixedSize
    {
      get { return (_items as IList).IsFixedSize; }
    }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="MySqlParameterCollection"/>
    /// is read-only. 
    /// </summary>
    public override bool IsReadOnly
    {
      get { return (_items as IList).IsReadOnly; }
    }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="MySqlParameterCollection"/>
    /// is synchronized. 
    /// </summary>
    public override bool IsSynchronized
    {
      get { return (_items as IList).IsSynchronized; }
    }
#endif

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
