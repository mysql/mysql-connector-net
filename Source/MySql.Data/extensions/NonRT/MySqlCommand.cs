using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;

namespace MySql.Data.MySqlClient
{
  [ToolboxBitmap(typeof(MySqlCommand), "MySqlClient.resources.command.bmp")]
  [DesignerCategory("Code")]
  public sealed partial class MySqlCommand : DbCommand
  {
    partial void Constructor()
    {
      UpdatedRowSource = UpdateRowSource.Both;
    }

    partial void PartialClone(MySqlCommand clone)
    {
      clone.UpdatedRowSource = UpdatedRowSource;
    }

    /// <summary>
    /// Gets or sets how command results are applied to the DataRow when used by the 
    /// Update method of the DbDataAdapter. 
    /// </summary>
    public override UpdateRowSource UpdatedRowSource { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command object should be visible in a Windows Form Designer control. 
    /// </summary>
    [Browsable(false)]
    public override bool DesignTimeVisible { get; set; }

    protected override DbParameter CreateDbParameter()
    {
      return new MySqlParameter();
    }

    protected override DbConnection DbConnection
    {
      get { return Connection; }
      set { Connection = (MySqlConnection)value; }
    }

    protected override DbParameterCollection DbParameterCollection
    {
      get { return Parameters; }
    }

    protected override DbTransaction DbTransaction
    {
      get { return Transaction; }
      set { Transaction = (MySqlTransaction)value; }
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
      return ExecuteReader(behavior);
    }
  }
}
