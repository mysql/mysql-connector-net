// Copyright © 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Configuration;
using System.Collections.Specialized;
using System.Web.Hosting;
using MySql.Data.MySqlClient;
using System.Configuration.Provider;
using System.IO;
using MySql.Web.Common;
using MySql.Web.General;
using System.Web.SessionState;
using System.Web.Configuration;
using System.Web;
using System.Diagnostics;
using System.Threading;
using System.Security;


namespace MySql.Web.SessionState
{
  /// <summary>
  /// This class allows ASP.NET applications to store and manage session state information in a
  /// MySQL database.
  /// Expired session data is periodically deleted from the database.
  /// </summary>
  public class MySqlSessionStateStore : SessionStateStoreProviderBase
  {
    string connectionString;
//    ConnectionStringSettings connectionStringSettings;
    string eventSource = "MySQLSessionStateStore";
    string eventLog = "Application";
    string exceptionMessage = "An exception occurred. Please check the event log.";
    Application app;

    SessionStateSection sessionStateConfig;

    // cleanup  old session
    Timer cleanupTimer;
    int cleanupInterval;
    bool cleanupRunning;


    bool writeExceptionsToEventLog = false;

    SessionStateItemExpireCallback expireCallback = null;
    bool enableExpireCallback = false;


    /// <summary>
    /// Indicates whether if expire callback is on or off.
    /// </summary>
    public bool EnableExpireCallback
    {
      get { return enableExpireCallback; }
      set { enableExpireCallback = value; }
    }

    /// <summary>
    /// Indicates whether to write exceptions to event log.
    /// </summary>
    public bool WriteExceptionsToEventLog
    {
      get { return writeExceptionsToEventLog; }
      set { writeExceptionsToEventLog = value; }
    }

    /// <summary>
    /// The name of the ASP .NET application.
    /// </summary>
    public string ApplicationName
    {
      get { return app.Name; }
      set { app.Name = value; }
    }

    private long ApplicationId
    {
      get { return app.Id; }
    }


    /// <summary>
    /// Handles a MySql type exception.
    /// </summary>
    /// <param name="e">exception</param>
    /// <param name="action"> name of the function that throwed the exception</param>
    /// <remarks>If <see cref="WriteExceptionsToEventLog"/> is set it will write exception info to event log.
    /// </remarks>
    /// <exception cref="ProviderException"><see cref="WriteExceptionsToEventLog"/> is <c>false</c>.</exception>
    private void HandleMySqlException(MySqlException e, string action)
    {
      if (WriteExceptionsToEventLog)
      {
        using (EventLog log = new EventLog())
        {
          log.Source = eventSource;
          log.Log = eventLog;

          string message = "An exception occurred communicating with the data source.\n\n";
          message += "Action: " + action;
          message += "Exception: " + e.ToString();
          log.WriteEntry(message);
        }
      }
      throw new ProviderException(exceptionMessage, e);
    }



    /// <summary>
    /// Initializes the provider with the property values specified in the ASP .NET application configuration file.
    /// </summary>
    /// <param name="name">The name of the provider instance to initialize.</param>
    /// <param name="config">Object that contains the names and values of configuration options for the provider.
    /// </param>
    public override void Initialize(string name, NameValueCollection config)
    {
      //Initialize values from web.config.
      if (config == null)
        throw new ArgumentException("config");
      if (name == null || name.Length == 0)
        throw new ArgumentException("name");
      if (String.IsNullOrEmpty(config["description"]))
      {
        config.Remove("description");
        config["description"] = "MySQL Session State Store Provider";
      }
      base.Initialize(name, config);
      string applicationName = HostingEnvironment.ApplicationVirtualPath;
      if (!String.IsNullOrEmpty(config["applicationName"]))
        applicationName = config["applicationName"];

      // Get <sessionState> configuration element.
      Configuration webConfig = WebConfigurationManager.OpenWebConfiguration(HostingEnvironment.ApplicationVirtualPath);
      sessionStateConfig = (SessionStateSection)webConfig.SectionGroups["system.web"].Sections["sessionState"];

      // Initialize connection.
      connectionString = ConfigUtility.GetConnectionString(config);
      if (string.IsNullOrEmpty(connectionString)) return;

      writeExceptionsToEventLog = false;
      if (config["writeExceptionsToEventLog"] != null)
      {
        writeExceptionsToEventLog = (config["writeExceptionsToEventLog"].ToUpper() == "TRUE");
      }

      enableExpireCallback = false;

      if (config["enableExpireCallback"] != null)
      {
        enableExpireCallback = (config["enableExpireCallback"].ToUpper() == "TRUE");
      }

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
          CheckStorageEngine(conn);
        }
      }
      catch (MySqlException e)
      {
        HandleMySqlException(e, "Initialize");
      }

      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          MySqlCommand cmd = new MySqlCommand(
              "INSERT IGNORE INTO my_aspnet_sessioncleanup SET" +
              " ApplicationId = @ApplicationId, " +
              " LastRun = NOW(), " +
              " IntervalMinutes = 10",
             conn);
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
          conn.Open();
          cmd.ExecuteNonQuery();
          cleanupInterval = GetCleanupInterval(conn, ApplicationId);
        }
      }
      catch (MySqlException e)
      {
        HandleMySqlException(e, "Initialize");      
      }

      // Setup the cleanup timer
      if (cleanupInterval <= 0)
        cleanupInterval = 1;
      cleanupTimer = new Timer(new TimerCallback(CleanupOldSessions), null, 0,
          cleanupInterval * 1000 * 60);
    }

    /// <summary>
    /// Creates a new <see cref="SessionStateStoreData"/> object for the current request.
    /// </summary>
    /// <param name="context">
    /// The HttpContext object for the current request.
    /// </param>
    /// <param name="timeout">
    /// The timeout value (in minutes) for the SessionStateStoreData object that is created.
    /// </param>
    public override SessionStateStoreData CreateNewStoreData(System.Web.HttpContext context, int timeout)
    {

      return new SessionStateStoreData(new SessionStateItemCollection(),
          SessionStateUtility.GetSessionStaticObjects(context), timeout);
    }

    /// <summary>
    /// Adds a new session state item to the database.
    /// </summary>
    /// <param name="context">
    /// The HttpContext object for the current request.
    /// </param>
    /// <param name="id">
    /// The session ID for the current request.
    /// </param>
    /// <param name="timeout">
    /// The timeout value for the current request.
    /// </param>
    public override void CreateUninitializedItem(System.Web.HttpContext context, string id, int timeout)
    {
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          MySqlCommand cmd = new MySqlCommand(
  @"INSERT INTO my_aspnet_sessions
    (SessionId, ApplicationId, Created, Expires, LockDate,
    LockId, Timeout, Locked, SessionItems, Flags)
    Values (@SessionId, @ApplicationId, NOW(), NOW() + INTERVAL @Timeout MINUTE, NOW(),
    @LockId , @Timeout, @Locked, @SessionItems, @Flags) 
  on duplicate key update Created = values( Created ), Expires = values( Expires ),
    LockDate = values( LockDate ), LockId = values( LockId ), Timeout = values( Timeout) ,
    Locked = values( Locked ), SessionItems = values( SessionItems ), Flags = values( Flags )",
             conn);

          cmd.Parameters.AddWithValue("@SessionId", id);
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
          cmd.Parameters.AddWithValue("@LockId", 0);
          cmd.Parameters.AddWithValue("@Timeout", timeout);
          cmd.Parameters.AddWithValue("@Locked", 0);
          cmd.Parameters.AddWithValue("@SessionItems", null);
          cmd.Parameters.AddWithValue("@Flags", 1);
          conn.Open();
          cmd.ExecuteNonQuery();
        }
      }
      catch (MySqlException e)
      {
        HandleMySqlException(e, "CreateUninitializedItem");      
      }
    }


    /// <summary>
    /// Releases all the resources for this instance.
    /// </summary>
    public override void Dispose()
    {
      if (cleanupTimer != null)
        cleanupTimer.Dispose();
    }

    /// <summary>
    /// Allows the <see cref="MySqlSessionStateStore"/> object to perform any cleanup that may be
    /// required for the current request.
    /// </summary>
    /// <param name="context">The HttpContext object for the current request.</param>
    public override void EndRequest(System.Web.HttpContext context)
    {
    }

    /// <summary>
    /// Returns a read-only session item from the database.
    /// </summary>
    public override SessionStateStoreData GetItem(System.Web.HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
    {
      return GetSessionStoreItem(false, context, id, out locked, out lockAge, out lockId, out actions);
    }

    /// <summary>
    /// Locks a session item and returns it from the database.
    /// </summary>
    /// <param name="context">The HttpContext object for the current request.</param>
    /// <param name="id">The session ID for the current request.</param>
    /// <param name="locked">
    /// <c>true</c> if the session item is locked in the database; otherwise, <c>false</c>.
    /// </param>
    /// <param name="lockAge">
    /// TimeSpan object that indicates the amount of time the session item has been locked in the database.
    /// </param>
    /// <param name="lockId">
    /// A lock identifier object.
    /// </param>
    /// <param name="actions">
    /// A <see cref="SessionStateActions"/> enumeration value that indicates whether or
    /// not the session is uninitialized and cookieless.
    /// </param>
    /// <returns></returns>
    public override SessionStateStoreData GetItemExclusive(System.Web.HttpContext context, string id,
        out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
    {
      return GetSessionStoreItem(true, context, id, out locked, out lockAge, out lockId, out actions);
    }

    /// <summary>
    /// Performs any per-request initializations that the MySqlSessionStateStore provider requires.
    /// </summary>
    public override void InitializeRequest(System.Web.HttpContext context)
    {
    }

    /// <summary>
    /// Forcibly releases the lock on a session item in the database if multiple attempts to
    /// retrieve the session item fail.
    /// </summary>
    /// <param name="context">The HttpContext object for the current request.</param>
    /// <param name="id">The session ID for the current request.</param>
    /// <param name="lockId">The lock identifier for the current request.</param>
    public override void ReleaseItemExclusive(System.Web.HttpContext context, string id, object lockId)
    {
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          MySqlCommand cmd = new MySqlCommand(
              "UPDATE my_aspnet_sessions SET Locked = 0, Expires = NOW() + INTERVAL @Timeout MINUTE " +
              "WHERE SessionId = @SessionId AND ApplicationId = @ApplicationId AND LockId = @LockId",
              conn);

          cmd.Parameters.AddWithValue("@Timeout", sessionStateConfig.Timeout.TotalMinutes);
          cmd.Parameters.AddWithValue("@SessionId", id);
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
          cmd.Parameters.AddWithValue("@LockId", lockId);
          conn.Open();
          cmd.ExecuteNonQuery();
        }
      }
      catch(MySqlException e)
      {
        HandleMySqlException(e, "ReleaseItemExclusive");      
      }
    }

    /// <summary>
    /// Removes the specified session item from the database
    /// </summary>
    /// <param name="context">The HttpContext object for the current request.</param>
    /// <param name="id">The session ID for the current request.</param>
    /// <param name="lockId">The lock identifier for the current request.</param>
    /// <param name="item">The session item to remove from the database.</param>
    public override void RemoveItem(System.Web.HttpContext context, string id, object lockId, SessionStateStoreData item)
    {
      bool sessionDeleted;
      
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          MySqlCommand cmd = new MySqlCommand("DELETE FROM my_aspnet_sessions " +
              " WHERE SessionId = @SessionId AND ApplicationId = @ApplicationId AND LockId = @LockId",
              conn);

          cmd.Parameters.AddWithValue("@SessionId", id);
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
          cmd.Parameters.AddWithValue("@LockId", lockId);
          conn.Open();      
          sessionDeleted = cmd.ExecuteNonQuery() > 0;          
        }
        if (sessionDeleted && this.enableExpireCallback)
        {
          this.expireCallback.Invoke(id, item);
        }
      }
      catch (MySqlException ex)
      {
        HandleMySqlException(ex, "RemoveItem Error: " + ex.Message);          
      }      
    }


    /// <summary>
    /// Resets the expiration date and timeout for a session item in the database.
    /// </summary>
    /// <param name="context">The HttpContext object for the current request.</param>
    /// <param name="id">The session ID for the current request.</param>
    public override void ResetItemTimeout(System.Web.HttpContext context, string id)
    {     
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          MySqlCommand cmd = new MySqlCommand(
              "UPDATE my_aspnet_sessions SET Expires = NOW() + INTERVAL @Timeout MINUTE" +
             " WHERE SessionId = @SessionId AND ApplicationId = @ApplicationId", conn);

          cmd.Parameters.AddWithValue("@Timeout", sessionStateConfig.Timeout.TotalMinutes);
          cmd.Parameters.AddWithValue("@SessionId", id);
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
          conn.Open();
          cmd.ExecuteNonQuery();
        }
      }
      catch (MySqlException e)
      {        
        HandleMySqlException(e, "ResetItemTimeout");
      }      
    }

    /// <summary>
    /// Updates the session time information in the database with the specified session item,
    /// and releases the lock.
    /// </summary>
    /// <param name="context">The HttpContext object for the current request.</param>
    /// <param name="id">The session ID for the current request.</param>
    /// <param name="item">The session item containing new values to update the session item in the database with.
    /// </param>
    /// <param name="lockId">The lock identifier for the current request.</param>
    /// <param name="newItem">A Boolean value that indicates whether or not the session item is new in the database.
    /// A false value indicates an existing item.
    /// </param>
    public override void SetAndReleaseItemExclusive(System.Web.HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
    {            
      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          // Serialize the SessionStateItemCollection as a byte array
          byte[] sessItems = Serialize((SessionStateItemCollection)item.Items);
          MySqlCommand cmd;
          if (newItem)
          {
            //Insert the new session item . If there was expired session
            //with the same SessionId and Application id, it will be removed

            cmd = new MySqlCommand(
  @"INSERT INTO my_aspnet_sessions
    (SessionId, ApplicationId, Created, Expires, LockDate,
    LockId, Timeout, Locked, SessionItems, Flags)
    Values (@SessionId, @ApplicationId, NOW(), NOW() + INTERVAL @Timeout MINUTE, NOW(),
    @LockId , @Timeout, @Locked, @SessionItems, @Flags) 
  on duplicate key update Created = values( Created ), Expires = values( Expires ),
    LockDate = values( LockDate ), LockId = values( LockId ), Timeout = values( Timeout) ,
    Locked = values( Locked ), SessionItems = values( SessionItems ), Flags = values( Flags )", conn);
            cmd.Parameters.AddWithValue("@SessionId", id);
            cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
            cmd.Parameters.AddWithValue("@Timeout", item.Timeout);
            cmd.Parameters.AddWithValue("@LockId", 0);
            cmd.Parameters.AddWithValue("@Locked", 0);
            cmd.Parameters.AddWithValue("@SessionItems", sessItems);
            cmd.Parameters.AddWithValue("@Flags", 0);
          }
          else
          {
            //Update the existing session item.
            cmd = new MySqlCommand(
                 "UPDATE my_aspnet_sessions SET Expires = NOW() + INTERVAL @Timeout MINUTE," +
                 " SessionItems = @SessionItems, Locked = @Locked " +
                 " WHERE SessionId = @SessionId AND ApplicationId = @ApplicationId AND LockId = @LockId",
                 conn);

            cmd.Parameters.AddWithValue("@Timeout", item.Timeout);
            cmd.Parameters.AddWithValue("@SessionItems", sessItems);
            cmd.Parameters.AddWithValue("@Locked", 0);
            cmd.Parameters.AddWithValue("@SessionId", id);
            cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
            cmd.Parameters.AddWithValue("@LockId", lockId);
          }
          conn.Open();          
          cmd.ExecuteNonQuery();          
        }
      }
      catch (MySqlException e)
      {       
        HandleMySqlException(e, "SetAndReleaseItemExclusive");
      }      
    }


    /// <summary>
    ///  GetSessionStoreItem is called by both the GetItem and GetItemExclusive methods. GetSessionStoreItem 
    ///  retrieves the session data from the data source. If the lockRecord parameter is true (in the case of 
    ///  GetItemExclusive), then GetSessionStoreItem locks the record and sets a New LockId and LockDate.
    /// </summary>
    private SessionStateStoreData GetSessionStoreItem(bool lockRecord,
           HttpContext context,
           string id,
           out bool locked,
           out TimeSpan lockAge,
           out object lockId,
           out SessionStateActions actionFlags)
    {

      // Initial values for return value and out parameters.
      SessionStateStoreData item = null;
      lockAge = TimeSpan.Zero;
      lockId = null;
      locked = false;
      actionFlags = SessionStateActions.None;

      // MySqlCommand for database commands.
      MySqlCommand cmd = null;
      // serialized SessionStateItemCollection.
      byte[] serializedItems = null;
      // True if a record is found in the database.
      bool foundRecord = false;
      // True if the returned session item is expired and needs to be deleted.
      bool deleteData = false;
      // Timeout value from the data store.
      int timeout = 0;

      try
      {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
          conn.Open();
          // lockRecord is True when called from GetItemExclusive and
          // False when called from GetItem.
          // Obtain a lock if possible. Ignore the record if it is expired.
          if (lockRecord)
          {
            cmd = new MySqlCommand(
              "UPDATE my_aspnet_sessions SET " +
              " Locked = 1, LockDate = NOW()" +
              " WHERE SessionId = @SessionId AND ApplicationId = @ApplicationId AND" +
              " Locked = 0 AND Expires > NOW()", conn);

            cmd.Parameters.AddWithValue("@SessionId", id);
            cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);

            if (cmd.ExecuteNonQuery() == 0)
            {
              // No record was updated because the record was locked or not found.
              locked = true;
            }
            else
            {
              // The record was updated.
              locked = false;
            }
          }

          // Retrieve the current session item information.
          cmd = new MySqlCommand(
            "SELECT NOW(), Expires , SessionItems, LockId,  Flags, Timeout, " +
      "  LockDate, Locked " +
            "  FROM my_aspnet_sessions" +
            "  WHERE SessionId = @SessionId AND ApplicationId = @ApplicationId", conn);

          cmd.Parameters.AddWithValue("@SessionId", id);
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);

          // Retrieve session item data from the data source.
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            if (reader.Read())
            {
              DateTime now = reader.GetDateTime(0);
              DateTime expires = reader.GetDateTime(1);
              if (now.CompareTo(expires) > 0)
              {
                //The record was expired. Mark it as not locked.
                locked = false;
                // The session was expired. Mark the data for deletion.
                deleteData = true;
              }
              else
              {
                foundRecord = true;
              }

              object items = reader.GetValue(2);
              serializedItems = (items is DBNull) ? null : (byte[])items;
              lockId = reader.GetValue(3);
              if (lockId is DBNull)
                lockId = (int)0;

              actionFlags = (SessionStateActions)(reader.GetInt32(4));
              timeout = reader.GetInt32(5);
              DateTime lockDate = reader.GetDateTime(6);
              lockAge = now.Subtract(lockDate);
              // If it's a read-only session set locked to the current lock
              // status (writable sessions have already done this)
              if (!lockRecord)
                locked = reader.GetBoolean(7);
            }
          }

          // The record was not found. Ensure that locked is false.
          if (!foundRecord)
            locked = false;

          // If the record was found and you obtained a lock, then set 
          // the lockId, clear the actionFlags,
          // and create the SessionStateStoreItem to return.
          if (foundRecord && !locked)
          {
            lockId = (int)(lockId) + 1;

            cmd = new MySqlCommand("UPDATE my_aspnet_sessions SET" +
              " LockId = @LockId, Flags = 0 " +
              " WHERE SessionId = @SessionId AND ApplicationId = @ApplicationId", conn);

            cmd.Parameters.AddWithValue("@LockId", lockId);
            cmd.Parameters.AddWithValue("@SessionId", id);
            cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
            cmd.ExecuteNonQuery();


            // If the actionFlags parameter is not InitializeItem, 
            // deserialize the stored SessionStateItemCollection.
            if (actionFlags == SessionStateActions.InitializeItem)
            {
              item = CreateNewStoreData(context, (int)sessionStateConfig.Timeout.TotalMinutes);
            }
            else
            {
              item = Deserialize(context, serializedItems, timeout);
            }
          }
        }
      }
      catch(MySqlException e)
      {
        HandleMySqlException(e, "GetSessionStoreItem");
      }
      return item;
    }

    /// <summary>
    /// Sets the reference for the ExpireCallback delegate if setting is enabled.
    /// </summary>
    /// <param name="expireCallback"></param>
    /// <returns><c>true</c> if <see cref="enableExpireCallback"/> is <c>true</c>; otherwise, <c>false</c>.</returns>
    public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
    {
      if (this.enableExpireCallback)
      {
        this.expireCallback = expireCallback;
        return true;
      }
      return false;
    }

    ///<summary>
    /// Serialize is called by the SetAndReleaseItemExclusive method to 
    /// convert the SessionStateItemCollection into a byte array to
    /// be stored in the blob field.
    /// </summary>
    private byte[] Serialize(SessionStateItemCollection items)
    {
      MemoryStream ms = new MemoryStream();
      BinaryWriter writer = new BinaryWriter(ms);
      if (items != null)
      {
        items.Serialize(writer);
      }
      writer.Close();
      return ms.ToArray();
    }

    ///<summary>
    /// Deserialize is called by the GetSessionStoreItem method to 
    /// convert the byte array stored in the blob field to a 
    /// SessionStateItemCollection.
    /// </summary>
    private SessionStateStoreData Deserialize(HttpContext context,
      byte[] serializedItems, int timeout)
    {

      SessionStateItemCollection sessionItems = new SessionStateItemCollection();

      if (serializedItems != null)
      {
        MemoryStream ms = new MemoryStream(serializedItems);
        if (ms.Length > 0)
        {
          BinaryReader reader = new BinaryReader(ms);
          sessionItems = SessionStateItemCollection.Deserialize(reader);
        }
      }

      return new SessionStateStoreData(sessionItems, SessionStateUtility.GetSessionStaticObjects(context),
          timeout);
    }

    
    private SessionStateItemCollection DeserializeSessionItems(byte[] serializedItems)
    {
      SessionStateItemCollection sessionItems = new SessionStateItemCollection();
      if (serializedItems != null)
      {
        MemoryStream ms = new MemoryStream(serializedItems);
        if (ms.Length > 0)
        {
          BinaryReader reader = new BinaryReader(ms);
          sessionItems = SessionStateItemCollection.Deserialize(reader);
        }
      }
      return sessionItems;
    }


    private void CleanupOldSessions(object o)
    {      
     
      lock(this)
      {
        if (cleanupRunning)
          return;

        cleanupRunning = true;
      }    
      try
      {
        using (MySqlConnection con = new MySqlConnection(connectionString))
        {
          con.Open();          
          MySqlCommand cmd = new MySqlCommand(
              "UPDATE my_aspnet_sessioncleanup SET LastRun=NOW() WHERE" +
              " LastRun + INTERVAL IntervalMinutes MINUTE < NOW() AND ApplicationId = @ApplicationId", con);
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
          int updatedSessions = cmd.ExecuteNonQuery();          
          if (updatedSessions > 0)                     
            DeleteTimedOutSessions();          
        }
      }
      catch (MySqlException e)
      {
        HandleMySqlException(e, "CleanupOldSessions");        
      }
      finally
      {
        lock (this)
        {
          cleanupRunning = false;
        }        
      }
    }

    int GetCleanupInterval(MySqlConnection con, long ApplicationId)
    {
      MySqlCommand cmd = new MySqlCommand("SELECT IntervalMinutes from my_aspnet_sessioncleanup where ApplicationId = @ApplicationId", con);
      cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
      return (int)cmd.ExecuteScalar();
    }

    /// <summary>
    /// Checks storage engine used by my_aspnet_sessions.
    /// </summary>
    /// <param name="con">The connection object used to check the storage engine.</param>
    /// <remarks>Warn if MyISAM is used - it does not handle concurrent updates well
    /// which is important for session provider, as each access to session
    /// does an update to "expires" field.</remarks>
    private void CheckStorageEngine(MySqlConnection con)
    {

      try
      {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT ENGINE FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='my_aspnet_sessions'",
            con);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          if (reader.Read())
          {
            string engine = reader.GetString(0);
            if (engine == "MyISAM")
            {
              string message =
                  "Storage engine for table my_aspnet_sessions is MyISAM." +
                  "If possible, please change it to a transactional storage engine " +
                   "to improve performance,e.g with 'alter table my_aspnet_sessions engine innodb'\n";
              try
              {
                using (EventLog log = new EventLog())
                {
                  log.Source = eventSource;
                  log.Log = eventLog;
                  log.WriteEntry(message);
                }
              }
              catch (SecurityException)
              {
                // Can't write to event log due to security restrictions
                Trace.WriteLine(message);
              }
            }
          }
        }
      }
      catch (MySqlException e)
      {
        Trace.Write("got exception while checking for engine" + e);
      }
    }

    private void DeleteTimedOutSessions()
    {
      if (this.enableExpireCallback)
      {
        DeleteTimedOutSessionsWithCallback();
      }
      else
      {
        DeleteTimedOutSessionsWithoutCallback();
      }
    }

    private void DeleteTimedOutSessionsWithoutCallback()
    {     
      try
      {
        using (MySqlConnection con = new MySqlConnection(connectionString))
        {
          con.Open();     
          MySqlCommand cmd = new MySqlCommand("DELETE FROM my_aspnet_sessions WHERE Expires < NOW() AND ApplicationId = @ApplicationId", con);          
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);
          cmd.ExecuteNonQuery();          
        }
      }
      catch (Exception e)
      {        
        Trace.Write("Got exception in Delete Timed Out Sessions With Out Callback " + e);
        throw;
      }      
    }

    private void DeleteTimedOutSessionsWithCallback()
    {
      using (MySqlConnection con = new MySqlConnection(connectionString))
        {
          con.Open();
          MySqlCommand cmd = new MySqlCommand("SELECT SessionID, SessionItems FROM my_aspnet_sessions WHERE Expires < NOW() AND ApplicationId = @ApplicationId", con);          
          cmd.Parameters.AddWithValue("@ApplicationId", ApplicationId);

          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string sid = reader.GetString(0);
              object items = reader.GetValue(1);
              byte[] rawSessionItems = (items is DBNull) ? null : (byte[])items;              

              SessionStateItemCollection sessionItems = this.DeserializeSessionItems(rawSessionItems);
              SessionStateStoreData ssd = new SessionStateStoreData(sessionItems, new HttpStaticObjectsCollection(), 0);
              
              try
              {
                if (this.expireCallback != null)  this.expireCallback.Invoke(sid, ssd);

                using (MySqlConnection con2 = new MySqlConnection(connectionString))
                {                 
                  MySqlCommand cmd2 = new MySqlCommand("DELETE FROM my_aspnet_sessions" +
                      " WHERE SessionId = @SessionId" +
                      " AND ApplicationId = @ApplicationId", con2);
                  cmd2.Parameters.AddWithValue("@SessionId", sid);
                  cmd2.Parameters.AddWithValue("@ApplicationId", ApplicationId);
                  con2.Open();      
                  cmd2.ExecuteNonQuery();                  
                }
               
              }
              catch(Exception e)
              {                                
                Trace.Write("Got exception in Delete Timed Out Sessions With Callback " + e);
                throw;
              }                            
            }
          }
       }     
    }
  }
}