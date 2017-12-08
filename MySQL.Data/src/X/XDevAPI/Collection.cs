// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI.CRUD;
using MySqlX.XDevAPI.Common;
using MySqlX;
using MySql.Data;
using MySql.Data.MySqlClient;
#if NETSTANDARD1_6
using System.Reflection;
#endif

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a collection of documents.
  /// </summary>
  public class Collection : DatabaseObject
  {
    internal Collection() :base(null, null)
    {
    }

    internal Collection(Schema schema, string name)
      : base(schema, name)
    {

    }

#region Add Operations

    /// <summary>
    /// Creates an <see cref="AddStatement"/> containing the provided objects that can be used to add
    /// one or more items to a collection.
    /// </summary>
    /// <param name="items">The objects to add.</param>
    /// <returns>An <see cref="AddStatement"/> object containing the objects to add.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
    /// <remarks>This method can take anonymous objects, domain objects, or just plain JSON strings.
    /// The statement can be further modified before execution.</remarks>
    public AddStatement Add(params object[] items)
    {
      if (items == null)
        throw new ArgumentNullException();

      AddStatement stmt = new AddStatement(this);
      stmt.Add(items);
      return stmt;
    }

#endregion

#region Remove Operations

    /// <summary>
    /// Creates a <see cref="RemoveStatement"/> with the given condition that can be used to remove
    /// one or more documents from a collection.The statement can then be further modified before execution.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="RemoveStatement"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c> or white space.</exception>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public RemoveStatement Remove(string condition)
    {
      if (string.IsNullOrWhiteSpace(condition))
        throw new ArgumentNullException(nameof(condition), Resources.ParameterNullOrEmpty);

      RemoveStatement stmt = new RemoveStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Creates a <see cref="RemoveStatement"/> with the given identifier that can be used to remove a single
    /// document from a collection.
    /// </summary>
    /// <param name="id">The identifier to match the document.</param>
    /// <returns>A <see cref="RemoveStatement"/> object set with the given identifier.</returns>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public RemoveStatement Remove(object id)
    {
      string key = id is string ?
        "\"" + id.ToString() + "\"" : id.ToString();
      string condition = String.Format("_id = {0}", key);
      RemoveStatement stmt = new RemoveStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Creates a <see cref="RemoveStatement"/> containing the identifier of the provided document that can
    /// be used to remove a single document from a collection.
    /// </summary>
    /// <param name="doc">The <see cref="DbDoc"/> representing the document to remove.</param>
    /// <returns>A <see cref="RemoveStatement"/> object set with the given document's identifier.</returns>
    /// <exception cref="InvalidOperationException">No identifier for the document was provided.</exception>
    /// <remarks>The remove statement can then be further modified before execution.</remarks>
    public RemoveStatement Remove(DbDoc doc)
    {
      if (!doc.HasId)
        throw new InvalidOperationException(ResourcesX.RemovingRequiresId);
      return Remove(doc.Id);
    }

    /// <summary>
    /// Removes the document with the given identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document to replace.</param>
    /// <returns>A <see cref="Result"/> object containing the results of the execution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c> or white space.</exception>
    /// <remarks>This is a direct execution method.</remarks>
    public Result RemoveOne(object id)
    {
      if (id == null)
        throw new ArgumentNullException(nameof(id));
      string stringId = id.ToString();
      if (string.IsNullOrWhiteSpace(stringId))
        throw new ArgumentNullException(nameof(id), Resources.ParameterNullOrEmpty);

      return Remove("_id = :id").Bind("id", id is string ? "\"" + stringId + "\"" : stringId).Execute();
    }

#endregion

#region Modify Operations

    /// <summary>
    /// Creates a <see cref="ModifyStatement"/> with the given condition that can be used to modify one or more
    /// documents from a collection.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="ModifyStatement"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c> or white space.</exception>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public ModifyStatement Modify(string condition)
    {
      if (string.IsNullOrWhiteSpace(condition))
        throw new ArgumentNullException(nameof(condition), Resources.ParameterNullOrEmpty);

      ModifyStatement stmt = new ModifyStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Replaces the document matching the given identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document to replace.</param>
    /// <param name="doc">The document to replace the matching document.</param>
    /// <returns>A <see cref="Result"/> object containing the results of the execution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c> or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="doc"/> is <c>null</c>.</exception>
    /// <remarks>This is a direct execution method. Operation succeeds even if no matching document was found;
    /// in which case, the Result.RecordsAffected property is zero. If the new document contains an identifier, the value
    /// is ignored.</remarks>
    public Result ReplaceOne(object id, object doc)
    {
      if (id == null)
        throw new ArgumentNullException(nameof(id));
      string stringId = id.ToString();
      if (string.IsNullOrWhiteSpace(stringId))
        throw new ArgumentNullException(nameof(id), Resources.ParameterNullOrEmpty);
      if (doc == null)
        throw new ArgumentNullException(nameof(doc));

      DbDoc currentDocument = GetOne(id);
      var modify = Modify("_id = :id").Bind("id", stringId);
      //if (currentDocument == null) return modify.Execute();
      DbDoc newDocument = doc is DbDoc ? doc as DbDoc : new DbDoc(doc);

      if (currentDocument != null)
      {
        // Unset all properties
        foreach (var dictionary in currentDocument.values)
          if (dictionary.Key != "_id") modify.Unset(dictionary.Key);
      }

      // Set new properties
      foreach (var dictionary in newDocument.values)
        if (dictionary.Key != "_id") modify.Set(dictionary.Key, dictionary.Value);

      return modify.Execute();
    }
#endregion

#region Add-Modify Operations

    /// <summary>
    /// Adds the given document to the collection unless the identifier or any other field that has a unique index
    /// already exists, in which case it will update the matching document.
    /// </summary>
    /// <param name="id">The unique identifier of the document to replace.</param>
    /// <param name="doc">The document to replace the matching document.</param>
    /// <returns>A <see cref="Result"/> object containing the results of the execution.</returns>
    /// <exception cref="MySqlException">The server version is lower than 8.0.3.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c> or white space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="doc"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The <paramref name="id"/> is different from the one in <paramref name="doc"/>.</exception>
    /// <remarks>This is a direct execution method.</remarks>
    public Result AddOrReplaceOne(object id, object doc)
    {
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8,0,3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));
      if (id == null)
        throw new ArgumentNullException(nameof(id));
      string stringId = id.ToString();
      if (string.IsNullOrWhiteSpace(stringId))
        throw new ArgumentNullException(nameof(id), Resources.ParameterNullOrEmpty);
      if (doc == null)
        throw new ArgumentNullException(nameof(doc));

      DbDoc newDocument = doc is DbDoc ? doc as DbDoc : new DbDoc(doc);
      newDocument.Id = id;
      AddStatement stmt = Add(newDocument);
      stmt.upsert = true;
      return stmt.Execute();
    }
#endregion

    /// <summary>
    /// Returns the number of documents in this collection on the server.
    /// </summary>
    /// <returns>The number of documents found.</returns>
    public long Count()
    {
      return Session.XSession.TableCount(Schema, Name);
    }

    /// <summary>
    /// Creates a <see cref="FindStatement"/> with the given condition which can be used to find documents in a
    /// collection.
    /// </summary>
    /// <param name="condition">An optional condition to match documents.</param>
    /// <returns>A <see cref="FindStatement"/> object set with the given condition.</returns>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public FindStatement Find(string condition = null)
    {
      FindStatement stmt = new FindStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Creates a <see cref="CreateCollectionIndexStatement"/> with the given parameters which can be used to create
    /// an index.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <param name="isUnique">True if the index is unique, false otherwise.</param>
    /// <returns>A <see cref="CreateCollectionIndexStatement"/> object set with the given index name and the isUnique flag.</returns>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public CreateCollectionIndexStatement CreateIndex(string indexName, bool isUnique)
    {
      return new CreateCollectionIndexStatement(this, indexName, isUnique);
    }

    /// <summary>
    /// Drops a collection index.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="indexName"/> is <c>null</c> or white space.</exception>
    public void DropIndex(string indexName)
    {
      if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentNullException(nameof(indexName));
      bool indexExists = Convert.ToInt32(Session.XSession.ExecuteQueryAsScalar(
        string.Format("SELECT COUNT(*)>0 FROM information_schema.statistics WHERE table_schema = '{0}' AND table_name = '{1}' AND index_name = '{2}'",
        this.Schema.Name, this.Name, indexName))) == 1;
      if (!indexExists) return;
      Session.XSession.DropCollectionIndex(this.Schema.Name, this.Name, indexName);
    }

    /// <summary>
    /// Verifies if the current collection exists in the server schema.
    /// </summary>
    /// <returns><c>true</c> if the collection exists; otherwise, <c>false</c>.</returns>
    public override bool ExistsInDatabase()
    {
      return Session.XSession.TableExists(Schema, Name);
    }

    /// <summary>
    /// Returns the document with the given identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document to replace.</param>
    /// <returns>A <see cref="DbDoc"/> object if a document matching given identifier exists; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c> or white space.</exception>
    /// <remarks>This is a direct execution method.</remarks>
    public DbDoc GetOne(object id)
    {
      if (id == null)
        throw new ArgumentNullException(nameof(id));
      string stringId = id.ToString();
      if (string.IsNullOrWhiteSpace(stringId))
        throw new ArgumentNullException(nameof(id), Resources.ParameterNullOrEmpty);

      return Find("_id = :id").Bind("id", id is string ? "\"" + stringId + "\"" : stringId).Execute().FetchOne();
    }
  }
}
