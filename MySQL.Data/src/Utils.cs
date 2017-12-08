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

using System.IO;
using System.Reflection;
using System.Text;

namespace MySql.Data.MySqlClient
{
  internal class Utils
  {
    public static string ReadResource(string name)
    {
      string rez = ReadResourceInternal(name);
      if (rez != null) return rez;
      return ReadResourceInternal("MySqlClient/" + name);
    }
    public static string ReadResourceInternal(string name)
    {
#if NETSTANDARD1_3
      var assembly = typeof(Utils).GetTypeInfo().Assembly;
#else
      var assembly = Assembly.GetExecutingAssembly();
#endif

      var resName = assembly.GetName().Name + "." + name.Replace(" ", "_")
                                                     .Replace("\\", ".")
                                                     .Replace("/", ".");
      var resourceStream = assembly.GetManifestResourceStream(resName);
      if (resourceStream == null) return null;

      using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
      {
        return reader.ReadToEnd();
      }
    }
  }
}
