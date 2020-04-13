// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System.Globalization;
using MySql.Data.MySqlClient;
using MySql.Data.Common;

namespace MySql.Data.Types
{
  internal class MetaData
  {
    public static bool IsNumericType(string typename)
    {
      string lowerType = typename.ToLower(CultureInfo.InvariantCulture);

      switch (lowerType)
      {
        case "int":
        case "integer":
        case "numeric":
        case "decimal":
        case "dec":
        case "fixed":
        case "tinyint":
        case "mediumint":
        case "bigint":
        case "real":
        case "double":
        case "float":
        case "serial":
        case "smallint": return true;
      }
      return false;
    }

    public static bool IsTextType(string typename)
    {
      string lowerType = typename.ToLower(CultureInfo.InvariantCulture);

      switch (lowerType)
      {
        case "varchar":
        case "char":
        case "text":
        case "longtext":
        case "tinytext":
        case "mediumtext":
        case "nchar":
        case "nvarchar":
        case "enum":
        case "set":
          return true;
      }
      return false;
    }

    public static bool SupportScale(string typename)
    {
      string lowerType = StringUtility.ToLowerInvariant(typename);
      switch (lowerType)
      {
        case "numeric":
        case "decimal":
        case "dec":
        case "real": return true;
      }
      return false;
    }

    public static MySqlDbType NameToType(string typeName, bool unsigned,
       bool realAsFloat, MySqlConnection connection)
    {
      switch (StringUtility.ToUpperInvariant(typeName))
      {
        case "CHAR": return MySqlDbType.String;
        case "VARCHAR": return MySqlDbType.VarChar;
        case "DATE": return MySqlDbType.Date;
        case "DATETIME": return MySqlDbType.DateTime;
        case "NUMERIC":
        case "DECIMAL":
        case "DEC":
        case "FIXED":
          if (connection.driver.Version.isAtLeast(5, 0, 3))
            return MySqlDbType.NewDecimal;
          else
            return MySqlDbType.Decimal;
        case "YEAR":
          return MySqlDbType.Year;
        case "TIME":
          return MySqlDbType.Time;
        case "TIMESTAMP":
          return MySqlDbType.Timestamp;
        case "SET": return MySqlDbType.Set;
        case "ENUM": return MySqlDbType.Enum;
        case "BIT": return MySqlDbType.Bit;

        case "TINYINT":
          return unsigned ? MySqlDbType.UByte : MySqlDbType.Byte;
        case "BOOL":
        case "BOOLEAN":
          return MySqlDbType.Byte;
        case "SMALLINT":
          return unsigned ? MySqlDbType.UInt16 : MySqlDbType.Int16;
        case "MEDIUMINT":
          return unsigned ? MySqlDbType.UInt24 : MySqlDbType.Int24;
        case "INT":
        case "INTEGER":
          return unsigned ? MySqlDbType.UInt32 : MySqlDbType.Int32;
        case "SERIAL":
          return MySqlDbType.UInt64;
        case "BIGINT":
          return unsigned ? MySqlDbType.UInt64 : MySqlDbType.Int64;
        case "FLOAT": return MySqlDbType.Float;
        case "DOUBLE": return MySqlDbType.Double;
        case "REAL": return
           realAsFloat ? MySqlDbType.Float : MySqlDbType.Double;
        case "TEXT":
          return MySqlDbType.Text;
        case "BLOB":
          return MySqlDbType.Blob;
        case "LONGBLOB":
          return MySqlDbType.LongBlob;
        case "LONGTEXT":
          return MySqlDbType.LongText;
        case "MEDIUMBLOB":
          return MySqlDbType.MediumBlob;
        case "MEDIUMTEXT":
          return MySqlDbType.MediumText;
        case "TINYBLOB":
          return MySqlDbType.TinyBlob;
        case "TINYTEXT":
          return MySqlDbType.TinyText;
        case "BINARY":
          return MySqlDbType.Binary;
        case "VARBINARY":
          return MySqlDbType.VarBinary;
      }
      throw new MySqlException("Unhandled type encountered");
    }

  }
}
