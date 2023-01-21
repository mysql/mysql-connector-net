// Copyright (c) 2004, 2023, Oracle and/or its affiliates.
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

    private async Task HandleExceptionAsync(MySqlException ex, bool execAsync)
    {
      if (ex.IsFatal)
        await owner.CloseAsync(execAsync).ConfigureAwait(false);
    }

    internal async Task SendPacketAsync(MySqlPacket p, bool execAsync)
    {
      await stream.SendPacketAsync(p, execAsync);
    }

    internal async Task SendEmptyPacketAsync(bool execAsync)
    {
      byte[] buffer = new byte[4];
      await stream.SendEntirePacketDirectlyAsync(buffer, 0, execAsync).ConfigureAwait(false);
    }

    internal async Task<MySqlPacket> ReadPacketAsync(bool execAsync)
    {
      return packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);
    }

    internal async Task<OkPacket> ReadOkAsync(bool read, bool execAsync)
    {
      try
      {
        if (read)
          packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);

        byte header = packet.ReadByte();
        if (header != 0)
        {
          throw new MySqlException("Out of sync with server", true, null);
        }

        OkPacket okPacket = await OkPacket.CreateAsync(packet, execAsync).ConfigureAwait(false);
        serverStatus = okPacket.ServerStatusFlags;

        return okPacket;
      }
      catch (MySqlException ex)
      {
        await HandleExceptionAsync(ex, execAsync).ConfigureAwait(false);
        throw;
      }
    }

    /// <summary>
    /// Sets the current database for the this connection
    /// </summary>
    /// <param name="dbName"></param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    public async Task SetDatabaseAsync(string dbName, bool execAsync)
    {
      byte[] dbNameBytes = Encoding.GetBytes(dbName);

      packet.Clear();
      packet.WriteByte((byte)DBCmd.INIT_DB);
      await packet.WriteAsync(dbNameBytes, execAsync).ConfigureAwait(false);
      await ExecutePacketAsync(packet, execAsync).ConfigureAwait(false);

      await ReadOkAsync(true, execAsync).ConfigureAwait(false);
    }

    public void Configure()
    {
      stream.MaxPacketSize = (ulong)owner.MaxPacketSize;
      stream.Encoding = Encoding;
    }

    public async Task OpenAsync(bool execAsync, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // connect to one of our specified hosts
      try
      {
        var result = await StreamCreator.GetStreamAsync(Settings, cancellationToken, execAsync).ConfigureAwait(false);

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
      packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);

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
      await packet.WriteIntegerAsync((int)connectionFlags, 4, execAsync).ConfigureAwait(false);
      await packet.WriteIntegerAsync(maxSinglePacket, 4, execAsync).ConfigureAwait(false);
      packet.WriteByte(33); //character set utf-8
      await packet.WriteAsync(new byte[23], execAsync).ConfigureAwait(false);

      // Server doesn't support SSL connections
      if ((serverCaps & ClientFlags.SSL) == 0)
      {
        if (Settings.SslMode != MySqlSslMode.Disabled && Settings.SslMode != MySqlSslMode.Prefered)
          throw new MySqlException(string.Format(Resources.NoServerSSLSupport, Settings.Server));
      }
      // Current connection doesn't support SSL connections
      else if ((connectionFlags & ClientFlags.SSL) == 0)
      {
        if (Settings.SslMode != MySqlSslMode.Disabled && Settings.SslMode != MySqlSslMode.Prefered)
          throw new MySqlException(string.Format(Resources.SslNotAllowedForConnectionProtocol, Settings.ConnectionProtocol));
      }
      // Server and connection supports SSL connections and Client are requisting a secure connection
      else
      {
        await stream.SendPacketAsync(packet, execAsync).ConfigureAwait(false);
        var result = await new Ssl(Settings).StartSSLAsync(baseStream, Encoding, Settings.ToString(), cancellationToken, execAsync).ConfigureAwait(false);
        stream = result.Item1;
        baseStream = result.Item2;
        packet.Clear();
        await packet.WriteIntegerAsync((int)connectionFlags, 4, execAsync).ConfigureAwait(false);
        await packet.WriteIntegerAsync(maxSinglePacket, 4, execAsync).ConfigureAwait(false);
        packet.WriteByte(33); //character set utf-8
        await packet.WriteAsync(new byte[23], execAsync).ConfigureAwait(false);
      }

      try
      {
        await AuthenticateAsync(authenticationMethod, false, execAsync).ConfigureAwait(false);
      }
      catch (Exception)
      {
        // If the authenticationMethod is kerberos and KerberosAuthMode is on AUTO, it will retry the connection using GSSAPI mode
        if ((authenticationMethod == "authentication_kerberos_client" || authPlugin.SwitchedPlugin == "authentication_kerberos_client")
          && Settings.KerberosAuthMode == KerberosAuthMode.AUTO)
        {
          Settings.KerberosAuthMode = KerberosAuthMode.GSSAPI;
          await OpenAsync(execAsync, cancellationToken).ConfigureAwait(false);
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

    public async Task AuthenticateAsync(string authMethod, bool reset, bool execAsync)
    {
      if (authMethod != null)
      {
        // Integrated security is a shortcut for windows auth
        if (Settings.IntegratedSecurity)
          authMethod = "authentication_windows_client";

        authPlugin = await MySqlAuthenticationPlugin.GetPluginAsync(authMethod, this, encryptionSeed, execAsync).ConfigureAwait(false);
      }
      await authPlugin.AuthenticateAsync(reset, execAsync);
    }

    #endregion

    public async Task ResetAsync(bool execAsync)
    {
      warnings = 0;
      stream.Encoding = this.Encoding;
      stream.SequenceByte = 0;
      packet.Clear();
      packet.WriteByte((byte)DBCmd.CHANGE_USER);
      await AuthenticateAsync(null, true, execAsync).ConfigureAwait(false);
    }

    /// <summary>
    /// Query is the method that is called to send all queries to the server
    /// </summary>
    public async Task SendQueryAsync(MySqlPacket queryPacket, bool execAsync, int paramsPosition)
    {
      warnings = 0;
      queryPacket.SetByte(4, (byte)DBCmd.QUERY);
      await ExecutePacketAsync(queryPacket, execAsync).ConfigureAwait(false);
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

    public async Task CloseAsync(bool isOpen, bool execAsync)
    {
      try
      {
        if (isOpen)
        {
          try
          {
            packet.Clear();
            packet.WriteByte((byte)DBCmd.QUIT);
            await ExecutePacketAsync(packet, execAsync).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            MySqlTrace.LogError(ThreadId, ex.ToString());
            // Eat exception here. We should try to closing 
            // the stream anyway.
          }
        }

        if (stream != null)
          await stream.CloseAsync(execAsync).ConfigureAwait(false);
        stream = null;
      }
      catch (Exception)
      {
        // we are just going to eat any exceptions
        // generated here
      }
    }

    public async Task<bool> PingAsync(bool execAsync)
    {
      try
      {
        packet.Clear();
        packet.WriteByte((byte)DBCmd.PING);
        await ExecutePacketAsync(packet, execAsync).ConfigureAwait(false);
        await ReadOkAsync(true, execAsync).ConfigureAwait(false);
        return true;
      }
      catch (Exception)
      {
        await owner.CloseAsync(execAsync).ConfigureAwait(false);
        return false;
      }
    }

    public async Task<Tuple<int, int, long>> GetResultAsync(int affectedRow, long insertedId, bool execAsync)
    {
      try
      {
        if (stream.Socket == null && networkStream?.Socket != null)
        {
          stream.Socket = networkStream.Socket;
        }
        packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);
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
            await ValidateLocalInfileSafePathAsync(filename, execAsync).ConfigureAwait(false);

          await SendFileToServerAsync(filename, execAsync).ConfigureAwait(false);

          return await GetResultAsync(affectedRow, insertedId, execAsync).ConfigureAwait(false);
        }
        else
        {
          await stream.CloseAsync(execAsync).ConfigureAwait(false);

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

        OkPacket okPacket = await OkPacket.CreateAsync(packet, execAsync).ConfigureAwait(false);
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
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    private async Task ValidateLocalInfileSafePathAsync(string filePath, bool execAsync)
    {
      if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(Settings.AllowLoadLocalInfileInPath)))
      {
        await stream.CloseAsync(execAsync).ConfigureAwait(false);
        throw new MySqlException(Resources.UnsafePathForLoadLocalInfile, (int)MySqlErrorCode.LoadInfo);
      }
    }

    /// <summary>
    /// Sends the specified file to the server. 
    /// This supports the LOAD DATA LOCAL INFILE
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    private async Task SendFileToServerAsync(string filename, bool execAsync)
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
            int count = execAsync
              ? await fs.ReadAsync(buffer, 4, (int)(len > 8192 ? 8192 : len)).ConfigureAwait(false)
              : fs.Read(buffer, 4, (int)(len > 8192 ? 8192 : len));

            await stream.SendEntirePacketDirectlyAsync(buffer, count, execAsync).ConfigureAwait(false);
            len -= count;
          }

          await stream.SendEntirePacketDirectlyAsync(buffer, 0, execAsync).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        await stream.CloseAsync(execAsync).ConfigureAwait(false);
        throw new MySqlException("Error during LOAD DATA LOCAL INFILE", ex);
      }
    }

    private async Task ReadNullMapAsync(int fieldCount, bool execAsync)
    {
      // if we are binary, then we need to load in our null bitmap
      nullMap = null;
      byte[] nullMapBytes = new byte[(fieldCount + 9) / 8];
      packet.ReadByte();
      await packet.ReadAsync(nullMapBytes, 0, nullMapBytes.Length, execAsync).ConfigureAwait(false);
      nullMap = new BitArray(nullMapBytes);
    }

    public async Task<IMySqlValue> ReadColumnValueAsync(int index, MySqlField field, IMySqlValue valObject, bool execAsync)
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
      var val = await valObject.ReadValueAsync(packet, length, isNull, execAsync).ConfigureAwait(false);

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

    public async Task GetColumnsDataAsync(MySqlField[] columns, bool execAsync)
    {
      for (int i = 0; i < columns.Length; i++)
        await GetColumnDataAsync(columns[i], execAsync).ConfigureAwait(false);
      await ReadEOFAsync(execAsync).ConfigureAwait(false);
    }

    private async Task GetColumnDataAsync(MySqlField field, bool execAsync)
    {
      stream.Encoding = Encoding;
      packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);
      field.Encoding = Encoding;
      field.CatalogName = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
      field.DatabaseName = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
      field.TableName = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
      field.RealTableName = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
      field.ColumnName = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
      field.OriginalColumnName = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
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

    private async Task ExecutePacketAsync(MySqlPacket packetToExecute, bool execAsync)
    {
      try
      {
        warnings = 0;
        stream.SequenceByte = 0;
        await stream.SendPacketAsync(packetToExecute, execAsync).ConfigureAwait(false);
      }
      catch (MySqlException ex)
      {
        await HandleExceptionAsync(ex, execAsync).ConfigureAwait(false);
        throw;
      }
    }

    public async Task ExecuteStatementAsync(MySqlPacket packetToExecute, bool execAsync)
    {
      warnings = 0;
      packetToExecute.SetByte(4, (byte)DBCmd.EXECUTE);
      await ExecutePacketAsync(packetToExecute, execAsync).ConfigureAwait(false);
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

    private async Task ReadEOFAsync(bool execAsync)
    {
      packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);
      CheckEOF();
    }

    public async Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql, bool execAsync)
    {
      //TODO: check this
      //ClearFetchedRow();
      MySqlField[] parameters = null;
      packet.Length = sql.Length * 4 + 5;
      byte[] buffer = packet.Buffer;
      int len = Encoding.GetBytes(sql, 0, sql.Length, packet.Buffer, 5);
      packet.Position = len + 5;
      buffer[4] = (byte)DBCmd.PREPARE;
      await ExecutePacketAsync(packet, execAsync).ConfigureAwait(false);

      packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);

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
        parameters = await owner.GetColumnsAsync(numParams, execAsync).ConfigureAwait(false);
        // we set the encoding for each parameter back to our connection encoding
        // since we can't trust what is coming back from the server
        for (int i = 0; i < parameters.Length; i++)
          parameters[i].Encoding = Encoding;
      }

      if (numCols > 0)
      {
        while (numCols-- > 0)
        {
          packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);
          //TODO: handle streaming packets
        }

        await ReadEOFAsync(execAsync).ConfigureAwait(false);
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
    public async Task<bool> FetchDataRowAsync(int statementId, int columns, bool execAsync)
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
      packet = await stream.ReadPacketAsync(execAsync).ConfigureAwait(false);
      if (packet.IsLastPacket)
      {
        CheckEOF();
        return false;
      }
      nullMap = null;
      if (statementId > 0)
        await ReadNullMapAsync(columns, execAsync).ConfigureAwait(false);

      return true;
    }

    public async Task CloseStatementAsync(int statementId, bool execAsync)
    {
      packet.Clear();
      packet.WriteByte((byte)DBCmd.CLOSE_STMT);
      await packet.WriteIntegerAsync((long)statementId, 4, execAsync).ConfigureAwait(false);
      stream.SequenceByte = 0;
      await stream.SendPacketAsync(packet, execAsync).ConfigureAwait(false);
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

    internal async Task SetConnectAttrsAsync(bool execAsync)
    {
      // Sets connect attributes
      if ((connectionFlags & ClientFlags.CONNECT_ATTRS) != 0)
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

        await packet.WriteLenStringAsync(connectAttrs, execAsync).ConfigureAwait(false);
      }
    }
  }
}
