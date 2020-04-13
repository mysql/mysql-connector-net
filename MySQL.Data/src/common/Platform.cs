// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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
      OperatingSystem os = Environment.OSVersion;
      switch (os.Platform)
      {
        case PlatformID.Win32NT:
        case PlatformID.Win32S:
        case PlatformID.Win32Windows:
          return true;
      }
      return false;

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
  }
}
