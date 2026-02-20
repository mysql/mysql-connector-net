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

using MySql.Data.Failover;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
#if NET8_0_OR_GREATER
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
    private static SemaphoreSlim waitHandle = new(1, 1);

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

#if NET8_0_OR_GREATER
      AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).Unloading += UnloadAssemblyLoadContext;
#endif
    }

#if NET8_0_OR_GREATER
    private static void UnloadAssemblyLoadContext(AssemblyLoadContext obj) => UnloadPoolManager();
#endif

    private static void UnloadAppDomain(object sender, EventArgs e) => UnloadPoolManager();

    private static void UnloadPoolManager()
    {
      ClearAllPools();
      timer?.Dispose();
      AppDomain.CurrentDomain.ProcessExit -= UnloadAppDomain;
      AppDomain.CurrentDomain.DomainUnload -= UnloadAppDomain;
#if NET8_0_OR_GREATER
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

    /// <summary>
    /// Retrieves an existing connection pool for the given settings or creates a new one if none exists.
    /// </summary>
    /// <param name="settings">The connection string settings used to identify or create the pool.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The MySqlPool instance for the specified settings.</returns>
    public static MySqlPool GetPool(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
    {
      string text = GetKey(settings);
      waitHandle.Wait(cancellationToken);
      try
      {
        MySqlPool pool;
        Pools.TryGetValue(text, out pool);
        if (pool == null)
        {
          pool = MySqlPool.CreateMySqlPool(settings, cancellationToken);
          Pools.Add(text, pool);
        }
        else
          pool.Settings = settings;

        return pool;
      }
      finally
      {
        waitHandle.Release();
      }
    }

    /// <summary>
    /// Asynchronously retrieves an existing connection pool for the given settings or creates a new one if none exists, using the specified cancellation token.
    /// </summary>
    /// <param name="settings">The connection string settings used to identify or create the pool.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task containing the MySqlPool instance for the specified settings.</returns>
    public static async Task<MySqlPool> GetPoolAsync(MySqlConnectionStringBuilder settings, CancellationToken cancellationToken)
    {
      string text = GetKey(settings);

      await waitHandle.WaitAsync(CancellationToken.None).ConfigureAwait(false);
      try
      {
        MySqlPool pool;
        Pools.TryGetValue(text, out pool);
        if (pool == null)
        {
          pool = await MySqlPool.CreateMySqlPoolAsync(settings, cancellationToken).ConfigureAwait(false);
          Pools.Add(text, pool);
        }
        else
          pool.Settings = settings;

        return pool;
      }
      finally
      {
        waitHandle.Release();
      }
    }

    public static void RemoveConnection(Driver driver)
    {
      Debug.Assert(driver != null);

      MySqlPool pool = driver.Pool;

      pool?.RemoveConnection(driver);
    }

    /// <summary>
    /// Releases a driver (connection) back to its associated pool for reuse.
    /// </summary>
    /// <param name="driver">The Driver instance representing the connection to release.</param>
    public static void ReleaseConnection(Driver driver)
    {
      Debug.Assert(driver != null);

      MySqlPool pool = driver.Pool;

      if (pool != null)
        pool.ReleaseConnection(driver);
    }

    /// <summary>
    /// Asynchronously releases a driver (connection) back to its associated pool for reuse.
    /// </summary>
    /// <param name="driver">The Driver instance representing the connection to release.</param>
    public static async Task ReleaseConnectionAsync(Driver driver)
    {
      Debug.Assert(driver != null);

      MySqlPool pool = driver.Pool;

      if (pool != null)
        await pool.ReleaseConnectionAsync(driver).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears the connection pool associated with the given settings.
    /// </summary>
    /// <param name="settings">The connection string settings identifying the pool to clear.</param>
    public static void ClearPool(MySqlConnectionStringBuilder settings)
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

      ClearPoolByText(text);
    }

    /// <summary>
    /// Asynchronously clears the connection pool associated with the given settings.
    /// </summary>
    /// <param name="settings">The connection string settings identifying the pool to clear.</param>
    public static async Task ClearPoolAsync(MySqlConnectionStringBuilder settings)
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

      await ClearPoolByTextAsync(text).ConfigureAwait(false);
    }

    /// <summary>
    /// Internally clears the connection pool identified by the specified key.
    /// </summary>
    /// <param name="key">The key identifying the pool to clear.</param>
    private static void ClearPoolByText(string key)
    {
      waitHandle.Wait();

      try
      {
        // if pools doesn't have it, then this pool must already have been cleared
        if (!Pools.ContainsKey(key)) return;

        // add the pool to our list of pools being cleared
        MySqlPool pool = (Pools[key] as MySqlPool);
        ClearingPools.Add(pool);

        // now tell the pool to clear itself
        pool.Clear();

        // and then remove the pool from the active pools list
        Pools.Remove(key);
      }
      finally
      {
        waitHandle.Release();
      }
    }

    /// <summary>
    /// Asynchronously clears the connection pool identified by the specified key.
    /// </summary>
    /// <param name="key">The key identifying the pool to clear.</param>
    private static async Task ClearPoolByTextAsync(string key)
    {
      await waitHandle.WaitAsync().ConfigureAwait(false);
      try
      { 
        // if pools doesn't have it, then this pool must already have been cleared
        if (!Pools.ContainsKey(key)) return;

        // add the pool to our list of pools being cleared
        MySqlPool pool = (Pools[key] as MySqlPool);
        ClearingPools.Add(pool);

        // now tell the pool to clear itself
        await pool.ClearAsync().ConfigureAwait(false);

        // and then remove the pool from the active pools list
        Pools.Remove(key);
      }
      finally
      {
        waitHandle.Release();
      }
    }

    /// <summary>
    /// Clears all connection pools managed by the pool manager.
    /// </summary>
    public static void ClearAllPools()
    {


      try
      {
        // Create separate keys list.
        List<string> keys = new List<string>(Pools.Count);
        keys.AddRange(Pools.Keys);

        // Remove all pools by key.
        foreach (string key in keys)
          ClearPoolByText(key);
      }
      catch
      {
        throw;
      }

      if (DemotedServersTimer != null)
      {
        DemotedServersTimer.Dispose();
        Hosts?.Clear();
        while (!DemotedHosts.IsEmpty)
          DemotedHosts.TryDequeue(out _);
      }
    }

    /// <summary>
    /// Asynchronously clears all connection pools managed by the pool manager.
    /// </summary>
    public static async Task ClearAllPoolsAsync()
    {


      try
      {
        // Create separate keys list.
        List<string> keys = new List<string>(Pools.Count);
        keys.AddRange(Pools.Keys);

        // Remove all pools by key.
        foreach (string key in keys)
          await ClearPoolByTextAsync(key).ConfigureAwait(false);
      }
      catch
      {
        throw;
      }

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
        driver.Close();
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
