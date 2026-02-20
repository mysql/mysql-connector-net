// Copyright © 2004, 2026, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient.Authentication;
using MySql.Data.Types;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for Driver.
  /// </summary>
  internal class NativeDriver : IDriver
  {
    private DBVersion version;
    private int threadId;
    protected byte[] encryptionSeed;
    protected ServerStatusFlags serverStatus;
    protected MySqlStream stream;
    protected Stream baseStream;
    protected MyNetworkStream networkStream;
    private BitArray nullMap;
    private MySqlPacket packet;
    private ClientFlags connectionFlags;
    private Driver owner;
    private int warnings;
    private MySqlAuthenticationPlugin authPlugin;

    // Regular expression that checks for GUID format 
    private static Regex guidRegex = new Regex(@"(?i)^[0-9A-F]{8}[-](?:[0-9A-F]{4}[-]){3}[0-9A-F]{12}$");

    public NativeDriver(Driver owner)
    {
      this.owner = owner;
      threadId = -1;
    }

    public ClientFlags Flags
    {
      get { return connectionFlags; }
    }

    public int ThreadId
    {
      get { return threadId; }
    }

    public DBVersion Version
    {
      get { return version; }
    }

    public ServerStatusFlags ServerStatus
    {
      get { return serverStatus; }
    }

    public int WarningCount
    {
      get { return warnings; }
    }

    public MySqlPacket Packet
    {
      get { return packet; }
    }

    internal MySqlConnectionStringBuilder Settings
    {
      get { return owner.Settings; }
    }

    internal Encoding Encoding
    {
      get { return owner.Encoding; }
    }

    /// <summary>
    /// Private method to handle a MySqlException by closing the owner connection if fatal.
    /// </summary>
    /// <param name="ex">The MySqlException to handle.</param>
    private void HandleException(MySqlException ex)
    {
      if (ex.IsFatal)
        owner.Close();
    }

    /// <summary>
    /// Private asynchronous method to handle a MySqlException by asynchronously closing the owner connection if fatal.
    /// </summary>
    /// <param name="ex">The MySqlException to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleExceptionAsync(MySqlException ex)
    {
      if (ex.IsFatal)
        await owner.CloseAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a MySqlPacket over the stream.
    /// </summary>
    /// <param name="p">The packet to send.</param>
    internal void SendPacket(MySqlPacket p)
    {
      stream.SendPacket(p);
    }

    /// <summary>
    /// Asynchronously sends a MySqlPacket over the stream.
    /// </summary>
    /// <param name="p">The packet to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task SendPacketAsync(MySqlPacket p)
    {
      await stream.SendPacketAsync(p).ConfigureAwait(false);
    }

    internal async Task SendEmptyPacketAsync()
    {
      byte[] buffer = new byte[4];
      await stream.SendEntirePacketDirectlyAsync(buffer, 0).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the next MySqlPacket from the stream.
    /// </summary>
    /// <returns>The read MySqlPacket.</returns>
    internal MySqlPacket ReadPacket()
    {
      return packet = stream.ReadPacket();
    }

    /// <summary>
    /// Asynchronously reads the next MySqlPacket from the stream.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, returning the read MySqlPacket.</returns>
    internal async Task<MySqlPacket> ReadPacketAsync()
    {
      return packet = await stream.ReadPacketAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reads an OK packet from the stream, parsing server status and handling exceptions.
    /// </summary>
    /// <param name="read">Whether to read the packet first.</param>
    /// <returns>The parsed OkPacket.</returns>
    /// <exception cref="MySqlException">Thrown on sync issues or other errors.</exception>
    internal OkPacket ReadOk(bool read)
    {
      try
      {
        if (read)
          packet = stream.ReadPacket();

        byte header = packet.ReadByte();
        if (header != 0)
        {
          throw new MySqlException("Out of sync with server", true, null);
        }

        OkPacket okPacket = OkPacket.Create(packet);
        serverStatus = okPacket.ServerStatusFlags;

        return okPacket;
      }
      catch (MySqlException ex)
      {
        HandleException(ex);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously reads an OK packet from the stream, parsing server status and handling exceptions.
    /// </summary>
    /// <param name="read">Whether to read the packet first.</param>
    /// <returns>A task representing the asynchronous operation, returning the parsed OkPacket.</returns>
    /// <exception cref="MySqlException">Thrown on sync issues or other errors.</exception>
    internal async Task<OkPacket> ReadOkAsync(bool read)
    {
      try
      {
        if (read)
          packet = await stream.ReadPacketAsync().ConfigureAwait(false);

        byte header = packet.ReadByte();
        if (header != 0)
        {
          throw new MySqlException("Out of sync with server", true, null);
        }

        OkPacket okPacket = await OkPacket.CreateAsync(packet).ConfigureAwait(false);
        serverStatus = okPacket.ServerStatusFlags;

        return okPacket;
      }
      catch (MySqlException ex)
      {
        await HandleExceptionAsync(ex).ConfigureAwait(false);
        throw;
      }
    }

    /// <summary>
    /// Sets the current database for the connection by sending an INIT_DB command.
    /// </summary>
    /// <param name="dbName">The database name to set.</param>
    public void SetDatabase(string dbName)
    {
      byte[] dbNameBytes = Encoding.GetBytes(dbName);

      packet.Clear();
      packet.WriteByte((byte)DBCmd.INIT_DB);
      packet.Write(dbNameBytes);
      ExecutePacket(packet);

      ReadOk(true);
    }

    /// <summary>
    /// Asynchronously sets the current database for the connection by sending an INIT_DB command.
    /// </summary>
    /// <param name="dbName">The database name to set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetDatabaseAsync(string dbName)
    {
      byte[] dbNameBytes = Encoding.GetBytes(dbName);

      packet.Clear();
      packet.WriteByte((byte)DBCmd.INIT_DB);
      await packet.WriteAsync(dbNameBytes).ConfigureAwait(false);
      await ExecutePacketAsync(packet).ConfigureAwait(false);

      await ReadOkAsync(true).ConfigureAwait(false);
    }

    public void Configure()
    {
      stream.MaxPacketSize = (ulong)owner.MaxPacketSize;
      stream.Encoding = Encoding;
    }

    /// <summary>
    /// Opens the connection to the MySQL server, reading greeting packet, negotiating capabilities, handling SSL if requested, and authenticating.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="MySqlException">Thrown on connection, auth, or protocol errors.</exception>
    public void Open(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // connect to one of our specified hosts
      try
      {
        var result = StreamCreator.GetStream(Settings, cancellationToken);

        baseStream = result.Item1;
        networkStream = result.Item2;

        if (Settings.IncludeSecurityAsserts)
          MySqlSecurityPermission.CreatePermissionSet(false).Assert();
      }
      catch (System.Security.SecurityException) { throw; }
      catch (TimeoutException) { throw; }
      catch (AggregateException ae)
      {
        ae.Handle(ex =>
        {
          if (ex is System.Net.Sockets.SocketException)
            throw new MySqlException(Resources.UnableToConnectToHost, (int)MySqlErrorCode.UnableToConnectToHost, ex);
          return ex is MySqlException;
        });
      }
      catch (Exception ex)
      {
        throw new MySqlException(Resources.UnableToConnectToHost, (int)MySqlErrorCode.UnableToConnectToHost, ex);
      }

      if (baseStream == null)
        throw new MySqlException(Resources.UnableToConnectToHost, (int)MySqlErrorCode.UnableToConnectToHost);

      int maxSinglePacket = 255 * 255 * 255;
      stream = new MySqlStream(baseStream, Encoding, false, networkStream?.Socket);

      stream.ResetTimeout((int)Settings.ConnectionTimeout * 1000);

      // read off the welcome packet and parse out it's values
      packet = stream.ReadPacket();

      int protocol = packet.ReadByte();
      if (protocol != 10)
        throw new MySqlException("Unsupported protocol version.");
      string versionString = packet.ReadString();
      version = DBVersion.Parse(versionString);
      threadId = packet.ReadInteger(4);

      byte[] seedPart1 = packet.ReadStringAsBytes();

      maxSinglePacket = (256 * 256 * 256) - 1;

      // read in Server capabilities if they are provided
      ClientFlags serverCaps = 0;
      if (packet.HasMoreData)
        serverCaps = (ClientFlags)packet.ReadInteger(2);

      /* New protocol with 16 bytes to describe server characteristics */
      owner.ConnectionCharSetIndex = (int)packet.ReadByte();

      serverStatus = (ServerStatusFlags)packet.ReadInteger(2);

      // Since 5.5, high bits of server caps are stored after status.
      // Previously, it was part of reserved always 0x00 13-byte filler.
      uint serverCapsHigh = (uint)packet.ReadInteger(2);
      serverCaps |= (ClientFlags)(serverCapsHigh << 16);

      packet.Position += 11;
      byte[] seedPart2 = packet.ReadStringAsBytes();
      encryptionSeed = new byte[seedPart1.Length + seedPart2.Length];
      seedPart1.CopyTo(encryptionSeed, 0);
      seedPart2.CopyTo(encryptionSeed, seedPart1.Length);

      string authenticationMethod = Settings.DefaultAuthenticationPlugin;
      if (string.IsNullOrWhiteSpace(authenticationMethod))
      {
        if ((serverCaps & ClientFlags.PLUGIN_AUTH) != 0)
          authenticationMethod = packet.ReadString();
        else
          // Some MySql versions like 5.1, don't give name of plugin, default to native password.
          authenticationMethod = "mysql_native_password";
      }

      // based on our settings, set our connection flags
      SetConnectionFlags(serverCaps);

      packet.Clear();
      packet.WriteInteger((int)connectionFlags, 4);
      packet.WriteInteger(maxSinglePacket, 4);
      packet.WriteByte(33); //character set utf-8
      packet.Write(new byte[23]);

      // Server doesn't support SSL connections
      if ((serverCaps & ClientFlags.SSL) == 0)
      {
        if (Settings.SslMode != MySqlSslMode.Disabled && Settings.SslMode != MySqlSslMode.Preferred)
          throw new MySqlException(string.Format(Resources.NoServerSSLSupport, Settings.Server));
      }
      // Current connection doesn't support SSL connections
      else if ((connectionFlags & ClientFlags.SSL) == 0)
      {
        if (Settings.SslMode != MySqlSslMode.Disabled && Settings.SslMode != MySqlSslMode.Preferred)
          throw new MySqlException(string.Format(Resources.SslNotAllowedForConnectionProtocol, Settings.ConnectionProtocol));
      }
      // Server and connection supports SSL connections and Client are requisting a secure connection
      else
      {
        stream.SendPacket(packet);
        var result = new Ssl(Settings).StartSSL(baseStream, Encoding, Settings.ToString(), cancellationToken);
        stream = result.Item1;
        baseStream = result.Item2;
        packet.Clear();
        packet.WriteInteger((int)connectionFlags, 4);
        packet.WriteInteger(maxSinglePacket, 4);
        packet.WriteByte(33); //character set utf-8
        packet.Write(new byte[23]);
      }

      try
      {
        Authenticate(authenticationMethod, false);
      }
      catch (Exception)
      {
        // If the authenticationMethod is kerberos and KerberosAuthMode is on AUTO, it will retry the connection using GSSAPI mode
        if ((authenticationMethod == "authentication_kerberos_client" || authPlugin.SwitchedPlugin == "authentication_kerberos_client")
          && Settings.KerberosAuthMode == KerberosAuthMode.AUTO)
        {
          Settings.KerberosAuthMode = KerberosAuthMode.GSSAPI;
          Open(cancellationToken);
        }
        else
          throw;
      }

      // if we are using compression, then we use our CompressedStream class
      // to hide the ugliness of managing the compression
      if ((connectionFlags & ClientFlags.COMPRESS) != 0)
        stream = new MySqlStream(baseStream, Encoding, true, networkStream?.Socket);

      // give our stream the server version we are connected to.  
      // We may have some fields that are read differently based 
      // on the version of the server we are connected to.
      packet.Version = version;
      stream.MaxBlockSize = maxSinglePacket;
    }

    /// <summary>
    /// Asynchronously opens the connection to the MySQL server, reading greeting packet, negotiating capabilities, handling SSL if requested, and authenticating.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="MySqlException">Thrown on connection, auth, or protocol errors.</exception>
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // connect to one of our specified hosts
      try
      {
        var result = await StreamCreator.GetStreamAsync(Settings, cancellationToken).ConfigureAwait(false);

        baseStream = result.Item1;
        networkStream = result.Item2;

        if (Settings.IncludeSecurityAsserts)
          MySqlSecurityPermission.CreatePermissionSet(false).Assert();
      }
      catch (System.Security.SecurityException) { throw; }
      catch (TimeoutException) { throw; }
      catch (AggregateException ae)
      {
        ae.Handle(ex =>
        {
          if (ex is System.Net.Sockets.SocketException)
            throw new MySqlException(Resources.UnableToConnectToHost, (int)MySqlErrorCode.UnableToConnectToHost, ex);
          return ex is MySqlException;
        });
      }
      catch (Exception ex)
      {
        throw new MySqlException(Resources.UnableToConnectToHost, (int)MySqlErrorCode.UnableToConnectToHost, ex);
      }

      if (baseStream == null)
        throw new MySqlException(Resources.UnableToConnectToHost, (int)MySqlErrorCode.UnableToConnectToHost);

      int maxSinglePacket = 255 * 255 * 255;
      stream = new MySqlStream(baseStream, Encoding, false, networkStream?.Socket);

      stream.ResetTimeout((int)Settings.ConnectionTimeout * 1000);

      // read off the welcome packet and parse out it's values
      packet = await stream.ReadPacketAsync().ConfigureAwait(false);

      int protocol = packet.ReadByte();
      if (protocol != 10)
        throw new MySqlException("Unsupported protocol version.");
      string versionString = packet.ReadString();
      version = DBVersion.Parse(versionString);
      threadId = packet.ReadInteger(4);

      byte[] seedPart1 = packet.ReadStringAsBytes();

      maxSinglePacket = (256 * 256 * 256) - 1;

      // read in Server capabilities if they are provided
      ClientFlags serverCaps = 0;
      if (packet.HasMoreData)
        serverCaps = (ClientFlags)packet.ReadInteger(2);

      /* New protocol with 16 bytes to describe server characteristics */
      owner.ConnectionCharSetIndex = (int)packet.ReadByte();

      serverStatus = (ServerStatusFlags)packet.ReadInteger(2);

      // Since 5.5, high bits of server caps are stored after status.
      // Previously, it was part of reserved always 0x00 13-byte filler.
      uint serverCapsHigh = (uint)packet.ReadInteger(2);
      serverCaps |= (ClientFlags)(serverCapsHigh << 16);

      packet.Position += 11;
      byte[] seedPart2 = packet.ReadStringAsBytes();
      encryptionSeed = new byte[seedPart1.Length + seedPart2.Length];
      seedPart1.CopyTo(encryptionSeed, 0);
      seedPart2.CopyTo(encryptionSeed, seedPart1.Length);

      string authenticationMethod = Settings.DefaultAuthenticationPlugin;
      if (string.IsNullOrWhiteSpace(authenticationMethod))
      {
        if ((serverCaps & ClientFlags.PLUGIN_AUTH) != 0)
          authenticationMethod = packet.ReadString();
        else
          // Some MySql versions like 5.1, don't give name of plugin, default to native password.
          authenticationMethod = "mysql_native_password";
      }

      // based on our settings, set our connection flags
      SetConnectionFlags(serverCaps);

      packet.Clear();
      await packet.WriteIntegerAsync((int)connectionFlags, 4).ConfigureAwait(false);
      await packet.WriteIntegerAsync(maxSinglePacket, 4).ConfigureAwait(false);
      packet.WriteByte(33); //character set utf-8
      await packet.WriteAsync(new byte[23]).ConfigureAwait(false);

      // Server doesn't support SSL connections
      if ((serverCaps & ClientFlags.SSL) == 0)
      {
        if (Settings.SslMode != MySqlSslMode.Disabled && Settings.SslMode != MySqlSslMode.Preferred)
          throw new MySqlException(string.Format(Resources.NoServerSSLSupport, Settings.Server));
      }
      // Current connection doesn't support SSL connections
      else if ((connectionFlags & ClientFlags.SSL) == 0)
      {
        if (Settings.SslMode != MySqlSslMode.Disabled && Settings.SslMode != MySqlSslMode.Preferred)
          throw new MySqlException(string.Format(Resources.SslNotAllowedForConnectionProtocol, Settings.ConnectionProtocol));
      }
      // Server and connection supports SSL connections and Client are requisting a secure connection
      else
      {
        await stream.SendPacketAsync(packet).ConfigureAwait(false);
        var result = await new Ssl(Settings).StartSSLAsync(baseStream, Encoding, Settings.ToString(), cancellationToken).ConfigureAwait(false);
        stream = result.Item1;
        baseStream = result.Item2;
        packet.Clear();
        await packet.WriteIntegerAsync((int)connectionFlags, 4).ConfigureAwait(false);
        await packet.WriteIntegerAsync(maxSinglePacket, 4).ConfigureAwait(false);
        packet.WriteByte(33); //character set utf-8
        await packet.WriteAsync(new byte[23]).ConfigureAwait(false);
      }

      try
      {
        await AuthenticateAsync(authenticationMethod, false).ConfigureAwait(false);
      }
      catch (Exception)
      {
        // If the authenticationMethod is kerberos and KerberosAuthMode is on AUTO, it will retry the connection using GSSAPI mode
        if ((authenticationMethod == "authentication_kerberos_client" || authPlugin.SwitchedPlugin == "authentication_kerberos_client")
          && Settings.KerberosAuthMode == KerberosAuthMode.AUTO)
        {
          Settings.KerberosAuthMode = KerberosAuthMode.GSSAPI;
          await OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else
          throw;
      }

      // if we are using compression, then we use our CompressedStream class
      // to hide the ugliness of managing the compression
      if ((connectionFlags & ClientFlags.COMPRESS) != 0)
        stream = new MySqlStream(baseStream, Encoding, true, networkStream?.Socket);

      // give our stream the server version we are connected to.  
      // We may have some fields that are read differently based 
      // on the version of the server we are connected to.
      packet.Version = version;
      stream.MaxBlockSize = maxSinglePacket;
    }

    #region Authentication

    /// <summary>
    /// Return the appropriate set of connection flags for our
    /// server capabilities and our user requested options.
    /// </summary>
    private void SetConnectionFlags(ClientFlags serverCaps)
    {
      // We always allow multiple result sets
      ClientFlags flags = ClientFlags.MULTI_RESULTS;

      // allow load data local infile
      if (Settings.AllowLoadLocalInfile || !String.IsNullOrWhiteSpace(Settings.AllowLoadLocalInfileInPath))
        flags |= ClientFlags.LOCAL_FILES;

      if (!Settings.UseAffectedRows)
        flags |= ClientFlags.FOUND_ROWS;

      flags |= ClientFlags.PROTOCOL_41;
      // Need this to get server status values
      flags |= ClientFlags.TRANSACTIONS;

      // user allows/disallows batch statements
      if (Settings.AllowBatch)
        flags |= ClientFlags.MULTI_STATEMENTS;

      // if the server allows it, tell it that we want long column info
      if ((serverCaps & ClientFlags.LONG_FLAG) != 0)
        flags |= ClientFlags.LONG_FLAG;

      // if the server supports it and it was requested, then turn on compression
      if ((serverCaps & ClientFlags.COMPRESS) != 0 && Settings.UseCompression)
        flags |= ClientFlags.COMPRESS;

      flags |= ClientFlags.LONG_PASSWORD; // for long passwords

      // did the user request an interactive session?
      if (Settings.InteractiveSession)
        flags |= ClientFlags.INTERACTIVE;

      // if the server allows it and a database was specified, then indicate
      // that we will connect with a database name
      if ((serverCaps & ClientFlags.CONNECT_WITH_DB) != 0 &&
          Settings.Database != null && Settings.Database.Length > 0)
        flags |= ClientFlags.CONNECT_WITH_DB;

      // if the server is requesting a secure connection, then we oblige
      if ((serverCaps & ClientFlags.SECURE_CONNECTION) != 0)
        flags |= ClientFlags.SECURE_CONNECTION;

      // if the server is capable of SSL and the user is requesting SSL
      if ((serverCaps & ClientFlags.SSL) != 0 && Settings.SslMode != MySqlSslMode.Disabled
        && Settings.ConnectionProtocol != MySqlConnectionProtocol.NamedPipe
        && Settings.ConnectionProtocol != MySqlConnectionProtocol.SharedMemory)
        flags |= ClientFlags.SSL;

      // if the server supports output parameters, then we do too
      if ((serverCaps & ClientFlags.PS_MULTI_RESULTS) != 0)
        flags |= ClientFlags.PS_MULTI_RESULTS;

      if ((serverCaps & ClientFlags.PLUGIN_AUTH) != 0)
        flags |= ClientFlags.PLUGIN_AUTH;

      // if the server supports connection attributes
      if ((serverCaps & ClientFlags.CONNECT_ATTRS) != 0)
        flags |= ClientFlags.CONNECT_ATTRS;

      if ((serverCaps & ClientFlags.CAN_HANDLE_EXPIRED_PASSWORD) != 0)
        flags |= ClientFlags.CAN_HANDLE_EXPIRED_PASSWORD;

      // if the server supports query attributes
      if ((serverCaps & ClientFlags.CLIENT_QUERY_ATTRIBUTES) != 0)
        flags |= ClientFlags.CLIENT_QUERY_ATTRIBUTES;

      // if the server supports MFA
      if ((serverCaps & ClientFlags.MULTI_FACTOR_AUTHENTICATION) != 0)
        flags |= ClientFlags.MULTI_FACTOR_AUTHENTICATION;

      // need this to get server session trackers
      flags |= ClientFlags.CLIENT_SESSION_TRACK;

      connectionFlags = flags;
    }

    /// <summary>
    /// Performs authentication using the specified plugin method, handling integrated security if applicable.
    /// </summary>
    /// <param name="authMethod">The authentication plugin method.</param>
    /// <param name="reset">Whether this is a reset authentication (e.g., CHANGE_USER).</param>
    public void Authenticate(string authMethod, bool reset)
    {
      if (authMethod != null)
      {
        // Integrated security is a shortcut for windows auth
        if (Settings.IntegratedSecurity)
          authMethod = "authentication_windows_client";

        authPlugin = MySqlAuthenticationPlugin.GetPlugin(authMethod, this, encryptionSeed);
      }
      authPlugin.Authenticate(reset);
    }

    /// <summary>
    /// Asynchronously performs authentication using the specified plugin method, handling integrated security if applicable.
    /// </summary>
    /// <param name="authMethod">The authentication plugin method.</param>
    /// <param name="reset">Whether this is a reset authentication (e.g., CHANGE_USER).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AuthenticateAsync(string authMethod, bool reset)
    {
      if (authMethod != null)
      {
        // Integrated security is a shortcut for windows auth
        if (Settings.IntegratedSecurity)
          authMethod = "authentication_windows_client";

        authPlugin = await MySqlAuthenticationPlugin.GetPluginAsync(authMethod, this, encryptionSeed).ConfigureAwait(false);
      }
      await authPlugin.AuthenticateAsync(reset).ConfigureAwait(false);
    }

    #endregion

    /// <summary>
    /// Performs the common setup for resetting the connection, including clearing warnings, setting encoding, resetting sequence byte, and preparing the CHANGE_USER packet.
    /// </summary>
    private void PrepareReset()
    {
      warnings = 0;
      stream.Encoding = this.Encoding;
      stream.SequenceByte = 0;
      packet.Clear();
      packet.WriteByte((byte)DBCmd.CHANGE_USER);
    }

    /// <summary>
    /// Resets the connection by sending a CHANGE_USER command and re-authenticating.
    /// </summary>
    public void Reset()
    {
      PrepareReset();
      Authenticate(null, true);
    }

    /// <summary>
    /// Asynchronously resets the connection by sending a CHANGE_USER command and re-authenticating.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ResetAsync()
    {
      PrepareReset();
      await AuthenticateAsync(null, true).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a query command (COM_QUERY) to the server with the given packet.
    /// </summary>
    /// <param name="queryPacket">The packet containing the query.</param>
    /// <param name="paramsPosition">Position of parameters (unused in native).</param>
    public void SendQuery(MySqlPacket queryPacket, int paramsPosition)
    {
      warnings = 0;
      queryPacket.SetByte(4, (byte)DBCmd.QUERY);
      ExecutePacket(queryPacket);
      // the server will respond in one of several ways with the first byte indicating
      // the type of response.
      // 0 == ok packet.  This indicates non-select queries
      // 0xff == error packet.  This is handled in stream.OpenPacket
      // > 0 = number of columns in select query
      // We don't actually read the result here since a single query can generate
      // multiple resultsets and we don't want to duplicate code.  See ReadResult
      // Instead we set our internal server status flag to indicate that we have a query waiting.
      // This flag will be maintained by ReadResult
      serverStatus |= ServerStatusFlags.AnotherQuery;
    }

    /// <summary>
    /// Asynchronously sends a query command (COM_QUERY) to the server with the given packet.
    /// </summary>
    /// <param name="queryPacket">The packet containing the query.</param>
    /// <param name="paramsPosition">Position of parameters (unused in native).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendQueryAsync(MySqlPacket queryPacket, int paramsPosition)
    {
      warnings = 0;
      queryPacket.SetByte(4, (byte)DBCmd.QUERY);
      await ExecutePacketAsync(queryPacket).ConfigureAwait(false);
      // the server will respond in one of several ways with the first byte indicating
      // the type of response.
      // 0 == ok packet.  This indicates non-select queries
      // 0xff == error packet.  This is handled in stream.OpenPacket
      // > 0 = number of columns in select query
      // We don't actually read the result here since a single query can generate
      // multiple resultsets and we don't want to duplicate code.  See ReadResult
      // Instead we set our internal server status flag to indicate that we have a query waiting.
      // This flag will be maintained by ReadResult
      serverStatus |= ServerStatusFlags.AnotherQuery;
    }

    /// <summary>
    /// Closes the connection, optionally sending QUIT command if open.
    /// </summary>
    /// <param name="isOpen">Whether the connection was open (send QUIT).</param>
    public void Close(bool isOpen)
    {
      try
      {
        if (isOpen)
        {
          try
          {
            packet.Clear();
            packet.WriteByte((byte)DBCmd.QUIT);
            ExecutePacket(packet);
          }
          catch (Exception ex)
          {
            MySqlTrace.LogError(ThreadId, ex.ToString());
            // Eat exception here. We should try to closing 
            // the stream anyway.
          }
        }

        if (stream != null)
          stream.Close();
        stream = null;
      }
      catch (Exception)
      {
        // we are just going to eat any exceptions
        // generated here
      }
    }

    /// <summary>
    /// Asynchronously closes the connection, optionally sending QUIT command if open.
    /// </summary>
    /// <param name="isOpen">Whether the connection was open (send QUIT).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CloseAsync(bool isOpen)
    {
      try
      {
        if (isOpen)
        {
          try
          {
            packet.Clear();
            packet.WriteByte((byte)DBCmd.QUIT);
            await ExecutePacketAsync(packet).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            MySqlTrace.LogError(ThreadId, ex.ToString());
            // Eat exception here. We should try to closing 
            // the stream anyway.
          }
        }

        if (stream != null)
          await stream.CloseAsync().ConfigureAwait(false);
        stream = null;
      }
      catch (Exception)
      {
        // we are just going to eat any exceptions
        // generated here
      }
    }

    /// <summary>
    /// Pings the server to check if the connection is alive.
    /// </summary>
    /// <returns>true if server responds OK; otherwise, false (closes on failure).</returns>
    public bool Ping()
    {
      try
      {
        packet.Clear();
        packet.WriteByte((byte)DBCmd.PING);
        ExecutePacket(packet);
        ReadOk(true);
        return true;
      }
      catch (Exception)
      {
        owner.Close();
        return false;
      }
    }

    /// <summary>
    /// Asynchronously pings the server to check if the connection is alive.
    /// </summary>
    /// <returns>A task representing the operation, true if server responds OK; otherwise, false (closes on failure).</returns>
    public async Task<bool> PingAsync()
    {
      try
      {
        packet.Clear();
        packet.WriteByte((byte)DBCmd.PING);
        await ExecutePacketAsync(packet).ConfigureAwait(false);
        await ReadOkAsync(true).ConfigureAwait(false);
        return true;
      }
      catch (Exception)
      {
        await owner.CloseAsync().ConfigureAwait(false);
        return false;
      }
    }

    /// <summary>
    /// Reads the result packet after a query, handling LOAD DATA LOCAL INFILE, OK packets, field counts, affected rows, and insert IDs.
    /// </summary>
    /// <param name="affectedRow">Current affected row count (updated).</param>
    /// <param name="insertedId">Current last insert ID (updated).</param>
    /// <returns>A tuple of (fieldCount, affectedRow, insertedId).</returns>
    /// <exception cref="MySqlException">Thrown on timeouts or other errors.</exception>
    public Tuple<int, int, long> GetResult(int affectedRow, long insertedId)
    {
      try
      {
        if (stream.Socket == null && networkStream?.Socket != null)
        {
          stream.Socket = networkStream.Socket;
        }
        packet = stream.ReadPacket();
      }
      catch (TimeoutException)
      {
        // Do not reset serverStatus, allow to reenter, e.g when
        // ResultSet is closed.
        throw;
      }
      catch (Exception)
      {
        serverStatus &= ~(ServerStatusFlags.AnotherQuery |
                          ServerStatusFlags.MoreResults);
        throw;
      }

      int fieldCount = (int)packet.ReadFieldLength();
      if (-1 == fieldCount)
      {
        if (Settings.AllowLoadLocalInfile || !string.IsNullOrWhiteSpace(Settings.AllowLoadLocalInfileInPath))
        {
          string filename = packet.ReadString();

          if (!Settings.AllowLoadLocalInfile)
            ValidateLocalInfileSafePath(filename);

          SendFileToServer(filename);

          return GetResult(affectedRow, insertedId);
        }
        else
        {
          stream.Close();

          if (Settings.AllowLoadLocalInfile)
            throw new MySqlException(Resources.LocalInfileDisabled, (int)MySqlErrorCode.LoadInfo);
          throw new MySqlException(Resources.InvalidPathForLoadLocalInfile, (int)MySqlErrorCode.LoadInfo);
        }
      }
      else if (fieldCount == 0)
      {
        // the code to read last packet will set these server status vars 
        // again if necessary.
        serverStatus &= ~(ServerStatusFlags.AnotherQuery |
                          ServerStatusFlags.MoreResults);

        OkPacket okPacket = OkPacket.Create(packet);
        affectedRow = (int)okPacket.AffectedRows;
        insertedId = okPacket.LastInsertId;
        serverStatus = okPacket.ServerStatusFlags;
        warnings += okPacket.WarningCount;
      }

      return new Tuple<int, int, long>(fieldCount, affectedRow, insertedId);
    }

    /// <summary>
    /// Asynchronously reads the result packet after a query, handling LOAD DATA LOCAL INFILE, OK packets, field counts, affected rows, and insert IDs.
    /// </summary>
    /// <param name="affectedRow">Current affected row count (updated).</param>
    /// <param name="insertedId">Current last insert ID (updated).</param>
    /// <returns>A task returning a tuple of (fieldCount, affectedRow, insertedId).</returns>
    /// <exception cref="MySqlException">Thrown on timeouts or other errors.</exception>
    public async Task<Tuple<int, int, long>> GetResultAsync(int affectedRow, long insertedId)
    {
      try
      {
        if (stream.Socket == null && networkStream?.Socket != null)
        {
          stream.Socket = networkStream.Socket;
        }
        packet = await stream.ReadPacketAsync().ConfigureAwait(false);
      }
      catch (TimeoutException)
      {
        // Do not reset serverStatus, allow to reenter, e.g when
        // ResultSet is closed.
        throw;
      }
      catch (Exception)
      {
        serverStatus &= ~(ServerStatusFlags.AnotherQuery |
                          ServerStatusFlags.MoreResults);
        throw;
      }

      int fieldCount = (int)packet.ReadFieldLength();
      if (-1 == fieldCount)
      {
        if (Settings.AllowLoadLocalInfile || !string.IsNullOrWhiteSpace(Settings.AllowLoadLocalInfileInPath))
        {
          string filename = packet.ReadString();

          if (!Settings.AllowLoadLocalInfile)
            await ValidateLocalInfileSafePathAsync(filename).ConfigureAwait(false);

          await SendFileToServerAsync(filename).ConfigureAwait(false);

          return await GetResultAsync(affectedRow, insertedId).ConfigureAwait(false);
        }
        else
        {
          await stream.CloseAsync().ConfigureAwait(false);

          if (Settings.AllowLoadLocalInfile)
            throw new MySqlException(Resources.LocalInfileDisabled, (int)MySqlErrorCode.LoadInfo);
          throw new MySqlException(Resources.InvalidPathForLoadLocalInfile, (int)MySqlErrorCode.LoadInfo);
        }
      }
      else if (fieldCount == 0)
      {
        // the code to read last packet will set these server status vars 
        // again if necessary.
        serverStatus &= ~(ServerStatusFlags.AnotherQuery |
                          ServerStatusFlags.MoreResults);

        OkPacket okPacket = await OkPacket.CreateAsync(packet).ConfigureAwait(false);
        affectedRow = (int)okPacket.AffectedRows;
        insertedId = okPacket.LastInsertId;
        serverStatus = okPacket.ServerStatusFlags;
        warnings += okPacket.WarningCount;
      }

      return new Tuple<int, int, long>(fieldCount, affectedRow, insertedId);
    }

    /// <summary>
    /// Verify that the file to upload is in a valid directory
    /// according to the safe path entered by a user under
    /// "AllowLoadLocalInfileInPath" connection option.
    /// </summary>
    /// <param name="filePath">File to validate against the safe path.</param>
    /// <exception cref="MySqlException">Thrown if path is unsafe.</exception>
    private void ValidateLocalInfileSafePath(string filePath)
    {
      if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(Settings.AllowLoadLocalInfileInPath)))
      {
        stream.Close();
        throw new MySqlException(Resources.UnsafePathForLoadLocalInfile, (int)MySqlErrorCode.LoadInfo);
      }
    }

    /// <summary>
    /// Verify that the file to upload is in a valid directory
    /// according to the safe path entered by a user under
    /// "AllowLoadLocalInfileInPath" connection option.
    /// </summary>
    /// <param name="filePath">File to validate against the safe path.</param>
    /// <returns>A task representing the validation operation.</returns>
    /// <exception cref="MySqlException">Thrown if path is unsafe.</exception>
    private async Task ValidateLocalInfileSafePathAsync(string filePath)
    {
      if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(Settings.AllowLoadLocalInfileInPath)))
      {
        await stream.CloseAsync().ConfigureAwait(false);
        throw new MySqlException(Resources.UnsafePathForLoadLocalInfile, (int)MySqlErrorCode.LoadInfo);
      }
    }

    /// <summary>
    /// Sends the specified file contents to the server for LOAD DATA LOCAL INFILE, in chunks.
    /// </summary>
    /// <param name="filename">Path to the file to send.</param>
    /// <exception cref="MySqlException">Thrown on file IO errors.</exception>
    private void SendFileToServer(string filename)
    {
      byte[] buffer = new byte[8196];

      long len = 0;
      try
      {
        using (Stream fs = owner.BulkLoaderStream ?? new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
          len = fs.Length;
          fs.Position = 0;

          while (len > 0)
          {
            int count = fs.Read(buffer, 4, (int)(len > 8192 ? 8192 : len));

            stream.SendEntirePacketDirectly(buffer, count);
            len -= count;
          }

          stream.SendEntirePacketDirectly(buffer, 0);
        }
      }
      catch (Exception ex)
      {
        stream.Close();
        throw new MySqlException("Error during LOAD DATA LOCAL INFILE", ex);
      }
    }

    /// <summary>
    /// Asynchronously sends the specified file contents to the server for LOAD DATA LOCAL INFILE, in chunks.
    /// </summary>
    /// <param name="filename">Path to the file to send.</param>
    /// <returns>A task representing the operation.</returns>
    /// <exception cref="MySqlException">Thrown on file IO errors.</exception>
    private async Task SendFileToServerAsync(string filename)
    {
      byte[] buffer = new byte[8196];

      long len = 0;
      try
      {
        using (Stream fs = owner.BulkLoaderStream ?? new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
          len = fs.Length;
          fs.Position = 0;

          while (len > 0)
          {
            int count = await fs.ReadAsync(buffer, 4, (int)(len > 8192 ? 8192 : len)).ConfigureAwait(false);
            await stream.SendEntirePacketDirectlyAsync(buffer, count).ConfigureAwait(false);
            len -= count;
          }

          await stream.SendEntirePacketDirectlyAsync(buffer, 0).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        await stream.CloseAsync().ConfigureAwait(false);
        throw new MySqlException("Error during LOAD DATA LOCAL INFILE", ex);
      }
    }

    /// <summary>
    /// Reads the null bitmap for binary protocol result rows.
    /// </summary>
    /// <param name="fieldCount">Number of fields to size the bitmap.</param>
    private void ReadNullMap(int fieldCount)
    {
      // if we are binary, then we need to load in our null bitmap
      nullMap = null;
      byte[] nullMapBytes = new byte[(fieldCount + 9) / 8];
      packet.ReadByte();
      packet.Read(nullMapBytes, 0, nullMapBytes.Length);
      nullMap = new BitArray(nullMapBytes);
    }

    /// <summary>
    /// Asynchronously reads the null bitmap for binary protocol result rows.
    /// </summary>
    /// <param name="fieldCount">Number of fields to size the bitmap.</param>
    /// <returns>A task representing the operation.</returns>
    private async Task ReadNullMapAsync(int fieldCount)
    {
      // if we are binary, then we need to load in our null bitmap
      nullMap = null;
      byte[] nullMapBytes = new byte[(fieldCount + 9) / 8];
      packet.ReadByte();
      await packet.ReadAsync(nullMapBytes, 0, nullMapBytes.Length).ConfigureAwait(false);
      nullMap = new BitArray(nullMapBytes);
    }

    /// <summary>
    /// Reads and parses a single column value from the packet, handling nulls, length, and type-specific reading.
    /// </summary>
    /// <param name="index">Column index for null map.</param>
    /// <param name="field">Field metadata.</param>
    /// <param name="valObject">Value object to read into.</param>
    /// <returns>The parsed IMySqlValue.</returns>
    public IMySqlValue ReadColumnValue(int index, MySqlField field, IMySqlValue valObject)
    {
      long length = -1;
      bool isNull;

      if (nullMap != null)
      {
        isNull = nullMap[index + 2];
        if (!MySqlField.GetIMySqlValue(field.Type).GetType().Equals(valObject.GetType()) && !field.IsUnsigned)
          length = packet.ReadFieldLength();
      }
      else
      {
        length = packet.ReadFieldLength();
        isNull = length == -1;
      }

      if (!isNull && (valObject.MySqlDbType is MySqlDbType.Guid && !Settings.OldGuids) &&
        (length > 0 && !guidRegex.IsMatch(Encoding.GetString(packet.Buffer, packet.Position, (int)length))))
      {
        field.Type = MySqlDbType.String;
        valObject = field.GetValueObject();
      }

      packet.Encoding = field.Encoding;
      packet.Version = version;
      var val = valObject.ReadValue(packet, length, isNull);

      if (val is MySqlDateTime d)
      {
        d.TimezoneOffset = field.driver.timeZoneOffset;
        return d;
      }

      return val;
    }

    /// <summary>
    /// Asynchronously reads and parses a single column value from the packet, handling nulls, length, and type-specific reading.
    /// </summary>
    /// <param name="index">Column index for null map.</param>
    /// <param name="field">Field metadata.</param>
    /// <param name="valObject">Value object to read into.</param>
    /// <returns>A task returning the parsed IMySqlValue.</returns>
    public async Task<IMySqlValue> ReadColumnValueAsync(int index, MySqlField field, IMySqlValue valObject)
    {
      long length = -1;
      bool isNull;

      if (nullMap != null)
      {
        isNull = nullMap[index + 2];
        if (!MySqlField.GetIMySqlValue(field.Type).GetType().Equals(valObject.GetType()) && !field.IsUnsigned)
          length = packet.ReadFieldLength();
      }
      else
      {
        length = packet.ReadFieldLength();
        isNull = length == -1;
      }

      if (!isNull && (valObject.MySqlDbType is MySqlDbType.Guid && !Settings.OldGuids) &&
        (length > 0 && !guidRegex.IsMatch(Encoding.GetString(packet.Buffer, packet.Position, (int)length))))
      {
        field.Type = MySqlDbType.String;
        valObject = field.GetValueObject();
      }

      packet.Encoding = field.Encoding;
      packet.Version = version;
      var val = await valObject.ReadValueAsync(packet, length, isNull).ConfigureAwait(false);

      if (val is MySqlDateTime d)
      {
        d.TimezoneOffset = field.driver.timeZoneOffset;
        return d;
      }

      return val;
    }

    public void SkipColumnValue(IMySqlValue valObject)
    {
      int length = -1;
      if (nullMap == null)
      {
        length = (int)packet.ReadFieldLength();
        if (length == -1) return;
      }
      if (length > -1)
        packet.Position += length;
      else
        valObject.SkipValue(packet);
    }

    /// <summary>
    /// Reads column metadata for all fields in the result set.
    /// </summary>
    /// <param name="columns">Array to populate with field data.</param>
    public void GetColumnsData(MySqlField[] columns)
    {
      for (int i = 0; i < columns.Length; i++)
        GetColumnData(columns[i]);
      ReadEOF();
    }

    public async Task GetColumnsDataAsync(MySqlField[] columns)
    {
      for (int i = 0; i < columns.Length; i++)
        await GetColumnDataAsync(columns[i]).ConfigureAwait(false);
      await ReadEOFAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Parses a single column metadata packet into the MySqlField.
    /// </summary>
    /// <param name="field">Field to populate.</param>
    private void GetColumnData(MySqlField field)
    {
      stream.Encoding = Encoding;
      packet = stream.ReadPacket();
      field.Encoding = Encoding;
      field.CatalogName = packet.ReadLenString();
      field.DatabaseName = packet.ReadLenString();
      field.TableName = packet.ReadLenString();
      field.RealTableName = packet.ReadLenString();
      field.ColumnName = packet.ReadLenString();
      field.OriginalColumnName = packet.ReadLenString();
      packet.ReadByte();
      field.CharacterSetIndex = packet.ReadInteger(2);
      field.ColumnLength = packet.ReadInteger(4);
      MySqlDbType type = (MySqlDbType)packet.ReadByte();
      ColumnFlags colFlags;
      if ((connectionFlags & ClientFlags.LONG_FLAG) != 0)
        colFlags = (ColumnFlags)packet.ReadInteger(2);
      else
        colFlags = (ColumnFlags)packet.ReadByte();
      field.Scale = (byte)packet.ReadByte();

      if (packet.HasMoreData)
      {
        packet.ReadInteger(2); // reserved
      }

      if (type == MySqlDbType.Decimal || type == MySqlDbType.NewDecimal)
      {
        field.Precision = ((colFlags & ColumnFlags.UNSIGNED) != 0) ? (byte)(field.ColumnLength) : (byte)(field.ColumnLength - 1);
        if (field.Scale != 0)
          field.Precision--;
      }

      field.SetTypeAndFlags(type, colFlags);
    }

    /// <summary>
    /// Asynchronously parses a single column metadata packet into the MySqlField.
    /// </summary>
    /// <param name="field">Field to populate.</param>
    /// <returns>A task representing the operation.</returns>
    private async Task GetColumnDataAsync(MySqlField field)
    {
      stream.Encoding = Encoding;
      packet = await stream.ReadPacketAsync().ConfigureAwait(false);
      field.Encoding = Encoding;
      field.CatalogName = await packet.ReadLenStringAsync().ConfigureAwait(false);
      field.DatabaseName = await packet.ReadLenStringAsync().ConfigureAwait(false);
      field.TableName = await packet.ReadLenStringAsync().ConfigureAwait(false);
      field.RealTableName = await packet.ReadLenStringAsync().ConfigureAwait(false);
      field.ColumnName = await packet.ReadLenStringAsync().ConfigureAwait(false);
      field.OriginalColumnName = await packet.ReadLenStringAsync().ConfigureAwait(false);
      packet.ReadByte();
      field.CharacterSetIndex = packet.ReadInteger(2);
      field.ColumnLength = packet.ReadInteger(4);
      MySqlDbType type = (MySqlDbType)packet.ReadByte();
      ColumnFlags colFlags;
      if ((connectionFlags & ClientFlags.LONG_FLAG) != 0)
        colFlags = (ColumnFlags)packet.ReadInteger(2);
      else
        colFlags = (ColumnFlags)packet.ReadByte();
      field.Scale = (byte)packet.ReadByte();

      if (packet.HasMoreData)
      {
        packet.ReadInteger(2); // reserved
      }

      if (type == MySqlDbType.Decimal || type == MySqlDbType.NewDecimal)
      {
        field.Precision = ((colFlags & ColumnFlags.UNSIGNED) != 0) ? (byte)(field.ColumnLength) : (byte)(field.ColumnLength - 1);
        if (field.Scale != 0)
          field.Precision--;
      }

      field.SetTypeAndFlags(type, colFlags);
    }

    /// <summary>
    /// Executes/sends a packet, resetting sequence and handling exceptions.
    /// </summary>
    /// <param name="packetToExecute">Packet to send.</param>
    private void ExecutePacket(MySqlPacket packetToExecute)
    {
      try
      {
        warnings = 0;
        stream.SequenceByte = 0;
        stream.SendPacket(packetToExecute);
      }
      catch (MySqlException ex)
      {
        HandleException(ex);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously executes/sends a packet, resetting sequence and handling exceptions.
    /// </summary>
    /// <param name="packetToExecute">Packet to send.</param>
    /// <returns>A task representing the operation.</returns>
    private async Task ExecutePacketAsync(MySqlPacket packetToExecute)
    {
      try
      {
        warnings = 0;
        stream.SequenceByte = 0;
        await stream.SendPacketAsync(packetToExecute).ConfigureAwait(false);
      }
      catch (MySqlException ex)
      {
        await HandleExceptionAsync(ex).ConfigureAwait(false);
        throw;
      }
    }

    /// <summary>
    /// Sends a prepared statement execution packet (COM_STMT_EXECUTE).
    /// </summary>
    /// <param name="packetToExecute">Execution packet.</param>
    public void ExecuteStatement(MySqlPacket packetToExecute)
    {
      warnings = 0;
      packetToExecute.SetByte(4, (byte)DBCmd.EXECUTE);
      ExecutePacket(packetToExecute);
      serverStatus |= ServerStatusFlags.AnotherQuery;
    }

    public async Task ExecuteStatementAsync(MySqlPacket packetToExecute)
    {
      warnings = 0;
      packetToExecute.SetByte(4, (byte)DBCmd.EXECUTE);
      await ExecutePacketAsync(packetToExecute).ConfigureAwait(false);
      serverStatus |= ServerStatusFlags.AnotherQuery;
    }

    private void CheckEOF()
    {
      if (!packet.IsLastPacket)
        throw new MySqlException("Expected end of data packet");

      packet.ReadByte(); // read off the 254

      if (packet.HasMoreData)
      {
        warnings += packet.ReadInteger(2);
        serverStatus = (ServerStatusFlags)packet.ReadInteger(2);

        // if we are at the end of this cursor based resultset, then we remove
        // the last row sent status flag so our next fetch doesn't abort early
        // and we remove this command result from our list of active CommandResult objects.
        //                if ((serverStatus & ServerStatusFlags.LastRowSent) != 0)
        //              {
        //                serverStatus &= ~ServerStatusFlags.LastRowSent;
        //              commandResults.Remove(lastCommandResult);
        //        }
      }
    }

    /// <summary>
    /// Reads the EOF packet from the stream after reading column definitions or row data.
    /// This method expects the next packet to be an EOF packet (header byte 0xFE) and validates it.
    /// Updates the warning count and server status if additional data is present.
    /// </summary>
    private void ReadEOF()
    {
      packet = stream.ReadPacket();
      CheckEOF();
    }

    /// <summary>
    /// Asynchronously reads the EOF packet from the stream after reading column definitions or row data.
    /// This method expects the next packet to be an EOF packet (header byte 0xFE) and validates it.
    /// Updates the warning count and server status if additional data is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ReadEOFAsync()
    {
      packet = await stream.ReadPacketAsync().ConfigureAwait(false);
      CheckEOF();
    }

    /// <summary>
    /// Prepares a SQL statement on the server using the COM_STMT_PREPARE command.
    /// Reads the response to extract the statement ID, number of parameters, and number of result columns.
    /// Skips column metadata if present and reads the trailing EOF packet.
    /// Parameter fields are populated with metadata from the server.
    /// </summary>
    /// <param name="sql">The SQL statement to prepare.</param>
    /// <returns>A tuple containing the prepared statement ID and an array of parameter fields (MySqlField[]), or null if no parameters.</returns>
    /// <exception cref="MySqlException">Thrown if the response marker is invalid or other protocol errors occur.</exception>
    public Tuple<int, MySqlField[]> PrepareStatement(string sql)
    {
      //TODO: check this
      //ClearFetchedRow();
      MySqlField[] parameters = null;
      packet.Length = sql.Length * 4 + 5;
      byte[] buffer = packet.Buffer;
      int len = Encoding.GetBytes(sql, 0, sql.Length, packet.Buffer, 5);
      packet.Position = len + 5;
      buffer[4] = (byte)DBCmd.PREPARE;
      ExecutePacket(packet);

      packet = stream.ReadPacket();

      int marker = packet.ReadByte();
      if (marker != 0)
        throw new MySqlException("Expected prepared statement marker");

      int statementId = packet.ReadInteger(4);
      int numCols = packet.ReadInteger(2);
      int numParams = packet.ReadInteger(2);
      //TODO: find out what this is needed for
      packet.ReadInteger(3);
      if (numParams > 0)
      {
        parameters = owner.GetColumns(numParams);
        // we set the encoding for each parameter back to our connection encoding
        // since we can't trust what is coming back from the server
        for (int i = 0; i < parameters.Length; i++)
          parameters[i].Encoding = Encoding;
      }

      if (numCols > 0)
      {
        while (numCols-- > 0)
        {
          packet = stream.ReadPacket();
          //TODO: handle streaming packets
        }

        ReadEOF();
      }

      return new Tuple<int, MySqlField[]>(statementId, parameters);
    }

    /// <summary>
    /// Asynchronously prepares a SQL statement on the server using the COM_STMT_PREPARE command.
    /// Reads the response to extract the statement ID, number of parameters, and number of result columns.
    /// Skips column metadata if present and reads the trailing EOF packet.
    /// Parameter fields are populated with metadata from the server.
    /// </summary>
    /// <param name="sql">The SQL statement to prepare.</param>
    /// <returns>A task that represents the asynchronous operation, containing a tuple of the prepared statement ID and an array of parameter fields (MySqlField[]), or null if no parameters.</returns>
    /// <exception cref="MySqlException">Thrown if the response marker is invalid or other protocol errors occur.</exception>
    public async Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql)
    {
      //TODO: check this
      //ClearFetchedRow();
      MySqlField[] parameters = null;
      packet.Length = sql.Length * 4 + 5;
      byte[] buffer = packet.Buffer;
      int len = Encoding.GetBytes(sql, 0, sql.Length, packet.Buffer, 5);
      packet.Position = len + 5;
      buffer[4] = (byte)DBCmd.PREPARE;
      await ExecutePacketAsync(packet).ConfigureAwait(false);

      packet = await stream.ReadPacketAsync().ConfigureAwait(false);

      int marker = packet.ReadByte();
      if (marker != 0)
        throw new MySqlException("Expected prepared statement marker");

      int statementId = packet.ReadInteger(4);
      int numCols = packet.ReadInteger(2);
      int numParams = packet.ReadInteger(2);
      //TODO: find out what this is needed for
      packet.ReadInteger(3);
      if (numParams > 0)
      {
        parameters = await owner.GetColumnsAsync(numParams).ConfigureAwait(false);
        // we set the encoding for each parameter back to our connection encoding
        // since we can't trust what is coming back from the server
        for (int i = 0; i < parameters.Length; i++)
          parameters[i].Encoding = Encoding;
      }

      if (numCols > 0)
      {
        while (numCols-- > 0)
        {
          packet = await stream.ReadPacketAsync().ConfigureAwait(false);
          //TODO: handle streaming packets
        }

        await ReadEOFAsync().ConfigureAwait(false);
      }

      return new Tuple<int, MySqlField[]>(statementId, parameters);
    }

    //		private void ClearFetchedRow() 
    //		{
    //			if (lastCommandResult == 0) return;

    //TODO
    /*			CommandResult result = (CommandResult)commandResults[lastCommandResult];
                result.ReadRemainingColumns();

                stream.OpenPacket();
                if (! stream.IsLastPacket)
                    throw new MySqlException("Cursor reading out of sync");

                ReadEOF(false);
                lastCommandResult = 0;*/
    //		}

    /// <summary>
    /// FetchDataRow is the method that the data reader calls to see if there is another 
    /// row to fetch.  In the non-prepared mode, it will simply read the next data packet.
    /// In the prepared mode (statementId > 0), it will 
    /// </summary>
    public bool FetchDataRow(int statementId, int columns)
    {
      /*			ClearFetchedRow();

                  if (!commandResults.ContainsKey(statementId)) return false;

                  if ( (serverStatus & ServerStatusFlags.LastRowSent) != 0)
                      return false;

                  stream.StartPacket(9, true);
                  stream.WriteByte((byte)DBCmd.FETCH);
                  stream.WriteInteger(statementId, 4);
                  stream.WriteInteger(1, 4);
                  stream.Flush();

                  lastCommandResult = statementId;
                      */
      packet = stream.ReadPacket();
      if (packet.IsLastPacket)
      {
        CheckEOF();
        return false;
      }
      nullMap = null;
      if (statementId > 0)
        ReadNullMap(columns);

      return true;
    }

    /// <summary>
    /// FetchDataRow is the method that the data reader calls to see if there is another 
    /// row to fetch.  In the non-prepared mode, it will simply read the next data packet.
    /// In the prepared mode (statementId > 0), it will 
    /// </summary>
    public async Task<bool> FetchDataRowAsync(int statementId, int columns)
    {
      /*			ClearFetchedRow();

                  if (!commandResults.ContainsKey(statementId)) return false;

                  if ( (serverStatus & ServerStatusFlags.LastRowSent) != 0)
                      return false;

                  stream.StartPacket(9, true);
                  stream.WriteByte((byte)DBCmd.FETCH);
                  stream.WriteInteger(statementId, 4);
                  stream.WriteInteger(1, 4);
                  stream.Flush();

                  lastCommandResult = statementId;
                      */
      packet = await stream.ReadPacketAsync().ConfigureAwait(false);
      if (packet.IsLastPacket)
      {
        CheckEOF();
        return false;
      }
      nullMap = null;
      if (statementId > 0)
        await ReadNullMapAsync(columns).ConfigureAwait(false);

      return true;
    }

    /// <summary>
    /// Closes a prepared statement on the server by sending the COM_STMT_CLOSE command.
    /// This deallocates the statement on the server side and invalidates the statement ID.
    /// </summary>
    /// <param name="statementId">The ID of the prepared statement to close.</param>
    public void CloseStatement(int statementId)
    {
      packet.Clear();
      packet.WriteByte((byte)DBCmd.CLOSE_STMT);
      packet.WriteInteger((long)statementId, 4);
      stream.SequenceByte = 0;
      stream.SendPacket(packet);
    }

    /// <summary>
    /// Asynchronously closes a prepared statement on the server by sending the COM_STMT_CLOSE command.
    /// This deallocates the statement on the server side and invalidates the statement ID.
    /// </summary>
    /// <param name="statementId">The ID of the prepared statement to close.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CloseStatementAsync(int statementId)
    {
      packet.Clear();
      packet.WriteByte((byte)DBCmd.CLOSE_STMT);
      await packet.WriteIntegerAsync((long)statementId, 4).ConfigureAwait(false);
      stream.SequenceByte = 0;
      await stream.SendPacketAsync(packet).ConfigureAwait(false);
    }

    /// <summary>
    /// Execution timeout, in milliseconds. When the accumulated time for network IO exceeds this value
    /// TimeoutException is thrown. This timeout needs to be reset for every new command
    /// </summary>
    /// 
    public void ResetTimeout(int timeout)
    {
      if (stream != null)
        stream.ResetTimeout(timeout);
    }

    /// <summary>
    /// Builds the connection attributes string by reflecting over MySqlConnectAttrs properties,
    /// formatting each attribute name and value as length-prefixed strings.
    /// </summary>
    /// <returns>The concatenated connection attributes string.</returns>
    private string BuildConnectAttrs()
    {
      string connectAttrs = string.Empty;
      MySqlConnectAttrs attrs = new MySqlConnectAttrs();
      foreach (PropertyInfo property in attrs.GetType().GetProperties())
      {
        string name = property.Name;
        object[] customAttrs = property.GetCustomAttributes(typeof(DisplayNameAttribute), false);

        if (customAttrs.Length > 0)
          name = (customAttrs[0] as DisplayNameAttribute).DisplayName;

        string value = (string)property.GetValue(attrs, null);
        connectAttrs += string.Format("{0}{1}", (char)name.Length, name);
        connectAttrs += string.Format("{0}{1}", (char)Encoding.UTF8.GetBytes(value).Length, value);
      }
      return connectAttrs;
    }

    /// <summary>
    /// Sets the connection attributes in the client handshake packet if the server supports CONNECT_ATTRS capability.
    /// Collects attributes from MySqlConnectAttrs instance, including client info like program name, PID, etc.,
    /// and writes them as length-prefixed strings to the packet.
    /// </summary>
    internal void SetConnectAttrs()
    {
      // Sets connect attributes
      if ((connectionFlags & ClientFlags.CONNECT_ATTRS) != 0)
      {
        string connectAttrs = BuildConnectAttrs();
        packet.WriteLenString(connectAttrs);
      }
    }

    /// <summary>
    /// Asynchronously sets the connection attributes in the client handshake packet if the server supports CONNECT_ATTRS capability.
    /// Collects attributes from MySqlConnectAttrs instance, including client info like program name, PID, etc.,
    /// and writes them as length-prefixed strings to the packet.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task SetConnectAttrsAsync()
    {
      // Sets connect attributes
      if ((connectionFlags & ClientFlags.CONNECT_ATTRS) != 0)
      {
        string connectAttrs = BuildConnectAttrs();
        await packet.WriteLenStringAsync(connectAttrs).ConfigureAwait(false);
      }
    }
  }
}
