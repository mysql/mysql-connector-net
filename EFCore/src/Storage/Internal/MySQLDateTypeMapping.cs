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

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  /// <summary>
  ///   <para>
  ///     Represents the mapping between a .NET <see cref="DateTime" /> type and a database type.
  ///   </para>
  ///   <para>
  ///     This type is typically used by database providers (and other extensions). It is generally
  ///     not used in application code.
  ///   </para>
  /// </summary>
  internal class MySQLDateTypeMapping : RelationalTypeMapping
  {
    private readonly bool _isDefaultValueCompatible;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public MySQLDateTypeMapping([NotNull] string storeType, Type clrType, bool isDefaultValueCompatible = false)
        : this(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(clrType),
                storeType,
                dbType: System.Data.DbType.Date))
    {
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    protected MySQLDateTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     Creates a copy of this mapping.
    /// </summary>
    /// <param name="parameters"> The parameters for this mapping. </param>
    /// <returns> The newly created mapping. </returns>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MySQLDateTypeMapping(parameters);

    /// <summary>
    ///     Creates a copy of this mapping.
    /// </summary>
    /// <param name="isDefaultValueCompatible"> Use a default value compatible syntax, or not. </param>
    /// <returns> The newly created mapping. </returns>
    public virtual RelationalTypeMapping Clone(bool isDefaultValueCompatible = false)
        => new MySQLDateTypeMapping(Parameters);

    /// <summary>
    ///     Gets the string format to be used to generate SQL literals of this type.
    /// </summary>
    protected override string SqlLiteralFormatString => $@"{(_isDefaultValueCompatible ? null : "DATE ")}'{{0:yyyy-MM-dd}}'";
  }
}
