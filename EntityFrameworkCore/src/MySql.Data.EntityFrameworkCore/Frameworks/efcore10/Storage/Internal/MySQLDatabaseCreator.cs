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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Migrations.Operations;

namespace MySql.Data.EntityFrameworkCore
{
  /// <summary>
  /// Relational Database creator implementation in MySQL
  /// </summary>
  internal partial class MySQLDatabaseCreator : RelationalDatabaseCreator
  {
    public MySQLDatabaseCreator(
        [NotNull] MySQLServerConnection cxn,
        [NotNull] IMigrationsModelDiffer differ,
        [NotNull] IMigrationsSqlGenerator generator,
        [NotNull] IMigrationCommandExecutor migrationCommandExecutor,
        [NotNull] IModel model,
        [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder,
        [NotNull] IExecutionStrategyFactory executionStrategyFactory)
      : base(model, cxn, differ, generator, migrationCommandExecutor, executionStrategyFactory)
    {
      ThrowIf.Argument.IsNull(cxn, "connection");
      ThrowIf.Argument.IsNull(differ, "modelDiffer");
      ThrowIf.Argument.IsNull(generator, "generator");
      ThrowIf.Argument.IsNull(rawSqlCommandBuilder, "commandBuilder");

      _connection = cxn;
      _sqlGenerator = generator;
      _rawSqlCommandBuilder = rawSqlCommandBuilder;
      _migrationCommandExecutor = migrationCommandExecutor;
    }

  }
}