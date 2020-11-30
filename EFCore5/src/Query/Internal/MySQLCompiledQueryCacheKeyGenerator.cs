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

using System;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.Data.MySqlClient;

namespace MySql.EntityFrameworkCore.Query.Internal
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
  internal class MySQLCompiledQueryCacheKeyGenerator : RelationalCompiledQueryCacheKeyGenerator
  {
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public MySQLCompiledQueryCacheKeyGenerator(
        [NotNull] CompiledQueryCacheKeyGeneratorDependencies dependencies,
        [NotNull] RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override object GenerateCacheKey(Expression query, bool async)
    => new MySQLCompiledQueryCacheKey(
      GenerateCacheKeyCore(query, async),
      RelationalDependencies.ContextOptions.FindExtension<MySQLOptionsExtension>()?.CharSet);

    private readonly struct MySQLCompiledQueryCacheKey
    {
      private readonly RelationalCompiledQueryCacheKey _relationalCompiledQueryCacheKey;
      private readonly CharacterSet _charSet;

      public MySQLCompiledQueryCacheKey(
          RelationalCompiledQueryCacheKey relationalCompiledQueryCacheKey,
          CharacterSet charSet)
      {
        _relationalCompiledQueryCacheKey = relationalCompiledQueryCacheKey;
        _charSet = charSet;
      }

      public override bool Equals(object obj)
          => !(obj is null)
             && obj is MySQLCompiledQueryCacheKey key
             && Equals(key);

      private bool Equals(MySQLCompiledQueryCacheKey other)
        => _relationalCompiledQueryCacheKey.Equals(other._relationalCompiledQueryCacheKey);

      public override int GetHashCode()
          => HashCode.Combine(_relationalCompiledQueryCacheKey, _charSet);
    }
  }
}
