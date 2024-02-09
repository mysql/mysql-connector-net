// Copyright © 2015, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using System;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a collection of documents.
  /// </summary>
  public class Collection : Collection<DbDoc>
  {
    internal Collection(Schema schema, string name)
      : base(schema, name)
    {
    }

    #region Add Operations

    /// <summary>
    /// Creates an <see cref="AddStatement{DbDoc}"/> containing the provided objects that can be used to add
    /// one or more items to a collection.
    /// </summary>
    /// <param name="items">The objects to add.</param>
    /// <returns>An <see cref="AddStatement{DbDoc}"/> object containing the objects to add.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
    /// <remarks>This method can take anonymous objects, domain objects, or just plain JSON strings.
    /// The statement can be further modified before execution.</remarks>
    public new AddStatement<DbDoc> Add(params object[] items) => base.Add(items);

    #endregion

    #region Remove Operations

    /// <summary>
    /// Creates a <see cref="RemoveStatement{DbDoc}"/> with the given condition that can be used to remove
    /// one or more documents from a collection.The statement can then be further modified before execution.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="RemoveStatement{DbDoc}"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c> or white space.</exception>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public new RemoveStatement<DbDoc> Remove(string condition) => base.Remove(condition);

    #endregion

    #region Modify Operations

    /// <summary>
    /// Creates a <see cref="ModifyStatement{DbDoc}"/> with the given condition that can be used to modify one or more
    /// documents from a collection.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="ModifyStatement{DbDoc}"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c> or white space.</exception>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public new ModifyStatement<DbDoc> Modify(string condition) => base.Modify(condition);

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
      DbDoc newDocument = doc is DbDoc ? doc as DbDoc : new DbDoc(doc);

      if (currentDocument != null)
      {
        // check for not matching id's
        if (newDocument.HasId && !newDocument.Id.Equals(currentDocument.Id))
          throw new MySqlException(ResourcesX.ReplaceWithNoMatchingId);

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
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3))
        throw new MySqlException(string.Format(ResourcesX.FunctionalityNotSupported, "8.0.3"));
      if (id == null)
        throw new ArgumentNullException(nameof(id));
      string stringId = id.ToString();
      if (string.IsNullOrWhiteSpace(stringId))
        throw new ArgumentNullException(nameof(id), Resources.ParameterNullOrEmpty);
      if (doc == null)
        throw new ArgumentNullException(nameof(doc));

      DbDoc currentDocument = GetOne(id);
      DbDoc newDocument = doc is DbDoc ? doc as DbDoc : new DbDoc(doc);

      // check for not matching id's
      if (currentDocument != null && newDocument.HasId
        && !newDocument.Id.Equals(currentDocument.Id))
        throw new MySqlException(ResourcesX.ReplaceWithNoMatchingId);

      newDocument.Id = id;
      AddStatement<DbDoc> stmt = Add(newDocument);
      stmt.upsert = true;
      return stmt.Execute();
    }
    #endregion

    /// <summary>
    /// Creates a <see cref="FindStatement{DbDoc}"/> with the given condition, which can be used to find documents in a
    /// collection.
    /// </summary>
    /// <param name="condition">An optional condition to match documents.</param>
    /// <returns>A <see cref="FindStatement{DbDoc}"/> object set with the given condition.</returns>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public new FindStatement<DbDoc> Find(string condition = null) => base.Find(condition);

    /// <summary>
    /// Returns the document with the given identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document to replace.</param>
    /// <returns>A <see cref="DbDoc"/> object if a document matching given identifier exists; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c> or white space.</exception>
    /// <remarks>This is a direct execution method.</remarks>
    public new DbDoc GetOne(object id) => base.GetOne(id);
  }
}
