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
using MySql.Web.Security;
using System.Collections.Specialized;
using System.Web.Security;
using MySql.Data.MySqlClient;

namespace MySql.Web.Tests
{
  public class SimpleMembership : IUseFixture<SetUpWeb>, IDisposable
  {
    private string _connString;
    private readonly string _userTable = "UserProfile";
    private readonly string _userIdColumn = "UserId";
    private readonly string _userNameColumn = "UserName";
    private readonly string _userName = "New User";
    private readonly string _pass = "password";
    private  MySqlSimpleMembershipProvider _simpleProvider;

    public void SetFixture(SetUpWeb data)
    {
      _connString = data.GetConnectionString(true);

      _simpleProvider = new MySqlSimpleMembershipProvider();

      var _config = new NameValueCollection();
      _config.Add("connectionStringName", "LocalMySqlServer");
      _config.Add("userTableName", "UserProfile");
      _config.Add("userIdColumn", "UserId");
      _config.Add("userNameColumn", "UserName");

      _simpleProvider.Initialize("Test", _config);

      MySqlWebSecurity.InitializeDatabaseConnection(_connString, "MySqlSimpleMembership", _userTable, _userIdColumn, _userNameColumn, true, true);
    }

    public void Dispose()
    {
    }

    [Fact]
    public void CreateUserAndAccountTest()
    {
      try
      {
        MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
        Assert.True(MySqlWebSecurity.UserExists(_userName));
        var user = MySqlHelper.ExecuteDataRow(_connString, string.Format("select * from {0} where {1} = '{2}'", _userTable, _userNameColumn, _userName));
        Assert.NotNull(user);
        Assert.Equal(_userName, user[_userNameColumn]);

        Assert.True(_simpleProvider.ValidateUser(_userName, _pass));
        //We need to mock the login because in that method there is a call to "FormsAuthentication.SetAuthCookie" which causes an "Object reference not set to an instance of an object" exception, because the test doesn't run on web application context
        //Assert.True(MySqlWebSecurity.Login(_userName, _pass));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    //We need to mock this test because there is no data on Membership object, there is no user available because login doesn't add it to the context
    //[Fact]
    public void ChangePasswordTest()
    {
      try
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
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void ConfirmAccountWithTokenTest()
    {
      try
      {
        var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
        Assert.True(MySqlWebSecurity.UserExists(_userName));
        Assert.True(MySqlWebSecurity.ConfirmAccount(token));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void ConfirmAccountWithUserAndTokenTest()
    {
      try
      {
        var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
        Assert.True(MySqlWebSecurity.UserExists(_userName));
        Assert.True(MySqlWebSecurity.ConfirmAccount(_userName, token));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void ConfirmAccountWithoutTokenTest()
    {
      try
      {
        var token = "falsetoken";
        MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
        Assert.True(MySqlWebSecurity.UserExists(_userName));
        Assert.False(MySqlWebSecurity.ConfirmAccount(token));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void CreatedDateTest()
    {
      try
      {
        MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
        Assert.NotEqual(DateTime.MinValue, MySqlWebSecurity.GetCreateDate(_userName));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void DeleteTest()
    {
      try
      {
        _simpleProvider.CreateUserAndAccount(_userName, _pass, false, null);
        Assert.True(_simpleProvider.DeleteAccount(_userName));
        _simpleProvider.CreateAccount(_userName, _pass, false);
        Assert.True(_simpleProvider.DeleteUser(_userName, true));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void UserIsConfirmedTest()
    {
      try
      {
        MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
        Assert.False(MySqlWebSecurity.IsConfirmed(_userName));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void UserIsLockedOutTest()
    {
      try
      {
        MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
        Assert.False(MySqlWebSecurity.IsAccountLockedOut(_userName, 5, 60));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    [Fact]
    public void PasswordTest()
    {
      try
      {
        MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
        Assert.Equal(DateTime.MinValue, MySqlWebSecurity.GetLastPasswordFailureDate(_userName));
        Assert.NotEqual(DateTime.MinValue, MySqlWebSecurity.GetPasswordChangedDate(_userName));
        Assert.Equal(0, MySqlWebSecurity.GetPasswordFailuresSinceLastSuccess(_userName));
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    //Password reset token must be assigned to the user but that field is not added in any part of the code, so maybe that field must be handled manually by the user
    // should we handle this functionality? WebMatrix.WebData.SimpleMembershipProvider doesn't handle it
    //[Fact]
    public void PasswordResetTokenTest()
    {
      try
      {
        var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
        int userID = MySqlWebSecurity.GetUserId(_userName);
        Assert.True(MySqlWebSecurity.ConfirmAccount(token));
        var newToken = MySqlWebSecurity.GeneratePasswordResetToken(_userName, 1440);
        Assert.NotEqual(null, newToken);
        Assert.Equal(MySqlWebSecurity.GetUserIdFromPasswordResetToken(newToken), userID);
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        CleanUser();
      }
    }

    private void CleanUser()
    {
      _simpleProvider.DeleteUser(_userName, true);
    }
  }
}
