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

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Query;
using System;
using System.Data.Common;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLJsonTypeMapping<T> : MySQLJsonTypeMapping
  {
    public MySQLJsonTypeMapping(
      [NotNull] string storeType,
      [CanBeNull] ValueConverter valueConverter,
      [CanBeNull] ValueComparer valueComparer,
      [NotNull] IMySQLOptions options)
      : base(
          storeType,
          typeof(T),
          valueConverter,
          valueComparer,
          options)
    {
    }

    protected MySQLJsonTypeMapping(
      RelationalTypeMappingParameters parameters,
      MySqlDbType mySqlDbType)
      : base(parameters, mySqlDbType)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLJsonTypeMapping<T>(parameters, MySqlDbType);
  }

  internal abstract class MySQLJsonTypeMapping : MySQLStringTypeMapping
  {
    [NotNull]
    protected virtual IMySQLOptions? Options { get; }

    public MySQLJsonTypeMapping(
      [NotNull] string storeType,
      [NotNull] Type clrType,
      [CanBeNull] ValueConverter valueConverter,
      [CanBeNull] ValueComparer valueComparer,
      [NotNull] IMySQLOptions options)
      : base(
          new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(
              clrType,
              valueConverter,
              valueComparer),
            storeType,
            unicode: true),
          MySqlDbType.JSON)
    {
      if (storeType != "json")
        throw new ArgumentException($"The store type '{nameof(storeType)}' must be 'json'.", nameof(storeType));

      Options = options;
    }

    protected MySQLJsonTypeMapping(
      RelationalTypeMappingParameters parameters,
      MySqlDbType mySqlDbType)
      : base(parameters, mySqlDbType)
    {
    }

    protected override void ConfigureParameter(DbParameter parameter)
    {
      base.ConfigureParameter(parameter);

      if (parameter.Value is MySQLJsonString mySqlJsonString)
      {
        parameter.Value = (string)mySqlJsonString;
      }
    }
  }
}
