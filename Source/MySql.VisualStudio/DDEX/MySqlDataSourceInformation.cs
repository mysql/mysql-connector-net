// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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

/*
 * This file contains class with data source specific information.
 */

using System;
using Microsoft.VisualStudio.Data;
using Microsoft.VisualStudio.Data.AdoDotNet;
using System.Data;
using System.Data.Common;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents a custom data source information class for MySQL
  /// </summary>
  public class MySqlDataSourceInformation : AdoDotNetDataSourceInformation
  {
    public const string DataSource = "DataSource";
    private DataTable values;

    /// <summary>
    /// Constructors fills available properties information
    /// </summary>
    /// <param name="connection">Reference to database connection object</param>
    public MySqlDataSourceInformation(DataConnection connection)
      : base(connection)
    {
      AddProperty(DataSource);
      AddProperty(DataSourceVersion);
      AddProperty(DefaultSchema);
      AddProperty(SupportsAnsi92Sql, true);
      AddProperty(SupportsQuotedIdentifierParts, true);
      AddProperty(IdentifierOpenQuote, "`");
      AddProperty(IdentifierCloseQuote, "`");
      AddProperty(ServerSeparator, ".");
      AddProperty(CatalogSupported, false);
      AddProperty(CatalogSupportedInDml, false);
      AddProperty(SchemaSupported, true);
      AddProperty(SchemaSupportedInDml, true);
      AddProperty(SchemaSeparator, ".");
      AddProperty(ParameterPrefix, "@");
      AddProperty(ParameterPrefixInName, true);
      AddProperty(DefaultCatalog, null);
    }

    internal void Refresh()
    {
      EnsureConnected();
      DbConnection c = (DbConnection)Connection.GetLockedProviderObject();
      values = c.GetSchema("DataSourceInformation");
      Connection.UnlockProviderObject();
    }

    #region Value retrieving

    public override object this[string propertyName]
    {
      get
      {
        // data source version can change so we need to 
        // refresh it here
        if (propertyName == "DataSourceVersion")
        {
          if (values == null)
            Refresh();
          return values.Rows[0]["DataSourceProductVersion"];
        }
        else
          return base[propertyName];
      }
    }

    /// <summary>
    /// Called to retrieve property value. Supports following custom properties:
    /// DataSource � MySQL server name.
    /// Database � default schema name.
    /// </summary>
    /// <param name="propertyName">Name of property to retrieve.</param>
    /// <returns>Property value</returns>
    protected override object RetrieveValue(string propertyName)
    {
      EnsureConnected();
      if (propertyName.Equals(DataSource, StringComparison.InvariantCultureIgnoreCase))
      {
        return (ProviderObject as DbConnection).DataSource;
      }
      else if (propertyName.Equals(DefaultSchema, StringComparison.InvariantCultureIgnoreCase))
      {
        return (ProviderObject as DbConnection).Database;
      }
      object value = base.RetrieveValue(propertyName);
      return value;
    }
    #endregion

    private void EnsureConnected()
    {
      if (Connection.State != DataConnectionState.Open)
        Connection.Open();
    }
  }
}
