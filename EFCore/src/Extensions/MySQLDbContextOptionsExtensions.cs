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

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore
{
  /// <summary>
  /// MySQL specific extension methods for <see cref="DbContextOptionsBuilder" />.
  /// </summary>
  public static class MySQLDbContextOptionsExtensions
  {
    /// <summary>
    ///   <para>
    ///     Configures the context to connect to a MySQL database, but without initially setting any
    ///     <see cref="DbConnection" /> or connection string.
    ///   </para>
    ///   <para>
    ///     The connection or connection string must be set before the <see cref="DbContext" /> is used to connect
    ///     to a database. Set a connection using <see cref="RelationalDatabaseFacadeExtensions.SetDbConnection" />.
    ///     Set a connection string using <see cref="RelationalDatabaseFacadeExtensions.SetConnectionString" />.
    ///   </para>
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="mySqlOptionsAction">An optional action to enable additional MySQL server-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseMySQL(
      this DbContextOptionsBuilder optionsBuilder,
      Action<MySQLDbContextOptionsBuilder>? mySqlOptionsAction = null)
    {
      Check.NotNull(optionsBuilder, nameof(optionsBuilder));

      var extension = GetOrCreateExtension(optionsBuilder);

      ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
      ConfigureWarnings(optionsBuilder);
      mySqlOptionsAction?.Invoke(new MySQLDbContextOptionsBuilder(optionsBuilder));

      return optionsBuilder;
    }

    /// <summary>
    ///   Configures the context used to connect to a MySQL database.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="mySqlOptionsAction">An optional action to enable additional MySQL server-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseMySQL(
      this DbContextOptionsBuilder optionsBuilder,
      string connectionString,
      Action<MySQLDbContextOptionsBuilder>? mySqlOptionsAction = null)
    {
      Check.NotNull(optionsBuilder, nameof(optionsBuilder));
      Check.NotEmpty(connectionString, nameof(connectionString));

      var extension = (MySQLOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
      ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

      ConfigureWarnings(optionsBuilder);

      mySqlOptionsAction?.Invoke(new MySQLDbContextOptionsBuilder(optionsBuilder));

      return optionsBuilder;
    }

    /// <summary>
    ///   Configures the context to connect to a MySQL database.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///   An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///   in the open state, then Entity Framework (EF) will not open or close the connection. If the connection is in the closed
    ///   state, then EF opens and closes the connection as needed.
    /// </param>
    /// <param name="mySqlOptionsAction">An optional action to enable additional MySQL server-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseMySQL(this DbContextOptionsBuilder optionsBuilder,
      DbConnection connection,
      Action<MySQLDbContextOptionsBuilder>? mySqlOptionsAction = null)
    {
      Check.NotNull(optionsBuilder, nameof(optionsBuilder));
      Check.NotNull(connection, nameof(connection));

      var extension = (MySQLOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnection(connection);
      ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

      ConfigureWarnings(optionsBuilder);

      mySqlOptionsAction?.Invoke(new MySQLDbContextOptionsBuilder(optionsBuilder));

      return optionsBuilder;
    }

    /// <summary>
    ///   <para>
    ///     Configures the context to connect to a MySQL database, but without initially setting any
    ///     <see cref="DbConnection" /> or connection string.
    ///   </para>
    ///   <para>
    ///     The connection or connection string must be set before the <see cref="DbContext" /> is used to connect
    ///     to a database. Set a connection using <see cref="RelationalDatabaseFacadeExtensions.SetDbConnection" />.
    ///     Set a connection string using <see cref="RelationalDatabaseFacadeExtensions.SetConnectionString" />.
    ///   </para>
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="mySqlOptionsAction">An optional action to enable additional MySQL server-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseMySQL<TContext>(
      this DbContextOptionsBuilder<TContext> optionsBuilder,
      Action<MySQLDbContextOptionsBuilder>? mySqlOptionsAction = null)
      where TContext : DbContext
      => (DbContextOptionsBuilder<TContext>)UseMySQL(
        (DbContextOptionsBuilder)optionsBuilder, mySqlOptionsAction);

    /// <summary>
    ///   Configures the context to connect to a MySQL database.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="mySqlOptionsAction">An optional action to enable additional MySQL server-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseMySQL<TContext>(
      this DbContextOptionsBuilder<TContext> optionsBuilder,
      string connectionString,
      Action<MySQLDbContextOptionsBuilder>? mySqlOptionsAction = null)
      where TContext : DbContext
      => (DbContextOptionsBuilder<TContext>)UseMySQL(
        (DbContextOptionsBuilder)optionsBuilder, connectionString, mySqlOptionsAction);

    /// <summary>
    ///   Configures the context to connect to a MySQL database.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///   An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///   in the open state, then Entity Framework (EF) will not open or close the connection. If the connection is in the closed
    ///   state, then EF will open and close the connection as needed.
    /// </param>
    /// <param name="mySqlOptionsAction">An optional action to enable additional MySQL server-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseMySQL<TContext>(
     this DbContextOptionsBuilder<TContext> optionsBuilder,
     DbConnection connection,
     Action<MySQLDbContextOptionsBuilder>? mySqlOptionsAction = null)
     where TContext : DbContext
     => (DbContextOptionsBuilder<TContext>)UseMySQL(
         (DbContextOptionsBuilder)optionsBuilder, connection, mySqlOptionsAction);

    private static MySQLOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
    {
      var existing = optionsBuilder.Options.FindExtension<MySQLOptionsExtension>();
      return existing != null
        ? new MySQLOptionsExtension(existing)
        : new MySQLOptionsExtension();
    }

    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
      var coreOptionsExtension
      = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()
      ?? new CoreOptionsExtension();

      coreOptionsExtension = coreOptionsExtension.WithWarningsConfiguration(
      coreOptionsExtension.WarningsConfiguration.TryWithExplicit(
        RelationalEventId.AmbientTransactionWarning, WarningBehavior.Throw));

      ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
    }
  }
}