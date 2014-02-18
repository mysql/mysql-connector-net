// Copyright © 2008, 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.Entity.Migrations.Sql;
using System.Data.Entity.Migrations.Model;
using MySql.Data.MySqlClient;
using System.Data.Metadata.Edm;

namespace MySql.Data.Entity
{
  /// <summary>
  /// Implementation of a MySql's Sql generator for EF 4.3 data migrations.
  /// </summary>
  public class MySqlMigrationSqlGenerator : MigrationSqlGenerator
  {
    private DbProviderManifest providerManifest;
    private Dictionary<string, OpDispatcher> dispatcher = new Dictionary<string, OpDispatcher>();

    private List<string> generatedTables { get; set; }
    private List<string> autoIncrementCols { get; set; }
    private List<string> primaryKeyCols { get; set; }
    delegate MigrationStatement OpDispatcher(MigrationOperation op);


    public MySqlMigrationSqlGenerator()
    {

      dispatcher.Add("AddColumnOperation", (OpDispatcher)((op) => { return Generate(op as AddColumnOperation); }));
      dispatcher.Add("AddForeignKeyOperation", (OpDispatcher)((op) => { return Generate(op as AddForeignKeyOperation); }));
      dispatcher.Add("AddPrimaryKeyOperation", (OpDispatcher)((op) => { return Generate(op as AddPrimaryKeyOperation); }));
      dispatcher.Add("AlterColumnOperation", (OpDispatcher)((op) => { return Generate(op as AlterColumnOperation); }));
      dispatcher.Add("CreateIndexOperation", (OpDispatcher)((op) => { return Generate(op as CreateIndexOperation); }));
      dispatcher.Add("CreateTableOperation", (OpDispatcher)((op) => { return Generate(op as CreateTableOperation); }));
      dispatcher.Add("DeleteHistoryOperation", (OpDispatcher)((op) => { return Generate(op as DeleteHistoryOperation); }));
      dispatcher.Add("DropColumnOperation", (OpDispatcher)((op) => { return Generate(op as DropColumnOperation); }));
      dispatcher.Add("DropForeignKeyOperation", (OpDispatcher)((op) => { return Generate(op as DropForeignKeyOperation); }));
      dispatcher.Add("DropIndexOperation", (OpDispatcher)((op) => { return Generate(op as DropIndexOperation); }));
      dispatcher.Add("DropPrimaryKeyOperation", (OpDispatcher)((op) => { return Generate(op as DropPrimaryKeyOperation); }));
      dispatcher.Add("DropTableOperation", (OpDispatcher)((op) => { return Generate(op as DropTableOperation); }));
      dispatcher.Add("InsertHistoryOperation", (OpDispatcher)((op) => { return Generate(op as InsertHistoryOperation); }));
      dispatcher.Add("MoveTableOperation", (OpDispatcher)((op) => { return Generate(op as MoveTableOperation); }));
      dispatcher.Add("RenameColumnOperation", (OpDispatcher)((op) => { return Generate(op as RenameColumnOperation); }));
      dispatcher.Add("RenameTableOperation", (OpDispatcher)((op) => { return Generate(op as RenameTableOperation); }));
      dispatcher.Add("SqlOperation", (OpDispatcher)((op) => { return Generate(op as SqlOperation); }));
      autoIncrementCols = new List<string>();
      primaryKeyCols = new List<string>();
    }

    public override IEnumerable<MigrationStatement> Generate(IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
    {
      List<MigrationStatement> stmts = new List<MigrationStatement>();
      MySqlConnection con = new MySqlConnection();
      providerManifest = DbProviderServices.GetProviderServices(con).GetProviderManifest(providerManifestToken);

      foreach (MigrationOperation op in migrationOperations)
      {
        OpDispatcher opdis = dispatcher[op.GetType().Name];
        stmts.Add(opdis(op));
      }
      return stmts;
    }

    protected virtual MigrationStatement Generate(AddColumnOperation op)
    {
      if (op == null) return null;

      MigrationStatement stmt = new MigrationStatement();
      stmt.Sql = string.Format("alter table `{0}` add column `{1}`",
        op.Table, op.Column.Name) + " " + Generate(op.Column);
      return stmt;
    }

    protected virtual MigrationStatement Generate(DropColumnOperation op)
    {
      if (op == null) return null;

      MigrationStatement stmt = new MigrationStatement();
      StringBuilder sb = new StringBuilder();
      stmt.Sql = string.Format("alter table `{0}` drop column `{1}`",
        op.Table, op.Name);
      return stmt;
    }

    protected virtual MigrationStatement Generate(AlterColumnOperation op)
    {
      if (op == null) return null;

      ColumnModel column = op.Column;
      StringBuilder sb = new StringBuilder();

      // for existing columns
      sb.Append("alter table `" + op.Table + "` modify `" + column.Name + "` ");

      // add definition
      sb.Append(Generate(column));

      return new MigrationStatement { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(RenameColumnOperation op)
    {
      if (op == null) return null;

      StringBuilder sb = new StringBuilder();

      sb.Append("set @columnType := (select case lower(IS_NULLABLE) when 'no' then CONCAT(column_type, ' not null ')  when 'yes' then column_type end from information_schema.columns where table_name = '" + op.Table + "' and column_name = '" + op.Name + "');");
      sb.AppendLine();
      sb.Append("set @sqlstmt := (select concat('alter table `" + op.Table + "` change `" + op.Name + "` `" + op.NewName + "` ' , @columnType));");
      sb.AppendLine();
      sb.Append("prepare stmt from @sqlstmt;");
      sb.AppendLine();
      sb.Append("execute stmt;");
      sb.AppendLine();
      sb.Append("deallocate prepare stmt;");
      return new MigrationStatement { Sql = sb.ToString() };

    }


    protected virtual MigrationStatement Generate(AddForeignKeyOperation op)
    {

      StringBuilder sb = new StringBuilder();
      string fkName = op.Name;
      if (fkName.Length > 64)
      {
        fkName = "FK_" + Guid.NewGuid().ToString().Replace("-", "");
      }
      sb.Append("alter table `" + TrimSchemaPrefix(op.DependentTable) + "` add constraint `" + TrimSchemaPrefix(fkName) + "` " +
                 " foreign key ");

      sb.Append("(" + string.Join(",", op.DependentColumns.Select(c => "`" + c + "`")) + ") ");
      sb.Append("references `" + op.PrincipalTable + "` ( " + string.Join(",", op.PrincipalColumns.Select(c => "`" + c + "`")) + ") ");
      
      if (op.CascadeDelete)
      {
        sb.Append(" on update cascade on delete cascade ");
      }

      return new MigrationStatement { Sql = sb.ToString() };
    }

    protected virtual string Generate(ColumnModel op)
    {
      TypeUsage typeUsage = providerManifest.GetStoreType(op.TypeUsage);
      StringBuilder sb = new StringBuilder();
      string type = MySqlProviderServices.Instance.GetColumnType(typeUsage);

      sb.Append(type);
      if (!type.EndsWith(")", StringComparison.InvariantCulture))
      {
        if ((op.ClrType == typeof(string)) ||
           ((op.ClrType == typeof(byte)) && (op.ClrType.IsArray)))
        {
          if (op.MaxLength.HasValue)
          {
            sb.AppendFormat("({0}) ", op.MaxLength.Value);
          }
        }
        else
        {
          if (op.Precision.HasValue && op.Scale.HasValue)
          {
            sb.AppendFormat("( {0}, {1} ) ", op.Precision.Value, op.Scale.Value);
          }
        }
      }

      if (!(op.IsNullable ?? true))
      {
        sb.Append(string.Format("{0} not null ", ((!primaryKeyCols.Contains(op.Name) && op.IsIdentity) ? " unsigned" : "")));
      }
      if (op.IsIdentity && (new string[] { "tinyint", "smallint", "mediumint", "int", "bigint" }).Contains(type.ToLower()))
      {
        sb.Append(" auto_increment ");
        autoIncrementCols.Add(op.Name);
      }
      if (!string.IsNullOrEmpty(op.DefaultValueSql))
      {
        sb.Append(string.Format(" default {0}", op.DefaultValueSql));
      }
      return sb.ToString();
    }

    protected virtual MigrationStatement Generate(DropForeignKeyOperation op)
    {
      StringBuilder sb = new StringBuilder();
      sb = sb.AppendFormat("alter table `{0}` drop foreign key `{1}`", op.DependentTable, op.Name);
      return new MigrationStatement { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(CreateIndexOperation op)
    {
      StringBuilder sb = new StringBuilder();

      sb = sb.Append("CREATE ");

      if (op.IsUnique)
      {
        sb.Append("UNIQUE ");
      }

      //index_col_name specification can end with ASC or DESC.
      // sort order are permitted for future extensions for specifying ascending or descending index value storage
      //Currently, they are parsed but ignored; index values are always stored in ascending order.

      object sort;
      op.AnonymousArguments.TryGetValue("Sort", out sort);
      var sortOrder = sort != null && sort.ToString() == "Ascending" ?
                      "ASC" : "DESC";

      sb.AppendFormat("index  `{0}` on `{1}` (", op.Name, op.Table);
      sb.Append(string.Join(",", op.Columns.Select(c => "`" + c + "` " + sortOrder)) + ") ");

      object indexTypeDefinition;
      op.AnonymousArguments.TryGetValue("Type", out indexTypeDefinition);
      var indexType = indexTypeDefinition != null && string.Compare(indexTypeDefinition.ToString(), "BTree", StringComparison.InvariantCultureIgnoreCase) > 0 ?
                      "BTREE" : "HASH";

      sb.Append("using " + indexType);

      return new MigrationStatement() { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(DropIndexOperation op)
    {
      return new MigrationStatement()
      {
        Sql = string.Format("alter table `{0}` drop index `{1}`",
          op.Table, op.Name)
      };
    }


    protected virtual MigrationStatement Generate(CreateTableOperation op)
    {
      StringBuilder sb = new StringBuilder();
      primaryKeyCols.Clear();
      autoIncrementCols.Clear();
      if (generatedTables == null)
        generatedTables = new List<string>();

      if (!generatedTables.Contains(op.Name))
      {
        generatedTables.Add(op.Name);
      }

      sb.Append("create table " + "`" + op.Name + "`" + " (");

      if (op.PrimaryKey != null)
      {
        op.PrimaryKey.Columns.ToList().ForEach(col => primaryKeyCols.Add(col));
      }
      //columns
      sb.Append(string.Join(",", op.Columns.Select(c => "`" + c.Name + "` " + Generate(c))));

      if (op.PrimaryKey != null)
      {
        sb.Append(",");
        sb.Append("primary key ( " + string.Join(",", op.PrimaryKey.Columns.Select(c => "`" + c + "`")) + ") ");
      }

      string keyFields = ",";
      autoIncrementCols.ForEach(col => keyFields += (!primaryKeyCols.Contains(col) ? string.Format(" KEY (`{0}`),", col) : ""));
      sb.Append(keyFields.Substring(0, keyFields.LastIndexOf(",")));

      sb.Append(") engine=InnoDb auto_increment=0");

      return new MigrationStatement() { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(DropTableOperation op)
    {
      return new MigrationStatement() { Sql = "drop table " + "`" + op.Name + "`" };
    }

    protected virtual MigrationStatement Generate(DeleteHistoryOperation op)
    {
      return new MigrationStatement { Sql = string.Format("delete from `{0}` where MigrationId = '{1}'", op.Table, op.MigrationId) };
    }

    protected virtual MigrationStatement Generate(AddPrimaryKeyOperation op)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("alter table `" + op.Table + "` add primary key ");
      sb.Append(" `" + op.Name + "` ");

      if (op.Columns.Count > 0)
        sb.Append("( " + string.Join(",", op.Columns.Select(c => "`" + c + "`")) + ") ");

      return new MigrationStatement { Sql = sb.ToString() };
    }


    protected virtual MigrationStatement Generate(DropPrimaryKeyOperation op)
    {
      object obj2;
      bool deleteAutoIncrement = false;
      StringBuilder sb = new StringBuilder();


      op.AnonymousArguments.TryGetValue("DeleteAutoIncrement", out obj2);
      if (obj2 != null)
        bool.TryParse(obj2.ToString(), out deleteAutoIncrement);

      if (deleteAutoIncrement && op.Columns.Count == 1)
      {
        var newColumn = new ColumnModel(PrimitiveTypeKind.Int32, null);
        newColumn.Name = op.Columns[0];
        var alterColumn = new AlterColumnOperation(op.Table, newColumn, false);
        var ms = Generate(alterColumn);
        sb.Append(ms.Sql + "; ");
      }

      return new MigrationStatement { Sql = sb.ToString() + " alter table `" + op.Table + "` drop primary key " };
    }


    protected virtual MigrationStatement Generate(InsertHistoryOperation op)
    {
      if (op == null) return null;

      StringBuilder sb = new StringBuilder();
      StringBuilder model = new StringBuilder();

      foreach (byte item in op.Model)
        model.Append(item.ToString("X2"));

      sb.Append("insert into `" + op.Table + "` (`migrationId`, `createdOn`, `model`, `productVersion`) ");
      sb.AppendFormat(" values ( '{0}', '{1}', {2}, '{3}' ) ",
                      op.MigrationId,
                      op.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                      "0x" + model.ToString(),
                      op.ProductVersion);

      return new MigrationStatement { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(RenameTableOperation op)
    {
      if (op == null) return null;

      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("rename table `{0}` to `{1}`", op.Name, op.NewName);
      return new MigrationStatement { Sql = sb.ToString() };
    }


    protected virtual MigrationStatement Generate(MoveTableOperation op)
    {
      return null; // TODO :check if we'll suppport this operation
    }

    protected virtual MigrationStatement Generate(SqlOperation op)
    {
      return new MigrationStatement { Sql = op.Sql, SuppressTransaction = op.SuppressTransaction };
    }

    private string TrimSchemaPrefix(string table)
    {
      if (table.StartsWith("dbo.") || table.Contains("dbo."))
        return table.Replace("dbo.", "");

      return table;
    }
  }
}


