// Copyright © 2014, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.Routing;
using System.Web.WebPages;
using System.Collections.Specialized;
using System.Net;
using MySql.Web.Properties;

namespace MySql.Web.Security
{
  /// <summary>
  /// Provides security features for web projects implementing a MySql database.
  /// </summary>
  public static class MySqlWebSecurity
  {
	/// <summary>
    /// Name of the key required to enable simple membership.
    /// </summary>
    public static readonly string EnableSimpleMembershipKey = "enableSimpleMembership";
    private static readonly string MySqlMembershipProviderName = "MySqlMembershipProvider";
    private static readonly string MySqlRoleProviderName = "MySQLRoleProvider";

    #region Public
    /// <summary>
    /// Changes the password for the user provided.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="oldPassword">The current pasword.</param>
    /// <param name="newPassword">The new Password.</param>
    /// <returns></returns>
    public static bool ChangePassword(string userName, string oldPassword, string newPassword)
    {
      ValidProvider();
      var user = Membership.GetUser(userName, true);
      return user.ChangePassword(oldPassword, newPassword);
    }

    /// <summary>
    /// Confirms user by confirmation token.
    /// </summary>
    /// <param name="confirmationToken">The confirmation token.</param>
    /// <returns><c>true</c> if the user was confirmed; otherwise, <c>false</c>.</returns>
    public static bool ConfirmAccount(string confirmationToken)
    {
      var provider = ValidProvider();
      return provider.ConfirmAccount(confirmationToken);
    }

    /// <summary>
    /// Confirms user by confirmation token and user name.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="confirmationToken">The confirmation token.</param>
    /// <returns><c>true</c> if the user was confirmed; otherwise, <c>false</c>.</returns>
    public static bool ConfirmAccount(string userName, string confirmationToken)
    {
      var provider = ValidProvider();
      return provider.ConfirmAccount(userName, confirmationToken);
    }

    /// <summary>
    /// Creates a user account.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The user password.</param>
    /// <param name="requireConfirmationToken">Flag to indicate if a confirmation token is required.</param>
    /// <returns>A confirmation token if required.</returns>
    public static string CreateAccount(string userName, string password, bool requireConfirmationToken = false)
    {
      var provider = ValidProvider();
      return provider.CreateAccount(userName, password, requireConfirmationToken);
    }

    /// <summary>
    /// Creates user and account.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The user password.</param>
    /// <param name="additionalUserAttributes">Additional data for user table.</param>
    /// <param name="requireConfirmationToken">Flag to indicate if a confirmation token is required.</param>
    /// <returns>A confirmation token if required.</returns>
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
    /// Gets the date when the specified user was created.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <returns>Date created or minimum date value if the user was not found.</returns>
    public static DateTime GetCreateDate(string userName)
    {
      var provider = ValidProvider();
      return provider.GetCreateDate(userName);
    }

    /// <summary>
    /// Gets the last date when password fails.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <returns>Last failure date or minimum date value if the user was not found.</returns>
    public static DateTime GetLastPasswordFailureDate(string userName)
    {
      var provider = ValidProvider();
      return provider.GetLastPasswordFailureDate(userName);
    }

    /// <summary>
    /// Gets the date when password was changed.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <returns>Last password changed date or minimum date value if the user was not found.</returns>
    public static DateTime GetPasswordChangedDate(string userName)
    {
      var provider = ValidProvider();
      return provider.GetPasswordChangedDate(userName);
    }

    /// <summary>
    /// Gets the password failures since last success.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <returns>The number of failures since last success.</returns>
    public static int GetPasswordFailuresSinceLastSuccess(string userName)
    {
      var provider = ValidProvider();
      return provider.GetPasswordFailuresSinceLastSuccess(userName);
    }

    /// <summary>
    /// Generates password reset token for a confirmed user.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="tokenExpirationInMinutesFromNow">The time that the token will be valid.</param>
    /// <returns>A generated token or <c>null</c> if the user is not confirmed or does not have a token.</returns>
    public static string GeneratePasswordResetToken(string userName, int tokenExpirationInMinutesFromNow = 1440)
    {
      var provider = ValidProvider();
      return provider.GeneratePasswordResetToken(userName, tokenExpirationInMinutesFromNow);
    }

    /// <summary>
    /// Gets the user id.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <returns>The user id. -1 if the user doesn't exists</returns>
    public static int GetUserId(string userName)
    {
      ValidProvider();
      var user = Membership.GetUser(userName);
      return user != null ? (int)user.ProviderUserKey : -1;
    }

    /// <summary>
    /// Gets the user id from the password reset token.
    /// </summary>
    /// <param name="resetToken">The reset token.</param>
    /// <returns>The user id. 0 if the user doesn't exists.</returns>
    public static int GetUserIdFromPasswordResetToken(string resetToken)
    {
      var provider = ValidProvider();
      return provider.GetUserIdFromPasswordResetToken(resetToken);
    }

    /// <summary>
    /// Initializes the simple membership provider with the values given.
    /// </summary>
    /// <param name="connectionStringName">The connection string name defined in the config file.</param>
    /// <param name="userTableName">The table name defined to create new users.</param>
    /// <param name="userIdColumn">The column name defined to store the user ids.</param>
    /// <param name="userNameColumn">The column name defined to store the user name.</param>
    /// <param name="createTables">Flag indicating if the tables should be created.</param>
    /// <param name="checkIfInitialized">Flag indicating to check if the database has been initialized.</param>
    public static void InitializeDatabaseConnection(string connectionStringName, string userTableName, string userIdColumn, string userNameColumn, bool createTables, bool checkIfInitialized = false)
    {
      InitializeMembershipProvider(connectionStringName, null, null, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      InitializeRoleProvider(connectionStringName, null, null, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      Initialized = true;
    }

    /// <summary>
    /// Initializes the simple membership provider with the values given.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="userTableName">The table name defined to create new users.</param>
    /// <param name="userIdColumn">The column name defined to store the user ids.</param>
    /// <param name="userNameColumn">The column name defined to store the user name.</param>
    /// <param name="createTables">Flag indicating if the tables should be created.</param>
    /// <param name="checkIfInitialized">Flag indicating to check if the database has been initialized.</param>
    public static void InitializeDatabaseConnection(string connectionString, string providerName, string userTableName, string userIdColumn, string userNameColumn, bool createTables, bool checkIfInitialized = false)
    {
      InitializeMembershipProvider(null, connectionString, providerName, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      InitializeRoleProvider(null, connectionString, providerName, userTableName, userIdColumn, userNameColumn, createTables, checkIfInitialized);
      Initialized = true;
    }

    /// <summary>
    /// Determines if the account is locked out.
    /// </summary>
    /// <param name="userName">The name of the user.</param>
    /// <param name="allowedPasswordAttempts">The number of allowed password attempts.</param>
    /// <returns><c>true</c> if the account is locked; otherwise, <c>false</c>.</returns>
    public static bool IsAccountLockedOut(string userName, int allowedPasswordAttempts, int intervalInSeconds)
    {
      return IsAccountLockedOut(userName, allowedPasswordAttempts, TimeSpan.FromSeconds(intervalInSeconds));
    }

    /// <summary>
    /// Determines if the account is locked out.
    /// </summary>
    /// <param name="userName">The name of the user.</param>
    /// <param name="allowedPasswordAttempts">The number of allowed password attempts.</param>
    /// <returns><c>true</c> if the account is locked; otherwise, <c>false</c>.</returns>
    public static bool IsAccountLockedOut(string userName, int allowedPasswordAttempts, TimeSpan interval)
    {
      var provider = ValidProvider();
      return (provider.GetUser(userName, false) != null && provider.GetPasswordFailuresSinceLastSuccess(userName) > allowedPasswordAttempts && provider.GetLastPasswordFailureDate(userName).Add(interval) > DateTime.UtcNow);
    }

    /// <summary>
    /// Determines if the user has been confirmed.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <returns><c>true</c> if the user is confirmed; otherwise <c>false</c>.</returns>
    public static bool IsConfirmed(string userName)
    {
      var provider = ValidProvider();
      return provider.IsConfirmed(userName);
    }

    /// <summary>
    /// Determines if the <see cref="CurrentUserName"/> is the same as the provided user name.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <returns><c>true</c> if the user matches the <see cref="CurrentUserName"/>; otherwise, <c>false</c>.</returns>
    public static bool IsCurrentUser(string userName)
    {
      ValidProvider();
      return string.Equals(CurrentUserName, userName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the <see cref="CurrentUserId"/> matches the provided user id.
    /// </summary>
    /// <param name="userId">The user id to match.</param>
    /// <returns><c>true</c> if the id matches the <see cref="CurrentUserId"/>; otherwise, <c>false</c>.</returns>
    public static bool IsUseLoggedOn(int userId)
    {
      ValidProvider();
      return CurrentUserId == userId;
    }

    /// <summary>
    /// Performs a login for the specified user.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The user password.</param>
    /// <param name="createPersistentCookie">Flag to indicate if a persistent cookie should be created.</param>
    /// <returns><c>true</c> if the login was successful; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Performs a logout for the current item.
    /// </summary>
    public static void Logout()
    {
      ValidProvider();
      FormsAuthentication.SignOut();
    }

    /// <summary>
    /// Evalutes if the user is authenticated.
    /// </summary>
    public static void RequireAuthenticatedUser()
    {
      ValidProvider();
      var user = CurrentContext.User;
      if (user == null || !user.Identity.IsAuthenticated)
      {
        CurrentContext.Response.SetStatus(HttpStatusCode.Unauthorized);
      }
    }

    /// <summary>
    /// Evaluates if the user belongs to the specified roles.
    /// </summary>
    /// <param name="roles"></param>
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

    /// <summary>
    /// Evaluates if the user is logged on.
    /// </summary>
    /// <param name="userId"></param>
    public static void RequiresUser(int userId)
    {
      ValidProvider();
      if (!IsUseLoggedOn(userId))
      {
        CurrentContext.Response.SetStatus(HttpStatusCode.Unauthorized);
      }
    }

    /// <summary>
    /// Evaluates if the provided user name matches the <see cref="CurrentUserName"/>.
    /// </summary>
    /// <param name="userName"></param>
    public static void RequiresUser(string userName)
    {
      ValidProvider();
      if (!string.Equals(CurrentUserName, userName, StringComparison.OrdinalIgnoreCase))
      {
        CurrentContext.Response.SetStatus(HttpStatusCode.Unauthorized);
      }
    }

    /// <summary>
    /// Resets the password identified by the provided password reset token.
    /// </summary>
    /// <param name="passwordResetToken">The password reset token.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns><c>true</c> if the password reset was successful; otherwise, <c>false</c>.</returns>
    public static bool ResetPassword(string passwordResetToken, string newPassword)
    {
      var provider = ValidProvider();
      return provider.ResetPasswordWithToken(passwordResetToken, newPassword);
    }

    /// <summary>
    /// Determines if the user exists.
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <returns><c>true</c> if the user exists; otherwise, <c>false</c>.</returns>
    public static bool UserExists(string username)
    {
      var curentProvider = ValidProvider();
      if (curentProvider != null)
        return curentProvider.GetUser(username, false) != null;
      else
        return Membership.GetUser(username) != null;
    }
    #endregion

    /// <summary>
    /// Gets the initialized status.
    /// </summary>
    #region Properties
    public static bool Initialized
    {
      get;
      private set;
    }

    /// <summary>
    /// Gets the current user id.
    /// </summary>
    public static int CurrentUserId
    {
      get { return GetUserId(CurrentUserName); }
    }

    /// <summary>
    /// Gets the current user name.
    /// </summary>
    public static string CurrentUserName
    {
      get { return CurrentContext.User.Identity.Name; }
    }

    /// <summary>
    /// Gets a flag indicating if there is an associated user id.
    /// </summary>
    public static bool HasUserId
    {
      get { return CurrentUserId != -1; }
    }

    /// <summary>
    /// Gets a flag indicating if the user is authenticated.
    /// </summary>
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
