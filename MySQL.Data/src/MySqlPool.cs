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
using MySql.Data.Failover;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for MySqlPool.
  /// </summary>
  internal sealed class MySqlPool
  {
    private readonly List<Driver> _inUsePool;
    private readonly LinkedList<Driver> _idlePool;
    private readonly uint _minSize;
    private readonly uint _maxSize;
    private readonly AutoResetEvent _autoEvent;
    private int _available;
    // Object used to lock the list of host obtained from DNS SRV lookup.
    private readonly SemaphoreSlim inUsePoolSemaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim idlePoolSemaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim dnsSrvSemaphore = new SemaphoreSlim(1, 1);

    private void EnqueueIdle(Driver driver)
    {
      driver.IdleSince = DateTime.Now;
      _idlePool.AddLast(driver);
    }

    public MySqlPool(MySqlConnectionStringBuilder settings)
    {
      _minSize = settings.MinimumPoolSize;
      _maxSize = settings.MaximumPoolSize;

      _available = (int)_maxSize;
      _autoEvent = new AutoResetEvent(false);

      if (_minSize > _maxSize)
        _minSize = _maxSize;
      this.Settings = settings;
      _inUsePool = new List<Driver>((int)_maxSize);
      _idlePool = new LinkedList<Driver>();

      ProcedureCache = new ProcedureCache((int)settings.ProcedureCacheSize);
    }

    /// <summary>
    /// Initializes the pool by prepopulating the idle pool with connections up to the minimum pool size.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The initialized MySqlPool instance.</returns>
    private MySqlPool Initialize(CancellationToken cancellationToken)
    {
      // prepopulate the idle pool to minSize
      for (int i = 0; i < _minSize; i++)
        EnqueueIdle(CreateNewPooledConnection(cancellationToken));

      return this;
    }

    /// <summary>
    /// Asynchronously initializes the pool by prepopulating the idle pool with connections up to the minimum pool size.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning the initialized MySqlPool instance.</returns>
    private async Task<MySqlPool> InitializeAsync(CancellationToken cancellationToken)
    {
      // prepopulate the idle pool to minSize
      for (int i = 0; i < _minSize; i++)
        EnqueueIdle(await CreateNewPooledConnectionAsync(cancellationToken).ConfigureAwait(false));

      return this;
    }

    /// <summary>
    /// Creates and initializes a new MySqlPool using the given settings.
    /// </summary>
    /// <param name="settings">The connection string settings for the pool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The initialized MySqlPool instance.</returns>
    public static MySqlPool CreateMySqlPool(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
    {
      var pool = new MySqlPool(settings);
      return pool.Initialize(cancellationToken);
    }

    /// <summary>
    /// Asynchronously creates and initializes a new MySqlPool using the given settings.
    /// </summary>
    /// <param name="settings">The connection string settings for the pool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning the initialized MySqlPool instance.</returns>
    public static Task<MySqlPool> CreateMySqlPoolAsync(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
    {
      var pool = new MySqlPool(settings);
      return pool.InitializeAsync(cancellationToken);
    }

    #region Properties

    public MySqlConnectionStringBuilder Settings { get; set; }

    public ProcedureCache ProcedureCache { get; }

    /// <summary>
    /// It is assumed that this property will only be used from inside an active
    /// lock.
    /// </summary>
    private bool HasIdleConnections => _idlePool.Count > 0;

    private int NumConnections => _idlePool.Count + _inUsePool.Count;

    /// <summary>
    /// Indicates whether this pool is being cleared.
    /// </summary>
    public bool BeingCleared { get; private set; }

    internal Dictionary<string, string> ServerProperties { get; set; }

    #endregion

    /// <summary>
    /// It is assumed that this method is only called from inside an active lock.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The pooled Driver instance.</returns>
    /// <exception cref="MySqlException">Thrown if no connection is available within the timeout.</exception>
    private Driver GetPooledConnection(CancellationToken cancellationToken)
    {
      Driver driver = null;

      // if we don't have an idle connection but we have room for a new
      // one, then create it here.
      idlePoolSemaphore.Wait(cancellationToken);

      try
      {
        if (HasIdleConnections)
        {
          driver = _idlePool.First.Value;
          _idlePool.RemoveFirst();
        }
      }
      finally
      {
        idlePoolSemaphore.Release();
      }

      // Obey the connection timeout
      if (driver != null)
      {
        try
        {
          driver.ResetTimeout((int)Settings.ConnectionTimeout * 1000);
        }
        catch (Exception)
        {
          driver.Close();
          driver = null;
        }
      }

      if (driver != null)
      {
        // first check to see that the server is still alive
        if (!driver.Ping())
        {
          driver.Close();
          driver = null;
        }
        else if (Settings.ConnectionReset)
        {
          // if the user asks us to ping/reset pooled connections
          // do so now
          try { driver.Reset(); }
          catch (Exception) { Clear(); }
        }
      }
      if (driver == null)
        driver = CreateNewPooledConnection(cancellationToken);

      Debug.Assert(driver != null);
      inUsePoolSemaphore.Wait(cancellationToken);

      try
      {
        _inUsePool.Add(driver);
      }
      finally
      {
        inUsePoolSemaphore.Release();
      }

      return driver;
    }

    /// <summary>
    /// It is assumed that this method is only called from inside an active lock.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning the pooled Driver instance.</returns>
    /// <exception cref="MySqlException">Thrown if no connection is available within the timeout.</exception>
    private async Task<Driver> GetPooledConnectionAsync(CancellationToken cancellationToken)
    {
      Driver driver = null;

      // if we don't have an idle connection but we have room for a new
      // one, then create it here.
      await idlePoolSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
      try
      {
        if (HasIdleConnections)
        {
          driver = _idlePool.First.Value;
          _idlePool.RemoveFirst();
        }
      }
      finally
      {
        idlePoolSemaphore.Release();
      }

      // Obey the connection timeout
      if (driver != null)
      {
        try
        {
          driver.ResetTimeout((int)Settings.ConnectionTimeout * 1000);
        }
        catch (Exception)
        {
          await driver.CloseAsync().ConfigureAwait(false);
          driver = null;
        }
      }

      if (driver != null)
      {
        // first check to see that the server is still alive
        if (!await driver.PingAsync().ConfigureAwait(false))
        {
          await driver.CloseAsync().ConfigureAwait(false);
          driver = null;
        }
        else if (Settings.ConnectionReset)
        {
          // if the user asks us to ping/reset pooled connections
          // do so now
          try { await driver.ResetAsync().ConfigureAwait(false); }
          catch (Exception) { await ClearAsync().ConfigureAwait(false); }
        }
      }
      if (driver == null)
        driver = await CreateNewPooledConnectionAsync(cancellationToken).ConfigureAwait(false);

      Debug.Assert(driver != null);
      await inUsePoolSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
      try
      {
        _inUsePool.Add(driver);
      }
      finally
      {
        inUsePoolSemaphore.Release();
      }

      return driver;
    }

    /// <summary>
    /// It is assumed that this method is only called from inside an active lock.
    /// </summary>
    private Driver CreateNewPooledConnection(CancellationToken cancellationToken)
    {
      Debug.Assert((_maxSize - NumConnections) > 0, "Pool out of sync.");

      Driver driver = Driver.Create(Settings, cancellationToken);
      driver.Pool = this;
      return driver;
    }

    /// <summary>
    /// It is assumed that this method is only called from inside an active lock.
    /// </summary>
    private async Task<Driver> CreateNewPooledConnectionAsync(CancellationToken cancellationToken)
    {
      Debug.Assert((_maxSize - NumConnections) > 0, "Pool out of sync.");

      Driver driver = await Driver.CreateAsync(Settings, cancellationToken).ConfigureAwait(false);
      driver.Pool = this;
      return driver;
    }

    /// <summary>
    /// Releases a Driver (connection) back to the pool, removing it from in-use, reenqueuing to idle if valid (not expired or during clear), handling DNS SRV updates, and signaling availability.
    /// </summary>
    /// <param name="driver">The Driver to release.</param>
    public void ReleaseConnection(Driver driver)
    {
      inUsePoolSemaphore.Wait();
      try
      {
        if (_inUsePool.Contains(driver))
          _inUsePool.Remove(driver);
      }
      finally
      {
        inUsePoolSemaphore.Release();
      }

      if (driver.ConnectionLifetimeExpired() || BeingCleared)
      {
        driver.Close();
        Debug.Assert(!_idlePool.Contains(driver));
      }
      else
      {
        idlePoolSemaphore.Wait();
        try
        {
          EnqueueIdle(driver);
        }
        finally
        {
          idlePoolSemaphore.Release();
        }
      }

      dnsSrvSemaphore.Wait();
      try
      {
        if (driver.Settings.DnsSrv)
        {
          var dnsSrvRecords = DnsSrv.GetDnsSrvRecords(DnsSrv.ServiceName);
          FailoverManager.SetHostList(dnsSrvRecords.ConvertAll(r => new FailoverServer(r.Target, r.Port, null)),
            FailoverMethod.Sequential);

          foreach (var idleConnection in _idlePool)
          {
            string idleServer = idleConnection.Settings.Server;
            if (!FailoverManager.FailoverGroup.Hosts.Exists(h => h.Host == idleServer) && !idleConnection.IsInActiveUse)
            {
              idleConnection.Close();
            }
          }
        }
      }
      finally
      {
        dnsSrvSemaphore.Release();
      }

      Interlocked.Increment(ref _available);
      _autoEvent.Set();
    }

    /// <summary>
    /// Asynchronously releases a Driver (connection) back to the pool, removing it from in-use, reenqueuing to idle if valid (not expired or during clear), handling DNS SRV updates, and signaling availability.
    /// </summary>
    /// <param name="driver">The Driver to release.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReleaseConnectionAsync(Driver driver)
    {
      await inUsePoolSemaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        if (_inUsePool.Contains(driver))
          _inUsePool.Remove(driver);
      }
      finally
      {
        inUsePoolSemaphore.Release();
      }

      if (driver.ConnectionLifetimeExpired() || BeingCleared)
      {
        await driver.CloseAsync().ConfigureAwait(false);
        Debug.Assert(!_idlePool.Contains(driver));
      }
      else
      {
        await idlePoolSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
          EnqueueIdle(driver);
        }
        finally
        {
          idlePoolSemaphore.Release();
        }
      }

      await dnsSrvSemaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        if (driver.Settings.DnsSrv)
        {
          var dnsSrvRecords = DnsSrv.GetDnsSrvRecords(DnsSrv.ServiceName);
          FailoverManager.SetHostList(dnsSrvRecords.ConvertAll(r => new FailoverServer(r.Target, r.Port, null)),
            FailoverMethod.Sequential);

          foreach (var idleConnection in _idlePool)
          {
            string idleServer = idleConnection.Settings.Server;
            if (!FailoverManager.FailoverGroup.Hosts.Exists(h => h.Host == idleServer) && !idleConnection.IsInActiveUse)
            {
              await idleConnection.CloseAsync().ConfigureAwait(false);
            }
          }
        }
      }
      finally
      {
        dnsSrvSemaphore.Release();
      }

      Interlocked.Increment(ref _available);
      _autoEvent.Set();
    }

    /// <summary>
    /// Removes a connection from the in use pool.  The only situations where this method 
    /// would be called are when a connection that is in use gets some type of fatal exception
    /// or when the connection is being returned to the pool and it's too old to be 
    /// returned.
    /// </summary>
    /// <param name="driver"></param>
    public void RemoveConnection(Driver driver)
    {
      inUsePoolSemaphore.Wait();
      try
      {
        if (_inUsePool.Contains(driver))
        {
          _inUsePool.Remove(driver);
          Interlocked.Increment(ref _available);
          _autoEvent.Set();
        }
      }
      finally
      {
        inUsePoolSemaphore.Release();
      }

      // if we are being cleared and we are out of connections then have
      // the manager destroy us.
      if (BeingCleared && NumConnections == 0)
        MySqlPoolManager.RemoveClearedPool(this);
    }

    /// <summary>
    /// Private helper to attempt getting a Driver from the pool by decrementing availability counter; returns null if unavailable.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The pooled Driver if available, otherwise null.</returns>
    private Driver TryToGetDriver(CancellationToken cancellationToken)
    {
      int count = Interlocked.Decrement(ref _available);
      if (count < 0)
      {
        Interlocked.Increment(ref _available);
        return null;
      }
      try
      {
        Driver driver = GetPooledConnection(cancellationToken);
        return driver;
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(-1, ex.Message);
        Interlocked.Increment(ref _available);
        throw;
      }
    }

    /// <summary>
    /// Private asynchronous helper to attempt getting a Driver from the pool by decrementing availability counter; returns null if unavailable.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation, returning the pooled Driver if available, otherwise null.</returns>
    private async Task<Driver> TryToGetDriverAsync(CancellationToken cancellationToken)
    {
      int count = Interlocked.Decrement(ref _available);
      if (count < 0)
      {
        Interlocked.Increment(ref _available);
        return null;
      }
      try
      {
        Driver driver = await GetPooledConnectionAsync(cancellationToken).ConfigureAwait(false);
        return driver;
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(-1, ex.Message);
        Interlocked.Increment(ref _available);
        throw;
      }
    }

    /// <summary>
    /// Gets a Driver (connection) from the pool, waiting up to ConnectionTimeout if none available, throwing on timeout.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The pooled Driver.</returns>
    /// <exception cref="MySqlException">Thrown if no connection available within timeout.</exception>
    public Driver GetConnection(CancellationToken cancellationToken)
    {
      int fullTimeOut = (int)Settings.ConnectionTimeout * 1000;
      int timeOut = fullTimeOut;

      DateTime start = DateTime.Now;

      while (timeOut > 0)
      {
        Driver driver = TryToGetDriver(cancellationToken);
        if (driver != null) return driver;

        // We have no tickets right now, lets wait for one.
        if (!_autoEvent.WaitOne(timeOut, false)) break;

        timeOut = fullTimeOut - (int)DateTime.Now.Subtract(start).TotalMilliseconds;
      }
      throw new MySqlException(Resources.TimeoutGettingConnection);
    }

    /// <summary>
    /// Asynchronously gets a Driver (connection) from the pool, waiting up to ConnectionTimeout if none available, throwing on timeout.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation, returning the pooled Driver.</returns>
    /// <exception cref="MySqlException">Thrown if no connection available within timeout.</exception>
    public async Task<Driver> GetConnectionAsync(CancellationToken cancellationToken)
    {
      int fullTimeOut = (int)Settings.ConnectionTimeout * 1000;
      int timeOut = fullTimeOut;

      DateTime start = DateTime.Now;

      while (timeOut > 0)
      {
        Driver driver = await TryToGetDriverAsync(cancellationToken).ConfigureAwait(false);
        if (driver != null) return driver;

        // We have no tickets right now, lets wait for one.
        if (!_autoEvent.WaitOne(timeOut, false)) break;

        timeOut = fullTimeOut - (int)DateTime.Now.Subtract(start).TotalMilliseconds;
      }
      throw new MySqlException(Resources.TimeoutGettingConnection);
    }

    /// <summary>
    /// Clears this pool of all idle connections and marks this pool and being cleared
    /// so all other connections are closed when they are returned.
    /// </summary>
    internal void Clear()
    {
      idlePoolSemaphore.Wait();
      try
      {
        // first, mark ourselves as being cleared
        BeingCleared = true;

        // then we remove all connections sitting in the idle pool
        while (_idlePool.Count > 0)
        {
          Driver d = _idlePool.Last.Value;
          d.Close();
          _idlePool.RemoveLast();
        }
      }
      finally
      {
        idlePoolSemaphore.Release();
      }
      // there is nothing left to do here.  Now we just wait for all
      // in use connections to be returned to the pool.  When they are
      // they will be closed.  When the last one is closed, the pool will
      // be destroyed.
    }

    /// <summary>
    /// Clears this pool of all idle connections and marks this pool and being cleared
    /// so all other connections are closed when they are returned.
    /// </summary>
    internal async Task ClearAsync()
    {
      await idlePoolSemaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        // first, mark ourselves as being cleared
        BeingCleared = true;

        // then we remove all connections sitting in the idle pool
        while (_idlePool.Count > 0)
        {
          Driver d = _idlePool.Last.Value;
          await d.CloseAsync().ConfigureAwait(false);
          _idlePool.RemoveLast();
        }
      }
      finally
      {
        idlePoolSemaphore.Release();
      }
      // there is nothing left to do here.  Now we just wait for all
      // in use connections to be returned to the pool.  When they are
      // they will be closed.  When the last one is closed, the pool will
      // be destroyed.
    }

    /// <summary>
    /// Remove expired drivers from the idle pool
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Closing driver is a potentially lengthy operation involving network
    /// IO. Therefore we do not close expired drivers while holding 
    /// idlePool.SyncRoot lock. We just remove the old drivers from the idle
    /// queue and return them to the caller. The caller will need to close 
    /// them (or let GC close them)
    /// </remarks>
    internal List<Driver> RemoveOldIdleConnections()
    {
      var connectionsToClose = new List<Driver>();
      DateTime now = DateTime.Now;

      idlePoolSemaphore.Wait();

      try
      {
        while (_idlePool.Count > _minSize)
        {
          var idleConnection = _idlePool.First.Value;
          DateTime expirationTime = idleConnection.IdleSince.Add(
            new TimeSpan(0, 0, MySqlPoolManager.maxConnectionIdleTime));

          if (expirationTime.CompareTo(now) < 0)
          {
            connectionsToClose.Add(idleConnection);
            _idlePool.RemoveFirst();
          }
          else
            break;
        }
      }
      finally
      {
        idlePoolSemaphore.Release();
      }
      return connectionsToClose;
    }
  }
}
