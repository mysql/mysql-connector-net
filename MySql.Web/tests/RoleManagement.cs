// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Specialized;
using System.Web.Security;
using Xunit;
using MySql.Web.Security;

namespace MySql.Web.Tests
{
  public class RoleManagement : WebTestBase
  {
    private MySQLMembershipProvider membershipProvider;
    private MySQLRoleProvider roleProvider;

    public RoleManagement()
    {
      membershipProvider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      membershipProvider.Initialize(null, config);

      roleProvider = new MySQLRoleProvider();
      roleProvider.Initialize(null, config);
    }

    private void AddUser(string username, string password)
    {
      MembershipCreateStatus status;
      membershipProvider.CreateUser(username, password, "foo@bar.com", null,
        null, true, null, out status);
      Assert.False(status != MembershipCreateStatus.Success, "User creation failed");
    }

    private void AttemptToAddUserToRole(string username, string role)
    {
      try
      {
        roleProvider.AddUsersToRoles(new string[] { username },
          new string[] { role });
      }
      catch (ArgumentException)
      {
      }
    }


    [Fact]
    public void CreateAndDeleteRoles()
    {
      // Add the role
      roleProvider.CreateRole("Administrator");
      string[] roles = roleProvider.GetAllRoles();
      Assert.Equal(1, roles.Length);
      Assert.Equal("Administrator", roles[0]);
      roleProvider.DeleteRole("Administrator", false);
    }   

    [Fact]
    public void AddUserToRole()
    {
      AddUser("eve", "eveeve!");
      roleProvider.CreateRole("Administrator");

      roleProvider.AddUsersToRoles(new string[] { "eve" },
        new string[] { "Administrator" });
      Assert.True(roleProvider.IsUserInRole("eve", "Administrator"));

      roleProvider.RemoveUsersFromRoles(new string[] { "eve" }, new string[] { "Administrator" });
      Assert.False(roleProvider.IsUserInRole("eve", "Administrator"));

      roleProvider.DeleteRole("Administrator", false);      
      Assert.Equal(0, roleProvider.GetAllRoles().Length);

      //clean up
      membershipProvider.DeleteUser("eve", true);

    }

    /// <summary>
    /// Bug #38243 Not Handling non existing user when calling AddUsersToRoles method 
    /// </summary>
    [Fact]
    public void AddNonExistingUserToRole()
    {
      roleProvider.CreateRole("Administrator");
      roleProvider.AddUsersToRoles(new string[] { "eve" },
        new string[] { "Administrator" });
      Assert.True(roleProvider.IsUserInRole("eve", "Administrator"));

      //Cleanup
      roleProvider.RemoveUsersFromRoles(new string[] { "eve" }, new string[] { "Administrator" });
      roleProvider.DeleteRole("Administrator", false);      
      
    }


    [Fact]
    public void IllegalRoleAndUserNames()
    {
      AttemptToAddUserToRole("test", null);
      AttemptToAddUserToRole("test", "");
      roleProvider.CreateRole("Administrator");
      AttemptToAddUserToRole(null, "Administrator");
      AttemptToAddUserToRole("", "Administrator");

      //Cleanup
      roleProvider.DeleteRole("Administrator", false); 
    }

    [Fact]
    public void AddUserToRoleWithRoleClass()
    {
      roleProvider.CreateRole("Administrator");

      MembershipCreateStatus status;
      membershipProvider.CreateUser("eve", "eve1@eve", "eve@boo.com",
        "question", "answer", true, null, out status);
      Assert.Equal(MembershipCreateStatus.Success, status);

      roleProvider.AddUsersToRoles(new string[] { "eve"}, new string[] { "Administrator"});
      Assert.True(roleProvider.IsUserInRole("eve", "Administrator"));
      
      //Cleanup     
      membershipProvider.DeleteUser("eve", true);
      roleProvider.DeleteRole("Administrator", true);

    }

    [Fact]
    public void IsUserInRoleCrossDomain()
    {
      MySQLMembershipProvider provider = new MySQLMembershipProvider();
      NameValueCollection config1 = new NameValueCollection();
      config1.Add("connectionStringName", "LocalMySqlServer");
      config1.Add("applicationName", "/");
      config1.Add("passwordStrengthRegularExpression", "bar.*");
      config1.Add("passwordFormat", "Clear");
      provider.Initialize(null, config1);
      MembershipCreateStatus status;
      provider.CreateUser("foo", "bar!bar", null, null, null, true, null, out status);

      MySQLMembershipProvider provider2 = new MySQLMembershipProvider();
      NameValueCollection config2 = new NameValueCollection();
      config2.Add("connectionStringName", "LocalMySqlServer");
      config2.Add("applicationName", "/myapp");
      config2.Add("passwordStrengthRegularExpression", ".*");
      config2.Add("passwordFormat", "Clear");
      provider2.Initialize(null, config2);

      roleProvider = new MySQLRoleProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      roleProvider.Initialize(null, config);

      MySQLRoleProvider r2 = new MySQLRoleProvider();
      NameValueCollection configr2 = new NameValueCollection();
      configr2.Add("connectionStringName", "LocalMySqlServer");
      configr2.Add("applicationName", "/myapp");
      r2.Initialize(null, configr2);

      roleProvider.CreateRole("Administrator");
      roleProvider.AddUsersToRoles(new string[] { "foo" },
        new string[] { "Administrator" });
      Assert.False(r2.IsUserInRole("foo", "Administrator"));

      roleProvider.DeleteRole("Administrator", false);
      Assert.Equal(0, roleProvider.GetAllRoles().Length);

      //Cleanup
      provider.DeleteUser("foo",true);      

    }

    /// <summary>
    /// Testing fix for Calling RoleProvider.RemoveUserFromRole() causes an exception due to a wrong table being used.
    /// http://clustra.no.oracle.com/orabugs/bug.php?id=14405338 / http://bugs.mysql.com/bug.php?id=65805.
    /// </summary>
    [Fact]
    public void TestUserRemoveFindFromRole()
    {
      roleProvider = new MySQLRoleProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      roleProvider.Initialize(null, config);

      AddUser("eve", "eveeve!");
      roleProvider.CreateRole("Administrator");
      roleProvider.AddUsersToRoles(new string[] { "eve" },
        new string[] { "Administrator" });
      Assert.True(roleProvider.IsUserInRole("eve", "Administrator"));
      string[] users = roleProvider.FindUsersInRole("Administrator", "eve");
      Assert.Equal(1, users.Length);
      Assert.Equal("eve", users[0]);
      roleProvider.RemoveUsersFromRoles(new string[] { "eve" }, new string[] { "Administrator" });
      Assert.False(roleProvider.IsUserInRole("eve", "Administrator"));
      
      //Cleanup
      membershipProvider.DeleteUser("eve", true);
      roleProvider.DeleteRole("Administrator", false);     
     
    }

  }
}
