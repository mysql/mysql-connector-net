// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
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

using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Common;
using IsolationLevel = System.Data.IsolationLevel;
using MySql.Data.MySqlClient.Interceptors;
using System.Transactions;
using MySql.Data.MySqlClient.Replication;
using MySql.Data.Failover;
#if NET452
using System.Drawing.Design;
#endif

namespace MySql.Data.MySqlClient
{
  /// <summary>
  ///  Represents a connection to a MySQL Server database. This class cannot be inherited.
  /// </summary>
  /// <remarks>
  ///  <para>
  ///    A <see cref="MySqlConnection"/> object represents a session to a MySQL Server
  ///    data source. When you create an instance of <see cref="MySqlConnection"/>, all
  ///    properties are set to their initial values.
  ///  </para>
  ///  <para>
  ///    If the <see cref="MySqlConnection"/> goes out of scope, it is not closed. Therefore,
  ///    you must explicitly close the connection by calling <see cref="MySqlConnection.Close"/>
  ///    or <see cref="MySqlConnection.Dispose()"/>.
  ///  </para>
  /// </remarks>
  public sealed partial class MySqlConnection : DbConnection
  {
    internal ConnectionState connectionState;
    internal Driver driver;
    internal bool hasBeenOpen;
    private SchemaProvider _schemaProvider;
    private ExceptionInterceptor _exceptionInterceptor;
    internal CommandInterceptor commandInterceptor;
    private bool _isKillQueryConnection;
    private string _database;
    private int _commandTimeout;

    /// <summary>
    /// Occurs when FIDO authentication request to perform gesture action on a device.
    /// </summary>
    public event FidoActionCallback FidoActionRequested;

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
      Settings.AnalyzeConnectionString(connectionString ?? string.Empty, false, false);
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
    /// Returns the id of the server thread this connection is executing on
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
    ///  A value of 0 indicates no limit, and should be avoided in a
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

    /// <summary>Gets a string containing the version of the MySQL server to which the client is connected.</summary>
    /// <returns>The version of the instance of MySQL.</returns>
    /// <exception cref = "InvalidOperationException" > The connection is closed.</exception>
    [Browsable(false)]
    public override string ServerVersion => driver.Version.ToString();

    /// <summary>
    ///  Gets or sets the string used to connect to a MySQL Server database.
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
              newSettings = new MySqlConnectionStringBuilder(value);
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

    #endregion

    /// <summary>
    /// Starts a database transaction.
    /// </summary>
    /// <param name="isolationLevel">Specifies the isolation level for the transaction.</param>
    /// <returns>An object representing the new transaction.</returns>
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
      if (isolationLevel == IsolationLevel.Unspecified)
        return BeginTransaction();
      return BeginTransaction(isolationLevel);
    }

    /// <summary>
    /// Creates and returns a System.Data.Common.DbCommand object associated with the current connection.
    /// </summary>
    /// <returns>A <see cref="DbCommand"/> object.</returns>
    protected override DbCommand CreateDbCommand()
    {
      return CreateCommand();
    }

    #region IDisposeable

    protected override void Dispose(bool disposing)
    {
      if (State == ConnectionState.Open)
        Close();
      base.Dispose(disposing);
    }

    #endregion

    #region Transactions
    /// <include file='docs/MySqlConnection.xml' path='docs/BeginTransaction/*'/>
    public new MySqlTransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.RepeatableRead);
    }

    /// <include file='docs/MySqlConnection.xml' path='docs/BeginTransaction1/*'/>
    public MySqlTransaction BeginTransaction(IsolationLevel iso, string scope = "")
    {
      //TODO: check note in help
      if (State != ConnectionState.Open)
        Throw(new InvalidOperationException(Resources.ConnectionNotOpen));

      // First check to see if we are in a current transaction
      if (driver.HasStatus(ServerStatusFlags.InTransaction))
        Throw(new InvalidOperationException(Resources.NoNestedTransactions));

      MySqlTransaction t = new MySqlTransaction(this, iso);

      MySqlCommand cmd = new MySqlCommand("", this);

      cmd.CommandText = $"SET {scope} TRANSACTION ISOLATION LEVEL ";
      switch (iso)
      {
        case IsolationLevel.ReadCommitted:
          cmd.CommandText += "READ COMMITTED";
          break;
        case IsolationLevel.ReadUncommitted:
          cmd.CommandText += "READ UNCOMMITTED";
          break;
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

      cmd.ExecuteNonQuery();

      cmd.CommandText = "BEGIN";
      cmd.CommandType = CommandType.Text;
      cmd.ExecuteNonQuery();

      return t;
    }

    #endregion

    /// <summary>Changes the current database for an open MySqlConnection.</summary>
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
    public override void ChangeDatabase(string databaseName)
    {
      if (databaseName == null || databaseName.Trim().Length == 0)
        Throw(new ArgumentException(Resources.ParameterIsInvalid, "databaseName"));

      if (State != ConnectionState.Open)
        Throw(new InvalidOperationException(Resources.ConnectionNotOpen));

      // This lock  prevents promotable transaction rollback to run
      // in parallel
      lock (driver)
      {
        // We use default command timeout for SetDatabase
        using (new CommandTimer(this, (int)Settings.DefaultCommandTimeout))
        {
          driver.SetDatabase(databaseName);
        }
      }
      this._database = databaseName;
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

    /// <summary>
    /// Pings the server.
    /// </summary>
    /// <returns><c>true</c> if the ping was successful; otherwise, <c>false</c>.</returns>
    public bool Ping()
    {
      if (Reader != null)
        Throw(new MySqlException(Resources.DataReaderOpen));
      if (driver != null && driver.Ping())
        return true;
      driver = null;
      SetState(ConnectionState.Closed, true);
      return false;
    }

    /// <summary>
    /// Executes the <see cref="Open"/> method as a Task.
    /// </summary>
    public override Task OpenAsync(CancellationToken cancellationToken)
    {
      return Task.Run(() => Open());
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
    public override void Open()
    {
      if (State == ConnectionState.Open)
        Throw(new InvalidOperationException(Resources.ConnectionAlreadyOpen));

      // start up our interceptors
      _exceptionInterceptor = new ExceptionInterceptor(this);
      commandInterceptor = new CommandInterceptor(this);

      SetState(ConnectionState.Connecting, true);
      AssertPermissions();

      Settings.FidoActionRequested = FidoActionRequested;

      //TODO: SUPPORT FOR 452 AND 46X
      // if we are auto enlisting in a current transaction, then we will be
      // treating the connection as pooled
      if (Settings.AutoEnlist && Transaction.Current != null)
      {
        driver = DriverTransactionManager.GetDriverInTransaction(Transaction.Current);
        if (driver != null &&
          (driver.IsInActiveUse ||
          !driver.Settings.EquivalentTo(this.Settings)))
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
            var dnsSrvRecords = DnsResolver.GetDnsSrvRecords(Settings.Server);
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
            ReplicationManager.GetNewConnection(Settings.Server, false, this);
          }
          else
            currentSettings = driver.Settings;
        }
        else if (FailoverManager.FailoverGroup != null && !Settings.Pooling)
        {
          FailoverManager.AttemptConnection(this, Settings.ConnectionString, out string connectionString);
          currentSettings.ConnectionString = connectionString;
        }

        if (Settings.Pooling)
        {
          if (FailoverManager.FailoverGroup != null)
          {
            FailoverManager.AttemptConnection(this, Settings.ConnectionString, out string connectionString, true);
            currentSettings.ConnectionString = connectionString;
          }

          MySqlPool pool = MySqlPoolManager.GetPool(currentSettings);
          if (driver == null || !driver.IsOpen)
            driver = pool.GetConnection();
          ProcedureCache = pool.ProcedureCache;
        }
        else
        {
          if (driver == null || !driver.IsOpen)
            driver = Driver.Create(currentSettings);
          ProcedureCache = new ProcedureCache((int)Settings.ProcedureCacheSize);
        }
      }
      catch (Exception)
      {
        SetState(ConnectionState.Closed, true);
        throw;
      }

      SetState(ConnectionState.Open, false);
      driver.Configure(this);

      if (driver.IsPasswordExpired && Settings.Pooling)
      {
        MySqlPoolManager.ClearPool(currentSettings);
      }

      if (!(driver.SupportsPasswordExpiration && driver.IsPasswordExpired))
      {
        if (!string.IsNullOrEmpty(Settings.Database))
          ChangeDatabase(Settings.Database);
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

    internal void Abort()
    {
      try
      {
        driver.Close();
      }
      catch (Exception ex)
      {
        MySqlTrace.LogWarning(ServerThread, String.Concat("Error occurred aborting the connection. Exception was: ", ex.Message));
      }
      finally
      {
        this.IsInUse = false;
      }
      SetState(ConnectionState.Closed, true);
    }

    internal void CloseFully()
    {
      if (Settings.Pooling && driver.IsOpen)
      {
        //TODO: SUPPORT FOR 452 AND 46X
        //// if we are in a transaction, roll it back
        if (driver.HasStatus(ServerStatusFlags.InTransaction))
        {
          MySqlTransaction t = new MySql.Data.MySqlClient.MySqlTransaction(this, IsolationLevel.Unspecified);
          t.Rollback();
        }

        MySqlPoolManager.ReleaseConnection(driver);
      }
      else
        driver.Close();

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
    public override void Close()
    {
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
          CloseFully();
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

    internal void HandleTimeoutOrThreadAbort(Exception ex)
    {
      bool isFatal = false;

      if (_isKillQueryConnection)
      {
        // Special connection started to cancel a query.
        // Abort will prevent recursive connection spawning
        Abort();
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
        CancelQuery(5);
        driver.ResetTimeout(5000);
        if (Reader != null)
        {
          Reader.Close();
          Reader = null;
        }
      }
      catch (Exception ex2)
      {
        MySqlTrace.LogWarning(ServerThread, "Could not kill query, " +
          " aborting connection. Exception was " + ex2.Message);
        Abort();
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
    /// <param name="timeout">The length of time (in seconds) to wait for the cancelation of the command execution.</param>
    public void CancelQuery(int timeout)
    {
      var cb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      cb.Pooling = false;
      cb.AutoEnlist = false;
      cb.ConnectionTimeout = (uint)timeout;

      using (MySqlConnection c = new MySqlConnection(cb.ConnectionString))
      {
        c._isKillQueryConnection = true;
        c.Open();
        string commandText = "KILL QUERY " + ServerThread;
        MySqlCommand cmd = new MySqlCommand(commandText, c) { CommandTimeout = timeout };
        cmd.ExecuteNonQuery();
      }
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
    /// yet cleared ClearCommandTimeout(), it has no effect.
    /// </summary>
    /// <param name="value">timeout in seconds</param>
    /// <returns>true if </returns>
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

    /// <summary>
    /// Gets a schema collection based on the provided restriction values.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="restrictionValues">The values to restrict.</param>
    /// <returns>A schema collection object.</returns>
    public MySqlSchemaCollection GetSchemaCollection(string collectionName, string[] restrictionValues)
    {
      if (collectionName == null)
        collectionName = SchemaProvider.MetaCollection;

      string[] restrictions = _schemaProvider.CleanRestrictions(restrictionValues);
      MySqlSchemaCollection c = _schemaProvider.GetSchema(collectionName, restrictions);
      return c;
    }

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
    public static void ClearPool(MySqlConnection connection)
    {
      MySqlPoolManager.ClearPool(connection.Settings);
    }

    /// <summary>
    /// Clears all connection pools.
    /// </summary>
    /// <remarks>ClearAllPools essentially performs a <see cref="ClearPool"/> on all current connection pools.</remarks>
    public static void ClearAllPools()
    {
      MySqlPoolManager.ClearAllPools();
    }

    #endregion

    internal void Throw(Exception ex)
    {
      if (_exceptionInterceptor == null)
        throw ex;
      _exceptionInterceptor.Throw(ex);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="MySqlConnection"/>
    /// </summary>
    public new void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #region Async
    /// <summary>
    /// Initiates the asynchronous execution of a transaction.
    /// </summary>
    /// <returns>An object representing the new transaction.</returns>
    public Task<MySqlTransaction> BeginTransactionAsync()
    {
      return BeginTransactionAsync(IsolationLevel.RepeatableRead, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of BeginTransaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An object representing the new transaction.</returns>
    public Task<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
      return BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
    }

    /// <summary>
    /// Asynchronous version of BeginTransaction.
    /// </summary>
    /// <param name="iso">The isolation level under which the transaction should run. </param>
    /// <returns>An object representing the new transaction.</returns>
    public Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel iso)
    {
      return BeginTransactionAsync(iso, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of BeginTransaction.
    /// </summary>
    /// <param name="iso">The isolation level under which the transaction should run. </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An object representing the new transaction.</returns>
    public Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel iso, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<MySqlTransaction>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          MySqlTransaction tranResult = BeginTransaction(iso);
          result.SetResult(tranResult);
        }
        catch (Exception ex)
        {
          result.SetException(ex);
        }
      }
      else
      {
        result.SetCanceled();
      }

      return result.Task;
    }

    /// <summary>
    /// Asynchronous version of the ChangeDataBase method.
    /// </summary>
    /// <param name="databaseName">The name of the database to use.</param>
    /// <returns></returns>
    public Task ChangeDataBaseAsync(string databaseName)
    {
      return ChangeDataBaseAsync(databaseName, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the ChangeDataBase method.
    /// </summary>
    /// <param name="databaseName">The name of the database to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public Task ChangeDataBaseAsync(string databaseName, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<bool>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          ChangeDatabase(databaseName);
          result.SetResult(true);
        }
        catch (Exception ex)
        {
          result.SetException(ex);
        }
      }
      return result.Task;
    }

    /// <summary>
    /// Asynchronous version of the Close method.
    /// </summary>
    public Task CloseAsync()
    {
      return CloseAsync(CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="Close"/> method.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task CloseAsync(CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<bool>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          Close();
          result.SetResult(true);
        }
        catch (Exception ex)
        {
          result.SetException(ex);
        }
      }
      else
      {
        result.SetCanceled();
      }
      return result.Task;
    }

    /// <summary>
    /// Asynchronous version of the <see cref="ClearPool(MySqlConnection)"/> method.
    /// </summary>
    /// <param name="connection">The connection associated with the pool to be cleared.</param>
    public Task ClearPoolAsync(MySqlConnection connection)
    {
      return ClearPoolAsync(connection, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="ClearPool(MySqlConnection)"/> method.
    /// </summary>
    /// <param name="connection">The connection associated with the pool to be cleared.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task ClearPoolAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<bool>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          ClearPool(connection);
          result.SetResult(true);
        }
        catch (Exception ex)
        {
          result.SetException(ex);
        }
      }
      else
      {
        result.SetCanceled();
      }
      return result.Task;
    }

    /// <summary>
    /// Asynchronous version of the <see cref="ClearAllPools"/> method.
    /// </summary>
    public Task ClearAllPoolsAsync()
    {
      return ClearAllPoolsAsync(CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="ClearAllPools"/> method.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task ClearAllPoolsAsync(CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<bool>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          ClearAllPools();
          result.SetResult(true);
        }
        catch (Exception ex)
        {
          result.SetException(ex);
        }
      }
      else
      {
        result.SetCanceled();
      }
      return result.Task;
    }

    /// <summary>
    /// Asynchronous version of the <see cref="GetSchemaCollection(string, string[])"/> method.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="restrictionValues">The values to restrict.</param>
    /// <returns>A collection of schema objects.</returns>
    public Task<MySqlSchemaCollection> GetSchemaCollectionAsync(string collectionName, string[] restrictionValues)
    {
      return GetSchemaCollectionAsync(collectionName, restrictionValues, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="GetSchemaCollection(string, string[])"/> method.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="restrictionValues">The values to restrict.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of schema objects.</returns>
    public Task<MySqlSchemaCollection> GetSchemaCollectionAsync(string collectionName, string[] restrictionValues, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<MySqlSchemaCollection>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schema = GetSchemaCollection(collectionName, restrictionValues);
          result.SetResult(schema);
        }
        catch (Exception ex)
        {
          result.SetException(ex);
        }
      }
      else
      {
        result.SetCanceled();
      }
      return result.Task;
    }
    #endregion
  }

  /// <summary>
  /// Represents the method to handle the <see cref="MySqlConnection.FidoActionRequested"/> event of a 
  /// <see cref="MySqlConnection"/>
  /// </summary>
  public delegate void FidoActionCallback();

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
    /// Gets or sets an array of <see cref="MySqlError"/> objects set with the errors found.
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
