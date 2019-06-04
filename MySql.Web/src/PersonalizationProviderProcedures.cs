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

using System;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace MySql.Web.Personalization
{
  internal class PersonalizationProviderProcedures
  {

    /// <summary>
    /// Retrieves profile data from my_aspnet_PersonalizationAllUsers or my_aspnet_PersonalizationPerUser meeting several input criteria.
    /// </summary>
    internal static int myaspnet_PersonalizationAdministration_FindState(bool allUsersScope, long applicationId, string applicationName, int pageIndex, int pageSize, string path, string userName, 
                                                                        DateTime inactiveSinceDate, MySQLPersonalizationConnectionHelper connection, ref MySqlCommand findStateCommand)
    {
      // create memory table to store results

      var sql = "CREATE TEMPORARY TABLE IF NOT EXISTS pageIndexResults(" +
                "IndexId int AUTO_INCREMENT NOT NULL PRIMARY KEY, " +
                "ItemId int not null)";

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var cmd = new MySqlCommand(sql, connection.Connection);
      cmd.ExecuteNonQuery();

      //make sure table is empty      
      cmd.CommandText = "TRUNCATE TABLE pageIndexResults";
      cmd.Connection = connection.Connection;
      cmd.ExecuteNonQuery();

      int pageLowerBound = pageSize * pageIndex;
      int pageUpperBound = pageSize - 1 + pageLowerBound;


      if (allUsersScope)
      {

        var query = "INSERT INTO pageIndexResults (ItemId) (" +
          "SELECT myaspnet_Paths.PathId " +
                    "FROM myaspnet_Paths, " +
                    "((SELECT aspnet_Paths.PathId " +
                    "FROM myaspnet_PersonalizationAllUsers, aspnet_Paths " +
                    "WHERE myaspnet_Paths.ApplicationId = @ApplicationId " +
                    "AND aspnet_PersonalizationAllUsers.PathId = aspnet_Paths.PathId " +
                    "AND (@Path IS NULL OR aspnet_Paths.LoweredPath LIKE @Path) " +
                    ") AS SharedDataPerPath " +
                    "FULL OUTER JOIN " +
                    "(SELECT DISTINCT aspnet_Paths.PathId " +
                    "FROM my_aspnet_personalizationperuser, my_aspnet_paths " +
                    "WHERE my_aspnet_paths.ApplicationId = @ApplicationId " +
                    "AND my_aspnet_personalizationperuser.PathId = aspnet_Paths.PathId " +
                    "AND (@Path IS NULL OR my_aspnet_paths.LoweredPath LIKE @Path) " +
                    ") AS UserDataPerPath " +
                    "ON SharedDataPerPath.PathId = UserDataPerPath.PathId " +
                    ") " +
                    "WHERE my_aspnet_Paths.PathId = SharedDataPerPath.PathId OR my_aspnet_Paths.PathId = UserDataPerPath.PathId " +
                    "ORDER BY my_aspnet_Paths.Path ASC)";

        cmd.CommandText = query;
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@Path", path);
        cmd.Connection = connection.Connection;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT Count(PathId) FROM pageIndexResults";
        cmd.Connection = connection.Connection;
        int totalRecords = (int)cmd.ExecuteScalar();
      
        query = "SELECT my_aspnet_Paths.Path, " +
                    "SharedDataPerPath.LastUpdatedDate, " +
                    "SharedDataPerPath.SharedDataLength, " +
                    "UserDataPerPath.UserDataLength, " +
                    "UserDataPerPath.UserCount " +
                    "FROM aspnet_Paths, " +
                    "((SELECT pageIndexResults.ItemId AS PathId, " +
                    "aspnet_PersonalizationAllUsers.LastUpdatedDate AS LastUpdatedDate, " +
                    "LENGTH(aspnet_PersonalizationAllUsers.PageSettings) AS SharedDataLength " +
                    "FROM my_aspnet_personalizationallusers, PageIndex " +
                    "WHERE my_aspnet_personalizationallusers.PathId = pageIndexResults.IndexId " +
                    "AND pageIndexResults.IndexId >= @PageLowerBound AND pageIndexResults.IndexId <= @PageUpperBound " +
                    ") AS SharedDataPerPath " +
                    "FULL OUTER JOIN " +
                    "(SELECT pageIndexResults.ItemId AS PathId, " +
                    "SUM(LENGTH(my_aspnet_personalizationperuser.PageSettings)) AS UserDataLength, " +
                    "COUNT(*) AS UserCount " +
                    "FROM my_aspnet_personalizationperuser, pageIndexResults " +
                    "WHERE my_aspnet_personalizationperuser.PathId = pageIndexResults.IndexId " +
                    "AND pageIndexResults.IndexId >= @PageLowerBound AND pageIndexResults.IndexId <= @PageUpperBound " +
                    "GROUP BY pageIndexResults.IndexId " +
                    ") AS UserDataPerPath " +
                    "ON SharedDataPerPath.PathId = UserDataPerPath.PathId " +
                    ") " +
                    "WHERE aspnet_Paths.PathId = SharedDataPerPath.PathId OR aspnet_Paths.PathId = UserDataPerPath.PathId " +
                    "ORDER BY my_aspnet_Paths.Path ASC ";

        findStateCommand.CommandText = query;
        findStateCommand.Connection = connection.Connection;
        findStateCommand.Parameters.AddWithValue("@PageLowerBound", pageLowerBound);
        findStateCommand.Parameters.AddWithValue("@PageUpperBound", pageUpperBound);

        return totalRecords;
      }
      else
      {
        var query = "INSERT INTO pageIndexResults (ItemId) (" +
                   "SELECT my_aspnet_personalizationperuser.Id " +
                   "FROM my_aspnet_personalizationperuser, my_aspnet_users, my_aspnet_paths " +
                   "WHERE my_aspnet_paths.ApplicationId = @ApplicationId " +
                   "AND my_aspnet_personalizationperuser.UserId = my_aspnet_Users.Id " +
                   "AND my_aspnet_personalizationperuser.PathId = my_aspnet_Paths.PathId " +
                   "AND (@Path IS NULL OR my_aspnet_paths.LoweredPath LIKE @Path) " +
                   "AND (@UserName IS NULL OR my_aspnet_users.name LIKE @UserName) " +
                   "AND (@InactiveSinceDate IS NULL OR my_aspnet_users.LastActivityDate <= @InactiveSinceDate) " +
                   "ORDER BY my_aspnet_paths.Path ASC, my_aspnet_users.name ASC )";
        
        cmd.CommandText = query;
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@Path", path);
        cmd.Parameters.AddWithValue("@UserName", userName);
        cmd.Parameters.AddWithValue("@InactiveSinceDate", inactiveSinceDate);
        cmd.Connection = connection.Connection;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT Count(IndexId) FROM pageIndexResults";
        cmd.Connection = connection.Connection;
        var totalRecords = cmd.ExecuteScalar().ToString();

        query = "SELECT my_aspnet_Paths.Path, my_aspnet_personalizationperuser.LastUpdatedDate, LENGTH(my_aspnet_personalizationperuser.PageSettings) as Size, my_aspnet_Users.Name, my_aspnet_Users.LastActivityDate " +
                    "FROM my_aspnet_personalizationperuser, my_aspnet_users, my_aspnet_paths, pageIndexResults " +
                    "WHERE my_aspnet_personalizationperuser.Id = PageIndexResults.IndexId " +
                    "AND my_aspnet_personalizationperuser.UserId = my_aspnet_users.Id " +
                    "AND my_aspnet_personalizationperuser.PathId = my_aspnet_paths.PathId " +
                    "AND pageIndexResults.ItemId >= @PageLowerBound AND PageIndexResults.ItemId <= @PageUpperBound " +
                    "ORDER BY my_aspnet_paths.Path ASC, my_aspnet_users.name ASC ";

        findStateCommand.CommandText = query;
        findStateCommand.Parameters.AddWithValue("@PageUpperBound", pageUpperBound);
        findStateCommand.Parameters.AddWithValue("@PageLowerBound", pageLowerBound);
        findStateCommand.Connection = connection.Connection;

        return int.Parse(totalRecords, CultureInfo.InvariantCulture);
      }
    }


    internal static int myaspnet_PersonalizationAdministration_GetCountOfState(bool allUsersScope, string applicationName, long applicationId, string path, string userName, DateTime inactiveSinceDate, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return 0;

       if (!connection.Opened)
        throw new Exception("Error: Connection should be open");
      
      if (allUsersScope)
      {

        MySqlCommand cmd = new MySqlCommand("Select count(*) from my_aspnet_personalizationallusers, my_aspnet_paths " +
                                             "where my_aspnet_paths.applicationId = @ApplicationId and " +
                                             "my_aspnet_personalizationallusers.pathid = my_aspnet_paths.pathid and " +
                                             "(@Path is null or my_aspnet_paths.loweredpath like lower(@Path))", connection.Connection);

        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@Path", path);
        cmd.Connection = connection.Connection;
        var count = cmd.ExecuteScalar().ToString();
        return int.Parse(count, CultureInfo.InvariantCulture);
      }
      else {
        MySqlCommand cmd = new MySqlCommand("Select count(*) from my_aspnet_personalizationperuser as peruser, my_aspnet_users as users, "+
                                        "my_aspnet_paths as paths " +
                                        "where paths.applicationId = @ApplicationId and " +
                                        "peruser.userid = users.id and " +
                                        "peruser.pathId = paths.pathId and " +                                         
                                        "(@Path is null or paths.loweredpath like lower(@Path) and " +
                                        "(@UserName is null or users.name like lower(@UserName))) and " +
                                        "(@InactiveSinceDate is null or users.lastactivitydate <= @InactiveSinceDate) ", connection.Connection);

        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@Path", path);
        cmd.Parameters.AddWithValue("@UserName", userName);
        cmd.Parameters.AddWithValue("@InactiveSinceDate", inactiveSinceDate);
        cmd.Connection = connection.Connection;
        var count = cmd.ExecuteScalar().ToString();
        return int.Parse(count, CultureInfo.InvariantCulture);
      }    
    }


    internal static Byte[] my_aspnet_PersonalizationPerUser_GetPageSettings(long applicationId, string path, string userName, DateTime currentTimeUtc, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return null;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      //get pathid
      var cmd = new MySqlCommand("select pathId from my_aspnet_paths where applicationid = @ApplicationId and loweredpath = lower(@Path)", connection.Connection);
      cmd.Connection = connection.Connection;

      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@Path", path);

      var pathId = (cmd.ExecuteScalar() ?? "").ToString();

      if (string.IsNullOrEmpty(pathId))
        return null;

      cmd = new MySqlCommand("select Id from my_aspnet_users where applicationid = @ApplicationId and name =  @UserName", connection.Connection);
      cmd.Connection = connection.Connection;
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@UserName", userName);


      var userId = (cmd.ExecuteScalar() ?? "").ToString();
      userId = string.IsNullOrEmpty(userId) ? "0" : userId;

      if (int.Parse(userId, CultureInfo.InvariantCulture) == 0)
        return null;

      UpdateUserLastActiveDate(connection.Connection, int.Parse(userId, CultureInfo.InvariantCulture), currentTimeUtc);      

      cmd = new MySqlCommand("select pagesettings from my_aspnet_personalizationperuser as peruser where peruser.pathid = @PathId and peruser.userid = @UserId");
      cmd.Connection = connection.Connection;
      cmd.Parameters.AddWithValue("@PathId", pathId);
      cmd.Parameters.AddWithValue("@UserId", userId);

      var reader = cmd.ExecuteReader();

      byte[] settings = null;
      while (reader.Read())
      {        
        int size = (int)reader.GetBytes(0, 0, null, 0, 0);
        settings = new byte[size];
        reader.GetBytes(0, 0, settings, 0, size);                
      }
      reader.Close();
      return settings;          
    }


    internal static byte[] my_aspnet_PersonalizationAllUsers_GetPageSettings(long applicationId, string path, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return null;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var cmd = new MySqlCommand("Select pathid from my_aspnet_paths as paths where paths.applicationid = @ApplicationId  and paths.loweredPath = lower(@Path)", connection.Connection);
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@Path", path);
      var pathId = (string)cmd.ExecuteScalar();

      if (!string.IsNullOrEmpty(pathId))
      {
        cmd.CommandText = "Select PageSettings from my_aspnet_personalizationallusers where pathId = @PathId";
        cmd.Parameters.AddWithValue("@PathId", pathId);
        cmd.Connection = connection.Connection;
        var reader = cmd.ExecuteReader();

        byte[] settings = null;
        while (reader.Read())
        {
          int size = (int)reader.GetBytes(0, 0, null, 0, 0);
          settings = new byte[size];
          reader.GetBytes(0, 0, settings, 0, size);
        }
        reader.Close();
        return settings;          
      }

      return null;
    }


    internal static void my_aspnet_PersonalizationPerUser_ResetPageSettings(long applicationId, string userName, string path, DateTime currentTimeUtc, MySQLPersonalizationConnectionHelper connection)
    {
      
      if (applicationId <= 0)
        return;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var cmd = new MySqlCommand("Select pathid from my_aspnet_paths as paths where paths.applicationid = @ApplicationId  and paths.loweredPath = lower(@Path)", connection.Connection);
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@Path", path);
      var pathId = (string)cmd.ExecuteScalar();

      if (!string.IsNullOrEmpty(pathId))
      {
        cmd = new MySqlCommand("select Id from my_aspnet_users where applicationid = @ApplicationId and name = @UserName", connection.Connection);
        cmd.Connection = connection.Connection;
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@UserName", userName);

        var userId = (int)cmd.ExecuteScalar();
        if (userId != 0)
        {
          var rows = UpdateUserLastActiveDate(connection.Connection, userId, currentTimeUtc);          
          if (rows != 0)
          {
            cmd = new MySqlCommand("delete from my_aspnet_personalizationperuser WHERE pathId = @PathId AND userId = @UserId");
            cmd.Connection = connection.Connection;
            cmd.Parameters.AddWithValue("@PathId", pathId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();          
          }
        }
      }    
    }

    internal static void my_aspnet_PersonalizationAllUsers_ResetPageSettings(long applicationId, string path, MySQLPersonalizationConnectionHelper connection)
    {

      if (applicationId <= 0)
        return;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var cmd = new MySqlCommand("Select pathid from my_aspnet_paths as paths where paths.applicationid = @ApplicationId  and paths.loweredPath = lower(@Path)", connection.Connection);
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@Path", path);
      var pathId = (cmd.ExecuteScalar() ?? "").ToString();

      if (!string.IsNullOrEmpty(pathId))
      {
        cmd = new MySqlCommand("delete my_aspnet_personalizationallusers.* from my_aspnet_personalizationallusers WHERE pathId = @PathId");
        cmd.Connection = connection.Connection;
        cmd.Parameters.AddWithValue("@PathId", pathId);        
        cmd.ExecuteNonQuery();          
      }

    }


    private static int UpdateUserLastActiveDate(MySqlConnection cnn, int userId, DateTime currentTimeUtc)
    {
      MySqlTransaction trans;
      trans = cnn.BeginTransaction();
      try
      {        
        var cmd = new MySqlCommand("update my_aspnet_users set lastactivitydate = @CurrentTimeUtc where id = @UserId");
        cmd.Connection = cnn;
        cmd.Transaction = trans;
        cmd.Parameters.AddWithValue("@CurrentTimeUtc", currentTimeUtc);
        cmd.Parameters.AddWithValue("@UserId", userId);
        var rows = cmd.ExecuteNonQuery();
        trans.Commit();
        return rows;
      }
      catch
      {
        trans.Rollback();
        throw;
      }
    }


    internal static int my_aspnet_PersonalizationAdministration_ResetUserState(long applicationId, DateTime inactiveSinceDate,string[] usernames, string[] paths, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return 0;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var rows = 0;
      var cmd = new MySqlCommand();
      if (usernames == null) usernames = new string[1] { "" };
      if (paths == null ) paths = new string[1] { "" };

      foreach (var username in usernames)
      {
        foreach (var path in paths)
        {
          var query = "DELETE peruser.* FROM (my_aspnet_personalizationperuser as peruser  " +
                            "INNER JOIN my_aspnet_users as users ON " +
                            "peruser.userid = users.id) " +
                            "INNER JOIN my_aspnet_paths as paths ON " +
                            "paths.applicationId = @ApplicationId AND " +
                            "paths.pathid = peruser.pathid AND " +
                            "(@InactiveSinceDate is null OR users.lastactivitydate <= @InactiveSinceDate) ";
          query = string.IsNullOrEmpty(username) ? query : query += " AND (@UserName is null OR users.name = @UserName)  ";
          query = string.IsNullOrEmpty(path) ? query : query +=  " AND (@Path is null OR paths.loweredpath = LOWER(@Path))";          
          cmd.CommandText = query;
          cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
          cmd.Parameters.AddWithValue("@InactiveSinceDate", inactiveSinceDate);
          cmd.Parameters.AddWithValue("@Path", path);
          cmd.Parameters.AddWithValue("@UserName", username);
          cmd.Connection = connection.Connection;
          rows += cmd.ExecuteNonQuery();
        }
      }
      
      return rows;
    }


    internal static int my_aspnet_PersonalizationAdministration_DeleteAllState(long applicationId, bool allUsersScope, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return 0;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var cmd = new MySqlCommand();
      cmd.Connection = connection.Connection;

      if (allUsersScope)
      {
        cmd.CommandText = "DELETE FROM my_aspnet_personalizationallusers " +
                          "WHERE PathId IN (SELECT Paths.PathId FROM my_aspnet_paths as paths " +
                          "WHERE paths.ApplicationId = @ApplicationId)";
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);        
      }
      else
      {
        cmd.CommandText = "DELETE FROM my_aspnet_personalizationperuser " +
                          "WHERE PathId IN (SELECT Paths.PathId FROM my_aspnet_paths as paths " +
                          "WHERE paths.ApplicationId = @ApplicationId)";
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);        
      }

      var rows = cmd.ExecuteNonQuery();
      return rows;
    }

    internal static int my_aspnet_PersonalizationAdministration_ResetSharedState(long applicationId, string[] paths, MySQLPersonalizationConnectionHelper connection)
    {
     if (applicationId <= 0)
        return 0;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      if (paths == null)
        return 0;

      var cmd = new MySqlCommand();
      cmd.Connection = connection.Connection;

      var rows = 0;

      foreach (var path in paths)
      {
        cmd.CommandText = "DELETE my_aspnet_personalizationallusers.* FROM my_aspnet_personalizationallusers " +
                           "INNER JOIN my_aspnet_paths as paths ON " +
                           "((paths.ApplicationId = @ApplicationId AND " +
                           "my_aspnet_personalizationallusers.PathId = paths.PathId) AND " +
                           "paths.loweredpath = LOWER(@Path))";
                           
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@Path", path);
        rows += cmd.ExecuteNonQuery();       
      }

      return rows;
    }


    internal static int my_aspnet_PersonalizationAdministration_DeleteAllState(bool allUsersScope, long applicationId, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return 0;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");
      
      var cmd = new MySqlCommand();
      cmd.Connection = connection.Connection;

      if (allUsersScope)
      {

        cmd.CommandText = "DELETE FROM my_aspnet_personalizationallusers " +
                          "WHERE PathId IN " +
                          "(SELECT paths.PathId FROM my_aspnet_paths as paths " +
                            "WHERE paths.ApplicationId = @ApplicationId)";        
      }
      else
      { 
        cmd.CommandText = "DELETE FROM my_aspnet_personalizationperuser " +
                          "WHERE PathId IN " +
                          "(SELECT Paths.PathId FROM my_aspnet_paths Paths " +
                          "WHERE Paths.ApplicationId = @ApplicationId)";     
      }
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      var rows = cmd.ExecuteNonQuery();
      return rows;
    }

    /// <summary>
    /// Saves per-user state for the specified page and the specified user in the my_aspnet_PersonalizationPerUser table.
    /// </summary>
    /// <returns></returns>
    internal static int my_aspnet_PersonalizationPerUser_SetPageSettings(long applicationId, string userName, string path, byte[] settings, DateTime currentTimeUtc, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return 0;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var cmd = new MySqlCommand();
      cmd.Connection = connection.Connection;

      cmd.CommandText = "SELECT PathId FROM my_aspnet_paths WHERE ApplicationId = @ApplicationId AND LoweredPath = LOWER(@Path)";
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@Path", path);
      var pathId = (string)cmd.ExecuteScalar();

      cmd.Parameters.Clear();

      if (pathId == null)
      {
        // create path        
        MySqlTransaction trans;
        trans = connection.Connection.BeginTransaction();

        try
        {                    
          cmd.Transaction = trans;
          cmd.CommandText = "INSERT INTO my_aspnet_paths (applicationId, pathId, path, loweredpath) values (@ApplicationId, @PathId, @Path, LOWER(@Path))";
          cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
          cmd.Parameters.AddWithValue("@PathId", pathId = Guid.NewGuid().ToString());
          cmd.Parameters.AddWithValue("@Path", path);
          cmd.ExecuteNonQuery();
          trans.Commit();
        }
        catch
        {
          trans.Rollback();
          throw;
        }
      }

      cmd.Parameters.Clear();
      cmd.CommandText = "SELECT id FROM my_aspnet_users WHERE ApplicationId = @ApplicationId AND name = LOWER(@UserName)";
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@UserName", userName);
      var userId = (cmd.ExecuteScalar() ?? "").ToString();

      userId = string.IsNullOrEmpty(userId) ? "0" : userId;

      // create user
      if (int.Parse(userId, CultureInfo.InvariantCulture) == 0)
      {
        // create path        
        MySqlTransaction trans;
        trans = connection.Connection.BeginTransaction();

        try
        {
          cmd.Parameters.Clear();
          cmd.Transaction = trans;
          cmd.CommandText = "INSERT INTO my_aspnet_users (applicationId, name, isAnonymous, lastActivityDate) values (@ApplicationId, @UserName, false, @CurrentTimeUtc)";
          cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
          cmd.Parameters.AddWithValue("@UserName", userName);
          cmd.Parameters.AddWithValue("@CurrentTimeUtc", DateTime.UtcNow);
          cmd.ExecuteNonQuery();
          trans.Commit();
        }
        catch
        {
          trans.Rollback();
          throw;
        }

          cmd.Parameters.Clear();
          cmd.CommandText = "SELECT Id from my_aspnet_users where applicationId = @ApplicationId and name = @UserName)";
          cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
          cmd.Parameters.AddWithValue("@UserName", userName);
          userId = (string)cmd.ExecuteScalar();
   
      }
      var rows = UpdateUserLastActiveDate(connection.Connection, int.Parse(userId, CultureInfo.InvariantCulture), DateTime.UtcNow);
      if (rows == 0)
        throw new Exception("User not found");

      cmd.Parameters.Clear();
      cmd.CommandText = "Select COUNT(*) from my_aspnet_personalizationperuser where userid = @UserId and pathId = @PathId";
      cmd.Parameters.AddWithValue("@UserId", userId);
      cmd.Parameters.AddWithValue("@PathId", pathId);
      if ((long)cmd.ExecuteScalar() > 0)
      {
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE my_aspnet_personalizationperuser SET PageSettings = @PageSettings, LastUpdatedDate = @CurrentTimeUtc " +
                          "where userid = @UserId and pathId = @PathId";
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@PathId", pathId);
        cmd.Parameters.AddWithValue("@PageSettings", settings);
        cmd.Parameters.AddWithValue("@CurrentTimeUtc", DateTime.UtcNow);
        cmd.ExecuteNonQuery();
      }
      else {
        cmd.Parameters.Clear();
        cmd.CommandText = "INSERT INTO my_aspnet_personalizationperuser(applicationId, pathId, userId, pageSettings, lastUpdatedDate) VALUES(@applicationId, @PathId, @userId, @PageSettings, @LastUpdatedDate)";
        cmd.Parameters.AddWithValue("@applicationId", applicationId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@PathId", pathId);
        cmd.Parameters.AddWithValue("@PageSettings", settings);
        cmd.Parameters.AddWithValue("@LastUpdatedDate", DateTime.UtcNow);
        rows = cmd.ExecuteNonQuery();      
      }
      return rows;
    }
    
    /// <summary>
    /// Saves shared state for the specified page in the aspnet_PersonalizationAllUsers table
    /// </summary>
    /// <returns></returns>
    internal static int my_aspnet_PersonalizationAllUsers_SetPageSettings(long applicationId, string path, byte[] settings, DateTime currentTimeUtc, MySQLPersonalizationConnectionHelper connection)
    {
      if (applicationId <= 0)
        return 0;

      if (!connection.Opened)
        throw new Exception("Error: Connection should be open");

      var cmd = new MySqlCommand();
      cmd.Connection = connection.Connection;

      cmd.CommandText = "SELECT PathId FROM my_aspnet_paths WHERE ApplicationId = @ApplicationId AND LoweredPath = LOWER(@Path)";
      cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
      cmd.Parameters.AddWithValue("@Path", path);
      var pathId = (string)cmd.ExecuteScalar();
      cmd.Parameters.Clear();

      if (pathId == null)
      {
        // create path        
        MySqlTransaction trans;
        trans = connection.Connection.BeginTransaction();

        try
        {          
          cmd.Transaction = trans;
          cmd.CommandText = "INSERT INTO my_aspnet_paths (applicationId, pathId, path, loweredpath) values (@ApplicationId, @PathId, @Path, LOWER(@Path))";
          cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
          cmd.Parameters.AddWithValue("@PathId", pathId = Guid.NewGuid().ToString());
          cmd.Parameters.AddWithValue("@Path", path);
          cmd.ExecuteNonQuery();
          trans.Commit();
        }
        catch
        {
          trans.Rollback();
          throw;
        }
      }
      
      cmd.CommandText = "INSERT INTO my_aspnet_personalizationallusers(PathId, PageSettings, LastUpdatedDate) VALUES (@PathId, @PageSettings, @CurrentTimeUtc)";
      cmd.CommandText += " ON DUPLICATE KEY UPDATE PageSettings=Values(PageSettings), LastUpdatedDate=Values(LastUpdatedDate)";
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("@PageSettings", settings);
      cmd.Parameters.AddWithValue("@PathId", pathId);
      cmd.Parameters.AddWithValue("@CurrentTimeUtc", currentTimeUtc);
      var rows = cmd.ExecuteNonQuery();
      return rows;
    }
  }
}
