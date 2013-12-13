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
 * This file contains implementation of custom connection support for MySQL.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Data;
using Microsoft.VisualStudio.Data.AdoDotNet;
using System.Globalization;
using MySql.Data.VisualStudio.Properties;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;


namespace MySql.Data.VisualStudio
{
  public class MySqlConnectionSupport : AdoDotNetConnectionSupport
  {
    private MySqlDataSourceInformation sourceInformation = null;

    public MySqlConnectionSupport()
      : base(MySqlConnectionProperties.InvariantName)
    {
    }

    /// <summary>
    /// Retrieves a service of the specified type. Following services are 
    /// supported:
    /// DataViewSupport � information about view schema.
    /// DataObjectSupport � information about object model.
    /// </summary>
    /// <param name="serviceType">A service type.</param>
    /// <returns>
    /// Returns the service of the specified type, or returns a null reference 
    /// if no service was found. 
    /// </returns>
    protected override object GetServiceImpl(Type serviceType)
    {
      if (serviceType == typeof(DataViewSupport))
      {
        return new MySqlDataViewSupport();
      }
      else if (serviceType == typeof(DataObjectSupport))
      {
        return new MySqlDataObjectSupport();
      }
      else if (serviceType == typeof(DataSourceInformation))
      {
        return new MySqlDataSourceInformation(Site as DataConnection);
      }
      else if (serviceType == typeof(DataObjectIdentifierConverter))
        return new MySqlDataObjectIdentifierConverter(Site as DataConnection);
      else
        return base.GetServiceImpl(serviceType);
    }

    public override bool Open(bool doPromptCheck)
    {
      // Open connection
      try
      {
        // Call base method first
        if (!base.Open(doPromptCheck))
          return false;

        // Validates expired password
        MySqlClient.MySqlConnection connection = base.Connection as MySqlClient.MySqlConnection;
        if (connection.IsPasswordExpired)
        {
          MySqlNewPasswordDialog newPasswordDialog = new MySqlNewPasswordDialog(connection);
          newPasswordDialog.ShowDialog();
        }
      }
      catch (MySqlException ex)
      {
        // If can prompt user data and is error 1045: Access denied for user
        if (doPromptCheck && ex.Number == 1045)
          return false;       

        // If can't prompt user for new authentication data, re-throw exception
        if (string.IsNullOrEmpty(base.Connection.ConnectionString))
          // If missing server & user, throw a more friendly error message
          throw new Exception( Resources.MissingServerAndUser, ex );
        throw;
      }

      // TODO: check server version compatibility

      // Rreturn true if everything is ok
      if (sourceInformation != null)
        sourceInformation.Refresh();
      return true;
    }
  }
}
