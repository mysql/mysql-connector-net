// Copyright (c) 2020 Oracle and/or its affiliates.
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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MySqlX.Common
{
  /// <summary>
  /// Provides functionality for loading unmanaged libraries.
  /// </summary>
  internal static class UnmanagedLibraryLoader
  {
    /// <summary>
    /// Loads the specified unmanaged library from the embedded resources.
    /// </summary>
    /// <param name="applicationName">The application name.</param>
    /// <param name="libraryName">The library name.</param>
    internal static bool LoadUnmanagedLibraryFromEmbeddedResources(string applicationName, string libraryName)
    {
      try
      {
        byte[] byteArray = null;
        var resource = $"{applicationName}.{libraryName}";
        var executingAssembly = Assembly.GetExecutingAssembly();
        using (var stream = executingAssembly.GetManifestResourceStream(resource))
        {
          byteArray = new byte[(int)stream.Length];
          stream.Read(byteArray, 0, (int)stream.Length);
        }

        var tempFile = $"{Path.GetTempPath()}{libraryName}";
        if (File.Exists(tempFile))
        {
          File.Delete(tempFile);
        }

        File.WriteAllBytes(tempFile, byteArray);
        LoadLibraryEx(tempFile, IntPtr.Zero, LoadLibraryFlags.LOAD_WITH_ALTERED_SEARCH_PATH);
        return true;
      }
      catch
      {
        return false;
      }
    }

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

    [Flags]
    private enum LoadLibraryFlags : uint
    {
      DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
      LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
      LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
      LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
      LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
      LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
      LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
      LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
      LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
      LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
      LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
    }
  }
}
