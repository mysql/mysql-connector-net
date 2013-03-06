// Copyright © 2008, 2013, Oracle and/or its affiliates. All rights reserved.
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
 * This file contains implementation of data object enumerator.
 */
using System;
using System.Data;
using System.Diagnostics;
using Microsoft.VisualStudio.Data;
using Microsoft.VisualStudio.Data.AdoDotNet;
using System.Data.Common;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Implements custom database objects enumerator for MySQL databases. 
  /// Uses information_schema database to enumerate objects.
  /// </summary>
  public class MySqlDataObjectEnumerator : AdoDotNetObjectEnumerator
  {
    /// <summary>
    /// Enumerates items for a set of data objects of the specified type 
    /// with the specified restrictions and sort string, if supported. 
    /// </summary>
    /// <param name="typeName">Name of the type of the object to enumerate.</param>
    /// <param name="items">
    /// The set of items to enumerate, specified as strings where named items are available, 
    /// otherwise as indexes. In cases in which a data provider does not support items 
    /// filtering, this parameter is ignored.
    /// NOT SUPPORTED.
    /// </param>
    /// <param name="restrictions">
    /// A set of filtering restrictions to apply to the set of returned objects.
    /// </param>
    /// <param name="sort">
    /// A sort string, which follows syntax for the SQL Server ORDER BY clause. 
    /// The actual sort order should be source-based; that is, if the client is 
    /// English and the source is Chinese, the sort should be applied in Chinese.
    /// NOT SUPPORTED.
    /// </param>
    /// <param name="parameters">
    /// An array whose contents are defined by the given implementation of 
    /// EnumerateObjects, and which is specified by the Data Object Support 
    /// XML file. Information supplied in this parameter can be used to provide 
    /// extra data indicating how to perform the enumeration, allowing 
    /// implementations of this method to be more data driven.
    /// NOT USED.
    /// </param>
    /// <returns>
    /// Returns a DataReader object containing the results of the enumeration call.
    /// </returns>
    public override DataReader EnumerateObjects(string typeName,
        object[] items, object[] restrictions, string sort, object[] parameters)
    {
      if (typeName == null)
        throw new ArgumentNullException("typeName");

      if (typeName == String.Empty)
        return EnumerateRoot();
      else return EnumerateSchemaObjects(parameters[0] as string, restrictions);
    }

    private DataReader EnumerateRoot()
    {
      DbConnection conn = (DbConnection)Connection.GetLockedProviderObject();
      try
      {
        DataTable table = new DataTable();
        table.Columns.Add("SERVER_NAME");
        table.Columns.Add("CATALOG_NAME");
        table.Columns.Add("SCHEMA_NAME");
        DataRow row = table.NewRow();
        row["SERVER_NAME"] = conn.DataSource;
        row["SCHEMA_NAME"] = conn.Database;
        table.Rows.Add(row);
        return new AdoDotNetDataTableReader(table);
      }
      finally
      {
        Connection.UnlockProviderObject();
      }
    }

    private DataReader EnumerateSchemaObjects(string typeName, object[] restrictions)
    {
      DbConnection conn = (DbConnection)Connection.GetLockedProviderObject();
      try
      {
        string[] rest;
        DataTable tables = null;

        if (restrictions != null)
        {
          int i = 0;
          rest = new string[restrictions.Length];
          foreach (object o in restrictions)
          {
            if (o is DBNull)
              rest[i++] = "";
            else
              rest[i++] = (string)o;
          }
          tables = conn.GetSchema(typeName, rest);
        }
        else
          tables = conn.GetSchema(typeName);

        foreach (DataRow row in tables.Rows)
          row["TABLE_CATALOG"] = DBNull.Value;
        return new AdoDotNetDataTableReader(tables);
      }
      finally
      {
        Connection.UnlockProviderObject();
      }
    }

    /*
                // Chose restricitions array
                object[] appliedRestrictions;
                if (typeName.Equals(RootDescriptor.TypeName, StringComparison.InvariantCultureIgnoreCase))
                {

                    appliedRestrictions = new object[] { 
                                            ConnectionWrapper.ServerName, 
                                            ConnectionWrapper.Schema 
                                            };
                }
                else
                {
                    appliedRestrictions = restrictions;
                }

                DataTable table;
                try
                {
                    // Enumerate objects into table
                    table = ObjectDescriptor.EnumerateObjects(ConnectionWrapper, typeName, appliedRestrictions, sort);
                }
                catch (DbException e)
                {
                    SqlErrorDialog.ShowError(e, ConnectionWrapper.GetFullStatus());
                    throw;
                }
                catch (Exception e)
                {
                    UIHelper.ShowError(e);
                    throw;
                }

                // Validate table
                if (table == null)
                {
                    Debug.Fail("Failed to enumerate objects of type '" + typeName + "'!");
                    return null;
                }

                // Enumerete objects in to DataReader
                return new AdoDotNetDataTableReader(table);*/

    #region Connection wrapper
    /// <summary>
    /// Returns wrapper for the underlying connection. Creates it at the first call.
    /// </summary>
    /*        private DataConnectionWrapper ConnectionWrapper
            {
                get
                {
                    if (connectionWrapperRef == null)                
                        connectionWrapperRef = new DataConnectionWrapper(Connection);
                    return connectionWrapperRef;
                }
            }
            /// <summary>
            /// Used to stroe connection wrapper.
            /// </summary>
            private DataConnectionWrapper connectionWrapperRef;*/
    #endregion

  }
}
