// Copyright � 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MySql.Data.MySqlClient;


namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for MySqlPool.
  /// </summary>
  internal sealed class MySqlPool
  {
    private readonly List<Driver> _inUsePool;
    private readonly Queue<Driver> _idlePool;
    private readonly uint _minSize;
    private readonly uint _maxSize;
    private readonly AutoResetEvent _autoEvent;
    private int _available;

    private void EnqueueIdle(Driver driver)
    {
      driver.IdleSince = DateTime.Now;
      _idlePool.Enqueue(driver);
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
      _idlePool = new Queue<Driver>((int)_maxSize);

      // prepopulate the idle pool to minSize
      for (int i = 0; i < _minSize; i++)
        EnqueueIdle(CreateNewPooledConnection());

      ProcedureCache = new ProcedureCache((int)settings.ProcedureCacheSize);
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

    internal Dictionary<string,string> ServerProperties { get; set; }

    #endregion

    /// <summary>
    /// It is assumed that this method is only called from inside an active lock.
    /// </summary>
    private Driver GetPooledConnection()
    {
      Driver driver = null;

      // if we don't have an idle connection but we have room for a new
      // one, then create it here.
      lock ((_idlePool as ICollection).SyncRoot)
      {
        if (HasIdleConnections)
          driver = _idlePool.Dequeue();
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
          // if the user asks us to ping/reset pooled connections
          // do so now
          driver.Reset();
      }
      if (driver == null)
        driver = CreateNewPooledConnection();

      Debug.Assert(driver != null);
      lock ((_inUsePool as ICollection).SyncRoot)
      {
        _inUsePool.Add(driver);
      }
      return driver;
    }

    /// <summary>
    /// It is assumed that this method is only called from inside an active lock.
    /// </summary>
    private Driver CreateNewPooledConnection()
    {
      Debug.Assert((_maxSize - NumConnections) > 0, "Pool out of sync.");

      Driver driver = Driver.Create(Settings);
      driver.Pool = this;
      return driver;
    }

    public void ReleaseConnection(Driver driver)
    {
      lock ((_inUsePool as ICollection).SyncRoot)
      {
        if (_inUsePool.Contains(driver))
          _inUsePool.Remove(driver);
      }

      if (driver.ConnectionLifetimeExpired() || BeingCleared)
      {
        driver.Close();
        Debug.Assert(!_idlePool.Contains(driver));
      }
      else
      {
        lock ((_idlePool as ICollection).SyncRoot)
        {
          EnqueueIdle(driver);
        }
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
      lock ((_inUsePool as ICollection).SyncRoot)
      {
        if (_inUsePool.Contains(driver))
        {
          _inUsePool.Remove(driver);
          Interlocked.Increment(ref _available);
          _autoEvent.Set();
        }
      }

      // if we are being cleared and we are out of connections then have
      // the manager destroy us.
      if (BeingCleared && NumConnections == 0)
        MySqlPoolManager.RemoveClearedPool(this);
    }

    private Driver TryToGetDriver()
    {
      int count = Interlocked.Decrement(ref _available);
      if (count < 0)
      {
        Interlocked.Increment(ref _available);
        return null;
      }
      try
      {
        Driver driver = GetPooledConnection();
        return driver;
      }
      catch (Exception ex)
      {
        MySqlTrace.LogError(-1, ex.Message);
        Interlocked.Increment(ref _available);
        throw;
      }
    }

    public Driver GetConnection()
    {
      int fullTimeOut = (int)Settings.ConnectionTimeout * 1000;
      int timeOut = fullTimeOut;

      DateTime start = DateTime.Now;

      while (timeOut > 0)
      {
        Driver driver = TryToGetDriver();
        if (driver != null) return driver;

        // We have no tickets right now, lets wait for one.
#if NETSTANDARD1_3
        if (!_autoEvent.WaitOne(timeOut)) break;
#else
        if (!_autoEvent.WaitOne(timeOut, false)) break;
#endif

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
      lock ((_idlePool as ICollection).SyncRoot)
      {
        // first, mark ourselves as being cleared
        BeingCleared = true;

        // then we remove all connections sitting in the idle pool
        while (_idlePool.Count > 0)
        {
          Driver d = _idlePool.Dequeue();
          d.Close();
        }

        // there is nothing left to do here.  Now we just wait for all
        // in use connections to be returned to the pool.  When they are
        // they will be closed.  When the last one is closed, the pool will
        // be destroyed.
      }
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
      List<Driver> oldDrivers = new List<Driver>();
      DateTime now = DateTime.Now;

      lock ((_idlePool as ICollection).SyncRoot)
      {
        // The drivers appear to be ordered by their age, i.e it is
        // sufficient to remove them until the first element is not
        // too old.
        while (_idlePool.Count > _minSize)
        {
          Driver d = _idlePool.Peek();
          DateTime expirationTime = d.IdleSince.Add(
            new TimeSpan(0, 0, MySqlPoolManager.maxConnectionIdleTime));
          if (expirationTime.CompareTo(now) < 0)
          {
            oldDrivers.Add(d);
            _idlePool.Dequeue();
          }
          else
          {
            break;
          }
        }
      }
      return oldDrivers;
    }
  }
}
