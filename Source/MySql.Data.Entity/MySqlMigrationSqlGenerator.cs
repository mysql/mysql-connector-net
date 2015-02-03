// Copyright © 2008, 2015 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Utilities;
using System.Collections;
#if EF6
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Common.CommandTrees;
#else
using System.Data.Metadata.Edm;
#endif


namespace MySql.Data.Entity
{

  /// <summary>
  /// Class used to customized code generation
  /// to avoid dbo. prefix added on table names.
  /// </summary>
  public class MySqlMigrationCodeGenerator : CSharpMigrationCodeGenerator
  {
    private IEnumerable<KeyValuePair<CreateTableOperation, AddForeignKeyOperation>> _foreignKeys;
    private IEnumerable<KeyValuePair<CreateTableOperation, CreateIndexOperation>> _tableIndexes;
    
    private string TrimSchemaPrefix(string table)
    {
      if (table.StartsWith("dbo."))
        return table.Replace("dbo.", "");

      return table;
    }

    private string PrepareSql(string sql, bool removeNonMySqlChars)
    {
      var sqlResult = sql;
      if (removeNonMySqlChars)
      {
        sqlResult = sql.Replace("[", "").Replace("]", "").Replace("@", "");
      }
      sqlResult = sqlResult.Replace("dbo.", "");
      return sqlResult;
    }

    private IEnumerable<MigrationOperation> ReorderOperations(IEnumerable<MigrationOperation> operations)
    {
      if (operations.Where(operation => operation.GetType() == typeof(AddPrimaryKeyOperation)).Count() > 0 &&
          operations.Where(operation => operation.GetType() == typeof(DropPrimaryKeyOperation)).Count() > 0)
      {
        List<MigrationOperation> reorderedOpes = new List<MigrationOperation>();
        reorderedOpes.AddRange(operations.Where(operation => operation.GetType() == typeof(AlterColumnOperation)));
        reorderedOpes.AddRange(operations.Where(operation => operation.GetType() == typeof(DropPrimaryKeyOperation)));
        reorderedOpes.AddRange(operations.Where(operation => operation.GetType() != typeof(DropPrimaryKeyOperation) && operation.GetType() != typeof(AlterColumnOperation)));
        return reorderedOpes;
      }
      return operations;
    }

    public override ScaffoldedMigration Generate(string migrationId, IEnumerable<MigrationOperation> operations, string sourceModel, string targetModel, string @namespace, string className)
    {
      _foreignKeys = (from tbl in operations.OfType<CreateTableOperation>()
                      from fk in operations.OfType<AddForeignKeyOperation>()
                      where tbl.Name.Equals(fk.DependentTable, StringComparison.InvariantCultureIgnoreCase)
                      select new KeyValuePair<CreateTableOperation, AddForeignKeyOperation>(tbl, fk)).ToList();

      _tableIndexes = (from tbl in operations.OfType<CreateTableOperation>()
                       from idx in operations.OfType<CreateIndexOperation>()
                       where tbl.Name.Equals(idx.Table, StringComparison.InvariantCultureIgnoreCase)
                       select new KeyValuePair<CreateTableOperation, CreateIndexOperation>(tbl, idx)).ToList();

      return base.Generate(migrationId, ReorderOperations(operations), sourceModel, targetModel, @namespace, className);
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
      addForeignKeyOperation.Name = PrepareSql(addForeignKeyOperation.Name, false);
      base.Generate(addForeignKeyOperation, writer);
    }

    protected override void GenerateInline(AddForeignKeyOperation addForeignKeyOperation, IndentedTextWriter writer)
    {
      writer.WriteLine();
      writer.Write(".ForeignKey(\"" + TrimSchemaPrefix(addForeignKeyOperation.PrincipalTable) + "\", ");
      Generate(addForeignKeyOperation.DependentColumns, writer);
      writer.Write(addForeignKeyOperation.CascadeDelete ? ", cascadeDelete: true)" : ")");
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

    protected override void GenerateInline(CreateIndexOperation createIndexOperation, IndentedTextWriter writer)
    {
      writer.WriteLine();
      writer.Write(".Index(");

      Generate(createIndexOperation.Columns, writer);

      writer.Write(createIndexOperation.IsUnique ? ", unique: true" : "");
      writer.Write(!createIndexOperation.HasDefaultName ? string.Format(", name: {0}", TrimSchemaPrefix(createIndexOperation.Name)) : "");

      writer.Write(")");
    }

    protected override void Generate(CreateTableOperation createTableOperation, IndentedTextWriter writer)
    {
      var create = new CreateTableOperation(TrimSchemaPrefix(createTableOperation.Name));
      
      foreach (var item in createTableOperation.Columns)	    
        create.Columns.Add(item);

      create.PrimaryKey = createTableOperation.PrimaryKey;

      base.Generate(create, writer);

      System.IO.StringWriter innerWriter = writer.InnerWriter as System.IO.StringWriter;
      if (innerWriter != null)
      {
        innerWriter.GetStringBuilder().Remove(innerWriter.ToString().LastIndexOf(";"), innerWriter.ToString().Length - innerWriter.ToString().LastIndexOf(";"));
        writer.Indent++;
        _foreignKeys.Where(tbl => tbl.Key == createTableOperation).ToList().ForEach(fk => GenerateInline(fk.Value, writer));
        _tableIndexes.Where(tbl => tbl.Key == createTableOperation).ToList().ForEach(idx => GenerateInline(idx.Value, writer));
        writer.WriteLine(";");
        writer.Indent--;
        writer.WriteLine();
      }
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
      dropForeignKeyOperation.Name = PrepareSql(dropForeignKeyOperation.Name, false);
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
    private string _providerManifestToken;
  private List<string> autoIncrementCols { get; set; }
    private List<string> primaryKeyCols { get; set; }
    private IEnumerable<AddPrimaryKeyOperation> _pkOperations = new List<AddPrimaryKeyOperation>();

    delegate MigrationStatement OpDispatcher(MigrationOperation op);


    public MySqlMigrationSqlGenerator()
    {

      _dispatcher.Add("AddColumnOperation", (OpDispatcher)((op) => { return Generate(op as AddColumnOperation); }));
      _dispatcher.Add("AddForeignKeyOperation", (OpDispatcher)((op) => { return Generate(op as AddForeignKeyOperation); }));
      _dispatcher.Add("AddPrimaryKeyOperation", (OpDispatcher)((op) => { return Generate(op as AddPrimaryKeyOperation); }));
      _dispatcher.Add("AlterColumnOperation", (OpDispatcher)((op) => { return Generate(op as AlterColumnOperation); }));
      _dispatcher.Add("CreateIndexOperation", (OpDispatcher)((op) => { return Generate(op as CreateIndexOperation); }));
      _dispatcher.Add("CreateTableOperation", (OpDispatcher)((op) => { return Generate(op as CreateTableOperation); }));
      _dispatcher.Add("DropColumnOperation", (OpDispatcher)((op) => { return Generate(op as DropColumnOperation); }));
      _dispatcher.Add("DropForeignKeyOperation", (OpDispatcher)((op) => { return Generate(op as DropForeignKeyOperation); }));
      _dispatcher.Add("DropIndexOperation", (OpDispatcher)((op) => { return Generate(op as DropIndexOperation); }));
      _dispatcher.Add("DropPrimaryKeyOperation", (OpDispatcher)((op) => { return Generate(op as DropPrimaryKeyOperation); }));
      _dispatcher.Add("DropTableOperation", (OpDispatcher)((op) => { return Generate(op as DropTableOperation); }));
      _dispatcher.Add("MoveTableOperation", (OpDispatcher)((op) => { return Generate(op as MoveTableOperation); }));
      _dispatcher.Add("RenameColumnOperation", (OpDispatcher)((op) => { return Generate(op as RenameColumnOperation); }));
      _dispatcher.Add("RenameTableOperation", (OpDispatcher)((op) => { return Generate(op as RenameTableOperation); }));
      _dispatcher.Add("SqlOperation", (OpDispatcher)((op) => { return Generate(op as SqlOperation); }));
    autoIncrementCols = new List<string>();
      primaryKeyCols = new List<string>();
#if EF6
      _dispatcher.Add("HistoryOperation", (OpDispatcher)((op) => { return Generate(op as HistoryOperation); }));
      _dispatcher.Add("CreateProcedureOperation", (OpDispatcher)((op) => { return Generate(op as CreateProcedureOperation); }));
      _dispatcher.Add("UpdateDatabaseOperation", (OpDispatcher)((op) => { return Generate(op as UpdateDatabaseOperation); }));
#endif
#if !EF6
      _dispatcher.Add("DeleteHistoryOperation", (OpDispatcher)((op) => { return Generate(op as DeleteHistoryOperation); }));
      _dispatcher.Add("InsertHistoryOperation", (OpDispatcher)((op) => { return Generate(op as InsertHistoryOperation); }));      
#endif
    }

    public override IEnumerable<MigrationStatement> Generate(IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
    {      
      MySqlConnection con = new MySqlConnection();
      List<MigrationStatement> stmts = new List<MigrationStatement>();
      _providerManifestToken = providerManifestToken;
      _providerManifest = DbProviderServices.GetProviderServices(con).GetProviderManifest(providerManifestToken);
      
      //verify if there is one or more add/alter column operation, if there is then look for primary key operations. Alter in case that the user wants to change the current PK column
      if ((from cols in migrationOperations.OfType<AddColumnOperation>() select cols).Count() > 0 || (from cols in migrationOperations.OfType<AlterColumnOperation>() select cols).Count() > 0)
        _pkOperations = (from pks in migrationOperations.OfType<AddPrimaryKeyOperation>() select pks).ToList();

      foreach (MigrationOperation op in migrationOperations)
      {
        if (!_dispatcher.ContainsKey(op.GetType().Name))
          throw new NotImplementedException(op.GetType().Name);
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

#if EF6

    private MigrationStatement Generate(UpdateDatabaseOperation updateDatabaseOperation)
    {
      if (updateDatabaseOperation == null)
        throw new ArgumentNullException("UpdateDatabaseOperation");

      MigrationStatement statement = new MigrationStatement();
      StringBuilder sql = new StringBuilder();
      const string idempotentScriptName = "_idempotent_script";
      SelectGenerator generator = new SelectGenerator();

      if (!updateDatabaseOperation.Migrations.Any())
        return statement;

      sql.AppendFormat("DROP PROCEDURE IF EXISTS `{0}`;", idempotentScriptName);
      sql.AppendLine();
      sql.AppendLine();

      sql.AppendLine("DELIMITER //");
      sql.AppendLine();

      sql.AppendFormat("CREATE PROCEDURE `{0}`()", idempotentScriptName);
      sql.AppendLine();
      sql.AppendLine("BEGIN");
      sql.AppendLine("  DECLARE CurrentMigration TEXT;");
      sql.AppendLine();
      sql.AppendLine("  IF EXISTS(SELECT 1 FROM information_schema.tables ");
      sql.AppendLine("  WHERE table_name = '__MigrationHistory' ");
      sql.AppendLine("  AND table_schema = DATABASE()) THEN ");

      foreach (var historyQueryTree in updateDatabaseOperation.HistoryQueryTrees)
      {
        string historyQuery = generator.GenerateSQL(historyQueryTree);
        ReplaceParemeters(ref historyQuery, generator.Parameters);
        sql.AppendLine(@"    SET CurrentMigration = (" + historyQuery + ");");
        sql.AppendLine("  END IF;");
        sql.AppendLine();
      }

      sql.AppendLine("  IF CurrentMigration IS NULL THEN");
      sql.AppendLine("    SET CurrentMigration = '0';");
      sql.AppendLine("  END IF;");
      sql.AppendLine();

      // Migrations
      foreach (var migration in updateDatabaseOperation.Migrations)
      {
        if (migration.Operations.Count == 0)
          continue;

        sql.AppendLine("  IF CurrentMigration < '" + migration.MigrationId + "' THEN ");
        var statements = Generate(migration.Operations, _providerManifestToken);
        foreach (var migrationStatement in statements)
        {
          string sqlStatement = migrationStatement.Sql;
          if (!sqlStatement.EndsWith(";"))
            sqlStatement += ";";
          sql.AppendLine(sqlStatement);
        }
        sql.AppendLine("  END IF;");
        sql.AppendLine();
      }

      sql.AppendLine("END //");
      sql.AppendLine();
      sql.AppendLine("DELIMITER ;");
      sql.AppendLine();
      sql.AppendFormat("CALL `{0}`();", idempotentScriptName);
      sql.AppendLine();
      sql.AppendLine();
      sql.AppendFormat("DROP PROCEDURE IF EXISTS `{0}`;", idempotentScriptName);
      sql.AppendLine();

      statement.Sql = sql.ToString();

      return statement;
    }

    protected virtual MigrationStatement Generate(HistoryOperation op)
    {
      if (op == null) return null;

      MigrationStatement stmt = new MigrationStatement();

      var cmdStr = "";
      SqlGenerator generator = new SelectGenerator();
      foreach (var commandTree in op.CommandTrees)
      {
        switch (commandTree.CommandTreeKind)
        {
          case DbCommandTreeKind.Insert:
            generator = new InsertGenerator();
            break;
          case DbCommandTreeKind.Delete:
            generator = new DeleteGenerator();
            break;
          case DbCommandTreeKind.Update:
            generator = new UpdateGenerator();
            break;
          case DbCommandTreeKind.Query:
            generator = new SelectGenerator();
            break;
          case DbCommandTreeKind.Function:
            generator = new FunctionGenerator();
            break;
          default:
            throw new NotImplementedException(commandTree.CommandTreeKind.ToString());
        }
        cmdStr = generator.GenerateSQL(commandTree);

        ReplaceParemeters(ref cmdStr, generator.Parameters);
        stmt.Sql += cmdStr.Replace("dbo", "") + ";";
      }
      return stmt;
    }

    private void ReplaceParemeters(ref string sql, IList<MySqlParameter> parameters)
    {
      foreach (var parameter in parameters)
      {
        if (parameter.DbType == System.Data.DbType.String)
          sql = sql.Replace(parameter.ParameterName, "'" + parameter.Value.ToString() + "'");
        else if (parameter.DbType == System.Data.DbType.Binary)
          sql = sql.Replace(parameter.ParameterName, "0x" + BitConverter.ToString((byte[])parameter.Value).Replace("-", ""));
        else
          sql = sql.Replace(parameter.ParameterName, parameter.Value.ToString());
      }
    }

    public override string GenerateProcedureBody(ICollection<DbModificationCommandTree> commandTrees, string rowsAffectedParameter, string providerManifestToken)
    {
      MySqlConnection con = new MySqlConnection();
      MigrationStatement stmt = new MigrationStatement();
      _providerManifest = DbProviderServices.GetProviderServices(con).GetProviderManifest(providerManifestToken);

      var cmdStr = "";
      SqlGenerator generator = new SelectGenerator();
      foreach (var commandTree in commandTrees)
      {
        switch (commandTree.CommandTreeKind)
        {
          case DbCommandTreeKind.Insert:
            generator = new InsertGenerator();
            cmdStr = generator.GenerateSQL(commandTree);
            break;
          case DbCommandTreeKind.Delete:
            generator = new DeleteGenerator();
            cmdStr = generator.GenerateSQL(commandTree);
            break;
          case DbCommandTreeKind.Update:
            generator = new UpdateGenerator();
            cmdStr = generator.GenerateSQL(commandTree);
            break;
          case DbCommandTreeKind.Query:
            generator = new SelectGenerator();
            cmdStr = generator.GenerateSQL(commandTree);
            break;
          case DbCommandTreeKind.Function:
            generator = new FunctionGenerator();
            cmdStr = generator.GenerateSQL(commandTree);
            break;
        }
        stmt.Sql += cmdStr.Replace("dbo.", "") + ";";
      }
      return stmt.Sql;
    }

    protected virtual MigrationStatement Generate(CreateProcedureOperation op)
    {
      MigrationStatement stmt = new MigrationStatement();
      stmt.Sql = GenerateProcedureCmd(op);
      return stmt;
    }

    private string GenerateProcedureCmd(CreateProcedureOperation po)
    {
      StringBuilder sql = new StringBuilder();
      sql.AppendLine(string.Format("CREATE PROCEDURE `{0}`({1})", po.Name.Replace("dbo.", ""), GenerateParamSentence(po.Parameters)));
      sql.AppendLine("BEGIN ");
      sql.AppendLine(po.BodySql);
      sql.AppendLine(" END");
      return sql.ToString().Replace("@", "");
    }

    private string GenerateParamSentence(IList<ParameterModel> Parameters)
    {
      StringBuilder sql = new StringBuilder();
      foreach (ParameterModel param in Parameters)
      {
        sql.AppendFormat("{0} {1} {2},",
                         (param.IsOutParameter ? "OUT" : "IN"),
                         param.Name,
                         BuildParamType(param));
      }

      return sql.ToString().Substring(0, sql.ToString().LastIndexOf(","));
    }

    private string BuildParamType(ParameterModel param)
    {
      string type = MySqlProviderServices.Instance.GetColumnType(_providerManifest.GetStoreType(param.TypeUsage));
      StringBuilder sb = new StringBuilder();
      sb.Append(type);

      if (new string[] { "char", "varchar" }.Contains(type.ToLower()))
      {
        if (param.MaxLength.HasValue)
        {
          sb.AppendFormat("({0}) ", param.MaxLength.Value);
        }
      }

      if (param.Precision.HasValue && param.Scale.HasValue)
      {
        sb.AppendFormat("( {0}, {1} ) ", param.Precision.Value, param.Scale.Value);
      }

      return sb.ToString();
    }
#endif

    protected virtual MigrationStatement Generate(AddColumnOperation op)
    {
      if (op == null) return null;

      _tableName = op.Table;

      MigrationStatement stmt = new MigrationStatement();
      //verify if there is any "AddPrimaryKeyOperation" related with the column that will be added and if it is defined as identity (auto_increment)
      bool uniqueAttr = (from pkOpe in _pkOperations
                         where (from col in pkOpe.Columns 
                                where col == op.Column.Name 
                                select col).Count() > 0
                         select pkOpe).Count() > 0 & op.Column.IsIdentity;

      //if the column to be added is PK as well as identity we need to specify the column as unique to avoid the error: "Incorrect table definition there can be only one auto column and it must be defined as a key", since unique and PK are almost equivalent we'll be able to add the new column and later add the PK related to it, this because the "AddPrimaryKeyOperation" is executed after the column is added
      stmt.Sql = string.Format("alter table `{0}` add column `{1}` {2} {3}", TrimSchemaPrefix(op.Table), op.Column.Name, Generate(op.Column), (uniqueAttr ? " unique " : ""));

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

      //verify if there is any "AddPrimaryKeyOperation" related with the column that will be added and if it is defined as identity (auto_increment)
      bool uniqueAttr = (from pkOpe in _pkOperations
                         where (from col in pkOpe.Columns
                                where col == op.Column.Name
                                select col).Count() > 0
                         select pkOpe).Count() > 0 & op.Column.IsIdentity;

      // for existing columns
      sb.Append("alter table `" + TrimSchemaPrefix(op.Table) + "` modify `" + column.Name + "` ");

      // add definition
      sb.Append(Generate(column) + (uniqueAttr ? " unique " : ""));

      return new MigrationStatement { Sql = sb.ToString() };
    }

    protected virtual MigrationStatement Generate(RenameColumnOperation op)
    {
      if (op == null) return null;

      StringBuilder sb = new StringBuilder();

      sb.Append("set @columnType := (select case lower(IS_NULLABLE) when 'no' then CONCAT(column_type, ' not null ')  when 'yes' then column_type end from information_schema.columns where table_name = '" + TrimSchemaPrefix(op.Table) + "' and column_name = '" + op.Name + "');");
      sb.AppendLine();
      sb.Append("set @sqlstmt := (select concat('alter table `" + TrimSchemaPrefix(op.Table) + "` change `" + op.Name + "` `" + op.NewName + "` ' , @columnType));");
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
      sb.Append("references `" + TrimSchemaPrefix(op.PrincipalTable) + "` ( " + string.Join(",", op.PrincipalColumns.Select(c => "`" + c + "`")) + ") ");

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
#if EF6
      string type = op.StoreType;
      if (type == null)
      {
        type = MySqlProviderServices.Instance.GetColumnType(typeUsage);
      }
#else
      string type = MySqlProviderServices.Instance.GetColumnType(typeUsage);
#endif

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

      op.StoreType = type;

      if (!(op.IsNullable ?? true))
      {
        sb.Append(string.Format("{0} not null ", 
          ((!primaryKeyCols.Contains(op.Name) && op.IsIdentity && op.Type != PrimitiveTypeKind.Guid ) ? " unsigned" : 
          (( op.Type == PrimitiveTypeKind.Guid )? " default '' " : "" ) )));
      }
      if (op.IsIdentity && (new string[] { "tinyint", "smallint", "mediumint", "int", "bigint" }).Contains(type.ToLower()))
      {
        sb.Append(" auto_increment ");
        autoIncrementCols.Add(op.Name);
      }
      else
      {
        // nothing
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
    primaryKeyCols.Clear();
      autoIncrementCols.Clear();
      if (_generatedTables == null)
        _generatedTables = new List<string>();

      if (!_generatedTables.Contains(tableName))
      {
        _generatedTables.Add(tableName);
      }
      sb.Append("create table " + "`" + tableName + "`" + " (");

      _tableName = op.Name;

    if (op.PrimaryKey != null)
      {
        op.PrimaryKey.Columns.ToList().ForEach(col => primaryKeyCols.Add(col));
      }

      //columns
      sb.Append(string.Join(",", op.Columns.Select(c => "`" + c.Name + "` " + Generate(c))));

      // Determine columns that are GUID & identity
      List<ColumnModel> guidCols = new List<ColumnModel>();
      ColumnModel guidPK = null;
      foreach( ColumnModel opCol in op.Columns )
      {
        if (opCol.Type == PrimitiveTypeKind.Guid && opCol.IsIdentity && String.Compare(opCol.StoreType, "CHAR(36) BINARY", true) == 0)
        {
          if( primaryKeyCols.Contains( opCol.Name ) )
            guidPK = opCol;
          guidCols.Add(opCol);
        } 
      }

      if (guidCols.Count != 0)
      {
        var createTrigger = new StringBuilder();
        createTrigger.AppendLine(string.Format("DROP TRIGGER IF EXISTS `{0}_IdentityTgr`;", TrimSchemaPrefix(_tableName)));
        createTrigger.AppendLine(string.Format("CREATE TRIGGER `{0}_IdentityTgr` BEFORE INSERT ON `{0}`", TrimSchemaPrefix(_tableName)));
        createTrigger.AppendLine("FOR EACH ROW BEGIN");
        for (int i = 0; i < guidCols.Count; i++)
        {
          ColumnModel opCol = guidCols[i];
          createTrigger.AppendLine(string.Format("SET NEW.{0} = UUID();", opCol.Name));
        }
        createTrigger.AppendLine(string.Format("DROP TEMPORARY TABLE IF EXISTS tmpIdentity_{0};", TrimSchemaPrefix(_tableName)));
        createTrigger.AppendLine(string.Format("CREATE TEMPORARY TABLE tmpIdentity_{0} (guid CHAR(36))ENGINE=MEMORY;", TrimSchemaPrefix(_tableName)));
        createTrigger.AppendLine(string.Format("INSERT INTO tmpIdentity_{0} VALUES(New.{1});", TrimSchemaPrefix(_tableName), guidPK.Name));
        createTrigger.AppendLine("END");
        var sqlOp = new SqlOperation(createTrigger.ToString());
        _specialStmts.Add(Generate(sqlOp));
      }

      if (op.PrimaryKey != null)// && !sb.ToString().Contains("primary key"))
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
      return new MigrationStatement() { Sql = "drop table " + "`" + TrimSchemaPrefix(op.Name) + "`" };
    }

#if !EF6
    protected virtual MigrationStatement Generate(DeleteHistoryOperation op)
    {
      return new MigrationStatement { Sql = string.Format("delete from `{0}` where MigrationId = '{1}'", TrimSchemaPrefix(op.Table), op.MigrationId) };
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
#endif

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


