// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Data.Common;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLDateTimeTypeMapping : DateTimeTypeMapping
  {
    private const string _dateTimeFormatConst = "'{0:yyyy-MM-dd HH:mm:ss.fff}'";
    private const string _dateFormatConst = "'{0:yyyy-MM-dd}'";

    // Note: this array will be accessed using the precision as an index
    // so the order of the entries in this array is important
    private readonly string[] _dateTimeFormats =
    {
        "'{0:yyyy-MM-dd HH:mm:ss}'",
        "'{0:yyyy-MM-dd HH:mm:ss.f}'",
        "'{0:yyyy-MM-dd HH:mm:ss.ff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.fff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.ffff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.fffff}'",
        "'{0:yyyy-MM-dd HH:mm:ss.ffffff}'"
    };

    public MySQLDateTimeTypeMapping(
      string storeType,
      int? precision = null,
      DbType? dbType = System.Data.DbType.DateTime,
      StoreTypePostfix storeTypePostfix = StoreTypePostfix.Precision)
      : base(
        new RelationalTypeMappingParameters(
          new CoreTypeMappingParameters(typeof(DateTime)),
          storeType,
          storeTypePostfix,
          dbType,
          precision: precision))
    {
    }

    protected MySQLDateTimeTypeMapping(RelationalTypeMappingParameters parameters)
      : base(parameters)
    {
    }

    /// <summary>
    ///   Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters"> The parameters for this mapping. </param>
    /// <returns> The newly created mapping. </returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLDateTimeTypeMapping(parameters);

    protected override string SqlLiteralFormatString
    {
      get
      {
        switch (StoreType)
        {
          case "date":
            return _dateFormatConst;
          case "datetime":
            return _dateTimeFormatConst;
          case "timestamp":
            return $"({_dateTimeFormatConst})";
          default:
            if (Precision.HasValue)
            {
              var precision = Precision.Value;
              if (precision <= 6 && precision >= 0)
                return _dateTimeFormats[precision];
            }

            return _dateTimeFormats[6];
        }
      }
    }

    protected override void ConfigureParameter([NotNull] DbParameter parameter)
    {
      if (parameter.Value!.GetType() == typeof(DateTimeOffset))
      {
        parameter.Value = ((DateTimeOffset)parameter.Value).DateTime;
      }
    }
  }
}