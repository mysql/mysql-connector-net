// Copyright © 2025, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MySql.EntityFrameworkCore.Migrations.Tests.TestData
{
  /// <inheritdoc />
  public partial class Bug37462099Migrations : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AlterDatabase()
        .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.CreateTable(
        name: "Bug37462099",
        columns: table => new
        {
          Id = table.Column<int>(type: "int", nullable: false).Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
          Name = table.Column<string>(type: "longtext", nullable: true),
          Created = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
        },
        constraints: table =>
        {
          table.PrimaryKey("PK_Bug37462099", x => x.Id);
        })
        .Annotation("MySQL:Charset", "utf8mb4");

      migrationBuilder.RenameColumn(
        name: "Name",
        table: "Bug37462099",
        newName: "ChangedName").Annotation("Relational:ColumnType", "LONGTEXT;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
        name: "Bug37462099");

      migrationBuilder.RenameColumn(
        name: "ChangedName",
        table: "Bug37462099",
        newName: "Name").Annotation("Relational:ColumnType", "LONGTEXT;");
    }
  }
}
