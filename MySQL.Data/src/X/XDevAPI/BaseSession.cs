// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
    /// <param name="connectionString">The connection string of the session.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is null.</exception>
    /// <exception cref="UriFormatException">Unable to parse the <paramref name="connectionString"/> when 
    /// in URI format.</exception>
    /// <remarks>
    /// <para>Multiple hosts can be specified as part of the <paramref name="connectionString"/> 
    /// which will enable client side failover when trying to establish a connection.</para>
    /// <para>&#160;</para>
    /// <para>Connection string examples (in URI format):
    /// <para />- mysqlx://test:test@[192.1.10.10,localhost]
    /// <para />- mysqlx://test:test@[192.1.10.10,127.0.0.1]
    /// <para />- mysqlx://test:test@[192.1.10.10:33060,127.0.0.1:33060]
    /// <para />- mysqlx://test:test@[192.1.10.10,120.0.0.2:22000,[::1]:33060]/test?connectiontimeout=10
    /// </para>
    /// <para>&#160;</para>
    /// <para>Connection string examples (in basic format):
    /// <para />- server=10.10.10.10,localhost;port=33060;uid=test;password=test;
    /// <para />- host=10.10.10.10,192.101.10.2,localhost;port=5202;uid=test;password=test;
    /// </para>
    /// <para>&#160;</para>
    /// <para>Connection attempts will be performed in a sequential order, that is, one after another until 
    /// a connection is successful or all the elements from the list have been tried.
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
    /// <param name="connectionData">The connection data as an anonymous type of the session.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connectionData"/> is null.</exception>
    public BaseSession(object connectionData)
    {
      if (connectionData == null)
        throw new ArgumentNullException("connectionData");
      var values = Tools.GetDictionaryFromAnonymous(connectionData);
      if (!values.Keys.Any(s => s.ToLowerInvariant() == "port"))
        values.Add("port", newDefaultPort);
      Settings = new MySqlConnectionStringBuilder();
      foreach (var value in values)
      {
        if (!Settings.ContainsKey(value.Key))
          throw new KeyNotFoundException(string.Format(ResourcesX.InvalidConnectionStringAttribute, value.Key));
        Settings.SetValue(value.Key, value.Value);
      }
      this.connectionString = Settings.ToString();
      _internalSession = InternalSession.GetSession(Settings);
      if (!string.IsNullOrWhiteSpace(Settings.Database))
        GetSchema(Settings.Database);
    }

    /// <summary>
    /// Drops the database/schema with the given name.
    /// </summary>
    /// <param name="schema">The name of the schema.</param>
    public void DropSchema(string schema)
    {
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

    /// <summary>
    /// Parses the connection string. The format (basic/URI) of the connection string is determined as well as the 
    /// prescence of multiple hosts.
    /// </summary>
    /// <param name="connectionString">The connection string in basic or URI format.</param>
    /// <returns>An updated connection string in basic format.</returns>
    protected internal string ParseConnectionString(string connectionString)
    {
      FailoverManager.Reset();

      // Connection string has a URI format.
      if (Regex.IsMatch(connectionString, @"^mysqlx(\+\w+)?://.*", RegexOptions.IgnoreCase))
      {
        Uri uri = null;
        string updatedUriString = null;
        try
        {
          uri = new Uri(connectionString);
        }
        catch (UriFormatException ex)
        {
          if (ex.Message != "Invalid URI: The hostname could not be parsed.")
            throw ex;

          // Identify if multiple hosts were specified.
          string[] splitUriString = connectionString.Split('@','?');
          string[] hostsAndDb = splitUriString[1].Split('/');
          ParseHostList(hostsAndDb[0]);
          if (FailoverManager.FailoverGroup == null)
            throw ex;

          updatedUriString = splitUriString[0] + "@" + FailoverManager.FailoverGroup.ActiveHost.Host +
            (FailoverManager.FailoverGroup.ActiveHost.Port != -1 ? ":" + FailoverManager.FailoverGroup.ActiveHost.Port : string.Empty) +
            (hostsAndDb.Length == 2 ? "/" + hostsAndDb[1] : string.Empty) +
            (splitUriString.Length == 3 ? "?" + splitUriString[2] : string.Empty);
        }
        finally
        {
          if (uri==null)
            uri = updatedUriString==null ? new Uri(connectionString) : new Uri(updatedUriString);
        }

        List<string> connectionParts = new List<string>();

        if (string.IsNullOrWhiteSpace(uri.Host))
          throw new UriFormatException(ResourcesX.InvalidUriData + "host");
        connectionParts.Add("server=" + uri.Host);
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
          string[] queries = uri.Query.Substring(1).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
          foreach (string query in queries)
          {
            string[] keyValue = query.Split('=');
            if (keyValue.Length > 2)
              throw new ArgumentException(ResourcesX.InvalidUriQuery + ":" + keyValue[0]);
            string part = Uri.UnescapeDataString(keyValue[0]) + "=" + (keyValue.Length == 2 ? keyValue[1] : "true");
            connectionParts.Add(part);
          }
        }

        return string.Join("; ", connectionParts);
      }

      // Connection string doesn't have a URI format.
      string updatedConnectionString = string.Empty;
      string[] keyValuePairs = connectionString.Substring(0).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (string keyValuePair in keyValuePairs)
      {
        string[] keyValue = keyValuePair.Split('=');
        if (keyValue.Length % 2 != 0)
          continue;

        var keyword = keyValue[0].ToLowerInvariant();
        var value = keyValue[1];
        if (keyword != "server" && keyword != "host" && keyword != "data source" && keyword != "datasource" && keyword != "address" && keyword != "addr" && keyword != "network address")
        {
          updatedConnectionString += keyValuePair + ";";
          continue;
        }

        // Identify if multiple hosts were specified.
        string[] hosts = value.Split(',');
        if (hosts.Length==1) break;
        List<XServer> hostList = new List<XServer>();
        foreach (var host in hosts)
          hostList.Add(new XServer(host.Trim()));

        FailoverManager.SetHostList(hostList, FailoverMethod.Sequential);
      }

      if (FailoverManager.FailoverGroup == null)
        return connectionString;

      return "server=" + FailoverManager.FailoverGroup.ActiveHost.Host + ";" + updatedConnectionString;
    }

    /// <summary>
    /// Initializes the <see cref="FailoverManager"/> whenever a list of hosts have been provided.
    /// </summary>
    /// <param name="hostsSubstring">Unparsed host list.</param>
    private void ParseHostList(string hostsSubstring)
    {
      if (string.IsNullOrWhiteSpace(hostsSubstring) || !hostsSubstring.StartsWith("[") || !hostsSubstring.EndsWith("]"))
        return;

      hostsSubstring = hostsSubstring.Substring(1, hostsSubstring.Length-2);
      string[] hostArray = hostsSubstring.Split(',');

      List<XServer> hostList = new List<XServer>();
      foreach (var host in hostArray)
      {
        int port = -1;
        string hostName = host;
        int colonIndex = host.LastIndexOf(":");
        if (colonIndex!=-1)
        {
          int.TryParse(host.Substring(colonIndex+1),out port);
          hostName = host.Substring(0,colonIndex);
        }

        hostList.Add(new XServer(hostName,port));
      }

      FailoverManager.SetHostList(hostList, FailoverMethod.Sequential);
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
