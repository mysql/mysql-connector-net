// Copyright (c) 2020, Oracle and/or its affiliates.
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

namespace MySql.Data.Authentication.GSSAPI.Utility
{
  /// <summary>
  /// Memory pinned object
  /// </summary>
  internal static class Pinned
  {
    /// <summary>
    /// Create memory pinned object from <paramref name="value"/>
    /// </summary>
    /// <typeparam name="T">Any class type</typeparam>
    /// <param name="value">Value to pin</param>
    /// <returns>Pinned value</returns>
    internal static Pinned<T> From<T>(T value) where T : class => new Pinned<T>(value);
  }

  /// <summary>
  /// Memory pinned object
  /// </summary>
  /// <typeparam name="T">Any class type</typeparam>
  internal sealed class Pinned<T> : IDisposable where T : class
  {
    /// <summary>
    /// Original object value, can be used with <code>ref</code>
    /// </summary>
    internal readonly T Value;

    /// <summary>
    /// In memory address of the object
    /// </summary>
    internal IntPtr Address { get; }

    private GCHandle _handle;

    /// <summary>
    /// Create memory pinned object from <paramref name="value"/>
    /// </summary>
    /// <param name="value">Value to pin</param>
    internal Pinned(T value)
    {
      Value = value;
      _handle = GCHandle.Alloc(value, GCHandleType.Pinned);
      Address = _handle.AddrOfPinnedObject();
    }

    /// <summary>
    /// Returns address of object in memory
    /// </summary>
    public static implicit operator IntPtr(Pinned<T> p)
    {
      return p.Address;
    }

    /// <summary>
    /// Returns original object value
    /// </summary>
    public static implicit operator T(Pinned<T> p)
    {
      return p.Value;
    }

    public void Dispose()
    {
      _handle.Free();
    }
  }
}