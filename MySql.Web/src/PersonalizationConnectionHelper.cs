// Copyright © 2014 Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using System.Web.Hosting;
using MySql.Data.MySqlClient;

namespace MySql.Web
{
  internal sealed class MySQLPersonalizationConnectionHelper
  {
    private MySqlConnection _connection;
    
    public MySqlConnection Connection
    {
      get {
        return _connection;
      }      
    }

    public bool Opened
    {
      get {
        return _connection.State == System.Data.ConnectionState.Open;      
      }    
    }

    internal MySQLPersonalizationConnectionHelper(string connectionString)
    {
      if (string.IsNullOrEmpty(connectionString))
      {
        throw new ArgumentNullException("connectionString");      
      }

      _connection = new MySqlConnection(connectionString);
    }

    internal void OpenConnection(bool impersonateContext)
    {
      if (_connection.State != System.Data.ConnectionState.Open)
      {
        if (impersonateContext)
        {
          using (HostingEnvironment.Impersonate())
          {
            _connection.Open();
          }
        }
        else
          _connection.Open();
      }    
    }

    internal void CloseConnection()
    {
      if (_connection.State != System.Data.ConnectionState.Closed)
        _connection.Close();    
    }

  }
}
