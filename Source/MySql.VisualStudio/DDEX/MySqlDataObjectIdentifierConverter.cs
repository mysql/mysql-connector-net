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
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Data.AdoDotNet;
using Microsoft.VisualStudio.Data;
using System.Data.Common;

namespace MySql.Data.VisualStudio
{
  public class MySqlDataObjectIdentifierConverter : AdoDotNetObjectIdentifierConverter
  {
    private DataConnection connection;

    public MySqlDataObjectIdentifierConverter(DataConnection c)
      : base(c)
    {
      connection = c;
    }

    protected override string BuildString(string typeName, string[] identifierParts, bool forDisplay)
    {
      string id = String.Empty;

      if (typeName == "Table")
      {
        DbConnection c = connection.ConnectionSupport.ProviderObject as DbConnection;
        string dbName = FormatPart("Table", c.Database, true);

        if (identifierParts.Length == 1)
          id = identifierParts[0];
        if (identifierParts.Length == 2 && identifierParts[0] == dbName)
          id = identifierParts[1];
        if (identifierParts.Length == 3 && identifierParts[1] == dbName)
          id = identifierParts[2];
      }
      if (id == String.Empty || forDisplay)
        id = base.BuildString(typeName, identifierParts, forDisplay);
      return id;
    }
  }
}
