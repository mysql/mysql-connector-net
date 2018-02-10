// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Migrations.Tests.Utilities;
using MySql.Data.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Metadata;
using MySql.Data.EntityFrameworkCore.Migrations;
using System.Diagnostics;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public partial class MySQLMigrationsGeneratorTest : MySQLMigrationsGeneratorTestBase
  {
    protected override IMigrationsSqlGenerator SqlGenerator
    {
      get
      {
        var typeMapper = new MySQLTypeMapper();

        var logger = new DiagnosticsLogger<DbLoggerCategory.Database.Command>(
          new LoggerFactory(),
          new LoggingOptions(),
          new DiagnosticListener("Fake"));

        var commandBuilderFactory = new RelationalCommandBuilderFactory(
          logger,
          typeMapper);

        var dependencies = new MigrationsSqlGeneratorDependencies(
          commandBuilderFactory,
          new MySQLSqlGenerationHelper(
            new Microsoft.EntityFrameworkCore.Storage.RelationalSqlGenerationHelperDependencies()),
          typeMapper);

        return new MySQLMigrationsSqlGenerator(dependencies);
      }
    }
  }
}
