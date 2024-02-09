// Copyright © 2012, 2024, Oracle and/or its affiliates.
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

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// Defines the default behavior for an authentication plugin.
  /// </summary>
  public abstract class MySqlAuthenticationPlugin
  {
    internal NativeDriver _driver;

    /// <summary>
    /// Handles the iteration of the multifactor authentication.
    /// </summary>
    private int _mfaIteration = 1;

    /// <summary>
    /// Gets the AuthPlugin name of the AuthSwitchRequest.
    /// </summary>
    internal string SwitchedPlugin { get; private set; }

    /// <summary>
    /// Gets or sets the authentication data returned by the server.
    /// </summary>
    protected byte[] AuthenticationData;

    /// <summary>
    /// This is a factory method that is used only internally.  It creates an auth plugin based on the method type
    /// </summary>
    /// <param name="method">Authentication method.</param>
    /// <param name="driver">The driver.</param>
    /// <param name="authData">The authentication data.</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <param name="mfaIteration">MultiFactorAuthentication iteration.</param>
    /// <returns></returns>
    internal static async Task<MySqlAuthenticationPlugin> GetPluginAsync(string method, NativeDriver driver, byte[] authData, bool execAsync, int mfaIteration = 1)
    {
      if (method == "mysql_old_password")
      {
        await driver.CloseAsync(true, execAsync).ConfigureAwait(false);
        throw new MySqlException(Resources.OldPasswordsNotSupported);
      }
      MySqlAuthenticationPlugin plugin = AuthenticationPluginManager.GetPlugin(method);
      if (plugin == null)
        throw new MySqlException(String.Format(Resources.UnknownAuthenticationMethod, method));

      plugin._driver = driver;
      plugin._mfaIteration = mfaIteration;
      plugin.SetAuthData(authData);
      return plugin;
    }

    /// <summary>
    /// Gets the connection option settings.
    /// </summary>
    protected MySqlConnectionStringBuilder Settings => _driver.Settings;

    /// <summary>
    /// Gets the server version associated with this authentication plugin.
    /// </summary>
    protected Version ServerVersion => new Version(_driver.Version.Major, _driver.Version.Minor, _driver.Version.Build);

    internal ClientFlags Flags => _driver.Flags;

    /// <summary>
    /// Gets the encoding assigned to the native driver.
    /// </summary>
    protected Encoding Encoding => _driver.Encoding;

    /// <summary>
    /// Sets the authentication data required to encode, encrypt, or convert the password of the user.
    /// </summary>
    /// <param name="data">A byte array containing the authentication data provided by the server.</param>
    /// <remarks>This method may be overriden based on the requirements by the implementing authentication plugin.</remarks>
    protected virtual void SetAuthData(byte[] data)
    {
      AuthenticationData = data;
    }

    /// <summary>
    /// Defines the behavior when checking for constraints.
    /// </summary>
    /// <remarks>This method is intended to be overriden.</remarks>
    protected virtual void CheckConstraints()
    {
    }

    /// <summary>
    /// Throws a <see cref="MySqlException"/> that encapsulates the original exception.
    /// </summary>
    /// <param name="ex">The exception to encapsulate.</param>
    protected virtual void AuthenticationFailed(MySqlException ex)
    {
      string msg = String.Format(Resources.AuthenticationFailed, Settings.Server, GetUsername(), PluginName, ex.Message);
      throw new MySqlException(msg, ex.Number, ex);
    }

    /// <summary>
    /// Defines the behavior when authentication is successful.
    /// </summary>
    /// <remarks>This method is intended to be overriden.</remarks>
    protected virtual void AuthenticationSuccessful()
    {
    }

    /// <summary>
    /// Defines the behavior when more data is required from the server.
    /// </summary>
    /// <param name="data">The data returned by the server.</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <returns>The data to return to the server.</returns>
    /// <remarks>This method is intended to be overriden.</remarks>
    protected virtual Task<byte[]> MoreDataAsync(byte[] data, bool execAsync)
    {
      return Task.FromResult<byte[]>(null);
    }

    internal async Task AuthenticateAsync(bool reset, bool execAsync)
    {
      CheckConstraints();

      MySqlPacket packet = _driver.Packet;

      // send auth response
      await packet.WriteStringAsync(GetUsername(), execAsync).ConfigureAwait(false);

      // now write the password
      await WritePasswordAsync(packet, execAsync).ConfigureAwait(false);

      if ((Flags & ClientFlags.CONNECT_WITH_DB) != 0 || reset)
      {
        if (!String.IsNullOrEmpty(Settings.Database))
          await packet.WriteStringAsync(Settings.Database, execAsync).ConfigureAwait(false);
      }

      if (reset)
        await packet.WriteIntegerAsync(8, 2, execAsync).ConfigureAwait(false);

      if ((Flags & ClientFlags.PLUGIN_AUTH) != 0)
        await packet.WriteStringAsync(PluginName, execAsync).ConfigureAwait(false);

      await _driver.SetConnectAttrsAsync(execAsync).ConfigureAwait(false);
      await _driver.SendPacketAsync(packet, execAsync);

      // Read server response.
      packet = await ReadPacketAsync(execAsync).ConfigureAwait(false);
      byte[] b = packet.Buffer;

      if (PluginName == "caching_sha2_password" && b[0] == 0x01)
      {
        // React to the authentication type set by server: FAST, FULL.
        await ContinueAuthenticationAsync(execAsync, new byte[] { b[1] }).ConfigureAwait(false);
      }

      // Auth switch request Protocol::AuthSwitchRequest.
      if (b[0] == 0xfe)
      {
        if (packet.IsLastPacket)
        {
          await _driver.CloseAsync(true, execAsync).ConfigureAwait(false);
          throw new MySqlException(Resources.OldPasswordsNotSupported);
        }
        else
        {
          await HandleAuthChangeAsync(packet, execAsync).ConfigureAwait(false);
        }
      }

      // Auth request Protocol::AuthNextFactor.
      while (packet.Buffer[0] == 0x02)
      {
        ++_mfaIteration;
        await HandleMFAAsync(packet, execAsync).ConfigureAwait(false);
      }

      await _driver.ReadOkAsync(false, execAsync).ConfigureAwait(false);

      AuthenticationSuccessful();
    }

    private async Task WritePasswordAsync(MySqlPacket packet, bool execAsync)
    {
      bool secure = (Flags & ClientFlags.SECURE_CONNECTION) != 0;
      object password = GetPassword();
      if (password is string)
      {
        if (secure)
          await packet.WriteLenStringAsync((string)password, execAsync).ConfigureAwait(false);
        else
          await packet.WriteStringAsync((string)password, execAsync).ConfigureAwait(false);
      }
      else if (password == null)
        packet.WriteByte(0);
      else if (password is byte[])
        await packet.WriteAsync(password as byte[], execAsync).ConfigureAwait(false);
      else throw new MySqlException("Unexpected password format: " + password.GetType());
    }

    internal async Task<MySqlPacket> ReadPacketAsync(bool execAsync)
    {
      try
      {
        MySqlPacket p = await _driver.ReadPacketAsync(execAsync).ConfigureAwait(false);
        return p;
      }
      catch (MySqlException ex)
      {
        // Make sure this is an auth failed ex
        AuthenticationFailed(ex);
        return null;
      }
    }

    private async Task HandleMFAAsync(MySqlPacket packet, bool execAsync)
    {
      byte b = packet.ReadByte();
      Debug.Assert(b == 0x02);

      var nextPlugin = await NextPluginAsync(packet, execAsync).ConfigureAwait(false);
      nextPlugin.CheckConstraints();
      await nextPlugin.ContinueAuthenticationAsync(execAsync).ConfigureAwait(false);
    }

    private async Task HandleAuthChangeAsync(MySqlPacket packet, bool execAsync)
    {
      byte b = packet.ReadByte();
      Debug.Assert(b == 0xfe);

      var nextPlugin = await NextPluginAsync(packet, execAsync).ConfigureAwait(false);
      nextPlugin.CheckConstraints();
      await nextPlugin.ContinueAuthenticationAsync(execAsync).ConfigureAwait(false);
    }

    private async Task<MySqlAuthenticationPlugin> NextPluginAsync(MySqlPacket packet, bool execAsync)
    {
      string method = packet.ReadString();
      SwitchedPlugin = method;
      byte[] authData = new byte[packet.Length - packet.Position];
      Array.Copy(packet.Buffer, packet.Position, authData, 0, authData.Length);

      MySqlAuthenticationPlugin plugin = await GetPluginAsync(method, _driver, authData, execAsync, _mfaIteration).ConfigureAwait(false);
      return plugin;
    }

    private async Task ContinueAuthenticationAsync(bool execAsync, byte[] data = null)
    {
      MySqlPacket packet = _driver.Packet;
      packet.Clear();

      byte[] moreData = await MoreDataAsync(data, execAsync).ConfigureAwait(false);

      while (moreData != null)
      {
        packet.Clear();
        await packet.WriteAsync(moreData, execAsync).ConfigureAwait(false);
        await _driver.SendPacketAsync(packet, execAsync).ConfigureAwait(false);

        packet = await ReadPacketAsync(execAsync).ConfigureAwait(false);
        byte prefixByte = packet.Buffer[0];
        if (prefixByte != 1) return;

        // A prefix of 0x01 means need more auth data.
        byte[] responseData = new byte[packet.Length - 1];
        Array.Copy(packet.Buffer, 1, responseData, 0, responseData.Length);
        moreData = await MoreDataAsync(responseData, execAsync).ConfigureAwait(false);
      }
      // We get here if MoreData returned null but the last packet read was a more data packet.
      await ReadPacketAsync(execAsync).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the password for the iteration of the multifactor authentication 
    /// </summary>
    /// <returns>A password</returns>
    protected string GetMFAPassword()
    {
      switch (_mfaIteration)
      {
        case 2:
          return Settings.Password2;
        case 3:
          return Settings.Password3;
        default:
          return Settings.Password;
      }
    }

    /// <summary>
    /// Gets the plugin name based on the authentication plugin type defined during the creation of this object.
    /// </summary>
    public abstract string PluginName { get; }

    /// <summary>
    /// Gets the user name associated to the connection settings.
    /// </summary>
    /// <returns>The user name associated to the connection settings.</returns>
    public virtual string GetUsername()
    {
      return !string.IsNullOrWhiteSpace(Settings.UserID) ? Settings.UserID : Environment.UserName;
    }

    /// <summary>
    /// Gets the encoded, encrypted, or converted password based on the authentication plugin type defined during the creation of this object.
    /// This method is intended to be overriden.
    /// </summary>
    /// <returns>An object containing the encoded, encrypted, or converted password.</returns>
    public virtual object GetPassword()
    {
      return null;
    }
  }
}
