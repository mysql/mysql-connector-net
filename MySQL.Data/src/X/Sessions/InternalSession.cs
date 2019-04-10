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
using MySqlX.Protocol;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using System;
using System.IO;

namespace MySqlX.Sessions
{

  /// <summary>
  /// Abstract class to manage and encapsulate one or more actual connections.
  /// </summary>  
  internal abstract class InternalSession : IDisposable
  {
    protected Stream _stream;
    internal BaseResult ActiveResult;
    private bool disposed = false;

    /// <summary>
    /// Creates a new session object with the values of the settings parameter.
    /// </summary>
    /// <param name="settings">Settings to be used in the session object</param>
    public InternalSession(MySqlXConnectionStringBuilder settings)
    {
      Settings = settings;
    }

    protected abstract void Open();

    public abstract void Close();

    internal abstract ProtocolBase GetProtocol();

    protected internal MySqlXConnectionStringBuilder Settings;

    public SessionState SessionState { get; protected set; }

    public static InternalSession GetSession(MySqlXConnectionStringBuilder settings)
    {
      InternalSession session = new XInternalSession(settings);
      int count = 0;
      do
      {
        try
        {
          session.Open();
          SetDefaultCollation(session, settings.CharacterSet);
          break;
        }
        catch (IOException ex)
        {
          // Retry SSL connection (manual fallback).
          if (count++ >= 5)
            throw new MySqlException(ResourcesX.UnableToOpenSession, ex);
        }
      } while (true);
      return session;
    }

    public Result ExecuteSqlNonQuery(string sql, params object[] args)
    {
      GetProtocol().SendSQL(sql, args);
      return new Result(this);
    }

    public RowResult GetSqlRowResult(string sql)
    {
      GetProtocol().SendSQL(sql);
      return new RowResult(this);
    }

    public SqlResult GetSQLResult(string sql, object[] args)
    {
      GetProtocol().SendSQL(sql, args);
      return new SqlResult(this);
    }

    public object ExecuteQueryAsScalar(string sql)
    {
      RowResult result = GetSqlRowResult(sql);
      var rows = result.FetchAll();
      if (rows.Count == 0)
        throw new MySqlException("No data found");
      return rows[0][0];
    }

    /// <summary>
    /// Sets the connection's charset default collation.
    /// </summary>
    /// <param name="session">The opened session.</param>
    /// <param name="charset">The character set.</param>
    private static void SetDefaultCollation(InternalSession session, string charset)
    {
      if (!session.GetServerVersion().isAtLeast(8, 0, 1)) return;

      session.GetSqlRowResult("SHOW CHARSET WHERE Charset='" + charset + "'");
      RowResult result = session.GetSqlRowResult("SHOW CHARSET WHERE Charset='" + charset + "'");
      var row = result.FetchOne();
      if (row != null)
      {
        var defaultCollation = row.GetString("Default collation");
        session.ExecuteSqlNonQuery("SET collation_connection = '" + defaultCollation + "'");
      }
      else
        session.ExecuteSqlNonQuery("SET collation_connection = 'utf8mb4_0900_ai_ci'");
    }

    /// <summary>
    /// Gets the version of the server.
    /// </summary>
    /// <returns>An instance of <see cref="DBVersion"/> containing the server version.</returns>
    internal DBVersion GetServerVersion()
    {
      return DBVersion.Parse(GetSqlRowResult("SHOW VARIABLES LIKE 'version'").FetchOne().GetString("Value"));
    }

    /// <summary>
    /// Gets the thread Id of the connection.
    /// </summary>
    /// <returns>Thread Id</returns>
    internal int GetThreadId()
    {
      return int.Parse(GetSqlRowResult("SELECT CONNECTION_ID()").FetchOne().GetString("connection_id()"));
    }

    #region IDisposable

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposed) return;

      if (disposing)
      {
        // Free any other managed objects here. 
        //
      }

      // Free any unmanaged objects here. 
      //
      disposed = true;
    }

    //~BaseSession()
    //{
    //  Dispose(false);
    //}

    #endregion
  }
}
