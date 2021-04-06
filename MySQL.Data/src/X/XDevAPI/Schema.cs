// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using MySql.Data;
using System.Linq;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a schema or database.
  /// </summary>
  public class Schema : DatabaseObject
  {
    internal Schema(BaseSession session, string name) : base(null, name)
    {
      Schema = this;
      Session = session;
    }

    /// <summary>
    /// Session related to current schema.
    /// </summary>
    public new BaseSession Session { get; private set; }


    #region Browse Functions

    /// <summary>
    /// Returns a list of all collections in this schema.
    /// </summary>
    /// <returns>A <see cref="Collection"/> list representing all found collections.</returns>
    public List<Collection> GetCollections()
    {
      List<Collection> result = null;
      try
      {
        ValidateOpenSession();
        result = Session.XSession.GetObjectList<Collection>(this, "COLLECTION");
      }
      catch (MySqlException ex)
      {
        switch (ex.Number)
        {
          case (int)CloseNotification.IDLE:
          case (int)CloseNotification.KILLED:
          case (int)CloseNotification.SHUTDOWN:
            XDevAPI.Session.ThrowSessionClosedByServerException(ex, Session);
            break;
          default:
            throw;
        }
      }
      return result;
    }

    /// <summary>
    /// Returns a list of all tables in this schema.
    /// </summary>
    /// <returns>A <see cref="Table"/> list representing all found tables.</returns>
    public List<Table> GetTables()
    {
      List<Table> result = null;
      try
      {
        ValidateOpenSession();
        result = Session.XSession.GetObjectList<Table>(this, "TABLE", "VIEW");
      }
      catch (MySqlException ex)
      {
        switch (ex.Number)
        {
          case (int)CloseNotification.IDLE:
          case (int)CloseNotification.KILLED:
          case (int)CloseNotification.SHUTDOWN:
            XDevAPI.Session.ThrowSessionClosedByServerException(ex, Session);
            break;
          default:
            throw;
        }
      }
      return result;
    }

    #endregion

    #region Instance Functions

    /// <summary>
    /// Gets a collection by name.
    /// </summary>
    /// <param name="name">The name of the collection to get.</param>
    /// <param name="ValidateExistence">Ensures the collection exists in the schema.</param>
    /// <returns>A <see cref="Collection"/> object matching the given name.</returns>
    public Collection GetCollection(string name, bool ValidateExistence = false)
    {
      Collection c = new Collection<DbDoc>(this, name);
      if (ValidateExistence)
      {
        ValidateOpenSession();
        if (!c.ExistsInDatabase())
          throw new MySqlException(String.Format("Collection '{0}' does not exist.", name));
      }
      return c;
    }

    /// <summary>
    /// Gets a typed collection object. This is useful for using domain objects.
    /// </summary>
    /// <typeparam name="T">The type of collection returned.</typeparam>
    /// <param name="name">The name of collection to get.</param>
    /// <returns>A generic <see cref="Collection"/> object set with the given name.</returns>
    public Collection<T> GetCollection<T>(string name)
    {
      return new Collection<T>(this, name);
    }

    /// <summary>
    /// Gets the given collection as a table.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <returns>A <see cref="Table"/> object set with the given name.</returns>
    public Table GetCollectionAsTable(string name)
    {
      return GetTable(name);
    }

    /// <summary>
    /// Gets a table object. Upon return the object may or may not be valid.
    /// </summary>
    /// <param name="name">The name of the table object.</param>
    /// <returns>A <see cref="Table"/> object set with the given name.</returns>
    public Table GetTable(string name)
    {
      return new Table(this, name);
    }

    #endregion

    #region Create Functions

    /// <summary>
    /// Creates a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="ReuseExisting">If false, throws an exception if the collection exists.</param>
    /// <returns>Collection referente.</returns>
    public Collection CreateCollection(string collectionName, bool ReuseExisting = false)
    {
      ValidateOpenSession();
      Collection coll = new Collection(this, collectionName);
      try
      {
        if (Session.Version.isAtLeast(8, 0, 19))
        {
          CreateCollectionOptions options = new CreateCollectionOptions() { ReuseExisting = ReuseExisting };
          Session.XSession.CreateCollection(Name, collectionName, options);
        }
        else
        {
          Session.XSession.CreateCollection(Name, collectionName);
        }
      }
      catch (MySqlException ex) when (ex.Number == (int)CloseNotification.IDLE || ex.Number == (int)CloseNotification.KILLED || ex.Number == (int)CloseNotification.SHUTDOWN)
      {
        XDevAPI.Session.ThrowSessionClosedByServerException(ex, Session);
      }
      catch (MySqlException ex) when (ex.Code == 1050)
      {
        if (ReuseExisting)
          return coll;
        throw;
      }
      return new Collection(this, collectionName);
    }

    /// <summary>
    /// Creates a collection including a schema validation.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="options">This object hold the parameters required to create the collection.</param>
    /// <see cref="CreateCollectionOptions"/>
    /// <returns>Collection referente.</returns>
    public Collection CreateCollection(string collectionName, CreateCollectionOptions options)
    {
      ValidateOpenSession();
      Collection coll = null;

      try
      {
        coll = new Collection(this, collectionName);
        Session.XSession.CreateCollection(Name, collectionName, options);
      }
      catch (MySqlException ex) when (ex.Number == (int)CloseNotification.IDLE || ex.Number == (int)CloseNotification.KILLED || ex.Number == (int)CloseNotification.SHUTDOWN)
      {
        XDevAPI.Session.ThrowSessionClosedByServerException(ex, Session);
      }
      catch (MySqlException ex_1) when(ex_1.Code==5015)
      {
        var msg = string.Format("{0}{1}{2}", ex_1.Message, ", ", ResourcesX.SchemaCreateCollectionMsg);
        throw new MySqlException(msg);
      }
      catch (MySqlException ex) when (ex.Code == 1050)
      {
        if (options.ReuseExisting)
          return coll;
        throw;
      }
      catch (MySqlException)
      {
        throw;
      }
      return coll;
    }

    /// <summary>
    /// Modify a collection adding or removing schema validation parameters.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="options">This object encapsulate the Validation parameters level and schema. </param>
    /// <returns>Collection referente.</returns>
    public Collection ModifyCollection(string collectionName, ModifyCollectionOptions? options)
    {
      ValidateOpenSession();
      Collection result = null;
      try
      {
        Session.XSession.ModifyCollection(Name, collectionName, options);
        result = new Collection(this, collectionName);
      }
      catch (MySqlException ex) when (ex.Number == (int)CloseNotification.IDLE || ex.Number == (int)CloseNotification.KILLED || ex.Number == (int)CloseNotification.SHUTDOWN)
      {
        XDevAPI.Session.ThrowSessionClosedByServerException(ex, Session);
      }
      catch (MySqlException ex_1) when (ex_1.Code == 5157)
      {
        var msg = string.Format("{0}{1}{2}", ex_1.Message, ", ", ResourcesX.SchemaCreateCollectionMsg);
        throw new MySqlException(msg);
      }
      catch (MySqlException)
      {
        throw;
      }
      return result;
    }

    #endregion

    /// <summary>
    /// Drops the given collection.
    /// </summary>
    /// <param name="name">The name of the collection to drop.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public void DropCollection(string name)
    {
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
      try
      {
        ValidateOpenSession();
        Collection c = GetCollection(name);
        if (!c.ExistsInDatabase()) return;
        Session.XSession.DropCollection(Name, name);
      }
      catch (MySqlException ex)
      {
        switch (ex.Number)
        {
          case (int)CloseNotification.IDLE:
          case (int)CloseNotification.KILLED:
          case (int)CloseNotification.SHUTDOWN:
            XDevAPI.Session.ThrowSessionClosedByServerException(ex, Session);
            break;
          default:
            throw;
        }
      }
    }

    #region Base Class

    /// <summary>
    /// Determines if this schema actually exists.
    /// </summary>
    /// <returns>True if exists, false otherwise.</returns>
    public override bool ExistsInDatabase()
    {
      bool result = false;
      try
      {
        ValidateOpenSession();
        string sql = String.Format("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name like '{0}'", Name);
        long count = (long)Session.InternalSession.ExecuteQueryAsScalar(sql);
        result = count > 0;
      }
      catch (MySqlException ex)
      {
        switch (ex.Number)
        {
          case (int)CloseNotification.IDLE:
          case (int)CloseNotification.KILLED:
          case (int)CloseNotification.SHUTDOWN:
            XDevAPI.Session.ThrowSessionClosedByServerException(ex, Session);
            break;
          default:
            throw;
        }
      }
      return result;
    }

    #endregion
  }
}
