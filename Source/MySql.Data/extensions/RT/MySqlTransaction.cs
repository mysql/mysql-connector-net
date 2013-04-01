using System;

namespace MySql.Data.MySqlClient
{
  public sealed partial class MySqlTransaction : RTTransaction
  {
  }

  public abstract class RTTransaction
  {
    public abstract void Commit();
    public abstract void Rollback();
    public abstract IsolationLevel IsolationLevel { get; }
  }

  public enum IsolationLevel
  {
    Unspecified,
    Chaos,
    ReadUncommitted,
    ReadCommitted,
    RepeatableRead,
    Serializable,
    Snapshot
  }
}
