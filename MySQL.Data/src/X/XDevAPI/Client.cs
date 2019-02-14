// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;
using MySqlX.Common;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Class encapsulating a session pooling functionality.
  /// </summary>
  public class Client : IDisposable
  {
    private string _connectionString;
    private ConnectionOptions _connectionOptions;

    private List<Session> _inUse;
    private ConcurrentQueue<Session> _inIdle;
    private int _available;
    private AutoResetEvent _autoResetEvent;
    private Timer _idleTimer;
    private bool _isClosed = false;


    internal Client(object connectionString, object connectionOptions)
    {
      if (connectionString == null
        || (connectionString is string
        && string.IsNullOrWhiteSpace(connectionString as string)))
        throw new ArgumentNullException(nameof(connectionString));

      if (connectionOptions == null
        || (connectionOptions is string
        && string.IsNullOrWhiteSpace(connectionOptions as string)))
        throw new ArgumentNullException(nameof(connectionOptions));

      if (connectionString is string)
      {
        //Validates the connection string or Uri string
        new MySqlXConnectionStringBuilder(new ClientSession().ParseConnectionData(connectionString as string));
        _connectionString = connectionString as string;
      }
      else
      {
        MySqlXConnectionStringBuilder settings = new MySqlXConnectionStringBuilder();
        foreach (var item in Tools.GetDictionaryFromAnonymous(connectionString))
        {
          if (!settings.ContainsKey(item.Key))
            throw new KeyNotFoundException(string.Format(ResourcesX.InvalidConnectionStringAttribute, item.Key));
          settings.SetValue(item.Key, item.Value);
        }
        _connectionString = settings.ToString().Replace("\"", "");
      }

      _connectionOptions = ParseConnectionOptions(connectionOptions);

      // Pooling setup
      _inUse = new List<Session>(_connectionOptions.Pooling.MaxSize);
      _inIdle = new ConcurrentQueue<Session>();
      _available = _connectionOptions.Pooling.MaxSize;
      _autoResetEvent = new AutoResetEvent(false);
      _idleTimer = new Timer(new TimerCallback(CleanIdleConnections),
        null,
        _connectionOptions.Pooling.MaxIdleTime,
        _connectionOptions.Pooling.MaxIdleTime == 0 ? Timeout.Infinite : _connectionOptions.Pooling.MaxIdleTime);
    }

    private void CleanIdleConnections(object state)
    {
      List<Session> oldSessions = new List<Session>();
      while (!_inIdle.IsEmpty)
      {
        if (_inIdle.TryPeek(out Session session))
        {
          if (session.IdleSince.AddMilliseconds((double)_connectionOptions.Pooling.MaxIdleTime) < DateTime.Now)
          {
            if (_inIdle.TryDequeue(out Session session2))
            {
              // TODO: check if there're more than one session open
              //Debug.Assert(session.Equals(session2), "different sessions in CleanIdleConnections");
              oldSessions.Add(session2);
            }
          }
          else
          {
            break;
          }
        }
      }

      foreach (Session session in oldSessions)
      {
        try
        {
          // tries to close the session
          session.CloseFully();
        }
        catch { }
      }
    }

    /// <summary>
    /// Get a session from pool or create a new one.
    /// </summary>
    /// <returns></returns>
    public Session GetSession()
    {
      if (_isClosed)
        throw new MySqlException(ResourcesX.ClientIsClosed);

      if (!_connectionOptions.Pooling.Enabled)
      {
        return new Session(_connectionString);
      }

      int fullQueueTimeout = _connectionOptions.Pooling.QueueTimeout;
      int queueTimeout = fullQueueTimeout;
      Stopwatch stopwatch = Stopwatch.StartNew();

      while (queueTimeout >= 0)
      {
        Session session = TryToGetSession();
        if (session != null)
          return session;

        if (!_autoResetEvent.WaitOne(fullQueueTimeout == 0 ? -1 : queueTimeout))
          break;

        queueTimeout = fullQueueTimeout - (int)stopwatch.ElapsedMilliseconds;
      }

      stopwatch.Stop();
      throw new TimeoutException(ResourcesX.PoolingQueueTimeout);
    }

    private Session TryToGetSession()
    {
      Debug.Assert(_inUse.Count + _inIdle.Count <= _connectionOptions.Pooling.MaxSize, "pool out of sync");
      int count = Interlocked.Decrement(ref _available);
      if (count < 0)
      {
        Interlocked.Increment(ref _available);
        return null;
      }
      try
      {
        Session session = GetPooledSession();
        return session;
      }
      catch (Exception ex)
      {
        Interlocked.Increment(ref _available);
        throw;
      }
    }

    private Session GetPooledSession()
    {
      Session session = null;

      while (!_inIdle.IsEmpty && session == null)
      {
        if (_inIdle.TryDequeue(out session))
        {
          try
          {
            session.XSession.Authenticate();
            session.XSession.SetState(SessionState.Open, false);
          }
          catch
          {
            session = null;
          }
        }
      }

      if (session == null)
      {
        session = CreateNewSession();
      }

      Debug.Assert(session != null, "pooled session is null");
      _inUse.Add(session);

      return session;
    }

    private Session CreateNewSession()
    {
      return new Session(_connectionString, this);
    }

    internal void ReleaseSession(BaseSession session)
    {
      Session newSession = new Session(session.InternalSession, this);
      if (_inUse.Contains((Session)session))
      {
        _inUse.Remove((Session)session);
        Interlocked.Increment(ref _available);
      }

      try
      {
        newSession.Reset();
        newSession.IdleSince = DateTime.Now;
        _inIdle.Enqueue(newSession);
      }
      catch
      {
        newSession = null;
      }
      _autoResetEvent.Set();
    }

    /// <summary>
    /// Closes all sessions the Client object created and destroys the managed pool.
    /// </summary>
    public void Close()
    {
      if (_isClosed) return;

      _isClosed = true;
      _idleTimer.Change(0, Timeout.Infinite);
      foreach (Session session in _inUse)
      {
        try
        {
          session.CloseFully();
        }
        catch { }
      }
      while (!_inIdle.IsEmpty)
      {
        if (_inIdle.TryDequeue(out Session session))
        {
          try
          {
            session.CloseFully();
          }
          catch { }
        }
      }
      Interlocked.Exchange(ref _available, -1);
    }

    #region Internals

    private class ClientSession : BaseSession
    {
    }

    internal class ConnectionOptions
    {
      public PoolingStruct Pooling { get; set; } =
        new PoolingStruct
        {
          Enabled = true,
          MaxSize = 25
        };

      internal class PoolingStruct
      {
        public bool Enabled { get; set; }

        private int _maxSize;
        public int MaxSize
        {
          get { return _maxSize; }
          set
          {
            if (value <= 0) throw new ArgumentException(nameof(MaxSize));
            _maxSize = value;
          }
        }

        private int _maxIdleTime;
        public int MaxIdleTime
        {
          get { return _maxIdleTime; }
          set
          {
            if (value < 0) throw new ArgumentException(nameof(MaxIdleTime));
            _maxIdleTime = value;
          }
        }

        private int _queueTimeout;
        public int QueueTimeout
        {
          get { return _queueTimeout; }
          set
          {
            if (value < 0) throw new ArgumentException(nameof(QueueTimeout));
            _queueTimeout = value;
          }
        }
      }

      public override bool Equals(object obj)
      {
        ConnectionOptions connectionOptions = obj as ConnectionOptions;
        if (connectionOptions == null)
          return false;
        return Equals(this, obj);
      }

      private bool Equals(object x, object y)
      {
        if (x.GetType() != y.GetType())
          return false;
        foreach (var property in x.GetType().GetProperties())
        {
          if (property.PropertyType.IsNested)
            return Equals(property.GetValue(x), property.GetValue(y));
          if (!property.GetValue(x).Equals(property.GetValue(y)))
            return false;
        }
        return true;
      }

      public override int GetHashCode()
      {
        return base.GetHashCode();
      }
    }

    internal static ConnectionOptions ParseConnectionOptions(object connectionOptions)
    {
      DbDoc options;
      try
      {
        options = new DbDoc(connectionOptions);
      }
      catch (Exception ex)
      {
        throw new ArgumentException(string.Format(ResourcesX.ClientOptionNotValid, "JSON"), ex);
      }
      if (options == null
        || options.values.Count == 0)
        throw new ArgumentException(string.Format(ResourcesX.ClientOptionNotValid, connectionOptions));

      ConnectionOptions instance = new ConnectionOptions();
      Type optionType = instance.GetType();
      foreach (var item in options.values)
      {
        PropertyInfo parent = optionType.GetProperty(item.Key,
          BindingFlags.Instance
          | BindingFlags.Public
          | BindingFlags.IgnoreCase);
        if (parent == null)
          throw new ArgumentException(string.Format(ResourcesX.ClientOptionNotValid, item.Key));

        object target = parent.GetValue(instance);
        if (parent.DeclaringType.IsNested)
        {
          DbDoc children;
          try
          {
            children = new DbDoc(item.Value);
          }
          catch (Exception ex)
          {
            throw new ArgumentException(string.Format(ResourcesX.ClientOptionInvalidValue, item.Key, "JSON"), ex);
          }
          if (children == null
            || children.values.Count == 0)
            throw new ArgumentException(string.Format(ResourcesX.ClientOptionInvalidValue, item.Key, item.Value));
          foreach (var option in children.values)
          {
            var key = parent.PropertyType.GetProperty(option.Key,
              BindingFlags.Instance
              | BindingFlags.Public
              | BindingFlags.IgnoreCase);
            if (key == null)
              throw new ArgumentException(string.Format(ResourcesX.ClientOptionNotValid,
                $"{item.Key}.{option.Key}"));
            try
            {
              key.SetValue(target, option.Value);
            }
            catch (Exception ex)
            {
              object value = option.Value;
              switch (value)
              {
                case MySqlExpression expr:
                  value = ((MySqlExpression)expr).value.Trim();
                  break;
              }
              throw new ArgumentException(string.Format(ResourcesX.ClientOptionInvalidValue,
                $"{item.Key}.{option.Key}",
                value),
                ex);
            }
          }
        }

        parent.SetValue(instance, target);
      }
      return instance;
    }

    #endregion

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          Close();
          _idleTimer.Dispose();
          _inUse.Clear();
        }

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~Client() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }
}
