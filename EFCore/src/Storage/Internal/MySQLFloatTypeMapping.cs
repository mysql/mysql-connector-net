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
using System.Data;
using System.Data.Common;
using System.Globalization;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLFloatTypeMapping : FloatTypeMapping
  {
    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public MySQLFloatTypeMapping(
      string storeType,
      DbType? dbType = System.Data.DbType.Single)
      : base(
          storeType,
          dbType)
    {
    }

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected MySQLFloatTypeMapping(RelationalTypeMappingParameters parameters)
      : base(parameters)
    {
    }

    /// <summary>
    ///   Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters">The parameters for this mapping.</param>
    /// <returns>The newly created mapping.</returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
      => new MySQLFloatTypeMapping(parameters);

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
      => ((float)value).ToString("R", CultureInfo.InvariantCulture);

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
    {
      base.ConfigureParameter(parameter);

      if (Size.HasValue && Size.Value != -1)
        parameter.Size = (byte)Size.Value;
    }
  }
}
