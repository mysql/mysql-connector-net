﻿// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if !NETSTANDARD1_3
using System.Runtime.InteropServices.WindowsRuntime;
#endif
namespace MySql.Data.MySqlClient
{
  public static class ExtensionMethods
  {
    
    public static byte[] GetBuffer(this Stream stream)
    {
      return ((MemoryStream)stream).ToArray();
    }

    public static void Close(this Stream stream)
    {
      stream.Dispose();
    }

    public static void Close(this StreamReader stream)
    {
      stream.Dispose();
    }

    public static string ToLower(this String newString, System.Globalization.CultureInfo culture)
    {
      if (culture == System.Globalization.CultureInfo.InvariantCulture)
        return newString.ToLowerInvariant();
      else
        return newString.ToLower();
    }

    public static string ToUpper(this String newString, System.Globalization.CultureInfo culture)
    {
      if (culture == System.Globalization.CultureInfo.InvariantCulture)
        return newString.ToUpperInvariant();
      else
        return newString.ToUpper();
    }

    public static string ToLongTimeString(this DateTime dateTime)
    {
      return dateTime.ToString(System.Globalization.DateTimeFormatInfo.CurrentInfo.LongTimePattern);
    }

    public static bool WaitOne(this WaitHandle waitHandle, int millisecondsTimeout, bool exitContext)
    {
      return waitHandle.WaitOne(millisecondsTimeout);
    }

    public static PropertyInfo[] GetProperties(this Type type)
    {
      return type.GetRuntimeProperties().ToArray();
    }
  }
}
