// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
using MySQL.Common;
using MySql.Data;
using MySql.Session;
using MySql.XDevAPI.Relational;

namespace MySql.XDevAPI
{
  /// <summary>
  /// Represents a base class for XSession and NodeSession
  /// </summary>
  public abstract class BaseSession : IDisposable
  {
    private InternalSession _internalSession;
    private string connectionString;
    private bool disposed = false;
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
      this.connectionString = connectionString;
      Settings = new MySqlConnectionStringBuilder(connectionString);
      _internalSession = InternalSession.GetSession(Settings);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connectionData">Session data as anonymous type</param>
    public BaseSession(object connectionData)
    {
      if (!connectionData.GetType().IsGenericType)
        throw new MySqlException("Connection Data format is incorrect.");

      var values = Tools.GetDictionaryFromAnonymous(connectionData);
      foreach (var value in values)
      {
        if (!Settings.ContainsKey(value.Key))
          throw new MySqlException(string.Format("Attribute '{0}' is not defined in the connection", value.Key));
        Settings.SetValue(value.Key, value.Value);
      }
      this.connectionString = Settings.ToString();
      _internalSession = InternalSession.GetSession(Settings);
    }

    /// <summary>
    /// Drop the database/schema with the given name
    /// </summary>
    /// <param name="schema">Name of the schema</param>
    public void DropSchema(string schema)
    {
      InternalSession.ExecuteSqlNonQuery("DROP DATABASE `" + schema + "`", true, null);
    }

    /// <summary>
    /// Create a schema/database with the given name
    /// </summary>
    /// <param name="schema">Name of the schema/database</param>
    /// <returns>Schema object</returns>
    public Schema CreateSchema(string schema)
    {
      InternalSession.ExecuteSqlNonQuery("CREATE DATABASE `" + schema + "`", true, null);
      return new Schema(this, schema);
    }

    /// <summary>
    /// Gets the schema with the given name
    /// </summary>
    /// <param name="schema">Name of the schema</param>
    /// <returns>Schema object</returns>
    public Schema GetSchema(string schema)
    {
      InternalSession.SetSchema(schema);
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
      TableResult result = XSession.ExecuteQuery("select * from information_schema.schemata");
      result.Buffer();

      var query = from row in result.Rows
                  select new Schema(this, row.GetString("schema_name"));
      return query.ToList<Schema>();
    }

    //public Type GetTopologyType()
    //{
    //  throw new NotImplementedException();
    //}

    //public List<Nodes> GetSlaveNodes()
    //{
    //  throw new NotImplementedException();
    //}

    /// <summary>
    /// Start a new transaction
    /// </summary>
    public void StartTransaction()
    {
      InternalSession.ExecuteSqlNonQuery("START TRANSACTION", true, null);
    }

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    public void Commit()
    {
      InternalSession.ExecuteSqlNonQuery("COMMIT", true, null);
    }

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    public void Rollback()
    {
      InternalSession.ExecuteSqlNonQuery("ROLLBACK", true, null);
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
