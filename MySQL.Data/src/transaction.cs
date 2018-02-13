// Copyright (c) 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Data.MySqlClient
{
  /// <include file='docs/MySqlTransaction.xml' path='docs/Class/*'/>
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
    public new MySqlConnection Connection { get; }

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

    protected override DbConnection DbConnection
    {
      get { return Connection; }
    }

    #endregion


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

    /// <include file='docs/MySqlTransaction.xml' path='docs/Commit/*'/>
    public override void Commit()
    {
      if (Connection == null || (Connection.State != ConnectionState.Open && !Connection.SoftClosed))
        throw new InvalidOperationException("Connection must be valid and open to commit transaction");
      if (!open)
        throw new InvalidOperationException("Transaction has already been committed or is not pending");
      MySqlCommand cmd = new MySqlCommand("COMMIT", Connection);
      cmd.ExecuteNonQuery();
      open = false;
    }

    /// <include file='docs/MySqlTransaction.xml' path='docs/Rollback/*'/>
    public override void Rollback()
    {
      if (Connection == null || (Connection.State != ConnectionState.Open && !Connection.SoftClosed))
        throw new InvalidOperationException("Connection must be valid and open to rollback transaction");
      if (!open)
        throw new InvalidOperationException("Transaction has already been rolled back or is not pending");
      MySqlCommand cmd = new MySqlCommand("ROLLBACK", Connection);
      cmd.ExecuteNonQuery();
      open = false;
    }

  }
}
