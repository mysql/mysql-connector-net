// Copyright (c) 2014, 2018, Oracle and/or its affiliates. All rights reserved.
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

    //internal static OS GetOS()
    //{
    //  if (Path.DirectorySeparatorChar == '/')
    //    return OS.Linux;
    //  if (Path.DirectorySeparatorChar == '\\')
    //    return OS.Windows;
    //  else
    //    return OS.MacOS;
    //}

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
