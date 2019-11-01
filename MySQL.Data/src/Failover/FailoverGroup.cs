// Copyright (c) 2017, 2019, Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Data.Failover
{
    internal abstract class FailoverGroup
    {
      #region Properties

      /// <summary>
      /// Gets and sets the host list.
      /// </summary>
      protected internal List<FailoverServer> Hosts { get; set; }

      /// <summary>
      /// Gets the active host.
      /// </summary>
      protected internal FailoverServer ActiveHost
      {
        get
        {
          if (Hosts == null)
            return null;

          if (_activeHost != null)
            return _activeHost;

          foreach (var host in Hosts)
          {
            if (host.IsActive)
              return host;
          }

          return null;
        }
      }

      #endregion

      #region Fields

      /// <summary>
      /// Active host.
      /// </summary>
      protected internal FailoverServer _activeHost;

      #endregion

      internal FailoverGroup(List<FailoverServer> hosts)
      {
        Hosts = hosts;
        SetInitialActiveServer();
      }

      /// <summary>
      /// Sets the initial active host.
      /// </summary>
      protected internal abstract void SetInitialActiveServer();

      /// <summary>
      /// Determines the next host.
      /// </summary>
      /// <returns><see cref="FailoverServer"/> object that represents the next available host.</returns>
      protected internal abstract FailoverServer GetNextHost();
    }
}
