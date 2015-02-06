// Copyright © 2013, 2015 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;
#if EF6
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data.EntityClient;
using System.Data.Metadata.Edm;
#endif

namespace MySql.Data.Entity.Migrations.Tests
{
  public class MySqlMigrationsTests : IUseFixture<SetUpMigrationsTests>
  {
    private MySqlProviderManifest ProviderManifest;

    private SetUpMigrationsTests st;

    public void SetFixture(SetUpMigrationsTests data)
    {
      st = data;
#if EF6
      Database.SetInitializer(new MigrateDatabaseToLatestVersion<BlogContext, EF6Configuration>()); 
#endif
    }    

    private MySqlConnection GetConnectionFromContext(DbContext ctx)
    {
      return (MySqlConnection)((EntityConnection)(((IObjectContextAdapter)ctx).ObjectContext.Connection)).StoreConnection;
    }

    /// <summary>
    /// Add int32 type column to existing table
    /// </summary>
    [Fact]
    public void AddColumnOperationMigration()
    {
      var migrationOperations = new List<MigrationOperation>();

      if (ProviderManifest == null)
      ProviderManifest = new MySqlProviderManifest(st.Version.ToString());

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
          Assert.Equal(true, GenerateAndExecuteMySQLStatements(migrationOperations));
          
          MySqlCommand query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Blogs' and column_name ='TotalPosts'" , conn);
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal("TotalPosts", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("int", reader[2].ToString());
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
    [Fact]
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
          Assert.Equal(true, GenerateAndExecuteMySQLStatements(migrationOperations));
          MySqlCommand query = new MySqlCommand("Select Column_name from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Posts'", conn);
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal(1, createTableOperation.Columns.Where(t => t.Name.Equals(reader[0].ToString())).Count());
          }
          reader.Close();
          conn.Close();
        }
      }      
    }

    /// <summary>
    /// CreateForeignKey operation
    /// between Blogs and Posts table
    /// </summary>
    [Fact]
    public void CreateForeignKeyOperation()
    {
      var migrationOperations = new List<MigrationOperation>();

      // create dependant table Posts
      var createTableOperation = CreateTableOperation();
      migrationOperations.Add(createTableOperation);

      // Add column BlogId to create the constraints

      if (ProviderManifest == null)
        ProviderManifest = new MySqlProviderManifest(st.Version.ToString());

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

        Assert.Equal(true, GenerateAndExecuteMySQLStatements(migrationOperations));

        using (var conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open(); 
          // check for foreign key creation
          MySqlCommand query = new MySqlCommand("select Count(*) from information_schema.table_constraints where constraint_type = 'foreign key' and constraint_schema = '" + conn.Database + "' and constraint_name = 'FKBlogs'", conn);
          int rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.Equal(1, rows);
          // check for table creation          
          query = new MySqlCommand("select Count(*) from information_schema.Tables WHERE `table_name` = 'Posts' and `table_schema` = '" + conn.Database + "' ", conn);          
          rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.Equal(1, rows);                    
          conn.Close();
        }

        // Test fix for 
        MySqlConnection con = GetConnectionFromContext(context);
        con.Open();
        try
        {
          MySqlCommand cmd = new MySqlCommand("show create table `posts`", con);
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

    [Fact]
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
        if (context.Database.Exists())  context.Database.Delete();
        context.Database.Create();
        

        Assert.Equal(true, GenerateAndExecuteMySQLStatements(migrationOperations));

        using (var conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();          
          
          // check for table creation          
          var query = new MySqlCommand("select Count(*) from information_schema.Tables WHERE `table_name` = 'Posts' and `table_schema` = '" + conn.Database + "' ", conn);
          int rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.Equal(1, rows);

          // check if PK exists          
          query = new MySqlCommand("select Count(*) from information_schema.table_constraints where `constraint_type` = 'primary key' and `constraint_schema` = '" + conn.Database + "' and table_name= 'Posts'", conn);
          rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.Equal(0, rows);
         
          //check the definition of the column that was PK
          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Posts' and column_name ='PostId'", conn);
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal("PostId", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("int", reader[2].ToString());
          }
          reader.Close();
          conn.Close();
        }
      }          
    }

   
    /// <summary>
    /// Drop primary key. No anonymous arguments
    /// </summary>
    [Fact]
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


        Assert.Equal(true, GenerateAndExecuteMySQLStatements(migrationOperations));

        using (var conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          if (conn.State == System.Data.ConnectionState.Closed) conn.Open();

          // check for table creation          
          var query = new MySqlCommand("select Count(*) from information_schema.Tables WHERE `table_name` = 'Posts' and `table_schema` = '" + conn.Database + "' ", conn);
          int rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.Equal(1, rows);

          // check if PK exists          
          query = new MySqlCommand("select Count(*) from information_schema.table_constraints where `constraint_type` = 'primary key' and `constraint_schema` = '" + conn.Database + "' and table_name= 'Posts'", conn);
          rows = Convert.ToInt32(query.ExecuteScalar());
          Assert.Equal(0, rows);

          //check the definition of the column that was PK
          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Posts' and column_name ='PostId'", conn);
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal("PostId", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("int", reader[2].ToString());
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
        ProviderManifest = new MySqlProviderManifest(st.Version.ToString());

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
    private bool GenerateAndExecuteMySQLStatements(List<MigrationOperation>  migrationOperations)
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



       