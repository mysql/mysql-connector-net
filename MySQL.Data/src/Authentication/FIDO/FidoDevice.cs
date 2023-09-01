// Copyright (c) 2022, 2023, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.FIDO.Native;
using MySql.Data.Authentication.FIDO.Utility;
using System;

namespace MySql.Data.Authentication.FIDO
{
  internal sealed unsafe class FidoDevice : IDisposable
  {
    private fido_dev_t* _device;

    #region Constructors
    static FidoDevice()
    {
      Init.Call();
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <exception cref="OutOfMemoryException" />
    public FidoDevice()
    {
      _device = NativeMethods.fido_dev_new();
      if (_device == null)
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~FidoDevice() => ReleaseUnmanagedResources();
    #endregion

    /// <summary>
    /// Opens the device at the given path.
    /// </summary>
    /// <param name="path">The path of the device</param>
    /// <exception cref="CtapException">Thrown if an error occurs while opening the device</exception>
    public void Open(string path) => NativeMethods.fido_dev_open(_device, path).Check();

    /// <summary>
    /// Closes the device, preventing further use
    /// </summary>
    /// <exception cref="CtapException">Thrown if an error occurs while closing</exception>
    public void Close() => NativeMethods.fido_dev_close(_device).Check();

    /// <summary>
    /// Determines whether this device supports CTAP 2.1 Credential Management.
    /// </summary>
    public bool SupportsCredman => NativeMethods.fido_dev_supports_credman(_device);

    /// <summary>
    /// Uses the device to generate an assertion
    /// </summary>
    /// <param name="assert">The assertion object with its input properties properly set</param>
    /// <exception cref="CtapException">Thrown if an error occurs while generating the assertion</exception>
    public void GetAssert(FidoAssertion assert) =>
        NativeMethods.fido_dev_get_assert(_device, (fido_assert_t*)assert, null).Check();

    private void ReleaseUnmanagedResources()
    {
      var native = _device;
      NativeMethods.fido_dev_free(&native);
      _device = null;
    }

    #region IDisposable
    public void Dispose()
    {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }
    #endregion
  }
}
