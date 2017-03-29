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
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.Routing;
using System.Web.WebPages;
using MySql.Web.Security;
using System.Collections.Specialized;
using System.Net;
using MySql.Web.Properties;

namespace MySql.Web.Security
{
  public static class MySqlWebSecurity
  {
    public static readonly string EnableSimpleMembershipKey = "enableSimpleMembership";
    private static readonly string MySqlMembershipProviderName = "MySqlMembershipProvider";
    private static readonly string MySqlRoleProviderName = "MySQLRoleProvider";

    #region Public
    /// <summary>
    /// Change the password for the user provided
    /// </summary>
    /// <param name="userName">User name</param>
    /// <param name="oldPassword">Current pasword</param>
    /// <param name="newPassword">New Password</param>
    /// <returns></returns>
    public static bool ChangePassword(string userName, string oldPassword, string newPassword)
    {
      ValidProvider();
      var user = Membership.GetUser(userName, true);
      return user.ChangePassword(oldPassword, newPassword);
    }

    /// <summary>
    /// Confirms user by confirmation token
    /// </summary>
    /// <param name="confirmationToken">Confirmation token</param>
    /// <returns>If user was confirmed</returns>
    public static bool ConfirmAccount(string confirmationToken)
    {
      var provider = ValidProvider();
      return provider.ConfirmAccount(confirmationToken);
    }

    /// <summary>
    /// Confirms user by confirmation token and user name
    /// </summary>
    /// <param name="userName">User name</param>
    /// <param name="confirmationToken">Confirmation token</param>
    /// <returns>If user was confirmed</returns>
    public static bool ConfirmAccount(string userName, string confirmationToken)
    {
      var provider = ValidProvider();
      return provider.ConfirmAccount(userName, confirmationToken);
    }

    /// <summary>
    /// Create user account
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <param name="password">User password</param>
    /// <param name="requireConfirmationToken">Confirmation token is required?</param>
    /// <returns>Confirmation token if required</returns>
    public static string CreateAccount(string userName, string password, bool requireConfirmationToken = false)
    {
      var provider = ValidProvider();
      return provider.CreateAccount(userName, password, requireConfirmationToken);
    }

    /// <summary>
    /// Create user and account
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <param name="password">User Password</param>
    /// <param name="additionalUserAttributes">Additional data for user table</param>
    /// <param name="requireConfirmationToken">Confirmation token is required?</param>
    /// <returns>Confirmation token if required</returns>
    public static string CreateUserAndAccount(string userName, string password, object additionalUserAttributes = null, bool requireConfirmationToken = false)
    {
      var provider = ValidProvider();
      IDictionary<string, object> userAttrs = additionalUserAttributes as RouteValueDictionary;

      if (userAttrs == null && additionalUserAttributes != null)
      {
        var attrs = additionalUserAttributes as IDictionary<string, object>;
        userAttrs = attrs != null ? new RouteValueDictionary(attrs) : new RouteValueDictionary(additionalUserAttributes);
      }

      return provider.CreateUserAndAccount(userName, password, requireConfirmationToken, userAttrs);
    }

    /// <summary>
    /// Get the date when the specified user was created
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <returns>Date created or minimum date value if the user was not found</returns>
    public static DateTime GetCreateDate(string userName)
    {
      var provider = ValidProvider();
      return provider.GetCreateDate(userName);
    }

    /// <summary>
    /// Get last date when password fails
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <returns>Last failure date or minimum date value if the user was not found</returns>
    public static DateTime GetLastPasswordFailureDate(string userName)
    {
      var provider = ValidProvider();
      return provider.GetLastPasswordFailureDate(userName);
    }

    /// <summary>
    /// Get date when password was changed
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <returns>Last password changed date or minimum date value if the user was not found</returns>
    public static DateTime GetPasswordChangedDate(string userName)
    {
      var provider = ValidProvider();
      return provider.GetPasswordChangedDate(userName);
    }

    /// <summary>
    /// Password failures since last success
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <returns>Number of failures since last success</returns>
    public static int GetPasswordFailuresSinceLastSuccess(string userName)
    {
      var provider = ValidProvider();
      return provider.GetPasswordFailuresSinceLastSuccess(userName);
    }

    /// <summary>
    /// Generates password reset token for confirmed user
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <param name="tokenExpirationInMinutesFromNow">Time that the token will be valid</param>
    /// <returns>Token generated or null if the user is not confirmed or does not has a token</returns>
    public static string GeneratePasswordResetToken(string userName, int tokenExpirationInMinutesFromNow = 1440)
    {
      var provider = ValidProvider();
      return provider.GeneratePasswordResetToken(userName, tokenExpirationInMinutesFromNow);
    }

    /// <summary>
    /// Get user id
    /// </summary>
    /// <param name="userName">User Name</param>
    /// <returns>User Id, -1 if user doesn't exists</returns>
    public static int GetUserId(string userName)
    {
      ValidProvider();
      var user = Membership.GetUser(userName);
      return user != null ? (int)user.ProviderUserKey : -1;
    }

    /// <summary>
    /// Get User Id from password reset token
    /// </summary>
    /// <param name="resetToken">Reset token</param>
    /// <returns>User Id, 0 if user dosn't exists</returns>
    public static int GetUserIdFromPasswordResetToken(string resetToken)
    {
      var provider = ValidProvider();
      return provider.GetUserIdFromPasswordResetToken(resetToken);
    }

    /// <summary>
    /// Initialize the simple membership provider with the values given
    /// </summary>
    /// <param name="connectionStringName">Connection string name defined in config file</param>
    /// <param name="userTableName">Table name defined to create new users</param>
    /// <param name="userIdColumn">Column name defined that will store the user id</param>
    /// <param name="userNameColumn">Column name defined that will store the user name</param>
    /// <param name="createTables">Create tables?</param>
    public static void InitializeDatabaseConnection(string connectionStringName, string userTableName, string userIdColumn, string userNameColumn, bool createTables, bool checkIfInitialized = false)
    {
      InitializeMembershipProvider(connectionStringName, null, null, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      InitializeRoleProvider(connectionStringName, null, null, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      Initialized = true;
    }

    public static void InitializeDatabaseConnection(string connectionString, string providerName, string userTableName, string userIdColumn, string userNameColumn, bool createTables, bool checkIfInitialized = false)
    {
      InitializeMembershipProvider(null, connectionString, providerName, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      InitializeRoleProvider(null, connectionString, providerName, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      Initialized = true;
    }

    public static bool IsAccountLockedOut(string userName, int allowedPasswordAttempts, int intervalInSeconds)
    {
      return IsAccountLockedOut(userName, allowedPasswordAttempts, TimeSpan.FromSeconds(intervalInSeconds));
    }

    public static bool IsAccountLockedOut(string userName, int allowedPasswordAttempts, TimeSpan interval)
    {
      var provider = ValidProvider();
      return (provider.GetUser(userName, false) != null && provider.GetPasswordFailuresSinceLastSuccess(userName) > allowedPasswordAttempts && provider.GetLastPasswordFailureDate(userName).Add(interval) > DateTime.UtcNow);
    }

    public static bool IsConfirmed(string userName)
    {
      var provider = ValidProvider();
      return provider.IsConfirmed(userName);
    }

    public static bool IsCurrentUser(string userName)
    {
      ValidProvider();
      return string.Equals(CurrentUserName, userName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUseLoggedOn(int userId)
    {
      ValidProvider();
      return CurrentUserId == userId;
    }

    public static bool Login(string userName, string password, bool createPersistentCookie = false)
    {
      var curentProvider = ValidProvider();
      bool loginSuccessful = false;
      if (curentProvider != null)
        loginSuccessful = curentProvider.ValidateUser(userName, password);
      else
        loginSuccessful = Membership.ValidateUser(userName, password);

      if (loginSuccessful)
      {
        FormsAuthentication.SetAuthCookie(userName, createPersistentCookie);
      }
      return loginSuccessful;
    }

    public static void Logout()
    {
      ValidProvider();
      FormsAuthentication.SignOut();
    }

    public static void RequireAuthenticatedUser()
    {
      ValidProvider();
      var user = CurrentContext.User;
      if (user == null || !user.Identity.IsAuthenticated)
      {
        CurrentContext.Response.SetStatus(HttpStatusCode.Unauthorized);
      }
    }

    public static void RequireRoles(params string[] roles)
    {
      ValidProvider();
      foreach (var role in roles)
      {
        if (!Roles.IsUserInRole(CurrentUserName, role))
        {
          CurrentContext.Response.SetStatus(HttpStatusCode.Unauthorized);
        }
      }
    }

    public static void RequiresUser(int userId)
    {
      ValidProvider();
      if (!IsUseLoggedOn(userId))
      {
        CurrentContext.Response.SetStatus(HttpStatusCode.Unauthorized);
      }
    }

    public static void RequiresUser(string userName)
    {
      ValidProvider();
      if (!string.Equals(CurrentUserName, userName, StringComparison.OrdinalIgnoreCase))
      {
        CurrentContext.Response.SetStatus(HttpStatusCode.Unauthorized);
      }
    }

    public static bool ResetPassword(string passwordResetToken, string newPassword)
    {
      var provider = ValidProvider();
      return provider.ResetPasswordWithToken(passwordResetToken, newPassword);
    }

    public static bool UserExists(string username)
    {
      var curentProvider = ValidProvider();
      if (curentProvider != null)
        return curentProvider.GetUser(username, false) != null;
      else
        return Membership.GetUser(username) != null;
    }
    #endregion

    #region Properties
    public static bool Initialized
    {
      get;
      private set;
    }

    public static int CurrentUserId
    {
      get { return GetUserId(CurrentUserName); }
    }

    public static string CurrentUserName
    {
      get { return CurrentContext.User.Identity.Name; }
    }

    public static bool HasUserId
    {
      get { return CurrentUserId != -1; }
    }

    public static bool IsAuthenticated
    {
      get { return CurrentContext.Request.IsAuthenticated; }
    }
    #endregion

    #region Private_Internal

    private static MySqlSimpleMembershipProvider CreateSimpleMembershipProvider(string name, MySQLMembershipProvider currentDefault)
    {
      MySqlSimpleMembershipProvider simpleProvider = new MySqlSimpleMembershipProvider(currentDefault);
      NameValueCollection config = new NameValueCollection();
      simpleProvider.Initialize(name, config);
      return simpleProvider;
    }

    private static MySqlSimpleRoleProvider CreateSimpleRoleProvider(string name, MySQLRoleProvider currentDefault)
    {
      MySqlSimpleRoleProvider simpleProvider = new MySqlSimpleRoleProvider(currentDefault);
      NameValueCollection config = new NameValueCollection();
      simpleProvider.Initialize(name, config);
      return simpleProvider;
    }

    private static MySqlSimpleMembershipProvider ValidProvider()
    {
      if (_provider.Initialized)
        return _provider;

      throw new Exception(Resources.SimpleMembershipNotInitialized);
    }

    private static void InitializeMembershipProvider(string connectionStringName, string connectionString, string providerName, string userTableName, string userIdColumn, string userNameColumn, bool createTables, bool checkIfInitialized = false)
    {
      if (checkIfInitialized)
      {
        if (_provider != null)
          return;
      }

      MySqlSimpleMembershipProvider simpleMembership = new MySqlSimpleMembershipProvider(Membership.Provider);
      if (_provider != null && _provider.Initialized)
      {
        throw new InvalidOperationException(Resources.SimpleMembershipAlreadyInitialized);
      }
      simpleMembership.ConnectionStringName = connectionStringName;
      simpleMembership.ConnectionString = connectionString;
      simpleMembership.ProviderName = providerName;
      simpleMembership.UserTableName = userTableName;
      simpleMembership.UserIdColumn = userIdColumn;
      simpleMembership.UserNameColumn = userNameColumn;
      if (createTables)
      {
        simpleMembership.CreateTables();
      }
      else
      {
        simpleMembership.ValidateUserTable();
      }
      simpleMembership.Initialized = true;
      _provider = simpleMembership;
    }

    private static void InitializeRoleProvider(string connectionStringName, string connectionString, string providerName, string userTableName, string userIdColumn, string userNameColumn, bool createTables, bool checkIfInitialized = false)
    {
      if (checkIfInitialized)
      {
        if (_roleprovider != null)
          return;
      }

      MySqlSimpleRoleProvider roleProvider = new MySqlSimpleRoleProvider(Roles.Provider);
      if (_roleprovider != null && _roleprovider.Initialized)
      {
        throw new InvalidOperationException(Resources.SimpleRoleAlreadyInitialized);
      }
      roleProvider.ConnectionStringName = connectionStringName;
      roleProvider.ConnectionString = connectionString;
      roleProvider.UserTableName = userTableName;
      roleProvider.UserIdColumn = userIdColumn;
      roleProvider.UserNameColumn = userNameColumn;
      if (createTables)
      {
        roleProvider.CreateTables();
      }
      roleProvider.Initialized = true;
      _roleprovider = roleProvider;
    }

    private static bool IsSimpleMembershipEnabled()
    {
      string config = ConfigurationManager.AppSettings[EnableSimpleMembershipKey];
      bool isEnabled = false;
      if (!string.IsNullOrEmpty(config))
      {
        bool.TryParse(config, out isEnabled);
      }
      return isEnabled;
    }

    internal static void PreAppStartInit()
    {
      if (IsSimpleMembershipEnabled())
      {
        MembershipProvider provider = Membership.Providers[MySqlMembershipProviderName];
        if (provider != null)
        {
          MySqlSimpleMembershipProvider mysqlProvider = CreateSimpleMembershipProvider(MySqlMembershipProviderName, (MySQLMembershipProvider)provider);
          Membership.Providers.Remove(MySqlMembershipProviderName);
          Membership.Providers.Add(mysqlProvider);
        }
        Roles.Enabled = true;
        RoleProvider roleProvider = Roles.Providers[MySqlRoleProviderName];
        if (roleProvider != null)
        {
          MySqlSimpleRoleProvider simpleRoleProv = CreateSimpleRoleProvider(MySqlRoleProviderName, (MySQLRoleProvider)roleProvider);
          Roles.Providers.Remove(MySqlRoleProviderName);
          Roles.Providers.Add(simpleRoleProv);
        }
      }
    }

    internal static HttpContextBase CurrentContext
    {
      get { return new HttpContextWrapper(HttpContext.Current); }
    }

    private static MySqlSimpleMembershipProvider _provider;
    private static MySqlSimpleRoleProvider _roleprovider;
    #endregion
  }
}
