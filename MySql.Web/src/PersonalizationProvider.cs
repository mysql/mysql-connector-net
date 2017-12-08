﻿// Copyright © 2014 Oracle and/or its affiliates. All rights reserved.
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
using System.Configuration;
using System.Diagnostics;
using System.Web.Hosting;
using System.Web.UI.WebControls.WebParts;
using MySql.Data.MySqlClient;
using MySql.Web.Common;
using MySql.Web.General;

namespace MySql.Web.Personalization
{
  /// <summary>
  /// Implementation for Personalization Provider
  /// to use web parts in ASP.NET websites
  /// </summary>  
  public class MySqlPersonalizationProvider : PersonalizationProvider
  {

    string connectionString;


    string eventSource = "MySQLPersonalizationProvider";
    string eventLog = "Application";
    string exceptionMessage = "An exception occurred. Please check the event log.";
    bool writeExceptionsToEventLog = false;

    Application app;
    
    /// <summary>
    /// Set or gets the Application Name 
    /// </summary>
    public override string ApplicationName
    {
      get { return app.Name; }
      set { app.Name = value; }
    }

    private long ApplicationId
    {
      get { return app.Id; }
    }

     private enum ResetUserStateMode {
            PerInactiveDate,
            PerPaths,
            PerUsers
        }


    /// <summary>
    /// Initializes settings values for Personalization Provider
    /// </summary>
    /// <param name="name"></param>
    /// <param name="config"></param>
    public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
    {
      if (config == null)
        throw new ArgumentNullException("config");

      if (string.IsNullOrEmpty(name))
      {
        name = "MySqlPersonalizationProvider";
      }

      if (string.IsNullOrEmpty(config["description"]))
      {
        config.Remove("description");
        config.Add("description", "MySql Personalization provider");      
      }

      base.Initialize(name, config);

      string applicationName = HostingEnvironment.ApplicationVirtualPath;
      if (!String.IsNullOrEmpty(config["applicationName"]))
        applicationName = config["applicationName"];


      if (!(config["writeExceptionsToEventLog"] == null))
      {
        if (config["writeExceptionsToEventLog"].ToUpper() == "TRUE")
        {
          writeExceptionsToEventLog = true;
        }
      }
      connectionString = ConfigUtility.GetConnectionString(config);
      if (String.IsNullOrEmpty(connectionString)) return;

      // Make sure we have the correct schema.
      SchemaManager.CheckSchema(connectionString, config);

      app = new Application(applicationName, base.Description);

      // Get the application id.
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          conn.Open();
          app.EnsureId(conn);          
        }
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - Initialize");        
        throw;
      }  
    }

   /// <summary>
   /// Returns a collection of PersonalizationStateInfo objects containing administrative information regarding records in the database that match the specified criteria
   /// -for example, records corresponding to users named Jeff* that have been modified since January 1, 2005. Wildcard support is provider-dependent.
   /// </summary>
   /// <param name="scope"></param>
   /// <param name="query"></param>
   /// <param name="pageIndex"></param>
   /// <param name="pageSize"></param>
   /// <param name="totalRecords"></param>
   /// <returns></returns>
   public override PersonalizationStateInfoCollection FindState(PersonalizationScope scope, PersonalizationStateQuery query, int pageIndex, int pageSize, out int totalRecords)
   {

     if (query == null)
       throw new ArgumentNullException("query");

     if (pageIndex < 0)
       throw new ArgumentOutOfRangeException("pageIndex");

     if (pageSize < 1)
       throw new ArgumentOutOfRangeException("pageSize");

     if (query.PathToMatch == null)
       throw new ArgumentNullException("query.PathToMatch");

     if (query.UsernameToMatch == null)
       throw new ArgumentNullException("query.UserToMatch");

     DateTime inactiveSinceDate = query.UserInactiveSinceDate;     

     if (scope == PersonalizationScope.User)
     {      
       return FindUserState(query.PathToMatch.Trim(), inactiveSinceDate, query.UsernameToMatch.Trim(), pageIndex, pageSize, out totalRecords);          
     }
     else 
     {
       return FindSharedState(query.PathToMatch.Trim(), pageIndex, pageSize, out totalRecords);
     }
     
   }


  /// <summary>
  /// Returns the number of records in the database that match the specified criteria-
  /// for example, records corresponding to users named Jeff* that haven't been modified since January 1, 2005. Wildcard support is provider-dependent.
  /// </summary>
  /// <param name="scope"></param>
  /// <param name="query"></param>
  /// <returns></returns>
   public override int GetCountOfState(PersonalizationScope scope, PersonalizationStateQuery query)
   {
     if (query == null)
       throw new ArgumentNullException("query");

     if (scope == PersonalizationScope.User)
     {
       return GetCountUserState(query.PathToMatch.Trim(), query.UserInactiveSinceDate, query.UsernameToMatch);
     }
     else
     {
       return GetCountOfSharedState(query.PathToMatch.Trim());
     }
   }


    /// <summary>
    /// Retrieves personalization state as opaque blobs from the data source. Retrieves both shared and user personalization state corresponding to a specified user and a specified page.
    /// </summary>
    /// <param name="webPartManager"></param>
    /// <param name="path"></param>
    /// <param name="userName"></param>
    /// <param name="sharedDataBlob"></param>
    /// <param name="userDataBlob"></param>
    protected override void LoadPersonalizationBlobs(WebPartManager webPartManager, string path, string userName, ref Byte[] sharedDataBlob, ref Byte[] userDataBlob)
    {
       sharedDataBlob = null;
       userDataBlob = null;
       MySQLPersonalizationConnectionHelper connection = new MySQLPersonalizationConnectionHelper(connectionString);
       connection.OpenConnection(true);
       try
       {
         sharedDataBlob = PersonalizationProviderProcedures.my_aspnet_PersonalizationAllUsers_GetPageSettings(
                          ApplicationId, path, connection);
         if (!String.IsNullOrEmpty(userName))
         {
           userDataBlob = PersonalizationProviderProcedures.my_aspnet_PersonalizationPerUser_GetPageSettings(
                             ApplicationId, path, userName, DateTime.UtcNow, connection);
         }

         connection.CloseConnection();
       }
       catch (Exception ex)
       {
         if (writeExceptionsToEventLog)
           WriteToEventLog(ex, "MySQLPersonalizationProvider - LoadPersonazalitionBlobs");        
         throw;
       }
       finally
       {
         connection.CloseConnection();       
       }
    }
    /// <summary>
    /// Deletes personalization state corresponding to a specified user and a specified page from the database.
    /// </summary>
    /// <param name="webPartManager"></param>
    /// <param name="path"></param>
    /// <param name="userName"></param>
    protected override void ResetPersonalizationBlob(WebPartManager webPartManager, string path,  string userName) 
    {
       MySQLPersonalizationConnectionHelper connection = new MySQLPersonalizationConnectionHelper(connectionString);
       connection.OpenConnection(true);
       try
       {
         if (string.IsNullOrEmpty(userName))
         {
           PersonalizationProviderProcedures.my_aspnet_PersonalizationAllUsers_ResetPageSettings(ApplicationId, path, connection);
         }
         else
         {
           PersonalizationProviderProcedures.my_aspnet_PersonalizationPerUser_ResetPageSettings(ApplicationId, userName, path, DateTime.UtcNow, connection);
         }
       }
       catch (Exception ex)
       {
         if (writeExceptionsToEventLog)
           WriteToEventLog(ex, "MySQLPersonalizationProvider - ResetPersonalizationBlob");        
         throw;
       }
       finally
       {
         connection.CloseConnection();       
       }
    }

    /// <summary>
    /// Deletes personalization state corresponding to the specified users and specified pages from the database
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="paths"></param>
    /// <param name="usernames"></param>
    /// <returns></returns>
    public override int ResetState(PersonalizationScope scope, string[] paths, string[] usernames)
    {      
     
      bool hasPaths = !(paths == null || paths.Length == 0);
      bool hasUsers = !(usernames == null || usernames.Length == 0);

      var count = 0;

      var connection = new MySQLPersonalizationConnectionHelper(connectionString);
      connection.OpenConnection(true);

      if (scope == PersonalizationScope.Shared)
      {        
        try
        {                
          if (paths == null) // reset all state
          {
            return PersonalizationProviderProcedures.my_aspnet_PersonalizationAdministration_DeleteAllState(true, ApplicationId, connection);
          }
          else
          {
            return PersonalizationProviderProcedures.my_aspnet_PersonalizationAdministration_ResetSharedState(ApplicationId, paths, connection);
          }
        }
        catch (Exception ex)
        {
          if (writeExceptionsToEventLog)
            WriteToEventLog(ex, "MySQLPersonalizationProvider - ResetState");        
          throw;
        }
        finally
        {
          connection.CloseConnection();
        }
      }
      else
      {
        if (!hasPaths && !hasUsers)
        {
          count = PersonalizationProviderProcedures.my_aspnet_PersonalizationAdministration_DeleteAllState(true, ApplicationId, connection);
        }
        else if (!hasUsers)
        {
          count = ResetUserState(ResetUserStateMode.PerPaths, DateTime.MaxValue, paths, null);
        }
        else
        {
          count = ResetUserState(ResetUserStateMode.PerUsers, DateTime.MaxValue, paths, usernames);
        }
      }
      return count;
    }


    /// <summary>
    /// Deletes user personalization state corresponding to the specified pages and that hasn't been updated since a specified date from the database.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="userInactiveSinceDate"></param>
    /// <returns></returns>
    public override int ResetUserState(string path, DateTime userInactiveSinceDate)
    {
      if (string.IsNullOrEmpty(path))
        return 0;

      string [] paths = (path == null) ? null : new string [] {path};
      try
      {
        return ResetUserState(ResetUserStateMode.PerInactiveDate, userInactiveSinceDate, paths, null);
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - ResetUserState");        
        throw;
      }
    }

    /// <summary>
    /// Writes personalization state corresponding to a specified user and a specified page as an opaque blob to the database. 
    /// If userName is null, then the personalization state is shared state and is not keyed by user name.
    /// </summary>
    /// <param name="webPartManager"></param>
    /// <param name="path"></param>
    /// <param name="userName"></param>
    /// <param name="dataBlob"></param>
    protected override void SavePersonalizationBlob(WebPartManager webPartManager, string path, string userName, Byte[] dataBlob)
    {

      MySQLPersonalizationConnectionHelper connection = new MySQLPersonalizationConnectionHelper(connectionString);

      try
      {
        MySqlCommand cmd = new MySqlCommand();
        connection.OpenConnection(true);
        if (!string.IsNullOrEmpty(userName))
        {
            PersonalizationProviderProcedures.my_aspnet_PersonalizationPerUser_SetPageSettings(ApplicationId, userName, path, dataBlob, DateTime.UtcNow, connection);
        }
        else
        {                     
            PersonalizationProviderProcedures.my_aspnet_PersonalizationAllUsers_SetPageSettings(ApplicationId, path, dataBlob, DateTime.UtcNow, connection);          
        }
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - SavePersonalizationBlob");        
        throw;
      }     
    }

    private PersonalizationStateInfoCollection FindSharedState(string path, int pageIndex, int pageSize, out int totalRecords)
    {
      MySQLPersonalizationConnectionHelper connection = new MySQLPersonalizationConnectionHelper(connectionString);

      try
      {
        MySqlCommand cmd = new MySqlCommand();
        connection.OpenConnection(true);
        totalRecords = PersonalizationProviderProcedures.myaspnet_PersonalizationAdministration_FindState(true, ApplicationId, ApplicationName, pageIndex, pageSize,
                       path, null, DateTime.MinValue, connection, ref cmd);

        PersonalizationStateInfoCollection sharedStateInfoCollection = new PersonalizationStateInfoCollection();

        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            string pathQuery = reader.GetString("Path");
            DateTime lastUpdatedDate = (reader.IsDBNull(1)) ? DateTime.MinValue :
                                         DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc);
            int size = (reader.IsDBNull(2)) ? 0 : reader.GetInt32("SharedDataLength");
            int userDataLength = (reader.IsDBNull(3)) ? 0 : reader.GetInt32("UserDataLength");
            int userCount = (reader.IsDBNull(4)) ? 0 : reader.GetInt32("UserCount");

            sharedStateInfoCollection.Add(new SharedPersonalizationStateInfo(
                                    pathQuery, lastUpdatedDate, size, userDataLength, userCount));
          }
        }
        connection.CloseConnection();
        return sharedStateInfoCollection;
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - FindSharedState");        
        throw;
      }
      finally
      {
        connection.CloseConnection();
      }
    }



    private PersonalizationStateInfoCollection FindUserState(string path, DateTime inactiveSinceDate, string userName, int pageIndex, int pageSize, out int totalRecords)
    {
      MySQLPersonalizationConnectionHelper connection = new MySQLPersonalizationConnectionHelper(connectionString);
      
      try
      {

        MySqlCommand cmd = new MySqlCommand();
        connection.OpenConnection(true);
        totalRecords = PersonalizationProviderProcedures.myaspnet_PersonalizationAdministration_FindState(false, ApplicationId, ApplicationName, pageIndex, pageSize, 
                       path, userName, inactiveSinceDate, connection, ref cmd);

        PersonalizationStateInfoCollection stateInfoCollection = new PersonalizationStateInfoCollection();

        using(var reader = cmd.ExecuteReader())
        {          
          while (reader.Read())
          {
            string pathQuery = reader.GetString("Path");
            DateTime lastUpdatedDate = DateTime.SpecifyKind(reader.GetDateTime("LastUpdatedDate"), DateTimeKind.Utc);
            int size = reader.GetInt32("Size");
            string usernameQuery = reader.GetString("name");
            DateTime lastActivityDate = DateTime.SpecifyKind(reader.GetDateTime("LastActivityDate"), DateTimeKind.Utc);
            stateInfoCollection.Add(new UserPersonalizationStateInfo(pathQuery, lastActivityDate, size, usernameQuery, lastActivityDate));        
          }
        }       
        connection.CloseConnection();        

        return stateInfoCollection;
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - FindUserState");        
        throw;
      }
      finally
      {
        connection.CloseConnection();
      }
    }  


    private int GetCountOfSharedState(string path)
    {     
      MySQLPersonalizationConnectionHelper connection = new MySQLPersonalizationConnectionHelper(connectionString);      
      try
      {
        MySqlCommand cmd = new MySqlCommand();
        connection.OpenConnection(true);
        return PersonalizationProviderProcedures.myaspnet_PersonalizationAdministration_GetCountOfState(
                                                 true, ApplicationName, ApplicationId, path,
                                                 null, DateTime.MinValue, connection);
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - GetCountOfSharedState");        
        throw;
      }
      finally 
      {
        connection.CloseConnection();      
      }
    }

    private int GetCountUserState(string path, DateTime userInactiveSinceDate, string userName)
    {
      MySQLPersonalizationConnectionHelper connection = new MySQLPersonalizationConnectionHelper(connectionString);      
      try
      {
        MySqlCommand cmd = new MySqlCommand();
        connection.OpenConnection(true);
        return PersonalizationProviderProcedures.myaspnet_PersonalizationAdministration_GetCountOfState(
                                                 false, ApplicationName, ApplicationId, path,
                                                 userName, userInactiveSinceDate, connection);
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - GetCountUserState");        
        throw;
      }
      finally 
      {
        connection.CloseConnection();      
      }
    }

    private int ResetUserState(ResetUserStateMode mode, DateTime userInactiveSinceDate, string[] paths,  string[] usernames)
    {
      var connection = new MySQLPersonalizationConnectionHelper(connectionString);
      connection.OpenConnection(true);
      
      try
      {
        if (ResetUserStateMode.PerInactiveDate == mode)
        {
          return PersonalizationProviderProcedures.my_aspnet_PersonalizationAdministration_ResetUserState(ApplicationId, userInactiveSinceDate.ToUniversalTime(), null, null, connection);        
        }
        if (ResetUserStateMode.PerPaths == mode)
        {
          return PersonalizationProviderProcedures.my_aspnet_PersonalizationAdministration_ResetUserState(ApplicationId, userInactiveSinceDate, null, paths, connection);
        }
        else
        {
          return PersonalizationProviderProcedures.my_aspnet_PersonalizationAdministration_ResetUserState(ApplicationId, userInactiveSinceDate, usernames, paths, connection);        
        }
      }
      catch (Exception ex)
      {
        if (writeExceptionsToEventLog)
          WriteToEventLog(ex, "MySQLPersonalizationProvider - ResetUserState");  
        throw;
      }
      finally
      {
        connection.CloseConnection(); 
      }
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

  }
}
