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

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Data.AdoDotNet;
using Microsoft.VisualStudio.Data;
using System.Data.Common;

namespace MySql.Data.VisualStudio
{
  [Guid("D949EA95-EDA1-4b65-8A9E-266949A99360")]
  class MySqlProviderObjectFactory : AdoDotNetProviderObjectFactory
  {
    private static DbProviderFactory factory;

    internal static DbProviderFactory Factory
    {
      get
      {
        if (factory == null)
          factory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
        return factory;
      }
    }

    public override object CreateObject(Type objType)
    {
      if (objType == typeof(DataConnectionUIControl))
        return new MySqlDataConnectionUI();
      else if (objType == typeof(DataConnectionProperties))
        return new MySqlConnectionProperties();
      else if (objType == typeof(DataConnectionSupport))
        return new MySqlConnectionSupport();
      if (objType == typeof(DataSourceSpecializer))
        return new MySqlDataSourceSpecializer();
      else if (objType == typeof(DataConnectionPromptDialog))
        return new MySqlDataConnectionPromptDialog();
      else
        return base.CreateObject(objType);
    }
  }
}
