// Copyright © 2004, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
      Dispose(false);
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

    /// <summary>
    /// Creates a new Driver instance and opens the connection synchronously.
    /// </summary>
    /// <param name="settings">The connection string settings.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created and opened Driver instance.</returns>
    public static Driver Create(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
    {
      Driver d = InitializeDriver(settings);
      var connToken = PrepareCancellationToken(settings, cancellationToken);

      //this try was added as suggested fix submitted on MySql Bug 72025, socket connections are left in CLOSE_WAIT status when connector fails to open a new connection.
      //the bug is present when the client try to get more connections that the server support or has configured in the max_connections variable.
      try
      {
        d.Open(connToken);
      }
      catch
      {
        d.Close();
        throw;
      }
      return d;
    }

    /// <summary>
    /// Asynchronously creates a new Driver instance and opens the connection.
    /// </summary>
    /// <param name="settings">The connection string settings.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous creation. The task result contains the created and opened Driver instance.</returns>
    public static async Task<Driver> CreateAsync(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
    {
      Driver d = InitializeDriver(settings);
      var connToken = PrepareCancellationToken(settings, cancellationToken);

      //this try was added as suggested fix submitted on MySql Bug 72025, socket connections are left in CLOSE_WAIT status when connector fails to open a new connection.
      //the bug is present when the client try to get more connections that the server support or has configured in the max_connections variable.
      try
      {
        await d.OpenAsync(connToken).ConfigureAwait(false);
      }
      catch
      {
        await d.CloseAsync().ConfigureAwait(false);
        throw;
      }
      return d;
    }

    /// <summary>
    /// Initializes a new Driver instance, attempting to use TracingDriver if logging or usage advisor is enabled.
    /// </summary>
    /// <param name="settings">The connection string settings.</param>
    /// <returns>The initialized Driver instance.</returns>
    private static Driver InitializeDriver(MySqlConnectionStringBuilder settings)
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

      return d ?? new Driver(settings);
    }

    /// <summary>
    /// Prepares the cancellation token for connection operations, incorporating connection timeout if specified.
    /// </summary>
    /// <param name="settings">The connection string settings.</param>
    /// <param name="cancellationToken">The provided cancellation token.</param>
    /// <returns>The prepared CancellationToken, combining timeout and provided token if applicable.</returns>
    private static CancellationToken PrepareCancellationToken(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
    {
      CancellationTokenSource connTimeoutSource = null;
      CancellationTokenSource linkedSource = null;

      if (settings.ConnectionTimeout != 0)
        connTimeoutSource = new CancellationTokenSource((int)settings.ConnectionTimeout * 1000);

      if (cancellationToken.CanBeCanceled && connTimeoutSource is not null)
        linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connTimeoutSource.Token);

      return linkedSource?.Token ?? connTimeoutSource?.Token ?? cancellationToken;
    }

    public bool HasStatus(ServerStatusFlags flag)
    {
      return (handler.ServerStatus & flag) != 0;
    }

    /// <summary>
    /// Opens the connection synchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public virtual void Open(CancellationToken cancellationToken)
    {
      int count = 0;
      do
      {
        try
        {
          creationTime = DateTime.Now;
          handler.Open(cancellationToken);
          IsOpen = true;
          break;
        }
        catch (IOException)
        {
          if (count++ >= 5) throw;
        }
      } while (true);
    }

    /// <summary>
    /// Asynchronously opens the connection.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous open operation.</returns>
    public virtual async Task OpenAsync(CancellationToken cancellationToken)
    {
      int count = 0;
      do
      {
        try
        {
          creationTime = DateTime.Now;
          await handler.OpenAsync(cancellationToken).ConfigureAwait(false);
          IsOpen = true;
          break;
        }
        catch (IOException)
        {
          if (count++ >= 5) throw;
        }
      } while (true);
    }

    /// <summary>
    /// Closes the connection synchronously.
    /// </summary>
    public virtual void Close()
    {
      Dispose(true);
    }

    /// <summary>
    /// Asynchronously closes the connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    public virtual async Task CloseAsync()
    {
      await DisposeAsync(true).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the driver with server properties, character sets, and connection settings synchronously.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying server properties.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public virtual void Configure(MySqlConnection connection, CancellationToken cancellationToken)
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
              Pool.ServerProperties = LoadServerProperties(connection, cancellationToken);
            serverProps = Pool.ServerProperties;
          }
          else
            serverProps = LoadServerProperties(connection, cancellationToken);

          LoadCharacterSets(connection, cancellationToken);
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

      ExecuteCharacterSetCommands(connection, charSet, cancellationToken);
      ConfigureCharSetAndEncoding(charSet);
    }

    /// <summary>
    /// Asynchronously configures the driver with server properties, character sets, and connection settings.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying server properties.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous configuration operation.</returns>
    public virtual async Task ConfigureAsync(MySqlConnection connection, CancellationToken cancellationToken)
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
              Pool.ServerProperties = await LoadServerPropertiesAsync(connection, cancellationToken).ConfigureAwait(false);
            serverProps = Pool.ServerProperties;
          }
          else
            serverProps = await LoadServerPropertiesAsync(connection, cancellationToken).ConfigureAwait(false);

          await LoadCharacterSetsAsync(connection, cancellationToken).ConfigureAwait(false);
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

      await ExecuteCharacterSetCommandsAsync(connection, charSet, cancellationToken).ConfigureAwait(false);
      ConfigureCharSetAndEncoding(charSet);
    }

    /// <summary>
    /// Executes the necessary SQL commands to configure character set settings on the server synchronously.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for executing the commands.</param>
    /// <param name="charSet">The character set to configure.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private void ExecuteCharacterSetCommands(MySqlConnection connection, string charSet, CancellationToken cancellationToken)
    {
      MySqlCommand charSetCmd = new MySqlCommand("SET character_set_results=NULL", connection)
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
        setNamesCmd.ExecuteNonQuery(cancellationToken);
      }

      // sets character_set_results to null to return values in their original character set
      charSetCmd.ExecuteNonQuery(cancellationToken);
    }

    /// <summary>
    /// Asynchronously executes the necessary SQL commands to configure character set settings on the server.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for executing the commands.</param>
    /// <param name="charSet">The character set to configure.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous configuration operation.</returns>
    private async Task ExecuteCharacterSetCommandsAsync(MySqlConnection connection, string charSet, CancellationToken cancellationToken)
    {
      MySqlCommand charSetCmd = new MySqlCommand("SET character_set_results=NULL", connection)
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
        await setNamesCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
      }

      // sets character_set_results to null to return values in their original character set
      await charSetCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the maximum packet size from server properties, configures the encoding based on the character set, and configures the handler.
    /// </summary>
    /// <param name="charSet">The character set to use for encoding configuration.</param>
    private void ConfigureCharSetAndEncoding(string charSet)
    {
      if (serverProps.ContainsKey("max_allowed_packet"))
        MaxPacketSize = Convert.ToInt64(serverProps["max_allowed_packet"]);

      Encoding = CharSetMap.GetEncoding(charSet ?? "utf-8");
      handler.Configure();
    }

    /// <summary>
    /// Populates the server properties dictionary from a single row in the MySqlDataReader.
    /// </summary>
    /// <param name="reader">The MySqlDataReader containing the row data.</param>
    /// <param name="hash">The dictionary to populate with server properties.</param>
    private void PopulateHashFromRow(MySqlDataReader reader, Dictionary<string, string> hash)
    {
      for (int i = 0; i <= reader.FieldCount - 1; i++)
      {
        string key = reader.GetName(i).Remove(0, 2);
        string value = reader[i].ToString();
        hash[key] = value;
      }
    }

    /// <summary>
    /// Loads server properties from the connected server into a dictionary.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary containing the server properties.</returns>
    private Dictionary<string, string> LoadServerProperties(MySqlConnection connection, CancellationToken cancellationToken)
    {
      // load server properties
      Dictionary<string, string> hash = new Dictionary<string, string>();
      MySqlCommand cmd = new MySqlCommand(@"SELECT @@max_allowed_packet, @@character_set_client, 
        @@character_set_connection, @@license, @@sql_mode, @@lower_case_table_names, @@autocommit;", connection);
      try
      {
        using (MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken))
        {
          while (reader.Read(cancellationToken))
          {
            PopulateHashFromRow(reader, hash);
          }
        }
        // Get time zone offset as numerical value
        timeZoneOffset = GetTimeZoneOffset(connection, cancellationToken);
        return hash;
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(ThreadID, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously loads server properties from the connected server into a dictionary.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary with the server properties.</returns>
    private async Task<Dictionary<string, string>> LoadServerPropertiesAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
      // load server properties
      Dictionary<string, string> hash = new Dictionary<string, string>();
      MySqlCommand cmd = new MySqlCommand(@"SELECT @@max_allowed_packet, @@character_set_client, 
        @@character_set_connection, @@license, @@sql_mode, @@lower_case_table_names, @@autocommit;", connection);
      try
      {
        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false))
        {
          while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
          {
            PopulateHashFromRow(reader, hash);
          }
        }
        // Get time zone offset as numerical value
        timeZoneOffset = await GetTimeZoneOffsetAsync(connection, cancellationToken).ConfigureAwait(false);
        return hash;
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(ThreadID, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Gets the time zone offset synchronously.
    /// </summary>
    /// <param name="con">The MySqlConnection to use for querying.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The time zone offset in hours.</returns>
    private int GetTimeZoneOffset(MySqlConnection con, CancellationToken cancellationToken)
    {
      MySqlCommand cmd = new MySqlCommand("SELECT TIMEDIFF(NOW(), UTC_TIMESTAMP())", con);
      TimeSpan? timeZoneDiff = cmd.ExecuteScalar(cancellationToken) as TimeSpan?;
      string timeZoneString = "0:00";
      if (timeZoneDiff.HasValue)
        timeZoneString = timeZoneDiff.ToString();

      return int.Parse(timeZoneString.Substring(0, timeZoneString.IndexOf(':')), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Asynchronously gets the time zone offset.
    /// </summary>
    /// <param name="con">The MySqlConnection to use for querying.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the time zone offset in hours.</returns>
    private async Task<int> GetTimeZoneOffsetAsync(MySqlConnection con, CancellationToken cancellationToken)
    {
      MySqlCommand cmd = new MySqlCommand("SELECT TIMEDIFF(NOW(), UTC_TIMESTAMP())", con);
      TimeSpan? timeZoneDiff = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as TimeSpan?;
      string timeZoneString = "0:00";
      if (timeZoneDiff.HasValue)
        timeZoneString = timeZoneDiff.ToString();

      return int.Parse(timeZoneString.Substring(0, timeZoneString.IndexOf(':')), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Loads all current character set names and IDs for the server synchronously.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private void LoadCharacterSets(MySqlConnection connection, CancellationToken cancellationToken)
    {
      serverProps.TryGetValue("autocommit", out var serverAutocommit);
      MySqlCommand cmd = new MySqlCommand("SHOW COLLATION", connection);

      // now we load all the currently active collations
      try
      {
        using (MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken))
        {
          CharacterSets = new Dictionary<int, string>();
          while (reader.Read(cancellationToken))
          {
            CharacterSets[Convert.ToInt32(reader["id"], NumberFormatInfo.InvariantInfo)] =
              reader.GetString(reader.GetOrdinal("charset"));
          }
        }

        if (Convert.ToInt32(serverAutocommit) == 0 && Version.isAtLeast(8, 0, 0))
        {
          cmd = new MySqlCommand("commit", connection);
          cmd.ExecuteNonQuery(cancellationToken);
        }
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(ThreadID, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously loads all current character set names and IDs for the server.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task LoadCharacterSetsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
      serverProps.TryGetValue("autocommit", out var serverAutocommit);
      MySqlCommand cmd = new MySqlCommand("SHOW COLLATION", connection);

      // now we load all the currently active collations
      try
      {
        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false))
        {
          CharacterSets = new Dictionary<int, string>();
          while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
          {
            CharacterSets[Convert.ToInt32(reader["id"], NumberFormatInfo.InvariantInfo)] =
              reader.GetString(reader.GetOrdinal("charset"));
          }
        }

        if (Convert.ToInt32(serverAutocommit) == 0 && Version.isAtLeast(8, 0, 0))
        {
          cmd = new MySqlCommand("commit", connection);
          await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(ThreadID, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Reports warnings from the server synchronously.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying warnings.</param>
    /// <returns>A list of MySqlError objects representing the warnings.</returns>
    public virtual List<MySqlError> ReportWarnings(MySqlConnection connection)
    {
      List<MySqlError> warnings = new List<MySqlError>();

      MySqlCommand cmd = new MySqlCommand("SHOW WARNINGS", connection) { InternallyCreated = true };
      using (MySqlDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.Default, CancellationToken.None))
      {
        while (reader.Read())
          warnings.Add(new MySqlError(reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
      }

      MySqlInfoMessageEventArgs args = new MySqlInfoMessageEventArgs();
      args.errors = warnings.ToArray();
      connection?.OnInfoMessage(args);
      return warnings;
    }

    /// <summary>
    /// Asynchronously reports warnings from the server.
    /// </summary>
    /// <param name="connection">The MySqlConnection to use for querying warnings.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of MySqlError objects representing the warnings.</returns>
    public virtual async Task<List<MySqlError>> ReportWarningsAsync(MySqlConnection connection)
    {
      List<MySqlError> warnings = new List<MySqlError>();

      MySqlCommand cmd = new MySqlCommand("SHOW WARNINGS", connection) { InternallyCreated = true };
      using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, CancellationToken.None).ConfigureAwait(false))
      {
        while (await reader.ReadAsync().ConfigureAwait(false))
          warnings.Add(new MySqlError(reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
      }

      MySqlInfoMessageEventArgs args = new MySqlInfoMessageEventArgs();
      args.errors = warnings.ToArray();
      connection?.OnInfoMessage(args);
      return warnings;
    }

    /// <summary>
    /// Sends a query packet to the server synchronously.
    /// </summary>
    /// <param name="p">The MySqlPacket containing the query.</param>
    /// <param name="paramsPosition">The position of parameters in the packet.</param>
    public virtual void SendQuery(MySqlPacket p, int paramsPosition)
    {
      handler.SendQuery(p, paramsPosition);
      firstResult = true;
    }

    /// <summary>
    /// Asynchronously sends a query packet to the server.
    /// </summary>
    /// <param name="p">The MySqlPacket containing the query.</param>
    /// <param name="paramsPosition">The position of parameters in the packet.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public virtual async Task SendQueryAsync(MySqlPacket p, int paramsPosition)
    {
      await handler.SendQueryAsync(p, paramsPosition).ConfigureAwait(false);
      firstResult = true;
    }

    /// <summary>
    /// Gets the next result set synchronously.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="force">If true, forces retrieval of the next result.</param>
    /// <returns>The next ResultSet, or null if no more results.</returns>
    public virtual ResultSet NextResult(int statementId, bool force)
    {
      if (!force && !firstResult && !HasStatus(ServerStatusFlags.AnotherQuery | ServerStatusFlags.MoreResults))
        return null;

      firstResult = false;
      int affectedRows = -1;
      long insertedId = -1;
      var result = GetResult(statementId, affectedRows, insertedId);
      int fieldCount = result.Item1;
      if (fieldCount == -1)
        return null;

      if (fieldCount > 0)
        return ResultSet.CreateResultSet(this, statementId, fieldCount);
      else
        return new ResultSet(result.Item2, result.Item3);
    }

    /// <summary>
    /// Asynchronously gets the next result set.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="force">If true, forces retrieval of the next result.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the next ResultSet, or null if no more results.</returns>
    public virtual async Task<ResultSet> NextResultAsync(int statementId, bool force)
    {
      if (!force && !firstResult && !HasStatus(ServerStatusFlags.AnotherQuery | ServerStatusFlags.MoreResults))
        return null;

      firstResult = false;
      int affectedRows = -1;
      long insertedId = -1;
      var result = await GetResultAsync(statementId, affectedRows, insertedId).ConfigureAwait(false);
      int fieldCount = result.Item1;
      if (fieldCount == -1)
        return null;

      if (fieldCount > 0)
        return await ResultSet.CreateResultSetAsync(this, statementId, fieldCount).ConfigureAwait(false);
      else
        return new ResultSet(result.Item2, result.Item3);
    }

    /// <summary>
    /// Gets the result information synchronously.
    /// </summary>
    /// <param name="statementId">The statement ID.</param>
    /// <param name="affectedRows">The number of affected rows.</param>
    /// <param name="insertedId">The inserted ID.</param>
    /// <returns>A tuple containing field count, affected rows, and inserted ID.</returns>
    protected virtual Tuple<int, int, long> GetResult(int statementId, int affectedRows, long insertedId)
    {
      return handler.GetResult(affectedRows, insertedId);
    }

    /// <summary>
    /// Asynchronously gets the result information.
    /// </summary>
    /// <param name="statementId">The statement ID.</param>
    /// <param name="affectedRows">The number of affected rows.</param>
    /// <param name="insertedId">The inserted ID.</param>
    /// <returns>A task containing a tuple with field count, affected rows, and inserted ID.</returns>
    protected virtual async Task<Tuple<int, int, long>> GetResultAsync(int statementId, int affectedRows, long insertedId)
    {
      return await handler.GetResultAsync(affectedRows, insertedId).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches the next data row synchronously.
    /// </summary>
    /// <param name="statementId">The statement ID.</param>
    /// <param name="columns">The number of columns.</param>
    /// <returns>True if a row was fetched; false otherwise.</returns>
    public virtual bool FetchDataRow(int statementId, int columns)
    {
      return handler.FetchDataRow(statementId, columns);
    }

    /// <summary>
    /// Asynchronously fetches the next data row.
    /// </summary>
    /// <param name="statementId">The statement ID.</param>
    /// <param name="columns">The number of columns.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if a row was fetched; false otherwise.</returns>
    public virtual async Task<bool> FetchDataRowAsync(int statementId, int columns)
    {
      return await handler.FetchDataRowAsync(statementId, columns).ConfigureAwait(false);
    }

    /// <summary>
    /// Skips the current data row synchronously.
    /// </summary>
    /// <returns>True if a row was skipped; false otherwise.</returns>
    public virtual bool SkipDataRow()
    {
      return FetchDataRow(-1, 0);
    }

    /// <summary>
    /// Asynchronously skips the current data row.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is true if a row was skipped; false otherwise.</returns>
    public virtual async Task<bool> SkipDataRowAsync()
    {
      return await FetchDataRowAsync(-1, 0).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a SQL statement directly on the server without preparing or using parameters.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    public virtual void ExecuteDirect(string sql)
    {
      MySqlPacket p = new MySqlPacket(Encoding);
      p.WriteString(sql);
      SendQuery(p, 0);
      NextResult(0, false);
    }

    /// <summary>
    /// Asynchronously executes a SQL statement directly on the server without preparing or using parameters.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task ExecuteDirectAsync(string sql)
    {
      MySqlPacket p = new MySqlPacket(Encoding);
      await p.WriteStringAsync(sql).ConfigureAwait(false);
      await SendQueryAsync(p, 0).ConfigureAwait(false);
      await NextResultAsync(0, false).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the column metadata for the result set synchronously.
    /// </summary>
    /// <param name="count">The number of columns in the result set.</param>
    /// <returns>An array of MySqlField objects representing the column metadata.</returns>
    public MySqlField[] GetColumns(int count)
    {
      MySqlField[] fields = new MySqlField[count];
      for (int i = 0; i < count; i++)
        fields[i] = new MySqlField(this);
      handler.GetColumnsData(fields);

      return fields;
    }

    /// <summary>
    /// Asynchronously retrieves the column metadata for the result set.
    /// </summary>
    /// <param name="count">The number of columns in the result set.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is an array of MySqlField objects representing the column metadata.</returns>
    public async Task<MySqlField[]> GetColumnsAsync(int count)
    {
      MySqlField[] fields = new MySqlField[count];
      for (int i = 0; i < count; i++)
        fields[i] = new MySqlField(this);
      await handler.GetColumnsDataAsync(fields).ConfigureAwait(false);

      return fields;
    }

    /// <summary>
    /// Prepares a SQL statement for execution synchronously.
    /// </summary>
    /// <param name="sql">The SQL statement to prepare.</param>
    /// <returns>A tuple containing the statement ID and an array of MySqlField objects for output parameters, if any.</returns>
    public virtual Tuple<int, MySqlField[]> PrepareStatement(string sql)
    {
      return handler.PrepareStatement(sql);
    }

    /// <summary>
    /// Asynchronously prepares a SQL statement for execution.
    /// </summary>
    /// <param name="sql">The SQL statement to prepare.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a tuple containing the statement ID and an array of MySqlField objects for output parameters, if any.</returns>
    public virtual async Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql)
    {
      return await handler.PrepareStatementAsync(sql).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the value of a column from the current row synchronously.
    /// </summary>
    /// <param name="index">The index of the column.</param>
    /// <param name="field">The MySqlField describing the column.</param>
    /// <param name="value">The IMySqlValue object to populate with the column value.</param>
    /// <returns>The populated IMySqlValue containing the column data.</returns>
    public IMySqlValue ReadColumnValue(int index, MySqlField field, IMySqlValue value)
    {
      return handler.ReadColumnValue(index, field, value);
    }

    /// <summary>
    /// Asynchronously reads the value of a column from the current row.
    /// </summary>
    /// <param name="index">The index of the column.</param>
    /// <param name="field">The MySqlField describing the column.</param>
    /// <param name="value">The IMySqlValue object to populate with the column value.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the populated IMySqlValue containing the column data.</returns>
    public async Task<IMySqlValue> ReadColumnValueAsync(int index, MySqlField field, IMySqlValue value)
    {
      return await handler.ReadColumnValueAsync(index, field, value).ConfigureAwait(false);
    }

    public void SkipColumnValue(IMySqlValue valObject)
    {
      handler.SkipColumnValue(valObject);
    }

    public void ResetTimeout(int timeoutMilliseconds)
    {
      handler.ResetTimeout(timeoutMilliseconds);
    }

    /// <summary>
    /// Pings the MySQL server to verify the connection is still active synchronously.
    /// </summary>
    /// <returns>True if the server responds successfully; otherwise, false.</returns>
    public bool Ping()
    {
      return handler.Ping();
    }

    /// <summary>
    /// Asynchronously pings the MySQL server to verify the connection is still active.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the server responds successfully; otherwise, false.</returns>
    public async Task<bool> PingAsync()
    {
      return await handler.PingAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the current database for the connection synchronously.
    /// </summary>
    /// <param name="dbName">The name of the database to set.</param>
    public virtual void SetDatabase(string dbName)
    {
      handler.SetDatabase(dbName);
    }

    /// <summary>
    /// Asynchronously sets the current database for the connection.
    /// </summary>
    /// <param name="dbName">The name of the database to set.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task SetDatabaseAsync(string dbName)
    {
      await handler.SetDatabaseAsync(dbName).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a prepared statement synchronously.
    /// </summary>
    /// <param name="packetToExecute">The MySqlPacket containing the prepared statement to execute.</param>
    public virtual void ExecuteStatement(MySqlPacket packetToExecute)
    {
      handler.ExecuteStatement(packetToExecute);
    }

    /// <summary>
    /// Asynchronously executes a prepared statement.
    /// </summary>
    /// <param name="packetToExecute">The MySqlPacket containing the prepared statement to execute.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task ExecuteStatementAsync(MySqlPacket packetToExecute)
    {
      await handler.ExecuteStatementAsync(packetToExecute).ConfigureAwait(false);
    }

    /// <summary>
    /// Closes a prepared statement synchronously.
    /// </summary>
    /// <param name="id">The ID of the statement to close.</param>
    public virtual void CloseStatement(int id)
    {
      handler.CloseStatement(id);
    }

    /// <summary>
    /// Asynchronously closes a prepared statement.
    /// </summary>
    /// <param name="id">The ID of the statement to close.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task CloseStatementAsync(int id)
    {
      await handler.CloseStatementAsync(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Resets the connection state synchronously.
    /// </summary>
    public virtual void Reset()
    {
      handler.Reset();
    }

    /// <summary>
    /// Asynchronously resets the connection state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task ResetAsync()
    {
      await handler.ResetAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Closes the query synchronously by reporting any server warnings if present.
    /// </summary>
    /// <param name="connection">The MySqlConnection associated with the query.</param>
    /// <param name="statementId">The ID of the statement.</param>
    public virtual void CloseQuery(MySqlConnection connection, int statementId)
    {
      if (handler.WarningCount > 0)
        ReportWarnings(connection);
    }

    /// <summary>
    /// Asynchronously closes the query by reporting any server warnings if present.
    /// </summary>
    /// <param name="connection">The MySqlConnection associated with the query.</param>
    /// <param name="statementId">The ID of the statement.</param>
    /// <returns>A task that represents the asynchronous close query operation.</returns>
    public virtual async Task CloseQueryAsync(MySqlConnection connection, int statementId)
    {
      if (handler.WarningCount > 0)
        await ReportWarningsAsync(connection).ConfigureAwait(false);
    }

    #region IDisposable Members

    protected virtual void Dispose(bool disposing)
    {
      if (disposed)
        return;

      // Avoid cyclic calls to Dispose.
      try
      {
        ResetTimeout(1000);
        handler.Close(IsOpen);
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

    protected virtual async Task DisposeAsync(bool disposing)
    {
      if (disposed)
        return;

      // Avoid cyclic calls to Dispose.
      try
      {
        ResetTimeout(1000);
        await handler.CloseAsync(IsOpen).ConfigureAwait(false);
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
      Dispose(true);
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

    void Open(CancellationToken cancellationToken);
    Task OpenAsync(CancellationToken cancellationToken);
    void SendQuery(MySqlPacket packet, int paramsPosition = 0);
    Task SendQueryAsync(MySqlPacket packet, int paramsPosition = 0);
    void Close(bool isOpen);
    Task CloseAsync(bool isOpen);
    bool Ping();
    Task<bool> PingAsync();
    Tuple<int, int, long> GetResult(int affectedRows, long insertedId);
    Task<Tuple<int, int, long>> GetResultAsync(int affectedRows, long insertedId);
    bool FetchDataRow(int statementId, int columns);
    Task<bool> FetchDataRowAsync(int statementId, int columns);
    Tuple<int, MySqlField[]> PrepareStatement(string sql);
    Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql);
    void ExecuteStatement(MySqlPacket packet);
    Task ExecuteStatementAsync(MySqlPacket packet);
    void CloseStatement(int statementId);
    Task CloseStatementAsync(int statementId);
    void SetDatabase(string dbName);
    Task SetDatabaseAsync(string dbName);
    void Reset();
    Task ResetAsync();
    IMySqlValue ReadColumnValue(int index, MySqlField field, IMySqlValue valObject);
    Task<IMySqlValue> ReadColumnValueAsync(int index, MySqlField field, IMySqlValue valObject);
    void SkipColumnValue(IMySqlValue valueObject);
    void GetColumnsData(MySqlField[] columns);
    Task GetColumnsDataAsync(MySqlField[] columns);
    void ResetTimeout(int timeout);
    int WarningCount { get; }
  }
}