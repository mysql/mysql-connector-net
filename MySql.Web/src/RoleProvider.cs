// Copyright (c) 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
using System.Web.Hosting;
using System.Web.Security;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using MySql.Web.Common;
using MySql.Web.General;

namespace MySql.Web.Security
{
  /// <summary>
  /// Manages storage of role membership information for an ASP.NET application in a MySQL database. 
  /// </summary>
  public sealed class MySQLRoleProvider : RoleProvider
  {
    private string eventSource = "MySQLRoleProvider";
    private string eventLog = "Application";
    private string exceptionMessage = "An exception occurred. Please check the Event Log.";
    private string connectionString;
    private bool pWriteExceptionsToEventLog = false;
    private Application app;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="name">The friendly name of the provider.</param>
    /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
    /// <exception cref="T:System.ArgumentNullException">The name of the provider is null.</exception>
    /// <exception cref="T:System.ArgumentException">The name of the provider has a length of zero.</exception>
    /// <exception cref="T:System.InvalidOperationException">An attempt is made to call <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/> on a provider after the provider has already been initialized.</exception>
    public override void Initialize(string name, NameValueCollection config)
    {
      if (config == null)
      {
        throw new ArgumentNullException("config");
      }
      if (name == null || name.Length == 0)
      {
        name = "MySQLRoleProvider";
      }
      if (string.IsNullOrEmpty(config["description"]))
      {
        config.Remove("description");
        config.Add("description", "MySQL Role provider");
      }
      base.Initialize(name, config);

      string applicationName = HostingEnvironment.ApplicationVirtualPath;
      if (!String.IsNullOrEmpty(config["applicationName"]))
        applicationName = config["applicationName"];

      if (!(config["writeExceptionsToEventLog"] == null))
      {
        if (config["writeExceptionsToEventLog"].ToUpper() == "TRUE")
        {
          pWriteExceptionsToEventLog = true;
        }
      }

      connectionString = ConfigUtility.GetConnectionString(config);
      if (String.IsNullOrEmpty(connectionString)) return;

      // make sure our schema is up to date
      SchemaManager.CheckSchema(connectionString, config);

      app = new Application(applicationName, Description);
    }

    #region Properties

    /// <summary>
    /// Gets or sets the name of the application to store and retrieve role information for.
    /// </summary>
    /// <value>The name of the application to store and retrieve role information for.</value>
    /// <example>
    /// <code source="CodeExamples\RoleCodeExample1.xml"/>
    /// </example>
    public override string ApplicationName
    {
      get { return app.Name; }
      set { app.Name = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether exceptions should be written to the event log.
    /// </summary>
    /// <value>
    /// <c>true</c> if exceptions should be written to the event log; otherwise, <c>false</c>.
    /// </value>
    /// <example>
    /// <code source="CodeExamples\RoleCodeExample1.xml"/>
    /// </example>
    public bool WriteExceptionsToEventLog
    {
      get { return pWriteExceptionsToEventLog; }
      set { pWriteExceptionsToEventLog = value; }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds the users to the specified roles.
    /// </summary>
    /// <param name="usernames">The user names.</param>
    /// <param name="rolenames">The role names.</param>
    public override void AddUsersToRoles(string[] usernames, string[] rolenames)
    {
      if (rolenames == null || rolenames.Length == 0) return;
      if (usernames == null || usernames.Length == 0) return;

      foreach (string rolename in rolenames)
      {
        if (String.IsNullOrEmpty(rolename))
          throw new ArgumentException(Properties.Resources.IllegalRoleName, "rolenames");
        if (!RoleExists(rolename))
          throw new ProviderException(Properties.Resources.RoleNameNotFound);
      }

      foreach (string username in usernames)
      {
        if (String.IsNullOrEmpty(username))
          throw new ArgumentException(Properties.Resources.IllegalUserName, "usernames");
        if (username.IndexOf(',') != -1)
          throw new ArgumentException(Properties.Resources.InvalidCharactersInUserName);

        foreach (string rolename in rolenames)
        {
          if (IsUserInRole(username, rolename))
            throw new ProviderException(Properties.Resources.UserIsAlreadyInRole);
        }
      }

      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        MySqlTransaction txn = null;
        try
        {
          connection.Open();
          txn = connection.BeginTransaction();
          MySqlCommand cmd = new MySqlCommand(
              "INSERT INTO my_aspnet_usersinroles VALUES(@userId, @roleId)", connection);
          cmd.Parameters.Add("@userId", MySqlDbType.Int32);
          cmd.Parameters.Add("@roleId", MySqlDbType.Int32);
          foreach (string username in usernames)
          {
            // either create a new user or fetch the existing user id
            long userId = SchemaManager.CreateOrFetchUserId(connection,
                username, app.FetchId(connection), true);
            foreach (string rolename in rolenames)
            {
              int roleId = GetRoleId(connection, rolename);
              cmd.Parameters[0].Value = userId;
              cmd.Parameters[1].Value = roleId;
              cmd.ExecuteNonQuery();
            }
          }
          txn.Commit();
        }
        catch (Exception ex)
        {
          if (txn != null)
            txn.Rollback();
          if (WriteExceptionsToEventLog)
            WriteToEventLog(ex, "AddUsersToRoles");
          throw;
        }
      }
    }

    /// <summary>
    /// Creates the specified role.
    /// </summary>
    /// <param name="rolename">The role name.</param>
    public override void CreateRole(string rolename)
    {
      if (rolename.IndexOf(',') != -1)
        throw new ArgumentException(Properties.Resources.InvalidCharactersInUserName);
      if (RoleExists(rolename))
        throw new ProviderException(Properties.Resources.RoleNameAlreadyExists);

      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        try
        {
          connection.Open();

          MySqlCommand cmd = new MySqlCommand(
                  @"INSERT INTO my_aspnet_roles Values(NULL, @appId, @name)", connection);
          cmd.Parameters.AddWithValue("@appId", app.EnsureId(connection));
          cmd.Parameters.AddWithValue("@name", rolename);
          cmd.ExecuteNonQuery();
        }
        catch (MySqlException e)
        {
          if (WriteExceptionsToEventLog)
            WriteToEventLog(e, "CreateRole");
          throw;
        }
      }
    }

    /// <summary>
    /// Deletes the specified role.
    /// </summary>
    /// <param name="rolename">The role name.</param>
    /// <param name="throwOnPopulatedRole">If set to <c>true</c> a <see cref="ProviderException"/> will be raised if
    /// there are users with the specified role.</param>
    /// <returns><c>true</c> if the role was successfully deleted; otherwise, <c>false</c>. </returns>
    /// <exception cref="ProviderException">The specified role doesn't exist or <paramref name="throwOnPopulatedRole"/>
    /// is set to <c>true</c> and there are users with the specified role.</exception>
    public override bool DeleteRole(string rolename, bool throwOnPopulatedRole)
    {
      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        MySqlTransaction txn = null;
        try
        {
          if (!(RoleExists(rolename)))
            throw new ProviderException(Properties.Resources.RoleNameNotFound);
          if (throwOnPopulatedRole && GetUsersInRole(rolename).Length > 0)
            throw new ProviderException(Properties.Resources.CannotDeleteAPopulatedRole);

          connection.Open();
          txn = connection.BeginTransaction();

          // first delete all the user/role mappings with that roleid
          MySqlCommand cmd = new MySqlCommand(
              @"DELETE uir FROM my_aspnet_usersinroles uir JOIN 
                        my_aspnet_roles r ON uir.roleId=r.id 
                        WHERE r.name LIKE @rolename AND r.applicationId=@appId", connection);
          cmd.Parameters.AddWithValue("@rolename", rolename);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
          cmd.ExecuteNonQuery();

          // now delete the role itself
          cmd.CommandText = @"DELETE FROM my_aspnet_roles WHERE name=@rolename 
                        AND applicationId=@appId";
          cmd.ExecuteNonQuery();
          txn.Commit();
        }
        catch (Exception ex)
        {
          if (txn != null)
            txn.Rollback();
          if (WriteExceptionsToEventLog)
            WriteToEventLog(ex, "DeleteRole");
          throw;
        }
      }
      return true;
    }

    /// <summary>
    /// Gets a list of all the roles for the configured applicationName.
    /// </summary>
    /// <returns>
    /// A string array containing the names of all the roles stored in the data source for the configured applicationName.
    /// </returns>
    public override string[] GetAllRoles()
    {
      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        return GetRolesByUserName(connection, null);
      }
    }

    /// <summary>
    /// Gets a list of the roles that a specified user is in for the configured applicationName.
    /// </summary>
    /// <param name="username">The user to return a list of roles for.</param>
    /// <returns>
    /// A string array containing the names of all the roles that the specified user is in for the configured applicationName.
    /// </returns>
    public override string[] GetRolesForUser(string username)
    {
      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        return GetRolesByUserName(connection, username);
      }
    }

    /// <summary>
    /// Gets the users with the specified role.
    /// </summary>
    /// <param name="rolename">The role name.</param>
    /// <returns>A string array containing the names of all the users
    /// who are members of the specified role. </returns>
    public override string[] GetUsersInRole(string rolename)
    {
      List<string> users = new List<string>();

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();
          int roleId = GetRoleId(connection, rolename);

          string sql = @"SELECT u.name FROM my_aspnet_users u JOIN
                    my_aspnet_usersinroles uir ON uir.userId=u.id AND uir.roleId=@roleId
                    WHERE u.applicationId=@appId";
          MySqlCommand cmd = new MySqlCommand(sql, connection);
          cmd.Parameters.AddWithValue("@roleId", roleId);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
              users.Add(reader.GetString(0));
          }
        }
        return users.ToArray();
      }
      catch (Exception ex)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(ex, "GetUsersInRole");
        throw;
      }
    }

    /// <summary>
    /// Determines whether [is user in role] [the specified username].
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="rolename">The rolename.</param>
    /// <returns>
    /// 	<c>true</c> if [is user in role] [the specified username]; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsUserInRole(string username, string rolename)
    {
      try
      {
        // this will refresh the app id if necessary
        if (!RoleExists(rolename)) return false;

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          string sql = @"SELECT COUNT(*) FROM my_aspnet_usersinroles uir 
                        JOIN my_aspnet_users u ON uir.userId=u.id
                        JOIN my_aspnet_roles r ON uir.roleId=r.id
                        WHERE u.applicationId=@appId AND 
                        u.name = @userName AND r.name = @roleName";
          MySqlCommand cmd = new MySqlCommand(sql, connection);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
          cmd.Parameters.AddWithValue("@userName", username);
          cmd.Parameters.AddWithValue("@roleName", rolename);
          int count = Convert.ToInt32(cmd.ExecuteScalar());
          return count > 0;
        }
      }
      catch (Exception ex)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(ex, "IsUserInRole");
        throw;
      }
    }

    /// <summary>
    /// Removes the users from the specified roles.
    /// </summary>
    /// <param name="usernames">The user names.</param>
    /// <param name="rolenames">The role names.</param>
    public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames)
    {
      if (rolenames == null || rolenames.Length == 0) return;
      if (usernames == null || usernames.Length == 0) return;

      foreach (string rolename in rolenames)
      {
        if (!(RoleExists(rolename)))
          throw new ProviderException(Properties.Resources.RoleNameNotFound);
      }

      foreach (string username in usernames)
      {
        foreach (string rolename in rolenames)
        {
          if (!(IsUserInRole(username, rolename)))
            throw new ProviderException(Properties.Resources.UserNotInRole);
        }
      }

      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        MySqlTransaction txn = null;
        try
        {
          connection.Open();
          txn = connection.BeginTransaction();

          string sql = @"DELETE uir FROM my_aspnet_usersinroles uir
                            JOIN my_aspnet_users u ON uir.userId=u.id 
                            JOIN my_aspnet_roles r ON uir.roleId=r.id
                            WHERE u.name LIKE @username AND r.name LIKE @rolename 
                            AND u.applicationId=@appId AND r.applicationId=@appId";

          MySqlCommand cmd = new MySqlCommand(sql, connection);
          cmd.Parameters.Add("@username", MySqlDbType.VarChar, 255);
          cmd.Parameters.Add("@rolename", MySqlDbType.VarChar, 255);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));

          foreach (string username in usernames)
          {
            foreach (string rolename in rolenames)
            {
              cmd.Parameters[0].Value = username;
              cmd.Parameters[1].Value = rolename;
              cmd.ExecuteNonQuery();
            }
          }
          txn.Commit();
        }
        catch (MySqlException e)
        {
          if (txn != null)
            txn.Rollback();
          if (WriteExceptionsToEventLog)
            WriteToEventLog(e, "RemoveUsersFromRoles");
          throw;
        }
      }
    }

    /// <summary>
    /// Determines whether the role exists.
    /// </summary>
    /// <param name="rolename">The rolename.</param>
    /// <returns>true if the role name already exists in the database; otherwise, false. </returns>
    public override bool RoleExists(string rolename)
    {
      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();
          MySqlCommand cmd = new MySqlCommand(
              @"SELECT COUNT(*) FROM my_aspnet_roles WHERE applicationId=@appId 
                        AND name LIKE @name", connection);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
          cmd.Parameters.AddWithValue("@name", rolename);
          int count = Convert.ToInt32(cmd.ExecuteScalar());
          return count != 0;
        }
      }
      catch (Exception ex)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(ex, "RoleExists");
        throw;
      }
    }

    /// <summary>
    /// Finds the users with the specified role.
    /// </summary>
    /// <param name="rolename">The role name.</param>
    /// <param name="usernameToMatch">The user name to match.</param>
    /// <returns>A string array containing the names of all the users where the
    /// user name matches usernameToMatch and the user is a member of the specified role. </returns>
    public override string[] FindUsersInRole(string rolename, string usernameToMatch)
    {
      if (!RoleExists(rolename))
        throw new ProviderException(Properties.Resources.RoleNameNotFound);

      List<string> users = new List<string>();

      try
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();

          string sql = @"SELECT u.name FROM my_aspnet_usersinroles uir
                        JOIN my_aspnet_users u ON uir.userId=u.id
                        JOIN my_aspnet_roles r ON uir.roleId=r.id
                        WHERE r.name LIKE @rolename AND
                        u.name LIKE @username AND
                        u.applicationId=@appId";

          MySqlCommand cmd = new MySqlCommand(sql, connection);
          cmd.Parameters.AddWithValue("@username", usernameToMatch);
          cmd.Parameters.AddWithValue("@rolename", rolename);
          cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
              users.Add(reader.GetString(0));
          }
        }
        return users.ToArray();
      }
      catch (MySqlException e)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(e, "FindUsersInRole");
        throw;
      }
    }

    #endregion

    internal static void DeleteUserData(MySqlConnection connection, int userId)
    {
      MySqlCommand cmd = new MySqlCommand(
          "DELETE FROM my_aspnet_usersinroles WHERE userId=@userId", connection);
      cmd.Parameters.AddWithValue("@userId", userId);
      cmd.ExecuteNonQuery();
    }

    #region Private Methods

    private string[] GetRolesByUserName(MySqlConnection connection, string username)
    {
      List<string> roleList = new List<string>();

      try
      {
        string sql = "SELECT r.name FROM my_aspnet_roles r ";
        if (username != null)
          sql += "JOIN my_aspnet_usersinroles uir ON uir.roleId=r.id AND uir.userId=" +
              GetUserId(connection, username);
        sql += " WHERE r.applicationId=@appId";
        MySqlCommand cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
            roleList.Add(reader.GetString(0));
        }
        return (string[])roleList.ToArray();
      }
      catch (Exception ex)
      {
        if (WriteExceptionsToEventLog)
          WriteToEventLog(ex, "GetRolesByUserName");
        throw;
      }
    }

    private int GetUserId(MySqlConnection connection, string username)
    {
      MySqlCommand cmd = new MySqlCommand(
          "SELECT id FROM my_aspnet_users WHERE name=@name AND applicationId=@appId",
          connection);
      cmd.Parameters.AddWithValue("@name", username);
      cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
      object id = cmd.ExecuteScalar();
      return Convert.ToInt32(id);
    }

    private int GetRoleId(MySqlConnection connection, string rolename)
    {
      MySqlCommand cmd = new MySqlCommand(
          "SELECT id FROM my_aspnet_roles WHERE name=@name AND applicationId=@appId",
          connection);
      cmd.Parameters.AddWithValue("@name", rolename);
      cmd.Parameters.AddWithValue("@appId", app.FetchId(connection));
      return (int)cmd.ExecuteScalar();
    }

    private void WriteToEventLog(Exception e, string action)
    {
      using (EventLog log = new EventLog())
      {
        log.Source = eventSource;
        log.Log = eventLog;
        string message = exceptionMessage + Environment.NewLine + Environment.NewLine;
        message += "Action: " + action + Environment.NewLine + Environment.NewLine;
        message += "Exception: " + e;
        log.WriteEntry(message);
      }
    }

    #endregion

  }
}