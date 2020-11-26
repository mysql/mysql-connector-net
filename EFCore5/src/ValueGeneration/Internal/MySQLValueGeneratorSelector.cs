// Copyright (c) 2020 Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using MySql.EntityFrameworkCore.Utils;
using System;

namespace MySql.EntityFrameworkCore.ValueGeneration.Internal
{
  /// <summary>
  ///     <para>
  ///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
  ///         the same compatibility standards as public APIs. It may be changed or removed without notice in
  ///         any release. You should only use it directly in your code with extreme caution and knowing that
  ///         doing so can result in application failures when updating to a new Entity Framework Core release.
  ///     </para>
  ///     <para>
  ///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each
  ///         <see cref="DbContext" /> instance will use its own instance of this service.
  ///         The implementation may depend on other services registered with any lifetime.
  ///         The implementation does not need to be thread-safe.
  ///     </para>
  /// </summary>
  internal class MySQLValueGeneratorSelector : RelationalValueGeneratorSelector
  {
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public MySQLValueGeneratorSelector(
        [NotNull] ValueGeneratorSelectorDependencies dependencies)
        : base(dependencies)
    {
    }


    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override ValueGenerator Create(IProperty property, IEntityType entityType)
    {
      Check.NotNull(property, nameof(property));
      Check.NotNull(entityType, nameof(entityType));

      var ret = property.ClrType.UnwrapNullableType() == typeof(Guid)
          ? property.ValueGenerated == ValueGenerated.Never
            || property.GetDefaultValueSql() != null
              ? (ValueGenerator)new TemporaryGuidValueGenerator()
              : new SequentialGuidValueGenerator()
          : base.Create(property, entityType);
      return ret;
    }
  }
}
