// Copyright (c) 2004, 2023, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an
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
using MySql.Data.Failover;
using MySql.Data.MySqlClient.Interceptors;
using MySql.Data.MySqlClient.Replication;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;
#if NET452
using System.Drawing;
using System.Drawing.Design;
#endif

namespace MySql.Data.MySqlClient
{
  /// <summary>
  ///  Represents a connection to a MySQL database. This class cannot be inherited.
  /// </summary>
  /// <remarks>
  ///  <para>
  ///    A <see cref="MySqlConnection"/> object represents a session to a MySQL 
  ///    data source. When you create an instance of <see cref="MySqlConnection"/>, all
  ///    properties are set to their initial values.
  ///  </para>
  ///  <para>
  ///    If the <see cref="MySqlConnection"/> goes out of scope, it is not closed. Therefore,
  ///    you must explicitly close the connection by calling <see cref="MySqlConnection.Close"/>
  ///    or <see cref="MySqlConnection.Dispose()"/>.
  ///  </para>
  /// </remarks>
#if NET452
  [ToolboxBitmap(typeof(MySqlConnection), "MySqlClient.resources.connection.bmp")]
#endif
  [DesignerCategory("Code")]
  [ToolboxItem(true)]
  public sealed class MySqlConnection : DbConnection, ICloneable
  {
    internal ConnectionState connectionState;
    internal Driver driver;
    internal bool hasBeenOpen;
    internal bool hasBeenDisposed;
    private SchemaProvider _schemaProvider;
    private ExceptionInterceptor _exceptionInterceptor;
    internal CommandInterceptor commandInterceptor;
    private bool _isKillQueryConnection;
    private string _database;
    private int _commandTimeout;
#if NET5_0_OR_GREATER
#nullable enable
    Activity? currentActivity;
#nullable disable
#endif

    /// <summary>
    /// Occurs when FIDO authentication requests to perform gesture action on a device.
    /// </summary>
    [Obsolete("FIDO authentication client-side plugin is now deprecated. Use WebAuthn authentication client-side plugin instead.")]
    public event FidoActionCallback FidoActionRequested;

    /// <summary>
    /// Occurs when WebAuthn authentication makes a request to perform the gesture action on a device.
    /// </summary>
    public event WebAuthnActionCallback WebAuthnActionRequested;

    /// <summary>
    /// Occurs when MySQL returns warnings as a result of executing a command or query.
    /// </summary>
    public event MySqlInfoMessageEventHandler InfoMessage;

    private static readonly Cache<string, MySqlConnectionStringBuilder> ConnectionStringCache =
      new Cache<string, MySqlConnectionStringBuilder>(0, 25);

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlConnection"/> class.
    /// </summary>
    /// <remarks>
    /// You can read more about it <see href="https://dev.mysql.com/doc/connector-net/en/connector-net-tutorials-connection.html">here</see>. 
    /// </remarks>
    public MySqlConnection()
    {
      //TODO: add event data to StateChange docs
      Settings = new MySqlConnectionStringBuilder();
      _database = String.Empty;
      hasBeenDisposed = false;
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="MySqlConnection"/> class when given a string containing the connection string.
    /// </summary>
    /// <remarks>
    /// You can read more about it <see href="https://dev.mysql.com/doc/connector-net/en/connector-net-tutorials-connection.html">here</see>.
    ///</remarks>
    ///<param name="connectionString">The connection properties used to open the MySQL database.
    ///</param>
    public MySqlConnection(string connectionString)
      : this()
    {
      Settings.AnalyzeConnectionString(connectionString ?? string.Empty, false, false, false);
      IsConnectionStringAnalyzed = true;
      ConnectionString = connectionString;
    }

    #region Destructor
    ~MySqlConnection()
    {
      Dispose(false);
    }
    #endregion

    #region Interal Methods & Properties

    internal PerformanceMonitor PerfMonitor { get; private set; }

    internal ProcedureCache ProcedureCache { get; private set; }

    internal MySqlConnectionStringBuilder Settings { get; private set; }

    internal MySqlDataReader Reader
    {
      get
      {
        return driver?.reader;
      }
      set
      {
        driver.reader = value;
        IsInUse = driver.reader != null;
      }
    }

    internal void OnInfoMessage(MySqlInfoMessageEventArgs args)
    {
      InfoMessage?.Invoke(this, args);
    }

    internal bool SoftClosed
    {
      get
      {
        return (State == ConnectionState.Closed) &&
               driver != null && driver.currentTransaction != null;
      }
    }

    internal bool IsInUse { get; set; }

    /// <summary>
    /// Determines whether the connection is a clone of other connection.
    /// </summary>
    internal bool IsClone { get; set; }
    internal bool ParentHasbeenOpen { get; set; }
    #endregion

    #region Properties

    /// <summary>
    /// Returns the ID of the server thread this connection is executing on.
    /// </summary>
    [Browsable(false)]
    public int ServerThread => driver.ThreadID;

    /// <summary>
    /// Gets the name of the MySQL server to which to connect.
    /// </summary>
    [Browsable(true)]
    public override string DataSource => Settings.Server;

    /// <summary>
    /// Gets the time to wait while trying to establish a connection before terminating the attempt and generating an error.
    /// </summary>
    /// <remarks>
    ///  A value of 0 indicates no limit, and should be avoided in a call to
    ///  <see cref="MySqlConnection.ConnectionString"/> because an attempt to connect
    ///  will wait indefinitely.
    /// </remarks>
    /// <exception cref="ArgumentException">The value set is less than 0.</exception>
    [Browsable(true)]
    public override int ConnectionTimeout => (int)Settings.ConnectionTimeout;

    /// <summary>Gets the name of the current database or the database to be used after a connection is opened.</summary>
    /// <returns>The name of the current database or the name of the database to be used after a connection is opened. 
    /// The default value is an empty string.</returns>
    /// <remarks>
    ///  <para>
    ///    The <see cref="Database"/> property does not update dynamically.
    ///    If you change the current database using a SQL statement, then this property
    ///    may reflect the wrong value. If you change the current database using the <see cref="ChangeDatabase"/>
    ///    method, this property is updated to reflect the new database.
    ///  </para>
    /// </remarks>
    [Browsable(true)]
    public override string Database => _database;

    /// <summary>
    /// Indicates if this connection should use compression when communicating with the server.
    /// </summary>
    [Browsable(false)]
    public bool UseCompression => Settings.UseCompression;

    /// <summary>Gets the current state of the connection.</summary>
    /// <returns>
    ///  A bitwise combination of the <see cref="ConnectionState"/> values. The default is <see cref="ConnectionState.Closed"/>.
    /// </returns>
    /// <remarks>
    ///  The allowed state changes are:
    ///  <list type="bullet">
    ///    <item>
    ///      From <see cref="ConnectionState.Closed"/> to <see cref="ConnectionState.Open"/>, 
    ///      using the <see cref="ConnectionState.Open"/> method of the connection object.
    ///    </item>
    ///    <item>
    ///      From <B>Open</B> to <B>Closed</B>, using either the <B>Close</B> method or the <B>Dispose</B> method of the connection object.
    ///    </item>
    ///  </list>
    ///</remarks>
    [Browsable(false)]
    public override ConnectionState State => connectionState;

    [Browsable(true)]
    /// <summary>
    ///  Gets a boolean indicating if the current connection had been disposed.
    /// </summary>
    public bool IsDisposed { get { return hasBeenDisposed; } }

    /// <summary>Gets a string containing the version of the MySQL server to which the client is connected.</summary>
    /// <returns>The version of the instance of MySQL.</returns>
    /// <exception cref = "InvalidOperationException" > The connection is closed.</exception>
    [Browsable(false)]
    public override string ServerVersion => driver.Version.ToString();

    /// <summary>
    ///  Gets or sets the string used to connect to a MySQL database.
    /// </summary>
    /// <remarks>
    /// You can read more about it <see href="https://dev.mysql.com/doc/connector-net/en/connector-net-8-0-connection-options.html">here</see>.
    /// </remarks>
#if NET452
    [Editor("MySql.Data.MySqlClient.Design.ConnectionStringTypeEditor,MySqlClient.Design", typeof(UITypeEditor))]
#endif
    [Browsable(true)]
    [Category("Data")]
    [Description("Information used to connect to a DataSource, such as 'Server=xxx;UserId=yyy;Password=zzz;Database=dbdb'.")]
    public override string ConnectionString
    {
      get
      {
        // Always return exactly what the user set.
        // Security-sensitive information may be removed.
        return Settings.GetConnectionString(!IsClone ? (!hasBeenOpen || Settings.PersistSecurityInfo) :
        !Settings.PersistSecurityInfo ? (ParentHasbeenOpen ? false : !hasBeenOpen) : (Settings.PersistSecurityInfo));
      }
      set
      {
        if (State != ConnectionState.Closed)
          Throw(new MySqlException(
            "Not allowed to change the 'ConnectionString' property while the connection (state=" + State + ")."));

        MySqlConnectionStringBuilder newSettings;
        lock (ConnectionStringCache)
        {
          if (value == null)
            newSettings = new MySqlConnectionStringBuilder();
          else
          {
            newSettings = ConnectionStringCache[value];
            if (null == newSettings || FailoverManager.FailoverGroup == null)
            {
              newSettings = new MySqlConnectionStringBuilder(value, IsConnectionStringAnalyzed);
              ConnectionStringCache.Add(value, newSettings);
            }
          }
        }

        Settings = newSettings;

        if (!string.IsNullOrEmpty(Settings.Database))
          _database = Settings.Database;

        if (driver != null)
          driver.Settings = newSettings;
      }
    }

    /// <summary>
    /// Gets the instance of the <see cref="MySqlClientFactory"/>
    /// </summary>
    protected override DbProviderFactory DbProviderFactory => MySqlClientFactory.Instance;

    /// <summary>
    /// Gets a boolean value that indicates whether the password associated to the connection is expired.
    /// </summary>
    public bool IsPasswordExpired => driver.IsPasswordExpired;

    /// <summary>
    /// Gets a boolean value that indicates whether the connection string has been analyzed or not.
    /// </summary>
    internal bool IsConnectionStringAnalyzed = false;

    #endregion

    /// <summary>
    /// Creates and returns a System.Data.Common.DbCommand object associated with the current connection.
    /// </summary>
    /// <returns>A <see cref="DbCommand"/> object.</returns>
    protected override DbCommand CreateDbCommand()
    {
      return CreateCommand();
    }

    internal void SetState(ConnectionState newConnectionState, bool broadcast)
    {
      if (newConnectionState == connectionState && !broadcast)
        return;
      ConnectionState oldConnectionState = connectionState;
      connectionState = newConnectionState;
      if (broadcast)
        OnStateChange(new StateChangeEventArgs(oldConnectionState, connectionState));
    }

    #region IDisposeable

    /// <summary>
    /// Releases the resources used by the <see cref="MySqlConnection"/>
    /// </summary>
    public new void Dispose() => Dispose(true);

    protected override void Dispose(bool disposing)
    {
      if (State == ConnectionState.Open)
        Close();
      hasBeenDisposed = true;
      base.Dispose(disposing);
    }

#if NETFRAMEWORK || NETSTANDARD2_0
    public async Task DisposeAsync()
#else
    public override async ValueTask DisposeAsync()
#endif
    {
      if (State != ConnectionState.Closed)
        await CloseAsync().ConfigureAwait(false);

      GC.SuppressFinalize(this);
    }

    #endregion

    #region Transactions

    /// <summary>
    /// Starts a database transaction.
    /// </summary>
    /// <param name="isolationLevel">Specifies the <see cref="IsolationLevel"/> for the transaction.</param>
    /// <returns>A <see cref="MySqlTransaction"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => BeginTransactionAsync(false, isolationLevel, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <returns>A <see cref="MySqlTransaction"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public new MySqlTransaction BeginTransaction() => BeginTransactionAsync(false, IsolationLevel.RepeatableRead, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Starts a database transaction.
    /// </summary>
    /// <param name="isolationLevel">Specifies the <see cref="IsolationLevel"/> for the transaction.</param>
    /// <param name="scope">The scope of the transaction.</param>
    /// <returns>A <see cref="MySqlTransaction"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public MySqlTransaction BeginTransaction(IsolationLevel isolationLevel, string scope = "") => BeginTransactionAsync(false, isolationLevel, CancellationToken.None, scope).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of <see cref="BeginTransaction()"/>.
    /// </summary>
    /// <returns>A <see cref="MySqlTransaction"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public ValueTask<MySqlTransaction> BeginTransactionAsync() => BeginTransactionAsync(true, IsolationLevel.RepeatableRead, CancellationToken.None);

#if NETSTANDARD2_0 || NETFRAMEWORK
    /// <summary>
    /// Asynchronous version of <see cref="BeginTransaction()"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="MySqlTransaction"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public ValueTask<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => BeginTransactionAsync(true, IsolationLevel.RepeatableRead, cancellationToken);

    /// <summary>
    /// Asynchronous version of <see cref="BeginDbTransaction(IsolationLevel)"/>.
    /// </summary>
    /// <param name="isolationLevel">Specifies the <see cref="IsolationLevel"/> for the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{MySqlTransaction}"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public ValueTask<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => BeginTransactionAsync(true, isolationLevel, cancellationToken);
#else
    /// <summary>
    /// Asynchronous version of <see cref="BeginTransaction()"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="MySqlTransaction"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public new ValueTask<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => BeginTransactionAsync(true, IsolationLevel.RepeatableRead, cancellationToken);

    /// <summary>
    /// Asynchronous version of <see cref="BeginDbTransaction(IsolationLevel)"/>.
    /// </summary>
    /// <param name="isolationLevel">Specifies the <see cref="IsolationLevel"/> for the transaction.</param>
    /// <returns>A <see cref="ValueTask{MySqlTransaction}"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public ValueTask<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) => BeginTransactionAsync(true, isolationLevel, CancellationToken.None);

    /// <summary>
    /// Asynchronous version of <see cref="BeginDbTransaction(IsolationLevel)"/>.
    /// </summary>
    /// <param name="isolationLevel">Specifies the <see cref="IsolationLevel"/> for the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{MySqlTransaction}"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    public new ValueTask<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => BeginTransactionAsync(true, isolationLevel, cancellationToken);

    /// <summary>
    /// Asynchronous version of <see cref="BeginDbTransaction(IsolationLevel)"/>.
    /// </summary>
    /// <param name="isolationLevel">Specifies the <see cref="IsolationLevel"/> for the transaction.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask{DbTransaction}"/> representing the new transaction.</returns>
    /// <exception cref="InvalidOperationException">Parallel transactions are not supported.</exception>
    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => await BeginTransactionAsync(true, isolationLevel, cancellationToken).ConfigureAwait(false);
#endif

    private async ValueTask<MySqlTransaction> BeginTransactionAsync(bool execAsync, IsolationLevel isolationLevel, CancellationToken cancellationToken, string scope = "")
    {
      if (State != ConnectionState.Open)
        Throw(new InvalidOperationException(Resources.ConnectionNotOpen));
      // First check to see if we are in a current transaction
      if (driver.HasStatus(ServerStatusFlags.InTransaction))
        Throw(new InvalidOperationException(Resources.NoNestedTransactions));

      MySqlCommand cmd = new MySqlCommand("", this);

      cmd.CommandText = $"SET {scope} TRANSACTION ISOLATION LEVEL ";

      switch (isolationLevel)
      {
        case IsolationLevel.ReadCommitted:
          cmd.CommandText += "READ COMMITTED";
          break;
        case IsolationLevel.ReadUncommitted:
          cmd.CommandText += "READ UNCOMMITTED";
          break;
        case IsolationLevel.Unspecified:
        case IsolationLevel.RepeatableRead:
          cmd.CommandText += "REPEATABLE READ";
          break;
        case IsolationLevel.Serializable:
          cmd.CommandText += "SERIALIZABLE";
          break;
        case IsolationLevel.Chaos:
          Throw(new NotSupportedException(Resources.ChaosNotSupported));
          break;
        case IsolationLevel.Snapshot:
          Throw(new NotSupportedException(Resources.SnapshotNotSupported));
          break;
      }

      await cmd.ExecuteNonQueryAsync(execAsync, cancellationToken).ConfigureAwait(false);
      cmd.CommandText = "BEGIN";
      cmd.CommandType = CommandType.Text;
      await cmd.ExecuteNonQueryAsync(execAsync, cancellationToken).ConfigureAwait(false);

      MySqlTransaction t = new MySqlTransaction(this, isolationLevel);
      return t;
    }

    #endregion

    /// <summary>Changes the current database for an open <see cref="MySqlConnection"/>.</summary>
    /// <param name="databaseName">The name of the database to use.</param>
    /// <remarks>
    ///  <para>
    ///    The value supplied in the <I>databaseName</I> parameter must be a valid database
    ///    name. The <I>databaseName</I> parameter cannot contain a null value, an empty
    ///    string, or a string with only blank characters.
    ///  </para>
    ///  <para>
    ///    When you are using connection pooling against MySQL, and you close
    ///    the connection, it is returned to the connection pool. The next time the
    ///    connection is retrieved from the pool, the reset connection request
    ///    executes before the user performs any operations.
    ///  </para>
    /// </remarks>
    /// <exception cref="ArgumentException">The database name is not valid.</exception>
    /// <exception cref="InvalidOperationException">The connection is not open.</exception>
    /// <exception cref="MySqlException">Cannot change the database.</exception>
    public override void ChangeDatabase(string databaseName) => ChangeDatabaseAsync(databaseName, false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of the <see cref="ChangeDatabase(string)"/> method.
    /// </summary>
    /// <param name="databaseName">The name of the database to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
#if NETFRAMEWORK || NETSTANDARD2_0
    public Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => ChangeDatabaseAsync(databaseName, true, cancellationToken);
#else
    public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => ChangeDatabaseAsync(databaseName, true, cancellationToken);
#endif

    internal async Task ChangeDatabaseAsync(string databaseName, bool execAsync, CancellationToken cancellationToken)
    {
      if (databaseName == null || databaseName.Trim().Length == 0)
        Throw(new ArgumentException(Resources.ParameterIsInvalid, "databaseName"));

      if (State != ConnectionState.Open)
        Throw(new InvalidOperationException(Resources.ConnectionNotOpen));

      // This semaphore prevents promotable transaction rollback to run
      // in parallel
      SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

      semaphoreSlim.Wait();
      // We use default command timeout for SetDatabase
      using (new CommandTimer(this, (int)Settings.DefaultCommandTimeout))
        await driver.SetDatabaseAsync(databaseName, execAsync).ConfigureAwait(false);

      semaphoreSlim.Release();

      _database = databaseName;
    }

    /// <summary>
    /// Pings the server.
    /// </summary>
    /// <returns><c>true</c> if the ping was successful; otherwise, <c>false</c>.</returns>
    public bool Ping() => PingAsync(false).GetAwaiter().GetResult();

    /// <summary>
    /// Pings the server.
    /// </summary>
    /// <returns><c>true</c> if the ping was successful; otherwise, <c>false</c>.</returns>
    public Task<bool> PingAsync() => PingAsync(true);

    private async Task<bool> PingAsync(bool execAsync)
    {
      if (Reader != null)
        Throw(new MySqlException(Resources.DataReaderOpen));
      if (driver != null && await driver.PingAsync(execAsync).ConfigureAwait(false))
        return true;
      driver = null;
      SetState(ConnectionState.Closed, true);
      return false;
    }

    /// <summary>Opens a database connection with the property settings specified by the <see cref="ConnectionString"/>.</summary>
    /// <exception cref="InvalidOperationException">Cannot open a connection without specifying a data source or server.</exception>
    /// <exception cref="MySqlException">A connection-level error occurred while opening the connection.</exception>
    /// <remarks>
    ///  <para>
    ///    The <see cref="MySqlConnection"/> draws an open connection from the connection pool if one is available.
    ///    Otherwise, it establishes a new connection to an instance of MySQL.
    ///  </para>
    /// </remarks>
    public override void Open() =>  OpenAsync(false, CancellationToken.None).GetAwaiter().GetResult();

    public override Task OpenAsync(CancellationToken cancellationToken) => OpenAsync(true, cancellationToken);

    internal async Task OpenAsync(bool execAsync, CancellationToken cancellationToken)
    {
      if (State != ConnectionState.Closed)
        Throw(new InvalidOperationException(Resources.ConnectionAlreadyOpen));

      if (hasBeenDisposed)
        Throw(new InvalidOperationException("The connection had been disposed."));

      // start up our interceptors
      _exceptionInterceptor = new ExceptionInterceptor(this);
      commandInterceptor = new CommandInterceptor(this);

      SetState(ConnectionState.Connecting, true);
      AssertPermissions();

      Settings.FidoActionRequested = FidoActionRequested;
      Settings.WebAuthnActionRequested = WebAuthnActionRequested;

      //TODO: SUPPORT FOR 452 AND 46X
      // if we are auto enlisting in a current transaction, then we will be
      // treating the connection as pooled
      if (Settings.AutoEnlist && Transaction.Current != null)
      {
        driver = DriverTransactionManager.GetDriverInTransaction(Transaction.Current);
        if (driver != null && (driver.IsInActiveUse || !driver.Settings.EquivalentTo(this.Settings)))
          Throw(new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported));
      }

      MySqlConnectionStringBuilder currentSettings = Settings;
      try
      {
        if (!Settings.Pooling || MySqlPoolManager.Hosts == null)
        {
          FailoverManager.Reset();

          if (Settings.DnsSrv)
          {
            var dnsSrvRecords = DnsSrv.GetDnsSrvRecords(Settings.Server);
            FailoverManager.SetHostList(dnsSrvRecords.ConvertAll(r => new FailoverServer(r.Target, r.Port, null)),
              FailoverMethod.Sequential);
          }
          else
            FailoverManager.ParseHostList(Settings.Server, false);
        }

        // Load balancing && Failover
        if (ReplicationManager.IsReplicationGroup(Settings.Server))
        {
          if (driver == null)
          {
            await ReplicationManager.GetNewConnectionAsync(Settings.Server, false, this, execAsync, cancellationToken).ConfigureAwait(false);
          }
          else
            currentSettings = driver.Settings;
        }
        else if (FailoverManager.FailoverGroup != null && !Settings.Pooling)
        {
          string connectionString = await FailoverManager.AttemptConnectionAsync(this, Settings.ConnectionString, execAsync, cancellationToken);
          currentSettings.ConnectionString = connectionString;
        }

        if (Settings.Pooling)
        {
          if (FailoverManager.FailoverGroup != null)
          {
            string connectionString = await FailoverManager.AttemptConnectionAsync(this, Settings.ConnectionString, execAsync, cancellationToken, true);
            currentSettings.ConnectionString = connectionString;
          }
#if NET5_0_OR_GREATER              
          currentActivity = MySQLActivitySource.OpenPooledConnection(currentSettings);
#endif
          MySqlPool pool = await MySqlPoolManager.GetPoolAsync(currentSettings, execAsync, cancellationToken).ConfigureAwait(false);
          if (driver == null || !driver.IsOpen)
            driver = await pool.GetConnectionAsync(execAsync, cancellationToken).ConfigureAwait(false);
          ProcedureCache = pool.ProcedureCache;
        }
        else
        {
            if (driver == null || !driver.IsOpen)
            {
#if NET5_0_OR_GREATER              
                currentActivity = MySQLActivitySource.OpenConnection(currentSettings);
#endif
                driver = await Driver.CreateAsync(currentSettings, execAsync, cancellationToken).ConfigureAwait(false);
            }

            ProcedureCache = new ProcedureCache((int)Settings.ProcedureCacheSize);
        }
      }
      catch (Exception ex)
      {
#if NET5_0_OR_GREATER              
        MySQLActivitySource.SetException(currentActivity, ex);
#endif
        SetState(ConnectionState.Closed, true);
        throw;
      }

      SetState(ConnectionState.Open, false);
      await driver.ConfigureAsync(this, execAsync, cancellationToken).ConfigureAwait(false);

      if (driver.IsPasswordExpired && Settings.Pooling)
        await MySqlPoolManager.ClearPoolAsync(currentSettings, execAsync).ConfigureAwait(false);

      if (!(driver.SupportsPasswordExpiration && driver.IsPasswordExpired))
      {
        if (!string.IsNullOrEmpty(Settings.Database))
          await ChangeDatabaseAsync(Settings.Database, execAsync, cancellationToken).ConfigureAwait(false);
      }

      // setup our schema provider
      _schemaProvider = new ISSchemaProvider(this);
      PerfMonitor = new PerformanceMonitor(this);

      // if we are opening up inside a current transaction, then autoenlist
      // TODO: control this with a connection string option
      if (Transaction.Current != null && Settings.AutoEnlist)
        EnlistTransaction(Transaction.Current);

      hasBeenOpen = true;
      SetState(ConnectionState.Open, true);
    }

    /// <summary>
    /// Creates and returns a <see cref="MySqlCommand"/> object associated with the <see cref="MySqlConnection"/>.
    /// </summary>
    /// <returns>A <see cref="MySqlCommand"/> object.</returns>
    public new MySqlCommand CreateCommand()
    {
      // Return a new instance of a command object.
      MySqlCommand c = new MySqlCommand();
      c.Connection = this;
      return c;
    }

    internal async Task AbortAsync(bool execAsync, CancellationToken cancellationToken = default)
    {
      try
      {
        if (driver.HasStatus(ServerStatusFlags.InTransaction))
        {
          MySqlConnection newConn = (MySqlConnection)this.Clone();
          Driver newDriver =
            await Driver.CreateAsync(new MySqlConnectionStringBuilder(newConn.ConnectionString), execAsync, cancellationToken).ConfigureAwait(false);

          lock (newDriver)
          {
            newConn.driver = newDriver;
            newDriver.currentTransaction = driver.currentTransaction;
            driver.currentTransaction.Connection = newConn;
          }
        }
      }
      catch (Exception ex)
      {
        MySqlTrace.LogWarning(ServerThread, String.Concat("Error occurred aborting the connection. Exception was: ", ex.Message));
      }
      finally
      {
        await driver.CloseAsync(execAsync).ConfigureAwait(false);
        this.IsInUse = false;
      }
      SetState(ConnectionState.Closed, true);
    }

    internal async Task CloseFullyAsync(bool execAsync)
    {
      if (Settings.Pooling && driver.IsOpen)
      {
        //TODO: SUPPORT FOR 452 AND 46X
        //// if we are in a transaction, roll it back
        if (driver.HasStatus(ServerStatusFlags.InTransaction))
        {
          MySqlTransaction t = new MySqlTransaction(this, IsolationLevel.Unspecified);
          t.Rollback();
        }

        await MySqlPoolManager.ReleaseConnectionAsync(driver, execAsync).ConfigureAwait(false);
      }
      else
        await driver.CloseAsync(execAsync).ConfigureAwait(false);

      driver = null;
    }

    /// <summary>Closes the connection to the database. This is the preferred method of closing any open connection.</summary>
    /// <remarks>
    ///  <para>
    ///    The <see cref="Close"/> method rolls back any pending transactions. It then releases
    ///    the connection to the connection pool, or closes the connection if connection
    ///    pooling is disabled.
    ///  </para>
    ///  <para>
    ///    An application can call <see cref="Close"/> more than one time. No exception is
    ///    generated.
    ///  </para>
    /// </remarks>
    public override void Close() => CloseAsync(false).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of the <see cref="Close"/> method.
    /// </summary>
#if NETSTANDARD2_0 || NETFRAMEWORK
    public Task CloseAsync() => CloseAsync(true);
#else
    public override Task CloseAsync() => CloseAsync(true);
#endif

    /// <summary>
    /// Asynchronous version of the <see cref="Close"/> method.
    /// </summary>
    internal async Task CloseAsync(bool execAsync)
    {
#if NET5_0_OR_GREATER              
      MySQLActivitySource.CloseConnection(currentActivity);
#endif

      if (driver != null)
        driver.IsPasswordExpired = false;

      if (State == ConnectionState.Closed) return;

      if (Reader != null)
        Reader.Close();

      // if the reader was opened with CloseConnection then driver
      // will be null on the second time through
      if (driver != null)
      {
        //TODO: Add support for 452 and 46X
        if (driver.currentTransaction == null)
          await CloseFullyAsync(execAsync).ConfigureAwait(false);
        //TODO: Add support for 452 and 46X
        else
          driver.IsInActiveUse = false;
      }

      FailoverManager.Reset();
      MySqlPoolManager.Hosts = null;

      SetState(ConnectionState.Closed, true);
    }

    internal string CurrentDatabase()
    {
      if (!string.IsNullOrEmpty(Database))
        return Database;
      MySqlCommand cmd = new MySqlCommand("SELECT database()", this);
      return cmd.ExecuteScalar().ToString();
    }

    internal async Task HandleTimeoutOrThreadAbortAsync(Exception ex, bool execAsync, CancellationToken cancellationToken = default)
    {
      bool isFatal = false;

      if (_isKillQueryConnection)
      {
        // Special connection started to cancel a query.
        // Abort will prevent recursive connection spawning
        await AbortAsync(execAsync, cancellationToken).ConfigureAwait(false);
        if (ex is TimeoutException)
        {
          Throw(new MySqlException(Resources.Timeout, true, ex));
        }
        else
        {
          return;
        }
      }

      try
      {
        // Do a fast cancel.The reason behind small values for connection
        // and command timeout is that we do not want user to wait longer
        // after command has already expired.
        // Microsoft's SqlClient seems to be using 5 seconds timeouts 
        // here as well.
        // Read the  error packet with "interrupted" message.
        if (execAsync)
          await CancelQueryAsync(5, cancellationToken).ConfigureAwait(false);
        else
          CancelQuery(5);

        driver.ResetTimeout(5000);
        if (Reader != null)
        {
          if (execAsync)
            await Reader.CloseAsync(execAsync).ConfigureAwait(false);
          else
            Reader.Close();

          Reader = null;
        }
      }
      catch (Exception ex2)
      {
        MySqlTrace.LogWarning(ServerThread, "Could not kill query, " +
          " aborting connection. Exception was " + ex2.Message);
        await AbortAsync(execAsync, cancellationToken).ConfigureAwait(false);
        isFatal = true;
      }
      if (ex is TimeoutException)
      {
        Throw(new MySqlException(Resources.Timeout, isFatal, ex));
      }
    }

    /// <summary>
    /// Cancels the query after the specified time interval.
    /// </summary>
    /// <param name="timeout">The length of time (in seconds) to wait for the cancellation of the command execution.</param>
    public void CancelQuery(int timeout) => CancelQueryAsync(timeout, false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of the <see cref="CancelQuery(int)"/> method.
    /// </summary>
    /// <param name="timeout">The length of time (in seconds) to wait for the cancellation of the command execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task CancelQueryAsync(int timeout, CancellationToken cancellationToken) => CancelQueryAsync(timeout, true, cancellationToken);

    private async Task CancelQueryAsync(int timeout, bool execAsync, CancellationToken cancellationToken)
    {
      var cb = new MySqlConnectionStringBuilder(Settings.ConnectionString, IsConnectionStringAnalyzed);
      cb.Pooling = false;
      cb.AutoEnlist = false;
      cb.ConnectionTimeout = (uint)timeout;

      using (MySqlConnection c = new MySqlConnection(cb.ConnectionString))
      {
        c._isKillQueryConnection = true;
        await c.OpenAsync(execAsync, cancellationToken).ConfigureAwait(false);
        string commandText = "KILL QUERY " + ServerThread;
        MySqlCommand cmd = new MySqlCommand(commandText, c) { CommandTimeout = timeout };
        cmd.ExecuteNonQuery();
      }
    }

    internal void Throw(Exception ex)
    {
      if (_exceptionInterceptor == null)
        throw ex;
      _exceptionInterceptor.Throw(ex);
    }

    /// <summary>
    /// Returns schema information for the data source of this <see cref="DbConnection"/>.
    /// </summary>
    /// <returns>A <see cref="DataTable"/> that contains schema information. </returns>
    public override DataTable GetSchema() => GetSchemaAsync(false).GetAwaiter().GetResult();

    /// <summary>
    /// Returns schema information for the data source of this 
    /// <see cref="DbConnection"/> using the specified string for the schema name. 
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return.</param>
    /// <returns>A <see cref="DataTable"/> that contains schema information.</returns>
    public override DataTable GetSchema(string collectionName) => GetSchemaAsync(false, collectionName).GetAwaiter().GetResult();

    /// <summary>
    /// Returns schema information for the data source of this <see cref="DbConnection"/>
    /// using the specified string for the schema name and the specified string array 
    /// for the restriction values. 
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return.</param>
    /// <param name="restrictionValues">Specifies a set of restriction values for the requested schema.</param>
    /// <returns>A <see cref="DataTable"/> that contains schema information.</returns>
    public override DataTable GetSchema(string collectionName, string[] restrictionValues) => GetSchemaAsync(false, collectionName, restrictionValues).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of <see cref="GetSchema()"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
    public Task<DataTable> GetSchemaAsync(CancellationToken cancellationToken = default)
#else
    public override Task<DataTable> GetSchemaAsync(CancellationToken cancellationToken = default)
#endif
      => GetSchemaAsync(true, cancellationToken: cancellationToken);


    /// <summary>
    /// Asynchronous version of <see cref="GetSchema(string)"/>.
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
    public Task<DataTable> GetSchemaAsync(string collectionName, CancellationToken cancellationToken = default)
#else
    public override Task<DataTable> GetSchemaAsync(string collectionName, CancellationToken cancellationToken = default)
#endif
      => GetSchemaAsync(true, collectionName, cancellationToken: cancellationToken);

    /// <summary>
    /// Asynchronous version of <see cref="GetSchema(string, string[])"/>.
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return.</param>
    /// <param name="restrictionValues">Specifies a set of restriction values for the requested schema.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
    public Task<DataTable> GetSchemaAsync(string collectionName, string[] restrictionValues, CancellationToken cancellationToken = default)
#else
    public override Task<DataTable> GetSchemaAsync(string collectionName, string[] restrictionValues, CancellationToken cancellationToken = default)
#endif
      => GetSchemaAsync(true, collectionName, restrictionValues, cancellationToken);

    internal async Task<DataTable> GetSchemaAsync(bool execAsync, string collectionName = null, string[] restrictionValues = null, CancellationToken cancellationToken = default)
    {
      collectionName ??= SchemaProvider.MetaCollection;
      string[] restrictions = _schemaProvider.CleanRestrictions(restrictionValues);
      MySqlSchemaCollection c = await _schemaProvider.GetSchemaAsync(collectionName, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      return c.AsDataTable();
    }

    /// <summary>
    /// Gets a schema collection based on the provided restriction values.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="restrictionValues">The values to restrict.</param>
    /// <returns>A schema collection object.</returns>
    public MySqlSchemaCollection GetSchemaCollection(string collectionName, string[] restrictionValues) => GetSchemaCollectionAsync(collectionName, restrictionValues, false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of the <see cref="GetSchemaCollection(string, string[])"/> method.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="restrictionValues">The values to restrict.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of schema objects.</returns>
    public Task<MySqlSchemaCollection> GetSchemaCollectionAsync(string collectionName, string[] restrictionValues, CancellationToken cancellationToken = default) => GetSchemaCollectionAsync(collectionName, restrictionValues, true, cancellationToken);

    /// <summary>
    /// Asynchronous version of the <see cref="GetSchemaCollection(string, string[])"/> method.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="restrictionValues">The values to restrict.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <returns>A collection of schema objects.</returns>
    private async Task<MySqlSchemaCollection> GetSchemaCollectionAsync(string collectionName, string[] restrictionValues, bool execAsync, CancellationToken cancellationToken)
    {
      collectionName ??= SchemaProvider.MetaCollection;

      string[] restrictions = _schemaProvider.CleanRestrictions(restrictionValues);
      MySqlSchemaCollection c = await _schemaProvider.GetSchemaAsync(collectionName, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      return c;
    }

    /// <summary>
    /// Enlists in the specified transaction. 
    /// </summary>
    /// <param name="transaction">A reference to an existing <see cref="Transaction"/> in which to enlist.</param>
    public override void EnlistTransaction(Transaction transaction)
    {
      // enlisting in the null transaction is a noop
      if (transaction == null)
        return;

      // guard against trying to enlist in more than one transaction
      if (driver.currentTransaction != null)
      {
        if (driver.currentTransaction.BaseTransaction == transaction)
          return;

        Throw(new MySqlException("Already enlisted"));
      }

      // now see if we need to swap out drivers.  We would need to do this since
      // we have to make sure all ops for a given transaction are done on the
      // same physical connection.
      Driver existingDriver = DriverTransactionManager.GetDriverInTransaction(transaction);
      if (existingDriver != null)
      {
        // we can't allow more than one driver to contribute to the same connection
        if (existingDriver.IsInActiveUse)
          Throw(new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported));

        // there is an existing driver and it's not being currently used.
        // now we need to see if it is using the same connection string
        string text1 = existingDriver.Settings.ConnectionString;
        string text2 = Settings.ConnectionString;
        if (String.Compare(text1, text2, true) != 0)
          Throw(new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported));

        // close existing driver
        // set this new driver as our existing driver
        CloseFullyAsync(false).GetAwaiter().GetResult();
        driver = existingDriver;
      }

      if (driver.currentTransaction == null)
      {
        MySqlPromotableTransaction t = new MySqlPromotableTransaction(this, transaction);
        if (!transaction.EnlistPromotableSinglePhase(t))
          Throw(new NotSupportedException(Resources.DistributedTxnNotSupported));

        driver.currentTransaction = t;
        DriverTransactionManager.SetDriverInTransaction(driver);
        driver.IsInActiveUse = true;
      }
    }

    void AssertPermissions()
    {
      // Security Asserts can only be done when the assemblies 
      // are put in the GAC as documented in 
      // http://msdn.microsoft.com/en-us/library/ff648665.aspx
      if (this.Settings.IncludeSecurityAsserts)
      {
        PermissionSet set = new PermissionSet(PermissionState.None);
        set.AddPermission(new MySqlClientPermission(ConnectionString));
        set.Demand();
        MySqlSecurityPermission.CreatePermissionSet(true).Assert();
      }
    }

    /// <summary>
    /// Creates a new <see cref="MySqlConnection"/> object with the exact same ConnectionString value.
    /// </summary>
    /// <returns>A cloned <see cref="MySqlConnection"/> object.</returns>
    public object Clone()
    {
      MySqlConnection clone = new MySqlConnection();
      clone.IsClone = true;
      clone.ParentHasbeenOpen = hasBeenOpen;
      clone.IsConnectionStringAnalyzed = IsConnectionStringAnalyzed;
      string connectionString = Settings.ConnectionString;
      if (connectionString != null)
        clone.ConnectionString = connectionString;
      return clone;
    }

    /// <summary>
    /// Returns an unopened copy of this connection with a new connection string. If the <c>Password</c>
    /// in <paramref name="connectionString"/> is not set, the password from this connection will be used.
    /// This allows creating a new connection with the same security information while changing other options,
    /// such as database or pooling.
    /// </summary>
    /// <param name="connectionString">The new connection string to be used.</param>
    /// <returns>A new <see cref="MySqlConnection"/> with different connection string options but
    /// the same password as this connection (unless overridden by <paramref name="connectionString"/>).</returns>
    public MySqlConnection CloneWith(string connectionString)
    {
      var newBuilder = new MySqlConnectionStringBuilder(connectionString ?? throw new ArgumentNullException(nameof(connectionString)), IsConnectionStringAnalyzed);
      var currentBuilder = new MySqlConnectionStringBuilder(ConnectionString, IsConnectionStringAnalyzed);
      var shouldCopyPassword = newBuilder.Password.Length == 0 && (!newBuilder.PersistSecurityInfo || currentBuilder.PersistSecurityInfo);

      if (shouldCopyPassword)
        newBuilder.Password = currentBuilder.Password;

      var cloneConnection = new MySqlConnection(newBuilder.ConnectionString);
      cloneConnection.ParentHasbeenOpen = hasBeenOpen;
      cloneConnection.IsClone = true;

      return cloneConnection;
    }

    #region Routines for timeout support.

    // Problem description:
    // Sometimes, ExecuteReader is called recursively. This is the case if
    // command behaviors are used and we issue "set sql_select_limit" 
    // before and after command. This is also the case with prepared 
    // statements , where we set session variables. In these situations, we 
    // have to prevent  recursive ExecuteReader calls from overwriting 
    // timeouts set by the top level command.

    // To solve the problem, SetCommandTimeout() and ClearCommandTimeout() are 
    // introduced . Query timeout here is  "sticky", that is once set with 
    // SetCommandTimeout, it only be overwritten after ClearCommandTimeout 
    // (SetCommandTimeout would return false if it timeout has not been 
    // cleared).

    // The proposed usage pattern of there routines is following: 
    // When timed operations starts, issue SetCommandTimeout(). When it 
    // finishes, issue ClearCommandTimeout(), but _only_ if call to 
    // SetCommandTimeout() was successful.

    /// <summary>
    /// Sets query timeout. If timeout has been set prior and not
    /// yet cleared with ClearCommandTimeout(), it has no effect.
    /// </summary>
    /// <param name="value">Timeout in seconds.</param>
    /// <returns><see langword="true"/> if a timeout is set.</returns>
    internal bool SetCommandTimeout(int value)
    {
      if (!hasBeenOpen)
        // Connection timeout is handled by driver
        return false;

      if (_commandTimeout != 0)
        // someone is trying to set a timeout while command is already
        // running. It could be for example recursive call to ExecuteReader
        // Ignore the request, as only top-level (non-recursive commands)
        // can set timeouts.
        return false;

      if (driver == null)
        return false;

      _commandTimeout = value;
      driver.ResetTimeout(_commandTimeout * 1000);
      return true;
    }

    /// <summary>
    /// Clears query timeout, allowing next SetCommandTimeout() to succeed.
    /// </summary>
    internal void ClearCommandTimeout()
    {
      if (!hasBeenOpen)
        return;
      _commandTimeout = 0;
      driver?.ResetTimeout(0);
    }

    #endregion

    #region Pool Routines

    /// <summary>Empties the connection pool associated with the specified connection.</summary>
    /// <param name="connection">
    ///  The <see cref="MySqlConnection"/> associated with the pool to be cleared.
    /// </param>
    /// <remarks>
    ///  <para>
    ///    <see cref="ClearPool(MySqlConnection)"/> clears the connection pool that is associated with the connection.
    ///    If additional connections associated with connection are in use at the time of the call,
    ///    they are marked appropriately and are discarded (instead of being returned to the pool)
    ///    when <see cref="Close"/> is called on them.
    ///  </para>
    /// </remarks>
    public static void ClearPool(MySqlConnection connection) => ClearPoolAsync(connection, false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of the <see cref="ClearPool(MySqlConnection)"/> method.
    /// </summary>
    /// <param name="connection">The connection associated with the pool to be cleared.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task ClearPoolAsync(MySqlConnection connection, CancellationToken cancellationToken = default) => ClearPoolAsync(connection, true, cancellationToken);

    private static async Task ClearPoolAsync(MySqlConnection connection, bool execAsync, CancellationToken cancellationToken)
    {
      await MySqlPoolManager.ClearPoolAsync(connection.Settings, execAsync).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears all connection pools.
    /// </summary>
    /// <remarks>ClearAllPools essentially performs a <see cref="ClearPool"/> on all current connection pools.</remarks>
    public static void ClearAllPools() => ClearAllPoolsAsync(false).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronous version of the <see cref="ClearAllPools"/> method.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task ClearAllPoolsAsync(CancellationToken cancellationToken = default) => ClearAllPoolsAsync(true);

    private static async Task ClearAllPoolsAsync(bool execAsync)
    {
      await MySqlPoolManager.ClearAllPoolsAsync(execAsync).ConfigureAwait(false);
    }

    #endregion
  }

  /// <summary>
  /// Represents the method to handle the <see cref="MySqlConnection.FidoActionRequested"/> event of a 
  /// <see cref="MySqlConnection"/>
  /// </summary>
  [Obsolete("FIDO authentication client-side plugin is now deprecated. Use WebAuthn authentication client-side plugin instead.")]
  public delegate void FidoActionCallback();

  /// <summary>
  /// Represents the method to handle the <see cref="MySqlConnection.WebAuthnActionRequested"/> event of a 
  /// <see cref="MySqlConnection"/>.
  /// </summary>
  public delegate void WebAuthnActionCallback();

  /// <summary>
  /// Represents the method to handle the <see cref="MySqlConnection.InfoMessage"/> event of a 
  /// <see cref="MySqlConnection"/>.
  /// </summary>
  public delegate void MySqlInfoMessageEventHandler(object sender, MySqlInfoMessageEventArgs args);

  /// <summary>
  /// Provides data for the InfoMessage event. This class cannot be inherited.
  /// </summary>
  public class MySqlInfoMessageEventArgs : EventArgs
  {
    /// <summary>
    /// Gets or sets an array of <see cref="MySqlError"/> objects together with the errors found.
    /// </summary>
    public MySqlError[] errors { get; set; }
  }

  /// <summary>
  /// IDisposable wrapper around SetCommandTimeout and ClearCommandTimeout functionality.
  /// </summary>
  internal class CommandTimer : IDisposable
  {
    private bool _timeoutSet;
    private MySqlConnection _connection;

    public CommandTimer(MySqlConnection connection, int timeout)
    {
      _connection = connection;
      if (connection != null)
      {
        _timeoutSet = connection.SetCommandTimeout(timeout);
      }
    }

    #region IDisposable Members
    public void Dispose()
    {
      if (!_timeoutSet) return;

      _timeoutSet = false;
      _connection.ClearCommandTimeout();
      _connection = null;
    }
    #endregion
  }
}
