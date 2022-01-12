// Copyright (c) 2015, 2022, Oracle and/or its affiliates.
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

using MySql.Data;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Authentication;
using MySqlX.Communication;
using MySqlX.Protocol;
using MySqlX.Protocol.X;
using MySqlX.Security;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MySqlX.Sessions
{

  /// <summary>
  /// Implementation class of InternalSession to manage connections using the Xprotocol type object.
  /// </summary>
  internal class XInternalSession : InternalSession
  {
    /// <summary>
    /// Defines the compression controller that will be passed on the <see cref="XPacketReaderWriter"/> instance when
    /// compression is enabled.
    /// </summary>
    private XCompressionController _readerCompressionController;

    /// <summary>
    /// Defines the compression controller that will be passed on the <see cref="XPacketReaderWriter"/> instance when
    /// compression is enabled.
    /// </summary>
    private XCompressionController _writerCompressionController;

    private XProtocol _protocol;
    private XPacketReaderWriter _packetReaderWriter;
    private bool _serverSupportsTls = false;
    private const string mysqlxNamespace = "mysqlx";
    internal bool _supportsPreparedStatements = true;
    private int _stmtId = 0;
    private List<int> _preparedStatements = new List<int>();
    internal bool? _sessionResetNoReauthentication = null;
    internal MyNetworkStream _myNetworkStream;

    public XInternalSession(MySqlXConnectionStringBuilder settings) : base(settings)
    {
    }

    protected override void Open()
    {
      bool isUnix = Settings.ConnectionProtocol == MySqlConnectionProtocol.Unix ||
        Settings.ConnectionProtocol == MySqlConnectionProtocol.UnixSocket;
      _stream = MyNetworkStream.CreateStream(
        Settings.Server == "127.0.0.1" || Settings.Server == "::1"
            ? "localhost"
            : Settings.Server,
        Settings.ConnectTimeout,
        Settings.Keepalive,
        Settings.Port,
        isUnix);
      _myNetworkStream = (MyNetworkStream)_stream;
      if (_stream == null)
        throw new MySqlException(ResourcesX.UnableToConnect);

      _packetReaderWriter = new XPacketReaderWriter(_stream, _myNetworkStream.Socket);
      _protocol = new XProtocol(_packetReaderWriter);

      Settings.CharacterSet = string.IsNullOrWhiteSpace(Settings.CharacterSet) ? "utf8mb4" : Settings.CharacterSet;

      var encoding = Encoding.GetEncoding(string.Compare(Settings.CharacterSet, "utf8mb4", true) == 0 ? "UTF-8" : Settings.CharacterSet);

      SetState(SessionState.Connecting, false);

      try
      {
        GetAndSetCapabilities();
      }
      catch (Exception)
      {
        throw;
      }

      // Validates use of TLS.
      if (Settings.SslMode != MySqlSslMode.Disabled)
      {
        if (_serverSupportsTls)
        {
          new Ssl(
              Settings.Server,
              Settings.SslMode,
              Settings.CertificateFile,
              Settings.CertificateStoreLocation,
              Settings.CertificatePassword,
              Settings.CertificateThumbprint,
              Settings.SslCa,
              Settings.SslCert,
              Settings.SslKey,
              Settings.TlsVersion)
              .StartSSL(ref _stream, encoding, Settings.ToString());

          if (_readerCompressionController != null && _readerCompressionController.IsCompressionEnabled)
          {
            _packetReaderWriter = new XPacketReaderWriter(_stream, _readerCompressionController, _writerCompressionController, _myNetworkStream.Socket);
          }
          else
          {
            _packetReaderWriter = new XPacketReaderWriter(_stream, _myNetworkStream.Socket);
          }

          _protocol.SetXPackets(_packetReaderWriter);
        }
        else
        {
          // Client requires SSL connections.
          string message = String.Format(Resources.NoServerSSLSupport,
              Settings.Server);
          throw new MySqlException(message);
        }
      }
      else if (_readerCompressionController != null && _readerCompressionController.IsCompressionEnabled)
      {
        _packetReaderWriter = new XPacketReaderWriter(_stream, _readerCompressionController, _writerCompressionController, _myNetworkStream.Socket);
        _protocol.SetXPackets(_packetReaderWriter);
      }

      Authenticate();

      SetState(SessionState.Open, false);
    }

    internal void Authenticate()
    {
      // Default authentication
      if (Settings.Auth == MySqlAuthenticationMode.Default)
      {
        if ((Settings.SslMode != MySqlSslMode.Disabled && _serverSupportsTls) || Settings.ConnectionProtocol == MySqlConnectionProtocol.Unix)
        {
          Settings.Auth = MySqlAuthenticationMode.PLAIN;
          AuthenticatePlain();
        }
        else
        {
          bool authenticated = false;
          // first try using MYSQL41
          Settings.Auth = MySqlAuthenticationMode.MYSQL41;
          try
          {
            AuthenticateMySQL41();
            authenticated = true;
          }
          catch (MySqlException ex)
          {
            // code 1045 Invalid user or password
            if (ex.Code != 1045)
              throw;
          }

          // second try using SHA256_MEMORY
          if (!authenticated)
          {
            try
            {
              Settings.Auth = MySqlAuthenticationMode.SHA256_MEMORY;
              AuthenticateSha256Memory();
              authenticated = true;
            }
            catch (MySqlException ex)
            {
              // code 1045 Invalid user or password
              if (ex.Code == 1045)
                throw new MySqlException(1045, "HY000", ResourcesX.AuthenticationFailed);
              else
                throw;
            }
          }
        }
      }
      // User defined authentication
      else
      {
        switch (Settings.Auth)
        {
          case MySqlAuthenticationMode.PLAIN:
            AuthenticatePlain();
            break;
          case MySqlAuthenticationMode.MYSQL41:
            AuthenticateMySQL41();
            break;
          case MySqlAuthenticationMode.EXTERNAL:
            AuthenticateExternal();
            break;
          case MySqlAuthenticationMode.SHA256_MEMORY:
            AuthenticateSha256Memory();
            break;
          default:
            throw new NotImplementedException(Settings.Auth.ToString());
        }
      }
    }

    private void GetAndSetCapabilities()
    {
      _protocol.GetServerCapabilities();
      var clientCapabilities = new Dictionary<string, object>();
      Mysqlx.Connection.Capability capability = null;

      // Validates TLS use.
      if (Settings.SslMode != MySqlSslMode.Disabled)
      {
        capability = _protocol.Capabilities.Capabilities_.FirstOrDefault(i => i.Name.ToLowerInvariant() == "tls");
        if (capability != null)
        {
          _serverSupportsTls = true;
          clientCapabilities.Add("tls", "1");
        }
      }

      // Set connection-attributes.
      if (Settings.ConnectionAttributes.ToLower() != "false")
        clientCapabilities.Add("session_connect_attrs", GetConnectionAttributes(Settings.ConnectionAttributes));

      // Set compression algorithm.
      if (Settings.Compression != CompressionType.Disabled)
      {
        capability = _protocol.Capabilities.Capabilities_.FirstOrDefault(i => i.Name.ToLowerInvariant() == XCompressionController.COMPRESSION_KEY);

        // Raise error if client expects compression but server doesn't support it.
        if (Settings.Compression == CompressionType.Required && capability == null)
        {
          throw new NotSupportedException(ResourcesX.CompressionNotSupportedByServer);
        }

        // Update capabilities with the compression algorithm negotiation if server supports compression.
        if (capability != null)
        {
          var algorithmsDictionary = capability.Value.Obj.Fld.ToDictionary(
            field => field.Key,
            field => field.Value.Array.Value.ToDictionary(value => value.Scalar.VString.Value.ToStringUtf8().ToLowerInvariant()).Keys.ToList());

          if (algorithmsDictionary.ContainsKey(XCompressionController.ALGORITHMS_SUBKEY))
          {
            var supportedCompressionAlgorithms = algorithmsDictionary[XCompressionController.ALGORITHMS_SUBKEY].ToList().ToArray();
            VerifyDefaultOrder(ref supportedCompressionAlgorithms);
            var userCompressionAlgorithms = NegotiateUserAgainstClientAlgorithms(Settings.CompressionAlgorithm);
            var compressionCapabilities = NegotiateCompression(supportedCompressionAlgorithms, userCompressionAlgorithms);
            if (compressionCapabilities != null)
            {
              clientCapabilities.Add(XCompressionController.COMPRESSION_KEY, compressionCapabilities);
              var compressionAlgorithm = compressionCapabilities.First().Value.ToString();
              _readerCompressionController = new XCompressionController((CompressionAlgorithms)Enum.Parse(typeof(CompressionAlgorithms), compressionAlgorithm), false);
              _writerCompressionController = new XCompressionController((CompressionAlgorithms)Enum.Parse(typeof(CompressionAlgorithms), compressionAlgorithm), true);
              _packetReaderWriter = new XPacketReaderWriter(_stream, _readerCompressionController, _writerCompressionController, _myNetworkStream.Socket);

            }
          }
        }
      }

      try
      {
        _protocol.SetCapabilities(clientCapabilities);
      }
      catch (MySqlException ex)
      {
        if (ex.Message == "Capability 'session_connect_attrs' doesn't exist")
          clientCapabilities.Remove("session_connect_attrs");
        _protocol.SetCapabilities(clientCapabilities);
      }
    }

    /// <summary>
    /// Reorder the list of algorithms retrieved from server to the preferred order
    /// </summary>
    private void VerifyDefaultOrder(ref string[] algorithms)
    {
      var clientSupportedAlgorithms = Enum.GetNames(typeof(CompressionAlgorithms));
      List<string> output = new List<string>();
      foreach (var item in clientSupportedAlgorithms)
      {
        if (algorithms.Contains(item))
        {
          output.Add(item);
        }
      }
      algorithms = output.ToArray();
    }

    /// <summary>
    /// Validate the algorithms given in the connection string are valid compared with enum CompressionAlgorithms
    /// </summary>
    public List<string> NegotiateUserAgainstClientAlgorithms(string inputString)
    {
      inputString = inputString.Contains("[") ? inputString.Replace("[", string.Empty) : inputString;
      inputString = inputString.Contains("]") ? inputString.Replace("]", string.Empty) : inputString;
      inputString.Trim();
      if (string.IsNullOrEmpty(inputString))
      {
        return Enum.GetNames(typeof(CompressionAlgorithms)).ToList();
      }
      var elements = inputString.ToLowerInvariant().Split(',');
      List<string> ret = new List<string>();
      for (var i = 0; i < elements.Length; i++)
      {
        switch (elements[i].ToLowerInvariant())
        {
          case "lz4":
          case "lz4_message":
            elements[i] = CompressionAlgorithms.lz4_message.ToString();
            break;
          case "zstd":
          case "zstd_stream":
            elements[i] = CompressionAlgorithms.zstd_stream.ToString();
            break;

          case "deflate":
          case "deflate_stream":
#if NETFRAMEWORK
            if (elements.Length == 1 && Settings.Compression == CompressionType.Required)
            {
              throw new NotSupportedException(string.Format(ResourcesX.CompressionForSpecificAlgorithmNotSupportedInNetFramework, elements[i]));
            }
#else
            elements[i] = CompressionAlgorithms.deflate_stream.ToString();
#endif
            break;

        }
        if (Enum.IsDefined(typeof(CompressionAlgorithms), elements[i]))
        {
          ret.Add(elements[i]);
        }
      }
      return ret;
    }

    /// <summary>
    /// Negotiates compression capabilities with the server.
    /// </summary>
    /// <param name="serverSupportedAlgorithms">An array containing the compression algorithms supported by the server.</param>
    /// <param name="clientAgainstUserAlgorithms">An array containing the compression algorithms given by user/client.</param>
    private Dictionary<string, object> NegotiateCompression(string[] serverSupportedAlgorithms, List<string> clientAgainstUserAlgorithms)
    {
      if (serverSupportedAlgorithms == null || serverSupportedAlgorithms.Length == 0)
      {
        if (Settings.Compression == CompressionType.Required && clientAgainstUserAlgorithms.Count > 0)
        {
          throw new NotSupportedException(ResourcesX.CompressionAlgorithmNegotiationFailed);
        }
        return null;
      }

      // If server and client don't have matching compression algorithms either log a warning message
      // or raise an exception based on the selected compression type.
      XCompressionController.LoadLibzstdLibrary(ref clientAgainstUserAlgorithms);
      if (!clientAgainstUserAlgorithms.Any(element => serverSupportedAlgorithms.Contains(element)))
      {
        if (Settings.Compression == CompressionType.Preferred)
        {
          MySqlTrace.LogWarning(-1, ResourcesX.CompressionAlgorithmNegotiationFailed);
          return null;
        }
        else if (Settings.Compression == CompressionType.Required)
        {
          throw new NotSupportedException(ResourcesX.CompressionAlgorithmNegotiationFailed);
        }
      }

      string negotiatedAlgorithm = null;
      for (int index = 0; index < clientAgainstUserAlgorithms.Count; index++)
      {
        if (!serverSupportedAlgorithms.Contains(clientAgainstUserAlgorithms[index]))
        {
          continue;
        }

        negotiatedAlgorithm = clientAgainstUserAlgorithms[index];
        break;
      }

      if (negotiatedAlgorithm == null)
      {
        return null;
      }

      // Create the compression capability object.
      var compressionCapabilities = new Dictionary<string, object>();
      compressionCapabilities.Add(XCompressionController.ALGORITHMS_SUBKEY, negotiatedAlgorithm);
      compressionCapabilities.Add(XCompressionController.SERVER_COMBINE_MIXED_MESSAGES_SUBKEY, XCompressionController.DEFAULT_SERVER_COMBINE_MIXED_MESSAGES_VALUE);

      // TODO: For future use.
      //compressionCapabilities.Add(XCompressionController.SERVER_MAX_COMBINE_MESSAGES_SUBKEY, XCompressionController.DEFAULT_SERVER_MAX_COMBINE_MESSAGES_VALUE);

      return compressionCapabilities;
    }

    private Dictionary<string, string> GetConnectionAttributes(string connectionAttrs)
    {
      Dictionary<string, string> attrs = new Dictionary<string, string>();

      if (connectionAttrs.StartsWith("[") && connectionAttrs.EndsWith("]"))
      {
        connectionAttrs = connectionAttrs.Substring(1, connectionAttrs.Length - 2);

        if (!string.IsNullOrWhiteSpace(connectionAttrs))
        {
          foreach (var pair in connectionAttrs.Split(','))
          {
            string[] keyValue = pair.Split('=');
            string key = keyValue[0].Trim();
            string value = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;

            if (key == string.Empty)
              throw new MySqlException(ResourcesX.EmptyKeyConnectionAttribute);

            if (key.StartsWith("_"))
              throw new MySqlException(ResourcesX.InvalidUserDefinedAttribute);

            try { attrs.Add(key, value); }
            catch (ArgumentException) { throw new MySqlException(string.Format(ResourcesX.DuplicateUserDefinedAttribute, key)); }
          }
        }
      }
      else if (connectionAttrs != "true")
        throw new MySqlException(ResourcesX.InvalidConnectionAttributes);

      MySqlConnectAttrs clientAttrs = new MySqlConnectAttrs();
      attrs.Add("_pid", clientAttrs.PID);
      attrs.Add("_platform", clientAttrs.Platform);
      attrs.Add("_os", clientAttrs.OSName);
      attrs.Add("_source_host", Settings.Server);
      attrs.Add("_client_name", clientAttrs.ClientName);
      attrs.Add("_client_version", clientAttrs.ClientVersion);
      attrs.Add("_client_license", clientAttrs.ClientLicence);
      attrs.Add("_framework", clientAttrs.Framework);

      return attrs;
    }

    private void AuthenticateMySQL41()
    {
      MySQL41AuthenticationPlugin plugin = new MySQL41AuthenticationPlugin(Settings);
      _protocol.SendAuthStart(plugin.AuthName, null, null);
      byte[] extraData = _protocol.ReadAuthContinue();
      _protocol.SendAuthContinue(plugin.Continue(extraData));
      _protocol.ReadAuthOk();
    }

    private void AuthenticatePlain()
    {
      PlainAuthenticationPlugin plugin = new PlainAuthenticationPlugin(Settings);
      _protocol.SendAuthStart(plugin.AuthName, plugin.GetAuthData(), null);
      _protocol.ReadAuthOk();
    }

    private void AuthenticateExternal()
    {
      ExternalAuthenticationPlugin plugin = new ExternalAuthenticationPlugin(Settings);
      _protocol.SendAuthStart(plugin.AuthName, Encoding.UTF8.GetBytes(""), null);
      _protocol.ReadAuthOk();
    }

    private void AuthenticateSha256Memory()
    {
      Sha256MemoryAuthenticationPlugin plugin = new Sha256MemoryAuthenticationPlugin();
      _protocol.SendAuthStart(plugin.PluginName, null, null);
      byte[] nonce = _protocol.ReadAuthContinue();

      string data = $"{Settings.Database}\0{Settings.UserID}\0";
      byte[] byteData = Encoding.UTF8.GetBytes(data);
      byte[] clientHash = plugin.GetClientHash(Settings.Password, nonce);
      byte[] authData = new byte[byteData.Length + clientHash.Length];
      byteData.CopyTo(authData, 0);
      clientHash.CopyTo(authData, byteData.Length);

      _protocol.SendAuthContinue(authData);
      _protocol.ReadAuthOk();
    }

    protected internal void SetState(SessionState newState, bool broadcast)
    {
      if (newState == SessionState && !broadcast)
        return;
      SessionState oldSessionState = SessionState;
      SessionState = newState;

      //TODO check if we need to send this event
      //if (broadcast)
      //OnStateChange(new StateChangeEventArgs(oldConnectionState, connectionState));
    }

    internal override ProtocolBase GetProtocol()
    {
      return _protocol;
    }

    public override void Close()
    {
      try
      {
        try
        {
          // Deallocate compression objects.
          _readerCompressionController?.Close();
          _writerCompressionController?.Close();

          // Deallocate all the remaining prepared statements for current session.
          foreach (int stmtId in _preparedStatements)
          {
            if (!_myNetworkStream.IsSocketClosed)
            {
              DeallocatePreparedStatement(stmtId);
            }
            _preparedStatements.Remove(stmtId);
          }
        }
        catch (Exception)
        {
          //TODO log exception
        }

        if (!_myNetworkStream.IsSocketClosed)
        {
          _protocol.SendSessionClose();
        }
      }
      finally
      {
        SessionState = SessionState.Closed;
        _stream.Dispose();
      }
    }

    public void CreateCollection(string schemaName, string collectionName)
    {
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_CREATE_COLLECTION,
        true,
        new KeyValuePair<string, object>("schema", schemaName),
        new KeyValuePair<string, object>("name", collectionName));
    }

    /// <summary>
    /// Prepare the dictionary of arguments required to create a MySQL message.
    /// </summary>
    /// <param name="schemaName">The name of the MySQL schema.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="options">This object hold the parameters required to create the collection.</param>
    /// <see cref="CreateCollectionOptions"/>
    /// <returns>Collection referente.</returns>
    public void CreateCollection(string schemaName, string collectionName, CreateCollectionOptions options)
    {
      var dictionary = new Dictionary<string, object>();
      if (!options.Equals(null))
      {
        if (!string.IsNullOrEmpty(options.Validation.Level.ToString()))
        {
          dictionary.Add("level", (string)options.Validation.Level.ToString().ToLowerInvariant());
        }

        if (!string.IsNullOrEmpty(options.Validation.Schema))
        {
          dictionary.Add("schema", new DbDoc(options.Validation.Schema));
        }
      }

      ExecuteCmdNonQueryOptions(XpluginStatementCommand.XPLUGIN_STMT_CREATE_COLLECTION,
        true,
        new KeyValuePair<string, object>("schema", schemaName),
        new KeyValuePair<string, object>("name", collectionName),
        new KeyValuePair<string, object>("reuse_existing", options.ReuseExisting),
        new KeyValuePair<string, object>("options", dictionary)
        );
    }

    /// <summary>
    /// Prepare the dictionary of arguments required to Modify a MySQL message.
    /// </summary>
    /// <param name="schemaName">The name of the MySQL schema.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="options">This object hold the parameters required to Modify the collection.</param>
    /// <see cref="ModifyCollectionOptions"/>
    public void ModifyCollection(string schemaName, string collectionName, ModifyCollectionOptions? options)
    {
      var dictionary = new Dictionary<string, object>();
      if (!options.Equals(null))
      {
        if (options.Value.Validation.Level != null)
        {
          dictionary.Add("level", options.Value.Validation.Level.ToString().ToLowerInvariant());
        }
        if (options.Value.Validation.Schema != null)
        {
          dictionary.Add("schema", new DbDoc(options.Value.Validation.Schema));
        }
      }
      ExecuteCmdNonQueryOptions(XpluginStatementCommand.XPLUGIN_STMT_MODIFY_COLLECTION,
        true,
        new KeyValuePair<string, object>("schema", schemaName),
        new KeyValuePair<string, object>("name", collectionName),
        new KeyValuePair<string, object>("options", dictionary));
    }

    public void DropCollection(string schemaName, string collectionName)
    {
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_DROP_COLLECTION,
        true,
        new KeyValuePair<string, object>("schema", schemaName),
        new KeyValuePair<string, object>("name", collectionName));
    }

    public Result CreateCollectionIndex(CreateCollectionIndexStatement statement)
    {
      List<KeyValuePair<string, object>> args = new List<KeyValuePair<string, object>>();
      args.Add(new KeyValuePair<string, object>("name", statement.createIndexParams.IndexName));
      args.Add(new KeyValuePair<string, object>("collection", statement.Target.Name));
      args.Add(new KeyValuePair<string, object>("schema", statement.Target.Schema.Name));
      args.Add(new KeyValuePair<string, object>("unique", false));

      if (statement.createIndexParams.Type != null)
        args.Add(new KeyValuePair<string, object>("type", statement.createIndexParams.Type));

      for (int i = 0; i < statement.createIndexParams.Fields.Count; i++)
      {
        var field = statement.createIndexParams.Fields[i];
        var dictionary = new Dictionary<string, object>();
        dictionary.Add("member", field.Field);
        if (field.Type != null)
          dictionary.Add("type", field.Type);

        if (field.Required == null)
          dictionary.Add("required", false);
        else
          dictionary.Add("required", (bool)field.Required);

        if (field.Options != null)
          dictionary.Add("options", (ulong)field.Options);

        if (field.Srid != null)
          dictionary.Add("srid", (ulong)field.Srid);

        if (field.Array != null)
          dictionary.Add("array", (bool)field.Array);

        args.Add(new KeyValuePair<string, object>("constraint", dictionary));
      }

      return ExecuteCreateCollectionIndex(XpluginStatementCommand.XPLUGIN_STMT_CREATE_COLLECTION_INDEX, false, args.ToArray());
    }

    public void DropCollectionIndex(string schemaName, string collectionName, string indexName)
    {
      List<KeyValuePair<string, object>> args = new List<KeyValuePair<string, object>>();
      args.Add(new KeyValuePair<string, object>("schema", schemaName));
      args.Add(new KeyValuePair<string, object>("collection", collectionName));
      args.Add(new KeyValuePair<string, object>("name", indexName));
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_DROP_COLLECTION_INDEX, false, args.ToArray());
    }

    public long TableCount(Schema schema, string name, string type)
    {
      try
      {
        string sql = String.Format("SELECT COUNT(*) FROM {0}.{1}",
          ExprUnparser.QuoteIdentifier(schema.Name), ExprUnparser.QuoteIdentifier(name));
        return (long)ExecuteQueryAsScalar(sql);
      }
      catch (MySqlException ex) when (ex.Code == 1146)
      {
        throw new MySqlException(string.Format(ResourcesX.CollectionTableDoesNotExist, type.ToString(), name, schema.Name), (int)ex.Code);
      }
    }

    public bool TableExists(Schema schema, string name)
    {
      string sql = String.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{0}' AND table_name = '{1}'",
        schema.Name, name);
      long count = (long)ExecuteQueryAsScalar(sql);
      return count != 0;
    }

    private Result ExecuteCmdNonQuery(string cmd, bool throwOnFail, params KeyValuePair<string, object>[] args)
    {
      _protocol.SendExecuteStatement(mysqlxNamespace, cmd, args);
      return new Result(this);
    }

    private Result ExecuteCmdNonQueryOptions(string cmd, bool throwOnFail, params KeyValuePair<string, object>[] args)
    {
      _protocol.SendExecuteStatementOptions(mysqlxNamespace, cmd, args);
      return new Result(this);
    }

    private Result ExecuteCreateCollectionIndex(string cmd, bool throwOnFail, params KeyValuePair<string, object>[] args)
    {
      _protocol.SendCreateCollectionIndexStatement(mysqlxNamespace, cmd, args);
      return new Result(this);
    }

    public List<T> GetObjectList<T>(Schema s, params string[] types) where T : DatabaseObject
    {
      for (int i = 0; i < types.Length; i++)
        types[i] = types[i].ToUpperInvariant();
      RowResult result = GetRowResult("list_objects", new KeyValuePair<string, object>("schema", s.Name));
      var rows = result.FetchAll();

      List<T> docs = new List<T>();
      foreach (var row in rows)
      {
        if (!types.Contains(row.GetString("type").ToUpperInvariant())) continue;

        List<object> parameters = new List<object>(new object[] { s, row.GetString("name") });
        if (row["name"] is Byte[])
        {
          Byte[] byteArray = row["name"] as Byte[];
          parameters[1] = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
        }

        switch (row.GetString("type").ToUpperInvariant())
        {
          case "TABLE":
            parameters.Add(false);
            break;
          case "VIEW":
            parameters.Add(true);
            break;
        }
        T t = (T)Activator.CreateInstance(typeof(T),
          BindingFlags.NonPublic | BindingFlags.Instance,
          null, parameters.ToArray(), null);
        docs.Add(t);
      }
      return docs;
    }

    public string GetObjectType(Schema s, string name)
    {
      RowResult result = GetRowResult("list_objects",
        new KeyValuePair<string, object>("schema", s.Name),
        new KeyValuePair<string, object>("pattern", name));
      var row = result.FetchOne();
      if (row == null)
        throw new MySqlException(string.Format(ResourcesX.NoObjectFound, name));
      System.Diagnostics.Debug.Assert(result.FetchOne() == null);
      return row.GetString("type");
    }

    public RowResult GetRowResult(string cmd, params KeyValuePair<string, object>[] args)
    {
      _protocol.SendExecuteStatement(mysqlxNamespace, cmd, args);
      return new RowResult(this);
    }

    public Result Insert(Collection collection, DbDoc[] json, List<string> newIds, bool upsert)
    {
      _protocol.SendInsert(collection.Schema.Name, false, collection.Name, json, null, upsert);
      return new Result(this);
    }

    public Result DeleteDocs(RemoveStatement rs)
    {
      _protocol.SendDelete(rs.Target.Schema.Name, rs.Target.Name, false, rs.FilterData);
      return new Result(this);
    }

    public Result DeleteRows(TableDeleteStatement statement)
    {
      _protocol.SendDelete(statement.Target.Schema.Name,
        statement.Target.Name, true,
        statement.FilterData);
      return new Result(this);
    }

    public Result ModifyDocs(ModifyStatement ms)
    {
      _protocol.SendUpdate(ms.Target.Schema.Name, ms.Target.Name, false, ms.FilterData, ms.Updates);
      return new Result(this);
    }

    public Result UpdateRows(TableUpdateStatement statement)
    {
      _protocol.SendUpdate(statement.Target.Schema.Name,
        statement.Target.Name, true,
        statement.FilterData,
        statement.updates);
      return new Result(this);
    }

    public DocResult FindDocs(FindStatement fs)
    {
      _protocol.SendFind(fs.Target.Schema.Name, fs.Target.Name, false, fs.FilterData, fs.findParams);
      DocResult result = new DocResult(this);
      return result;
    }

    public RowResult FindRows(TableSelectStatement ss)
    {
      _protocol.SendFind(ss.Target.Schema.Name, ss.Target.Name, true, ss.FilterData, ss.findParams);
      return new RowResult(this);
    }

    public Result InsertRows(TableInsertStatement statement)
    {
      _protocol.SendInsert(statement.Target.Schema.Name, true, statement.Target.Name, statement.values.ToArray(), statement.fields, false);
      return new Result(this);
    }

    protected Result ExpectOpen(Mysqlx.Expect.Open.Types.Condition.Types.Key condition, object value = null)
    {
      _protocol.SendExpectOpen(condition, value);
      return new Result(this);
    }

    public Result ExpectDocidGenerated()
    {
      return ExpectOpen(Mysqlx.Expect.Open.Types.Condition.Types.Key.ExpectDocidGenerated);
    }

    public void ResetSession()
    {
      if (_sessionResetNoReauthentication == null)
      {
        try
        {
          if (!_myNetworkStream.IsSocketClosed)
          {
            ExpectOpen(Mysqlx.Expect.Open.Types.Condition.Types.Key.ExpectFieldExist, "6.1");
          }
          _sessionResetNoReauthentication = true;
        }
        catch
        {
          _sessionResetNoReauthentication = false;
        }
      }

      if (!_myNetworkStream.IsSocketClosed)
      {
        _protocol.SendResetSession((bool)_sessionResetNoReauthentication);
        _protocol.ReadOk();
      }

    }

    public int PrepareStatement<TResult>(BaseStatement<TResult> statement)
      where TResult : BaseResult
    {
      int stmtId = Interlocked.Increment(ref _stmtId);
      switch (statement.GetType().Name)
      {
        case nameof(FindStatement):
          FindStatement fs = statement as FindStatement;
          Debug.Assert(fs != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.Find,
            fs.Target.Schema.Name,
            fs.Target.Name,
            false,
            fs.FilterData,
            fs.findParams);
          break;

        case nameof(TableSelectStatement):
          TableSelectStatement ss = statement as TableSelectStatement;
          Debug.Assert(ss != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.Find,
            ss.Target.Schema.Name,
            ss.Target.Name,
            true,
            ss.FilterData,
            ss.findParams);
          break;

        case nameof(ModifyStatement):
          ModifyStatement ms = statement as ModifyStatement;
          Debug.Assert(ms != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.Update,
            ms.Target.Schema.Name,
            ms.Target.Name,
            false,
            ms.FilterData,
            null,
            ms.Updates);
          break;

        case nameof(TableUpdateStatement):
          TableUpdateStatement us = statement as TableUpdateStatement;
          Debug.Assert(us != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.Update,
            us.Target.Schema.Name,
            us.Target.Name,
            true,
            us.FilterData,
            null,
            us.updates);
          break;

        case nameof(RemoveStatement):
          RemoveStatement rs = statement as RemoveStatement;
          Debug.Assert(rs != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.Delete,
            rs.Target.Schema.Name,
            rs.Target.Name,
            false,
            rs.FilterData,
            null);
          break;

        case nameof(TableDeleteStatement):
          TableDeleteStatement ds = statement as TableDeleteStatement;
          Debug.Assert(ds != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.Delete,
            ds.Target.Schema.Name,
            ds.Target.Name,
            true,
            ds.FilterData,
            null);
          break;

        case nameof(TableInsertStatement):
          TableInsertStatement insert = statement as TableInsertStatement;
          Debug.Assert(insert != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.Insert,
            insert.Target.Schema.Name,
            insert.Target.Name,
            true,
            null,
            null,
            null,
            insert.values.ToArray(),
            insert.fields,
            false);
          break;

        case nameof(SqlStatement):
          SqlStatement sqlStatement = statement as SqlStatement;
          Debug.Assert(sqlStatement != null);
          _protocol.SendPrepareStatement(
            (uint)stmtId,
            DataAccess.PreparedStatementType.SqlStatement,
            null,
            null,
            true,
            null,
            null,
            null,
            sqlStatement.parameters.ToArray(),
            null,
            false,
            sqlStatement.SQL);
          break;

        default:
          throw new NotSupportedException(statement.GetType().Name);
      }
      _preparedStatements.Add(stmtId);
      return stmtId;
    }

    public TResult ExecutePreparedStatement<TResult>(int stmtId, IEnumerable args)
      where TResult : BaseResult
    {
      _protocol.SendExecutePreparedStatement((uint)stmtId, args);
      BaseResult result = null;
      if (typeof(TResult) == typeof(DocResult))
        result = new DocResult(this);
      else if (typeof(TResult) == typeof(RowResult))
        result = new RowResult(this);
      else if (typeof(TResult) == typeof(SqlResult))
        result = new SqlResult(this);
      else if (typeof(TResult) == typeof(Result))
        result = new Result(this);
      else
        throw new ArgumentNullException(typeof(TResult).Name);

      return (TResult)result;
    }

    public void DeallocatePreparedStatement(int stmtId)
    {
      _protocol.SendDeallocatePreparedStatement((uint)stmtId);
      _preparedStatements.Remove(stmtId);
    }

    /// <summary>
    /// Gets the compression algorithm being used to compress or decompress data.
    /// </summary>
    /// <param name="fromReaderController">Flag to indicate if the compression algorithm should be
    /// retrieved from the reader or writer controller.</param>
    /// <returns>The name of the compression algorithm being used if any.
    /// <c>null</c> if no compression algorithm is being used.</returns>
    public string GetCompressionAlgorithm(bool fromReaderController)
    {
      if (fromReaderController && _readerCompressionController != null)
      {
        return _readerCompressionController.CompressionAlgorithm.ToString();
      }
      else if (!fromReaderController && _writerCompressionController != null)
      {
        return _writerCompressionController.CompressionAlgorithm.ToString();
      }

      return null;
    }
  }
}
