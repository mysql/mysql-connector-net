// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.Failover;
using System.Collections.Generic;

namespace MySqlX.Failover
{
    internal abstract class FailoverGroup
    {
      #region Properties

      /// <summary>
      /// Gets and sets the host list.
      /// </summary>
      protected internal List<XServer> Hosts { get; set; }

      /// <summary>
      /// Gets the active host.
      /// </summary>
      protected internal XServer ActiveHost
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
      protected internal XServer _activeHost;

      #endregion

      internal FailoverGroup(List<XServer> hosts)
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
      /// <returns><see cref="XServer"/> object that represents the next available host.</returns>
      protected internal abstract XServer GetNextHost();
    }
}
