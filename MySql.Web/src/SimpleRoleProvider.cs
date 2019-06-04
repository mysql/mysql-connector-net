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

using MySql.Web.Common;
using MySql.Web.General;
using MySql.Web.Properties;
using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Web.Hosting;
using System.Web.Security;
using WebMatrix.WebData;

namespace MySql.Web.Security
{
  /// <summary>
  /// Manages storage of simple role membership information for an ASP.NET application in a MySQL database.
  /// </summary>
  public class MySqlSimpleRoleProvider : SimpleRoleProvider
  {
    #region Private
    private readonly RoleProvider _prevProvider;

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
    string _userTableName;
    string _userIdColumn;
    string _userNameColumn;
    bool _autoGenerateTables;
    private readonly string _rolesTable = "webpages_Roles";
    private readonly string _userInRolesTable = "webpages_UsersInRoles";
    private static string GetConfigValue(string configVal, string defaultVal)
    {
      return !string.IsNullOrEmpty(configVal) ? configVal : defaultVal;
    }

    private void NotInitializedException()
    {
      throw new Exception(Resources.SimpleMembershipNotInitialized);
    }

    #endregion
    public MySqlSimpleRoleProvider()
      : this(null)
    { }

    public MySqlSimpleRoleProvider(RoleProvider previousProvider)
    {
      _prevProvider = previousProvider;
    }

    public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
    {
      if (config == null)
      {
        throw new ArgumentNullException("config");
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
      ConnectionString = ConfigUtility.GetConnectionString(config);
      if (string.IsNullOrEmpty(ConnectionString)) return;

      UserTableName = GetConfigValue(config["userTableName"], "");
      UserIdColumn = GetConfigValue(config["userIdColumn"], "");
      UserNameColumn = GetConfigValue(config["userNameColumn"], "");
      _autoGenerateTables = bool.Parse(GetConfigValue(config["autoGenerateTables"], "True"));
      if (_autoGenerateTables)
        CreateTables();

      Initialized = true;
    }

    public override void AddUsersToRoles(string[] usernames, string[] roleNames)
    {
      if (!Initialized)
      {
        _prevProvider.AddUsersToRoles(usernames, roleNames);
      }
      else
      {
        if (usernames.Where(username => string.IsNullOrEmpty(username)).Count() > 0 || usernames.Where(username => string.IsNullOrEmpty(username)).Count() > 0)
          throw new ArgumentException(Resources.InvalidArrayValue);

        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
        {
          foreach (var userid in GetUsersId(usernames))
          {
            foreach (var roleid in GetRolesId(roleNames))
            {
              if (userid > 0 && roleid > 0)
              {
                dbConn.ExecuteNonQuery(string.Format("insert into {0} (userid, roleid) values(?,?);", _userInRolesTable), userid, roleid);
              }
            }
          }
        }
      }
    }

    public override void CreateRole(string roleName)
    {
      if (!Initialized)
      {
        _prevProvider.CreateRole(roleName);
      }
      else
      {
        if (string.IsNullOrEmpty(roleName))
          MySqlSimpleMembershipProvider.NullArgumentException("roleName");

        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
        {
          int roleid = GetRoleId(roleName);
          if (roleid != 0)
          {
            throw new InvalidOperationException(string.Format(Resources.RoleAlreadyExists, roleName));
          }
          if (dbConn.ExecuteNonQuery(string.Format("insert into {0} (rolename) values(?);", _rolesTable), roleName) <= 0)
          {
            throw new ProviderException(string.Format(Resources.CreateRoleFailed, roleName));
          }
        }
      }
    }

    public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
    {
      if (!Initialized)
      {
        return _prevProvider.DeleteRole(roleName, throwOnPopulatedRole);
      }
      if (string.IsNullOrEmpty(roleName))
        MySqlSimpleMembershipProvider.NullArgumentException("roleName");

      int roleid = GetRoleId(roleName);
      if (roleid <= 0)
      {
        return false;
      }
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
      {
        if (throwOnPopulatedRole)
        {
          if (IsRoleInUse(roleid))
            throw new InvalidOperationException(string.Format(Resources.RoleInUse, roleName));
        }
        else
        {
          dbConn.ExecuteNonQuery(string.Format("delete from {0} where roleid=?;", _userInRolesTable), roleid);
        }
        return dbConn.ExecuteNonQuery(string.Format("delete from {0} where roleid=?;", _rolesTable), roleid) > 0;
      }
    }

    public override string[] FindUsersInRole(string roleName, string usernameToMatch)
    {
      if (!Initialized)
      {
        return _prevProvider.FindUsersInRole(roleName, usernameToMatch);
      }
      if (string.IsNullOrEmpty(roleName))
        MySqlSimpleMembershipProvider.NullArgumentException("roleName");
      if (string.IsNullOrEmpty(usernameToMatch))
        return GetUsersInRole(roleName);

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
      {
        var usersName = dbConn.ExecuteQuery(string.Format("select ut.{0} from {1} as rt join {2} as urt on rt.roleid=urt.roleid join {3} as ut on rt.userid=ut.{4} where rt.rolename=? and ut.name like '%?%'", UserNameColumn, _rolesTable, _userInRolesTable, UserTableName, UserIdColumn), roleName, usernameToMatch);
        if (usersName.Count() > 0)
          return usersName.Select(username => username[0].ToString()).ToArray();
      }
      return null;
    }

    public override string[] GetAllRoles()
    {
      if (!Initialized)
        return _prevProvider.GetAllRoles();

      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
      {
        var roles = dbConn.ExecuteQuery(string.Format("select rolename from {0};", _rolesTable));
        if (roles.Count() > 0)
          return roles.Select(role => role[0].ToString()).ToArray();
      }
      return null;
    }

    public override string[] GetRolesForUser(string username)
    {
      if (!Initialized)
        return _prevProvider.GetRolesForUser(username);
      if (string.IsNullOrEmpty(username))
        MySqlSimpleMembershipProvider.NullArgumentException("username");

      string connString = ConnectionString;
      int userid = MySqlSimpleMembershipProvider.GetUserId(username, connString, UserTableName, UserIdColumn, UserNameColumn);
      if (userid > 0)
      {
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(connString))
        {
          var roles = dbConn.ExecuteQuery(string.Format("select rt.rolename from {0} as urt join {1} as rt on urt.roleid = rt.roleid where urt.userid=?;", _userInRolesTable, _rolesTable), userid);
          if (roles.Count() > 0)
            return roles.Select(role => role[0].ToString()).ToArray();
        }
      }
      return null;
    }

    public override string[] GetUsersInRole(string roleName)
    {
      if (!Initialized)
        return _prevProvider.GetUsersInRole(roleName);
      if (string.IsNullOrEmpty(roleName))
        MySqlSimpleMembershipProvider.NullArgumentException("roleName");

      int roleid = GetRoleId(roleName);
      if (roleid > 0)
      {
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
        {
          var users = dbConn.ExecuteQuery(string.Format("select ut.{0} from {1} as urt join {2} as ut on urt.userid = ut.{3} where urt.roleid=?;", UserNameColumn, _userInRolesTable, UserTableName, UserIdColumn), roleid);
          if (users.Count() > 0)
            return users.Select(role => role[0].ToString()).ToArray();
        }
      }
      return null;
    }

    public override bool IsUserInRole(string username, string roleName)
    {
      if (!Initialized)
        return _prevProvider.IsUserInRole(username, roleName);
      string connString = ConnectionString;
      if (string.IsNullOrEmpty(username))
        MySqlSimpleMembershipProvider.NullArgumentException("username");
      if (string.IsNullOrEmpty(roleName))
        MySqlSimpleMembershipProvider.NullArgumentException("roleName");
      int userid = MySqlSimpleMembershipProvider.GetUserId(username, connString, UserTableName, UserIdColumn, UserNameColumn);
      int roleid = GetRoleId(roleName);
      if (userid <= 0 || roleid <= 0)
        return false;
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(connString))
      {
        return (dbConn.ExecuteQuery(string.Format("select count(userid) from {0} where userid=? and roleid=?;", _userInRolesTable), userid, roleid)).Count() > 0;
      }
    }

    public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
    {
      if (!Initialized)
        _prevProvider.RemoveUsersFromRoles(usernames, roleNames);
      else
      {
        if (usernames.Where(username => string.IsNullOrEmpty(username)).Count() > 0 || usernames.Where(username => string.IsNullOrEmpty(username)).Count() > 0)
          throw new ArgumentException(Resources.InvalidArrayValue);
        using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
        {
          foreach (var userid in GetUsersId(usernames))
          {
            foreach (var roleid in GetRolesId(roleNames))
            {
              if (userid > 0 && roleid > 0)
              {
                dbConn.ExecuteNonQuery(string.Format("delete from {0} where userid=? and roleid=?;", _userInRolesTable), userid, roleid);
              }
            }
          }
        }
      }
    }

    public override bool RoleExists(string roleName)
    {
      if (!Initialized)
        return _prevProvider.RoleExists(roleName);
      return (GetRoleId(roleName) > 0);
    }

    #region Properties

    public override string ApplicationName
    {
      get
      {
        if (Initialized)
        {
          throw new NotSupportedException();
        }
        else
        {
          return _prevProvider.ApplicationName;
        }
      }
      set
      {
        if (Initialized)
        {
          throw new NotSupportedException();
        }
        else
        {
          _prevProvider.ApplicationName = value;
        }
      }
    }

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set;  }

    /// <summary>
    /// Gets or sets the name associated to the connection string when stored in the configuration manager.
    /// </summary>
    public string ConnectionStringName { get; set; }

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
        _userTableName = value;
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
    internal bool Initialized
    {
      get;
      set;
    }

    internal void CreateTables()
    {
      var connString = ConnectionString;
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(connString))
      {
        //create schema
        string schema = SchemaManager.GetSchema(11);
        dbConn.ExecuteNonQuery(schema);
      }
    }

    //private string GetConnectionString()
    //{
    //  if (!string.IsNullOrEmpty(ConnectionString))
    //    return ConnectionString;
    //  else
    //  {
    //    ConnectionStringSettings connString = ConfigurationManager.ConnectionStrings[ConnectionStringName];
    //    if (connString != null)
    //      return connString.ConnectionString;
    //  }

    //  if (!string.IsNullOrEmpty(_connString))
    //    return _connString;
    //  throw new InvalidOperationException(Resources.NoConnString);
    //}

    private IEnumerable<int> GetUsersId(string[] usersName)
    {
      foreach (string userName in usersName)
      {
        yield return MySqlSimpleMembershipProvider.GetUserId(userName, ConnectionString, UserTableName, UserIdColumn, UserNameColumn);
      }
    }

    private IEnumerable<int> GetRolesId(string[] roles)
    {
      foreach (string role in roles)
      {
        yield return GetRoleId(role);
      }
    }

    internal int GetRoleId(string role)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
      {
        var roleid = dbConn.ExecuteQuerySingleRecord(string.Format("select roleid from {0} where rolename=?;", _rolesTable), role);
        if (roleid != null)
          return (int)role[0];

        return 0;
      }
    }

    private bool UserHasRole(int userid, int roleid)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
      {
        return (dbConn.ExecuteQuery(string.Format("select count(*) from {0} where userid=? and roleid=?;", _userInRolesTable), userid, roleid).Count() > 0);
      }
    }

    private bool IsRoleInUse(int roleid)
    {
      using (MySqlDatabaseWrapper dbConn = new MySqlDatabaseWrapper(ConnectionString))
      {
        return (dbConn.ExecuteQuery(string.Format("select count(*) from {0} where roleid=?;", _userInRolesTable), roleid).Count() > 0);
      }
    }
    #endregion
  }
}
