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

    protected override Task<byte[]> MoreDataAsync(byte[] data, bool execAsync)
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
