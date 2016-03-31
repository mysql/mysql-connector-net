// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Session;
using MySqlX.XDevAPI.Relational;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a base class for XSession and NodeSession
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
      this.connectionString = ParseConnectionStringFromUri(connectionString);
      if (this.connectionString.IndexOf("port", StringComparison.OrdinalIgnoreCase) == -1)
        this.connectionString += ";port=" + newDefaultPort;
      Settings = new MySqlConnectionStringBuilder(this.connectionString);
      _internalSession = InternalSession.GetSession(Settings);
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
          throw new KeyNotFoundException(string.Format(Properties.ResourcesX.InvalidConnectionStringAttribute, value.Key));
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
    public void Commit()
    {
      InternalSession.ExecuteSqlNonQuery("COMMIT");
    }

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    public void Rollback()
    {
      InternalSession.ExecuteSqlNonQuery("ROLLBACK");
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
    /// Implementation of the Dispose pattern.  Same as Close
    /// </summary>
    public void Dispose()
    {
      Close();
    }

    internal protected string ParseConnectionStringFromUri(string connectionstring)
    {
      if (connectionstring.StartsWith("mysqlx://") || connectionstring.StartsWith("//"))
      {
        string pattern = @"^(mysqlx:)?//(?<user>[^:]+)(:(?<password>.+))?@(?<server>[^:]+)(:\s*(?<port>\d+)\s*)?$";
        List<string> connectionParts = new List<string>();
        string newConnectionString = null;

        var matches = Regex.Matches(connectionstring, pattern, RegexOptions.ExplicitCapture);
        if (matches.Count != 1) throw new ArgumentException(Properties.ResourcesX.InvalidConnectionString);
        Match match = matches[0];
        if (match.Success)
        {
          if (match.Groups["user"].Success) connectionParts.Add("uid=" + match.Groups["user"].Value.Trim());
          if (match.Groups["password"].Success) connectionParts.Add("password=" + match.Groups["password"].Value.Trim());
          if (match.Groups["server"].Success) connectionParts.Add("server=" + match.Groups["server"].Value.Trim());
          connectionParts.Add("port=" + (match.Groups["port"].Success ? match.Groups["port"].Value.Trim() : newDefaultPort.ToString()));
          newConnectionString = string.Join(";", connectionParts);
        }

        return newConnectionString;
      }
      return connectionstring;
    }
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
