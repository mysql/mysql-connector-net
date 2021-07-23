// Copyright (c) 2021, Oracle and/or its affiliates.
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

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// Enables connections from a user account set with the authentication_iam authentication plugin.
  /// </summary>
  internal class OciAuthenticationPlugin : MySqlAuthenticationPlugin
  {
    public override string PluginName => "authentication_oci_client";
    private Assembly _ociAssembly;

    private const string DEFAULT_PROFILE = "DEFAULT";
    private const string KEY_FILE = "key_file";
    private const string FINGERPRINT = "fingerprint";

    /// <summary>
    /// Verify that OCI .NET SDK is referenced.
    /// </summary>
    protected override void CheckConstraints()
    {
      try
      {
        _ociAssembly = Assembly.Load("OCI.DotNetSDK.Common");
      }
      catch (Exception ex)
      {
        throw new MySqlException(Resources.OciSDKNotFound, ex);
      }
    }

    protected override void SetAuthData(byte[] data)
    {
      base.SetAuthData(data);
    }

    protected override byte[] MoreData(byte[] data)
    {
      string ociConfigPath = Settings.OciConfigFile;
      var configFileReaderType = _ociAssembly.GetType("Oci.Common.ConfigFileReader");
      Dictionary<string, Dictionary<string, string>> profiles;

      if (string.IsNullOrWhiteSpace(ociConfigPath))
      {
        FieldInfo pathField = configFileReaderType.GetField("DEFAULT_FILE_PATH");
        ociConfigPath = (string)pathField.GetValue(null);
      }

      try
      {
        MethodInfo methodInfo = configFileReaderType.GetMethod("Parse", new Type[] { typeof(string) });
        var configFile = methodInfo.Invoke(null, new object[] { ociConfigPath });
        profiles = (Dictionary<string, Dictionary<string, string>>)configFile.GetType().GetMethod("GetConfiguration").Invoke(configFile, null);
      }
      catch (Exception ex)
      {
        throw new MySqlException(Resources.OciConfigFileNotFound, ex);
      }

      GetValues(profiles, out string keyFilePath, out string fingerprint);
      byte[] signedToken = SignData(AuthenticationData, keyFilePath, fingerprint);

      return signedToken;
    }

    /// <summary>
    /// Get the values for the key_file and fingerprint entries.
    /// </summary>
    internal void GetValues(Dictionary<string, Dictionary<string, string>> profiles, out string keyFilePath, out string fingerprint)
    {
      keyFilePath = string.Empty;
      fingerprint = string.Empty;

      if (profiles.ContainsKey(DEFAULT_PROFILE) && profiles[DEFAULT_PROFILE].ContainsKey(KEY_FILE)
        && profiles[DEFAULT_PROFILE].ContainsKey(FINGERPRINT))
      {
        keyFilePath = profiles[DEFAULT_PROFILE][KEY_FILE];
        fingerprint = profiles[DEFAULT_PROFILE][FINGERPRINT];
      }
      else
      {
        foreach (var profile in profiles)
        {
          if (profile.Value.ContainsKey(KEY_FILE) && profile.Value.ContainsKey(FINGERPRINT))
          {
            keyFilePath = profile.Value[KEY_FILE];
            fingerprint = profile.Value[FINGERPRINT];
          }
        }
      }

      if (string.IsNullOrEmpty(keyFilePath) || string.IsNullOrEmpty(fingerprint))
        throw new MySqlException(Resources.OciEntryNotFound);
    }

    /// <summary>
    /// Sign nonce sent by server using SHA256 algorithm and the private key provided by the user.
    /// </summary>
    internal byte[] SignData(byte[] data, string keyFilePath, string fingerprint)
    {
      // Init algorithm
      RsaDigestSigner signer256 = new RsaDigestSigner(new Sha256Digest());
      RsaPrivateCrtKeyParameters rsaPrivate;

      // Read key file
      try
      {
        using (var reader = File.OpenText(keyFilePath))
        {
          PemReader pemReader = new PemReader(reader);
          rsaPrivate = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();
        }
      }
      catch (Exception ex)
      {
        throw new MySqlException(Resources.OciKeyFileDoesNotExists, ex);
      }

      if (rsaPrivate == null)
        throw new MySqlException(Resources.OciInvalidKeyFile);

      // Populate with key 
      signer256.Init(true, rsaPrivate);
      // Calculate the signature
      signer256.BlockUpdate(data, 0, data.Length);
      // Generates signature
      byte[] sig = signer256.GenerateSignature();
      // Base 64 encode the sig so its 8-bit clean
      string signedString = Convert.ToBase64String(sig);

      string payload = "{ \"fingerprint\" : \"" + fingerprint + "\" , \"signature\": \"" + signedString + "\" }";
      byte[] result = Encoding.UTF8.GetBytes(payload);

      return result;
    }
  }
}
