// Copyright (c) 2020, 2023, Oracle and/or its affiliates.
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

namespace MySql.Data.Authentication.GSSAPI.Utility
{
  /// <summary>
  /// Automatic dynamic disposable
  /// </summary>
  internal static class Disposable
  {
    /// <summary>
    /// Automatic dynamic disposable storing <paramref name="value"/>
    /// </summary> 
    public static Disposable<T> From<T>(T value) =>
        From(value, (IDisposable[])null, (Action)null);

    /// <summary>
    /// Automatic dynamic disposable storing <paramref name="value"/>, <paramref name="disposeAction"/> will be called at dispose
    /// </summary>
    public static Disposable<T> From<T>(T value, Action disposeAction) =>
        From(value, null, disposeAction);

    /// <summary>
    /// Automatic dynamic disposable storing <paramref name="loader"/>, <paramref name="disposable"/> will be disposed
    /// </summary>
    public static Disposable<T> From<T, D>(D disposable, Func<D, T> loader) where D : IDisposable =>
        From(loader(disposable), disposable);
    /// <summary>
    /// Automatic dynamic disposable storing <paramref name="loader"/>, <paramref name="disposable"/> will be disposed
    /// </summary>
    public static Disposable<T> From<T, D>(D disposable, Func<D, T> loader, Action<T> disposer) where D : IDisposable =>
        From(loader(disposable), disposable);

    /// <summary>
    /// Automatic dynamic disposable storing <paramref name="value"/>, <paramref name="disposables"/> will be disposed
    /// </summary>
    public static Disposable<T> From<T>(T value, params IDisposable[] disposables) =>
        From(value, disposables, null);

    /// <summary>
    /// Automatic dynamic disposable storing <paramref name="value"/>, <paramref name="disposables"/> will be disposed and <paramref name="disposeAction"/> will be called at dispose
    /// </summary>
    public static Disposable<T> From<T>(T value, IDisposable[] disposables, Action disposeAction) =>
        new Disposable<T>(value, disposables, disposeAction);
  }

  /// <summary>
  /// Automatic dynamic disposable
  /// </summary>
  internal sealed class Disposable<T> : IDisposable
  {
    /// <summary>
    /// Original value, can be used with <code>ref</code>
    /// </summary>
    public T Value;

    private readonly IDisposable[] _disposables;
    private readonly Action _disposeAction;

    /// <summary>
    /// Automatic dynamic disposable storing <paramref name="value"/>, <paramref name="disposables"/> will be disposed and <paramref name="disposeAction"/> will be called at dispose
    /// </summary>
    public Disposable(T value, IDisposable[] disposables, Action disposeAction)
    {
      Value = value;
      _disposables = disposables;
      _disposeAction = disposeAction;
    }

    /// <summary>
    /// Returns stored value
    /// </summary>
    public static implicit operator T(Disposable<T> p)
    {
      return p.Value;
    }

    private bool _disposed = false;

    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;

      if (_disposables != null)
        foreach (var t in _disposables)
          t?.Dispose();

      _disposeAction?.Invoke();
    }
  }
}