// Copyright © 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Data.Common;
using System.Security;
using System.Security.Permissions;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Enables the provider to help ensure that a user has a security level adequate for accessing data.
  /// </summary>
  [Serializable]
  public sealed class MySqlClientPermission : DBDataPermission
  {

  #region Contructors

  public MySqlClientPermission(PermissionState permissionState)
    : base(permissionState)
  {
  }

  private MySqlClientPermission(MySqlClientPermission permission):base(permission)
  { 
  }

  internal MySqlClientPermission(MySqlClientPermissionAttribute permissionAttribute):base(permissionAttribute)
  { 
  }

	internal MySqlClientPermission (DBDataPermission permission)
		: base (permission)
	{
	}

  internal MySqlClientPermission(string connectionString)
    : base(PermissionState.None)
  {
    if ((connectionString == null) || connectionString.Length == 0)
      base.Add(string.Empty, string.Empty, KeyRestrictionBehavior.AllowOnly);
    else
      base.Add(connectionString, string.Empty, KeyRestrictionBehavior.AllowOnly);
  }


  #endregion
   
  #region Methods
  
  /// <summary>
  /// Adds a new connection string with set of restricted keywords to the MySqlClientPermission object 
  /// </summary>
  ///<param name="connectionString">Settings to be used for the connection</param>
  ///<param name="restrictions">Keywords to define the restrictions</param>
  ///<param name="behavior">KeyRestrictionBehavior to be used</param>
  public override void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior)
  {
    base.Add(connectionString, restrictions, behavior);
  }
  
  /// <summary>
  /// Returns MySqlClientPermission as an IPermission
  /// </summary>
  /// <returns></returns>
  public override IPermission Copy()
  {
    return new MySqlClientPermission(this);
  }

  #endregion

  }
}
