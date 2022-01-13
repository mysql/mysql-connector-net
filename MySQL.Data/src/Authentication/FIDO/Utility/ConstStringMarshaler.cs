// Copyright (c) 2022, Oracle and/or its affiliates.
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

namespace MySql.Data.Authentication.FIDO.Utility
{
  internal sealed class ConstStringMarshaler : ICustomMarshaler
  {
    private static readonly ConstStringMarshaler Instance = new ConstStringMarshaler();

    /// <summary>
    /// Gets the global instance of this class as required by <see cref="ICustomMarshaler"/>
    /// </summary>
    /// <param name="cookie">The cookie to use when getting the global instance (ignored)</param>
    /// <returns>The global instance</returns>
    public static ICustomMarshaler GetInstance(string cookie) => Instance;

    #region ICustomMarshaler
    public void CleanUpManagedData(object ManagedObj)
    {
      throw new NotImplementedException();
    }

    public void CleanUpNativeData(IntPtr pNativeData) { }

    public int GetNativeDataSize()
    {
      return IntPtr.Size;
    }

    public IntPtr MarshalManagedToNative(object ManagedObj)
    {
      throw new NotImplementedException();
    }

    public object MarshalNativeToManaged(IntPtr pNativeData)
    {
      return Marshal.PtrToStringAnsi(pNativeData);
    }
    #endregion
  }
}
