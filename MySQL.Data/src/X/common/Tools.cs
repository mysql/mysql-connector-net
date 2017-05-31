// Copyright © 2014, 2017, Oracle and/or its affiliates. All rights reserved.
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
using System.Reflection;
using MySql.Data;
using MySqlX.DataAccess;
using System.IO;

namespace MySqlX.Common
{
  internal static class Tools
  {
    public static Dictionary<string, object> GetDictionaryFromAnonymous(object anonymousObject)
    {
      Dictionary<string, object> result = new Dictionary<string, object>();

      if (!anonymousObject.GetType().IsConstructedGenericType)
        throw new FormatException(ResourcesX.InvalidConnectionData);

      foreach (PropertyInfo property in anonymousObject.GetType().GetProperties())
      {
        object value = property.GetValue(anonymousObject, null);
        result.Add(property.Name, value);
      }

      return result;
    }

    internal static OS GetOS()
    {
      if (Path.DirectorySeparatorChar == '/')
        return OS.Linux;
      if (Path.DirectorySeparatorChar == '\\')
        return OS.Windows;
      else
        return OS.MacOS;
    }

    /// <summary>
    /// Compares two Guids in string format.
    /// </summary>
    /// <param name="guid1">The first string to compare.</param>
    /// <param name="guid2">The first string to compare.</param>
    /// <returns>An integer that indicates the lexical relationship between the two comparands, similar to <see cref="string.Compare(string, string)"/></returns>
    internal static int CompareGuids(string guid1, string guid2)
    {
      return string.Compare(guid1.Replace("-",""),guid2.Replace("-",""));
    }

    /// <summary>
    /// Compares two <see cref="Guid"/> objects.
    /// </summary>
    /// <param name="guid1">The first <see cref="Guid"/> to compare.</param>
    /// <param name="guid2">The second <see cref="Guid"/> to compare.</param>
    /// <returns>An integer that indicates the lexical relationship between the two comparands, similar to <see cref="string.Compare(string, string)"/></returns>
    internal static int CompareGuids(Guid guid1, Guid guid2)
    {
      return CompareGuids(guid1.ToString(), guid2.ToString());
    }
  }
}
