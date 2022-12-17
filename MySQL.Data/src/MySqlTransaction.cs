// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  ///  Represents a SQL transaction to be made in a MySQL database. This class cannot be inherited.
  /// </summary>
  /// <remarks>
  ///  The application creates a <see cref="MySqlTransaction"/> object by calling <see cref="MySqlConnection.BeginTransaction()"/>
  ///  on the <see cref="MySqlConnection"/> object. All subsequent operations associated with the
  ///  transaction (for example, committing or aborting the transaction), are performed on the
  ///  <see cref="MySqlTransaction"/> object.
  /// </remarks>
  /// <example>
  ///  The following example creates a <see cref="MySqlConnection"/> and a <see cref="MySqlTransaction"/>.
  ///  It also demonstrates how to use the <see cref="MySqlConnection.BeginTransaction()"/>,
  ///  <see cref="MySqlTransaction.Commit"/>, and <see cref="MySqlTransaction.Rollback"/> methods.
  ///  <code>
  ///    public void RunTransaction(string myConnString)
  ///    {
  ///      MySqlConnection myConnection = new MySqlConnection(myConnString);
  ///      myConnection.Open();
  ///      MySqlCommand myCommand = myConnection.CreateCommand();
  ///      MySqlTransaction myTrans;
  ///      // Start a local transaction
  ///      myTrans = myConnection.BeginTransaction();
  ///      // Must assign both transaction object and connection
  ///      // to Command object for a pending local transaction
  ///      myCommand.Connection = myConnection;
  ///      myCommand.Transaction = myTrans;
  ///      
  ///      try
  ///      {
  ///        myCommand.CommandText = "Insert into Region (RegionID, RegionDescription) VALUES (100, 'Description')";
  ///        myCommand.ExecuteNonQuery();
  ///        myCommand.CommandText = "Insert into Region (RegionID, RegionDescription) VALUES (101, 'Description')";
  ///        myCommand.ExecuteNonQuery();
  ///        myTrans.Commit();
  ///        Console.WriteLine("Both records are written to database.");
  ///      }
  ///      catch(Exception e)
  ///      {
  ///        try
  ///        {
  ///          myTrans.Rollback();
  ///        }
  ///        catch (MySqlException ex)
  ///        {
  ///          if (myTrans.Connection != null)
  ///          {
  ///            Console.WriteLine("An exception of type " + ex.GetType() +
  ///            " was encountered while attempting to roll back the transaction.");
  ///          }
  ///        }
  ///        
  ///        Console.WriteLine("An exception of type " + e.GetType() +
  ///        " was encountered while inserting the data.");
  ///        Console.WriteLine("Neither record was written to database.");
  ///      }
  ///      finally
  ///      {
  ///        myConnection.Close();
  ///      }
  ///    }
  ///  </code>
  /// </example>
  public sealed class MySqlTransaction : DbTransaction
  {
    private bool open;
    private bool disposed = false;

    internal MySqlTransaction(MySqlConnection c, IsolationLevel il)
    {
      Connection = c;
      IsolationLevel = il;
      open = true;
    }

    #region Destructor
    ~MySqlTransaction()
    {
      Dispose(false);
    }
    #endregion

    #region Properties

    /// <summary>
    /// Gets the <see cref="MySqlConnection"/> object associated with the transaction, or a null reference (Nothing in Visual Basic) if the transaction is no longer valid.
    /// </summary>
    /// <value>The <see cref="MySqlConnection"/> object associated with this transaction.</value>
    /// <remarks>
    /// A single application may have multiple database connections, each 
    /// with zero or more transactions. This property enables you to 
    /// determine the connection object associated with a particular 
    /// transaction created by <see cref="MySqlConnection.BeginTransaction()"/>.
    /// </remarks>
    public new MySqlConnection Connection { get; set; }

    /// <summary>
    /// Specifies the <see cref="IsolationLevel"/> for this transaction.
    /// </summary>
    /// <value>
    /// The <see cref="IsolationLevel"/> for this transaction. The default is <b>ReadCommitted</b>.
    /// </value>
    /// <remarks>
    /// Parallel transactions are not supported. Therefore, the IsolationLevel 
    /// applies to the entire transaction.
    /// </remarks>
    public override IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Gets the <see cref="DbConnection"/> object associated with the transaction,
    /// or a null reference if the transaction is no longer valid.
    /// </summary>
    protected override DbConnection DbConnection
    {
      get { return Connection; }
    }

    #endregion

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="MySqlTransaction"/>
    /// and optionally releases the managed resources
    /// </summary>
    /// <param name="disposing">If true, this method releases all resources held by any managed objects that
    /// this <see cref="MySqlTransaction"/> references.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposed) return;
      base.Dispose(disposing);
      if (disposing)
      {
        if ((Connection != null && Connection.State == ConnectionState.Open || Connection.SoftClosed) && open)
          Rollback();
      }
      disposed = true;
    }

    /// <summary>
    ///  Commits the database transaction.
    /// </summary>
    /// <remarks>
    ///  The <see cref="Commit"/> method is equivalent to the MySQL SQL statement COMMIT.
    /// </remarks>
    public override void Commit() => CommitAsync(false).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously commits the database transaction.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
#if NETFRAMEWORK || NETSTANDARD2_0
    public Task CommitAsync(CancellationToken cancellationToken = default)
#else
    public override Task CommitAsync(CancellationToken cancellationToken = default)
#endif
    => CommitAsync(true, cancellationToken);

    private async Task CommitAsync(bool execAsync, CancellationToken cancellationToken = default)
    {
      if (Connection == null || (Connection.State != ConnectionState.Open && !Connection.SoftClosed))
        throw new InvalidOperationException("Connection must be valid and open to commit transaction");
      if (!open)
        throw new InvalidOperationException("Transaction has already been committed or is not pending");
      using (MySqlCommand cmd = new MySqlCommand("COMMIT", Connection))
      {
        await cmd.ExecuteNonQueryAsync(execAsync, cancellationToken).ConfigureAwait(false);
        open = false;
      }
    }

    /// <summary>
    ///  Rolls back a transaction from a pending state.
    /// </summary>
    /// <remarks>
    ///  The <see cref="Rollback"/> method is equivalent to the MySQL statement ROLLBACK.
    ///  The transaction can only be rolled back from a pending state
    ///  (after BeginTransaction has been called, but before Commit is
    ///  called).
    /// </remarks>
    public override void Rollback() => RollbackAsync(false).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously rolls back a transaction from a pending state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
#if NETFRAMEWORK || NETSTANDARD2_0
    public Task RollbackAsync(CancellationToken cancellationToken = default)
#else
    public override Task RollbackAsync(CancellationToken cancellationToken = default)
#endif
    => RollbackAsync(true, cancellationToken);

    private async Task RollbackAsync(bool execAsync, CancellationToken cancellationToken = default)
    {
      if (Connection == null || (Connection.State != ConnectionState.Open && !Connection.SoftClosed))
        throw new InvalidOperationException("Connection must be valid and open to rollback transaction");
      if (!open)
        throw new InvalidOperationException("Transaction has already been rolled back or is not pending");
      using (MySqlCommand cmd = new MySqlCommand("ROLLBACK", Connection))
      {
        await cmd.ExecuteNonQueryAsync(execAsync, cancellationToken).ConfigureAwait(false);
        open = false;
      }
    }
  }
}
