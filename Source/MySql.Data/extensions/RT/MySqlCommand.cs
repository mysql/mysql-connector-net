using System;
using System.ComponentModel;

namespace MySql.Data.MySqlClient
{
  public sealed partial class MySqlCommand : RTCommand
  {
  }

  public abstract class RTCommand
  {
    internal RTCommand() { }

    public abstract string CommandText { get; set; }
    public abstract int CommandTimeout { get; set; }
    public abstract CommandType CommandType { get; set; }

    public abstract int ExecuteNonQuery();
    public abstract object ExecuteScalar();
    public abstract void Prepare();
    public abstract void Cancel();
  }

  public enum CommandType
  {
    Text,
    StoredProcedure,
    TableDirect
  }

  public enum CommandBehavior
  {
    Default,
    SingleResult,
    SchemaOnly,
    KeyInfo,
    SingleRow,
    SequentialAccess,
    CloseConnection
  }
}
