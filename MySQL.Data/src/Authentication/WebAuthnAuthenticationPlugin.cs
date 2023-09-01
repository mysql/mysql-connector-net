// Copyright (c) 2023, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.FIDO;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  internal class WebAuthnAuthenticationPlugin : MySqlAuthenticationPlugin
  {
    public override string PluginName => "authentication_webauthn_client";

    private enum AuthState
    {
      INITIAL,
      REQUEST_CREDENTIAL_ID
    }

    // Constants
    private const int RANDOM_BYTES_LENGTH = 32;
    private const int RELYING_PARTY_ID_MAX_LENGTH = 255;

    // Fields
    private string _devicePath;
    private string _relyingPartyId;
    private string _clientDataJson;
    private byte[] _credentialId;
    private byte[] _clientDataHash;
    private byte[] _challenge;
    private AuthState _state;

    protected override void SetAuthData(byte[] data)
    {
      if (data.Length > 0)
        ParseChallenge(data);
      else
        throw new MySqlException(Resources.FidoRegistrationMissing);
    }

    protected byte[] MoreData(byte[] data)
    {
      switch (_state)
      {
        default:
        case AuthState.INITIAL:
          using (var fidoDeviceInfo = new FidoDeviceInfo())
            _devicePath = fidoDeviceInfo.Path;

          SetClientData();

          using (var fidoDevice = new FidoDevice())
          {
            fidoDevice.Open(_devicePath);

            if (fidoDevice.SupportsCredman)
              return GetAssertion(fidoDevice);
            else
            {
              _state = AuthState.REQUEST_CREDENTIAL_ID;
              return new byte[] { 0x01 }; // status_tag
            }
          }

        case AuthState.REQUEST_CREDENTIAL_ID:
          ParseChallenge(data);

          using (var fidoDevice = new FidoDevice())
          {
            fidoDevice.Open(_devicePath);
            return GetAssertion(fidoDevice);
          }
      }
    }

    /// <summary>
    /// Method that parse the challenge received from server during authentication process.
    /// This method extracts salt and relying party name.
    /// </summary>
    /// <param name="data">Buffer holding the server challenge.</param>
    /// <exception cref="MySqlException">Thrown if an error occurs while parsing the challenge.</exception>
    private void ParseChallenge(byte[] data)
    {
      switch (_state)
      {
        case AuthState.INITIAL:
          int pos = 0;
          // capability (1 byte flag that will be used in the future)
          int capability = data[pos];
          pos++;

          // random bytes (challenge) length should be 32 bytes
          int challengeLength = data[pos];
          pos++;
          if (challengeLength != RANDOM_BYTES_LENGTH) throw new MySqlException(Resources.FidoChallengeCorrupt);
          // client_data_hash
          _challenge = new byte[challengeLength];
          Array.Copy(data, pos, _challenge, 0, challengeLength);
          pos += challengeLength;

          // relyting_party_id length
          int relyingPartyIdLength = data[pos];
          pos++;
          if (relyingPartyIdLength > RELYING_PARTY_ID_MAX_LENGTH) throw new MySqlException(Resources.FidoChallengeCorrupt);
          // relying_party_id (value defined by the @@authentication_webauth_rp_id variable)
          _relyingPartyId = Encoding.GetString(data, pos, relyingPartyIdLength);

          break;
        case AuthState.REQUEST_CREDENTIAL_ID:
          // credential_id length
          int credentialIdLength = data[0];
          // credential_id
          _credentialId = new byte[credentialIdLength];
          Array.Copy(data, 1, _credentialId, 0, credentialIdLength);

          break;
      }
    }

    /// <summary>
    /// Sets the ClientDataHash for the assertion
    /// </summary>
    private void SetClientData()
    {
      //  The challenge should be encoded in base64 which should be url safe.
      string safeUrlEncoded = Encoding.GetString(UrlBase64.Encode(_challenge));

      // ClientDataHash:
      //      ---------------
      //      ClientDataHash is represented as: SHA256(JSON object{ client data})
      // ex:
      // SHA256(
      //   {
      //     "type": "webauthn.get",
      //     "challenge": "",
      //     "origin": ""
      //   }
      // );
      _clientDataJson = $"{{\"type\": \"webauthn.get\"," +
        $"\"challenge\":\"{safeUrlEncoded}\"," +
        $"\"origin\":\"https://{_relyingPartyId}\"," +
        $"\"crossOrigin\":false}}";

      using (var sha256 = SHA256.Create())
        _clientDataHash = sha256.ComputeHash(Encoding.GetBytes(_clientDataJson));
    }

    /// <summary>
    /// Method to obtains an assertion from a FIDO device.
    /// </summary>
    /// <returns>The assertion.</returns>
    /// <exception cref="MySqlException">Thrown if an error occurs while getting the assertion.</exception>
    private byte[] GetAssertion(FidoDevice fidoDevice)
    {
      byte[] clientDataJson = Encoding.GetBytes(_clientDataJson);

      using (var fidoAssertion = new FidoAssertion())
      {
        fidoAssertion.Rp = _relyingPartyId;
        fidoAssertion.ClientDataHash = _clientDataHash;

        if (_state == AuthState.REQUEST_CREDENTIAL_ID)
          fidoAssertion.AllowCredential(_credentialId);

        if (_driver.Settings.WebAuthnActionRequested != null)
          _driver.Settings.WebAuthnActionRequested?.Invoke();
        else
          throw new MySqlException(Resources.WebAuthnMissingHandler);

        var task = Task.Run(() => fidoDevice.GetAssert(fidoAssertion));
        // wait for user interaction with FIDO device (15 seconds)
        if (!task.Wait(15000))
          throw new MySqlException(Resources.WebAuthnTimeout);

        int responseLength = 0;
        int assertionsCount = fidoAssertion.GetAssertCount();
        for (int i = 0; i < assertionsCount; i++)
        {
          var fidoAssertionStatement = fidoAssertion.GetFidoAssertionStatement(i);
          responseLength += Utils.GetLengthSize((ulong)fidoAssertionStatement.SignatureLen) + 1; // +1 for length encoding
          responseLength += Utils.GetLengthSize((ulong)fidoAssertionStatement.AuthDataLen) + 1; // +1 for length encoding
        }
        int clientDataLengthSize = Utils.GetLengthSize((ulong)_clientDataJson.Length);
        responseLength += clientDataJson.Length + clientDataLengthSize + 1 + 2; // +1  for status_tag & +2 for number of assertions and length encoding

        var response = new MySqlPacket(new MemoryStream(responseLength));
        response.Write(new byte[] { 0x02 }); // status_tag
        response.Write(BitConverter.GetBytes(assertionsCount)); // assertions count
        for (int i = 0; i < assertionsCount; i++)
        {
          var fidoAssertionStatement = fidoAssertion.GetFidoAssertionStatement(i);
          response.WriteLength(fidoAssertionStatement.AuthDataLen);
          response.Write(fidoAssertionStatement.AuthData.ToArray());
          response.WriteLength(fidoAssertionStatement.SignatureLen);
          response.Write(fidoAssertionStatement.Signature.ToArray());
        }
        response.WriteLength(clientDataJson.Length);
        response.Write(clientDataJson);

        return response.Buffer;
      }
    }
  }
}
