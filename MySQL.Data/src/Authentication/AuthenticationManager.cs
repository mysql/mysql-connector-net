// Copyright © 2012, 2017, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Reflection;
using MySql.Data.MySqlClient;


namespace MySql.Data.MySqlClient.Authentication
{
  internal partial class AuthenticationPluginManager
  {
    private static readonly Dictionary<string, PluginInfo> Plugins = new Dictionary<string, PluginInfo>();

    static partial void AuthenticationManagerCtorConfiguration();

    static AuthenticationPluginManager()
    {
      Plugins["mysql_native_password"] = new PluginInfo("MySql.Data.MySqlClient.Authentication.MySqlNativePasswordPlugin");
      Plugins["sha256_password"] = new PluginInfo("MySql.Data.MySqlClient.Authentication.Sha256AuthenticationPlugin");
      Plugins["authentication_windows_client"] = new PluginInfo("MySql.Data.MySqlClient.Authentication.MySqlWindowsAuthenticationPlugin");
      Plugins["caching_sha2_password"] = new PluginInfo("MySql.Data.MySqlClient.Authentication.CachingSha2AuthenticationPlugin");

      AuthenticationManagerCtorConfiguration();
    }

    public static MySqlAuthenticationPlugin GetPlugin(string method)
    {
      if (!Plugins.ContainsKey(method))
        throw new MySqlException(String.Format(Resources.AuthenticationMethodNotSupported, method));
      return CreatePlugin(method);
    }

    private static MySqlAuthenticationPlugin CreatePlugin(string method)
    {
      PluginInfo pi = Plugins[method];

      try
      {
        Type t = Type.GetType(pi.Type);
        MySqlAuthenticationPlugin o = (MySqlAuthenticationPlugin)Activator.CreateInstance(t);
        return o;
      }
      catch (Exception e)
      {
        throw new MySqlException(String.Format(Resources.UnableToCreateAuthPlugin, method), e);
      }
    }
  }

  struct PluginInfo
  {
    public string Type;
    public Assembly Assembly;

    public PluginInfo(string type)
    {
      Type = type;
      Assembly = null;
    }
  }
}
