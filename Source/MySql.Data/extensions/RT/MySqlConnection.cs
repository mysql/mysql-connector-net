using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  public delegate void StateChangeEventHandler(object sender, StateChangeEventArgs e);

  public sealed partial class MySqlConnection : RTConnection, ICloneable, IDisposable
  {
    public event StateChangeEventHandler StateChange;

    protected void OnStateChange(StateChangeEventArgs stateChange)
    {
      StateChangeEventHandler handler = StateChange;
      if (handler != null)
        handler(this, stateChange);
    }

    void IDisposable.Dispose()
    {
      if (State == ConnectionState.Open)
        Close();
    }
  }

  public abstract class RTConnection
  {
    internal RTConnection() { }

    public abstract string DataSource { get; }
    public abstract int ConnectionTimeout { get; }
    public abstract string Database { get; }
    public abstract ConnectionState State { get; }
    public abstract string ServerVersion { get; }
    public abstract string ConnectionString { get; set;  }

    public abstract void ChangeDatabase(string database);
    public abstract void Open();
    public abstract void Close();
  }

  public enum ConnectionState
  {
    Closed,
    Open,
    Connecting,
    Executing,
    Fetching,
    Broken
  }

}
