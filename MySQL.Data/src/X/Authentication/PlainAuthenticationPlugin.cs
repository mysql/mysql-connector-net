// Copyright (c) 2017, 2018, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Authentication;
using MySqlX.XDevAPI;
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
    private MySqlXConnectionStringBuilder _settings;

    public PlainAuthenticationPlugin(MySqlXConnectionStringBuilder settings)
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
      return Encoding.UTF8.GetBytes(string.Format("{0}\0{1}\0{2}",
        _settings.Database,
        _settings.UserID,
        _settings.Password
      ));
    }
  }
}