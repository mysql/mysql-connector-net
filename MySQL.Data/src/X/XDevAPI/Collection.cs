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
    /// Creates an <see cref="AddStatement"/> containing the provided objects. The add statement
    /// can be further modified before execution. This method is intended to add one or more
    /// items to a collection and can take anonymous objects, domain objects, or just plain
    /// JSON strings.
    /// </summary>
    /// <param name="items">The objects to add.</param>
    /// <returns>An <see cref="AddStatement"/> object containing the objects to add.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
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
    /// Creates a <see cref="RemoveStatement"/> with the given condition. The remove statement
    /// can then be further modified before execution. This method is intended to remove
    /// one or more documents from a collection.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="RemoveStatement"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is null or whitespace.</exception>
    public RemoveStatement Remove(string condition)
    {
      if (string.IsNullOrWhiteSpace(condition))
        throw new ArgumentNullException(nameof(condition), Resources.ParameterNullOrEmpty);

      RemoveStatement stmt = new RemoveStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Creates a <see cref="RemoveStatement"/> with the given identifier. The remove statement
    /// can then be further modified before execution. This method is intended to remove
    /// a single document from a collection. The identifier can really be of any type.
    /// </summary>
    /// <param name="id">The identifier to match the document.</param>
    /// <returns>A <see cref="RemoveStatement"/> object set with the given identifier.</returns>
    public RemoveStatement Remove(object id)
    {
      string key = id is string ?
        "\"" + id.ToString() + "\"" : id.ToString();
      string condition = String.Format("_id = {0}", key);
      RemoveStatement stmt = new RemoveStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Creates a <see cref="RemoveStatement"/> containing the identifier of the provided document. 
    /// The remove statement can then be further modified before execution. This method is intended 
    /// to remove a single document from a collection.
    /// </summary>
    /// <param name="doc">The <see cref="DbDoc"/> representing the document to remove.</param>
    /// <returns>A <see cref="RemoveStatement"/> object set with the given document's identifier.</returns>
    /// <exception cref="InvalidOperationException">No identifier for the document was provided.</exception>
    public RemoveStatement Remove(DbDoc doc)
    {
      if (!doc.HasId)
        throw new InvalidOperationException(ResourcesX.RemovingRequiresId);
      return Remove(doc.Id);
    }

    #endregion

    #region Modify Operations

    /// <summary>
    /// Creates a <see cref="ModifyStatement"/> with the given condition. The modify statement
    /// can be further modified before execution. This method is intended to modify one or more 
    /// documents from a collection.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="ModifyStatement"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is null or whitespace.</exception>
    public ModifyStatement Modify(string condition)
    {
      if (string.IsNullOrWhiteSpace(condition))
        throw new ArgumentNullException(nameof(condition), Resources.ParameterNullOrEmpty);

      ModifyStatement stmt = new ModifyStatement(this, condition);
      return stmt;
    }

    #endregion

    /// <summary>
    /// Returns the number of documents in this collection on the server.
    /// </summary>
    /// <returns>The number of documents.</returns>
    public long Count()
    {
      return Session.XSession.TableCount(Schema, Name);
    }

    /// <summary>
    /// Creates a <see cref="FindStatement"/> with the given condition. The find statement can be
    /// further modified before execution. This method is intened to find documents in a collection.
    /// </summary>
    /// <param name="condition">Optional condition to match documents.</param>
    /// <returns>A <see cref="FindStatement"/> object set with the given condition.</returns>
    public FindStatement Find(string condition = null)
    {
      FindStatement stmt = new FindStatement(this, condition);
      return stmt;
    }

    /// <summary>
    /// Creates a collection index.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <param name="isUnique">True if the index is unique, false otherwise.</param>
    /// <returns>A <see cref="CreateCollectionIndexStatement"/> object set with the given index name and the isUnique flag.</returns>
    public CreateCollectionIndexStatement CreateIndex(string indexName, bool isUnique)
    {
      return new CreateCollectionIndexStatement(this, indexName, isUnique);
    }

    /// <summary>
    /// Drops a collection index.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <returns>A <see cref="Result"/> object containing the result of the index drop.</returns>
    public Result DropIndex(string indexName)
    {
      return Session.XSession.DropCollectionIndex(this.Schema.Name, this.Name, indexName);
    }

    /// <summary>
    /// Verifies if the current collection exists in the server schema.
    /// </summary>
    /// <returns>True if the collection exists, false otherwise.</returns>
    public override bool ExistsInDatabase()
    {
      return Session.XSession.TableExists(Schema, Name);
    }
  }
}
