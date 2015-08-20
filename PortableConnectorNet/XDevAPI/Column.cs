using System;
using MySql.Protocol;
using MySql.Data;
using System.Text;

namespace MySql.XDevAPI
{
  public class Column
  {
    internal ValueDecoder _decoder;
    internal UInt64 _collationNumber;

    public string Name { get; internal set; }
    public string OriginalName { get; internal set; }
    public string Table { get; internal set; }
    public string OriginalTable { get; internal set; }

    public string Schema { get; internal set; }
    public string Catalog { get; internal set;  }
    public string Collation { get; internal set; }
    public UInt32 Length { get; internal set; }
    public UInt32 FractionalDigits { get; internal set; }
    public MySQLDbType DbType { get; internal set; }
    public Type ClrType { get; internal set; }

  }
}
