// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;
using System.Security;
using System.Security.Permissions;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{  
  public class MySqlClientPermissionTests : SetUpClass
  {

    //[Test]
    //[ExpectedException(typeof(System.Security.SecurityException))]
    //public void CanChangeConnectionSettingsOnClientPermission()
    //{
    //  MySqlConnection dummyconn = new MySqlConnection();
    //  PermissionSet permissionsSet = new PermissionSet(PermissionState.None);
    //  MySqlClientPermission permission = new MySqlClientPermission(PermissionState.None);

    //  // Allow only server localhost, any database, only with root user     
    //  permission.Add("server=localhost;", "database=; user id=root;", KeyRestrictionBehavior.PreventUsage);
    //  permissionsSet.AddPermission(permission);
    //  permissionsSet.PermitOnly();
    //  dummyconn.ConnectionString = "server=localhost; user id=test;includesecurityasserts=true;";
    //  dummyconn.Open();
    //  if (dummyconn.State == ConnectionState.Open) dummyconn.Close();
    //}

    //[Test]
    //public void CanAllowConnectionAfterPermitOnlyPermission()
    //{
    //  PermissionSet permissionset = new PermissionSet(PermissionState.None);

    //  MySqlClientPermission permission = new MySqlClientPermission(PermissionState.None);

    //  MySqlConnectionStringBuilder strConn = new MySqlConnectionStringBuilder(conn.ConnectionString);
      
    //  //// Allow connections only to specified database no additional optional parameters     
    //  permission.Add("server=localhost;User Id=root;database=" + strConn.Database + ";port=" + strConn.Port + ";", "", KeyRestrictionBehavior.PreventUsage);
    //  permission.PermitOnly();
    //  permissionset.AddPermission(permission);
    //  permissionset.Demand();

    //  // this conection should be allowed
    //  MySqlConnection dummyconn = new MySqlConnection();
    //  dummyconn.ConnectionString = "server=localhost;User Id=root;database=" + strConn.Database + ";port=" + strConn.Port + ";includesecurityasserts=true;";
    //  dummyconn.Open();    
    //  if (dummyconn.State == ConnectionState.Open) dummyconn.Close();

    //}

    //[Test]
    //[ExpectedException(typeof(System.Security.SecurityException))]
    //public void CanDenyConnectionAfterPermitOnlyPermission()
    //{      
    //  PermissionSet permissionset = new PermissionSet(PermissionState.None);

    //  MySqlClientPermission permission = new MySqlClientPermission(PermissionState.None);

    //  MySqlConnectionStringBuilder strConn = new MySqlConnectionStringBuilder(conn.ConnectionString);

    //  //// Allow connections only to specified database no additional optional parameters     
    //  permission.Add("server=localhost;User Id=root; database=" + strConn.Database + ";", "", KeyRestrictionBehavior.PreventUsage);
    //  permission.PermitOnly();
    //  permissionset.AddPermission(permission);
    //  permissionset.Demand();

    //  // this connection should NOT be allowed
    //  MySqlConnection dummyconn = new MySqlConnection();
    //  dummyconn.ConnectionString = "server=localhost;User Id=root;database=test;includesecurityasserts=true;";
    //  dummyconn.Open();      
    //  if (dummyconn.State == ConnectionState.Open) dummyconn.Close();        
    
    //}

  }
}
