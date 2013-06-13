// Copyright © 2004, 2011, 2013, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

//#define BOUNCY_CASTLE_INCLUDED

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MySql.Data.Common;
using MySql.Data.Types;
using System.Security.Cryptography.X509Certificates;
using MySql.Data.MySqlClient.Properties;
using System.Text;
using System.Reflection;
using System.ComponentModel;
#if !CF
using System.Net.Security;
using System.Security.Authentication;
using System.Globalization;
#endif
#if BOUNCY_CASTLE_INCLUDED
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for Driver.
  /// </summary>
  internal class NativeDriver : IDriver
  {
    private DBVersion version;
    private int threadId;
    protected String encryptionSeed;
    protected ServerStatusFlags serverStatus;
    protected MySqlStream stream;
    protected Stream baseStream;
    private BitArray nullMap;
    private MySqlPacket packet;
    private ClientFlags connectionFlags;
    private Driver owner;
    private int warnings;

    // Windows authentication method string, used by the protocol.
    // Also known as "client plugin name".
    const string AuthenticationWindowsPlugin = "authentication_windows_client";

    // Predefined username for IntegratedSecurity
    const string AuthenticationWindowsUser = "auth_windows";

    private string _authPluginMethod;

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

    private MySqlConnectionStringBuilder Settings
    {
      get { return owner.Settings; }
    }

    private Encoding Encoding
    {
      get { return owner.Encoding; }
    }

    private void HandleException(MySqlException ex)
    {
      if (ex.IsFatal)
        owner.Close();
    }

    private void ReadOk(bool read)
    {
      try
      {
        if (read)
          packet = stream.ReadPacket();
        byte marker = (byte)packet.ReadByte();
        if (marker != 0)
        {
          throw new MySqlException("Out of sync with server", true, null);
        }

        packet.ReadFieldLength(); /* affected rows */
        packet.ReadFieldLength(); /* last insert id */
        if (packet.HasMoreData)
        {
          serverStatus = (ServerStatusFlags)packet.ReadInteger(2);
          packet.ReadInteger(2);  /* warning count */
          if (packet.HasMoreData)
          {
            packet.ReadLenString();  /* message */
          }
        }
      }
      catch (MySqlException ex)
      {
        HandleException(ex);
        throw;
      }
    }

    /// <summary>
    /// Sets the current database for the this connection
    /// </summary>
    /// <param name="dbName"></param>
    public void SetDatabase(string dbName)
    {
      byte[] dbNameBytes = Encoding.GetBytes(dbName);

      packet.Clear();
      packet.WriteByte((byte)DBCmd.INIT_DB);
      packet.Write(dbNameBytes);
      ExecutePacket(packet);

      ReadOk(true);
    }

    public void Configure()
    {
      stream.MaxPacketSize = (ulong)owner.MaxPacketSize;
      stream.Encoding = Encoding;
    }

    public void Open()
    {
      // connect to one of our specified hosts
      try
      {
#if !CF
        if (Settings.ConnectionProtocol == MySqlConnectionProtocol.SharedMemory)
        {
          SharedMemoryStream str = new SharedMemoryStream(Settings.SharedMemoryName);
          str.Open(Settings.ConnectionTimeout);
          baseStream = str;
        }
        else
        {
#endif
          string pipeName = Settings.PipeName;
          if (Settings.ConnectionProtocol != MySqlConnectionProtocol.NamedPipe)
            pipeName = null;
          StreamCreator sc = new StreamCreator(Settings.Server, Settings.Port, pipeName,
              Settings.Keepalive, this.Version);
#if !CF
         if (Settings.IncludeSecurityAsserts)
            MySqlSecurityPermission.CreatePermissionSet(false).Assert();
#endif
          baseStream = sc.GetStream(Settings.ConnectionTimeout);
#if !CF
        }
#endif
      }
      catch (System.Security.SecurityException)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new MySqlException(Resources.UnableToConnectToHost,
            (int)MySqlErrorCode.UnableToConnectToHost, ex);
      }
      if (baseStream == null)
        throw new MySqlException(Resources.UnableToConnectToHost,
            (int)MySqlErrorCode.UnableToConnectToHost);

      int maxSinglePacket = 255 * 255 * 255;
      stream = new MySqlStream(baseStream, Encoding, false);

      stream.ResetTimeout((int)Settings.ConnectionTimeout * 1000);

      // read off the welcome packet and parse out it's values
      packet = stream.ReadPacket();
      int protocol = packet.ReadByte();
      string versionString = packet.ReadString();
      version = DBVersion.Parse(versionString);
      if (!version.isAtLeast(5, 0, 0))
        throw new NotSupportedException(Resources.ServerTooOld);
      threadId = packet.ReadInteger(4);
      encryptionSeed = packet.ReadString();

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
      string seedPart2 = packet.ReadString();
      encryptionSeed += seedPart2;

      string authenticationMethod = "";
      if ((serverCaps & ClientFlags.PLUGIN_AUTH) != 0)
      {
        _authPluginMethod = authenticationMethod = packet.ReadString();
      }

      // based on our settings, set our connection flags
      SetConnectionFlags(serverCaps);

      packet.Clear();
      packet.WriteInteger((int)connectionFlags, 4);

#if !CF
      if ((serverCaps & ClientFlags.SSL) == 0)
      {
        if ((Settings.SslMode != MySqlSslMode.None)
        && (Settings.SslMode != MySqlSslMode.Preferred))
        {
          // Client requires SSL connections.
          string message = String.Format(Resources.NoServerSSLSupport,
              Settings.Server);
          throw new MySqlException(message);
        }
      }
      else if (Settings.SslMode != MySqlSslMode.None)
      {
        stream.SendPacket(packet);
        StartSSL();
        packet.Clear();
        packet.WriteInteger((int)connectionFlags, 4);
      }
#endif

      packet.WriteInteger(maxSinglePacket, 4);
      packet.WriteByte(8);
      packet.Write(new byte[23]);

      Authenticate(false);

      // if we are using compression, then we use our CompressedStream class
      // to hide the ugliness of managing the compression
      if ((connectionFlags & ClientFlags.COMPRESS) != 0)
        stream = new MySqlStream(baseStream, Encoding, true);

      // give our stream the server version we are connected to.  
      // We may have some fields that are read differently based 
      // on the version of the server we are connected to.
      packet.Version = version;
      stream.MaxBlockSize = maxSinglePacket;
    }

#if !CF

    #region SSL

    /// <summary>
    /// Retrieve client SSL certificates. Dependent on connection string 
    /// settings we use either file or store based certificates.
    /// </summary>
    private X509CertificateCollection GetClientCertificates()
    {
      X509CertificateCollection certs = new X509CertificateCollection();

      // Check for file-based certificate
      if (Settings.CertificateFile != null)
      {
        if (!Version.isAtLeast(5, 1, 0))
          throw new MySqlException(Properties.Resources.FileBasedCertificateNotSupported);

        X509Certificate2 clientCert = new X509Certificate2(Settings.CertificateFile,
            Settings.CertificatePassword);
        certs.Add(clientCert);
        return certs;
      }

      if (Settings.CertificateStoreLocation == MySqlCertificateStoreLocation.None)
        return certs;

      StoreLocation location =
          (Settings.CertificateStoreLocation == MySqlCertificateStoreLocation.CurrentUser) ?
          StoreLocation.CurrentUser : StoreLocation.LocalMachine;

      // Check for store-based certificate
      X509Store store = new X509Store(StoreName.My, location);
      store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);


      if (Settings.CertificateThumbprint == null)
      {
        // Return all certificates from the store.
        certs.AddRange(store.Certificates);
        return certs;
      }

      // Find certificate with given thumbprint
      certs.AddRange(store.Certificates.Find(X509FindType.FindByThumbprint,
                Settings.CertificateThumbprint, true));

      if (certs.Count == 0)
      {
        throw new MySqlException("Certificate with Thumbprint " +
           Settings.CertificateThumbprint + " not found");
      }
      return certs;
    }


    private void StartSSL()
    {
      RemoteCertificateValidationCallback sslValidateCallback =
          new RemoteCertificateValidationCallback(ServerCheckValidation);
      SslStream ss = new SslStream(baseStream, true, sslValidateCallback, null);
      X509CertificateCollection certs = GetClientCertificates();
      ss.AuthenticateAsClient(Settings.Server, certs, SslProtocols.Default, false);
      baseStream = ss;
      stream = new MySqlStream(ss, Encoding, false);
      stream.SequenceByte = 2;

    }

    private bool ServerCheckValidation(object sender, X509Certificate certificate,
                                              X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.None)
        return true;

      if (Settings.SslMode == MySqlSslMode.Preferred ||
          Settings.SslMode == MySqlSslMode.Required)
      {
        //Tolerate all certificate errors.
        return true;
      }

      if (Settings.SslMode == MySqlSslMode.VerifyCA &&
          sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
      {
        // Tolerate name mismatch in certificate, if full validation is not requested.
        return true;
      }

      return false;
    }


    #endregion

#endif

    #region Authentication

    /// <summary>
    /// Return the appropriate set of connection flags for our
    /// server capabilities and our user requested options.
    /// </summary>
    private void SetConnectionFlags(ClientFlags serverCaps)
    {
      // allow load data local infile
      ClientFlags flags = ClientFlags.LOCAL_FILES;

      if (!Settings.UseAffectedRows)
        flags |= ClientFlags.FOUND_ROWS;

      flags |= ClientFlags.PROTOCOL_41;
      // Need this to get server status values
      flags |= ClientFlags.TRANSACTIONS;

      // user allows/disallows batch statements
      if (Settings.AllowBatch)
        flags |= ClientFlags.MULTI_STATEMENTS;

      // We always allow multiple result sets
      flags |= ClientFlags.MULTI_RESULTS;

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

#if !CF
      // if the server is capable of SSL and the user is requesting SSL
      if ((serverCaps & ClientFlags.SSL) != 0 && Settings.SslMode != MySqlSslMode.None)
        flags |= ClientFlags.SSL;
#endif

      // if the server supports output parameters, then we do too
      if ((serverCaps & ClientFlags.PS_MULTI_RESULTS) != 0)
        flags |= ClientFlags.PS_MULTI_RESULTS;
      
      if ((serverCaps & ClientFlags.PLUGIN_AUTH) != 0)
        flags |= ClientFlags.PLUGIN_AUTH;

      if ((serverCaps & ClientFlags.CONNECT_ATTRS) != 0)
        flags |= ClientFlags.CONNECT_ATTRS;

      if ((serverCaps & ClientFlags.CAN_HANDLE_EXPIRED_PASSWORD) != 0)
        flags |= ClientFlags.CAN_HANDLE_EXPIRED_PASSWORD;

      connectionFlags = flags;
    }


    private void AuthenticateSSPI()
    {

      string targetName = ""; // target name (required by Kerberos)

      // First packet sent by server should include target name (for
      // Kerberos) as UTF8 string. It might however be prepended by junk 
      // at the start of the string (0xfe"authentication_win_client"\0,
      // see Bug#57442), this junk will be ignored. Target name can also 
      // be an empty string if server is not running in a domain environment, 
      // in this case authentication will fallback to NTLM.

      // Note that 0xfe byte at the start could also indicate that windows
      // authentication is not supported by sérver, we throw an exception
      // if this happens.
      targetName = packet.ReadString(Encoding.UTF8);

      // Do SSPI authentication handshake
      SSPI sspi = new SSPI(targetName, stream.BaseStream, stream.SequenceByte, version);
      sspi.AuthenticateClient();

      // read ok packet.
      packet = stream.ReadPacket();
      ReadOk(false);
    }


    #region SHA256 implementation

    private void AuthenticateSha256( byte[] authData )
    {
      // Do SHA256 authentication
      byte[] passBytes = GetSha256Password( authData );
      packet.Clear();
      packet.Write(passBytes);
      stream.SendPacket(packet);
      packet = stream.ReadPacket();
      ReadOk(false);
    }
    
    private byte[] rawPubkey;

    public byte[] GetSha256Password( byte[] authData )
    {
#if !CF
      if (Settings.SslMode != MySqlSslMode.None)
      {
        // send as clear text, since the channel is already encrypted
        byte[] passBytes = this.Encoding.GetBytes(Settings.Password);
        byte[] buffer = new byte[passBytes.Length + 1];
        Array.Copy(passBytes, 0, buffer, 0, passBytes.Length);
        buffer[passBytes.Length] = 0;
        return buffer;
      }
      else
      {
#endif

#if BOUNCY_CASTLE_INCLUDED
        // send RSA encrypted, since the channel is not protected
        if (publicKey == null)
        {
          // we have no public key, ask for it
          packet.Clear();
          packet.WriteByte(0x01);
          stream.SendPacket(packet);
          packet = stream.ReadPacket();
          byte prefixByte = packet.Buffer[0];
          if (prefixByte != 1) throw new MySqlException("Server did not return public key");
          byte[] responseData = new byte[packet.Length - 1];
          Array.Copy(packet.Buffer, 1, responseData, 0, responseData.Length);
          publicKey = GenerateKeysFromPem(responseData);
        }
        // we have the key, we can proceed.
        byte[] bytes = GetRsaPassword(Settings.Password, authData );
        if (bytes != null && bytes.Length == 1 && bytes[0] == 0) return null;
        return bytes;
#else
        throw new NotImplementedException( "You can use sha256 plugin only in SSL connections in this implementation." );
#endif
#if !CF
      }
#endif
    }

#if BOUNCY_CASTLE_INCLUDED
    
    private RsaKeyParameters publicKey;

    private RsaKeyParameters GenerateKeysFromPem(byte[] rawData)
    {
      PemReader pem = new PemReader(new StreamReader(new MemoryStream(rawData)));
      RsaKeyParameters keyPair = (RsaKeyParameters)pem.ReadObject();
      return keyPair;
    }

    private byte[] GetRsaPassword(string password, byte[] seedBytes)
    {
      // Obfuscate the plain text password with the session scramble
      byte[] ofuscated = GetSha256Xor(packet.Encoding.GetBytes(password), seedBytes);
      // Encrypt the password and send it to the server
      byte[] result = RsaEncrypt(ofuscated, publicKey);
      return result;
    }

    private byte[] GetSha256Xor(byte[] src, byte[] pattern)
    {
      byte[] src2 = new byte[src.Length + 1];
      Array.Copy(src, 0, src2, 0, src.Length);
      src2[src.Length] = 0;
      byte[] result = new byte[src2.Length];
      for (int i = 0; i < src2.Length; i++)
      {
        result[i] = (byte)(src2[i] ^ (pattern[i % pattern.Length]));
      }
      return result;
    }

    private byte[] RsaEncrypt(byte[] data, AsymmetricKeyParameter key)
    {
      IBufferedCipher c = CipherUtilities.GetCipher("RSA/NONE/OAEPPadding");
      c.Init(true, key);
      byte[] result = c.DoFinal(data);
      return result;
    }
#endif
    #endregion


    /// <summary>
    /// Perform an authentication against a 4.1.1 server
    /// <param name="reset">
    /// True, if this function is called as part of CHANGE_USER request
    /// (connection reset)
    /// False, for first-time logon
    /// </param>
    /// </summary>
    private void AuthenticateNew(bool reset)
    {
      if ((connectionFlags & ClientFlags.SECURE_CONNECTION) == 0)
        AuthenticateOld();
      
      packet.Write(Crypt.Get411Password(Settings.Password, encryptionSeed));
      
      if ((Flags & ClientFlags.CONNECT_WITH_DB) != 0 || reset)
      {
        if (!String.IsNullOrEmpty(Settings.Database))
          packet.WriteString(Settings.Database);
      }

      if (reset)
      {
        packet.WriteInteger(8, 2); // Charset number
      }

      if ((Flags & ClientFlags.PLUGIN_AUTH) != 0)
        packet.WriteString(_authPluginMethod);
      
      SetConnectAttrs();
      stream.SendPacket(packet);
      
      packet = stream.ReadPacket();
      // An authentication request switch?
      if (packet.Buffer[0] == 0xfe)
      {
        if (packet.IsLastPacket)
        {
          // this result means the server wants us to send the password using
          // old encryption
          packet.Clear();
          packet.WriteString(Crypt.EncryptPassword(
                                 Settings.Password, encryptionSeed.Substring(0, 8), true));
          stream.SendPacket(packet);
          ReadOk(true);
        }
        else
        {
          // use the correct plugin
          packet.ReadByte();
          string method = packet.ReadString(); // packet.Encoding.GetString(packet.Buffer, 1, packet.Buffer.Length - 1);
          byte[] authData = new byte[packet.Length - packet.Position];
          Array.Copy(packet.Buffer, packet.Position, authData, 0, authData.Length);
          switch (method)
          {
            case "authentication_windows_client":
              // send auth data here.
              AuthenticateSSPI();
              return;
              break;
            case "sha256_password":
              AuthenticateSha256( authData );
              return;
              break;
            case "mysql_native_password":
              packet.Clear();
              byte[] passBytes = null;
              if( authData[ authData.Length - 1 ] == 0 )
                passBytes = Crypt.Get411Password(Settings.Password, packet.Encoding.GetString(authData, 0, authData.Length - 1) );
              else
                passBytes = Crypt.Get411Password(Settings.Password, packet.Encoding.GetString(authData, 0, authData.Length) );
              
              byte[] buffer = new byte[passBytes.Length - 1];
              Array.Copy(passBytes, 1, buffer, 0, passBytes.Length - 1);
              packet.Write(buffer);
              stream.SendPacket(packet);
              ReadOk(true);
              break;
          }
        }
      }
      else
      {
        if (packet.IsLastPacket)
        {
          packet.Clear();
          packet.WriteString(Crypt.EncryptPassword(
                                 Settings.Password, encryptionSeed.Substring(0, 8), true));
          stream.SendPacket(packet);
          ReadOk(true);
        }
        else
          ReadOk(false);
      }
    }

    private void WriteNormalizedPassword(byte[] password)
    {
      if (password == null)
        packet.WriteByte(0);
      else 
        packet.Write(password );
    }

    private void AuthenticateOld()
    {
      packet.WriteString(Crypt.EncryptPassword(
                             Settings.Password, encryptionSeed, true));
      if ((connectionFlags & ClientFlags.CONNECT_WITH_DB) != 0 && Settings.Database != null)
        packet.WriteString(Settings.Database);

      stream.SendPacket(packet);
      ReadOk(true);
    }


    public void Authenticate(bool reset)
    {
      if (Settings.IntegratedSecurity)
      {
        if (String.IsNullOrEmpty(Settings.UserID))
          packet.WriteString(AuthenticationWindowsUser);
        else
          packet.WriteString(Settings.UserID);
      }
      else
      {
        // write the user id to the auth packet
        packet.WriteString(Settings.UserID);
      }
      AuthenticateNew(reset);
    }

    #endregion

    public void Reset()
    {
      warnings = 0;
      stream.Encoding = this.Encoding;
      stream.SequenceByte = 0;
      packet.Clear();
      packet.WriteByte((byte)DBCmd.CHANGE_USER);
      Authenticate(true);
    }

    /// <summary>
    /// Query is the method that is called to send all queries to the server
    /// </summary>
    public void SendQuery(MySqlPacket queryPacket)
    {
      warnings = 0;
      queryPacket.Buffer[4] = (byte)DBCmd.QUERY;
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
          catch (Exception)
          {
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

    public int GetResult(ref int affectedRow, ref long insertedId)
    {
      try
      {
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
        serverStatus = 0;
        throw;
      }

      int fieldCount = (int)packet.ReadFieldLength();
      if (-1 == fieldCount)
      {
        string filename = packet.ReadString();
        SendFileToServer(filename);

        return GetResult(ref affectedRow, ref insertedId);
      }
      else if (fieldCount == 0)
      {
        // the code to read last packet will set these server status vars 
        // again if necessary.
        serverStatus &= ~(ServerStatusFlags.AnotherQuery |
                          ServerStatusFlags.MoreResults);
        affectedRow = (int)packet.ReadFieldLength();
        insertedId = (long)packet.ReadFieldLength();

        serverStatus = (ServerStatusFlags)packet.ReadInteger(2);
        warnings += packet.ReadInteger(2);
        if (packet.HasMoreData)
        {
          packet.ReadLenString(); //TODO: server message
        }
      }
      return fieldCount;
    }

    /// <summary>
    /// Sends the specified file to the server. 
    /// This supports the LOAD DATA LOCAL INFILE
    /// </summary>
    /// <param name="filename"></param>
    private void SendFileToServer(string filename)
    {
      byte[] buffer = new byte[8196];

      long len = 0;
      try
      {
        using (FileStream fs = new FileStream(filename, FileMode.Open,
            FileAccess.Read))
        {
          len = fs.Length;
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
        throw new MySqlException("Error during LOAD DATA LOCAL INFILE", ex);
      }
    }

    private void ReadNullMap(int fieldCount)
    {
      // if we are binary, then we need to load in our null bitmap
      nullMap = null;
      byte[] nullMapBytes = new byte[(fieldCount + 9) / 8];
      packet.ReadByte();
      packet.Read(nullMapBytes, 0, nullMapBytes.Length);
      nullMap = new BitArray(nullMapBytes);
    }

    public IMySqlValue ReadColumnValue(int index, MySqlField field, IMySqlValue valObject)
    {
      long length = -1;
      bool isNull;

      if (nullMap != null)
        isNull = nullMap[index + 2];
      else
      {
        length = packet.ReadFieldLength();
        isNull = length == -1;
      }

      packet.Encoding = field.Encoding;
      packet.Version = version;
      return valObject.ReadValue(packet, length, isNull);
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

    public void GetColumnsData(MySqlField[] columns)
    {
      for (int i = 0; i < columns.Length; i++)
        GetColumnData(columns[i]);
      ReadEOF();
    }

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
        field.Precision = (byte)(field.ColumnLength - 2);
        if ((colFlags & ColumnFlags.UNSIGNED) != 0)
          field.Precision++;
      }

      field.SetTypeAndFlags(type, colFlags);
    }

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

    public void ExecuteStatement(MySqlPacket packetToExecute)
    {
      warnings = 0;
      packetToExecute.Buffer[4] = (byte)DBCmd.EXECUTE;
      ExecutePacket(packetToExecute);
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

    private void ReadEOF()
    {
      packet = stream.ReadPacket();
      CheckEOF();
    }

    public int PrepareStatement(string sql, ref MySqlField[] parameters)
    {
      //TODO: check this
      //ClearFetchedRow();

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

      return statementId;
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

    public void CloseStatement(int statementId)
    {
      packet.Clear();
      packet.WriteByte((byte)DBCmd.CLOSE_STMT);
      packet.WriteInteger((long)statementId, 4);
      stream.SequenceByte = 0;
      stream.SendPacket(packet);
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

    private void SetConnectAttrs()
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
          connectAttrs += string.Format("{0}{1}", (char)value.Length, value);
        }
        packet.WriteLenString(connectAttrs);
      }
    }
  }

}
