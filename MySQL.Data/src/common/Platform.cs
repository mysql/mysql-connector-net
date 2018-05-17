// Copyright © 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Runtime.InteropServices;

namespace MySql.Data.Common
{
  internal class Platform
  {
    private static bool _inited;
    private static bool _isMono;

    /// <summary>
    /// By creating a private ctor, we keep the compiler from creating a default ctor
    /// </summary>
    private Platform()
    {
    }

    public static bool IsWindows()
    {
#if NETSTANDARD1_3
      return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
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
    }

    public static bool IsMacOSX()
    {
#if NET452
      return Environment.OSVersion.Platform == PlatformID.MacOSX;
#else
      return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
    }


    public static bool IsMono()
    {
      if (!_inited)
        Init();
      return _isMono;
    }

    private static void Init()
    {
      _inited = true;
      Type t = Type.GetType("Mono.Runtime");
      _isMono = t != null;
    }

    public static bool IsDotNetCore()
    {
#if NETSTANDARD1_3
      return true;
#else
      return false;
#endif
    }
  }
}
