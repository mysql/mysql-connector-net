// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text;

namespace MySql.Data.EntityFrameworkCore.Migrations.Internal
{
  internal partial class MySQLHistoryRepository : HistoryRepository
  {
    protected override string ExistsSql
    {
      get
      {
        var builder = new StringBuilder();

        builder.AppendLine("SELECT 1 FROM information_schema.tables ")
               .AppendLine("WHERE table_name = '")
               .Append(SqlGenerationHelper.EscapeLiteral(TableName))
               .Append("' AND table_schema = DATABASE()");

        return builder.ToString();
      }
    }


    protected override bool InterpretExistsResult(object value) => value != DBNull.Value;

    public override string GetBeginIfExistsScript(string migrationId)
    {
      ThrowIf.Argument.IsNull(migrationId, "migrationId");

      return new StringBuilder()
          .Append("IF EXISTS(SELECT * FROM ")
          .Append(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
          .Append(" WHERE ")
          .Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
          .Append(" = '")
          .Append(SqlGenerationHelper.EscapeLiteral(migrationId))
          .AppendLine("')")
          .Append("BEGIN")
          .ToString();
    }

    public override string GetBeginIfNotExistsScript(string migrationId)
    {
      ThrowIf.Argument.IsNull(migrationId, "migrationId");

      return new StringBuilder()
          .Append("IF NOT EXISTS(SELECT * FROM ")
          .Append(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
          .Append(" WHERE ")
          .Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
          .Append(" = '")
          .Append(SqlGenerationHelper.EscapeLiteral(migrationId))
          .AppendLine("')")
          .Append("BEGIN")
          .ToString();
    }

    public override string GetCreateIfNotExistsScript()
    {
      var builder = new StringBuilder();
      builder.AppendLine("  IF EXISTS(SELECT 1 FROM information_schema.tables ");
      builder.AppendLine("  WHERE table_name = '")
             .Append(SqlGenerationHelper.EscapeLiteral(TableName))
             .AppendLine("' AND table_schema = DATABASE()) ")
             .AppendLine("BEGIN")
             .AppendLine(GetCreateScript())
             .AppendLine("END;");
      return builder.ToString();
    }

    public override string GetEndIfScript() => "END;" + Environment.NewLine;

  }
}
