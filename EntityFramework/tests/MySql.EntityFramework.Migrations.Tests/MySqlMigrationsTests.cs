// Copyright © 2013, 2024, Oracle and/or its affiliates.
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
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Linq;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;

namespace MySql.Data.EntityFramework.Migrations.Tests
{
  public class MySqlMigrationsTests : SetUpMigrationsTests
  {
    private MySqlProviderManifest ProviderManifest;

    public MySqlMigrationsTests()
    {
      Database.SetInitializer(new MigrateDatabaseToLatestVersion<BlogContext, EF6Configuration>());
    }

    private MySqlConnection GetConnectionFromContext(DbContext ctx)
    {
      return (MySqlConnection)((EntityConnection)(((IObjectContextAdapter)ctx).ObjectContext.Connection)).StoreConnection;
    }

    /// <summary>
    /// Add int32 type column to existing table
    /// </summary>
    [Test]
    public void AddColumnOperationMigration()
    {
      var migrationOperations = new List<MigrationOperation>();

      if (ProviderManifest == null)
        ProviderManifest = new MySqlProviderManifest(Version.ToString());

      TypeUsage tu = TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
      TypeUsage result = ProviderManifest.GetStoreType(tu);

      var intColumn = new ColumnModel(PrimitiveTypeKind.Int32, result)
      {
        Name = "TotalPosts",
        IsNullable = false
      };

      var addColumnMigratioOperation = new AddColumnOperation("Blogs", intColumn);
      migrationOperations.Add(addColumnMigratioOperation);

      using (BlogContext context = new BlogContext())
      {
        if (context.Database.Exists()) context.Database.Delete();
        context.Database.Create();

        using (MySqlConnection conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
          Assert.AreEqual(true, GenerateAndExecuteMySQLStatements(migrationOperations));

          MySqlCommand query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Blogs' and column_name ='TotalPosts'", conn);
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("TotalPosts", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("int", reader[2].ToString());
          }
          reader.Close();
          conn.Close();
        }
      }
    }

    /// <summary>
    /// CreateTable operation
    /// with the following columns int PostId string Title string Body 
    /// </summary>
    [Test]
    public void CreateTableOperationMigration()
    {

      var migrationOperations = new List<MigrationOperation>();
      var createTableOperation = CreateTableOperation();

      migrationOperations.Add(createTableOperation);

      using (BlogContext context = new BlogContext())
      {
        if (context.Database.Exists()) context.Database.Delete();
        context.Database.Create();

        using (var conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
          Assert.True(GenerateAndExecuteMySQLStatements(migrationOperations));
          using (MySqlCommand query = new MySqlCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA ='{conn.Database}'" +
            $" AND TABLE_NAME = 'Posts'", conn))
          {
            using (MySqlDataReader reader = query.ExecuteReader())
            {
              while (reader.Read())
                Assert.That(createTableOperation.Columns.Where(t => t.Name.Equals(reader[0].ToString())), Has.One.Items);
              reader.Close();
            }
            

            query.CommandText = $"SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{conn.Database}' AND " +
              $"TABLE_NAME = 'Posts' AND COLUMN_NAME = 'Password'";

            StringAssert.AreEqualIgnoringCase("binary(10)", query.ExecuteScalar().ToString());
          }
        }
      }
    }

    /// <summary>
    /// CreateForeignKey operation
    /// between Blogs and Posts table
    /// </summary>
    [Test]
    public void CreateForeignKeyOperation()
    {
      var migrationOperations = new List<MigrationOperation>();

      // create dependant table Posts
      var createTableOperation = CreateTableOperation();
      migrationOperations.Add(createTableOperation);

      // Add column BlogId to create the constraints

      if (ProviderManifest == null)
        ProviderManifest = new MySqlProviderManifest(Version.ToString());

      TypeUsage tu = TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
      TypeUsage result = ProviderManifest.GetStoreType(tu);

      var intColumn = new ColumnModel(PrimitiveTypeKind.Int32, result)
      {
        Name = "BlogId",
        IsNullable = false
      };

      var addColumnMigratioOperation = new AddColumnOperation("Posts", intColumn);
      migrationOperations.Add(addColumnMigratioOperation);

      // create constrain object
      var createForeignkeyOperation = new AddForeignKeyOperation();

      createForeignkeyOperation.Name = "FKBlogs";
      createForeignkeyOperation.DependentTable = "Posts";
      createForeignkeyOperation.DependentColumns.Add("BlogId");
      createForeignkeyOperation.CascadeDelete = true;
      createForeignkeyOperation.PrincipalTable = "Blogs";
      createForeignkeyOperation.PrincipalColumns.Add("BlogId");

      //create index to use
      migrationOperations.Add(createForeignkeyOperation.CreateCreateIndexOperation());

      migrationOperations.Add(createForeignkeyOperation);


      using (BlogContext context = new BlogContext())
      {

        if (context.Database.Exists()) context.Database.Delete();
        context.Database.Create();

        Assert.AreEqual(true, GenerateAndExecuteMySQLStatements(migrationOperations));

        using (var conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
          // check for foreign key creation
          MySqlCommand query = new MySqlCommand("select Count(*) from information_schema.table_constraints where LOWER(constraint_type) = 'foreign key' and constraint_schema = '" + conn.Database + "' and constraint_name = 'FKBlogs'", conn);
          int rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.AreEqual(1, rows);
          // check for table creation          
          query = new MySqlCommand("select Count(*) from information_schema.Tables WHERE `table_name` = 'Posts' and `table_schema` = '" + conn.Database + "' ", conn);
          rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.AreEqual(1, rows);
          conn.Close();
        }

        // Test fix for 
        MySqlConnection con = GetConnectionFromContext(context);
        con.Open();
        try
        {
          MySqlCommand cmd = new MySqlCommand("show create table `Posts`", con);
          using (MySqlDataReader r = cmd.ExecuteReader())
          {
            r.Read();
            string sql = r.GetString(1);
            Assert.True(sql.IndexOf(
              " CONSTRAINT `FKBlogs` FOREIGN KEY (`BlogId`) REFERENCES `blogs` (`BlogId`) ON DELETE CASCADE ON UPDATE CASCADE",
              StringComparison.OrdinalIgnoreCase) != -1);
          }
        }
        finally
        {
          con.Close();
        }
      }
    }


    /// <summary>
    /// Remove PK and the autoincrement property for the column
    /// </summary>

    [Test]
    public void DropPrimaryKeyOperationWithAnonymousArguments()
    {

      var migrationOperations = new List<MigrationOperation>();

      // create table where the PK exists
      var createTableOperation = CreateTableOperation();
      migrationOperations.Add(createTableOperation);

      var createDropPKOperation = new DropPrimaryKeyOperation(anonymousArguments: new { DeleteAutoIncrement = true });
      createDropPKOperation.Table = "Posts";
      createDropPKOperation.Columns.Add("PostId");
      migrationOperations.Add(createDropPKOperation);

      using (BlogContext context = new BlogContext())
      {
        if (context.Database.Exists()) context.Database.Delete();
        context.Database.Create();


        Assert.AreEqual(true, GenerateAndExecuteMySQLStatements(migrationOperations));

        using (var conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();

          // check for table creation          
          var query = new MySqlCommand("select Count(*) from information_schema.Tables WHERE `table_name` = 'Posts' and `table_schema` = '" + conn.Database + "' ", conn);
          int rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.AreEqual(1, rows);

          // check if PK exists          
          query = new MySqlCommand("select Count(*) from information_schema.table_constraints where `constraint_type` = 'primary key' and `constraint_schema` = '" + conn.Database + "' and table_name= 'Posts'", conn);
          rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.AreEqual(0, rows);

          //check the definition of the column that was PK
          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Posts' and column_name ='PostId'", conn);
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("PostId", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("int", reader[2].ToString());
          }
          reader.Close();
          conn.Close();
        }
      }
    }


    /// <summary>
    /// Drop primary key. No anonymous arguments
    /// </summary>
    [Test]
    public void DropPrimaryKeyOperation()
    {

      var migrationOperations = new List<MigrationOperation>();

      // create table where the PK exists
      var createTableOperation = CreateTableOperation();
      migrationOperations.Add(createTableOperation);

      var createDropPKOperation = new DropPrimaryKeyOperation(anonymousArguments: new { DeleteAutoIncrement = true });
      createDropPKOperation.Table = "Posts";
      createDropPKOperation.Columns.Add("PostId");
      migrationOperations.Add(createDropPKOperation);

      using (BlogContext context = new BlogContext())
      {
        if (context.Database.Exists()) context.Database.Delete();
        context.Database.Create();


        Assert.AreEqual(true, GenerateAndExecuteMySQLStatements(migrationOperations));

        using (var conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();

          // check for table creation          
          var query = new MySqlCommand("select Count(*) from information_schema.Tables WHERE `table_name` = 'Posts' and `table_schema` = '" + conn.Database + "' ", conn);
          int rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.AreEqual(1, rows);

          // check if PK exists          
          query = new MySqlCommand("select Count(*) from information_schema.table_constraints where `constraint_type` = 'primary key' and `constraint_schema` = '" + conn.Database + "' and table_name= 'Posts'", conn);
          rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.AreEqual(0, rows);

          //check the definition of the column that was PK
          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Posts' and column_name ='PostId'", conn);
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("PostId", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("int", reader[2].ToString());
          }
          reader.Close();
          conn.Close();
        }
      }
    }


    /// <summary>
    /// Creates a table named Posts 
    /// and columns int PostId, string Title, string Body 
    /// </summary>
    /// <returns></returns>

    private CreateTableOperation CreateTableOperation()
    {
      TypeUsage tu;
      TypeUsage result;

      if (ProviderManifest == null)
        ProviderManifest = new MySqlProviderManifest(Version.ToString());

      var createTableOperation = new CreateTableOperation("Posts");

      //Column model for int IdPost
      tu = TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
      result = ProviderManifest.GetStoreType(tu);

      var intColumn = new ColumnModel(PrimitiveTypeKind.Int32, result)
      {
        Name = "PostId",
        IsNullable = false,
        IsIdentity = true
      };

      createTableOperation.Columns.Add(intColumn);

      //Column model for string 
      tu = TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));
      result = ProviderManifest.GetStoreType(tu);

      var stringColumnTitle = new ColumnModel(PrimitiveTypeKind.String, result)
      {
        Name = "Title",
        IsNullable = false
      };

      var stringColumnBody = new ColumnModel(PrimitiveTypeKind.String, result)
      {
        Name = "Body",
        IsNullable = true
      };

      createTableOperation.Columns.Add(stringColumnTitle);
      createTableOperation.Columns.Add(stringColumnBody);

      //Column model for binary 
      tu = TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Binary));
      result = ProviderManifest.GetStoreType(tu);

      var binaryColumn = new ColumnModel(PrimitiveTypeKind.Binary, result)
      {
        Name = "Password",
        MaxLength = 10,
        StoreType = "binary"
      };

      createTableOperation.Columns.Add(binaryColumn);

      var primaryKey = new AddPrimaryKeyOperation();

      primaryKey.Columns.Add("PostId");

      createTableOperation.PrimaryKey = primaryKey;

      return createTableOperation;

    }


    /// <summary>
    /// Generate and apply sql statemens from the
    /// migration operations list
    /// return false is case of fail or if database doesn't exist
    /// </summary>
    private bool GenerateAndExecuteMySQLStatements(List<MigrationOperation> migrationOperations)
    {
      MySqlProviderServices ProviderServices;

      ProviderServices = new MySqlProviderServices();

      using (BlogContext context = new BlogContext())
      {
        if (!context.Database.Exists()) return false;

        using (MySqlConnection conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          var migratorGenerator = new MySqlMigrationSqlGenerator();
          var Token = ProviderServices.GetProviderManifestToken(conn);
          var sqlStmts = migratorGenerator.Generate(migrationOperations, providerManifestToken: Token);
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
          foreach (MigrationStatement stmt in sqlStmts)
          {
            try
            {
              MySqlCommand cmd = new MySqlCommand(stmt.Sql, conn);
              cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
              return false;
            }
          }
        }
      }
      return true;
    }
  }
}
