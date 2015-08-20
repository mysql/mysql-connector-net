using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;

namespace MySql.Protocol.X
{
  internal class ByteDecoder : ValueDecoder
  {
    private Encoding _encoding;

    public override void DecodeMetadata()
    {
      if (Column.Collation == "utf8")
      {
        Column.DbType = MySQLDbType.VarChar;
        Column.ClrType = typeof(string);
      }
    }

    public override object GetClrValue(byte[] value)
    {
      switch (Column.DbType)
      {
        case MySQLDbType.VarChar:
        case MySQLDbType.Char:  return GetStringValue(value);
      }
      return null;
    }

    private string GetStringValue(byte[] value)
    {
      if (value.Length == 0) return null;

      if (_encoding == null)
        _encoding = CharSetMap.GetEncoding(Column.Collation);
      return _encoding.GetString(value, 0, value.Length - 1);
    }
  }
}
