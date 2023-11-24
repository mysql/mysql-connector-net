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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLGeometryTypeMapping : MySQLTypeMapping
  {
    protected MySQLGeometryTypeMapping(RelationalTypeMappingParameters parameters)
    : base(parameters)
    {
    }

#if NET8_0
    public CoreTypeMapping Clone(ValueConverter? converter)
      => new MySQLGeometryTypeMapping(Parameters.WithComposedConverter(converter,null,null,null,null));
#else
    public override CoreTypeMapping Clone(ValueConverter? converter)
      => new MySQLGeometryTypeMapping(Parameters.WithComposedConverter(converter));
#endif

    protected override void ConfigureParameter(DbParameter parameter)
    {
      MySqlParameter mysqlParameter = (MySqlParameter)parameter;
      mysqlParameter.MySqlDbType = MySqlDbType.Geometry;
    }

    public override MethodInfo GetDataReaderMethod()
    {
      return typeof(MySqlDataReader).GetMethod("GetMySqlGeometry", new Type[] { typeof(int) })!;
    }

    public MySQLGeometryTypeMapping(
      string storeType,
      DbType dbType = System.Data.DbType.Binary)
      : base(storeType, typeof(Data.Types.MySqlGeometry), dbType)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLGeometryTypeMapping(parameters);
  }
}