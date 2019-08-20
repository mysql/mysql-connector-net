// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI.CRUD;
using MySqlX.XDevAPI.Common;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Represents a collection of documents.
  /// </summary>
  public class Collection : DatabaseObject
  {
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

      return Remove("_id = :id").Bind("id", id).Execute();
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
      if (!this.Session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3))
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
      ValidateOpenSession();
      return Session.XSession.TableCount(Schema, Name, "Collection");
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
    /// Creates an index based on the properties provided in the JSON document.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <param name="indexDefinition">JSON document describing the index to be created.</param>
    /// <remarks>
    /// <para><paramref name="indexDefinition"/> is a JSON document with the following fields:</para>
    /// <para>
    /// <para />- <c>fields</c>: array of <c>IndexField</c> objects, each describing a single document member to be
    /// included in the index (see below).
    /// <para />- <c>type: string</c>, (optional) the type of index. One of INDEX or SPATIAL. Default is INDEX and may
    /// be omitted.
    /// </para>
    /// <para>&#160;</para>
    /// <para>A single <c>IndexField</c> description consists of the following fields:</para>
    /// <para>
    /// <para />- <c>field</c>: string, the full document path to the document member or field to be indexed.
    /// <para />- <c>type</c>: string, one of the supported SQL column types to map the field into (see the following list).
    /// For numeric types, the optional UNSIGNED keyword may follow. For the TEXT type, the length to consider for
    /// indexing may be added.
    /// <para />- <c>required</c>: bool, (optional) true if the field is required to exist in the document. defaults to
    /// false, except for GEOJSON where it defaults to true.
    /// <para />- <c>options</c>: int, (optional) special option flags for use when decoding GEOJSON data.
    /// <para />- <c>srid</c>: int, (optional) srid value for use when decoding GEOJSON data.
    /// </para>
    /// <para>&#160;</para>
    /// <para>Supported SQL column types:</para>
    /// <para>
    /// <para />- INT [UNSIGNED]
    /// <para />- TINYINT [UNSIGNED]
    /// <para />- SMALLINT [UNSIGNED]
    /// <para />- MEDIUMINT [UNSIGNED]
    /// <para />- INTEGER [UNSIGNED]
    /// <para />- BIGINT [UNSIGNED]
    /// <para />- REAL [UNSIGNED]
    /// <para />- FLOAT [UNSIGNED]
    /// <para />- DOUBLE [UNSIGNED]
    /// <para />- DECIMAL [UNSIGNED]
    /// <para />- NUMERIC [UNSIGNED]
    /// <para />- DATE
    /// <para />- TIME
    /// <para />- TIMESTAMP
    /// <para />- DATETIME
    /// <para />- TEXT[(length)]
    /// <para />- CHAR[(lenght)] 
    /// <para />- GEOJSON (extra options: options, srid)
    /// </para>
    /// </remarks>
    public void CreateIndex(string indexName, object indexDefinition)
    {
      new CreateCollectionIndexStatement(this, indexName, new DbDoc(indexDefinition)).Execute();
    }

    /// <summary>
    /// Drops a collection index.
    /// </summary>
    /// <param name="indexName">The index name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="indexName"/> is <c>null</c> or white space.</exception>
    public void DropIndex(string indexName)
    {
      if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentNullException(nameof(indexName));

      ValidateOpenSession();

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
      ValidateOpenSession();
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

      return Find("_id = :id").Bind("id", id).Execute().FetchOne();
    }
  }
}
