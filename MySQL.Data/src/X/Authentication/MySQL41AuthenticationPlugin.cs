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
  /// Implementation of MySQL41 authentication type.
  /// </summary>
  internal class MySQL41AuthenticationPlugin : MySqlNativePasswordPlugin
  {
    private MySqlXConnectionStringBuilder _settings;

    public MySQL41AuthenticationPlugin(MySqlXConnectionStringBuilder settings)
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