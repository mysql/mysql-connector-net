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

using System.Text;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Storage;

namespace MySql.Data.EntityFrameworkCore
{
  internal partial class MySQLUpdateSqlGenerator : UpdateSqlGenerator
  {
    protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification)
    {
      ThrowIf.Argument.IsNull(columnModification, "columnModification");
      ThrowIf.Argument.IsNull(commandStringBuilder, "commandStringBuilder");
      commandStringBuilder.AppendFormat("{0}=LAST_INSERT_ID()", SqlGenerationHelper.DelimitIdentifier(columnModification.ColumnName));
    }


    protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
    {
      ThrowIf.Argument.IsNull(commandStringBuilder, "commandStringBuilder");
      commandStringBuilder
        .Append("ROW_COUNT() = " + expectedRowsAffected)
        .AppendLine();
    }

    protected override ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string schemaName, int commandPosition)
    {
      ThrowIf.Argument.IsNull(commandStringBuilder, "commandStringBuilder");
      ThrowIf.Argument.IsEmpty(name, "name");


      commandStringBuilder
        .Append("SELECT ROW_COUNT()")
        .Append(SqlGenerationHelper.StatementTerminator)
        .AppendLine();

      return ResultSetMapping.LastInResultSet;
    }

    public enum ResultsGrouping
    {
      OneResultSet,
      OneCommandPerResultSet
    }
  }
}
