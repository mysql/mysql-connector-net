// Copyright (c) 2004, 2014, Oracle and/or its affiliates. All rights reserved.
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

//  This code was contributed by Sean Wright (srwright@alcor.concordia.ca) on 2007-01-12
//  The copyright was assigned and transferred under the terms of
//  the MySQL Contributor License Agreement (CLA)

using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;
using System.Web.Security;
using MySql.Data.MySqlClient;
using MySql.Web.Profile;
using MySql.Web.Common;
using System.Text.RegularExpressions;
using MySql.Web.General;

namespace MySql.Web.Security
{
  /// <summary>
  /// Manages storage of membership information for an ASP.NET application in a MySQL database. 
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class is used by the <see cref="Membership"/> and <see cref="MembershipUser"/> classes
  /// to provide membership services for ASP.NET applications using a MySQL database.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code source="CodeExamples/MembershipCodeExample2.xml"/>
  /// </example>
  public sealed class MySQLMembershipProvider : MembershipProvider
  {
    private int newPasswordLength = 8;
    private string eventSource = "MySQLMembershipProvider";
    private string eventLog = "Application";
    private string exceptionMessage = "An exception occurred. Please check the Event Log.";
    private string connectionString;
    private int minRequiredPasswordLength;
    private bool writeExceptionsToEventLog;
    private bool enablePasswordReset;
    private bool enablePasswordRetrieval;
    private bool requiresQuestionAndAnswer;
    private bool requiresUniqueEmail;
    private int maxInvalidPasswordAttempts;
    private int passwordAttemptWindow;
    private MembershipPasswordFormat passwordFormat;
    private int minRequiredNonAlphanumericCharacters;
    private string passwordStrengthRegularExpression;
    private Application app;

    /// <summary>
    /// Initializes the MySQL membership provider with the property values specified in the 
    /// ASP.NET application's configuration file. This method is not intended to be used directly 
    /// from your code. 
    /// </summary>
    /// <param name="name">The name of the <see cref="MySQLMembershipProvider"/> instance to initialize.</param>
    /// <param name="config">A collection of the name/value pairs representing the 
    /// provider-specific attributes specified in the configuration for this provider.</param>
    /// <exception cref="T:System.ArgumentNullException">config is a null reference.</exception>
    /// <exception cref="T:System.InvalidOperationException">An attempt is made to call <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/> on a provider after the provider has already been initialized.</exception>
    /// <exception cref="T:System.Configuration.Provider.ProviderException"></exception>
    public override void Initialize(string name, NameValueCollection config)
    {
      if (config == null)
      {
        throw new ArgumentNullException("config");
      }
      if (name == null || name.Length == 0)
      {
        name = "MySQLMembershipProvider";
      }
      if (string.IsNullOrEmpty(config["description"]))
      {
        config.Remove("description");
        config.Add("description", "MySQL default application");
      }
      base.Initialize(name, config);

      string applicationName = GetConfigValue(config["applicationName"],
          HostingEnvironment.ApplicationVirtualPath);
      maxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
      passwordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
      minRequiredNonAlphanumericCharacters =
          Convert.ToInt32(GetConfigValue(config["minRequiredNonalphanumericCharacters"], "1"));
      minRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
      passwordStrengthRegularExpression =
          Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
      enablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "True"));
      enablePasswordRetrieval = Convert.ToBoolean(
          GetConfigValue(config["enablePasswordRetrieval"], "False"));
      requiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "False"));
      requiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "True"));
      writeExceptionsToEventLog = Convert.ToBoolean(GetConfigValue(config["writeExceptionsToEventLog"], "True"));
      string temp_format = config["passwordFormat"];

      if (temp_format == null)
        temp_format = "hashed";
      else
        temp_format = temp_format.ToLowerInvariant();

      if (temp_format == "hashed")
        passwordFormat = MembershipPasswordFormat.Hashed;
      else if (temp_format == "encrypted")
        passwordFormat = MembershipPasswordFormat.Encrypted;
      else if (temp_format == "clear")
        passwordFormat = MembershipPasswordFormat.Clear;
      else
        throw new ProviderException("Password format not supported.");

      // if the user is asking for the ability to retrieve hashed passwords, then let
      // them know we can't
      if (PasswordFormat == MembershipPasswordFormat.Hashed)
      {
        if (EnablePasswordRetrieval)
          throw new ProviderException(Properties.Resources.CannotRetrieveHashedPasswords);
      }

      connectionString = ConfigUtility.GetConnectionString(config);
      if (String.IsNullOrEmpty(connectionString)) return;

      // make sure we have the correct schema
      SchemaManager.CheckSchema(connectionString, config);

      app = new Application(applicationName, base.Description);
    }

    private static string GetConfigValue(string configValue, string defaultValue)
    {
      if (string.IsNullOrEmpty(configValue))
      {
        return defaultValue;
      }
      return configValue;
    }

    #region Properties

    /// <summary>
    /// The name of the application using the MySQL membership provider.
    /// </summary>
    /// <value>The name of the application using the MySQL membership provider.  The default is the 
    /// application virtual path.</value>
    /// <remarks>The ApplicationName is used by the MySqlMembershipProvider to separate 
    /// membership information for multiple applications.  Using different application names, 
    /// applications can use the same membership database.
    /// Likewise, multiple applications can make use of the same membership data by simply using
    /// the same application name.
    /// Caution should be taken with multiple applications as the ApplicationName property is not
    /// thread safe during writes.
    /// </remarks>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// The application name setting is being used.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override string ApplicationName
    {
      get { return app.Name; }
      set
      {
        lock (this)
        {
          if (value.ToLowerInvariant() == app.Name.ToLowerInvariant()) return;
          app = new Application(value, String.Empty);
        }
      }
    }

    /// <summary>
    /// Indicates whether the membership provider is configured to allow users to reset their passwords.
    /// </summary>
    /// <value>true if the membership provider supports password reset; otherwise, false. The default is true.</value>
    /// <remarks>Allows the user to replace their password with a new, randomly generated password.  
    /// This can be especially handy when using hashed passwords since hashed passwords cannot be
    /// retrieved.</remarks>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override bool EnablePasswordReset
    {
      get { return enablePasswordReset; }
    }

    /// <summary>
    /// Indicates whether the membership provider is configured to allow users to retrieve 
    /// their passwords.
    /// </summary>
    /// <value>true if the membership provider is configured to support password retrieval; 
    /// otherwise, false. The default is false.</value>
    /// <remarks>If the system is configured to use hashed passwords, then retrieval is not possible.  
    /// If the user attempts to initialize the provider with hashed passwords and enable password retrieval
    /// set to true then a <see cref="ProviderException"/> is thrown.</remarks>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override bool EnablePasswordRetrieval
    {
      get { return enablePasswordRetrieval; }
    }

    /// <summary>
    /// Gets a value indicating whether the membership provider is 
    /// configured to require the user to answer a password question 
    /// for password reset and retrieval.
    /// </summary>
    /// <value>true if a password answer is required for password 
    /// reset and retrieval; otherwise, false. The default is false.</value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override bool RequiresQuestionAndAnswer
    {
      get { return requiresQuestionAndAnswer; }
    }

    /// <summary>
    /// Gets a value indicating whether the membership provider is configured 
    /// to require a unique e-mail address for each user name.
    /// </summary>
    /// <value>true if the membership provider requires a unique e-mail address; 
    /// otherwise, false. The default is true.</value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override bool RequiresUniqueEmail
    {
      get { return requiresUniqueEmail; }
    }

    /// <summary>
    /// Gets the number of invalid password or password-answer attempts allowed 
    /// before the membership user is locked out.
    /// </summary>
    /// <value>The number of invalid password or password-answer attempts allowed 
    /// before the membership user is locked out.</value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override int MaxInvalidPasswordAttempts
    {
      get { return maxInvalidPasswordAttempts; }
    }

    /// <summary>
    /// Gets the number of minutes in which a maximum number of invalid password or 
    /// password-answer attempts are allowed before the membership user is locked out.
    /// </summary>
    /// <value>The number of minutes in which a maximum number of invalid password or 
    /// password-answer attempts are allowed before the membership user is locked out.</value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override int PasswordAttemptWindow
    {
      get { return passwordAttemptWindow; }
    }

    /// <summary>
    /// Gets a value indicating the format for storing passwords in the membership data store.
    /// </summary>
    /// <value>One of the <see cref="T:System.Web.Security.MembershipPasswordFormat"/> 
    /// values indicating the format for storing passwords in the data store.</value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override MembershipPasswordFormat PasswordFormat
    {
      get { return passwordFormat; }
    }

    /// <summary>
    /// Gets the minimum number of special characters that must be present in a valid password.
    /// </summary>
    /// <value>The minimum number of special characters that must be present 
    /// in a valid password.</value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override int MinRequiredNonAlphanumericCharacters
    {
      get { return minRequiredNonAlphanumericCharacters; }
    }

    /// <summary>
    /// Gets the minimum length required for a password.
    /// </summary>
    /// <value>The minimum length required for a password. </value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override int MinRequiredPasswordLength
    {
      get { return minRequiredPasswordLength; }
    }

    /// <summary>
    /// Gets the regular expression used to evaluate a password.
    /// </summary>
    /// <value>A regular expression used to evaluate a password.</value>
    /// <example>
    /// The following example shows the membership element being used in an applications web.config file.
    /// In this example, the regular expression specifies that the password must meet the following
    /// criteria:
    /// <ul>
    /// <list>Is at least seven characters.</list>
    /// <list>Contains at least one digit.</list>
    /// <list>Contains at least one special (non-alphanumeric) character.</list>
    /// </ul>
    /// <code source="CodeExamples/MembershipCodeExample1.xml"/>
    /// </example>
    public override string PasswordStrengthRegularExpression
    {
      get { return passwordStrengthRegularExpression; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether exceptions are written to the event log.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if exceptions should be written to the log; otherwise, <c>false</c>.
    /// </value>
    public bool WriteExceptionsToEventLog
    {
      get { return writeExceptionsToEventLog; }
      set { writeExceptionsToEventLog = value; }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Changes the password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="oldPassword">The old password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns><c>true</c> if the password was updated successfully; <c>false</c> if the supplied old password
    /// is invalid, the user is locked out, or the user does not exist in the database.</returns>
    public override bool ChangePassword(string username, string oldPassword, string newPassword)
    {
      // this will return false if the username doesn't exist
      if (!(ValidateUser(username, oldPassword)))
        return false;

      ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);
      OnValidatingPassword(args);
      if (args.Cancel)
      {
        if (!(args.FailureInformation == null))
          throw args.FailureInformation;
        else
          throw new ProviderException(Properties.Resources.ChangePasswordCanceled);
      }

      // validate the password according to current guidelines
      if (!ValidatePassword(newPassword, "newPassword", true))
        return false;

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          // retrieve the existing key and format for this user
          string passwordKey;
          MembershipPasswordFormat passwordFormat;
          int userId = GetUserId(connection, username);

          GetPasswordInfo(connection, userId, out passwordKey, out passwordFormat);

          MySqlCommand cmd = new MySqlCommand(
              @"UPDATE my_aspnet_membership
                        SET Password = @pass, LastPasswordChangedDate = @lastPasswordChangedDate 
                        WHERE userId=@userId", connection);
          cmd.Parameters.AddWithValue("@pass",
              EncodePassword(newPassword, passwordKey, passwordFormat));
          cmd.Parameters.AddWithValue("@lastPasswordChangedDate", DateTime.Now);
          cmd.Parameters.AddWithValue("@userId", userId);
          return cmd.ExecuteNonQuery() > 0;
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "ChangePassword");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Changes the password question and answer.
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="newPwdQuestion">The new password question.</param>
    /// <param name="newPwdAnswer">The new password answer.</param>
    /// <returns><c>true</c> if the update was successful; otherwise, <c>false</c>. A value of <c>false</c> is
    /// also returned if the password is incorrect, the user is locked out, or the user
    /// does not exist in the database.</returns>
    public override bool ChangePasswordQuestionAndAnswer(string username,
        string password, string newPwdQuestion, string newPwdAnswer)
    {
      // this handles the case where the username doesn't exist
      if (!(ValidateUser(username, password)))
        return false;

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          string passwordKey;
          MembershipPasswordFormat passwordFormat;
          int userId = GetUserId(connection, username);

          GetPasswordInfo(connection, userId, out passwordKey, out passwordFormat);


          MySqlCommand cmd = new MySqlCommand(
              @"UPDATE my_aspnet_membership 
                        SET PasswordQuestion = @passwordQuestion, PasswordAnswer = @passwordAnswer
                        WHERE userId=@userId", connection);
          cmd.Parameters.AddWithValue("@passwordQuestion", newPwdQuestion);
          cmd.Parameters.AddWithValue("@passwordAnswer",
              EncodePassword(newPwdAnswer, passwordKey, passwordFormat));
          cmd.Parameters.AddWithValue("@userId", userId);
          return cmd.ExecuteNonQuery() > 0;
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "ChangePasswordQuestionAndAnswer");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Adds a new membership user to the data source.
    /// </summary>
    /// <param name="username">The user name for the new user.</param>
    /// <param name="password">The password for the new user.</param>
    /// <param name="email">The e-mail address for the new user.</param>
    /// <param name="passwordQuestion">The password question for the new user.</param>
    /// <param name="passwordAnswer">The password answer for the new user</param>
    /// <param name="isApproved">Whether or not the new user is approved to be validated.</param>
    /// <param name="providerUserKey">The unique identifier from the membership data source for the user.</param>
    /// <param name="status">A <see cref="T:System.Web.Security.MembershipCreateStatus"/> enumeration value indicating whether the user was created successfully.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the information for the newly created user.
    /// </returns>
    public override MembershipUser CreateUser(string username, string password,
        string email, string passwordQuestion, string passwordAnswer,
        bool isApproved, object providerUserKey, out MembershipCreateStatus status)
    {
      //basis on MSDN documentation we should trim all the paramater values: http://msdn.microsoft.com/en-us/library/d8t4h2es%28v=vs.110%29.aspx
      TrimParametersValues(ref username, ref password, ref email, ref passwordQuestion, ref passwordAnswer);

      ValidatePasswordEventArgs Args = new ValidatePasswordEventArgs(username, password, true);
      OnValidatingPassword(Args);
      if (Args.Cancel)
      {
        status = MembershipCreateStatus.InvalidPassword;
        return null;
      }
      if (RequiresUniqueEmail && !String.IsNullOrEmpty(GetUserNameByEmail(email)))
      {
        status = MembershipCreateStatus.DuplicateEmail;
        return null;
      }

      ValidateQA(passwordQuestion, passwordAnswer);

      // now try to validate the password
      if (!ValidatePassword(password, "password", false))
      {
        status = MembershipCreateStatus.InvalidPassword;
        return null;
      }

      // now check to see if we already have a member by this name
      MembershipUser u = GetUser(username, false);
      if (u != null)
      {
        status = MembershipCreateStatus.DuplicateUserName;
        return null;
      }

      string passwordKey = GetPasswordKey();
      DateTime createDate = DateTime.Now;
      MySqlTransaction transaction = null;

      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        try
        {
          connection.Open();
          transaction = connection.BeginTransaction();

          // either create a new user or fetch the existing user id
          long userId = SchemaManager.CreateOrFetchUserId(connection, username,
              app.EnsureId(connection), true);

          MySqlCommand cmd = new MySqlCommand(
              @"INSERT INTO my_aspnet_membership 
                        VALUES(@userId, @email, @comment, @password, @passwordKey, 
                        @passwordFormat, @passwordQuestion, @passwordAnswer, 
                        @isApproved, @lastActivityDate, @lastLoginDate,
                        @lastPasswordChangedDate, @creationDate, 
                        @isLockedOut, @lastLockedOutDate, @failedPasswordAttemptCount,
                        @failedPasswordAttemptWindowStart, @failedPasswordAnswerAttemptCount, 
                        @failedPasswordAnswerAttemptWindowStart)",
              connection);
          cmd.Parameters.AddWithValue("@userId", userId);
          cmd.Parameters.AddWithValue("@email", email);
          cmd.Parameters.AddWithValue("@comment", "");
          cmd.Parameters.AddWithValue("@password",
              EncodePassword(password, passwordKey, PasswordFormat));
          cmd.Parameters.AddWithValue("@passwordKey", passwordKey);
          cmd.Parameters.AddWithValue("@passwordFormat", PasswordFormat);
          cmd.Parameters.AddWithValue("@passwordQuestion", passwordQuestion);
          cmd.Parameters.AddWithValue("@passwordAnswer",
              EncodePassword(passwordAnswer, passwordKey, PasswordFormat));
          cmd.Parameters.AddWithValue("@isApproved", isApproved);
          cmd.Parameters.AddWithValue("@lastActivityDate", createDate);
          cmd.Parameters.AddWithValue("@lastLoginDate", createDate);
          cmd.Parameters.AddWithValue("@lastPasswordChangedDate", createDate);
          cmd.Parameters.AddWithValue("@creationDate", createDate);
          cmd.Parameters.AddWithValue("@isLockedOut", false);
          cmd.Parameters.AddWithValue("@lastLockedOutDate", createDate);
          cmd.Parameters.AddWithValue("@failedPasswordAttemptCount", 0);
          cmd.Parameters.AddWithValue("@failedPasswordAttemptWindowStart", createDate);
          cmd.Parameters.AddWithValue("@failedPasswordAnswerAttemptCount", 0);
          cmd.Parameters.AddWithValue("@failedPasswordAnswerAttemptWindowStart", createDate);

          int recAdded = cmd.ExecuteNonQuery();
          if (recAdded > 0)
            status = MembershipCreateStatus.Success;
          else
            status = MembershipCreateStatus.UserRejected;
          transaction.Commit();
        }
        catch (MySqlException e)
        {
          if (WriteExceptionsToEventLog)
            WriteToEventLog(e, "CreateUser");
          status = MembershipCreateStatus.ProviderError;
          if (transaction != null)
            transaction.Rollback();
          return null;
        }
      }

      return GetUser(username, false);
    }

    /// <summary>
    /// Removes a user from the membership data source.
    /// </summary>
    /// <param name="username">The name of the user to delete.</param>
    /// <param name="deleteAllRelatedData"><c>true</c> to delete data related to the user from the database; <c>false</c> to leave data related to the user in the database.</param>
    /// <returns>
    /// <c>true</c> if the user was successfully deleted; otherwise, <c>false</c>.
    /// </returns>
    public override bool DeleteUser(string username, bool deleteAllRelatedData)
    {
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          conn.Open();

          int userId = GetUserId(conn, username);
          if (-1 == userId) return false;

          // if we are supposed to delete all related data, then delegate that to those providers
          if (deleteAllRelatedData)
          {
            MySQLRoleProvider.DeleteUserData(conn, userId);
            MySQLProfileProvider.DeleteUserData(conn, userId);
          }

          string sql = @"DELETE {0}m 
                        FROM my_aspnet_users u, my_aspnet_membership m
                        WHERE u.id=m.userId AND u.id=@userId";

          MySqlCommand cmd = new MySqlCommand(
              String.Format(sql, deleteAllRelatedData ? "u," : ""), conn);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(conn));
          cmd.Parameters.AddWithValue("@userId", userId);
          return cmd.ExecuteNonQuery() > 0;
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "DeleteUser");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Gets a collection of all the users in the data source in pages of data.
    /// </summary>
    /// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
    /// <param name="pageSize">The size of the page of results to return.</param>
    /// <param name="totalRecords">The total number of matched users.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
    /// </returns>
    public override MembershipUserCollection GetAllUsers(int pageIndex,
        int pageSize, out int totalRecords)
    {
      return GetUsers(null, null, pageIndex, pageSize, out totalRecords);
    }

    /// <summary>
    /// Gets the number of users currently accessing the application.
    /// </summary>
    /// <returns>
    /// The number of users currently accessing the application.
    /// </returns>
    public override int GetNumberOfUsersOnline()
    {
      TimeSpan onlineSpan = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
      DateTime compareTime = DateTime.Now.Subtract(onlineSpan);

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();
          MySqlCommand cmd = new MySqlCommand(
              @"SELECT COUNT(*) FROM my_aspnet_membership m JOIN my_aspnet_users u
                        ON m.userId=u.id WHERE m.LastActivityDate > @date AND u.applicationId=@appId",
              connection);
          cmd.Parameters.AddWithValue("@date", compareTime);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
          return Convert.ToInt32(cmd.ExecuteScalar());
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "GetNumberOfUsersOnline");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Gets the password for the specified user name from the data source.
    /// </summary>
    /// <param name="username">The user to retrieve the password for.</param>
    /// <param name="answer">The password answer for the user.</param>
    /// <returns>
    /// The password for the specified user name.
    /// </returns>
    public override string GetPassword(string username, string answer)
    {
      if (!EnablePasswordRetrieval)
        throw new ProviderException(Properties.Resources.PasswordRetrievalNotEnabled);

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          int userId = GetUserId(connection, username);
          if (-1 == userId)
            throw new ProviderException("Username not found.");

          string sql = @"SELECT Password, PasswordAnswer, PasswordKey, PasswordFormat, 
                    IsLockedOut FROM my_aspnet_membership WHERE userId=@userId";
          MySqlCommand cmd = new MySqlCommand(sql, connection);
          cmd.Parameters.AddWithValue("@userId", userId);

          using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
          {
            reader.Read();
            if (reader.GetBoolean("IsLockedOut"))
              throw new MembershipPasswordException(Properties.Resources.UserIsLockedOut);

            string password = reader.GetString("Password");
            string passwordAnswer = reader.GetValue(reader.GetOrdinal("PasswordAnswer")).ToString();
            string passwordKey = reader.GetString("PasswordKey");
            MembershipPasswordFormat format = (MembershipPasswordFormat)reader.GetInt32(3);
            reader.Close();

            if (RequiresQuestionAndAnswer &&
                !(CheckPassword(answer, passwordAnswer, passwordKey, format)))
            {
              UpdateFailureCount(userId, "PasswordAnswer", connection);
              throw new MembershipPasswordException(Properties.Resources.IncorrectPasswordAnswer);
            }
            if (PasswordFormat == MembershipPasswordFormat.Encrypted)
            {
              password = UnEncodePassword(password, format);
            }
            return password;
          }
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "GetPassword");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Gets information from the data source for a user. Provides an option to update the last-activity date/time stamp for the user.
    /// </summary>
    /// <param name="username">The name of the user to get information for.</param>
    /// <param name="userIsOnline"><c>true</c> to update the last-activity date/time stamp for the user; <c>false</c> to return user information without updating the last-activity date/time stamp for the user.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the specified user's information from the data source.
    /// </returns>
    public override MembershipUser GetUser(string username, bool userIsOnline)
    {
      try
      {
        int userId = -1;
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          userId = GetUserId(connection, username);
          if (-1 == userId) return null;
        }

        return GetUser(userId, userIsOnline);
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "GetUser(String, Boolean)");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Gets user information from the data source based on the unique identifier for the membership user. Provides an option to update the last-activity date/time stamp for the user.
    /// </summary>
    /// <param name="providerUserKey">The unique identifier for the membership user to get information for.</param>
    /// <param name="userIsOnline"><c>true</c> to update the last-activity date/time stamp for the user; <c>false</c> to return user information without updating the last-activity date/time stamp for the user.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUser"/> object populated with the specified user's information from the data source.
    /// </returns>
    public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
    {
      MySqlTransaction txn = null;

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          txn = connection.BeginTransaction();
          MySqlCommand cmd = new MySqlCommand("", connection);
          cmd.Parameters.AddWithValue("@userId", providerUserKey);

          if (userIsOnline)
          {
            cmd.CommandText =
                @"UPDATE my_aspnet_users SET lastActivityDate = @date WHERE id=@userId";
            cmd.Parameters.AddWithValue("@date", DateTime.Now);
            cmd.ExecuteNonQuery();

            cmd.CommandText = "UPDATE my_aspnet_membership SET LastActivityDate=@date WHERE userId=@userId";
            cmd.ExecuteNonQuery();
          }

          cmd.CommandText = @"SELECT m.*,u.name 
                    FROM my_aspnet_membership m JOIN my_aspnet_users u ON m.userId=u.id 
                    WHERE u.id=@userId";

          MembershipUser user;
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            if (!reader.Read()) return null;
            user = GetUserFromReader(reader);
          }
          txn.Commit();
          return user;
        }
      }
      catch (MySqlException e)
      {
        if (txn != null)
          txn.Rollback();
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "GetUser(Object, Boolean)");
        throw new ProviderException(exceptionMessage);
      }
    }

    /// <summary>
    /// Unlocks the user.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns><c>true</c> if the membership user was successfully unlocked;
    /// otherwise, <c>false</c>. A value of <c>false</c> is also returned if the user
    /// does not exist in the database. </returns>
    public override bool UnlockUser(string username)
    {
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          conn.Open();

          int userId = GetUserId(conn, username);
          if (-1 == userId) return false;

          string sql = @"UPDATE my_aspnet_membership  
                        SET IsLockedOut = false, LastLockedOutDate = @lastDate 
                        WHERE userId=@userId";

          MySqlCommand cmd = new MySqlCommand(sql, conn);
          cmd.Parameters.AddWithValue("@lastDate", DateTime.Now);
          cmd.Parameters.AddWithValue("@userId", userId);
          return cmd.ExecuteNonQuery() > 0;
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "UnlockUser");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Gets the user name associated with the specified e-mail address.
    /// </summary>
    /// <param name="email">The e-mail address to search for.</param>
    /// <returns>
    /// The user name associated with the specified e-mail address. If no match is found, return null.
    /// </returns>
    public override string GetUserNameByEmail(string email)
    {
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          conn.Open();

          string sql = @"SELECT u.name FROM my_aspnet_users u
                        JOIN my_aspnet_membership m ON m.userid=u.id
                        WHERE m.Email = @email AND u.applicationId=@appId";
          MySqlCommand cmd = new MySqlCommand(sql, conn);
          cmd.Parameters.AddWithValue("@email", email);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(conn));
          return (string)cmd.ExecuteScalar();
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "GetUserNameByEmail");
        throw new ProviderException(exceptionMessage);
      }
    }

    /// <summary>
    /// Resets a user's password to a new, automatically generated password.
    /// </summary>
    /// <param name="username">The user to reset the password for.</param>
    /// <param name="answer">The password answer for the specified user.</param>
    /// <returns>The new password for the specified user.</returns>
    public override string ResetPassword(string username, string answer)
    {
      if (!(EnablePasswordReset))
        throw new NotSupportedException(Properties.Resources.PasswordResetNotEnabled);

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          // fetch the userid first
          int userId = GetUserId(connection, username);
          if (-1 == userId)
            throw new ProviderException(Properties.Resources.UsernameNotFound);

          if (answer == null && RequiresQuestionAndAnswer)
          {
            UpdateFailureCount(userId, "PasswordAnswer", connection);
            throw new ProviderException(Properties.Resources.PasswordRequiredForReset);
          }

          string newPassword = Membership.GeneratePassword(newPasswordLength, MinRequiredNonAlphanumericCharacters);
          ValidatePasswordEventArgs Args = new ValidatePasswordEventArgs(username, newPassword, true);
          OnValidatingPassword(Args);
          if (Args.Cancel)
          {
            if (!(Args.FailureInformation == null))
              throw Args.FailureInformation;
            else
              throw new MembershipPasswordException(Properties.Resources.PasswordResetCanceledNotValid);
          }

          MySqlCommand cmd = new MySqlCommand(@"SELECT PasswordAnswer, 
                    PasswordKey, PasswordFormat, IsLockedOut 
                    FROM my_aspnet_membership WHERE userId=@userId", connection);
          cmd.Parameters.AddWithValue("@userId", userId);

          string passwordKey = String.Empty;
          MembershipPasswordFormat format;
          using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
          {
            reader.Read();
            if (reader.GetBoolean("IsLockedOut"))
              throw new MembershipPasswordException(Properties.Resources.UserIsLockedOut);

            object passwordAnswer = reader.GetValue(reader.GetOrdinal("PasswordAnswer"));
            passwordKey = reader.GetString("PasswordKey");
            format = (MembershipPasswordFormat)reader.GetByte("PasswordFormat");
            reader.Close();

            if (RequiresQuestionAndAnswer)
            {
              if (!CheckPassword(answer, (string)passwordAnswer, passwordKey, format))
              {
                UpdateFailureCount(userId, "PasswordAnswer", connection);
                throw new MembershipPasswordException(Properties.Resources.IncorrectPasswordAnswer);
              }
            }
          }

          cmd.CommandText = @"UPDATE my_aspnet_membership 
                        SET Password = @pass, LastPasswordChangedDate = @lastPassChange
                        WHERE userId=@userId";

          cmd.Parameters.AddWithValue("@pass",
              EncodePassword(newPassword, passwordKey, format));
          cmd.Parameters.AddWithValue("@lastPassChange", DateTime.Now);
          int rowsAffected = cmd.ExecuteNonQuery();
          if (rowsAffected != 1)
            throw new MembershipPasswordException(Properties.Resources.ErrorResettingPassword);
          return newPassword;
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "ResetPassword");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Updates information about a user in the data source.
    /// </summary>
    /// <param name="user">A <see cref="T:System.Web.Security.MembershipUser"/> object 
    /// that represents the user to update and the updated information for the user.</param>
    public override void UpdateUser(MembershipUser user)
    {
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          conn.Open();

          int userId = GetUserId(conn, user.UserName);
          if (-1 == userId)
            throw new ProviderException(Properties.Resources.UsernameNotFound);

          string sql = @"UPDATE my_aspnet_membership m, my_aspnet_users u 
                        SET m.Email=@email, m.Comment=@comment, m.IsApproved=@isApproved,
                        m.LastLoginDate=@lastLoginDate, u.lastActivityDate=@lastActivityDate,
                        m.LastActivityDate=@lastActivityDate
                        WHERE m.userId=u.id AND u.name LIKE @name AND u.applicationId=@appId";
          MySqlCommand cmd = new MySqlCommand(sql, conn);
          cmd.Parameters.AddWithValue("@Email", user.Email);
          cmd.Parameters.AddWithValue("@Comment", user.Comment);
          cmd.Parameters.AddWithValue("@isApproved", user.IsApproved);
          cmd.Parameters.AddWithValue("@lastLoginDate", user.LastLoginDate);
          cmd.Parameters.AddWithValue("@lastActivityDate", user.LastActivityDate);
          cmd.Parameters.AddWithValue("@name", user.UserName);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(conn));
          cmd.ExecuteNonQuery();
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "UpdateUser");
        throw new ProviderException(exceptionMessage);
      }
    }

    /// <summary>
    /// Verifies that the specified user name and password exist in the data source.
    /// </summary>
    /// <param name="username">The name of the user to validate.</param>
    /// <param name="password">The password for the specified user.</param>
    /// <returns>
    /// <c>true</c> if the specified username and password are valid; otherwise, <c>false</c>.
    /// </returns>
    public override bool ValidateUser(string username, string password)
    {
      bool isValid = false;
      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          // first get the user id.  If that is -1, then the user doesn't exist
          // so we just return false since we can't bump any counters
          int userId = GetUserId(connection, username);
          if (-1 == userId) return false;

          string sql = @"SELECT Password, PasswordKey, PasswordFormat, IsApproved,
                            Islockedout FROM my_aspnet_membership WHERE userId=@userId";
          MySqlCommand cmd = new MySqlCommand(sql, connection);
          cmd.Parameters.AddWithValue("@userId", userId);

          using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
          {
            if (!reader.HasRows) return false;
            reader.Read();
            if (reader.GetBoolean("IsLockedOut")) return false;

            string pwd = reader.GetString(0);
            string passwordKey = reader.GetString(1);
            MembershipPasswordFormat format = (MembershipPasswordFormat)
                reader.GetInt32(2);
            bool isApproved = reader.GetBoolean(3);
            reader.Close();

            if (!CheckPassword(password, pwd, passwordKey, format))
              UpdateFailureCount(userId, "Password", connection);
            else if (isApproved)
            {
              isValid = true;
              DateTime currentDate = DateTime.Now;
              MySqlCommand updateCmd = new MySqlCommand(
                  @"UPDATE my_aspnet_membership m, my_aspnet_users u 
                                SET m.LastLoginDate = @lastLoginDate, u.lastActivityDate = @date,
                                m.LastActivityDate=@date 
                                WHERE m.userId=@userid AND u.id=@userid", connection);
              updateCmd.Parameters.AddWithValue("@lastLoginDate", currentDate);
              updateCmd.Parameters.AddWithValue("@date", currentDate);
              updateCmd.Parameters.AddWithValue("@userid", userId);
              updateCmd.ExecuteNonQuery();
            }
          }
          return isValid;
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "ValidateUser");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    /// <summary>
    /// Gets a collection of membership users where the user name contains the specified user name to match.
    /// </summary>
    /// <param name="usernameToMatch">The user name to search for.</param>
    /// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
    /// <param name="pageSize">The size of the page of results to return.</param>
    /// <param name="totalRecords">The total number of matched users.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
    /// </returns>
    public override MembershipUserCollection FindUsersByName(string usernameToMatch,
                                     int pageIndex, int pageSize, out int totalRecords)
    {
      return GetUsers(usernameToMatch, null, pageIndex, pageSize, out totalRecords);
    }

    /// <summary>
    /// Gets a collection of membership users where the e-mail address contains the specified e-mail address to match.
    /// </summary>
    /// <param name="emailToMatch">The e-mail address to search for.</param>
    /// <param name="pageIndex">The index of the page of results to return. <paramref name="pageIndex"/> is zero-based.</param>
    /// <param name="pageSize">The size of the page of results to return.</param>
    /// <param name="totalRecords">The total number of matched users.</param>
    /// <returns>
    /// A <see cref="T:System.Web.Security.MembershipUserCollection"/> collection that contains a page of <paramref name="pageSize"/><see cref="T:System.Web.Security.MembershipUser"/> objects beginning at the page specified by <paramref name="pageIndex"/>.
    /// </returns>
    public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex,
                                                              int pageSize, out int totalRecords)
    {
      return GetUsers(null, emailToMatch, pageIndex, pageSize, out totalRecords);
    }

    #endregion

    #region Private Methods

    private int GetUserId(MySqlConnection connection, string username)
    {
      MySqlCommand cmd = new MySqlCommand(
          "SELECT id FROM my_aspnet_users WHERE name = @name AND applicationId=@appId", connection);
      cmd.Parameters.AddWithValue("@name", username);
      cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
      object id = cmd.ExecuteScalar();
      if (id == null) return -1;
      return (int)id;
    }

    private void WriteToEventLog(Exception e, string action)
    {
      using (EventLog log = new EventLog())
      {
        log.Source = eventSource;
        log.Log = eventLog;
        string message = "An exception occurred communicating with the data source." +
                         Environment.NewLine + Environment.NewLine;
        message += "Action: " + action + Environment.NewLine + Environment.NewLine;
        message += "Exception: " + e;
        log.WriteEntry(message);
      }
    }

    private MembershipUser GetUserFromReader(MySqlDataReader reader)
    {
      object providerUserKey = reader.GetInt32("userId");
      string username = reader.GetString("name");

      string email = null;
      if (!reader.IsDBNull(reader.GetOrdinal("Email")))
        email = reader.GetString("Email");

      string passwordQuestion = "";
      if (!(reader.GetValue(reader.GetOrdinal("PasswordQuestion")) == DBNull.Value))
        passwordQuestion = reader.GetString("PasswordQuestion");

      string comment = "";
      if (!(reader.GetValue(reader.GetOrdinal("Comment")) == DBNull.Value))
        comment = reader.GetString("Comment");

      bool isApproved = reader.GetBoolean("IsApproved");
      bool isLockedOut = reader.GetBoolean("IsLockedOut");
      DateTime creationDate = reader.GetDateTime("CreationDate");
      DateTime lastLoginDate = new DateTime();
      if (!(reader.GetValue(reader.GetOrdinal("LastLoginDate")) == DBNull.Value))
        lastLoginDate = reader.GetDateTime("LastLoginDate");

      DateTime lastActivityDate = reader.GetDateTime("LastActivityDate");
      DateTime lastPasswordChangedDate = reader.GetDateTime("LastPasswordChangedDate");
      DateTime lastLockedOutDate = new DateTime();
      if (!(reader.GetValue(reader.GetOrdinal("LastLockedoutDate")) == DBNull.Value))
        lastLockedOutDate = reader.GetDateTime("LastLockedoutDate");

      MembershipUser u =
          new MembershipUser(Name, username, providerUserKey, email, passwordQuestion, comment, isApproved,
                             isLockedOut, creationDate, lastLoginDate, lastActivityDate, lastPasswordChangedDate,
                             lastLockedOutDate);
      return u;
    }

    private string UnEncodePassword(string encodedPassword, MembershipPasswordFormat format)
    {
      string password = encodedPassword;
      if (format == MembershipPasswordFormat.Clear)
        return encodedPassword;
      else if (format == MembershipPasswordFormat.Encrypted)
        return Encoding.Unicode.GetString(DecryptPassword(
            Convert.FromBase64String(password)));
      else if (format == MembershipPasswordFormat.Hashed)
        throw new ProviderException(Properties.Resources.CannotUnencodeHashedPwd);
      else
        throw new ProviderException(Properties.Resources.UnsupportedPasswordFormat);
    }

    private string GetPasswordKey()
    {
      RNGCryptoServiceProvider cryptoProvider =
          new RNGCryptoServiceProvider();
      byte[] key = new byte[16];
      cryptoProvider.GetBytes(key);
      return Convert.ToBase64String(key);
    }

    /// <summary>
    /// this method is only necessary because early versions of Mono did not support
    /// the HashAlgorithmType property
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private string HashPasswordBytes(byte[] key, byte[] bytes)
    {
      HashAlgorithm hash = HashAlgorithm.Create(Membership.HashAlgorithmType);

      if (hash is KeyedHashAlgorithm)
      {
        KeyedHashAlgorithm keyedHash = hash as KeyedHashAlgorithm;
        keyedHash.Key = key;
      }
      return Convert.ToBase64String(hash.ComputeHash(bytes));
    }

    private string EncodePassword(string password, string passwordKey,
        MembershipPasswordFormat format)
    {
      if (password == null)
        return null;
      if (format == MembershipPasswordFormat.Clear)
        return password;

      byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
      byte[] keyBytes = Convert.FromBase64String(passwordKey);
      byte[] keyedBytes = new byte[passwordBytes.Length + keyBytes.Length];
      Array.Copy(keyBytes, keyedBytes, keyBytes.Length);
      Array.Copy(passwordBytes, 0, keyedBytes, keyBytes.Length, passwordBytes.Length);

      if (format == MembershipPasswordFormat.Encrypted)
      {
        byte[] encryptedBytes = EncryptPassword(passwordBytes);
        return Convert.ToBase64String(encryptedBytes);
      }
      else if (format == MembershipPasswordFormat.Hashed)
        return HashPasswordBytes(keyBytes, keyedBytes);
      else
        throw new ProviderException(Properties.Resources.UnsupportedPasswordFormat);
    }

    private void UpdateFailureCount(int userId, string failureType, MySqlConnection connection)
    {
      MySqlCommand cmd = new MySqlCommand(
          @"SELECT FailedPasswordAttemptCount, 
                FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, 
                FailedPasswordAnswerAttemptWindowStart FROM my_aspnet_membership 
                WHERE userId=@userId", connection);
      cmd.Parameters.AddWithValue("@userId", userId);

      DateTime windowStart = new DateTime();
      int failureCount = 0;
      try
      {
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
        {
          if (!reader.HasRows)
            throw new ProviderException(Properties.Resources.UnableToUpdateFailureCount);

          reader.Read();
          if (failureType == "Password")
          {
            failureCount = reader.GetInt32(0);
            windowStart = reader.GetDateTime(1);
          }
          if (failureType == "PasswordAnswer")
          {
            failureCount = reader.GetInt32(2);
            windowStart = reader.GetDateTime(3);
          }
        }

        DateTime windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);
        if (failureCount == 0 || DateTime.Now > windowEnd)
        {
          if (failureType == "Password")
          {
            cmd.CommandText =
                @"UPDATE my_aspnet_membership 
                            SET FailedPasswordAttemptCount = @count, 
                            FailedPasswordAttemptWindowStart = @windowStart 
                            WHERE userId=@userId";
          }
          if (failureType == "PasswordAnswer")
          {
            cmd.CommandText =
                @"UPDATE my_aspnet_membership 
                            SET FailedPasswordAnswerAttemptCount = @count, 
                            FailedPasswordAnswerAttemptWindowStart = @windowStart 
                            WHERE userId = @userId";
          }
          cmd.Parameters.Clear();
          cmd.Parameters.AddWithValue("@count", 1);
          cmd.Parameters.AddWithValue("@windowStart", DateTime.Now);
          cmd.Parameters.AddWithValue("@userId", userId);
          if (cmd.ExecuteNonQuery() < 0)
            throw new ProviderException(Properties.Resources.UnableToUpdateFailureCount);
        }
        else
        {
          failureCount += 1;
          if (failureCount >= MaxInvalidPasswordAttempts)
          {
            cmd.CommandText =
                @"UPDATE my_aspnet_membership SET IsLockedOut = @isLockedOut, 
                            LastLockedOutDate = @lastLockedOutDate WHERE userId=@userId";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@isLockedOut", true);
            cmd.Parameters.AddWithValue("@lastLockedOutDate", DateTime.Now);
            cmd.Parameters.AddWithValue("@userId", userId);
            if (cmd.ExecuteNonQuery() < 0)
              throw new ProviderException(Properties.Resources.UnableToLockOutUser);
          }
          else
          {
            if (failureType == "Password")
            {
              cmd.CommandText =
                  @"UPDATE my_aspnet_membership 
                                SET FailedPasswordAttemptCount = @count WHERE userId=@userId";
            }
            if (failureType == "PasswordAnswer")
            {
              cmd.CommandText =
                  @"UPDATE my_aspnet_membership 
                                SET FailedPasswordAnswerAttemptCount = @count 
                                WHERE userId=@userId";
            }
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@count", failureCount);
            cmd.Parameters.AddWithValue("@userId", userId);
            if (cmd.ExecuteNonQuery() < 0)
              throw new ProviderException("Unable to update failure count.");
          }
        }
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "UpdateFailureCount");
        throw new ProviderException(exceptionMessage, e);
      }
    }

    private bool CheckPassword(string password, string dbpassword,
        string passwordKey, MembershipPasswordFormat format)
    {
      password = EncodePassword(password, passwordKey, format);
      return password == dbpassword;
    }

    private void GetPasswordInfo(MySqlConnection connection, int userId,
        out string passwordKey, out MembershipPasswordFormat passwordFormat)
    {
      MySqlCommand cmd = new MySqlCommand(
          @"SELECT PasswordKey, PasswordFormat FROM my_aspnet_membership WHERE
                  userId=@userId", connection);
      cmd.Parameters.AddWithValue("@userId", userId);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        passwordKey = reader.GetString(reader.GetOrdinal("PasswordKey"));
        passwordFormat = (MembershipPasswordFormat)reader.GetByte(
            reader.GetOrdinal("PasswordFormat"));
      }
    }

    private MembershipUserCollection GetUsers(string username, string email,
        int pageIndex, int pageSize, out int totalRecords)
    {
      MembershipUserCollection users = new MembershipUserCollection();
      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();
          MySqlCommand cmd = new MySqlCommand();
          cmd.Connection = connection;

          string sql = @"SELECT SQL_CALC_FOUND_ROWS u.name,m.* FROM my_aspnet_users u
                        JOIN my_aspnet_membership m ON m.userId=u.id 
                        WHERE u.applicationId=@appId";

          if (username != null)
          {
            sql += " AND u.name LIKE @name";
            cmd.Parameters.AddWithValue("@name", username);
          }
          else if (email != null)
          {
            sql += " AND m.Email LIKE @email";
            cmd.Parameters.AddWithValue("@email", email);
          }
          sql += " ORDER BY u.id ASC LIMIT {0},{1}";
          cmd.CommandText = String.Format(sql, pageIndex * pageSize, pageSize);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
              users.Add(GetUserFromReader(reader));
          }
          cmd.CommandText = "SELECT FOUND_ROWS()";
          cmd.Parameters.Clear();
          totalRecords = Convert.ToInt32(cmd.ExecuteScalar());
        }
        return users;
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "GetUsers");
        throw new ProviderException(exceptionMessage);
      }
    }

    private void ValidateQA(string question, string answer)
    {
      if (RequiresQuestionAndAnswer && String.IsNullOrEmpty(question))
        throw new ArgumentException(Properties.Resources.PasswordQuestionInvalid);
      if (RequiresQuestionAndAnswer && String.IsNullOrEmpty(answer))
        throw new ArgumentException(Properties.Resources.PasswordAnswerInvalid);
    }

    private bool ValidatePassword(string password, string argumentName, bool throwExceptions)
    {
      string exceptionString = null;
      object correctValue = MinRequiredPasswordLength;

      if (password.Length < MinRequiredPasswordLength)
        exceptionString = Properties.Resources.PasswordNotLongEnough;
      else
      {
        int count = 0;
        foreach (char c in password)
          if (!char.IsLetterOrDigit(c))
            count++;
        if (count < MinRequiredNonAlphanumericCharacters)
          exceptionString = Properties.Resources.NotEnoughNonAlphaNumericInPwd;
        correctValue = MinRequiredNonAlphanumericCharacters;
      }

      if (exceptionString != null)
      {
        if (throwExceptions)
          throw new ArgumentException(
              string.Format(exceptionString, argumentName, correctValue),
              argumentName);
        else
          return false;
      }

      if (PasswordStrengthRegularExpression.Length > 0)
        if (!Regex.IsMatch(password, PasswordStrengthRegularExpression))
          return false;

      return true;
    }

    private void TrimParametersValues(ref string username, ref string password, ref string email, ref string passwordQuestion, ref string passwordAnswer)
    {
      username = string.IsNullOrEmpty(username) ? username : username.Trim();
      password = string.IsNullOrEmpty(password) ? password : password.Trim();
      email = string.IsNullOrEmpty(email) ? email : email.Trim();
      passwordQuestion = string.IsNullOrEmpty(passwordQuestion) ? passwordQuestion : passwordQuestion.Trim();
      passwordAnswer = string.IsNullOrEmpty(passwordAnswer) ? passwordAnswer : passwordAnswer.Trim();
    }

    #endregion
  }
}
