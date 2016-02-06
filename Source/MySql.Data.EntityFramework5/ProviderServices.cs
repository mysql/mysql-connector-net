// Copyright (c) 2014, 2016 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using System.Collections.Generic;
using System.Data;
using MySql.Data.Entity;
using System.Reflection;
using System.Diagnostics;
using MySql.Data.Entity.Properties;
using System.Text;
using System.Linq;
using System.Globalization;
using MySql.Data.Common;
#if EF6
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Spatial;
using System.Data.Entity.Infrastructure;
#else
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
#if NET_45_OR_GREATER
using System.Data.Spatial;
#endif
#endif

namespace MySql.Data.MySqlClient
{
	public class MySqlScriptServices
	{
		public string GetTableCreateScript(EntitySet entitySet, string connectionString, string version)
		{
			MySqlProviderServices service = new MySqlProviderServices();

			if (!String.IsNullOrEmpty(version))
				service.serverVersion = new Version(version);
			else
			{
				using (var conn = new MySqlConnection(connectionString.Replace(@"""", "")))
				{
					conn.Open();                  
					var v = DBVersion.Parse(conn.ServerVersion.ToString());        
					service.serverVersion = new Version(v.Major + "." + v.Minor);
				}
			}
			if (service.serverVersion == null) service.serverVersion = new Version("5.5");
			return service.GetTableCreateScript(entitySet); 
		}
	}

#if EF6
	public sealed class MySqlProviderServices : DbProviderServices
#else
		internal partial class MySqlProviderServices : System.Data.Common.DbProviderServices
#endif
	{
		internal static readonly MySqlProviderServices Instance;
		internal Version serverVersion { get; set; }

		static MySqlProviderServices()
		{
			Instance = new MySqlProviderServices();
		}

#if NET_45_OR_GREATER
		protected override DbSpatialDataReader GetDbSpatialDataReader(DbDataReader fromReader, string manifestToken)
		{
			if (fromReader == null)
				throw new ArgumentNullException("fromReader must not be null");      

			EFMySqlDataReader efReader = fromReader as EFMySqlDataReader;
			if (efReader == null)
			{
				throw new ArgumentException(
						string.Format(
								"Spatial readers can only be produced from readers of type EFMySqlDataReader.   A reader of type {0} was provided.",
								fromReader.GetType()));
			}

			return new MySqlSpatialDataReader(efReader);
		}
#endif
		
		protected override DbCommandDefinition CreateDbCommandDefinition(
				DbProviderManifest providerManifest, DbCommandTree commandTree)
		{
			if (commandTree == null)
				throw new ArgumentNullException("commandTree");

			SqlGenerator generator = null;
			if (commandTree is DbQueryCommandTree)
				generator = new SelectGenerator();
			else if (commandTree is DbInsertCommandTree)
				generator = new InsertGenerator();
			else if (commandTree is DbUpdateCommandTree)
				generator = new UpdateGenerator();
			else if (commandTree is DbDeleteCommandTree)
				generator = new DeleteGenerator();
			else if (commandTree is DbFunctionCommandTree)
				generator = new FunctionGenerator();

			string sql = generator.GenerateSQL(commandTree);

			EFMySqlCommand cmd = new EFMySqlCommand();
			cmd.CommandText = sql;
			if (generator is FunctionGenerator)
				cmd.CommandType = (generator as FunctionGenerator).CommandType;

			SetExpectedTypes(commandTree, cmd);

			EdmFunction function = null;
			if (commandTree is DbFunctionCommandTree)
				function = (commandTree as DbFunctionCommandTree).EdmFunction;

			// Now make sure we populate the command's parameters from the CQT's parameters:
			foreach (KeyValuePair<string, TypeUsage> queryParameter in commandTree.Parameters)
			{
				DbParameter parameter = cmd.CreateParameter();
				parameter.ParameterName = queryParameter.Key;
				parameter.Direction = ParameterDirection.Input;
				parameter.DbType = Metadata.GetDbType(queryParameter.Value);

#if NET_45_OR_GREATER
				if (queryParameter.Value.EdmType is PrimitiveType &&
				((PrimitiveType)queryParameter.Value.EdmType).PrimitiveTypeKind == PrimitiveTypeKind.Geometry)
				{
					((MySqlParameter)parameter).MySqlDbType = MySqlDbType.Geometry;
				}
#endif

				FunctionParameter funcParam;
				if (function != null &&
						function.Parameters.TryGetValue(queryParameter.Key, false, out funcParam))
				{
					parameter.ParameterName = funcParam.Name;
					parameter.Direction = Metadata.ModeToDirection(funcParam.Mode);
					parameter.DbType = Metadata.GetDbType(funcParam.TypeUsage);
				}
				cmd.Parameters.Add(parameter);
			}

			// Now add parameters added as part of SQL gen 
			foreach (DbParameter p in generator.Parameters)
				cmd.Parameters.Add(p);

			return CreateCommandDefinition(cmd);
		}

		/// <summary>
		/// Sets the expected column types
		/// </summary>
		private void SetExpectedTypes(DbCommandTree commandTree, EFMySqlCommand cmd)
		{
			if (commandTree is DbQueryCommandTree)
				SetQueryExpectedTypes(commandTree as DbQueryCommandTree, cmd);
			else if (commandTree is DbFunctionCommandTree)
				SetFunctionExpectedTypes(commandTree as DbFunctionCommandTree, cmd);
		}

		/// <summary>
		/// Sets the expected column types for a given query command tree
		/// </summary>
		private void SetQueryExpectedTypes(DbQueryCommandTree tree, EFMySqlCommand cmd)
		{
			DbProjectExpression projectExpression = tree.Query as DbProjectExpression;
			if (projectExpression != null)
			{
				EdmType resultsType = projectExpression.Projection.ResultType.EdmType;

				StructuralType resultsAsStructuralType = resultsType as StructuralType;
				if (resultsAsStructuralType != null)
				{
					cmd.ColumnTypes = new PrimitiveType[resultsAsStructuralType.Members.Count];

					for (int ordinal = 0; ordinal < resultsAsStructuralType.Members.Count; ordinal++)
					{
						EdmMember member = resultsAsStructuralType.Members[ordinal];
						PrimitiveType primitiveType = member.TypeUsage.EdmType as PrimitiveType;
						cmd.ColumnTypes[ordinal] = primitiveType;
					}
				}
			}
		}

		/// <summary>
		/// Sets the expected column types for a given function command tree
		/// </summary>
		private void SetFunctionExpectedTypes(DbFunctionCommandTree tree, EFMySqlCommand cmd)
		{
			if (tree.ResultType != null)
			{
				Debug.Assert(tree.ResultType.EdmType.BuiltInTypeKind == BuiltInTypeKind.CollectionType,
						Resources.WrongFunctionResultType);

				CollectionType collectionType = (CollectionType)(tree.ResultType.EdmType);
				EdmType elementType = collectionType.TypeUsage.EdmType;

				if (elementType.BuiltInTypeKind == BuiltInTypeKind.RowType)
				{
					ReadOnlyMetadataCollection<EdmMember> members = ((RowType)elementType).Members;
					cmd.ColumnTypes = new PrimitiveType[members.Count];

					for (int ordinal = 0; ordinal < members.Count; ordinal++)
					{
						EdmMember member = members[ordinal];
						PrimitiveType primitiveType = (PrimitiveType)member.TypeUsage.EdmType;
						cmd.ColumnTypes[ordinal] = primitiveType;
					}

				}
				else if (elementType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType)
				{
					cmd.ColumnTypes = new PrimitiveType[1];
					cmd.ColumnTypes[0] = (PrimitiveType)elementType;
				}
				else
				{
					Debug.Fail(Resources.WrongFunctionResultType);
				}
			}
		}

		protected override string GetDbProviderManifestToken(DbConnection connection)
		{
			// we need the connection option to determine what version of the server
			// we are connected to
			MySqlConnectionStringBuilder msb = new MySqlConnectionStringBuilder((connection as MySqlConnection).Settings.ConnectionString);
			msb.Database = null;
			using (MySqlConnection c = new MySqlConnection(msb.ConnectionString))
			{
				c.Open();
				
				var v = DBVersion.Parse(c.ServerVersion);
				serverVersion = new Version(v.Major + "." + v.Minor);        
				
				double version = double.Parse(c.ServerVersion.Substring(0, 3), CultureInfo.InvariantCulture);
				if (version < 5.0) throw new NotSupportedException("Versions of MySQL prior to 5.0 are not currently supported");
				if (version < 5.1) return "5.0";
				if (version < 5.5) return "5.1";
				if (version < 5.6) return "5.5";
				if (version < 5.7) return "5.6";
				return "5.7";
				
			}
		}

		protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
		{
			return new MySqlProviderManifest(manifestToken);
		}

#if NET_40_OR_GREATER
		protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");
			MySqlConnection conn = connection as MySqlConnection;
			if (conn == null)
				throw new ArgumentException(Resources.ConnectionMustBeOfTypeMySqlConnection, "connection");
			// Ensure a valid provider manifest token.
			string providerManifestToken = this.GetDbProviderManifestToken(connection);
			string query = DbCreateDatabaseScript(providerManifestToken, storeItemCollection);

			using (MySqlConnection c = new MySqlConnection())
			{
				MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(conn.ConnectionString);
				string dbName = sb.Database;
				sb.Database = null;
				c.ConnectionString = sb.ConnectionString;
				c.Open();

				string fullQuery = String.Format("CREATE DATABASE `{0}`; USE `{0}`; {1}", dbName, query);
				MySqlScript s = new MySqlScript(c, fullQuery);
				s.Execute();
			}
		}

		protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");
			MySqlConnection conn = connection as MySqlConnection;
			if (conn == null)
				throw new ArgumentException(Resources.ConnectionMustBeOfTypeMySqlConnection, "connection");

			MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
			builder.ConnectionString = conn.ConnectionString;
			string dbName = builder.Database;
			builder.Database = null;

			using (MySqlConnection c = new MySqlConnection(builder.ConnectionString))
			{
				c.Open();
				DataTable table = c.GetSchema("Databases", new string[] { dbName });
				if (table != null && table.Rows.Count == 1) return true;
				return false;
			}
		}

		protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");
			MySqlConnection conn = connection as MySqlConnection;
			if (conn == null)
				throw new ArgumentException(Resources.ConnectionMustBeOfTypeMySqlConnection, "connection");

			MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
			builder.ConnectionString = conn.ConnectionString;
			string dbName = builder.Database;
			builder.Database = null;

			using (MySqlConnection c = new MySqlConnection(builder.ConnectionString))
			{
				c.Open();
				MySqlCommand cmd = new MySqlCommand(String.Format("DROP DATABASE IF EXISTS `{0}`", dbName), c);
				if (commandTimeout.HasValue)
					cmd.CommandTimeout = commandTimeout.Value;
				cmd.ExecuteNonQuery();
			}
		}

		protected override string DbCreateDatabaseScript(string providerManifestToken,
				StoreItemCollection storeItemCollection)
		{
			StringBuilder sql = new StringBuilder();

			sql.AppendLine("-- MySql script");
			sql.AppendLine("-- Created on " + DateTime.Now);

			if (serverVersion == null)
				serverVersion = new Version(providerManifestToken == null ? "5.5" : providerManifestToken);

			foreach (EntityContainer container in storeItemCollection.GetItems<EntityContainer>())
			{
				// now output the tables
				foreach (EntitySet es in container.BaseEntitySets.OfType<EntitySet>())
				{
					sql.Append(GetTableCreateScript(es));
				}

				// now output the foreign keys
				foreach (AssociationSet a in container.BaseEntitySets.OfType<AssociationSet>())
				{
					sql.Append(GetAssociationCreateScript(a.ElementType));
				}
			}

			return sql.ToString();
		}

		private string GetAssociationCreateScript(AssociationType a)
		{
			StringBuilder sql = new StringBuilder();
			StringBuilder keySql = new StringBuilder();

			if (a.IsForeignKey)
			{
				EntityType childType = (EntityType)a.ReferentialConstraints[0].ToProperties[0].DeclaringType;
				EntityType parentType = (EntityType)a.ReferentialConstraints[0].FromProperties[0].DeclaringType;
		string fkName = a.Name;
				if (fkName.Length > 64)
				{
					fkName = "FK_" + Guid.NewGuid().ToString().Replace("-", "");
				}
				sql.AppendLine(String.Format(
						"ALTER TABLE `{0}` ADD CONSTRAINT {1}", _pluralizedNames[ childType.Name ], fkName));
				sql.Append("\t FOREIGN KEY (");
				string delimiter = "";
				foreach (EdmProperty p in a.ReferentialConstraints[0].ToProperties)
				{
					EdmMember member;
					if (!childType.KeyMembers.TryGetValue(p.Name, false, out member))
						keySql.AppendLine(String.Format(
								"ALTER TABLE `{0}` ADD KEY (`{1}`);", _pluralizedNames[childType.Name], p.Name));
					sql.AppendFormat("{0}{1}", delimiter, p.Name);          
					delimiter = ", ";
				}
				sql.AppendLine(")");        
				delimiter = "";
				sql.Append(String.Format("\tREFERENCES `{0}` (", _pluralizedNames[parentType.Name]));
				foreach (EdmProperty p in a.ReferentialConstraints[0].FromProperties)
				{
					EdmMember member;
					if (!parentType.KeyMembers.TryGetValue(p.Name, false, out member))
						keySql.AppendLine(String.Format(
								"ALTER TABLE `{0}` ADD KEY (`{1}`);", _pluralizedNames[parentType.Name], p.Name));
					sql.AppendFormat("{0}{1}", delimiter, p.Name);
					delimiter = ", ";
				}
				sql.AppendLine(")");
				OperationAction oa = a.AssociationEndMembers[0].DeleteBehavior;
				sql.AppendLine(String.Format(" ON DELETE {0} ON UPDATE {1};",
					oa == OperationAction.None ? "NO ACTION" : oa.ToString(), "NO ACTION"));
				sql.AppendLine();
			}

			keySql.Append(sql.ToString());
			return keySql.ToString();
		}

#endif

		private Dictionary<string, string> _pluralizedNames = new Dictionary<string, string>();
		private List<string> _guidIdentityColumns;

		internal string GetTableCreateScript(EntitySet entitySet)
		{
			EntityType e = entitySet.ElementType;
			_guidIdentityColumns = new List<string>();

			string typeName = null;
			if (_pluralizedNames.ContainsKey(e.Name))
			{
				typeName = _pluralizedNames[e.Name];
			}
			else
			{
				_pluralizedNames.Add(e.Name, 
					(string)entitySet.MetadataProperties["Table"].Value == null ? 
					e.Name : (string)entitySet.MetadataProperties["Table"].Value);
				typeName = _pluralizedNames[e.Name];
			}

			StringBuilder sql = new StringBuilder("CREATE TABLE ");
			sql.AppendFormat("`{0}`(", typeName );
			string delimiter = "";
			foreach (EdmProperty c in e.Properties)
			{
				sql.AppendFormat("{0}{1}\t`{2}` {3}{4}", delimiter, Environment.NewLine, c.Name,
						GetColumnType(c.TypeUsage), GetFacetString(c, e.KeyMembers.Contains(c.Name)));
				delimiter = ", ";
			}
			sql.AppendLine(");");
			sql.AppendLine();
			if (e.KeyMembers.Count > 0)
			{
				sql.Append(String.Format(
						"ALTER TABLE `{0}` ADD PRIMARY KEY (", typeName ));
				delimiter = "";
				foreach (EdmMember m in e.KeyMembers)
				{
					sql.AppendFormat("{0}`{1}`", delimiter, m.Name);
					delimiter = ", ";
				}
				sql.AppendLine(");");
				sql.AppendLine();
			}
			if (_guidIdentityColumns.Count > 0)
			{
				sql.AppendLine("DELIMITER ||");
				sql.AppendLine(string.Format("CREATE TRIGGER `{0}` BEFORE INSERT ON `{1}`", typeName + "_IdentityTgr", typeName));
				sql.AppendLine("\tFOR EACH ROW BEGIN");
				foreach (string guidColumn in _guidIdentityColumns)
				{
					if (e.KeyMembers.Contains(guidColumn))
					{
						sql.AppendLine(string.Format("\t\tDROP TEMPORARY TABLE IF EXISTS tmpIdentity_{0};", typeName));
						sql.AppendLine(string.Format("\t\tCREATE TEMPORARY TABLE tmpIdentity_{0} (guid CHAR(36))ENGINE=MEMORY;", typeName));
						sql.AppendLine(string.Format("\t\tSET @var_{0} = UUID();", guidColumn));
						sql.AppendLine(string.Format("\t\tINSERT INTO tmpIdentity_{0} VALUES(@var_{1});", typeName, guidColumn));
						sql.AppendLine(string.Format("\t\tSET new.{0} = @var_{0};", guidColumn));
					}
					else
						sql.AppendLine(string.Format("\t\tSET new.{0} = UUID();", guidColumn));
				}
				sql.AppendLine("\tEND ||");
				sql.AppendLine("DELIMITER ;");
			}
			sql.AppendLine();
			return sql.ToString();
		}

		internal string GetColumnType(TypeUsage type)
		{
			string t = type.EdmType.Name;
			if (t.StartsWith("u", StringComparison.OrdinalIgnoreCase))
			{
				t = t.Substring(1).ToUpperInvariant() + " UNSIGNED";
			}
			else if (String.Compare(t, "guid", true) == 0)
				return "CHAR(36) BINARY";      
			return t;
		}

		private string GetFacetString(EdmProperty column, bool IsKeyMember)
		{
			StringBuilder sql = new StringBuilder();
			Facet facet;
			Facet fcDateTimePrecision = null;

			ReadOnlyMetadataCollection<Facet> facets = column.TypeUsage.Facets;

			if (column.TypeUsage.EdmType.BaseType.Name == "String")
			{
				// types tinytext, mediumtext, text & longtext don't have a length.
				if (!column.TypeUsage.EdmType.Name.EndsWith("text", StringComparison.OrdinalIgnoreCase))
				{
					if (facets.TryGetValue("MaxLength", true, out facet))
					{
							sql.AppendFormat(" ({0})", facet.Value);
					}
				}
			}
			else if (column.TypeUsage.EdmType.BaseType.Name == "Decimal")
			{
				Facet fcScale;
				Facet fcPrecision;
				if (facets.TryGetValue("Scale", true, out fcScale) && facets.TryGetValue("Precision", true, out fcPrecision))
				{
					// Enforce scale to a reasonable value.
					int scale = fcScale.Value == null ? 0 : ( int )( byte )fcScale.Value;
					if (scale == 0)
						scale = MySqlProviderManifest.DEFAULT_DECIMAL_SCALE;
					sql.AppendFormat("( {0}, {1} ) ", fcPrecision.Value, scale);
				}
			}
			else if (column.TypeUsage.EdmType.BaseType.Name == "DateTime")
			{
				if (serverVersion >= new Version(5, 6) && facets.TryGetValue("Precision", true, out fcDateTimePrecision))
				{        
					 if (Convert.ToByte(fcDateTimePrecision.Value) >= 1)
							sql.AppendFormat("( {0} ) ", fcDateTimePrecision.Value);            
				}
			}
			

			if (facets.TryGetValue("Nullable", true, out facet) && (bool)facet.Value == false)
				sql.Append(" NOT NULL");

			if (facets.TryGetValue("StoreGeneratedPattern", true, out facet))
			{
				if (facet.Value.Equals(StoreGeneratedPattern.Identity))
				{

					if (column.TypeUsage.EdmType.BaseType.Name.StartsWith("Int"))
						sql.Append(" AUTO_INCREMENT UNIQUE");
					else if (column.TypeUsage.EdmType.BaseType.Name == "Guid")
						_guidIdentityColumns.Add(column.Name);
					else if (serverVersion >= new Version(5, 6) && column.TypeUsage.EdmType.BaseType.Name == "DateTime")
						sql.AppendFormat(" DEFAULT CURRENT_TIMESTAMP{0}", fcDateTimePrecision != null && Convert.ToByte(fcDateTimePrecision.Value) >= 1 ? "( " + fcDateTimePrecision.Value.ToString() + " )" : "");                              
					else
						throw new MySqlException("Invalid identity column type.");
				}      
			}
			return sql.ToString();
		}

		private bool IsStringType(TypeUsage type)
		{
			return false;
		}
	}
}
