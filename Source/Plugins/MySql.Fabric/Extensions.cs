// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;

namespace MySql.Fabric
{
  public static class Extensions
  {
#if NET_40_OR_GREATER
    public static void SetFabricProperties(this MySqlConnection connection, string groupId = null,
      string table = null, string key = null, FabricServerModeEnum? mode = null, FabricScopeEnum? scope = null)
#else
    public static void SetFabricProperties(this MySqlConnection connection, string groupId,
      string table, string key, FabricServerModeEnum? mode, FabricScopeEnum? scope)
#endif
    {
      if (!string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(table))
        throw new MySqlFabricException(Properties.Resources.errorGroupAndTable);
      if (!string.IsNullOrEmpty(groupId))
      {
        connection.Settings.FabricGroup = groupId;
        connection.Settings.ShardingTable = null;
        connection.Settings.ShardingKey = null;
      }
      if (!string.IsNullOrEmpty(table))
      {
        connection.Settings.ShardingTable = table;
        connection.Settings.FabricGroup = null;
      }
      /*if (!string.IsNullOrEmpty(key)) */
      connection.Settings.ShardingKey = key;
      connection.Settings.FabricServerMode = (int?)mode;
      connection.Settings.FabricScope = (int?)scope;
    }
  }
}
