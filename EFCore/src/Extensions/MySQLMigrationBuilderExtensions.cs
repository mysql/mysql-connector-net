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

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Migrations.Operations;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   MySQL extension methods for <see cref="MigrationBuilder" />.
  /// </summary>
  public static class MySQLMigrationBuilderExtensions
  {
    /// <summary>
    ///   Indicates whether the database provider currently in use is the MySQL provider.
    /// </summary>
    /// <param name="migrationBuilder">The migrationBuilder from the parameters on <see cref="Migration.Up(MigrationBuilder)" /> 
    ///   or <see cref="Migration.Down(MigrationBuilder)" />.</param>
    /// <returns><see langword="true"/> if MySQL is being used; otherwise, <see langword="false"/>.</returns>
    public static bool IsMySql([NotNull] this MigrationBuilder migrationBuilder)
      => string.Equals(migrationBuilder.ActiveProvider,
      typeof(MySQLOptionsExtension).GetTypeInfo().Assembly.GetName().Name,
      StringComparison.Ordinal);

    internal static OperationBuilder<MySQLDropPrimaryKeyAndRecreateForeignKeysOperation> DropPrimaryKey(
      [NotNull] this MigrationBuilder migrationBuilder,
      [NotNull] string name,
      [NotNull] string table,
      [CanBeNull] string? schema = null,
      bool recreateForeignKeys = false)
    {
      Check.NotNull(migrationBuilder, nameof(migrationBuilder));
      Check.NotEmpty(name, nameof(name));
      Check.NotEmpty(table, nameof(table));

      var operation = new MySQLDropPrimaryKeyAndRecreateForeignKeysOperation
      {
        Schema = schema,
        Table = table,
        Name = name,
        RecreateForeignKeys = recreateForeignKeys,
      };
      migrationBuilder.Operations.Add(operation);

      return new OperationBuilder<MySQLDropPrimaryKeyAndRecreateForeignKeysOperation>(operation);
    }

    internal static OperationBuilder<MySQLDropUniqueConstraintAndRecreateForeignKeysOperation> DropUniqueConstraint(
      [NotNull] this MigrationBuilder migrationBuilder,
      [NotNull] string name,
      [NotNull] string table,
      [CanBeNull] string? schema = null,
      bool recreateForeignKeys = false)
    {
      Check.NotNull(migrationBuilder, nameof(migrationBuilder));
      Check.NotEmpty(name, nameof(name));
      Check.NotEmpty(table, nameof(table));

      var operation = new MySQLDropUniqueConstraintAndRecreateForeignKeysOperation
      {
        Schema = schema,
        Table = table,
        Name = name,
        RecreateForeignKeys = recreateForeignKeys,
      };
      migrationBuilder.Operations.Add(operation);

      return new OperationBuilder<MySQLDropUniqueConstraintAndRecreateForeignKeysOperation>(operation);
    }
  }
}
