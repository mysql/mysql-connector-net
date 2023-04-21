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
using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for BaseDriver.
  /// </summary>
  internal class Driver : IDisposable
  {
    protected Encoding encoding;
    protected MySqlConnectionStringBuilder ConnectionString;
    protected DateTime creationTime;
    protected string serverCharSet;
    protected Dictionary<string, string> serverProps;
    internal int timeZoneOffset;
    private bool firstResult;
    protected IDriver handler;
    internal MySqlDataReader reader;
    private bool disposed;

    /// <summary>
    /// For pooled connections, time when the driver was
    /// put into idle queue
    /// </summary>
    public DateTime IdleSince { get; set; }

    public Driver(MySqlConnectionStringBuilder settings)
    {
      encoding = Encoding.GetEncoding("UTF-8");
      if (encoding == null)
        throw new MySqlException(Resources.DefaultEncodingNotFound);
      ConnectionString = settings;
      serverCharSet = "utf8";
      ConnectionCharSetIndex = -1;
      MaxPacketSize = 1024;
      handler = new NativeDriver(this);
    }

    ~Driver()
    {
      DisposeAsync(false, false).GetAwaiter().GetResult();
    }

    #region Properties

    public int ThreadID => handler.ThreadId;

    public DBVersion Version => handler.Version;

    public MySqlConnectionStringBuilder Settings
    {
      get { return ConnectionString; }
      set { ConnectionString = value; }
    }
    public Encoding Encoding
    {
      get { return encoding; }
      set { encoding = value; }
    }

    public MySqlPromotableTransaction currentTransaction { get; set; }

    public bool IsInActiveUse { get; set; }

    public bool IsOpen { get; protected set; }

    public MySqlPool Pool { get; set; }

    public long MaxPacketSize { get; protected set; }

    protected internal int ConnectionCharSetIndex { get; set; }

    protected internal Dictionary<int, string> CharacterSets { get; protected set; }

    public bool SupportsOutputParameters => Version.isAtLeast(5, 5, 0);

    public bool SupportsBatch => (handler.Flags & ClientFlags.MULTI_STATEMENTS) != 0;

    public bool SupportsConnectAttrs => (handler.Flags & ClientFlags.CONNECT_ATTRS) != 0;

    public bool SupportsPasswordExpiration => (handler.Flags & ClientFlags.CAN_HANDLE_EXPIRED_PASSWORD) != 0;

    public bool SupportsQueryAttributes => (handler.Flags & ClientFlags.CLIENT_QUERY_ATTRIBUTES) != 0;

    public bool IsPasswordExpired { get; internal set; }

    public Stream BulkLoaderStream { get; set; }
    #endregion

    public string Property(string key)
    {
      return serverProps[key];
    }

    public bool ConnectionLifetimeExpired()
    {
      TimeSpan ts = DateTime.Now.Subtract(creationTime);
      return Settings.ConnectionLifeTime != 0 &&
             ts.TotalSeconds > Settings.ConnectionLifeTime;
    }

    public static async Task<Driver> CreateAsync(MySqlConnectionStringBuilder settings, bool execAsync, CancellationToken cancellationToken)
    {
      Driver d = null;

      try
      {
        if (settings.Logging || settings.UseUsageAdvisor)
          d = new TracingDriver(settings);
      }
      catch (TypeInitializationException ex)
      {
        if (ex.InnerException is not SecurityException)
          throw;
        //Only rethrow if InnerException is not a SecurityException. If it is a SecurityException then 
        //we couldn't initialize MySqlTrace because we don't have unmanaged code permissions. 
      }

      d ??= new Driver(settings);

      CancellationTokenSource connTimeoutSource = null;
      CancellationTokenSource linkedSource = null;

      if (settings.ConnectionTimeout != 0)
        connTimeoutSource = new CancellationTokenSource((int)settings.ConnectionTimeout * 1000);

      if (cancellationToken.CanBeCanceled && connTimeoutSource is not null)
        linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connTimeoutSource.Token);

      var connToken = linkedSource?.Token ?? connTimeoutSource?.Token ?? cancellationToken;

      //this try was added as suggested fix submitted on MySql Bug 72025, socket connections are left in CLOSE_WAIT status when connector fails to open a new connection.
      //the bug is present when the client try to get more connections that the server support or has configured in the max_connections variable.
      try
      {
        await d.OpenAsync(execAsync, connToken).ConfigureAwait(false);
      }
      catch
      {
        await d.CloseAsync(execAsync).ConfigureAwait(false);
        throw;
      }
      return d;
    }

    public bool HasStatus(ServerStatusFlags flag)
    {
      return (handler.ServerStatus & flag) != 0;
    }

    public virtual async Task OpenAsync(bool execAsync, CancellationToken cancellationToken)
    {
      int count = 0;
      do
      {
        try
        {
          creationTime = DateTime.Now;
          await handler.OpenAsync(execAsync, cancellationToken).ConfigureAwait(false);
          IsOpen = true;
          break;
        }
        catch (IOException)
        {
          if (count++ >= 5) throw;
        }
      } while (true);
    }

    public virtual async Task CloseAsync(bool execAsync)
    {
      await DisposeAsync(true, execAsync).ConfigureAwait(false);
    }

    public virtual async Task ConfigureAsync(MySqlConnection connection, bool execAsync, CancellationToken cancellationToken)
    {
      bool firstConfigure = false;

      // if we have not already configured our server variables
      // then do so now
      if (serverProps == null)
      {
        firstConfigure = true;

        // if we are in a pool and the user has said it's ok to cache the
        // properties, then grab it from the pool
        try
        {
          if (Pool != null && Settings.CacheServerProperties)
          {
            if (Pool.ServerProperties == null)
              Pool.ServerProperties = await LoadServerPropertiesAsync(connection, execAsync, cancellationToken).ConfigureAwait(false);
            serverProps = Pool.ServerProperties;
          }
          else
            serverProps = await LoadServerPropertiesAsync(connection, execAsync, cancellationToken).ConfigureAwait(false);

          await LoadCharacterSetsAsync(connection, execAsync, cancellationToken).ConfigureAwait(false);
        }
        catch (MySqlException ex)
        {
          // expired password capability
          if (ex.Number == 1820)
          {
            IsPasswordExpired = true;
            return;
          }
          throw;
        }
      }

      // if the user has indicated that we are not to reset
      // the connection and this is not our first time through,
      // then we are done.
      if (!Settings.ConnectionReset && !firstConfigure) return;

      string charSet = ConnectionString.CharacterSet;
      if (string.IsNullOrEmpty(charSet))
      {
        if (ConnectionCharSetIndex >= 0 && CharacterSets.ContainsKey(ConnectionCharSetIndex))
          charSet = CharacterSets[ConnectionCharSetIndex];
        else
          charSet = serverCharSet;
      }

      if (serverProps.ContainsKey("max_allowed_packet"))
        MaxPacketSize = Convert.ToInt64(serverProps["max_allowed_packet"]);

      // now tell the server which character set we will send queries in and which charset we
      // want results in
      MySqlCommand charSetCmd = new MySqlCommand("SET character_set_results=NULL",
        connection)
      { InternallyCreated = true };

      string clientCharSet;
      serverProps.TryGetValue("character_set_client", out clientCharSet);
      string connCharSet;
      serverProps.TryGetValue("character_set_connection", out connCharSet);
      if ((clientCharSet != null && clientCharSet.ToString() != charSet) ||
        (connCharSet != null && connCharSet.ToString() != charSet))
      {
        using MySqlCommand setNamesCmd = new MySqlCommand("SET NAMES " + charSet, connection);
        setNamesCmd.InternallyCreated = true;
        await setNamesCmd.ExecuteNonQueryAsync(execAsync, cancellationToken).ConfigureAwait(false);
      }
      // sets character_set_results to null to return values in their original character set
      await charSetCmd.ExecuteNonQueryAsync(execAsync, cancellationToken).ConfigureAwait(false);

      Encoding = CharSetMap.GetEncoding(charSet ?? "utf-8");

      handler.Configure();
    }

    /// <summary>
    /// Loads the properties from the connected server into a hashtable
    /// </summary>
    /// <param name="connection">The connection to be used.</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    private async Task<Dictionary<string, string>> LoadServerPropertiesAsync(MySqlConnection connection, bool execAsync, CancellationToken cancellationToken)
    {
      // load server properties
      Dictionary<string, string> hash = new Dictionary<string, string>();
      MySqlCommand cmd = new MySqlCommand(@"SELECT @@max_allowed_packet, @@character_set_client, 
        @@character_set_connection, @@license, @@sql_mode, @@lower_case_table_names, @@autocommit;", connection);
      try
      {
        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, execAsync, cancellationToken).ConfigureAwait(false))
        {
          while (await reader.ReadAsync(execAsync, cancellationToken).ConfigureAwait(false))
          {
            for (int i = 0; i <= reader.FieldCount - 1; i++)
            {
              string key = reader.GetName(i).Remove(0, 2);
              string value = reader[i].ToString();
              hash[key] = value;
            }
          }
        }
        // Get time zone offset as numerical value
        timeZoneOffset = await GetTimeZoneOffsetAsync(connection, execAsync, cancellationToken).ConfigureAwait(false);
        return hash;
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(ThreadID, ex.Message);
        throw;
      }
    }

    private async Task<int> GetTimeZoneOffsetAsync(MySqlConnection con, bool execAsync, CancellationToken cancellationToken)
    {
      MySqlCommand cmd = new MySqlCommand("SELECT TIMEDIFF(NOW(), UTC_TIMESTAMP())", con);
      TimeSpan? timeZoneDiff = await cmd.ExecuteScalarAsync(execAsync, cancellationToken).ConfigureAwait(false) as TimeSpan?;
      string timeZoneString = "0:00";
      if (timeZoneDiff.HasValue)
        timeZoneString = timeZoneDiff.ToString();

      return int.Parse(timeZoneString.Substring(0, timeZoneString.IndexOf(':')), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Loads all the current character set names and ids for this server 
    /// into the charSets hashtable
    /// </summary>
    private async Task LoadCharacterSetsAsync(MySqlConnection connection, bool execAsync, CancellationToken cancellationToken)
    {
      serverProps.TryGetValue("autocommit", out var serverAutocommit);
      MySqlCommand cmd = new MySqlCommand("SHOW COLLATION WHERE ID IS NOT NULL", connection);

      // now we load all the currently active collations
      try
      {
        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, execAsync, cancellationToken).ConfigureAwait(false))
        {
          CharacterSets = new Dictionary<int, string>();
          while (await reader.ReadAsync(execAsync, cancellationToken).ConfigureAwait(false))
          {
            CharacterSets[Convert.ToInt32(reader["id"], NumberFormatInfo.InvariantInfo)] =
              reader.GetString(reader.GetOrdinal("charset"));
          }
        }

        if (Convert.ToInt32(serverAutocommit) == 0 && Version.isAtLeast(8, 0, 0))
        {
          cmd = new MySqlCommand("commit", connection);
          await cmd.ExecuteNonQueryAsync(execAsync, cancellationToken).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(ThreadID, ex.Message);
        throw;
      }
    }

    public virtual async Task<List<MySqlError>> ReportWarningsAsync(MySqlConnection connection, bool execAsync)
    {
      List<MySqlError> warnings = new List<MySqlError>();

      MySqlCommand cmd = new MySqlCommand("SHOW WARNINGS", connection) { InternallyCreated = true };
      using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, execAsync, CancellationToken.None).ConfigureAwait(false))
      {
        while (await reader.ReadAsync(execAsync).ConfigureAwait(false))
          warnings.Add(new MySqlError(reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
      }

      MySqlInfoMessageEventArgs args = new MySqlInfoMessageEventArgs();
      args.errors = warnings.ToArray();
      connection?.OnInfoMessage(args);
      return warnings;
    }

    public virtual async Task SendQueryAsync(MySqlPacket p, int paramsPosition, bool execAsync)
    {
      await handler.SendQueryAsync(p, execAsync, paramsPosition).ConfigureAwait(false);
      firstResult = true;
    }

    public virtual async Task<ResultSet> NextResultAsync(int statementId, bool force, bool execAsync)
    {
      if (!force && !firstResult && !HasStatus(ServerStatusFlags.AnotherQuery | ServerStatusFlags.MoreResults))
        return null;
      firstResult = false;

      int affectedRows = -1;
      long insertedId = -1;
      var result = await GetResultAsync(statementId, affectedRows, insertedId, execAsync).ConfigureAwait(false);
      int fieldCount = result.Item1;
      if (fieldCount == -1)
        return null;
      if (fieldCount > 0)
        return await ResultSet.CreateResultSetAsync(this, statementId, fieldCount, execAsync).ConfigureAwait(false);
      else
        return new ResultSet(result.Item2, result.Item3);
    }

    protected virtual async Task<Tuple<int, int, long>> GetResultAsync(int statementId, int affectedRows, long insertedId, bool execAsync)
    {
      return await handler.GetResultAsync(affectedRows, insertedId, execAsync).ConfigureAwait(false);
    }

    public virtual async Task<bool> FetchDataRowAsync(int statementId, int columns, bool execAsync)
    {
      return await handler.FetchDataRowAsync(statementId, columns, execAsync).ConfigureAwait(false);
    }

    public virtual async Task<bool> SkipDataRowAsync(bool execAsync)
    {
      return await FetchDataRowAsync(-1, 0, execAsync).ConfigureAwait(false);
    }

    public virtual async Task ExecuteDirectAsync(string sql, bool execAsync)
    {
      MySqlPacket p = new MySqlPacket(Encoding);
      await p.WriteStringAsync(sql, execAsync).ConfigureAwait(false);
      await SendQueryAsync(p, 0, execAsync).ConfigureAwait(false);
      await NextResultAsync(0, false, execAsync).ConfigureAwait(false);
    }

    public async Task<MySqlField[]> GetColumnsAsync(int count, bool execAsync)
    {
      MySqlField[] fields = new MySqlField[count];
      for (int i = 0; i < count; i++)
        fields[i] = new MySqlField(this);
      await handler.GetColumnsDataAsync(fields, execAsync).ConfigureAwait(false);

      return fields;
    }

    public virtual async Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql, bool execAsync)
    {
      return await handler.PrepareStatementAsync(sql, execAsync).ConfigureAwait(false);
    }

    public async Task<IMySqlValue> ReadColumnValueAsync(int index, MySqlField field, IMySqlValue value, bool execAsync)
    {
      return await handler.ReadColumnValueAsync(index, field, value, execAsync).ConfigureAwait(false);
    }

    public void SkipColumnValue(IMySqlValue valObject)
    {
      handler.SkipColumnValue(valObject);
    }

    public void ResetTimeout(int timeoutMilliseconds)
    {
      handler.ResetTimeout(timeoutMilliseconds);
    }

    public async Task<bool> PingAsync(bool execAsync)
    {
      return await handler.PingAsync(execAsync).ConfigureAwait(false);
    }

    public virtual async Task SetDatabaseAsync(string dbName, bool execAsync)
    {
      await handler.SetDatabaseAsync(dbName, execAsync).ConfigureAwait(false);
    }

    public virtual async Task ExecuteStatementAsync(MySqlPacket packetToExecute, bool execAsync)
    {
      await handler.ExecuteStatementAsync(packetToExecute, execAsync).ConfigureAwait(false);
    }


    public virtual async Task CloseStatementAsync(int id, bool execAsync)
    {
      await handler.CloseStatementAsync(id, execAsync).ConfigureAwait(false);
    }

    public virtual async Task ResetAsync(bool execAsync)
    {
      await handler.ResetAsync(execAsync).ConfigureAwait(false);
    }

    public virtual async Task CloseQueryAsync(MySqlConnection connection, int statementId, bool execAsync)
    {
      if (handler.WarningCount > 0)
        await ReportWarningsAsync(connection, execAsync).ConfigureAwait(false);
    }

    #region IDisposable Members

    protected virtual async Task DisposeAsync(bool disposing, bool execAsync)
    {
      if (disposed)
        return;

      // Avoid cyclic calls to Dispose.
      try
      {
        ResetTimeout(1000);
        await handler.CloseAsync(IsOpen, execAsync).ConfigureAwait(false);
        // if we are pooling, then release ourselves
        if (ConnectionString.Pooling)
          MySqlPoolManager.RemoveConnection(this);
      }
      catch (Exception ex)
      {
        if (disposing)
        {
          MySqlException mysqlEx = ex as MySqlException;
          if (mysqlEx == null)
            MySqlTrace.LogError(0, ex.GetBaseException().Message);
          else
            MySqlTrace.LogError(mysqlEx.Number, ex.GetBaseException().Message);
        }
      }
      finally
      {
        disposed = true;
        reader = null;
        IsOpen = false;
      }
    }

    public void Dispose()
    {
      DisposeAsync(true, false).GetAwaiter().GetResult();
      GC.SuppressFinalize(this);
    }

    #endregion
  }

  internal interface IDriver
  {
    int ThreadId { get; }
    DBVersion Version { get; }
    ServerStatusFlags ServerStatus { get; }
    ClientFlags Flags { get; }
    void Configure();
    Task OpenAsync(bool execAsync, CancellationToken cancellationToken);
    Task SendQueryAsync(MySqlPacket packet, bool execAsync, int paramsPosition = 0);
    Task CloseAsync(bool isOpen, bool execAsync);
    Task<bool> PingAsync(bool execAsync);
    Task<Tuple<int, int, long>> GetResultAsync(int affectedRows, long insertedId, bool execAsync);
    Task<bool> FetchDataRowAsync(int statementId, int columns, bool execAsync);
    Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql, bool execAsync);
    Task ExecuteStatementAsync(MySqlPacket packet, bool execAsync);
    Task CloseStatementAsync(int statementId, bool execAsync);
    Task SetDatabaseAsync(string dbName, bool execAsync);
    Task ResetAsync(bool execAsync);
    Task<IMySqlValue> ReadColumnValueAsync(int index, MySqlField field, IMySqlValue valObject, bool execAsync);
    void SkipColumnValue(IMySqlValue valueObject);
    Task GetColumnsDataAsync(MySqlField[] columns, bool execAsync);
    void ResetTimeout(int timeout);
    int WarningCount { get; }
  }
}
