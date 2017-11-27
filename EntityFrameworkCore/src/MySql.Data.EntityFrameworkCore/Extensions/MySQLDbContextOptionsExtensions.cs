// Copyright © 2015, 2017 Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using Microsoft.EntityFrameworkCore.Infrastructure;
using MySql.Data.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Infraestructure;
using System;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore
{
  /// <summary>
  /// Represents the context-option extensions implemented for MySQL.
  /// </summary>
  public static class MySQLDbContextOptionsExtensions
  {
    /// <summary>
    /// Configures the <see cref="DbContext" /> to use MySQL Sever.
    /// </summary>
    /// <param name="optionsBuilder">DbContext option builder.</param>
    /// <param name="connectionString">MySQL connection string.</param>
    /// <param name="MySQLOptionsAction">DbContext option builder action.</param>
    /// <returns>DbContext option builder using MySQL.</returns>
    public static DbContextOptionsBuilder UseMySQL(this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<MySQLDbContextOptionsBuilder> MySQLOptionsAction = null)
    {
      var extension = optionsBuilder.Options.FindExtension<MySQLOptionsExtension>();
      if (extension == null)
        extension = new MySQLOptionsExtension();
      extension = (MySQLOptionsExtension)extension.WithConnectionString(connectionString);

      IDbContextOptionsBuilderInfrastructure o = optionsBuilder as IDbContextOptionsBuilderInfrastructure;
      o.AddOrUpdateExtension(extension);

      MySQLOptionsAction?.Invoke(new MySQLDbContextOptionsBuilder(optionsBuilder));

      return optionsBuilder;
    }

    /// <summary>
    /// Configures the <see cref="DbContext" /> to use MySQL Sever.
    /// </summary>
    /// <param name="optionsBuilder">DbContext option builder.</param>
    /// <param name="connection">MySQL connection object.</param>
    /// <param name="MySQLOptionsAction">DbContext option builder action.</param>
    /// <returns>DbContext option builder using MySQL.</returns>
    public static DbContextOptionsBuilder UseMySQL(this DbContextOptionsBuilder optionsBuilder,
                                                        DbConnection connection,
                                                        Action<MySQLDbContextOptionsBuilder> MySQLOptionsAction = null)
    {
      var extension = GetOrCreateExtension(optionsBuilder);
      extension = (MySQLOptionsExtension)extension.WithConnection(connection);
      IDbContextOptionsBuilderInfrastructure o = optionsBuilder as IDbContextOptionsBuilderInfrastructure;
      o.AddOrUpdateExtension(extension);
      MySQLOptionsAction?.Invoke(new MySQLDbContextOptionsBuilder(optionsBuilder));
      return optionsBuilder;
    }

    /// <summary>
    /// Configures the <see cref="DbContext" /> to use MySQL Sever.
    /// </summary>
    /// <typeparam name="TContext"><see cref="DbContext" /> type.</typeparam>
    /// <param name="optionsBuilder">DbContext option builder.</param>
    /// <param name="connectionString">MySQL connection string.</param>
    /// <param name="MySQLOptionsAction">DbContext option builder action.</param>
    /// <returns>DbContext option builder using MySQL.</returns>
    public static DbContextOptionsBuilder<TContext> UseMySQL<TContext>(
       this DbContextOptionsBuilder<TContext> optionsBuilder,
       string connectionString,
       Action<MySQLDbContextOptionsBuilder> MySQLOptionsAction = null)
       where TContext : DbContext
       => (DbContextOptionsBuilder<TContext>)UseMySQL(
           (DbContextOptionsBuilder)optionsBuilder, connectionString, MySQLOptionsAction);

    /// <summary>
    /// Configures the <see cref="DbContext" /> to use MySQL Sever.
    /// </summary>
    /// <typeparam name="TContext"><see cref="DbContext" /> type.</typeparam>
    /// <param name="optionsBuilder">DbContext option builder.</param>
    /// <param name="connection">MySQL connection object.</param>
    /// <param name="MySQLOptionsAction">DbContext option builder action.</param>
    /// <returns>DbContext option builder using MySQL.</returns>
    public static DbContextOptionsBuilder<TContext> UseMySQL<TContext>(
     [NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder,
     [NotNull] DbConnection connection,
     [CanBeNull] Action<MySQLDbContextOptionsBuilder> MySQLOptionsAction = null)
     where TContext : DbContext
     => (DbContextOptionsBuilder<TContext>)UseMySQL(
         (DbContextOptionsBuilder)optionsBuilder, connection, MySQLOptionsAction);


    private static MySQLOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
    {
      var existing = optionsBuilder.Options.FindExtension<MySQLOptionsExtension>();
      return existing != null
          ? new MySQLOptionsExtension(existing)
          : new MySQLOptionsExtension();
    }

  }
}