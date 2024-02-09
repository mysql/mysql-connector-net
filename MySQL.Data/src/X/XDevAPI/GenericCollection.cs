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
  /// Represents a collection of documents with a generic type.
  /// <typeparam name="T"/>
  /// </summary>
  public class Collection<T> : DatabaseObject
  {
    /// <summary>
    /// Initializes a new instance of the generic Collection class based on the specified schema 
    /// and name.
    /// </summary>
    /// <param name="s">The <see cref="Schema"/> object associated to this collection.</param>
    /// <param name="name">The name of the collection.</param>
    public Collection(Schema s, string name) : base(s, name) { }

    /// <summary>
    /// Creates an <see cref="AddStatement{T}"/> containing the provided generic object. The add 
    /// statement can be further modified before execution.
    /// </summary>
    /// <param name="items">The generic object to add.</param>
    /// <returns>An <see cref="AddStatement{T}"/> object containing the object to add.</returns>
    public AddStatement<T> Add(params object[] items)
    {
      if (items == null)
        throw new ArgumentNullException();

      AddStatement<T> stmt = new AddStatement<T>(this);
      stmt.Add(items);
      return stmt;
    }

    #region Remove Operations

    /// <summary>
    /// Creates a <see cref="RemoveStatement{T}"/> with the given condition that can be used to remove
    /// one or more documents from a collection.The statement can then be further modified before execution.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="RemoveStatement{T}"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c> or white space.</exception>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public RemoveStatement<T> Remove(string condition)
    {
      if (string.IsNullOrWhiteSpace(condition))
        throw new ArgumentNullException(nameof(condition), Resources.ParameterNullOrEmpty);

      RemoveStatement<T> stmt = new RemoveStatement<T>(this, condition);
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
    /// Creates a <see cref="ModifyStatement{T}"/> with the given condition that can be used to modify one or more
    /// documents from a collection.
    /// </summary>
    /// <param name="condition">The condition to match documents.</param>
    /// <returns>A <see cref="ModifyStatement{T}"/> object set with the given condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c> or white space.</exception>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public ModifyStatement<T> Modify(string condition)
    {
      if (string.IsNullOrWhiteSpace(condition))
        throw new ArgumentNullException(nameof(condition), Resources.ParameterNullOrEmpty);

      ModifyStatement<T> stmt = new ModifyStatement<T>(this, condition);
      return stmt;
    }
    #endregion

    /// <summary>
    /// Returns the number of documents in this collection on the server.
    /// </summary>
    /// <returns>The number of documents found.</returns>
    public long Count()
    {
      long result = 0;
      try
      {
        ValidateOpenSession();
        result = Session.XSession.TableCount(Schema, Name, "Collection");
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
    /// Creates a <see cref="FindStatement{T}"/> with the given condition which can be used to find documents in a
    /// collection.
    /// </summary>
    /// <param name="condition">An optional condition to match documents.</param>
    /// <returns>A <see cref="FindStatement{T}"/> object set with the given condition.</returns>
    /// <remarks>The statement can then be further modified before execution.</remarks>
    public FindStatement<T> Find(string condition = null)
    {
      FindStatement<T> stmt = new FindStatement<T>(this, condition);
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
    /// <para />- SMALLINT[UNSIGNED]
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
      new CreateCollectionIndexStatement<T>(this, indexName, new DbDoc(indexDefinition)).Execute();
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

      try
      {
        bool indexExists = Convert.ToInt32(Session.XSession.ExecuteQueryAsScalar(
           string.Format("SELECT COUNT(*)>0 FROM information_schema.statistics WHERE table_schema = '{0}' AND table_name = '{1}' AND index_name = '{2}'",
           this.Schema.Name, this.Name, indexName))) == 1;
        if (!indexExists) return;

        Session.XSession.DropCollectionIndex(this.Schema.Name, this.Name, indexName);
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

    /// <summary>
    /// Verifies if the current collection exists in the server schema.
    /// </summary>
    /// <returns><c>true</c> if the collection exists; otherwise, <c>false</c>.</returns>
    public override bool ExistsInDatabase()
    {
      bool result = false;
      try
      {
        ValidateOpenSession();
        result = Session.XSession.TableExists(Schema, Name);
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
    /// Returns the document with the given identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document to replace.</param>
    /// <returns>A <typeparamref name="T"/> object if a document matching given identifier exists; otherwise, <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c> or white space.</exception>
    /// <remarks>This is a direct execution method.</remarks>
    public T GetOne(object id)
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
