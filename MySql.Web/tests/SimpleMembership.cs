// Copyright © 2014, 2015, Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using MySql.Web.Security;
using System.Collections.Specialized;
using MySql.Data.MySqlClient;
using System.Web.Security;

namespace MySql.Web.Tests
{
  public class SimpleMembership : WebTestBase
  {
    private readonly string _userTable = "UserProfile";
    private readonly string _userIdColumn = "UserId";
    private readonly string _userNameColumn = "UserName";
    private readonly string _userName = "New User";
    private readonly string _pass = "password";
    private MySqlSimpleMembershipProvider _simpleProvider;
    private MySqlSimpleRoleProvider _simpleRoleProvider;

    public SimpleMembership()
    {
      _simpleProvider = new MySqlSimpleMembershipProvider();
      _simpleRoleProvider = new MySqlSimpleRoleProvider();

      var _config = new NameValueCollection();
      _config.Add("connectionStringName", "LocalMySqlServer");
      _config.Add("userTableName", "UserProfile");
      _config.Add("userIdColumn", "UserId");
      _config.Add("userNameColumn", "UserName");

      _simpleProvider.Initialize("Test", _config);
      _simpleRoleProvider.Initialize("TestRoleProvider", _config);

      MySqlWebSecurity.InitializeDatabaseConnection(ConnectionString, "MySqlSimpleMembership", _userTable, _userIdColumn, _userNameColumn, true, true);
    }

    [Fact]
    public void CheckIfRoleNotExists()
    {
      var roleExists = _simpleRoleProvider.RoleExists("roleName");
      Assert.False(roleExists);
    }

    [Fact]
    public void CheckIfRoleExists()
    {
      if (!Roles.RoleExists("Administrator"))
      {
        _simpleRoleProvider.CreateRole("Administrator");
        var roleExists = _simpleRoleProvider.RoleExists("Administrator");
        Assert.True(roleExists);
      }
    }

    [Fact]
    public void CreateUserAndAccountTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.True(MySqlWebSecurity.UserExists(_userName));
      var user = MySqlHelper.ExecuteDataRow(ConnectionString, string.Format("select * from {0} where {1} = '{2}'", _userTable, _userNameColumn, _userName));
      Assert.NotNull(user);
      Assert.Equal(_userName, user[_userNameColumn]);

      Assert.True(_simpleProvider.ValidateUser(_userName, _pass));
      //We need to mock the login because in that method there is a call to "FormsAuthentication.SetAuthCookie" which causes an "Object reference not set to an instance of an object" exception, because the test doesn't run on web application context
      //Assert.True(MySqlWebSecurity.Login(_userName, _pass));    
    }

    //We need to mock this test because there is no data on Membership object, there is no user available because login doesn't add it to the context
    //[Fact]
    public void ChangePasswordTest()
    {
      string newPass = "newpassword";
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.True(MySqlWebSecurity.UserExists(_userName));

      //We need to mock the login because in that method there is a call to "FormsAuthentication.SetAuthCookie" which causes an "Object reference not set to an instance of an object" exception, because the test doesn't run on web application context

      Assert.True(_simpleProvider.ValidateUser(_userName, _pass));
      //Assert.True(MySqlWebSecurity.Login(_userName, _pass));

      Assert.True(MySqlWebSecurity.ChangePassword(_userName, _pass, newPass));

      Assert.True(_simpleProvider.ValidateUser(_userName, newPass));
      //Assert.True(MySqlWebSecurity.Login(_userName, newPass));
    }

    [Fact]
    public void ConfirmAccountWithTokenTest()
    {
      var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.True(MySqlWebSecurity.UserExists(_userName));
      Assert.True(MySqlWebSecurity.ConfirmAccount(token));
    }

    [Fact]
    public void ConfirmAccountWithUserAndTokenTest()
    {
      var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.True(MySqlWebSecurity.UserExists(_userName));
      Assert.True(MySqlWebSecurity.ConfirmAccount(_userName, token));
    }

    [Fact]
    public void ConfirmAccountWithoutTokenTest()
    {
      var token = "falsetoken";
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.True(MySqlWebSecurity.UserExists(_userName));
      Assert.False(MySqlWebSecurity.ConfirmAccount(token));
    }

    [Fact]
    public void CreatedDateTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.NotEqual(DateTime.MinValue, MySqlWebSecurity.GetCreateDate(_userName));
    }

    [Fact]
    public void DeleteTest()
    {
      _simpleProvider.CreateUserAndAccount(_userName, _pass, false, null);
      Assert.True(_simpleProvider.DeleteAccount(_userName));
      _simpleProvider.CreateAccount(_userName, _pass, false);
      Assert.True(_simpleProvider.DeleteUser(_userName, true));
    }

    [Fact]
    public void UserIsConfirmedTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.False(MySqlWebSecurity.IsConfirmed(_userName));
    }

    [Fact]
    public void UserIsLockedOutTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.False(MySqlWebSecurity.IsAccountLockedOut(_userName, 5, 60));
    }

    [Fact]
    public void PasswordTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.Equal(DateTime.MinValue, MySqlWebSecurity.GetLastPasswordFailureDate(_userName));
      Assert.NotEqual(DateTime.MinValue, MySqlWebSecurity.GetPasswordChangedDate(_userName));
      Assert.Equal(0, MySqlWebSecurity.GetPasswordFailuresSinceLastSuccess(_userName));
    }

    //Password reset token must be assigned to the user but that field is not added in any part of the code, so maybe that field must be handled manually by the user
    // should we handle this functionality? WebMatrix.WebData.SimpleMembershipProvider doesn't handle it
    [Fact(Skip ="Fix Me")]
    public void PasswordResetTokenTest()
    {
      var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      int userID = MySqlWebSecurity.GetUserId(_userName);
      Assert.True(MySqlWebSecurity.ConfirmAccount(token));
      var newToken = MySqlWebSecurity.GeneratePasswordResetToken(_userName, 1440);
      Assert.NotEqual(null, newToken);
      Assert.Equal(MySqlWebSecurity.GetUserIdFromPasswordResetToken(newToken), userID);
    }

    public void Dispose()
    {
      _simpleProvider.DeleteUser(_userName, true);
    }
  }
}
