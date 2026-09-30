// Copyright © 2026, Oracle and/or its affiliates.
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
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;
using System;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public class MySQLIdempotentScriptTests
  {
    private const string Initial = "20260730000001_Initial";
    private const string Second = "20260730000002_AddDescription";

    // Bug #121043: execute the provider's actual script, including delimiter
    // directives, rather than testing only the text of its history guards.
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void Idempotent_scripts_apply_only_missing_migrations(bool noTransactions, bool customHistory)
    {
      var database = "idempotent_" + Guid.NewGuid().ToString("N");
      var historyTable = customHistory ? "migration`history" : "__EFMigrationsHistory";
      var delimitedHistory = "`" + historyTable.Replace("`", "``") + "`";
      using var connection = new MySqlConnection(MySQLTestStore.RootConnectionString);
      connection.Open();
      Execute(connection, $"CREATE DATABASE `{database}`;");
      connection.ChangeDatabase(database);
      try
      {
        var options = new DbContextOptionsBuilder<ScriptContext>()
          .UseMySQL(connection.ConnectionString, mysql => mysql.MigrationsHistoryTable(historyTable))
          .Options;
        using var context = new ScriptContext(options);
        var migrator = context.GetService<IMigrator>();
        var flags = MigrationsSqlGenerationOptions.Idempotent;
        if (noTransactions)
          flags |= MigrationsSqlGenerationOptions.NoTransactions;

        var up = migrator.GenerateScript(options: flags);
        Assert.That(up, Does.Contain("IF NOT EXISTS("));
        Assert.That(up, Does.Contain(") THEN"));
        Assert.That(up, Does.Contain("END IF;"));
        if (noTransactions)
          Assert.That(up, Does.Not.Contain("START TRANSACTION"));
        Assert.That(migrator.GenerateScript(), Does.Not.Contain("DELIMITER"));

        Execute(connection, "CREATE PROCEDURE `__EFMigrationsHistory_Migrate`() SIGNAL SQLSTATE '45000';");
        RunScript(connection, up);
        AssertState(connection, delimitedHistory, 2, "seed-up");
        RunScript(connection, up);
        AssertState(connection, delimitedHistory, 2, "seed-up");

        var downSecond = migrator.GenerateScript(Second, Initial, flags);
        RunScript(connection, downSecond);
        AssertState(connection, delimitedHistory, 1, "seed-up-down");
        RunScript(connection, downSecond);
        AssertState(connection, delimitedHistory, 1, "seed-up-down");

        // Applying the full script to a partially migrated database must skip
        // the initial CREATE TABLE and its non-idempotent seed INSERT.
        RunScript(connection, up);
        AssertState(connection, delimitedHistory, 2, "seed-up-down-up");
        RunScript(connection, up);
        AssertState(connection, delimitedHistory, 2, "seed-up-down-up");

        var down = migrator.GenerateScript(Second, Migration.InitialDatabase, flags);
        RunScript(connection, down);
        RunScript(connection, down);
        Assert.That(Scalar(connection, $"SELECT COUNT(*) FROM {delimitedHistory};"), Is.EqualTo(0));
        Assert.That(Scalar(connection,
          "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'Widgets';"),
          Is.EqualTo(0));

        RunScript(connection, up);
        AssertState(connection, delimitedHistory, 2, "seed-up");
      }
      finally
      {
        Execute(connection, $"DROP DATABASE `{database}`;");
      }
    }

    private static void AssertState(MySqlConnection connection, string history, int count, string name)
    {
      Assert.That(Scalar(connection, $"SELECT COUNT(*) FROM {history};"), Is.EqualTo(count));
      Assert.That(Scalar(connection, "SELECT COUNT(*) FROM Widgets;"), Is.EqualTo(1));
      Assert.That(Scalar(connection, "SELECT Name FROM Widgets WHERE Id = 1;"), Is.EqualTo(name));
      Assert.That(Scalar(connection,
        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE() AND routine_name = '__EFMigrationsHistory_Migrate';"),
        Is.EqualTo(0));
    }

    private static object Scalar(MySqlConnection connection, string sql)
    {
      using var command = new MySqlCommand(sql, connection);
      return command.ExecuteScalar();
    }

    private static void Execute(MySqlConnection connection, string sql)
    {
      using var command = new MySqlCommand(sql, connection);
      command.ExecuteNonQuery();
    }

    private static void RunScript(MySqlConnection connection, string sql)
      => new MySqlScript(connection, sql).Execute();

    public class ScriptContext : DbContext
    {
      public ScriptContext(DbContextOptions<ScriptContext> options) : base(options) { }
    }

    [DbContext(typeof(ScriptContext))]
    [Migration(Initial)]
    public class InitialMigration : Migration
    {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
        migrationBuilder.CreateTable("Widgets", columns => new
        {
          Id = columns.Column<int>(type: "int", nullable: false),
          Name = columns.Column<string>(type: "varchar(100)", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Widgets", x => x.Id));
        migrationBuilder.Sql("SET @seed = 'seed;$$';\n"
          + "INSERT INTO `Widgets` (`Id`, `Name`) VALUES (1, SUBSTRING_INDEX(@seed, ';', 1));");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable("Widgets");
    }

    [DbContext(typeof(ScriptContext))]
    [Migration(Second)]
    public class AddDescriptionMigration : Migration
    {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
        migrationBuilder.AddColumn<string>("Description", "Widgets", type: "varchar(100)", nullable: true);
        migrationBuilder.CreateIndex("IX_Widgets_Description", "Widgets", "Description");
        migrationBuilder.Sql("UPDATE `Widgets` SET `Name` = CONCAT(`Name`, '-up'), `Description` = 'added';",
          suppressTransaction: true);
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
        migrationBuilder.Sql("UPDATE `Widgets` SET `Name` = CONCAT(`Name`, '-down');", suppressTransaction: true);
        migrationBuilder.DropIndex("IX_Widgets_Description", "Widgets");
        migrationBuilder.DropColumn("Description", "Widgets");
      }
    }
  }
}
