// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
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
using System.Text;
using MySql.Data.MySqlClient;
using System.Security;
using System.Security.Permissions;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{  
//  public class MySqlClientPermissionTests : SetUpClass
  //{

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

  //}
}
