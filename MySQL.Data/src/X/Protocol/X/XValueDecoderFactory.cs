// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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


using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;

namespace MySqlX.Protocol.X
{
  internal class XValueDecoderFactory
  {
    public static ValueDecoder GetValueDecoder(Column c, Mysqlx.Resultset.ColumnMetaData.Types.FieldType type)
    {
      switch (type)
      {
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Bit: return new BitDecoder();
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Bytes: return new ByteDecoder(false);
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Enum: return new ByteDecoder(true);
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Set: return new SetDecoder();
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Time: return new XTimeDecoder();
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Datetime: return new XDateTimeDecoder();
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Sint: return new IntegerDecoder(true);
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Uint: return new IntegerDecoder(false);
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Float: return new FloatDecoder(true);
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Double: return new FloatDecoder(false);
        case Mysqlx.Resultset.ColumnMetaData.Types.FieldType.Decimal: return new DecimalDecoder();
      }
      throw new MySqlException("Unknown field type " + type.ToString());
    }
  }
}
