// Copyright (c) 2004-2008 MySQL AB, 2008-2009 Sun Microsystems, Inc.
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
using System.IO;

namespace MySql.Data.Common
{
  internal class Platform
  {
    private static bool inited;
    private static bool isMono;

    /// <summary>
    /// By creating a private ctor, we keep the compiler from creating a default ctor
    /// </summary>
    private Platform()
    {
    }

#if !NETSTANDARD1_3
    public static bool IsWindows()
    {
#endif
#if NETFX_CORE
      return true;
#elif !NETSTANDARD1_3
      OperatingSystem os = Environment.OSVersion;
      switch (os.Platform)
      {
        case PlatformID.Win32NT:
        case PlatformID.Win32S:
        case PlatformID.Win32Windows:
          return true;
      }
      return false;
#endif
#if !NETSTANDARD1_3
    }
#endif

    public static char DirectorySeparatorChar
    {
      get
      {
#if NETFX_CORE
        return '\\';
#else
          return Path.DirectorySeparatorChar;
#endif
      }
    }

    public static bool IsMono()
    {
      if (!inited)
        Init();
      return isMono;
    }

    private static void Init()
    {
      inited = true;
      Type t = Type.GetType("Mono.Runtime");
      isMono = t != null;
    }
  }
}
