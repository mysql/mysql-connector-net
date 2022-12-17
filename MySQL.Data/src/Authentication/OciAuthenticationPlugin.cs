// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// Enables connections from a user account set with the authentication_iam authentication plugin.
  /// </summary>
  internal class OciAuthenticationPlugin : MySqlAuthenticationPlugin
  {
    public override string PluginName => "authentication_oci_client";
    private Assembly _ociAssembly;
    internal string _ociConfigProfile;

    private const string KEY_FILE = "key_file";
    private const string FINGERPRINT = "fingerprint";
    private const string SECURITY_TOKEN_FILE = "security_token_file";

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

    protected override Task<byte[]> MoreDataAsync(byte[] data, bool execAsync)
    {
      Dictionary<string, Dictionary<string, string>> profiles = LoadOciConfigProfiles();
      GetOciConfigValues(profiles, out string keyFilePath, out string fingerprint, out string securityTokenFilePath);
      string signedToken = SignData(AuthenticationData, keyFilePath);
      string securityToken = LoadSecurityToken(securityTokenFilePath);
      byte[] response = BuildResponse(fingerprint, signedToken, securityToken);

      return Task.FromResult<byte[]>(response);
    }

    /// <summary>
    /// Loads the profiles from the OCI config file.
    /// </summary>
    internal Dictionary<string, Dictionary<string, string>> LoadOciConfigProfiles()
    {
      string ociConfigPath = Settings.OciConfigFile;
      _ociConfigProfile = Settings.OciConfigProfile;
      var configFileReaderType = _ociAssembly.GetType("Oci.Common.ConfigFileReader");
      Dictionary<string, Dictionary<string, string>> profiles;

      if (string.IsNullOrWhiteSpace(ociConfigPath))
      {
        FieldInfo pathField = configFileReaderType.GetField("DEFAULT_FILE_PATH");
        ociConfigPath = (string)pathField.GetValue(null);
      }

      try
      {
        MethodInfo methodInfo = configFileReaderType.GetMethod("Parse", new Type[] { typeof(string), typeof(string) });
        var configFile = methodInfo.Invoke(null, new object[] { ociConfigPath, _ociConfigProfile });
        profiles = (Dictionary<string, Dictionary<string, string>>)configFile.GetType().GetMethod("GetConfiguration").Invoke(configFile, null);
      }
      catch (Exception ex)
      {
        switch (ex.InnerException)
        {
          case IOException:
            throw new MySqlException(Resources.OciConfigFileNotFound, ex);
          case ArgumentException:
            throw new MySqlException(Resources.OciConfigProfileNotFound, ex);
          default:
            throw new MySqlException(string.Format(Resources.AuthenticationFailed, Settings.Server, Settings.UserID, ex.InnerException));
        }
      }

      return profiles;
    }

    /// <summary>
    /// Get the values for the key_file, fingerprint and security_token_file entries.
    /// </summary>
    internal void GetOciConfigValues(Dictionary<string, Dictionary<string, string>> profiles, out string keyFilePath, out string fingerprint, out string securityTokenFilePath)
    {
      profiles.TryGetValue(_ociConfigProfile, out Dictionary<string, string> profileData);

      keyFilePath = profileData.TryGetValue(KEY_FILE, out string keyFilePathValue)
        ? keyFilePathValue : string.Empty;
      fingerprint = profileData.TryGetValue(FINGERPRINT, out string fingerprintValue)
        ? fingerprintValue : string.Empty;
      securityTokenFilePath = profileData.TryGetValue(SECURITY_TOKEN_FILE, out string securityTokenFilePathValue)
        ? securityTokenFilePathValue : string.Empty;

      if (string.IsNullOrEmpty(keyFilePath) || string.IsNullOrEmpty(fingerprint))
        throw new MySqlException(Resources.OciEntryNotFound);
    }

    /// <summary>
    /// Sign nonce sent by server using SHA256 algorithm and the private key provided by the user.
    /// </summary>
    internal static string SignData(byte[] data, string keyFilePath)
    {
      // Init algorithm
      RsaDigestSigner signer256 = new RsaDigestSigner(new Sha256Digest());
      RsaPrivateCrtKeyParameters rsaPrivate;

      // Read key file and security token
      try
      {
        using StreamReader reader = File.OpenText(keyFilePath);
        PemReader pemReader = new PemReader(reader);
        rsaPrivate = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();
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

      return signedString;
    }

    /// <summary>
    /// Reads the security token file and verify it does not exceed the maximum value of 10KB.
    /// </summary>
    /// <param name="securityTokenFilePath">The path to the security token.</param>
    internal static string LoadSecurityToken(string securityTokenFilePath)
    {
      byte[] securityToken = new byte[0];

      try
      {
        if (!string.IsNullOrWhiteSpace(securityTokenFilePath))
        {
          using var reader = File.OpenRead(securityTokenFilePath);

          if (reader.Length > 10240)
            throw new MySqlException(Resources.OciSecurityTokenFileExceeds10KB);

          securityToken = new byte[reader.Length];
          reader.Read(securityToken, 0, securityToken.Length);
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new MySqlException(Resources.OciSecurityTokenDoesNotExists, ex);
      }
      catch (MySqlException ex)
      {
        throw ex;
      }

      return Encoding.UTF8.GetString(securityToken);
    }

    /// <summary>
    /// Wraps up the fingerprint, signature and the token into a JSON format and encode it to a byte array.
    /// </summary>
    /// <returns>The response packet that will be sent to the server.</returns>
    internal static byte[] BuildResponse(string fingerprint, string signature, string token)
    {
      string payload = 
        "{ \"fingerprint\" : \"" + fingerprint + "\" ," +
        " \"signature\": \"" + signature + "\"," +
        " \"token\": \"" + token + "\" }";

      return Encoding.UTF8.GetBytes(payload);
    }
  }
}
