// Copyright © 2013, 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Web.Security;
using System.Collections.Specialized;
using System.Data;
using System.Configuration.Provider;
using Xunit;
using MySql.Web.Security;
using MySql.Data.MySqlClient;

namespace MySql.Web.Tests
{
  public class UserManagement : WebTestBase 
  {
    private MySQLMembershipProvider provider { get; set; }

    private void CreateUserWithFormat(MembershipPasswordFormat format)
    {
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("passwordStrengthRegularExpression", "bar.*");
      config.Add("passwordFormat", format.ToString());
      provider.Initialize(null, config);

      // create the user
      MembershipCreateStatus status;
      provider.CreateUser("foo", "barbar!", "foo@bar.com", null, null, true, null, out status);
      Assert.Equal(MembershipCreateStatus.Success, status);

      // verify that the password format is hashed.
      DataTable table = FillTable("SELECT * FROM my_aspnet_membership");
      MembershipPasswordFormat rowFormat =
        (MembershipPasswordFormat)Convert.ToInt32(table.Rows[0]["PasswordFormat"]);
      Assert.Equal(format, rowFormat);

      //  then attempt to verify the user
      Assert.True(provider.ValidateUser("foo", "barbar!"));
      
    }

    [Fact]
    public void CreateUserWithHashedPassword()
    {
      CreateUserWithFormat(MembershipPasswordFormat.Hashed);      
      //Cleanup
      provider.DeleteUser("foo", true);
    }

    [Fact]
    public void CreateUserWithEncryptedPasswordWithAutoGenKeys()
    {
      //TODO check this test logic
      try
      {
        CreateUserWithFormat(MembershipPasswordFormat.Encrypted);
      }
      catch (ProviderException)
      {
      }
      //Cleanup
      provider.DeleteUser("foo", true);
    }

    [Fact]
    public void CreateUserWithClearPassword()
    {
      CreateUserWithFormat(MembershipPasswordFormat.Clear);
      //Cleanup
      provider.DeleteUser("foo", true);
    }

    /// <summary>
    /// Bug #34792 New User/Changing Password Validation Not working. 
    /// </summary>
    [Fact]
    public void ChangePassword()
    {
      CreateUserWithFormat(MembershipPasswordFormat.Hashed);   
      ArgumentException ex = Assert.Throws<ArgumentException>(() => provider.ChangePassword("foo", "barbar!", "bar2"));
      
      Assert.Equal("newPassword", ex.ParamName);
      Assert.True(ex.Message.Contains("length of parameter"));
      ArgumentException ex1 = Assert.Throws<ArgumentException>(() => provider.ChangePassword("foo", "barbar!", "barbar2"));
      Assert.Equal("newPassword", ex1.ParamName);
      Assert.True(ex1.Message.Contains("alpha numeric"));

      // now test regex strength testing
      bool result = provider.ChangePassword("foo", "barbar!", "zzzxxx!");
      Assert.False(result);

      // now do one that should work
      result = provider.ChangePassword("foo", "barbar!", "barfoo!");
      Assert.True(result);

      provider.ValidateUser("foo", "barfoo!");

      //Cleanup
      provider.DeleteUser("foo", true);
    }

    /// <summary>
    /// Bug #34792 New User/Changing Password Validation Not working. 
    /// </summary>
    [Fact]
    public void CreateUserWithErrors()
    {
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("passwordStrengthRegularExpression", "bar.*");
      config.Add("passwordFormat", "Hashed");
      provider.Initialize(null, config);

      // first try to create a user with a password not long enough
      MembershipCreateStatus status;
      MembershipUser user = provider.CreateUser("foo", "xyz",
        "foo@bar.com", null, null, true, null, out status);
      Assert.Null(user);
      Assert.Equal(MembershipCreateStatus.InvalidPassword, status);

      // now with not enough non-alphas
      user = provider.CreateUser("foo", "xyz1234",
        "foo@bar.com", null, null, true, null, out status);
      Assert.Null(user);
      Assert.Equal(MembershipCreateStatus.InvalidPassword, status);

      // now one that doesn't pass the regex test
      user = provider.CreateUser("foo", "xyzxyz!",
        "foo@bar.com", null, null, true, null, out status);
      Assert.Null(user);
      Assert.Equal(MembershipCreateStatus.InvalidPassword, status);

      // now one that works
      user = provider.CreateUser("foo", "barbar!",
        "foo@bar.com", null, null, true, null, out status);
      Assert.NotNull(user);
      Assert.Equal(MembershipCreateStatus.Success, status);

      //Cleanup
      provider.DeleteUser("foo", true);

    }

    [Fact]
    public void DeleteUser()
    {
      CreateUserWithFormat(MembershipPasswordFormat.Hashed);      
      Assert.True(provider.DeleteUser("foo", true));
      DataTable table = FillTable("SELECT * FROM my_aspnet_membership");
      Assert.Equal(0, table.Rows.Count);
      table = FillTable("SELECT * FROM my_aspnet_users");
      Assert.Equal(0, table.Rows.Count);

      CreateUserWithFormat(MembershipPasswordFormat.Hashed);     
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      provider.Initialize(null, config);
      Assert.True(Membership.DeleteUser("foo", false));
      table = FillTable("SELECT * FROM my_aspnet_membership");
      Assert.Equal(0, table.Rows.Count);
      table = FillTable("SELECT * FROM my_aspnet_users");
      Assert.Equal(1, table.Rows.Count);
    }

    [Fact]
    public void FindUsersByName()
    {
      CreateUserWithFormat(MembershipPasswordFormat.Hashed);      
      int records;
      MembershipUserCollection users = provider.FindUsersByName("F%", 0, 10, out records);
      Assert.Equal(1, records);
      Assert.Equal("foo", users["foo"].UserName);

      //Cleanup
      provider.DeleteUser("foo", true);
    }

    [Fact]
    public void FindUsersByEmail()
    {
      CreateUserWithFormat(MembershipPasswordFormat.Hashed);   

      int records;
      MembershipUserCollection users = provider.FindUsersByEmail("foo@bar.com", 0, 10, out records);
      Assert.Equal(1, records);
      Assert.Equal("foo", users["foo"].UserName);
      
      //Cleanup
      provider.DeleteUser("foo", true);
    }

    [Fact]
    public void TestCreateUserOverrides()
    {
      try
      {
        MembershipCreateStatus status;
        Membership.CreateUser("foo", "barbar!", null, "question", "answer", true, out status);
        int records;
        MembershipUserCollection users = Membership.FindUsersByName("F%", 0, 10, out records);
        Assert.Equal(1, records);
        Assert.Equal("foo", users["foo"].UserName);

        Membership.CreateUser("test", "barbar!", "myemail@host.com",
          "question", "answer", true, out status);
        users = Membership.FindUsersByName("T%", 0, 10, out records);
        Assert.Equal(1, records);
        Assert.Equal("test", users["test"].UserName);
      }
      catch (Exception ex)
      {
        Assert.True(ex.Message != String.Empty, ex.Message);
      }

      //Cleanup
      Membership.DeleteUser("test", true);      
      Membership.DeleteUser("foo", true);
    }

    [Fact]
    public void NumberOfUsersOnline()
    {
      int numOnline = Membership.GetNumberOfUsersOnline();
      Assert.Equal(0, numOnline);

      MembershipCreateStatus status;
      Membership.CreateUser("foo", "barbar!", null, "question", "answer", true, out status);
      Membership.CreateUser("foo2", "barbar!", null, "question", "answer", true, out status);

      numOnline = Membership.GetNumberOfUsersOnline();
      Assert.Equal(2, numOnline);

      //Cleanup
      Membership.DeleteUser("foo");
      Membership.DeleteUser("foo2");      
    }

    [Fact]
    public void UnlockUser()
    {
      MembershipCreateStatus status;
      Membership.CreateUser("foo", "barbar!", null, "question", "answer", true, out status);
      Assert.False(Membership.ValidateUser("foo", "bar2"));
      Assert.False(Membership.ValidateUser("foo", "bar3"));
      Assert.False(Membership.ValidateUser("foo", "bar3"));
      Assert.False(Membership.ValidateUser("foo", "bar3"));
      Assert.False(Membership.ValidateUser("foo", "bar3"));

      // the user should be locked now so the right password should fail
      Assert.False(Membership.ValidateUser("foo", "barbar!"));

      MembershipUser user = Membership.GetUser("foo");
      Assert.True(user.IsLockedOut);

      Assert.True(user.UnlockUser());
      user = Membership.GetUser("foo");
      Assert.False(user.IsLockedOut);

      Assert.True(Membership.ValidateUser("foo", "barbar!"));

      //Cleanup
      Membership.DeleteUser("foo");
    }

    [Fact]
    public void GetUsernameByEmail()
    {
      MembershipCreateStatus status;
      Membership.CreateUser("foo", "barbar!", "foo@bar.com", "question", "answer", true, out status);
      string username = Membership.GetUserNameByEmail("foo@bar.com");
      Assert.Equal("foo", username);

      username = Membership.GetUserNameByEmail("foo@b.com");
      Assert.Null(username);

      username = Membership.GetUserNameByEmail("  foo@bar.com   ");
      Assert.Equal("foo", username);
      
      //Cleanup
      Membership.DeleteUser("foo");
    }

    [Fact]
    public void UpdateUser()
    {
      MembershipCreateStatus status;
      Membership.CreateUser("foo", "barbar!", "foo@bar.com", "color", "blue", true, out status);
      Assert.Equal(MembershipCreateStatus.Success, status);

      MembershipUser user = Membership.GetUser("foo");

      user.Comment = "my comment";
      user.Email = "my email";
      user.IsApproved = false;
      user.LastActivityDate = new DateTime(2008, 1, 1);
      user.LastLoginDate = new DateTime(2008, 2, 1);
      Membership.UpdateUser(user);

      MembershipUser newUser = Membership.GetUser("foo");
      Assert.Equal(user.Comment, newUser.Comment);
      Assert.Equal(user.Email, newUser.Email);
      Assert.Equal(user.IsApproved, newUser.IsApproved);
      Assert.Equal(user.LastActivityDate, newUser.LastActivityDate);
      Assert.Equal(user.LastLoginDate, newUser.LastLoginDate);

      //Cleanup
      Membership.DeleteUser("foo");
    }

    private void ChangePasswordQAHelper(MembershipUser user, string pw, string newQ, string newA)
    {
      try
      {
        user.ChangePasswordQuestionAndAnswer(pw, newQ, newA);        
      }
      catch (ArgumentNullException ane)
      {
        Assert.Equal("password", ane.ParamName);
      }
      catch (ArgumentException)
      {
        Assert.NotNull(pw);
      }
    }

    [Fact]
    public void ChangePasswordQuestionAndAnswer()
    {
      MembershipCreateStatus status;
      Membership.CreateUser("foo", "barbar!", "foo@bar.com", "color", "blue", true, out status);
      Assert.Equal(MembershipCreateStatus.Success, status);

      MembershipUser user = Membership.GetUser("foo");
      ChangePasswordQAHelper(user, "", "newQ", "newA");
      ChangePasswordQAHelper(user, "barbar!", "", "newA");
      ChangePasswordQAHelper(user, "barbar!", "newQ", "");
      ChangePasswordQAHelper(user, null, "newQ", "newA");

      bool result = user.ChangePasswordQuestionAndAnswer("barbar!", "newQ", "newA");
      Assert.True(result);

      user = Membership.GetUser("foo");
      Assert.Equal("newQ", user.PasswordQuestion);
      
      //Cleanup
      Membership.DeleteUser("foo");

    }

    [Fact]
    public void GetAllUsers()
    {
      MembershipCreateStatus status;
      // first create a bunch of users
      for (int i = 0; i < 100; i++)
        Membership.CreateUser(String.Format("foo{0}", i), "barbar!", null,
          "question", "answer", true, out status);

      MembershipUserCollection users = Membership.GetAllUsers();
      Assert.Equal(100, users.Count);
      int index = 0;
      foreach (MembershipUser user in users)
        Assert.Equal(String.Format("foo{0}", index++), user.UserName);

      int total;
      users = Membership.GetAllUsers(2, 10, out total);
      Assert.Equal(10, users.Count);
      Assert.Equal(100, total);
      index = 0;
      foreach (MembershipUser user in users)
        Assert.Equal(String.Format("foo2{0}", index++), user.UserName);

      //Cleanup
      MySqlHelper.ExecuteScalar(Connection, "DELETE FROM my_aspnet_users");
      MySqlHelper.ExecuteScalar(Connection, "DELETE FROM my_aspnet_membership");      
    }

    private void GetPasswordHelper(bool requireQA, bool enablePasswordRetrieval, string answer)
    {
      MembershipCreateStatus status;
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("requiresQuestionAndAnswer", requireQA ? "true" : "false");
      config.Add("enablePasswordRetrieval", enablePasswordRetrieval ? "true" : "false");
      config.Add("passwordFormat", "clear");
      config.Add("applicationName", "/");
      config.Add("writeExceptionsToEventLog", "false");
      provider.Initialize(null, config);

      provider.CreateUser("foo", "barbar!", "foo@bar.com", "color", "blue", true, null, out status);
     
      string password = string.Empty;
      if (!enablePasswordRetrieval)
      {
        if (requireQA && answer != null)
        {
          Exception ex = Assert.Throws<MembershipPasswordException>(() => provider.GetPassword("foo", answer));
        }
        else
        {
          Exception ex = Assert.Throws<ProviderException>(() => provider.GetPassword("foo", answer));
          Assert.Equal(ex.Message, "Password Retrieval Not Enabled.");
        }        
      }
      else
      {
        if (requireQA && answer != null)
        {
          provider.GetPassword("foo", answer);        
        }
        else if (requireQA && answer == null)
        {
          //Incorrect password answer.        
          Assert.Throws<MembershipPasswordException>(() => provider.GetPassword("foo", answer));          
        }
        else
        {
          password = provider.GetPassword("foo", answer);
          Assert.Equal("barbar!", password);
        }
      }
            
      //Cleanup
      provider.DeleteUser("foo", true);
    }

    [Fact]
    public void GetPassword()
    {
      GetPasswordHelper(false, false, null);
      GetPasswordHelper(false, true, null);
      GetPasswordHelper(true, true, null);
      GetPasswordHelper(true, true, "blue");
    }

    /// <summary>
    /// Bug #38939 MembershipUser.GetPassword(string answer) fails when incorrect answer is passed.
    /// </summary>
    [Fact]
    public void GetPasswordWithWrongAnswer()
    {
      MembershipCreateStatus status;
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("requiresQuestionAndAnswer", "true");
      config.Add("enablePasswordRetrieval", "true");
      config.Add("passwordFormat", "Encrypted");
      config.Add("applicationName", "/");
      provider.Initialize(null, config);
      provider.CreateUser("foo", "barbar!", "foo@bar.com", "color", "blue", true, null, out status);

      MySQLMembershipProvider provider2 = new MySQLMembershipProvider();
      NameValueCollection config2 = new NameValueCollection();
      config2.Add("connectionStringName", "LocalMySqlServer");
      config2.Add("requiresQuestionAndAnswer", "true");
      config2.Add("enablePasswordRetrieval", "true");
      config2.Add("passwordFormat", "Encrypted");
      config2.Add("applicationName", "/");
      provider2.Initialize(null, config2);     
      Assert.Throws<MembershipPasswordException>(() => provider2.GetPassword("foo", "wrong"));
      
      //Cleanup
      provider.DeleteUser("foo", true);
    }

    [Fact]
    public void GetUser()
    {
      //Resetting PK
      MySqlHelper.ExecuteScalar(Connection, "ALTER TABLE my_aspnet_users AUTO_INCREMENT = 1;");

      MembershipCreateStatus status;
      Membership.CreateUser("foo", "barbar!", null, "question", "answer", true, out status);
      MembershipUser user = Membership.GetUser(1);   
      Assert.Equal("foo", user.UserName);

      // now move the activity date back outside the login
      // window
      user.LastActivityDate = new DateTime(2008, 1, 1);
      Membership.UpdateUser(user);

      user = Membership.GetUser("foo");
      Assert.False(user.IsOnline);

      user = Membership.GetUser("foo", true);
      Assert.True(user.IsOnline);

      // now move the activity date back outside the login
      // window again
      user.LastActivityDate = new DateTime(2008, 1, 1);
      Membership.UpdateUser(user);

      user = Membership.GetUser(1);
      Assert.False(user.IsOnline);

      user = Membership.GetUser(1, true);
      Assert.True(user.IsOnline);

      //Cleanup
      Membership.DeleteUser("foo");
    }

    [Fact (Skip = "Fix this")]
    public void FindUsers()
    {
      MembershipCreateStatus status;
      for (int i = 0; i < 100; i++)
        Membership.CreateUser(String.Format("boo{0}", i), "barbar!", null,
          "question", "answer", true, out status);
      for (int i = 0; i < 100; i++)
        Membership.CreateUser(String.Format("foo{0}", i), "barbar!", null,
          "question", "answer", true, out status);
      for (int i = 0; i < 100; i++)
        Membership.CreateUser(String.Format("schmoo{0}", i), "barbar!", null,
          "question", "answer", true, out status);


      MembershipUserCollection users = Membership.FindUsersByName("fo%");
      Assert.Equal(100, users.Count);

      int total;
      users = Membership.FindUsersByName("fo%", 2, 10, out total);
      Assert.Equal(10, users.Count);
      Assert.Equal(100, total);
      int index = 0;
      foreach (MembershipUser user in users)
        Assert.Equal(String.Format("foo2{0}", index++), user.UserName);
      
      //Cleanup
      MySqlHelper.ExecuteScalar(Connection, "DELETE FROM my_aspnet_users");
      MySqlHelper.ExecuteScalar(Connection, "DELETE FROM my_aspnet_membership");      

    }

    [Fact]
    public void CreateUserWithNoQA()
    {
      MembershipCreateStatus status;
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("requiresQuestionAndAnswer", "true");
      config.Add("passwordFormat", "clear");
      config.Add("applicationName", "/");
      provider.Initialize(null, config);
     
      Exception ex = Assert.Throws<ArgumentException>(() => provider.CreateUser("foo", "barbar!", "foo@bar.com", "color", null, true, null, out status));
      Assert.True(ex.Message.StartsWith("Password answer supplied is invalid", StringComparison.OrdinalIgnoreCase));

      ex = Assert.Throws<ArgumentException>(() => provider.CreateUser("foo", "barbar!", "foo@bar.com", "", "blue", true, null, out status));
      Assert.True(ex.Message.StartsWith("Password question supplied is invalid", StringComparison.OrdinalIgnoreCase));

    }

    [Fact]
    public void MinRequiredAlpha()
    {
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("minRequiredNonalphanumericCharacters", "3");
      provider.Initialize(null, config);

      MembershipCreateStatus status;
      MembershipUser user = provider.CreateUser("foo", "pw!pass", "email", null, null, true, null, out status);
      Assert.Null(user);

      user = provider.CreateUser("foo", "pw!pa!!", "email", null, null, true, null, out status);
      Assert.NotNull(user);

      //Cleanup
      Membership.DeleteUser("foo");

    }

    /// <summary>
    /// Bug #35332 GetPassword() don't working (when PasswordAnswer is NULL) 
    /// </summary>
    [Fact]
    public void GetPasswordWithNullValues()
    {
      MembershipCreateStatus status;
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("requiresQuestionAndAnswer", "false");
      config.Add("enablePasswordRetrieval", "true");
      config.Add("passwordFormat", "clear");
      config.Add("applicationName", "/");
      provider.Initialize(null, config);

      MembershipUser user = provider.CreateUser("foo", "barbar!", "foo@bar.com", null, null, true, null, out status);
      Assert.NotNull(user);

      string pw = provider.GetPassword("foo", null);
      Assert.Equal("barbar!", pw);

      //Cleanup
      Membership.DeleteUser("foo");
    }

    /// <summary>
    /// Bug #35336 GetPassword() return wrong password (when format is encrypted) 
    /// </summary>
    [Fact]
    public void GetEncryptedPassword()
    {
      MembershipCreateStatus status;
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("requiresQuestionAndAnswer", "false");
      config.Add("enablePasswordRetrieval", "true");
      config.Add("passwordFormat", "encrypted");
      config.Add("applicationName", "/");
      provider.Initialize(null, config);

      MembershipUser user = provider.CreateUser("foo", "barbar!", "foo@bar.com", null, null, true, null, out status);
      Assert.NotNull(user);

      string pw = provider.GetPassword("foo", null);
      Assert.Equal("barbar!", pw);

      //Cleanup
      provider.DeleteUser("foo", true);
    }

    /// <summary>
    /// Bug #42574	ValidateUser does not use the application id, allowing cross application login
    /// </summary>
    [Fact]
    public void CrossAppLogin()
    {
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("passwordStrengthRegularExpression", "bar.*");
      config.Add("passwordFormat", "Clear");
      provider.Initialize(null, config);
      MembershipCreateStatus status;
      provider.CreateUser("foo", "bar!bar", null, null, null, true, null, out status);

      MySQLMembershipProvider provider2 = new MySQLMembershipProvider();
      NameValueCollection config2 = new NameValueCollection();
      config2.Add("connectionStringName", "LocalMySqlServer");
      config2.Add("applicationName", "/myapp");
      config2.Add("passwordStrengthRegularExpression", ".*");
      config2.Add("passwordFormat", "Clear");
      provider2.Initialize(null, config2);

      bool worked = provider2.ValidateUser("foo", "bar!bar");
      Assert.Equal(false, worked);

      //Cleanup
      provider.DeleteUser("foo", true);
    }

    /// <summary>
    /// Bug #41408	PasswordReset not possible when requiresQuestionAndAnswer="false"
    /// </summary>
    [Fact]
    public void ResetPassword()
    {
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("passwordStrengthRegularExpression", "bar.*");
      config.Add("passwordFormat", "Clear");
      config.Add("requiresQuestionAndAnswer", "false");
      provider.Initialize(null, config);

      MembershipCreateStatus status;
      provider.CreateUser("foo", "bar!bar", null, null, null, true, null, out status);

      MembershipUser u = provider.GetUser("foo", false);
      string newpw = provider.ResetPassword("foo", null);

      //Cleanup
      provider.DeleteUser("foo", true);
    }

    /// <summary>
    /// Bug #59438	setting Membership.ApplicationName has no effect
    /// </summary>
    [Fact]
    public void ChangeAppName()
    {
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("passwordStrengthRegularExpression", "bar.*");
      config.Add("passwordFormat", "Clear");
      provider.Initialize(null, config);
      MembershipCreateStatus status;
      provider.CreateUser("foo", "bar!bar", null, null, null, true, null, out status);
      Assert.True(status == MembershipCreateStatus.Success);

      MySQLMembershipProvider provider2 = new MySQLMembershipProvider();
      NameValueCollection config2 = new NameValueCollection();
      config2.Add("connectionStringName", "LocalMySqlServer");
      config2.Add("applicationName", "/myapp");
      config2.Add("passwordStrengthRegularExpression", "foo.*");
      config2.Add("passwordFormat", "Clear");
      provider2.Initialize(null, config2);
      provider2.CreateUser("foo2", "foo!foo", null, null, null, true, null, out status);
      Assert.True(status == MembershipCreateStatus.Success);

      provider.ApplicationName = "/myapp";
      Assert.False(provider.ValidateUser("foo", "bar!bar"));
      Assert.True(provider.ValidateUser("foo2", "foo!foo"));

      //Cleanup
      //provider.DeleteUser("foo2", true);
      //provider.DeleteUser("foo", true);
      
      //Cleanup
      MySqlHelper.ExecuteScalar(Connection, "DELETE FROM my_aspnet_users");
      MySqlHelper.ExecuteScalar(Connection, "DELETE FROM my_aspnet_membership");      

    }

    [Fact]
    public void GetUserLooksForExactUsername()
    {
      MembershipCreateStatus status;
      Membership.CreateUser("code", "thecode!", null, "question", "answer", true, out status);

      MembershipUser user = Membership.GetUser("code");
      Assert.Equal("code", user.UserName);

      user = Membership.GetUser("co_e");
      Assert.Null(user);

      //Cleanup
      Membership.DeleteUser("code");
    }

    [Fact]
    public void GetUserNameByEmailLooksForExactEmail()
    {
      MembershipCreateStatus status;
      Membership.CreateUser("code", "thecode!", "code@mysql.com", "question", "answer", true, out status);

      string username = Membership.GetUserNameByEmail("code@mysql.com");
      Assert.Equal("code", username);

      username = Membership.GetUserNameByEmail("co_e@mysql.com");
      Assert.Null(username);

      //Cleanup
      Membership.DeleteUser("code");
    }

    /// <summary>
    /// MySqlBug 73411, Oracle Bug: 19453313
    /// </summary>
    [Fact]
    public void CreateUserWithLeadingAndTrailingSpaces()
    {
      provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("autogenerateschema", "true");
      config.Add("applicationName", "/");
      provider.Initialize(null, config);

      MembershipCreateStatus status;
      MembershipUser muser1 = provider.CreateUser(" with trailing space ", "dummypassword1!", "w@w1.w", "yes", "yes", true, null, out status);
      Assert.NotEqual(null, muser1);
      MembershipUser muser2 = provider.GetUser("with trailing space", false);
      Assert.NotEqual(null, muser1);

      Roles.CreateRole("SomeRole");
      Assert.True(Roles.GetAllRoles().Length > 0);

      bool isInRole = Roles.IsUserInRole(muser2.UserName, "SomeRole");
      Assert.False(isInRole);

      Roles.AddUserToRole(muser2.UserName, "SomeRole");

      isInRole = Roles.IsUserInRole(muser2.UserName, "SomeRole");
      Assert.True(isInRole);
    }
  }
}
