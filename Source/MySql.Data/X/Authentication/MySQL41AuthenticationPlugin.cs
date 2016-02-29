// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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
  /// Implementation of MySQL41 authentication type.
  /// </summary>
  internal class MySQL41AuthenticationPlugin : MySqlNativePasswordPlugin
  {
    private MySqlConnectionStringBuilder _settings;

    public MySQL41AuthenticationPlugin(MySqlConnectionStringBuilder settings)
    {
      _settings = settings;
    }

    public override string PluginName
    {
      get { return "MySQL 4.1 Authentication Plugin"; }
    }

    public string AuthName
    {
      get
      {
        return "MYSQL41";
      }
    }

    public byte[] Continue(byte[] salt)
    {
      var encoding = Encoding.GetEncoding("utf-8");

      byte[] userBytes = encoding.GetBytes(_settings.UserID);
      byte[] databaseBytes = encoding.GetBytes(_settings.Database);
      byte[] hashedPassword = new byte[0];
      byte[] hex = new byte[0];
      if (!string.IsNullOrWhiteSpace(_settings.Password))
      {
        hashedPassword = Get411Password(_settings.Password, salt);
        Array.Copy(hashedPassword, 1, hashedPassword, 0, hashedPassword.Length - 1);
        Array.Resize(ref hashedPassword, hashedPassword.Length - 1);
        //convert to hex value 
        hex = encoding.GetBytes(string.Format("*{0}", BitConverter.ToString(hashedPassword).Replace("-", string.Empty)));
      }

      // create response
      byte[] response = new byte[databaseBytes.Length + userBytes.Length + hex.Length + 2];
      databaseBytes.CopyTo(response, 0);
      var index = databaseBytes.Length;
      response[index++] = 0;
      userBytes.CopyTo(response, index);
      index += userBytes.Length;
      response[index++] = 0;
      hex.CopyTo(response, index);
      return response;
    }
  }
}