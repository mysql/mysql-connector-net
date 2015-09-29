// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Properties;
using MySql.XDevAPI.CRUD;

namespace MySql.XDevAPI
{
  /// <summary>
  /// Represents a collection of documents
  /// </summary>
  public class Collection : DatabaseObject
  {
    internal Collection(Schema schema, string name)
      : base(schema, name)
    {

    }

    #region Add Operations

    /// <summary>
    /// Add one ore more objects to the collection.  This method can take anonymous objects, 
    /// domain objects, or just plain JSON strings.
    /// </summary>
    /// <param name="items">The objects to insert</param>
    /// <returns>AddStatement</returns>
    public AddStatement Add(params object[] items)
    {
      AddStatement stmt = new AddStatement(this);
      stmt.Add(items);
      return stmt;
    }

    /// <summary>
    /// Add one or more objects to the collection.  This method takes strings that are intended
    /// to be JSON objects.
    /// </summary>
    /// <param name="json">JSON strings to insert</param>
    /// <returns>AddStatement</returns>
    public AddStatement Add(params string[] json)
    {
      AddStatement stmt = new AddStatement(this);
      stmt.Add(json);
      return stmt;
    }

    #endregion

    #region Remove Operations

    /// <summary>
    /// Creates a remove statement with the given condition.  The RemoveStatement
    /// can then be further modified before execution.  This method is intended to remove
    /// one or more documents from a collection.
    /// </summary>
    /// <param name="condition">The condition to match documents</param>
    /// <returns>RemoveStatement</returns>
    public RemoveStatement Remove(string condition)
    {
      RemoveStatement stmt = new RemoveStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Remove a single document from this collectoin.  The id can really be of any type.
    /// </summary>
    /// <param name="id">The id to match the document</param>
    /// <returns>RemoveStatement</returns>
    public RemoveStatement Remove(object id)
    {
      string key = id is string ?
        "\"" + id.ToString() + "\"" : id.ToString();
      string condition = String.Format("_id = {0}", key);
      RemoveStatement stmt = new RemoveStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Remove a single document from this collection.
    /// </summary>
    /// <param name="doc">The DbDoc representing the document to remove</param>
    /// <returns>RemoveStatement</returns>
    public RemoveStatement Remove(DbDoc doc)
    {
      if (!doc.HasId)
        throw new InvalidOperationException(Resources.RemovingRequiresId);
      return Remove(doc.Id);
    }

    #endregion

    #region Modify Operations

    /// <summary>
    /// Modify a single document from this collection.
    /// </summary>
    /// <param name="condition">The condition to match documents</param>
    /// <returns>ModifyStatement object</returns>
    public ModifyStatement Modify(string condition)
    {
      ModifyStatement stmt = new ModifyStatement(this, condition);
      return stmt;
    }

    #endregion

    /// <summary>
    /// Drop the current collection in server
    /// </summary>
    public void Drop()
    {
      Session.XSession.DropCollection(Schema.Name, Name);
    }

    /// <summary>
    /// Returns the number of documents in this collection on the server.
    /// </summary>
    /// <returns>Number of documents</returns>
    public long Count()
    {
      return Session.XSession.TableCount(Schema, Name);
    }

    /// <summary>
    /// Finds documents in current collection
    /// </summary>
    /// <param name="condition">Optional condition to match documents</param>
    /// <returns>FindStatement object</returns>
    public FindStatement Find(string condition = null)
    {
      FindStatement stmt = new FindStatement(this, condition);
      return stmt;
    }


    public Collection As(string alias)
    {
      throw new NotImplementedException();
    }

    public Collection On(string condition)
    {
      throw new NotImplementedException();
    }

    public Collection Join(Collection collection)
    {
      throw new NotImplementedException();
    }

    public Collection Fields(params string[] fields)
    {
      throw new NotImplementedException();
    }

    public Collection Join(Collection collection, string condition)
    {
      throw new NotImplementedException();
    }

    public void OnCompleted(Action completedMethod)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Verifies if current collection exists in server schema
    /// </summary>
    /// <returns></returns>
    public override bool ExistsInDatabase()
    {
      return Session.XSession.TableExists(Schema, Name);
    }
  }
}
