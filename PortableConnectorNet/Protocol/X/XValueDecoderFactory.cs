using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Protocol.X
{
  internal class XValueDecoderFactory
  {
    public static ValueDecoder GetValueDecoder(Mysqlx.Resultset.ColumnMetaData.Types.FieldType type)
    {
      switch (type)
      {
//        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.BIT: return XBitDecoder();
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.BYTES: return new ByteDecoder();
        //      case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.DATETIME: return XDateTimeDecoder();
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.SINT:
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.UINT: return new IntegerDecoder();
      }
      throw new MySqlException("Unknown field type " + type.ToString());
    }
  }
}
