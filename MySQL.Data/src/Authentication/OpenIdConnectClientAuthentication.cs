// Copyright © 2024, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Authentication;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  internal class OpenIdConnectClientAuthentication : MySqlAuthenticationPlugin
  {
    public override string PluginName => "authentication_openid_connect_client";

    private static int IdentityToken_sizelimit = 10 * 1024;

    protected override Task<byte[]> MoreDataAsync(byte[] data)
    {
      byte[] IdToken = Encoding.GetBytes(Settings.OpenIdIdentityToken);
      int responseLength = 10;//1 Byte for capability flag. the rest is for Bytes lenenc.
      responseLength += IdToken.Length;

      if (IdToken == null || IdToken.Length == 0)
        throw new ArgumentException(Resources.OpenIdIdentityTokenIsEmpty);

      if (IdToken.Length > IdentityToken_sizelimit)
        throw new ArgumentException(Resources.OpenIdIdentityTokenTooBig);

      var response = new MySqlPacket(new MemoryStream(responseLength));
      response.Write(new byte[] { 0x01 }); //capability flag.
      response.WriteLength(IdToken.Length);
      response.Write(IdToken);

      response.Position = 0;
      return Task.FromResult<byte[]>(response.Buffer);
    }
  }
}
