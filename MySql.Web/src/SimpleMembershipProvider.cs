// Copyright (c) 2014, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Web.General;
using MySql.Web.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Security;
using WebMatrix.WebData;
using System.Security.Cryptography;
using System.Web;
using System.Resources;
using MySql.Web.Common;
using System.Globalization;

namespace MySql.Web.Security
{
  /// <summary>
  /// Manages storage of simple membership information for an ASP.NET application in a MySQL database.
  /// </summary>
  public class MySqlSimpleMembershipProvider : ExtendedMembershipProvider
  {
    #region Private
    private readonly MembershipProvider _prevProvider;

    Application _app;
    bool _enablePwdReset;
    bool _enablePwdRetrival;
    int _maxPwdAttempts;
    int _minReqNonAlphanumericalChars;
    int _minReqPwdLength;
    int _pwdAttemptWindow;
    MembershipPasswordFormat _pwdFormat;
    string _pwdStrenghtRegex;
    bool _reqQuestionAnswer;
    bool _reqUniqueEmail;
    string _connString;
    string _userTableName;
    string _userIdColumn;
    string _userNameColumn;
    bool _autoGenerateTables;
    private readonly string _membershipTable = "webpages_membership";
    private readonly string _oauthMembershipTable = "webpages_oauthmembership";
    private readonly string _userInRolesTable = "webpages_usersinroles";
    private readonly string _oauthTokenTable = "webpages_oauthtoken";

    private static string GetConfigValue(string configVal, string defaultVal)
    {
      return !string.IsNullOrEmpty(configVal) ? configVal : defaultVal;
    }
    #endregion
    public MySqlSimpleMembershipProvider()
      : this(null)
    { }

    public MySqlSimpleMembershipProvider(MembershipProvider previousProvider)
    {
      _prevProvider = previousProvider;
      if (_prevProvider != null)
      {
        _prevProvider.ValidatingPassword += delegate(object sender, ValidatePasswordEventArgs args) { this.OnValidatingPassword(args); };
      }
    }

    public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
    {
      if (config == null)
      {
        NullArgumentException("config");
      }
      if (string.IsNullOrEmpty(name))
      {
        name = "MySqlExtendedMembershipProvider";
      }
      if (string.IsNullOrEmpty(config["description"]))
      {
        config.Remove("description");
        config.Add("description", string.Format("MySql Default {0} Description", name));
      }

      base.Initialize(name, config);

      var appName = GetConfigValue(config["applicationName"], HostingEnvironment.SiteName);
      _maxPwdAttempts = Int32.Parse(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"), CultureInfo.InvariantCulture);
      _pwdAttemptWindow = Int32.Parse(GetConfigValue(config["passwordAttemptWindow"], "10"), CultureInfo.InvariantCulture);
      _minReqNonAlphanumericalChars = Int32.Parse(GetConfigValue(config["minRequiredNonalphanumericCharacters"], "1"), CultureInfo.InvariantCulture);
      _minReqPwdLength = Int32.Parse(GetConfigValue(config["minRequiredPasswordLength"], "7"), CultureInfo.InvariantCulture);
      _pwdStrenghtRegex = GetConfigValue(config["passwordStrengthRegularExpression"], "");
      _enablePwdReset = bool.Parse(GetConfigValue(config["enablePasswordReset"], "True"));
      _enablePwdRetrival = bool.Parse(GetConfigValue(config["enablePasswordRetrieval"], "False"));
      _reqQuestionAnswer = bool.Parse(GetConfigValue(config["requiresQuestionAndAnswer"], "False"));
      _reqUniqueEmail = bool.Parse(GetConfigValue(config["requiresUniqueEmail"], "True"));

      var pwdFormat = !string.IsNullOrEmpty(config["passwordFormat"]) ? config["passwordFormat"].ToString().ToLowerInvariant() : "hashed";

      switch (pwdFormat)
      {
        case "hashed":
          _pwdFormat = MembershipPasswordFormat.Hashed;
          break;
        case "encrypted":
          _pwdFormat = MembershipPasswordFormat.Encrypted;
          break;
        case "clear":
          _pwdFormat = MembershipPasswordFormat.Clear;
          break;
        default:
          throw new ProviderException(Resources.PasswordFormatNotSupported);
      }

      if (_pwdFormat == MembershipPasswordFormat.Hashed)
      {
        if (_enablePwdRetrival)
          throw new ProviderException(Resources.CannotRetrieveHashedPasswords);
      }

      _app = new Application(appName, base.Description);
      _connString = ConfigUtility.GetConnectionString(config);
      if (string.IsNullOrEmpty(_connString)) return;

      UserTableName = GetConfigValue(config["userTableName"], "");
      UserIdColumn = GetConfigValue(config["userIdColumn"], "");
      UserNameColumn = GetConfigValue(config["userNameColumn"], "");
      _autoGenerateTables = bool.Parse(GetConfigValue(config["autoGenerateTables"], "True"));
      if (_autoGenerateTables)
        CreateTables();
      else
        ValidateUserTable();

      Initialized = true;
    }

    public override bool ChangePassword(string username, string oldPassword, string newPassword)
    {
      if (!Initialized)
      {
        _prevProvider.ChangePassword(username, oldPassword, newPassword);
      }

      if (string.IsNullOrEmpty(username))
        NullArgumentException("username");
      if (string.IsNullOrEmpty(oldPassword))
        NullArgumentException("oldPassword");
      if (string.IsNullOrEmpty(newPassword))
        NullArgumentException("newPassword");

      int userid = GetUserId(username);
      if (userid <= 0)
      {
        return false;
      }
      if (VerifyPassword(userid, oldPassword, GetHashedUserPassword(userid)))
      {
        return UpdatePassword(userid, newPassword) > 0;
      }

      return false;
    }

    public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
    {
      IsValidOperation(false);
      return _prevProvider.ChangePasswordQuestionAndAnswer(username, password, newPasswordQuestion, newPasswordAnswer);
    }

    public override bool ConfirmAccount(string accountConfirmationToken)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var token = dbConn.ExecuteQuerySingleRecord(string.Format("select userid, confirmationtoken from {0} where confirmationtoken=?", _membershipTable), accountConfirmationToken);
        if (token == null || (token != null && string.IsNullOrEmpty(token[1].ToString())))
        {
          return false;
        }
        return dbConn.ExecuteNonQuery(string.Format("update {0} set isconfirmed=1 where userid=?;", _membershipTable), (int)token[0]) > 0;
      }
    }

    public override bool ConfirmAccount(string userName, string accountConfirmationToken)
    {
      var userid = GetUserId(userName);
      if (userid <= 0)
        return false;

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var token = dbConn.ExecuteQuerySingleRecord(string.Format("select userid, confirmationtoken from {0} where confirmationtoken=? and userid=?", _membershipTable), accountConfirmationToken, userid);
        if (token == null || (token != null && string.IsNullOrEmpty(token[1].ToString())))
        {
          return false;
        }
        return dbConn.ExecuteNonQuery(string.Format("update {0} set isconfirmed=1 where userid=?;", _membershipTable), userid) > 0;
      }
    }

    public override string CreateAccount(string userName, string password, bool requireConfirmationToken)
    {
      IsValidOperation(true);
      if (string.IsNullOrEmpty(userName))
        NullArgumentException(userName);
      if (string.IsNullOrEmpty(password))
        NullArgumentException(password);
      var hashedPass = HashPassword(password);
      if (hashedPass.Length > 128)
        throw new ArgumentException(Resources.PasswordExceedsMaxLength, password);

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var userid = GetUserId(userName);
        if (userid <= 0)
        {
          throw new InvalidOperationException(string.Format(Resources.UserNotFound, UserTableName));
        }
        if (dbConn.ExecuteQuerySingleRecord(string.Format("select userid from {0} where userid=?;", _membershipTable), userid) != null)
        {
          throw new InvalidOperationException(string.Format(Resources.UserAlreadyExists, userName));
        }
        object token = DBNull.Value;
        if (requireConfirmationToken)
          token = GenerateToken();

        string insertQuery = string.Format("insert into {0} (userid, createdate, confirmationtoken, isconfirmed, password, passwordchangeddate, passwordsalt) values(?,now(),?,?,?,now(),?)", _membershipTable);
        if (dbConn.ExecuteNonQuery(insertQuery, userid, token, !requireConfirmationToken, hashedPass, string.Empty) <= 0)
        {
          throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);
        }
        return token == DBNull.Value ? null : token.ToString();
      }
    }

    public override void CreateOrUpdateOAuthAccount(string provider, string providerUserId, string userName)
    {
      IsValidOperation(true);
      if (string.IsNullOrEmpty(userName))
        NullArgumentException(userName);
      var userid = GetUserId(userName);
      if (userid <= 0)
      {
        throw new InvalidOperationException(string.Format(Resources.UserNotFound, UserTableName));
      }
      var oauthUserId = GetUserIdFromOAuth(provider, providerUserId);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        if (oauthUserId == 0)
        {
          if (dbConn.ExecuteNonQuery(string.Format("insert into {0} (provider, provideruserid, userid) values(?,?,?)", _oauthMembershipTable), provider, providerUserId, userid) <= 0)
          {
            throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);
          }
        }
        else
        {
          if (dbConn.ExecuteNonQuery(string.Format("update {0} set userid=? where upper(provider) = ? and upper(provideruserid)=?;", _oauthMembershipTable), userid, provider.ToUpper(), providerUserId.ToUpper()) <= 0)
          {
            throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);
          }
        }
      }
    }

    public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
    {
      IsValidOperation(false);
      return _prevProvider.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status);
    }

    public override string CreateUserAndAccount(string userName, string password, bool requireConfirmation, IDictionary<string, object> values)
    {
      IsValidOperation(true);
      CreateUserInUserTable(userName, values);
      return CreateAccount(userName, password, requireConfirmation);
    }

    public override bool DeleteAccount(string userName)
    {
      IsValidOperation(true);
      int userid = GetUserId(userName);
      if (userid < 0)
      {
        return false;
      }
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        return (dbConn.ExecuteNonQuery(string.Format("delete from {0} where userid=?;", _membershipTable), userid) > 0);
      }
    }

    public override void DeleteOAuthAccount(string provider, string providerUserId)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        if (dbConn.ExecuteNonQuery(string.Format("delete from {0} where upper(provider) = ? and upper(provideruserid)=?;", _oauthMembershipTable), provider.ToUpper(), providerUserId.ToUpper()) <= 0)
        {
          throw new Exception(string.Format(Resources.DeleteOAuthAccountFailed, provider, providerUserId));
        }
      }
    }

    public override void DeleteOAuthToken(string token)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        dbConn.ExecuteNonQuery(string.Format("delete from {0} where token=?", _oauthTokenTable), token);
      }
    }

    public override bool DeleteUser(string username, bool deleteAllRelatedData)
    {
      if (!Initialized)
        return _prevProvider.DeleteUser(username, deleteAllRelatedData);

      int userid = GetUserId(username);
      if (userid < 0)
        return false;

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        if (deleteAllRelatedData)
        {
          return dbConn.ExecuteInTransaction(
              new List<Tuple<string, object[]>>() 
                    {
                        new Tuple<string, object[]>(string.Format("delete from {0} where {1}=?;", UserTableName, UserIdColumn), new object [] { userid }),
                        new Tuple<string, object[]>(string.Format("delete from {0} where userid=?;", _oauthMembershipTable), new object [] { userid }),
                        new Tuple<string, object[]>(string.Format("delete from {0} where userid=?;", _membershipTable), new object [] { userid }),
                        new Tuple<string, object[]>(string.Format("delete from {0} where userid=?;", _userInRolesTable), new object [] { userid })
                    });
        }
        else
        {
          return (dbConn.ExecuteNonQuery(string.Format("delete from {0} where {1}=?;", UserTableName, UserIdColumn), userid) > 0);
        }
      }
    }

    public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
    {
      IsValidOperation(false);
      return _prevProvider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
    }

    public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
    {
      IsValidOperation(false);
      return _prevProvider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
    }

    public override string GeneratePasswordResetToken(string userName, int tokenExpirationInMinutesFromNow)
    {
      IsValidOperation(true);
      if (string.IsNullOrEmpty(userName))
        NullArgumentException("username");
      int userid = GetUserId(userName);
      if (userid <= 0)
        InvalidUserException(userName);

      if (UserConfirmed(userid))
      {
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
        {
          string token = dbConn.ExecuteScalar(string.Format("select PasswordVerificationToken from {0} where userid=? and PasswordVerificationTokenExpirationDate > ?;", _membershipTable), userid, DateTime.Now) as string;
          if (token != null)
          {
            token = GenerateToken();
            if (dbConn.ExecuteNonQuery(string.Format("update {0} set PasswordVerificationToken=?, PasswordVerificationTokenExpirationDate=? where userid=?;", _membershipTable), token, DateTime.Now.AddMinutes(tokenExpirationInMinutesFromNow), userid) <= 0)
            {
              throw new ProviderException(Resources.GeneratePassVerificationTokenFailed);
            }
          }
          return token;
        }
      }
      return null;
    }

    public override ICollection<OAuthAccountData> GetAccountsForUser(string userName)
    {
      IsValidOperation(true);
      int userid = GetUserId(userName);
      if (userid > 0)
      {
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
        {
          var records = dbConn.ExecuteQuery(string.Format("select provider, provideruserid from {0} where userid=?", _oauthMembershipTable), userid);
          if (records != null && records.Count() > 0)
          {
            var accounts = new List<OAuthAccountData>();
            records.ToList().ForEach(record => accounts.Add(new OAuthAccountData(record["provider"].ToString(), record["provideruserid"].ToString())));
            return accounts;
          }
        }
      }
      return new OAuthAccountData[0];
    }

    public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
    {
      IsValidOperation(false);
      return _prevProvider.GetAllUsers(pageIndex, pageSize, out totalRecords);
    }

    public override DateTime GetCreateDate(string userName)
    {
      int userid = GetUserId(userName);
      if (userid < 0)
        InvalidUserException(userName);

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var createDate = dbConn.ExecuteScalar(string.Format("select CreateDate from {0} where userid=?;", _membershipTable), userid);
        if (createDate != null && createDate != DBNull.Value)
          return (DateTime)createDate;

        return DateTime.MinValue;
      }
    }

    public override DateTime GetLastPasswordFailureDate(string userName)
    {
      int userid = GetUserId(userName);
      if (userid < 0)
        InvalidUserException(userName);

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var failureDate = dbConn.ExecuteScalar(string.Format("select LastPasswordFailureDate from {0} where userid=?;", _membershipTable), userid);
        if (failureDate != null && failureDate != DBNull.Value)
          return (DateTime)failureDate;

        return DateTime.MinValue;
      }
    }

    public override int GetNumberOfUsersOnline()
    {
      IsValidOperation(false);
      return _prevProvider.GetNumberOfUsersOnline();
    }

    public override string GetPassword(string username, string answer)
    {
      IsValidOperation(false);
      return _prevProvider.GetPassword(username, answer);
    }

    public override DateTime GetPasswordChangedDate(string userName)
    {
      int userid = GetUserId(userName);
      if (userid < 0)
        InvalidUserException(userName);

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var changedDate = dbConn.ExecuteScalar(string.Format("select PasswordChangedDate from {0} where userid=?;", _membershipTable), userid);
        if (changedDate != null)
          return (DateTime)changedDate;

        return DateTime.MinValue;
      }
    }

    public override int GetPasswordFailuresSinceLastSuccess(string userName)
    {
      int userid = GetUserId(userName);
      if (userid < 0)
        InvalidUserException(userName);

      return GetPasswordFailuresSinceLastSuccess(userid);
    }

    private int GetPasswordFailuresSinceLastSuccess(int userId)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var failures = dbConn.ExecuteScalar(string.Format("select PasswordFailuresSinceLastSuccess from {0} where userid=?;", _membershipTable), userId);
        if (failures != null)
          return (int)failures;

        return -1;
      }
    }

    public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
    {
      IsValidOperation(false);
      return _prevProvider.GetUser(providerUserKey, userIsOnline);
    }

    public override MembershipUser GetUser(string username, bool userIsOnline)
    {
      if (!Initialized)
        return _prevProvider.GetUser(username, userIsOnline);

      int userid = GetUserId(username);
      if (userid < 0)
      {
        return null;
      }
      return new MembershipUser(Membership.Provider.Name, username, userid, null, null, null, true, false, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
    }

    /// <summary>
    /// Gets the id of the specified user.
    /// </summary>
    /// <param name="userName">The name of the user.</param>
    /// <returns>An integer representing the id of the user.</returns>
    public int GetUserId(string userName)
    {
      return GetUserId(userName, GetConnectionString(), UserTableName, UserIdColumn, UserNameColumn);
    }

    internal static int GetUserId(string userName, string connectionString, string userTableName, string userIdColumn, string userNameColumn)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(connectionString))
      {
        var user = dbConn.ExecuteQuerySingleRecord(string.Format("select {0} from {1} where {2} = ?;", userIdColumn, userTableName, userNameColumn), userName);
        if (user != null)
          return (int)user[userIdColumn];

        return 0;
      }
    }

    public override int GetUserIdFromOAuth(string provider, string providerUserId)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var user = dbConn.ExecuteQuerySingleRecord(string.Format("select userid from {0} where upper(provider) = ? and upper(provideruserid)=?;", _oauthMembershipTable), provider.ToUpper(), providerUserId.ToUpper());
        if (user != null)
          return (int)user["userid"];

        return 0;
      }
    }

    public override int GetUserIdFromPasswordResetToken(string token)
    {
      IsValidOperation(true);
      return GetUserIdFromPasswordResetToken(token, false);
    }

    public override string GetOAuthTokenSecret(string token)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        return dbConn.ExecuteScalar(string.Format("select secret from {0} where token=?;", _oauthTokenTable), token) as string;
      }
    }

    private int GetUserIdFromPasswordResetToken(string token, bool checkExpirationDate = false)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var user = dbConn.ExecuteQuerySingleRecord(string.Format("select userid from {0} where PasswordVerificationToken = ? {1};", _membershipTable, (checkExpirationDate ? "and PasswordVerificationTokenExpirationDate > now()" : "")), token);
        if (user != null)
          return (int)user["userid"];

        return 0;
      }
    }

    public override string GetUserNameByEmail(string email)
    {
      IsValidOperation(false);
      return _prevProvider.GetUserNameByEmail(email);
    }

    public override string GetUserNameFromId(int userId)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        return dbConn.ExecuteScalar(string.Format("select {0} from {1} where {2}=?;", UserNameColumn, UserTableName, UserIdColumn), userId) as string;
      }
    }

    public override bool HasLocalAccount(int userId)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        return (dbConn.ExecuteQuery(string.Format("select userid from {0} where userid=?;", _membershipTable), userId).Count() > 0);
      }
    }

    public override bool IsConfirmed(string userName)
    {
      IsValidOperation(true);
      if (string.IsNullOrEmpty(userName))
        NullArgumentException("username");
      int userid = GetUserId(userName);
      if (userid <= 0)
        InvalidUserException(userName);
      return UserConfirmed(userid);
    }

    public override void ReplaceOAuthRequestTokenWithAccessToken(string requestToken, string accessToken, string accessTokenSecret)
    {
      IsValidOperation(true);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        dbConn.ExecuteNonQuery(string.Format("delete from {0} where token=?", _oauthTokenTable), requestToken);
      }
      StoreOAuthRequestToken(accessToken, accessTokenSecret);
    }

    public override string ResetPassword(string username, string answer)
    {
      IsValidOperation(false);
      return _prevProvider.ResetPassword(username, answer);
    }

    public override bool ResetPasswordWithToken(string token, string newPassword)
    {
      IsValidOperation(true);
      if (string.IsNullOrEmpty(token))
      {
        NullArgumentException("token");
      }
      if (string.IsNullOrEmpty(newPassword))
      {
        NullArgumentException("newPasword");
      }
      int userid = GetUserIdFromPasswordResetToken(token, true);
      if (userid <= 0)
      {
        return false;
      }
      bool passUpdated = UpdatePassword(userid, newPassword) > 0;
      if (passUpdated)
      {
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
        {
          if (dbConn.ExecuteNonQuery(string.Format("update {0} set PasswordVerificationToken=null, PasswordVerificationTokenExpirationDate=null where userid=?;", _membershipTable), userid) <= 0)
          {
            throw new ProviderException(string.Format(Resources.ClearPassTokenFailed, userid, _membershipTable));
          }
        }
      }
      return passUpdated;
    }

    public override void StoreOAuthRequestToken(string requestToken, string requestTokenSecret)
    {
      IsValidOperation(true);
      string secret = GetOAuthTokenSecret(requestToken);
      if (secret != null)
      {
        if (secret.Equals(requestTokenSecret, StringComparison.OrdinalIgnoreCase))
          return;
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
        {
          if (dbConn.ExecuteNonQuery(string.Format("update {0} set secret=? where token=?;", _oauthTokenTable), requestTokenSecret, requestToken) <= 0)
            throw new ProviderException(string.Format(Resources.UpdateTokenFailed, requestTokenSecret));
        }
      }
      else
      {
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
        {
          if (dbConn.ExecuteNonQuery(string.Format("insert into {0} (token, secret) values (?,?);", _oauthTokenTable), requestTokenSecret, requestToken) <= 0)
            throw new ProviderException(string.Format(Resources.SaveTokenFailed, requestTokenSecret));
        }
      }
    }

    public override bool UnlockUser(string userName)
    {
      IsValidOperation(false);
      return _prevProvider.UnlockUser(userName);
    }

    public override void UpdateUser(MembershipUser user)
    {
      IsValidOperation(false);
      _prevProvider.UpdateUser(user);
    }

    public override bool ValidateUser(string username, string password)
    {
      if (!Initialized)
        return _prevProvider.ValidateUser(username, password);
      if (string.IsNullOrEmpty(username))
        NullArgumentException("userName");
      if (string.IsNullOrEmpty(password))
        NullArgumentException("password");
      int userid = GetUserId(username);
      if (userid > 0)
      {
        if (!UserConfirmed(userid))
          return false;
        else
          return VerifyPassword(userid, password, GetHashedUserPassword(userid));
      }
      return false;
    }

    #region Properties
    public override string ApplicationName
    {
      get
      {
        if (Initialized)
          throw new NotSupportedException();
        else
          return _prevProvider.ApplicationName;
      }
      set
      {
        if (Initialized)
          throw new NotSupportedException();
        else
          _prevProvider.ApplicationName = value;
      }
    }

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString
    { get; set; }

    /// <summary>
    /// Gets or sets the name associated to the connection string when stored in the configuration manager.
    /// </summary>
    public string ConnectionStringName
    { get; set; }

    public override bool EnablePasswordReset
    {
      get
      {
        return Initialized ? false : _prevProvider.EnablePasswordReset;
      }
    }

    public override bool EnablePasswordRetrieval
    {
      get
      {
        return Initialized ? false : _prevProvider.EnablePasswordRetrieval;
      }
    }

    public override int MaxInvalidPasswordAttempts
    {
      get
      {
        return Initialized ? Int32.MaxValue : _prevProvider.MaxInvalidPasswordAttempts;
      }
    }

    public override int MinRequiredPasswordLength
    {
      get
      {
        return Initialized ? 7 : _prevProvider.MinRequiredPasswordLength;
      }
    }

    public override int MinRequiredNonAlphanumericCharacters
    {
      get
      {
        return Initialized ? 1 : _prevProvider.MinRequiredNonAlphanumericCharacters;
      }
    }

    public override int PasswordAttemptWindow
    {
      get
      {
        return Initialized ? Int32.MaxValue : _prevProvider.PasswordAttemptWindow;
      }
    }

    public override MembershipPasswordFormat PasswordFormat
    {
      get
      {
        return Initialized ? MembershipPasswordFormat.Hashed : _prevProvider.PasswordFormat;
      }
    }

    public override string PasswordStrengthRegularExpression
    {
      get
      {
        return Initialized ? string.Empty : _prevProvider.PasswordStrengthRegularExpression;
      }
    }

    /// <summary>
    /// Gets or sets the name of this provider.
    /// </summary>
    public string ProviderName
    { get; set; }

    public override bool RequiresQuestionAndAnswer
    {
      get
      {
        return Initialized ? false : _prevProvider.RequiresQuestionAndAnswer;
      }
    }

    public override bool RequiresUniqueEmail
    {
      get
      {
        return Initialized ? false : _prevProvider.RequiresUniqueEmail;
      }
    }

    /// <summary>
    /// Gets the name of the table storing user information.
    /// </summary>
    public string UserTableName
    {
      get
      {
        if (string.IsNullOrEmpty(_userTableName))
          throw new InvalidOperationException(Resources.UserTableNameNotInitilized);

        return _userTableName;
      }
      internal set
      {
        _userTableName =value;
      }
    }

    /// <summary>
    /// Gets the name of the column storing the user ids.
    /// </summary>
    public string UserIdColumn
    {
      get
      {
        if (string.IsNullOrEmpty(_userIdColumn))
          throw new InvalidOperationException(Resources.UserIdColumnNotInitialized);

        return _userIdColumn;
      }
      internal set
      {
        _userIdColumn = value;
      }
    }

    /// <summary>
    /// Gets the name of the column storing the user names.
    /// </summary>
    public string UserNameColumn
    {
      get
      {
        if (string.IsNullOrEmpty(_userNameColumn))
          throw new InvalidOperationException(Resources.UserNameColumnNotInitialized);

        return _userNameColumn;
      }
      internal set
      {
        _userNameColumn = value;
      }
    }

    #endregion

    #region Private_Internal

    internal void CreateTables()
    {
      string connString = GetConnectionString();
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(connString))
      {
        if (!VerifyIfTableExists(connString, UserTableName))
        {
          dbConn.ExecuteNonQuery(string.Format("create table {0} ({1} int not null primary key auto_increment, {2} varchar(250) not null unique);", UserTableName, UserIdColumn, UserNameColumn));
        }

        //create schema
        string schema = SchemaManager.GetSchema(11);
        dbConn.ExecuteNonQuery(schema);
      }
    }

    internal void ValidateUserTable()
    {
      if (!VerifyIfTableExists(GetConnectionString(), UserTableName))
      {
        throw new InvalidOperationException(string.Format(Resources.UserTableNotFound, UserTableName));
      }
    }

    internal bool Initialized
    {
      get;
      set;
    }

    private string GetConnectionString()
    {
      if (!string.IsNullOrEmpty(ConnectionString))
        return ConnectionString;
      else
      {
        ConnectionStringSettings connString = ConfigurationManager.ConnectionStrings[ConnectionStringName];
        if (connString != null)
          return connString.ConnectionString;
      }

      if (!string.IsNullOrEmpty(_connString))
        return _connString;
      throw new InvalidOperationException(Resources.NoConnString);
    }

    internal static bool VerifyIfTableExists(string connectionString, string tableName)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(connectionString))
      {
        var tables = dbConn.ExecuteQuery("show tables;");
        return tables.Where(record => record[0].ToString().Equals(tableName, StringComparison.OrdinalIgnoreCase)).Count() > 0;
      }
    }

    internal string GetHashedUserPassword(int userId)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        var userPassword = dbConn.ExecuteQuerySingleRecord(string.Format("select password from {0} where userid=?;", _membershipTable), userId);
        if (userPassword != null)
          return userPassword[0].ToString();
        return null;
      }
    }

    internal string HashPassword(string password)
    {
      if (string.IsNullOrEmpty(password))
      {
        throw new ArgumentException(Resources.InvalidArgument, password);
      }

      Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, 16, 1000);
      byte[] salt = rfc2898.Salt;
      byte[] passBytes = rfc2898.GetBytes(32);
      byte[] result = new byte[48];
      Buffer.BlockCopy(salt, 0, result, 0, 16);
      Buffer.BlockCopy(passBytes, 0, result, 16, 32);
      return Convert.ToBase64String(result);
    }

    internal bool VerifyPassword(int userid, string password, string hashedPassword)
    {
      if (string.IsNullOrEmpty(password))
      {
        throw new ArgumentException(Resources.InvalidArgument, password);
      }
      if (string.IsNullOrEmpty(hashedPassword))
      {
        throw new ArgumentException(Resources.InvalidArgument, hashedPassword);
      }
      byte[] hashed = Convert.FromBase64String(hashedPassword);
      if (hashed.Length != 48)
      {
        return false;
      }
      byte[] salt = new byte[16];
      byte[] passBytes = new byte[32];
      Buffer.BlockCopy(hashed, 0, salt, 0, 16);
      Buffer.BlockCopy(hashed, 16, passBytes, 0, 32);

      Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, salt, 1000);
      bool validation = CompareBuffer(rfc2898.GetBytes(32), passBytes);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        if (validation)
        {
          dbConn.ExecuteNonQuery(string.Format("update {0} set PasswordFailuresSinceLastSuccess=0 where userid=?;", _membershipTable), userid);
        }
        else
        {
          int failures = GetPasswordFailuresSinceLastSuccess(userid);
          dbConn.ExecuteNonQuery(string.Format("update {0} set PasswordFailuresSinceLastSuccess=?, LastPasswordFailureDate=now() where userid=?;", _membershipTable), (failures == -1 ? 1 : failures + 1), userid);
        }
      }
      return validation;
    }

    internal bool CompareBuffer(byte[] source, byte[] target)
    {
      if (source == null || target == null || (source.Length != target.Length))
        return false;
      for (int ctr = 0; ctr < target.Length; ctr++)
      {
        if (target[ctr] != source[ctr])
          return false;
      }

      return true;
    }

    private int UpdatePassword(int userId, string newPassword)
    {
      string hashedPass = HashPassword(newPassword);
      if (hashedPass.Length > 128)
        throw new ArgumentException(Resources.PasswordExceedsMaxLength, newPassword);
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        return dbConn.ExecuteNonQuery(string.Format("update {0} set password=?, PasswordChangedDate=now(), PasswordSalt='' where userid=?;", _membershipTable), hashedPass, userId);
      }
    }

    internal void IsValidOperation(bool currentProvider)
    {
      switch (currentProvider)
      {
        case true:
          if (!Initialized)
            ProviderException();
          break;
        case false:
          if (Initialized)
            PreviousProviderException();
          break;
      }
    }

    private void ProviderException()
    {
      throw new Exception(Resources.SimpleMembershipNotInitialized);
    }

    private void PreviousProviderException()
    {
      throw new NotSupportedException(Resources.PreviousProviderException);
    }

    private void InvalidUserException(string userName)
    {
      throw new Exception(string.Format(Resources.InvalidUser, userName, UserTableName));
    }

    internal static void NullArgumentException(string parameterName)
    {
      throw new ArgumentException(Resources.InvalidArgument, parameterName);
    }

    private string GenerateToken()
    {
      RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
      byte[] data = new byte[16];
      provider.GetBytes(data);
      return HttpServerUtility.UrlTokenEncode(data);
    }

    private void CreateUserInUserTable(string userName, IDictionary<string, object> values)
    {
      IsValidOperation(true);
      var userid = GetUserId(userName);
      if (userid > 0)
      {
        throw new MembershipCreateUserException(MembershipCreateStatus.DuplicateUserName);
      }
      StringBuilder columns = new StringBuilder();
      columns.Append(UserNameColumn);
      StringBuilder args = new StringBuilder();
      args.Append("?");
      var argsValues = new List<object>();
      argsValues.Add(userName);

      if (values != null)
      {
        foreach (var value in values)
        {
          if (string.Equals(UserNameColumn, value.Key, StringComparison.OrdinalIgnoreCase))
            continue;
          columns.Append(string.Format(",{0}", value.Key));
          args.Append(",?");
          argsValues.Add(value.Value != null ? value.Value : DBNull.Value);
        }
      }
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        string query = string.Format("insert into {0} ({1}) values({2})", UserTableName, columns.ToString(), args.ToString());
        if (dbConn.ExecuteNonQuery(query, argsValues.ToArray()) < 1)
        {
          throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);
        }
      }
    }

    private bool UserConfirmed(int userId)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(GetConnectionString()))
      {
        return ((long)dbConn.ExecuteScalar(string.Format("select count(*) from {0} where userid=? and isconfirmed=1;", _membershipTable), userId)) > 0;
      }
    }
    #endregion
  }
}
