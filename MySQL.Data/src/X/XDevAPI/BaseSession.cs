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
  /// Represents a base class for a Session
  /// </summary>
  public abstract class BaseSession : IDisposable
  {
    private InternalSession _internalSession;
    private string connectionString;
    private bool disposed = false;
    private const uint newDefaultPort = 33060;
    internal QueueTaskScheduler scheduler = new QueueTaskScheduler();

    /// <summary>
    /// Connection settings for this session
    /// </summary>
    public MySqlConnectionStringBuilder Settings { get; private set; }

    /// <summary>
    /// Currently active schema
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
    /// Constructor
    /// </summary>
    /// <param name="connectionString">Session connection string</param>
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
    /// Constructor
    /// </summary>
    /// <param name="connectionData">Session data as anonymous type</param>
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
    /// Drop the database/schema with the given name
    /// </summary>
    /// <param name="schema">Name of the schema</param>
    public void DropSchema(string schema)
    {
      InternalSession.ExecuteSqlNonQuery("DROP DATABASE `" + schema + "`");
    }

    /// <summary>
    /// Create a schema/database with the given name
    /// </summary>
    /// <param name="schema">Name of the schema/database</param>
    /// <returns>Schema object</returns>
    public Schema CreateSchema(string schema)
    {
      InternalSession.ExecuteSqlNonQuery("CREATE DATABASE `" + schema + "`");
      return new Schema(this, schema);
    }

    /// <summary>
    /// Gets the schema with the given name
    /// </summary>
    /// <param name="schema">Name of the schema</param>
    /// <returns>Schema object</returns>
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
    /// Get a list of schemas/databases in this session
    /// </summary>
    /// <returns>List<Schema></returns>
    public List<Schema> GetSchemas()
    {
      RowResult result = XSession.GetSqlRowResult("select * from information_schema.schemata");
      result.FetchAll();
      var query = from row in result.Rows
                  select new Schema(this, row.GetString("schema_name"));
      return query.ToList<Schema>();
    }

    /// <summary>
    /// Start a new transaction
    /// </summary>
    public void StartTransaction()
    {
      InternalSession.ExecuteSqlNonQuery("START TRANSACTION");
    }

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    public Result Commit()
    {
      return InternalSession.ExecuteSqlNonQuery("COMMIT");
    }

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    public Result Rollback()
    {
      return InternalSession.ExecuteSqlNonQuery("ROLLBACK");
    }

    /// <summary>
    /// Close this session
    /// </summary>
    public void Close()
    {
      if (XSession.SessionState != SessionState.Closed)
      {
        XSession.Close();
      }
    }

    /// <summary>
    /// Parses the connection string depending on its format.
    /// </summary>
    /// <param name="connectiontring">Connection string in basic or URI format.</param>
    /// <returns>Parsed Connection String.</returns>
    internal protected string ParseConnectionString(string connectionString)
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
    /// Initializes the <see cref="FailoverManager"/> if more than one host is found.
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
    /// This code added to correctly implement the disposable pattern.
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
  /// Session state
  /// </summary>
  public enum SessionState
  {
    // Summary:
    //     The session is closed.
    Closed = 0,
    //
    // Summary:
    //     The session is open.
    Open = 1,
    //
    // Summary:
    //     The session object is connecting to the data source.
    Connecting = 2,
    //
    // Summary:
    //     The session object is executing a command. 
    Executing = 4,
  }
}
