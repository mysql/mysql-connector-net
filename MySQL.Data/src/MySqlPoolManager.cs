// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Failover;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for MySqlPoolManager.
  /// </summary>
  internal class MySqlPoolManager
  {
    private static readonly Dictionary<string, MySqlPool> Pools = new Dictionary<string, MySqlPool>();
    private static readonly List<MySqlPool> ClearingPools = new List<MySqlPool>();
    internal const int DEMOTED_TIMEOUT = 120000;

    #region Properties
    /// <summary>
    /// Queue of demoted hosts.
    /// </summary>
    internal static ConcurrentQueue<FailoverServer> DemotedHosts { get; set; }
    /// <summary>
    /// List of hosts that will be attempted to connect to.
    /// </summary>

    internal static List<FailoverServer> Hosts { get; set; }
    /// <summary>
    /// Timer to be used when a host have been demoted.
    /// </summary>

    internal static Timer DemotedServersTimer { get; set; }
    #endregion

    // Timeout in seconds, after which an unused (idle) connection 
    // should be closed.
    internal static int maxConnectionIdleTime = 180;

    static MySqlPoolManager()
    {
      AppDomain.CurrentDomain.ProcessExit += UnloadAppDomain;
      AppDomain.CurrentDomain.DomainUnload += UnloadAppDomain;

#if !NETFRAMEWORK
      AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).Unloading += UnloadAssemblyLoadContext;
#endif
    }

#if !NETFRAMEWORK
    private static void UnloadAssemblyLoadContext(AssemblyLoadContext obj) => UnloadPoolManager();
#endif

    private static void UnloadAppDomain(object sender, EventArgs e) => UnloadPoolManager();

    private static void UnloadPoolManager()
    {
      ClearAllPoolsAsync(false).GetAwaiter().GetResult();
      timer?.Dispose();
      AppDomain.CurrentDomain.ProcessExit -= UnloadAppDomain;
      AppDomain.CurrentDomain.DomainUnload -= UnloadAppDomain;
#if !NETFRAMEWORK
      AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).Unloading -= UnloadAssemblyLoadContext;
#endif
    }

    // we add a small amount to the due time to let the cleanup detect
    //expired connections in the first cleanup.
    private static Timer timer = new Timer(CleanIdleConnections,
      null, (maxConnectionIdleTime * 1000) + 8000, maxConnectionIdleTime * 1000);

    private static string GetKey(MySqlConnectionStringBuilder settings)
    {
      string key = "";
      lock (settings)
      {
        key = settings.ConnectionString;
      }

      if (!settings.IntegratedSecurity || settings.ConnectionReset) return key;

      try
      {
        // Append SID to the connection string to generate a key
        // With Integrated security different Windows users with the same
        // connection string may be mapped to different MySQL accounts.
        System.Security.Principal.WindowsIdentity id =
          System.Security.Principal.WindowsIdentity.GetCurrent();

        key += ";" + id.User;
      }
      catch (System.Security.SecurityException ex)
      {
        // Documentation for WindowsIdentity.GetCurrent() states 
        // SecurityException can be thrown. In this case the 
        // connection can only be pooled if reset is done.
        throw new MySqlException(Resources.NoWindowsIdentity, ex);
      }

      return key;
    }

    public static async Task<MySqlPool> GetPoolAsync(MySqlConnectionStringBuilder settings, bool execAsync, CancellationToken cancellationToken)
    {
      var poolKey = GetKey(settings);

      if (Pools.TryGetValue(poolKey, out var pool) == false)
      {
        var poolFactory = PoolFactory.GetFactory(settings);

        return await poolFactory.GetMySqlPoolAsync(settings, cancellationToken);
      }

      return pool;
    }

    private class PoolFactory
    {
      private readonly MySqlConnectionStringBuilder settings;
      private readonly string poolKey;

      readonly SemaphoreSlim tryStartCreatingPoolSemaphore = new SemaphoreSlim(1);
      readonly ConcurrentQueue<TaskCompletionSource<MySqlPool>> poolAwaiters = new ConcurrentQueue<TaskCompletionSource<MySqlPool>>();

      public PoolFactory(MySqlConnectionStringBuilder settings, string poolKey)
      {
        this.settings = settings;
        this.poolKey = poolKey;
      }

      static readonly ConcurrentDictionary<string, PoolFactory> Factories = new();

      public static PoolFactory GetFactory(MySqlConnectionStringBuilder settings)
      {
        var poolKey = GetKey(settings);

      GET_FACTORY:
        if (Factories.TryGetValue(poolKey, out var factory) == false)
        {
          if (Factories.TryAdd(poolKey, factory = new PoolFactory(settings, poolKey)) == false)
          {
            goto GET_FACTORY;
          }
        }

        return factory;
      }

      public async Task<MySqlPool> GetMySqlPoolAsync(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
      {
        TaskCompletionSource<MySqlPool> poolTaskCompletionSource = new TaskCompletionSource<MySqlPool>();

        if (Pools.TryGetValue(poolKey, out var pool) == false)
        {
          poolAwaiters.Enqueue(poolTaskCompletionSource);

          _ = TryStartCreatingPoolAsync();

          pool = await poolTaskCompletionSource.Task;

          pool.Settings = settings;

          return pool;
        }

        return pool;
      }

      public async Task TryStartCreatingPoolAsync()
      {
        MySqlPool pool;

        if (tryStartCreatingPoolSemaphore.Wait(0) == false)
        {
          return;
        }

        try
        {

          if (Pools.TryGetValue(poolKey, out pool) == false)
          {
            pool = await MySqlPool.CreateMySqlPoolAsync(settings, true, default).ConfigureAwait(false);

            Pools.Add(poolKey, pool);
          }

          DispatchPoolAwaiters(pool);
        }
        finally
        {
          tryStartCreatingPoolSemaphore.Release();
        }

        if (tryStartCreatingPoolSemaphore.Wait(0) == false)
        {
          return;
        }

        try
        {
          DispatchPoolAwaiters(pool);

          Factories.TryRemove(poolKey, out _);
        }
        finally
        {
          tryStartCreatingPoolSemaphore.Release();
        }
      }

      private void DispatchPoolAwaiters(MySqlPool pool)
      {
        while (poolAwaiters.TryDequeue(out var poolAwaiter))
        {
          poolAwaiter.SetResult(pool);
        }
      }
    }

    public static void RemoveConnection(Driver driver)
    {
      Debug.Assert(driver != null);

      MySqlPool pool = driver.Pool;

      pool?.RemoveConnection(driver);
    }

    public static async Task ReleaseConnectionAsync(Driver driver, bool execAsync)
    {
      Debug.Assert(driver != null);

      MySqlPool pool = driver.Pool;

      await pool?.ReleaseConnectionAsync(driver, execAsync);
    }

    public static async Task ClearPoolAsync(MySqlConnectionStringBuilder settings, bool execAsync)
    {
      Debug.Assert(settings != null);
      string text;
      try
      {
        text = GetKey(settings);
      }
      catch (MySqlException)
      {
        // Cannot retrieve windows identity for IntegratedSecurity=true
        // This can be ignored.
        return;
      }

      await ClearPoolByTextAsync(text, execAsync).ConfigureAwait(false);
    }

    private static async Task ClearPoolByTextAsync(string key, bool execAsync)
    {
      SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
      semaphoreSlim.Wait();

      // if pools doesn't have it, then this pool must already have been cleared
      if (!Pools.ContainsKey(key)) return;

      // add the pool to our list of pools being cleared
      MySqlPool pool = (Pools[key] as MySqlPool);
      ClearingPools.Add(pool);

      // now tell the pool to clear itself
      await pool.ClearAsync(execAsync).ConfigureAwait(false);

      // and then remove the pool from the active pools list
      Pools.Remove(key);

      semaphoreSlim.Release();
    }

    public static async Task ClearAllPoolsAsync(bool execAsync)
    {
      SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
      semaphoreSlim.Wait();

      // Create separate keys list.
      List<string> keys = new List<string>(Pools.Count);
      keys.AddRange(Pools.Keys);

      // Remove all pools by key.
      foreach (string key in keys)
        await ClearPoolByTextAsync(key, execAsync).ConfigureAwait(false);

      semaphoreSlim.Release();

      if (DemotedServersTimer != null)
      {
        DemotedServersTimer.Dispose();
        Hosts?.Clear();
        while (!DemotedHosts.IsEmpty)
          DemotedHosts.TryDequeue(out _);
      }
    }

    public static void RemoveClearedPool(MySqlPool pool)
    {
      Debug.Assert(ClearingPools.Contains(pool));
      ClearingPools.Remove(pool);
    }

    /// <summary>
    /// Remove drivers that have been idle for too long.
    /// </summary>
    public static async void CleanIdleConnections(object obj)
    {
      List<Driver> oldDrivers = new List<Driver>();

      lock (Pools)
      {
        foreach (MySqlPool pool in Pools.Keys.Select(key => Pools[key]))
        {
          oldDrivers.AddRange(pool.RemoveOldIdleConnections());
        }
      }

      foreach (Driver driver in oldDrivers)
      {
        await driver.CloseAsync(false).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Remove hosts that have been on the demoted list for more
    /// than 120,000 milliseconds and add them to the available hosts list.
    /// </summary>
    internal static void ReleaseDemotedHosts(object state)
    {
      while (!DemotedHosts.IsEmpty)
      {
        if (DemotedHosts.TryPeek(out FailoverServer demotedServer) &&
        demotedServer.DemotedTime.AddMilliseconds(DEMOTED_TIMEOUT) < DateTime.Now)
        {
          demotedServer.Attempted = false;
          Hosts?.Add(demotedServer);
          DemotedHosts.TryDequeue(out demotedServer);
        }
        else
        {
          break;
        }
      }

      DemotedServersTimer.Change(DEMOTED_TIMEOUT, Timeout.Infinite);
    }
  }
}