// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System.Collections.Generic;
using MySqlX.Protocol;
using MySqlX.Sessions;
using System.Collections.ObjectModel;
using System;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Base abstract class that defines elements inherited by all result types.
  /// </summary>
  public abstract class BaseResult
  {
    private List<WarningInfo> _warnings = new List<WarningInfo>();
    internal ulong _recordsAffected;
    internal ulong _affectedItemsCount;
    internal ulong _autoIncrementValue;
    internal InternalSession _session;
    internal bool _hasData;
    internal bool _hasMoreResults = false;
    internal List<string> _documentIds = new List<string>();

    /// <summary>
    /// Gets the number of records affected by the statement that generated this result.
    /// </summary>
    public UInt64 AffectedItemsCount
    {
      get { return _affectedItemsCount; }
    }

    internal BaseResult(InternalSession session)
    {
      if (session == null) return;
      _session = session;

      // if we have an active resultset then we must buffer it entirely
      if (session.ActiveResult != null)
      {
        session.ActiveResult.Buffer();
        session.ActiveResult = null;
      }

      _hasData = Protocol.HasData(this);
      if (_hasData)
        session.ActiveResult = this;
    }

    /// <summary>
    /// Gets the <see cref="ProtocolBase"/> object of the session.
    /// </summary>
    protected ProtocolBase Protocol
    {
      get { return _session?.GetProtocol(); }
    }

    internal void AddWarning(WarningInfo w)
    {
      _warnings.Add(w);
    }

    /// <summary>
    /// Gets a read-only collection of <see cref="WarningInfo"/> objects derived from statement execution.
    /// </summary>
#if NET_45_OR_GREATER
    public IReadOnlyList<WarningInfo> Warnings
#else
    public ReadOnlyCollection<WarningInfo> Warnings
#endif
    {
      get { return _warnings.AsReadOnly(); }
    }

    /// <summary>
    /// Gets the number of warnings in the <see cref="Warnings"/> collection derived from statement execution.
    /// </summary>
    public Int32 WarningsCount
    {
      get { return _warnings.Count; }
    }

    /// <summary>
    /// No action is performed by this method. It is intended to be overriden by child classes if required.
    /// </summary>
    protected virtual void Buffer() { }
  }
}
