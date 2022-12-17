// Copyright (c) 2021, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.GSSAPI;
using MySql.Data.Authentication.SSPI;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// Enables connections to a user account set with the authentication_kerberos authentication plugin.
  /// </summary>
  internal class KerberosAuthenticationPlugin : MySqlAuthenticationPlugin
  {
    private const string PACKAGE = "Kerberos";
    public override string PluginName => "authentication_kerberos_client";
    protected string Username { get; private set; }
    protected string Password => GetMFAPassword();
    private string _servicePrincipal;
    private string _realm;
    private GssapiMechanism _gssapiMechanism;
    private SspiSecurityContext _sspiSecurityContext;

    protected override void SetAuthData(byte[] data)
    {
      Username = GetUsername();

      //Protocol::AuthSwitchRequest plugin data contains:
      // int<2> SPN string length
      // string<VAR> SPN string
      // int<2> User Principal Name realm string length
      // string<VAR> User Principal Name realm string
      short servicePrincipalNameLength = BitConverter.ToInt16(data, 0);
      if (servicePrincipalNameLength > data.Length) return; // not an AuthSwitchRequest
      _servicePrincipal = Encoding.GetString(data, 2, servicePrincipalNameLength);
      short userPrincipalRealmLength = BitConverter.ToInt16(data, servicePrincipalNameLength + 2);
      _realm = Encoding.GetString(data, servicePrincipalNameLength + 4, userPrincipalRealmLength);
    }

    public override string GetUsername()
    {
      Username = string.IsNullOrWhiteSpace(Username) ? Settings.UserID : Username;

      if (string.IsNullOrWhiteSpace(Username))
      {
        if (Settings.KerberosAuthMode == KerberosAuthMode.GSSAPI)
        {
          try
          {
            // Try to obtain the user name from a cached TGT
            Username = new GssCredentials().UserName.Trim();
          }
          catch (Exception)
          {
            // Fall-back to system login user
            Username = base.GetUsername();
          }
        }
        else
        {
          // Use system login user
          Username = base.GetUsername();
        }
      }
      else
      {
        // If no password is provided, MySQL user and Windows logged-in user should match for SSPI mode
        if (Settings.KerberosAuthMode == KerberosAuthMode.SSPI && string.IsNullOrWhiteSpace(Password) && Username != Environment.UserName)
          throw new MySqlException(string.Format(Resources.UnmatchedWinUserAndMySqlUser, Username, Environment.UserName));
      }

      int posAt = Username.IndexOf('@');
      return posAt < 0 ? Username : Username.Substring(0, posAt);
    }

    protected override Task<byte[]> MoreDataAsync(byte[] data, bool execAsync)
    {
      if (Settings.KerberosAuthMode == KerberosAuthMode.GSSAPI)
        return Task.FromResult<byte[]>(GssapiMode(data));
      else
        return Task.FromResult<byte[]>(SspiMode(data));
    }

    private byte[] SspiMode(byte[] data)
    {
      if (_sspiSecurityContext == null)
      {
        string upn = $"{Username}@{_realm}";
        var sspiCreds = string.IsNullOrWhiteSpace(Password) ? new SspiCredentials(PACKAGE) : new SspiCredentials(_servicePrincipal, upn, Password, _realm, PACKAGE);
        _sspiSecurityContext = new SspiSecurityContext(sspiCreds);
      }

      var status = _sspiSecurityContext.InitializeSecurityContext(out byte[] clientBlob, data, _servicePrincipal);

      if (clientBlob.Length == 0 && status == ContextStatus.Accepted)
        return null;

      return clientBlob;
    }

    private byte[] GssapiMode(byte[] data)
    {
      if (_gssapiMechanism == null)
      {
        string upn = $"{Username}@{_realm}";
        _gssapiMechanism = new GssapiMechanism(upn, Password, _servicePrincipal);
      }

      var response = _gssapiMechanism.Challenge(data);

      if (response.Length == 0 && _gssapiMechanism.gssContext.IsEstablished)
        return null;

      return response;
    }
  }
}