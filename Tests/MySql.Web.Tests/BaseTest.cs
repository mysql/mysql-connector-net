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

//  This code was contributed by Sean Wright (srwright@alcor.concordia.ca) on 2007-01-12
//  The copyright was assigned and transferred under the terms of
//  the MySQL Contributor License Agreement (CLA)

using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Data;
using System.Configuration;
using System.Reflection;
using System;
using System.Web.Configuration;
using System.Collections.Specialized;
using System.Diagnostics;
using MySql.Data.MySqlClient.Tests;
using System.Resources;
using MySql.Web.Common;
using MySql.Web.Security;

namespace MySql.Web.Tests
{
  public class BaseWebTest : BaseTest
  {
    protected override void Initialize()
    {
      base.Initialize();
      MySqlConnection.ClearAllPools();
      AddConnectionStringToConfigFile();
    }

    public override void Setup()
    {
      base.Setup();

      for (int ver = 1; ver <= SchemaManager.Version; ver++)
        LoadSchema(ver);
    }

    protected void LoadSchema(int version)
    {
      if (version < 1) return;

      MySQLMembershipProvider provider = new MySQLMembershipProvider();

      ResourceManager r = new ResourceManager("MySql.Web.Properties.Resources", typeof(MySQLMembershipProvider).Assembly);
      string schema = r.GetString(String.Format("schema{0}", version));
      MySqlScript script = new MySqlScript(conn);
      script.Query = schema;

      try
      {
        script.Execute();
      }
      catch (MySqlException ex)
      {
        if (ex.Number == 1050 && version == 7)
        {
          // Schema7 performs several renames of tables to their lowercase representation. 
          // If the current server OS does not support renaming to lowercase, then let's just continue.          
          script.Query = "UPDATE my_aspnet_schemaversion SET version=7";
          script.Execute();
        }
      }
    }

    private void SetupRootConnection()
    {
      string fullname = Assembly.GetExecutingAssembly().FullName;
      string[] parts = fullname.Split(new char[] { '=' });
      string[] versionParts = parts[1].Split(new char[] { '.' });
      database0 = String.Format("db{0}{1}{2}-a", versionParts[0], versionParts[1], port - 3300);
      database1 = String.Format("db{0}{1}{2}-b", versionParts[0], versionParts[1], port - 3300);

      string connStr = GetConnectionString(rootUser, rootPassword, false);
      rootConn = new MySqlConnection(connStr + ";database=mysql");
      rootConn.Open();
    }

    private void AddConnectionStringToConfigFile()
    {
      Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      config.ConnectionStrings.ConnectionStrings.Remove("LocalMySqlServer");
      config.Save();
      ConfigurationManager.RefreshSection("connectionStrings");

      ConnectionStringSettings css = new ConnectionStringSettings();
      css.ConnectionString = String.Format(
          "server={0};uid={1};password={2};database={3};pooling=false;port={4}",
          this.host, this.user, this.password, this.database0, this.port);
      css.Name = "LocalMySqlServer";
      config.ConnectionStrings.ConnectionStrings.Add(css);
      config.Save();
      ConfigurationManager.RefreshSection("connectionStrings");
    }
  }
}
