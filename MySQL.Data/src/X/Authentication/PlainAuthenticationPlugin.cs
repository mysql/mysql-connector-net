// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Authentication;
using System;
using System.Security.Cryptography;
using System.Text;

namespace MySqlX.Security
{
  /// <summary>
  /// Implementation of PLAIN authentication type.
  /// </summary>
  internal class PlainAuthenticationPlugin : Sha256AuthenticationPlugin
  {
    private MySqlConnectionStringBuilder _settings;

    public PlainAuthenticationPlugin(MySqlConnectionStringBuilder settings)
    {
      _settings = settings;
    }

    public override string PluginName
    {
      get { return "Plain Authentication Plugin"; }
    }

    public string AuthName
    {
      get
      {
        return "PLAIN";
      }
    }

    public byte[] GetAuthData()
    {
      return Encoding.UTF8.GetBytes(string.Format("\0{0}\0{1}", _settings.UserID, _settings.Password));
    }
  }
}