// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Common;
using MySql.Data.Failover;
using MySqlX.Sessions;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a base class for a Session.
  /// </summary>
  public abstract class BaseSession : IDisposable
  {
    private InternalSession _internalSession;
    private string _connectionString;
    private bool _isDefaultPort = true;
    private const uint X_PROTOCOL_DEFAULT_PORT = 33060;
    private const char CONNECTION_DATA_KEY_SEPARATOR = ';';
    private const char CONNECTION_DATA_VALUE_SEPARATOR = '=';
    private const string PORT_CONNECTION_OPTION_KEYWORD = "port";
    private const string SERVER_CONNECTION_OPTION_KEYWORD = "server";
    private const string CONNECT_TIMEOUT_CONNECTION_OPTION_KEYWORD = "connect-timeout";
    private const string CONNECTION_ATTRIBUTES_CONNECTION_OPTION_KEYWORD = "connection-attributes";
    private const string DNS_SRV_CONNECTION_OPTION_KEYWORD = "dns-srv";
    private const string DNS_SRV_URI_SCHEME = "mysqlx+srv";
    private const string MYSQLX_URI_SCHEME = "mysqlx";
    private const string SSH_URI_SCHEME = "mysqlx+ssh";
    internal QueueTaskScheduler _scheduler = new QueueTaskScheduler();
    protected readonly Client _client;

    internal InternalSession InternalSession
    {
      get
      {
        if (_internalSession == null)
          throw new MySqlException(ResourcesX.InvalidSession);
        return _internalSession;
      }
    }

    internal XInternalSession XSession
    {
      get { return InternalSession as XInternalSession; }
    }

    internal DateTime IdleSince { get; set; }

    #region Session status properties

    private DBVersion? _version = null;

    internal DBVersion Version => _version ?? (_version = XSession.GetServerVersion()).Value;

    private int? _threadId = null;
    internal int ThreadId => _threadId ?? (_threadId = XSession.GetThreadId()).Value;

    /// <summary>
    /// Flag to set if prepared statements are supported.
    /// </summary>
    internal bool SupportsPreparedStatements { get; set; } = true;

    #endregion

    /// <summary>
    /// Gets the connection settings for this session.
    /// </summary>
    public MySqlXConnectionStringBuilder Settings { get; private set; }

    /// <summary>
    /// Gets the currently active schema.
    /// </summary>
    public Schema Schema { get; protected set; }

    /// <summary>
    /// Gets the default schema provided when creating the session.
    /// </summary>
    public Schema DefaultSchema { get; private set; }

    /// <summary>
    /// Gets the connection uri representation of the connection options provided during the creation of the session.
    /// </summary>
    public String Uri
    {
      get
      {
        var builder = new StringBuilder(string.Format("mysqlx://{0}:{1}{2}?",
          Settings.Server,
          Settings.Port,
          string.IsNullOrEmpty(Settings.Database) ?
            string.Empty :
            "/" + Settings.Database));
        var firstItemAdded = false;
        var certificateFileAdded = false;
        foreach (var item in Settings.values)
        {
          // Skip connection options already included in the connection URI.
          if (item.Key == "server" || item.Key == "database" || item.Key == "port")
            continue;

          // Skip CertificateFile if it has already been included.
          if ((item.Key == "certificatefile" || item.Key == "sslca") && certificateFileAdded)
            continue;

          try
          {
            var value = Settings[item.Key];
            // Get the default value of the connection option.
            var option = MySqlConnectionStringBuilder.Options.Values.First(
                o => o.Keyword == item.Key ||
                (o.Synonyms != null && o.Synonyms.Contains(item.Key)));
            var defaultValue = option.DefaultValue;
            // If the default value has been changed then include it in the connection URI.
            if (value != null && (defaultValue == null || (value.ToString() != defaultValue.ToString())))
            {
              if (!firstItemAdded)
                firstItemAdded = true;
              else
                builder.Append("&");

              if (item.Key == "certificatefile" || item.Key == "sslca")
              {
                certificateFileAdded = true;
                builder.Append("sslca");
              }
              else
                builder.Append(item.Key);
              builder.Append("=");
              builder.Append(value is bool ? value.ToString().ToLower() : value.ToString());
            }
          }
          // Dismiss any not supported exceptions since they are expected.
          catch (NotSupportedException) { }
          catch (ArgumentException) { }
        }

        return builder.ToString();
      }
    }

    /// <summary>
    /// Initializes a new instance of the BaseSession class based on the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection used to create the session.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is <c>null</c>.</exception>
    /// <exception cref="UriFormatException">Unable to parse the <paramref name="connectionString"/> when 
    /// in URI format.</exception>
    /// <remarks>
    /// <para>When using Unix sockets the <c>protocol=unix</c> or <c>protocol=unixsocket</c> connection option is required.
    /// This will enable elements passed in the <c>server</c> connection option to be treated as Unix sockets. The user is also required
    /// to explicitly set <c>sslmode</c> to <c>none</c> since X Plugin does not support SSL when using Unix sockets. Note that
    /// <c>protocol=unix</c> and <c>protocol=unixsocket</c> are synonyms.</para>
    /// <para>&#160;</para>
    /// <para>Multiple hosts can be specified as part of the <paramref name="connectionString"/>,
    /// which enables client-side failover when trying to establish a connection.</para>
    /// <para>&#160;</para>
    /// <para>Connection URI examples:
    /// <para />- mysqlx://test:test@[192.1.10.10,localhost]
    /// <para />- mysqlx://test:test@[192.1.10.10,127.0.0.1]
    /// <para />- mysqlx://root:@[../tmp/mysqlx.sock,/tmp/mysqld.sock]?protocol=unix&#38;sslmode=none
    /// <para />- mysqlx://test:test@[192.1.10.10:33060,127.0.0.1:33060]
    /// <para />- mysqlx://test:test@[192.1.10.10,120.0.0.2:22000,[::1]:33060]/test?connectiontimeout=10
    /// <para />- mysqlx://test:test@[(address=server.example,priority=20),(address=127.0.0.1,priority=100)]
    /// <para />- mysqlx://test:test@[(address=server.example,priority=100),(address=127.0.0.1,priority=75),(address=192.0.10.56,priority=25)]
    /// </para>
    /// <para>&#160;</para>
    /// <para>Connection string examples:
    /// <para />- server=10.10.10.10,localhost;port=33060;uid=test;password=test;
    /// <para />- host=10.10.10.10,192.101.10.2,localhost;port=5202;uid=test;password=test;
    /// <para />- host=./tmp/mysqld.sock,/var/run/mysqldx.sock;port=5202;uid=root;protocol=unix;sslmode=none;
    /// <para />- server=(address=server.example,priority=20),(address=127.0.0.1,priority=100);port=33060;uid=test;password=test;
    /// <para />- server=(address=server.example,priority=100),(address=127.0.0.1,priority=75),(address=192.0.10.56,priority=25);port=33060;uid=test;password=test;
    /// </para>
    /// <para>&#160;</para>
    /// <para>Failover methods</para>
    /// <para>- Sequential: Connection attempts will be performed in a sequential order, that is, one after another until
    /// a connection is successful or all the elements from the list have been tried.
    /// </para>
    /// <para>- Priority based: If a priority is provided, the connection attemps will be performed in descending order, starting
    /// with the host with the highest priority. Priority must be a value between 0 and 100. Additionally, it is required to either 
    /// give a priority for every host or no priority to any host.
    /// </para>
    /// </remarks>
    internal BaseSession(string connectionString, Client client = null) : this()
    {
      if (string.IsNullOrWhiteSpace(connectionString))
        throw new ArgumentNullException("connectionString");

      _client = client;
      this._connectionString = ParseConnectionData(connectionString, client);

      // Multiple hosts were specified.
      if (FailoverManager.FailoverGroup != null && FailoverManager.FailoverGroup.Hosts?.Count > 1)
      {
        _internalSession = FailoverManager.AttemptConnectionXProtocol(this._connectionString, out this._connectionString, _isDefaultPort, client);
        Settings.ConnectionString = this._connectionString;
        Settings.AnalyzeConnectionString(this._connectionString, true, _isDefaultPort);
      }
      // A single host was specified.
      else
      {
        Settings.ConnectionString = _connectionString;
        if (!(_connectionString.Contains("sslmode") || _connectionString.Contains("ssl mode") || _connectionString.Contains("ssl-mode")))
          Settings.SslMode = MySqlSslMode.Required;
        Settings.AnalyzeConnectionString(this._connectionString, true, _isDefaultPort);

        if (Settings.DnsSrv)
        {
          var dnsSrvRecords = DnsResolver.GetDnsSrvRecords(Settings.Server);
          FailoverManager.SetHostList(dnsSrvRecords.ConvertAll(r => new FailoverServer(r.Target, r.Port, r.Priority)),
            FailoverMethod.Sequential);
          _internalSession = FailoverManager.AttemptConnectionXProtocol(this._connectionString, out this._connectionString, _isDefaultPort, client);
          Settings.ConnectionString = this._connectionString;
        }
        else
          _internalSession = InternalSession.GetSession(Settings);
      }

      // Set the default schema if provided by the user.
      if (!string.IsNullOrWhiteSpace(Settings.Database))
        DefaultSchema = GetSchema(Settings.Database);
    }

    /// <summary>
    /// Initializes a new instance of the BaseSession class based on the specified anonymous type object.
    /// </summary>
    /// <param name="connectionData">The connection data as an anonymous type used to create the session.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connectionData"/> is null.</exception>
    /// <remarks>
    /// <para>Multiple hosts can be specified as part of the <paramref name="connectionData"/>, which enables client-side failover when trying to
    /// establish a connection.</para>
    /// <para>&#160;</para>
    /// <para>To assign multiple hosts, create a property similar to the connection string examples shown in
    /// <see cref="BaseSession(string)"/>. Note that the value of the property must be a string.
    /// </para>
    /// </remarks>
    internal BaseSession(object connectionData, Client client = null) : this()
    {
      if (connectionData == null)
        throw new ArgumentNullException("connectionData");

      _client = client;
      if (client == null)
        FailoverManager.Reset();

      var values = Tools.GetDictionaryFromAnonymous(connectionData);
      if (!values.Keys.Any(s => s.ToLowerInvariant() == PORT_CONNECTION_OPTION_KEYWORD))
        values.Add(PORT_CONNECTION_OPTION_KEYWORD, X_PROTOCOL_DEFAULT_PORT);

      bool hostsParsed = false;
      foreach (var value in values)
      {
        if (!Settings.ContainsKey(value.Key))
          throw new KeyNotFoundException(string.Format(ResourcesX.InvalidConnectionStringAttribute, value.Key));

        Settings.SetValue(value.Key, value.Value);
        if (!hostsParsed && !string.IsNullOrEmpty(Settings[SERVER_CONNECTION_OPTION_KEYWORD].ToString()))
        {
          var server = value.Value.ToString();
          if (IsUnixSocket(server))
            Settings.SetValue(value.Key, server = NormalizeUnixSocket(server));

          FailoverManager.ParseHostList(server, true, false);
          if (FailoverManager.FailoverGroup != null && FailoverManager.FailoverGroup.Hosts?.Count > 1)
            Settings[SERVER_CONNECTION_OPTION_KEYWORD] = null;
          else if (FailoverManager.FailoverGroup != null)
            Settings[SERVER_CONNECTION_OPTION_KEYWORD] = FailoverManager.FailoverGroup.Hosts[0].Host;

          hostsParsed = true;
        }
      }
      this._connectionString = Settings.ToString();

      Settings.AnalyzeConnectionString(this._connectionString, true, _isDefaultPort);
      if (FailoverManager.FailoverGroup != null && FailoverManager.FailoverGroup.Hosts?.Count > 1)
      {
        // Multiple hosts were specified.
        _internalSession = FailoverManager.AttemptConnectionXProtocol(this._connectionString, out this._connectionString, _isDefaultPort, client);
        Settings.ConnectionString = _connectionString;
      }
      else
      {
        if (Settings.DnsSrv)
        {
          var dnsSrvRecords = DnsResolver.GetDnsSrvRecords(Settings.Server);
          FailoverManager.SetHostList(dnsSrvRecords.ConvertAll(r => new FailoverServer(r.Target, r.Port, null)),
            FailoverMethod.Sequential);
          _internalSession = FailoverManager.AttemptConnectionXProtocol(this._connectionString, out this._connectionString, _isDefaultPort, client);
          Settings.ConnectionString = this._connectionString;
        }
        else
          _internalSession = InternalSession.GetSession(Settings);
      }

      if (!string.IsNullOrWhiteSpace(Settings.Database))
        DefaultSchema = GetSchema(Settings.Database);
    }

    internal BaseSession(InternalSession internalSession, Client client)
    {
      _internalSession = internalSession;
      Settings = internalSession.Settings;
      _client = client;
    }

    // Constructor used exclusively to parse connection string or connection data
    internal BaseSession()
    {
      Settings = new MySqlXConnectionStringBuilder();
    }

    /// <summary>
    /// Drops the database/schema with the given name.
    /// </summary>
    /// <param name="schema">The name of the schema.</param>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is null.</exception>
    public void DropSchema(string schema)
    {
      if (string.IsNullOrWhiteSpace(schema)) throw new ArgumentNullException(nameof(schema));
      Schema s = this.GetSchema(schema);
      if (!s.ExistsInDatabase()) return;
      InternalSession.ExecuteSqlNonQuery("DROP DATABASE `" + schema + "`");
    }

    /// <summary>
    /// Creates a schema/database with the given name.
    /// </summary>
    /// <param name="schema">The name of the schema/database.</param>
    /// <returns>A <see cref="Schema"/> object that matches the recently created schema/database.</returns>
    public Schema CreateSchema(string schema)
    {
      InternalSession.ExecuteSqlNonQuery("CREATE DATABASE `" + schema + "`");
      return new Schema(this, schema);
    }

    /// <summary>
    /// Gets the schema with the given name.
    /// </summary>
    /// <param name="schema">The name of the schema.</param>
    /// <returns>A <see cref="Schema"/> object set with the provided schema name.</returns>
    public Schema GetSchema(string schema)
    {
      this.Schema = new Schema(this, schema);
      return this.Schema;
    }

    /// <summary>
    /// Gets a list of schemas (or databases) in this session.
    /// </summary>
    /// <returns>A <see cref="Schema"/> list containing all existing schemas (or databases).</returns>
    public List<Schema> GetSchemas()
    {
      RowResult result = XSession.GetSqlRowResult("select * from information_schema.schemata");
      result.FetchAll();
      var query = from row in result.Rows
                  select new Schema(this, row.GetString("schema_name"));
      return query.ToList<Schema>();
    }

    /// <summary>
    /// Starts a new transaction.
    /// </summary>
    public void StartTransaction()
    {
      InternalSession.ExecuteSqlNonQuery("START TRANSACTION");
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <returns>A <see cref="Result"/> object containing the results of the commit operation.</returns>
    public void Commit()
    {
      InternalSession.ExecuteSqlNonQuery("COMMIT");
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public void Rollback()
    {
      InternalSession.ExecuteSqlNonQuery("ROLLBACK");
    }

    /// <summary>
    /// Closes this session or releases it to the pool.
    /// </summary>
    public void Close()
    {
      if (XSession.SessionState != SessionState.Closed)
      {
        if (_client == null)
          CloseFully();
        else
        {
          _client.ReleaseSession(this);
          XSession.SetState(SessionState.Closed, false);
          _internalSession = null;
        }
      }
    }

    /// <summary>
    /// Closes this session
    /// </summary>
    internal void CloseFully()
    {
      XSession.Close();
    }

    internal void Reset()
    {
      XSession.ResetSession();
    }

    #region Savepoints

    /// <summary>
    /// Sets a transaction savepoint with an autogenerated name.
    /// </summary>
    /// <returns>The autogenerated name of the transaction savepoint.</returns>
    public string SetSavepoint()
    {
      // Autogenerate the name of the savepoint.
      return SetSavepoint($"savepoint_{Guid.NewGuid().ToString().Replace("-", "_")}");
    }

    /// <summary>
    /// Sets a named transaction savepoint.
    /// </summary>
    /// <param name="name">The name of the transaction savepoint.</param>
    /// <returns>The name of the transaction savepoint.</returns>
    public string SetSavepoint(string name)
    {
      InternalSession.ExecuteSqlNonQuery($"SAVEPOINT {name}");
      return name;
    }

    /// <summary>
    /// Removes the named savepoint from the set of savepoints within the current transaction.
    /// </summary>
    /// <param name="name">The name of the transaction savepoint.</param>
    public void ReleaseSavepoint(string name)
    {
      InternalSession.ExecuteSqlNonQuery($"RELEASE SAVEPOINT {name}");
    }

    /// <summary>
    /// Rolls back a transaction to the named savepoint without terminating the transaction.
    /// </summary>
    /// <param name="name">The name of the transaction savepoint.</param>
    public void RollbackTo(string name)
    {
      InternalSession.ExecuteSqlNonQuery($"ROLLBACK TO {name}");
    }

    #endregion

    /// <summary>
    /// Parses the connection data.
    /// </summary>
    /// <param name="connectionData">The connection string or connection URI.</param>
    /// <returns>An updated connection string representation of the provided connection string or connection URI.</returns>
    protected internal string ParseConnectionData(string connectionData, Client client = null)
    {
      if (client == null)
        FailoverManager.Reset();

      if (Regex.IsMatch(connectionData, @"^mysqlx(\+\w+)?://.*", RegexOptions.IgnoreCase))
      {
        return ParseConnectionUri(connectionData);
      }
      else
        return ParseConnectionString(connectionData);
    }

    /// <summary>
    /// Parses a connection URI.
    /// </summary>
    /// <param name="connectionUri">The connection URI to parse.</param>
    /// <returns>The connection string representation of the provided <paramref name="connectionUri"/>.</returns>
    private string ParseConnectionUri(string connectionUri)
    {
      Uri uri = null;
      string updatedUri = null;
      bool parseServerAsUnixSocket = false;
      string hierPart = null;
      try
      {
        uri = new Uri(connectionUri);
      }
      catch (UriFormatException ex)
      {
        if (ex.Message != "Invalid URI: The hostname could not be parsed.")
          throw ex;

        // Identify if multiple hosts were specified.
        string[] splitUri = connectionUri.Split('@', '?');
        if (splitUri.Length == 1) throw ex;

        hierPart = splitUri[1];
        var schema = string.Empty;
        parseServerAsUnixSocket = IsUnixSocket(hierPart);
        bool isArray = hierPart.StartsWith("[") && hierPart.Contains("]");

        // Remove schema.
        if ((!parseServerAsUnixSocket && hierPart.Contains("/")) && !isArray ||
          (parseServerAsUnixSocket && hierPart.Contains(")/")) ||
          (hierPart.StartsWith("[") && hierPart.Contains("]/") && isArray))
        {
          schema = hierPart.Substring(hierPart.LastIndexOf('/') + 1);
          hierPart = hierPart.Substring(0, hierPart.Length - schema.Length - 1);
        }

        if (parseServerAsUnixSocket)
        {
          updatedUri = splitUri[0] + "@localhost" +
            (schema != string.Empty ? "/" + schema : string.Empty) +
            (splitUri.Length > 2 ? "?" + splitUri[2] : string.Empty);
        }
        else if (isArray)
        {
          hierPart = hierPart.Substring(1, hierPart.Length - 2);
          int hostCount = FailoverManager.ParseHostList(hierPart, true, true);
          if (FailoverManager.FailoverGroup != null)
          {
            hierPart = FailoverManager.FailoverGroup.ActiveHost.Host;
            parseServerAsUnixSocket = IsUnixSocket(FailoverManager.FailoverGroup.ActiveHost.Host);
            updatedUri = splitUri[0] + "@" +
              (parseServerAsUnixSocket ? "localhost" : hierPart) +
              (FailoverManager.FailoverGroup.ActiveHost.Port != -1 ? ":" + FailoverManager.FailoverGroup.ActiveHost.Port : string.Empty) +
              (schema != string.Empty ? "/" + schema : string.Empty) +
              (splitUri.Length == 3 ? "?" + splitUri[2] : string.Empty);
          }
          else if (hostCount == 1)
            updatedUri = splitUri[0] + "@" + hierPart +
              (schema != string.Empty ? "/" + schema : string.Empty) +
              (splitUri.Length == 3 ? "?" + splitUri[2] : string.Empty);
          else
            throw ex;
        }
      }

      if (uri == null)
        uri = updatedUri == null ? new Uri(connectionUri) : new Uri(updatedUri);

      if (uri.Scheme == DNS_SRV_URI_SCHEME)
      {
        if (FailoverManager.FailoverGroup != null && FailoverManager.FailoverGroup.Hosts?.Count > 1)
          throw new ArgumentException(Resources.DnsSrvInvalidConnOptionMultihost);
        if (!uri.IsDefaultPort)
          throw new ArgumentException(Resources.DnsSrvInvalidConnOptionPort);
        if (parseServerAsUnixSocket)
          throw new ArgumentException(Resources.DnsSrvInvalidConnOptionUnixSocket);
      }
      else if (uri.Scheme != MYSQLX_URI_SCHEME && uri.Scheme != SSH_URI_SCHEME)
        throw new ArgumentException(string.Format(ResourcesX.DnsSrvInvalidScheme, uri.Scheme));

      return ConvertToConnectionString(uri, hierPart, parseServerAsUnixSocket, uri.Scheme == DNS_SRV_URI_SCHEME);
    }

    /// <summary>
    /// Validates if the string provided is a Unix socket file.
    /// </summary>
    /// <param name="unixSocket">The Unix socket to evaluate.</param>
    /// <returns><c>true</c> if <paramref name="unixSocket"/> is a valid Unix socket; otherwise, <c>false</c>.</returns>
    internal static bool IsUnixSocket(string unixSocket)
    {
      if (unixSocket.StartsWith(".") ||
        unixSocket.StartsWith("/") ||
        unixSocket.StartsWith("(.") ||
        unixSocket.StartsWith("(/") ||
        unixSocket.StartsWith("%2") ||
        unixSocket.StartsWith("(%2"))
        return true;

      return false;
    }

    /// <summary>
    /// Converts the URI object into a connection string.
    /// </summary>
    /// <param name="uri">An <see cref="Uri"/> instance with the values for the provided connection options.</param>
    /// <param name="unixSocketPath">The path of the Unix socket file.</param>
    /// <param name="parseServerAsUnixSocket">If <c>true</c> the <paramref name="unixSocketPath"/> replaces the value for the server connection option; otherwise, <c>false</c></param>
    /// <returns>A connection string.</returns>
    private string ConvertToConnectionString(Uri uri, string unixSocketPath, bool parseServerAsUnixSocket, bool isDnsSrvScheme)
    {
      List<string> connectionParts = new List<string>();

      if (string.IsNullOrWhiteSpace(uri.Host))
        throw new UriFormatException(ResourcesX.InvalidUriData + "host");
      connectionParts.Add("server=" + (parseServerAsUnixSocket ?
        NormalizeUnixSocket(unixSocketPath) :
        uri.Host));
      connectionParts.Add("port=" + (uri.Port == -1 ? 33060 : uri.Port));
      _isDefaultPort = uri.IsDefaultPort;
      if (uri.Scheme == DNS_SRV_URI_SCHEME)
        connectionParts.Add("dns-srv=true");

      if (!string.IsNullOrWhiteSpace(uri.UserInfo))
      {
        string[] userData = uri.UserInfo.Split(':');
        if (userData.Length > 2)
          throw new UriFormatException(ResourcesX.InvalidUriData + "user info");
        connectionParts.Add("uid=" + System.Uri.UnescapeDataString(userData[0]));
        if (userData.Length > 1)
          connectionParts.Add("password=" + System.Uri.UnescapeDataString(userData[1]));
      }
      if (uri.Segments.Length > 2)
        throw new UriFormatException(ResourcesX.InvalidUriData + "segments");
      if (uri.Segments.Length > 1)
      {
        connectionParts.Add("database=" + System.Uri.UnescapeDataString(uri.Segments[1]));
      }
      if (!string.IsNullOrWhiteSpace(uri.Query))
      {
        string[] queries = System.Uri.UnescapeDataString(uri.Query).Substring(1).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string query in queries)
        {
          string[] keyValue = query.Replace(";", string.Empty).Split('=');
          string part;
          var connectionAttributesOption = MySqlXConnectionStringBuilder.Options.Options.First(item => item.Keyword == CONNECTION_ATTRIBUTES_CONNECTION_OPTION_KEYWORD);
          var dnsSrvOption = MySqlXConnectionStringBuilder.Options.Options.First(item => item.Keyword == DNS_SRV_CONNECTION_OPTION_KEYWORD);

          if (!((connectionAttributesOption.Keyword == keyValue[0]) || connectionAttributesOption.Synonyms.Contains(keyValue[0]) && keyValue.Count() > 2))
          {
            if (keyValue.Length > 2)
              throw new ArgumentException(ResourcesX.InvalidUriQuery + ":" + keyValue[0]);
            var connecttimeoutOption = MySqlXConnectionStringBuilder.Options.Options.First(item => item.Keyword == CONNECT_TIMEOUT_CONNECTION_OPTION_KEYWORD);
            if ((connecttimeoutOption.Keyword == keyValue[0] || connecttimeoutOption.Synonyms.Contains(keyValue[0])) &&
              String.IsNullOrWhiteSpace(keyValue[1]))
              throw new FormatException(ResourcesX.InvalidConnectionTimeoutValue);
            part = keyValue[0] + "=" + (keyValue.Length == 2 ? keyValue[1] : "true").Replace("(", string.Empty).Replace(")", string.Empty);
          }
          else if (keyValue[1] == string.Empty)
            throw new MySqlException(ResourcesX.InvalidUriQuery + ": " + keyValue[0]);
          else
            part = keyValue[0] + "=" + query.Replace(keyValue[0] + "=", string.Empty);

          if (isDnsSrvScheme && (dnsSrvOption.Keyword == keyValue[0] || dnsSrvOption.Synonyms.Contains(keyValue[0])) && !Convert.ToBoolean(keyValue[1]))
            throw new ArgumentException(string.Format(ResourcesX.DnsSrvConflictingOptions, dnsSrvOption.Keyword));
          else if (isDnsSrvScheme && (dnsSrvOption.Keyword == keyValue[0] || dnsSrvOption.Synonyms.Contains(keyValue[0])))
            continue;

          connectionParts.Add(part);
        }
      }

      return string.Join("; ", connectionParts);
    }

    /// <summary>
    /// Parses a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>The parsed connection string.</returns>
    private string ParseConnectionString(string connectionString)
    {
      var updatedConnectionString = string.Empty;
      bool portProvided = false;
      bool isDnsSrv = false;
      var connectionOptionsDictionary = connectionString.Split(CONNECTION_DATA_KEY_SEPARATOR)
                .Select(item => item.Split(new char[] { CONNECTION_DATA_VALUE_SEPARATOR }, 2))
                .Where(item => item.Length == 2)
                .ToDictionary(item => item[0], item => item[1]);
      var serverOption = MySqlXConnectionStringBuilder.Options.Options.First(item => item.Keyword == SERVER_CONNECTION_OPTION_KEYWORD);
      var connecttimeoutOption = MySqlXConnectionStringBuilder.Options.Options.First(item => item.Keyword == CONNECT_TIMEOUT_CONNECTION_OPTION_KEYWORD);
      foreach (KeyValuePair<string, string> keyValuePair in connectionOptionsDictionary)
      {
        // Value is an equal or a semicolon
        if (keyValuePair.Value == "=" || keyValuePair.Value == "\"")
          throw new MySqlException(string.Format(Resources.InvalidConnectionStringValue, (keyValuePair.Value == "\"" ? ";" : "="), keyValuePair.Key));

        // Key is not server or any of its synonyms.
        if (keyValuePair.Key != serverOption.Keyword && !serverOption.Synonyms.Contains(keyValuePair.Key))
        {
          if ((connecttimeoutOption.Keyword == keyValuePair.Key || connecttimeoutOption.Synonyms.Contains(keyValuePair.Key)) &&
            String.IsNullOrWhiteSpace(keyValuePair.Value))
            throw new FormatException(ResourcesX.InvalidConnectionTimeoutValue);
          if (keyValuePair.Key == PORT_CONNECTION_OPTION_KEYWORD)
            portProvided = true;
          if (keyValuePair.Key == DNS_SRV_CONNECTION_OPTION_KEYWORD)
            isDnsSrv = Convert.ToBoolean(keyValuePair.Value);

          updatedConnectionString += $"{keyValuePair.Key}{CONNECTION_DATA_VALUE_SEPARATOR}{keyValuePair.Value}{CONNECTION_DATA_KEY_SEPARATOR}";
          continue;
        }

        // Key is server or one of its synonyms.
        var updatedValue = keyValuePair.Value;
        if (IsUnixSocket(keyValuePair.Value))
          updatedValue = NormalizeUnixSocket(keyValuePair.Value);

        // The value for the server connection option doesn't have a server list format.
        if (FailoverManager.ParseHostList(updatedValue, true, false) == 1 && FailoverManager.FailoverGroup == null)
          updatedConnectionString = $"{SERVER_CONNECTION_OPTION_KEYWORD}{CONNECTION_DATA_VALUE_SEPARATOR}{updatedValue}{CONNECTION_DATA_KEY_SEPARATOR}{updatedConnectionString}";
      }

      // DNS SRV Validation - Port cannot be provided by the user and multihost is not allowed if dns-srv is true
      if (isDnsSrv)
      {
        if (portProvided)
          throw new ArgumentException(Resources.DnsSrvInvalidConnOptionPort);
        if (FailoverManager.FailoverGroup != null)
          throw new ArgumentException(Resources.DnsSrvInvalidConnOptionMultihost);
      }

      // Default port must be added if not provided by the user.
      if (FailoverManager.FailoverGroup == null)
        return portProvided ? updatedConnectionString : $"{updatedConnectionString}{CONNECTION_DATA_KEY_SEPARATOR}{PORT_CONNECTION_OPTION_KEYWORD}{CONNECTION_DATA_VALUE_SEPARATOR}{X_PROTOCOL_DEFAULT_PORT}";

      return $"{SERVER_CONNECTION_OPTION_KEYWORD}{CONNECTION_DATA_VALUE_SEPARATOR}{FailoverManager.FailoverGroup.ActiveHost.Host}{CONNECTION_DATA_KEY_SEPARATOR}" +
        (!portProvided ? $"{PORT_CONNECTION_OPTION_KEYWORD}{CONNECTION_DATA_VALUE_SEPARATOR}{X_PROTOCOL_DEFAULT_PORT}{CONNECTION_DATA_KEY_SEPARATOR}" : string.Empty) +
        updatedConnectionString;
    }

    /// <summary>
    /// Normalizes the Unix socket by removing leading and ending parenthesis as well as removing special characters.
    /// </summary>
    /// <param name="unixSocket">The Unix socket to normalize.</param>
    /// <returns>A normalized Unix socket.</returns>
    internal static string NormalizeUnixSocket(string unixSocket)
    {
      unixSocket = unixSocket.Replace("%2F", "/");
      if (unixSocket.StartsWith("(") && unixSocket.EndsWith(")"))
        unixSocket = unixSocket.Substring(1, unixSocket.Length - 2);

      return unixSocket;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    /// <summary>
    /// Disposes the current object. Disposes of the managed state if the flag is set to true.
    /// </summary>
    /// <param name="disposing">Flag to indicate if the managed state is to be disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          // dispose managed state (managed objects).
          Close();
        }

        // free unmanaged resources (unmanaged objects) and override a finalizer below.
        // set large fields to null.

        disposedValue = true;
      }
    }

    // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~BaseSession() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    /// <summary>
    /// Disposes the current object. Code added to correctly implement the disposable pattern.
    /// </summary>
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }

  /// <summary>
  /// Describes the state of the session.
  /// </summary>
  public enum SessionState
  {
    /// <summary>
    /// The session is closed.
    /// </summary>
    Closed = 0,
    /// <summary>
    /// The session is open.
    /// </summary>
    Open = 1,
    /// <summary>
    /// The session object is connecting to the data source.
    /// </summary>
    Connecting = 2,
    /// <summary>
    /// The session object is executing a command.
    /// </summary>
    Executing = 4,
  }
}
