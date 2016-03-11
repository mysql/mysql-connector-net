// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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

using System.Collections.Generic;
using MySqlX.Protocol;
using MySqlX.Session;
using System.Collections.ObjectModel;

namespace MySqlX.XDevAPI.Common
{
  public abstract class BaseResult 
  {
    private List<WarningInfo> _warnings = new List<WarningInfo>();
    internal ulong _recordsAffected;
    internal ulong _autoIncrementValue;
    protected InternalSession _session;
    internal bool _hasData;
    internal bool _hasMoreResults = false;

    internal BaseResult(InternalSession session)
    {
      _session = session;

      // if we have an active resultset then we must buffer it entirely
      if (session.ActiveResult != null)
      {
        session.ActiveResult.Buffer();
        session.ActiveResult = null;
      }

      _hasData = Protocol.HasData();
      if (_hasData)
        session.ActiveResult = this;
    }

    protected ProtocolBase Protocol
    {
      get { return _session.GetProtocol();  }
    }

    internal void AddWarning(WarningInfo w)
    {
      _warnings.Add(w);
    }

    /// <summary>
    /// Warnings derived from statement execution
    /// </summary>
#if NET_45_OR_GREATER
    public IReadOnlyList<WarningInfo> Warnings
#else
    public ReadOnlyCollection<WarningInfo> Warnings
#endif
    {
      get { return _warnings.AsReadOnly(); }
    }

    protected virtual void Buffer() { }
  }
}
