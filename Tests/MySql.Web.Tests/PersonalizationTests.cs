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
using Xunit;
using System.Data;
using MySql.Web.Common;
using MySql.Web.Security;
using System.Collections.Specialized;
using System.Configuration.Provider;
using MySql.Data.MySqlClient;
using System.Web.Security;
using System.Configuration;
using MySql.Web.Personalization;
using System.Web.UI.WebControls.WebParts;
using MySql.Data.MySqlClient.Tests;


namespace MySql.Web.Tests
{
  public class PersonalizationTests : IUseFixture<SetUpWeb>, IDisposable
  {
    private SetUpWeb st;
    private long applicationId;

    public void SetFixture(SetUpWeb data)
    {
      st = data;
      st.rootConn.Close();
      st.rootConn = new MySqlConnection("server=localhost;userid=root;pwd=;database=" + st.conn.Database + ";port=" + st.port);
      st.rootConn.Open();     
    }

    private void CreateDataForSharedScope()
    {
      var cmd = new MySqlCommand();
      cmd.CommandText = @"insert into my_aspnet_applications(name,description) values('\\', '\\')";
      cmd.Connection = st.conn;
      cmd.ExecuteNonQuery();
      applicationId = cmd.LastInsertedId;

      // Add my_aspnet_paths
      var pathId = new Guid();
      cmd.CommandText = @"insert into my_aspnet_paths(applicationId, pathid, path, loweredpath) values(" + applicationId +
                          ",'" + pathId.ToString() + @"', '~/default.aspx', '~/default.aspx')";
      cmd.Connection = st.conn;
      cmd.ExecuteNonQuery();

      
      // personalization all users      
      byte[] settings = Utils.CreateBlob(1000);

      cmd.CommandText = @"insert into my_aspnet_personalizationallusers(pathid, pagesettings, lastUpdatedDate) values(" +
                        "'" + pathId.ToString() + "',   @pageSettings, @LastUpdatedDate)";
      cmd.Parameters.AddWithValue("@pathId", pathId);
      cmd.Parameters.AddWithValue("@pageSettings", settings);
      cmd.Parameters.AddWithValue("@LastUpdatedDate", DateTime.UtcNow);
      cmd.Connection = st.conn;
      cmd.ExecuteNonQuery();
    }


    private void CreateDataForUserScope()
    {
      var cmd = new MySqlCommand();
      cmd.CommandText = @"insert into my_aspnet_applications(name,description) values('\\', '\\')";
      cmd.Connection = st.conn;
      cmd.ExecuteNonQuery();
      applicationId = cmd.LastInsertedId;

     // Add my_aspnet_paths
      var pathId = new Guid();
      cmd.CommandText = @"insert into my_aspnet_paths(applicationId, pathid, path, loweredpath) values(" + applicationId +
                          ",'" + pathId.ToString() + @"', '~/default.aspx', '~/default.aspx')";
      cmd.Connection = st.conn;
      cmd.ExecuteNonQuery();
      
      // add user
      cmd.CommandText = @"insert into my_aspnet_users(applicationId, name, isAnonymous, lastActivityDate) values(" + applicationId +
                        @",'GabPC\\Gab', 0, @LastActivityDate)";
      cmd.Connection = st.conn;
      cmd.Parameters.AddWithValue("@LastActivityDate", DateTime.UtcNow);
      cmd.ExecuteNonQuery();
      var userId = cmd.LastInsertedId;
      
      // personalization per user      
      byte[] settings = Utils.CreateBlob(1000);
      
      cmd.CommandText = @"insert into my_aspnet_personalizationperuser(applicationId, pathid, userId, pagesettings, lastUpdatedDate) values(" + 
                        applicationId + ", '" + pathId.ToString() + "', " + userId + ", @pageSettings, @LastUpdatedDate)";
      cmd.Parameters.AddWithValue("@pageSettings", settings);
      cmd.Parameters.AddWithValue("@LastUpdatedDate", DateTime.UtcNow);
      cmd.Connection = st.conn;
      cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
      st.ExecuteSQLAsRoot("Delete from my_aspnet_profiles");
      st.ExecuteSQLAsRoot("Delete from my_aspnet_users");
      st.ExecuteSQLAsRoot("Delete from my_aspnet_applications");
      st.ExecuteSQLAsRoot("Delete from my_aspnet_personalizationperuser");
      st.ExecuteSQLAsRoot("Delete from my_aspnet_paths");
      st.ExecuteSQLAsRoot("Delete from my_aspnet_personalizationallusers");
    }

    private MySqlPersonalizationProvider InitPersonalizationProvider()
    {
      MySqlPersonalizationProvider p = new MySqlPersonalizationProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", @"\");
      config.Add("description", @"\");
      config.Add("autogenerateschema", "true");      
      p.Initialize(null, config);
      return p;
    }

    [Fact]
    public void CanFindState()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;
      var psq = new PersonalizationStateQuery();
      psq.UsernameToMatch = @"GabPC\\Gab";
      psq.PathToMatch = "~/default.aspx";
      psq.UserInactiveSinceDate = DateTime.UtcNow.AddMinutes(1);
      var collection = p.FindState(PersonalizationScope.User, psq, 1, 1, out totalRecords);
      Assert.Equal(1, totalRecords);
    }

    [Fact]
    public void CanGetCountofStateForUser()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;
      var psq = new PersonalizationStateQuery();
      psq.UsernameToMatch = @"GabPC\\Gab";
      psq.PathToMatch = "~/default.aspx";
      psq.UserInactiveSinceDate = DateTime.UtcNow.AddMinutes(1);
      //System.Threading.Thread.Sleep(1000);
      totalRecords = p.GetCountOfState(PersonalizationScope.User, psq);
      Assert.Equal(1, totalRecords);    
    }

    [Fact]
    public void CanGetCountofStateForAllUsers()
    {
      CreateDataForSharedScope();
      var p = InitPersonalizationProvider();
      int totalRecords;
      var psq = new PersonalizationStateQuery();      
      psq.PathToMatch = "~/default.aspx";
      psq.UserInactiveSinceDate = DateTime.UtcNow;
      totalRecords = p.GetCountOfState(PersonalizationScope.Shared, psq);
      Assert.Equal(1, totalRecords);    
    }

    [Fact]
    public void CanResetStateForUser()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;      
      string[] paths = new string[1];
      paths[0] = "~/default.aspx";

      string[] users = new string[1];
      users[0] = @"GabPC\Gab";

      totalRecords = p.ResetState(PersonalizationScope.User, paths, users);
      Assert.Equal(1, totalRecords);    
    }

    [Fact]
    public void CanResetStateForAllUsers()
    {
      CreateDataForSharedScope();
      var p = InitPersonalizationProvider();
      
      string[] paths = new string[1];
      paths[0] = "~/default.aspx";

      string[] users = new string[1];
      users[0] = @"GabPC\Gab";

      int totalRecords;
      totalRecords = p.ResetState(PersonalizationScope.Shared, paths, users);
      Assert.Equal(1, totalRecords);    
    }

    [Fact]
    public void CanResetAllState()
    {
      CreateDataForSharedScope();
      var p = InitPersonalizationProvider();           

      int totalRecords;
      totalRecords = p.ResetState(PersonalizationScope.Shared, null, null);
      Assert.Equal(1, totalRecords);
    }


    [Fact]
    public void CanResetUsertState()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;     

      totalRecords = p.ResetUserState("~/default.aspx", DateTime.MaxValue);
      Assert.Equal(1, totalRecords);
    }

  }
}
