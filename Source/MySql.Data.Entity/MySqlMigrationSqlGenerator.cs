// Copyright © 2008, 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Utilities;
using System.Collections;

namespace MySql.Data.Entity
{

  /// <summary>
  /// Class used to customized code generation
  /// to avoid dbo. prefix added on table names.
  /// </summary>
  public class MySqlMigrationCodeGenerator : CSharpMigrationCodeGenerator
  {

    private string TrimSchemaPrefix(string table)
    {
      if (table.StartsWith("dbo."))
        return table.Replace("dbo.", "");

      return table;
    }

    protected override void Generate(AddColumnOperation addColumnOperation, IndentedTextWriter writer)
    {
      var add = new AddColumnOperation(TrimSchemaPrefix(addColumnOperation.Table), addColumnOperation.Column);
      base.Generate(add, writer);
    }

    protected override void Generate(AddForeignKeyOperation addForeignKeyOperation, IndentedTextWriter writer)
    {
      addForeignKeyOperation.PrincipalTable = TrimSchemaPrefix(addForeignKeyOperation.PrincipalTable);
      addForeignKeyOperation.DependentTable = TrimSchemaPrefix(addForeignKeyOperation.DependentTable);      
      base.Generate(addForeignKeyOperation, writer);
    }

    protected override void Generate(AddPrimaryKeyOperation addPrimaryKeyOperation, IndentedTextWriter writer)
    {
      addPrimaryKeyOperation.Table = TrimSchemaPrefix(addPrimaryKeyOperation.Table);
      base.Generate(addPrimaryKeyOperation, writer);
    }   

    protected override void Generate(AlterColumnOperation alterColumnOperation, IndentedTextWriter writer)
    {    
      AlterColumnOperation alter = null;
      if (alterColumnOperation.Inverse != null)
        alter = new AlterColumnOperation(TrimSchemaPrefix(alterColumnOperation.Table), alterColumnOperation.Column, alterColumnOperation.IsDestructiveChange, (AlterColumnOperation)alterColumnOperation.Inverse);
      else
        alter = new AlterColumnOperation(TrimSchemaPrefix(alterColumnOperation.Table), alterColumnOperation.Column, alterColumnOperation.IsDestructiveChange);

      if (alter != null)
        base.Generate(alter, writer);
      else
        base.Generate(alterColumnOperation);
    }

    protected override void Generate(CreateIndexOperation createIndexOperation, IndentedTextWriter writer)
    {      
      createIndexOperation.Table = TrimSchemaPrefix(createIndexOperation.Table);
      base.Generate(createIndexOperation, writer);
    }

    protected override void Generate(CreateTableOperation createTableOperation, IndentedTextWriter writer)
    {
      var create = new CreateTableOperation(TrimSchemaPrefix(createTableOperation.Name));
      
      foreach (var item in createTableOperation.Columns)	    
        create.Columns.Add(item);

      create.PrimaryKey = createTableOperation.PrimaryKey;

      base.Generate(create, writer);
    }

    protected override void Generate(DropColumnOperation dropColumnOperation, IndentedTextWriter writer)
    {
      var drop = new DropColumnOperation(TrimSchemaPrefix(dropColumnOperation.Table), dropColumnOperation.Name);
      base.Generate(drop, writer);
    }

    protected override void Generate(DropForeignKeyOperation dropForeignKeyOperation, IndentedTextWriter writer)
    {
      dropForeignKeyOperation.PrincipalTable = TrimSchemaPrefix(dropForeignKeyOperation.PrincipalTable);
      dropForeignKeyOperation.DependentTable = TrimSchemaPrefix(dropForeignKeyOperation.DependentTable);
      base.Generate(dropForeignKeyOperation, writer);
    }

    protected override void Generate(DropIndexOperation dropIndexOperation, IndentedTextWriter writer)
    {
      dropIndexOperation.Table = TrimSchemaPrefix(dropIndexOperation.Table);
      base.Generate(dropIndexOperation, writer);
    }

    protected override void Generate(DropPrimaryKeyOperation dropPrimaryKeyOperation, IndentedTextWriter writer)
    {
      dropPrimaryKeyOperation.Table = TrimSchemaPrefix(dropPrimaryKeyOperation.Table);
      base.Generate(dropPrimaryKeyOperation, writer);
    }

    protected override void Generate(DropTableOperation dropTableOperation, IndentedTextWriter writer)
    {
      var drop = new DropTableOperation(TrimSchemaPrefix(dropTableOperation.Name));                        
      base.Generate(drop, writer);
    }

    protected override void Generate(MoveTableOperation moveTableOperation, IndentedTextWriter writer)
    {
      var move = new MoveTableOperation(TrimSchemaPrefix(moveTableOperation.Name), moveTableOperation.NewSchema);      
      base.Generate(move, writer);
    }

    protected override void Generate(RenameColumnOperation renameColumnOperation, IndentedTextWriter writer)
    {
      var rename = new RenameColumnOperation(TrimSchemaPrefix(renameColumnOperation.Table), renameColumnOperation.Name, renameColumnOperation.NewName);      
      base.Generate(rename, writer);
    }

    protected override void Generate(RenameTableOperation renameTableOperation, IndentedTextWriter writer)
    {
      var rename = new RenameTableOperation(TrimSchemaPrefix(renameTableOperation.Name), renameTableOperation.NewName);
      base.Generate(rename, writer);
    }    
  }


  /// <summary>
  /// Implementation of a MySql's Sql generator for EF 4.3 data migrations.
  /// </summary>
  public class MySqlMigrationSqlGenerator : MigrationSqlGenerator
  {
    private List<MigrationStatement> _specialStmts = new List<MigrationStatement>();
    private DbProviderManifest _providerManifest;
    private Dictionary<string, OpDispatcher> _dispatcher = new Dictionary<string, OpDispatcher>();
    private List<string> _generatedTables { get; set; }
    private string _tableName { get; set; }

    delegate MigrationStatement OpDispatcher(MigrationOperation op);


    public MySqlMigrationSqlGenerator()
    {

      _dispatcher.Add("AddColumnOperation", (OpDispatcher)((op) => { return Generate(op as AddColumnOperation); }));
      _dispatcher.Add("AddForeignKeyOperation", (OpDispatcher)((op) => { return Generate(op as AddForeignKeyOperation); }));
      _dispatcher.Add("AddPrimaryKeyOperation", (OpDispatcher)((op) => { return Generate(op as AddPrimaryKeyOperation); }));
      _dispatcher.Add("AlterColumnOperation", (OpDispatcher)((op) => { return Generate(op as AlterColumnOperation); }));
      _dispatcher.Add("CreateIndexOperation", (OpDispatcher)((op) => { return Generate(op as CreateIndexOperation); }));
      _dispatcher.Add("CreateTableOperation", (OpDispatcher)((op) => { return Generate(op as CreateTableOperation); }));
      _dispatcher.Add("DeleteHistoryOperation", (OpDispatcher)((op) => { return Generate(op as DeleteHistoryOperation); }));
      _dispatcher.Add("DropColumnOperation", (OpDispatcher)((op) => { return Generate(op as DropColumnOperation); }));
      _dispatcher.Add("DropForeignKeyOperation", (OpDispatcher)((op) => { return Generate(op as DropForeignKeyOperation); }));
      _dispatcher.Add("DropIndexOperation", (OpDispatcher)((op) => { return Generate(op as DropIndexOperation); }));
      _dispatcher.Add("DropPrimaryKeyOperation", (OpDispatcher)((op) => { return Generate(op as DropPrimaryKeyOperation); }));
      _dispatcher.Add("DropTableOperation", (OpDispatcher)((op) => { return Generate(op as DropTableOperation); }));
      _dispatcher.Add("InsertHistoryOperation", (OpDispatcher)((op) => { return Generate(op as InsertHistoryOperation); }));
      _dispatcher.Add("MoveTableOperation", (OpDispatcher)((op) => { return Generate(op as MoveTableOperation); }));
      _dispatcher.Add("RenameColumnOperation", (OpDispatcher)((op) => { return Generate(op as RenameColumnOperation); }));
      _dispatcher.Add("RenameTableOperation", (OpDispatcher)((op) => { return Generate(op as RenameTableOperation); }));
      _dispatcher.Add("SqlOperation", (OpDispatcher)((op) => { return Generate(op as SqlOperation); }));
    }

    public override IEnumerable<MigrationStatement> Generate(IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
    {      
      MySqlConnection con = new MySqlConnection();
      List<MigrationStatement> stmts = new List<MigrationStatement>();
      _providerManifest = DbProviderServices.GetProviderServices(con).GetProviderManifest(providerManifestToken);

      foreach (MigrationOperation op in migrationOperations)
      {
        OpDispatcher opdis = _dispatcher[op.GetType().Name];
        stmts.Add(opdis(op));
      }
      if (_specialStmts.Count > 0)
      {
        foreach (var item in _specialStmts)
          stmts.Add(item);
      }        	        
      return stmts;
    }

    protected virtual MigrationStatement Generate(AddColumnOperation op)
    {
      if (op == null) return null;

      _tableName = op.Table;

      MigrationStatement stmt = new MigrationStatement();
      stmt.Sql = string.Format("alter table `{0}` add column `{1}`",
        TrimSchemaPrefix(op.Table), op.Column.Name) + " " + Generate(op.Column);
      return stmt;
    }

    protected virtual MigrationStatement Generate(DropColumnOperation op)
    {
      if (op == null) return null;

      MigrationStatement stmt = new MigrationStatement();
      StringBuilder sb = new StringBuilder();
      stmt.Sql = string.Format("alter table `{0}` drop column `{1}`",
        TrimSchemaPrefix(op.Table), op.Name);
      return stmt;
    }

    protected virtual MigrationStatement Generate(AlterColumnOperation op)
    {
      if (op == null) return null;

      ColumnModel column = op.Column;
      StringBuilder sb = new StringBuilder();
      _tableName = op.Table;

      // for existing columns
      sb.Append("alter table `" + TrimSchemaPrefix(op.Table) + "` modify `" + column.Name + "` ");

      // add definition
      sb.Append(Generate(column));

      return new MigrationStatement { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(RenameColumnOperation op)
    {
      if (op == null) return null;

      StringBuilder sb = new StringBuilder();

      sb.Append("set @columnType := (select case lower(IS_NULLABLE) when `no` then CONCAT(column_type, ` ` , `not null `)  when `yes` then column_type end from information_schema.columns where table_name = `" + TrimSchemaPrefix(op.Table) + "` and column_name = `" + op.Name + "` );");
      sb.AppendLine();
      sb.Append("set @sqlstmt := (select concat(`alter table " + TrimSchemaPrefix(op.Table) + " change `" + op.Name + "` " + op.NewName + "` , @columnType));");
      sb.AppendLine();
      sb.Append("prepare stmt @sqlstmt;");
      sb.AppendLine();
      sb.Append("execute stmt;");
      sb.AppendLine();
      sb.Append("deallocate prepare stmt");
      return new MigrationStatement { Sql = sb.ToString() };

    }


    protected virtual MigrationStatement Generate(AddForeignKeyOperation op)
    {

      StringBuilder sb = new StringBuilder();
      sb.Append("alter table `" + op.DependentTable + "` add constraint `" + op.Name + "` " +
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
      TypeUsage typeUsage = _providerManifest.GetStoreType(op.TypeUsage);
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
        if (op.Precision.HasValue && op.Scale.HasValue)
        {
            sb.AppendFormat("( {0}, {1} ) ", op.Precision.Value, op.Scale.Value);
        }
        else
        {
          if (type == "datetime" || type == "timestamp" || type == "time")
          {
            if (op.Precision.HasValue && op.Precision.Value >= 1)
            {            
              sb.AppendFormat("({0}) ", op.Precision.Value <= 6 ? op.Precision.Value : 6);
            }
            if (op.IsIdentity && (String.Compare(type, "datetime", true) == 0 || String.Compare(type, "timestamp", true) == 0))
            {
              sb.AppendFormat(" DEFAULT CURRENT_TIMESTAMP{0}", op.Precision.HasValue && op.Precision.Value >= 1 ? "( " + op.Precision.Value.ToString() + " )" : "");                              
            }
          }            
        }         
      }      

      if (!(op.IsNullable ?? true))
      {
        sb.Append(" not null ");
      }
      if (op.IsIdentity && type == "int")
      {
        sb.Append(" auto_increment primary key ");
      }
      else
      {
        if (op.IsIdentity && String.Compare(type,"CHAR(36) BINARY", true) == 0)
         {
           var createTrigger = new StringBuilder();
           createTrigger.AppendLine(string.Format("DROP TRIGGER IF EXISTS `{0}_IdentityTgr`;", _tableName));           
           createTrigger.AppendLine(string.Format("CREATE TRIGGER `{0}_IdentityTgr` BEFORE INSERT ON `{0}`", _tableName));           
           createTrigger.AppendLine("FOR EACH ROW BEGIN");           
           createTrigger.AppendLine(string.Format("SET NEW.{0} = UUID();", op.Name));           
           createTrigger.AppendLine(string.Format("DROP TEMPORARY TABLE IF EXISTS tmpIdentity_{0};", _tableName));           
           createTrigger.AppendLine(string.Format("CREATE TEMPORARY TABLE tmpIdentity_{0} (guid CHAR(36))ENGINE=MEMORY;", _tableName));           
           createTrigger.AppendLine(string.Format("INSERT INTO tmpIdentity_{0} VALUES(New.{1});", _tableName, op.Name));           
           createTrigger.AppendLine("END");          
           var sqlOp = new SqlOperation(createTrigger.ToString());
           _specialStmts.Add(Generate(sqlOp));           
         }
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

      sb.AppendFormat("index  `{0}` on `{1}` (", op.Name, TrimSchemaPrefix(op.Table));
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
          TrimSchemaPrefix(op.Table), op.Name)
      };
    }


    protected virtual MigrationStatement Generate(CreateTableOperation op)
    {
      StringBuilder sb = new StringBuilder();
      string tableName = TrimSchemaPrefix(op.Name);

      if (_generatedTables == null)
        _generatedTables = new List<string>();

      if (!_generatedTables.Contains(tableName))
      {
        _generatedTables.Add(tableName);
      }
      sb.Append("create table " + "`" + tableName + "`" + " (");

      _tableName = op.Name;
      //columns
      sb.Append(string.Join(",", op.Columns.Select(c => "`" + c.Name + "` " + Generate(c))));

      if (op.PrimaryKey != null && !sb.ToString().Contains("primary key"))
      {
        sb.Append(",");
        sb.Append("primary key ( " + string.Join(",", op.PrimaryKey.Columns.Select(c => "`" + c + "`")) + ") ");
      }

      sb.Append(") engine=InnoDb auto_increment=0");

      return new MigrationStatement() { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(DropTableOperation op)
    {
      return new MigrationStatement() { Sql = "drop table " + "`" + TrimSchemaPrefix(op.Name) + "`" };
    }

    protected virtual MigrationStatement Generate(DeleteHistoryOperation op)
    {
      return new MigrationStatement { Sql = string.Format("delete from `{0}` where MigrationId = '{1}'", TrimSchemaPrefix(op.Table), op.MigrationId) };
    }

    protected virtual MigrationStatement Generate(AddPrimaryKeyOperation op)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("alter table `" + TrimSchemaPrefix(op.Table) + "` add primary key ");
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

      model.Append(BitConverter.ToString(op.Model).Replace("-", ""));

      sb.Append("insert into `" + TrimSchemaPrefix(op.Table) + "` (`migrationId`, `model`, `productVersion`) ");
      sb.AppendFormat(" values ( '{0}', {1}, '{2}') ",
                      op.MigrationId,
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
      if (table.StartsWith("dbo."))
        return table.Replace("dbo.", "");

      return table;
    }
  }
}


