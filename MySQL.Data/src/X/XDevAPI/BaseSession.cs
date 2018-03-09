// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Linq;
using MySqlX.Common;
using MySqlX.Data;
using MySqlX.Sessions;
using MySqlX.XDevAPI.Relational;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using MySqlX.Failover;
using MySqlX.XDevAPI.Common;
using System.Net;
using MySql.Data.Common;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a base class for a Session.
  /// </summary>
  public abstract class BaseSession : IDisposable
  {
    private InternalSession _internalSession;
    private string connectionString;
    private bool disposed = false;
    private const uint newDefaultPort = 33060;
    internal QueueTaskScheduler scheduler = new QueueTaskScheduler();

    /// <summary>
    /// Gets the connection settings for this session.
    /// </summary>
    public MySqlConnectionStringBuilder Settings { get; private set; }

    /// <summary>
    /// Gets or sets the currently active schema.
    /// </summary>
    public Schema Schema { get; protected set; }

    internal InternalSession InternalSession
    {
      get { return _internalSession; }
    }

    internal XInternalSession XSession
    {
      get { return InternalSession as XInternalSession; }
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
    /// which will enable client side failover when trying to establish a connection.</para>
    /// <para>&#160;</para>
    /// <para>Connection string examples (in URI format):
    /// <para />- mysqlx://test:test@[192.1.10.10,localhost]
    /// <para />- mysqlx://test:test@[192.1.10.10,127.0.0.1]
    /// <para />- mysqlx://root:@[../tmp/mysqlx.sock,/tmp/mysqld.sock]?protocol=unix&#38;sslmode=none
    /// <para />- mysqlx://test:test@[192.1.10.10:33060,127.0.0.1:33060]
    /// <para />- mysqlx://test:test@[192.1.10.10,120.0.0.2:22000,[::1]:33060]/test?connectiontimeout=10
    /// <para />- mysqlx://test:test@[(address=server.example,priority=20),(address=127.0.0.1,priority=100)]
    /// <para />- mysqlx://test:test@[(address=server.example,priority=100),(address=127.0.0.1,priority=75),(address=192.0.10.56,priority=25)]
    /// </para>
    /// <para>&#160;</para>
    /// <para>Connection string examples (in basic format):
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
    public BaseSession(string connectionString)
    {
      if (string.IsNullOrWhiteSpace(connectionString))
        throw new ArgumentNullException("connectionString");

      this.connectionString = ParseConnectionString(connectionString);

      if (FailoverManager.FailoverGroup != null)
      {
        // Multiple hosts were specified.
        _internalSession = FailoverManager.AttemptConnection(this.connectionString, out this.connectionString);
        Settings = new MySqlConnectionStringBuilder(this.connectionString);
      }
      else
      {
        // A single host was specified.
        Settings = new MySqlConnectionStringBuilder(this.connectionString);
        _internalSession = InternalSession.GetSession(Settings);
      }

      if (!string.IsNullOrWhiteSpace(Settings.Database))
        GetSchema(Settings.Database);
    }

    /// <summary>
    /// Initializes a new instance of the BaseSession class based on the specified anonymous type object.
    /// </summary>
    /// <param name="connectionData">The connection data as an anonymous type used to create the session.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connectionData"/> is null.</exception>
    /// <remarks>
    /// <para>Multiple hosts can be specified as part of the <paramref name="connectionData"/>, which will enable client-side failover when trying to
    /// establish a connection.</para>
    /// <para>&#160;</para>
    /// <para>To assign multiple hosts create a property similar to the connection string examples (in basic format) shown in
    /// <see cref="BaseSession(string)"/>. Note that the value of the property must be a string.
    /// </para>
    /// </remarks>
    public BaseSession(object connectionData)
    {
      if (connectionData == null)
        throw new ArgumentNullException("connectionData");
      var values = Tools.GetDictionaryFromAnonymous(connectionData);
      if (!values.Keys.Any(s => s.ToLowerInvariant() == "port"))
        values.Add("port", newDefaultPort);
      Settings = new MySqlConnectionStringBuilder();
      bool hostsParsed = false;
      foreach (var value in values)
      {
        if (!Settings.ContainsKey(value.Key))
          throw new KeyNotFoundException(string.Format(ResourcesX.InvalidConnectionStringAttribute, value.Key));
        Settings.SetValue(value.Key, value.Value);
        if (!hostsParsed && !string.IsNullOrEmpty(Settings["server"].ToString()))
        {
          var server = value.Value.ToString();
          if (IsUnixSocket(server))
            Settings.SetValue(value.Key, server = NormalizeUnixSocket(server));
          ParseHostList(server, false);
          if (FailoverManager.FailoverGroup != null) Settings["server"] = FailoverManager.FailoverGroup.ActiveHost.Host;
          hostsParsed = true;
        }
      }
      this.connectionString = Settings.ToString();

      if (FailoverManager.FailoverGroup != null)
      {
        // Multiple hosts were specified.
        _internalSession = FailoverManager.AttemptConnection(this.connectionString, out this.connectionString);
        Settings = new MySqlConnectionStringBuilder(this.connectionString);
      }
      else _internalSession = InternalSession.GetSession(Settings);

      if (!string.IsNullOrWhiteSpace(Settings.Database))
        GetSchema(Settings.Database);
    }

    #region Session status properties

    private DBVersion? _version = null;

    internal DBVersion Version => _version ?? (_version = XSession.GetServerVersion()).Value;

    #endregion

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

    //public Schema GetDefaultSchema()
    //{
    //  return new Schema(this, "default");
    //}

    //public Schema UseDefaultSchema()
    //{
    //  return new Schema(this, "default");
    //}

    /// <summary>
    /// Gets a list of schemas/databases in this session.
    /// </summary>
    /// <returns>A <see cref="Schema"/> list containing all existing schemas/databases.</returns>
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
    public Result Commit()
    {
      return InternalSession.ExecuteSqlNonQuery("COMMIT");
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public Result Rollback()
    {
      return InternalSession.ExecuteSqlNonQuery("ROLLBACK");
    }

    /// <summary>
    /// Closes this session.
    /// </summary>
    public void Close()
    {
      if (XSession.SessionState != SessionState.Closed)
      {
        XSession.Close();
      }
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
    /// Parses the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string in basic or URI format.</param>
    /// <returns>An updated connection string in basic format.</returns>
    /// <remarks>The format (basic or URI) of the connection string is determined as well as the
    /// prescence of multiple hosts.</remarks>
    protected internal string ParseConnectionString(string connectionString)
    {
      FailoverManager.Reset();

      // Connection string is in URI format.
      if (Regex.IsMatch(connectionString, @"^mysqlx(\+\w+)?://.*", RegexOptions.IgnoreCase))
        return ParseUriConnectionString(connectionString);
      else
        return ParseBasicConnectionString(connectionString);
    }

    /// <summary>
    /// Parses a connection string in URI format.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>A connection string in basic format.</returns>
    private string ParseUriConnectionString(string connectionString)
    {
      Uri uri = null;
      string updatedUriString = null;
      bool parseServerAsUnixSocket = false;
      string hierPart = null;
      try
      {
        uri = new Uri(connectionString);
      }
      catch (UriFormatException ex)
      {
        if (ex.Message != "Invalid URI: The hostname could not be parsed.")
          throw ex;

        // Identify if multiple hosts were specified.
        string[] splitUriString = connectionString.Split('@', '?');
        if (splitUriString.Length == 1) throw ex;

        hierPart = splitUriString[1];
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
          updatedUriString = splitUriString[0] + "@localhost" +
            (schema != string.Empty ? "/" + schema : string.Empty) +
            (splitUriString.Length > 2 ? "?" + splitUriString[2] : string.Empty);
        }
        else if (isArray)
        {
          hierPart = hierPart.Substring(1, hierPart.Length - 2);
          int hostCount = ParseHostList(hierPart, true);
          if (FailoverManager.FailoverGroup != null)
          {
            hierPart = FailoverManager.FailoverGroup.ActiveHost.Host;
            parseServerAsUnixSocket = IsUnixSocket(FailoverManager.FailoverGroup.ActiveHost.Host);
            updatedUriString = splitUriString[0] + "@" +
              (parseServerAsUnixSocket ? "localhost" : hierPart) +
              (FailoverManager.FailoverGroup.ActiveHost.Port != -1 ? ":" + FailoverManager.FailoverGroup.ActiveHost.Port : string.Empty) +
              (schema != string.Empty ? "/" + schema : string.Empty) +
              (splitUriString.Length == 3 ? "?" + splitUriString[2] : string.Empty);
          }
          else if (hostCount == 1)
            updatedUriString = splitUriString[0] + "@" + hierPart +
              (schema != string.Empty ? "/" + schema : string.Empty) +
              (splitUriString.Length == 3 ? "?" + splitUriString[2] : string.Empty);
          else
            throw ex;
        }
      }

      if (uri == null)
        uri = updatedUriString == null ? new Uri(connectionString) : new Uri(updatedUriString);

      return ConvertToBasicConnectionString(uri, hierPart, parseServerAsUnixSocket);
    }

    /// <summary>
    /// Validates if the string provided is a Unix socket.
    /// </summary>
    /// <param name="unixSocket">The Unix socket to evaluate.</param>
    /// <returns><c>true</c> if <paramref name="unixSocket"/> is a valid Unix socket; otherwise, <c>false</c>.</returns>
    private bool IsUnixSocket(string unixSocket)
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
    /// Converts the URI object into a connection string in basic format.
    /// </summary>
    /// <param name="uri">An <see cref="Uri"/> instance with the values for the provided connection options.</param>
    /// <param name="unixSocketPath">The path of the Unix socket.</param>
    /// <param name="parseServerAsUnixSocket">if <c>true</c> the <paramref name="unixSocketPath"/> will replace the value for the server connection option; otherwise, <c>false</c></param>
    /// <returns>A connection string in basic format.</returns>
    private string ConvertToBasicConnectionString(Uri uri, string unixSocketPath, bool parseServerAsUnixSocket)
    {
      List<string> connectionParts = new List<string>();

      if (string.IsNullOrWhiteSpace(uri.Host))
        throw new UriFormatException(ResourcesX.InvalidUriData + "host");
      connectionParts.Add("server=" + (parseServerAsUnixSocket ?
        NormalizeUnixSocket(unixSocketPath) :
        uri.Host));
      connectionParts.Add("port=" + (uri.Port == -1 ? 33060 : uri.Port));

      if (!string.IsNullOrWhiteSpace(uri.UserInfo))
      {
        string[] userData = uri.UserInfo.Split(':');
        if (userData.Length > 2)
          throw new UriFormatException(ResourcesX.InvalidUriData + "user info");
        connectionParts.Add("uid=" + Uri.UnescapeDataString(userData[0]));
        if (userData.Length > 1)
          connectionParts.Add("password=" + Uri.UnescapeDataString(userData[1]));
      }
      if (uri.Segments.Length > 2)
        throw new UriFormatException(ResourcesX.InvalidUriData + "segments");
      if (uri.Segments.Length > 1)
      {
        connectionParts.Add("database=" + Uri.UnescapeDataString(uri.Segments[1]));
      }
      if (!string.IsNullOrWhiteSpace(uri.Query))
      {
        string[] queries = Uri.UnescapeDataString(uri.Query).Substring(1).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string query in queries)
        {
          string[] keyValue = query.Split('=');
          if (keyValue.Length > 2)
            throw new ArgumentException(ResourcesX.InvalidUriQuery + ":" + keyValue[0]);
          string part = keyValue[0] + "=" + (keyValue.Length == 2 ? keyValue[1] : "true").Replace("(", string.Empty).Replace(")", string.Empty);
          connectionParts.Add(part);
        }
      }

      return string.Join("; ", connectionParts);
    }

    /// <summary>
    /// Parses a connection string in basic format.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>The parsed connection string.</returns>
    private string ParseBasicConnectionString(string connectionString)
    {
      // Connection string is in basic format.
      string updatedConnectionString = string.Empty;
      string[] keyValuePairs = connectionString.Substring(0).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      bool portProvided = false;
      foreach (string keyValuePair in keyValuePairs)
      {
        int separatorCharIndex = keyValuePair.IndexOf('=');
        if (separatorCharIndex == -1) continue;

        var keyword = keyValuePair.Substring(0, separatorCharIndex);
        var value = keyValuePair.Substring(separatorCharIndex + 1);
        if (keyword != "server" && keyword != "host" && keyword != "data source" && keyword != "datasource" && keyword != "address" && keyword != "addr" && keyword != "network address")
        {
          if (keyword == "port") portProvided = true;
          updatedConnectionString += keyValuePair + ";";
          continue;
        }

        if (IsUnixSocket(value)) value = NormalizeUnixSocket(value);
        if (ParseHostList(value, false) == 1 && FailoverManager.FailoverGroup == null)
          updatedConnectionString = "server=" + value + ";" + updatedConnectionString;
      }

      if (FailoverManager.FailoverGroup == null)
        return portProvided ? updatedConnectionString : updatedConnectionString + ";port=" + newDefaultPort;

      return "server=" +
        FailoverManager.FailoverGroup.ActiveHost.Host + ";" +
        (!portProvided ? "port=" + newDefaultPort + ";" : string.Empty) +
        updatedConnectionString;
    }

    /// <summary>
    /// Initializes the <see cref="FailoverManager"/> if more than one host is found.
    /// </summary>
    /// <param name="hierPart">A string containing an unparsed host list.</param>
    /// <param name="connectionStringIsInUriFormat">True if the connection string is in URI format, false otherwise.</param>
    /// <returns>The number of hosts found, -1 if an error was raised during parsing.</returns>
    private int ParseHostList(string hierPart, bool connectionStringIsInUriFormat)
    {
      if (string.IsNullOrWhiteSpace(hierPart)) return -1;

      int hostCount = -1;
      FailoverMethod failoverMethod = FailoverMethod.Sequential;
      string[] hostArray = null;
      List<XServer> hostList = new List<XServer>();
      hierPart = hierPart.Replace(" ", "");

      if (!hierPart.StartsWith("(") && !hierPart.EndsWith(")"))
      {
        hostArray = hierPart.Split(',');
        foreach (var host in hostArray)
        {
          if (IsUnixSocket(host)) hostList.Add(new XServer(NormalizeUnixSocket(host), -1, -1));
          else hostList.Add(this.ConvertToXServer(host, connectionStringIsInUriFormat));
        }

        if (hostArray.Length == 1) return 1;
        hostCount = hostArray.Length;
      }
      else
      {
        string[] groups = hierPart.Split(new string[] { "),(" }, StringSplitOptions.RemoveEmptyEntries);
        bool? allHavePriority = null;
        int defaultPriority = 100;
        foreach (var group in groups)
        {
          // Remove leading parenthesis.
          var normalizedGroup = group;
          if (normalizedGroup.StartsWith("(")) normalizedGroup = group.Substring(1);
          if (normalizedGroup.EndsWith(")")) normalizedGroup = normalizedGroup.Substring(0, group.Length - 1);
          string[] items = normalizedGroup.Split(',');
          string[] keyValuePairs = items[0].Split('=');
          if (keyValuePairs[0].ToLowerInvariant() != "address")
            throw new KeyNotFoundException(string.Format(ResourcesX.KeywordNotFound, "address"));

          string host = keyValuePairs[1];
          if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentNullException("server");

          if (items.Length == 2)
          {
            if (allHavePriority != null && allHavePriority == false)
              throw new ArgumentException(ResourcesX.PriorityForAllOrNoHosts);
            allHavePriority = allHavePriority ?? true;
            keyValuePairs = items[1].Split('=');
            if (keyValuePairs[0].ToLowerInvariant() != "priority")
              throw new KeyNotFoundException(string.Format(ResourcesX.KeywordNotFound, "priority"));

            if (string.IsNullOrWhiteSpace(keyValuePairs[1]))
              throw new ArgumentNullException("priority");

            int priority = -1;
            Int32.TryParse(keyValuePairs[1], out priority);
            if (priority < 0 || priority > 100)
              throw new ArgumentException(ResourcesX.PriorityOutOfLimits);

            hostList.Add(ConvertToXServer(IsUnixSocket(host) ? NormalizeUnixSocket(host) : host, connectionStringIsInUriFormat, priority));
          }
          else
          {
            if (allHavePriority != null && allHavePriority == true)
              throw new ArgumentException(ResourcesX.PriorityForAllOrNoHosts);
            allHavePriority = allHavePriority ?? false;

            hostList.Add(ConvertToXServer(host, connectionStringIsInUriFormat, defaultPriority > 0 ? defaultPriority-- : 0));
          }
        }

        hostCount = groups.Length;
        failoverMethod = FailoverMethod.Priority;
      }

      FailoverManager.SetHostList(hostList, failoverMethod);
      return hostCount;
    }

    /// <summary>
    /// Creates a <see cref="XServer"/> object based on the provided parameters.
    /// </summary>
    /// <param name="host">The host string which can be a simple host name or a host name and port.</param>
    /// <param name="connectionStringIsInUriFormat">True if the connection string is in URI format, false otherwise.</param>
    /// <param name="priority">The priority of the host.</param>
    /// <param name="port">The port number of the host.</param>
    /// <returns></returns>
    private XServer ConvertToXServer(string host, bool connectionStringIsInUriFormat, int priority = -1, int port = -1)
    {
      host = host.Trim();
      IPAddress address;
      int colonIndex = -1;
      if (IPAddress.TryParse(host, out address))
      {
        switch (address.AddressFamily)
        {
          case System.Net.Sockets.AddressFamily.InterNetworkV6:
            if (host.StartsWith("[") && host.Contains("]") && !host.EndsWith("]"))
              colonIndex = host.LastIndexOf(":");
            break;
          default:
            colonIndex = host.IndexOf(":");
            break;
        }
      }
      else colonIndex = host.IndexOf(":");

      if (colonIndex != -1)
      {
        if (!connectionStringIsInUriFormat)
          throw new ArgumentException(ResourcesX.PortNotSupported);
        int.TryParse(host.Substring(colonIndex + 1), out port);
        host = host.Substring(0, colonIndex);
      }

      return new XServer(host, port, priority);
    }

    /// <summary>
    /// Normalizes the Unix socket by removing leading and ending parenthesis as well as removing special characters.
    /// </summary>
    /// <param name="unixSocket">The Unix socket to normalize.</param>
    /// <returns>A normalized Unix socket.</returns>
    private string NormalizeUnixSocket(string unixSocket)
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
