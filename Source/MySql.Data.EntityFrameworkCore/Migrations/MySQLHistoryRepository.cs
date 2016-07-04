// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;

namespace MySQL.Data.Entity.Migrations
{
  class MySQLHistoryRepository : HistoryRepository
  {
    public MySQLHistoryRepository(
        IDatabaseCreator databaseCreator,
        ISqlCommandBuilder sqlCommandBuilder,
        MySQLConnection connection,
        IDbContextOptions options,
        IMigrationsModelDiffer modelDiffer,
        MySQLMigrationsSqlGenerator sqlGenerator,
        IRelationalAnnotationProvider annotations,
        ISqlGenerator sql)
            : base(
                  databaseCreator,
                  sqlCommandBuilder,
                  connection,
                  options,
                  modelDiffer,
                  sqlGenerator,
                  annotations,
                  sql)
    {
    }

    protected override string ExistsSql
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public override string GetBeginIfExistsScript(string migrationId)
    {
      throw new NotImplementedException();
    }

    public override string GetBeginIfNotExistsScript(string migrationId)
    {
      throw new NotImplementedException();
    }

    public override string GetCreateIfNotExistsScript()
    {
      throw new NotImplementedException();
    }

    public override string GetEndIfScript()
    {
      throw new NotImplementedException();
    }

    protected override bool InterpretExistsResult([NotNull] object value)
    {
      throw new NotImplementedException();
    }
  }
}
