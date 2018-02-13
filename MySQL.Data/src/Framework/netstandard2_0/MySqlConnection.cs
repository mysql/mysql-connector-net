// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Security;
using System.Drawing;
using System.Security.Permissions;
using System.Transactions;

namespace MySql.Data.MySqlClient
{
#if NET452
  [ToolboxBitmap(typeof(MySqlConnection), "MySqlClient.resources.connection.bmp")]
#endif
  [DesignerCategory("Code")]
  [ToolboxItem(true)]
  public sealed partial class MySqlConnection : DbConnection, ICloneable
  {
    /// <summary>
    /// Returns schema information for the data source of this <see cref="DbConnection"/>. 
    /// </summary>
    /// <returns>A <see cref="DataTable"/> that contains schema information. </returns>
    public override DataTable GetSchema()
    {
      return GetSchema(null);
    }

    /// <summary>
    /// Returns schema information for the data source of this 
    /// <see cref="DbConnection"/> using the specified string for the schema name. 
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return. </param>
    /// <returns>A <see cref="DataTable"/> that contains schema information. </returns>
    public override DataTable GetSchema(string collectionName)
    {
      if (collectionName == null)
        collectionName = SchemaProvider.MetaCollection;

      return GetSchema(collectionName, null);
    }

    /// <summary>
    /// Returns schema information for the data source of this <see cref="DbConnection"/>
    /// using the specified string for the schema name and the specified string array 
    /// for the restriction values. 
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return.</param>
    /// <param name="restrictionValues">Specifies a set of restriction values for the requested schema.</param>
    /// <returns>A <see cref="DataTable"/> that contains schema information.</returns>
    public override DataTable GetSchema(string collectionName, string[] restrictionValues)
    {
      if (collectionName == null)
        collectionName = SchemaProvider.MetaCollection;

      string[] restrictions = _schemaProvider.CleanRestrictions(restrictionValues);
      MySqlSchemaCollection c = _schemaProvider.GetSchema(collectionName, restrictions);
      return c.AsDataTable();
    }

    /// <summary>
    /// Enlists in the specified transaction. 
    /// </summary>
    /// <param name="transaction">
    /// A reference to an existing <see cref="System.Transactions.Transaction"/> in which to enlist.
    /// </param>
    public override void EnlistTransaction(Transaction transaction)
    {
      // enlisting in the null transaction is a noop
      if (transaction == null)
        return;

      // guard against trying to enlist in more than one transaction
      if (driver.currentTransaction != null)
      {
        if (driver.currentTransaction.BaseTransaction == transaction)
          return;

        Throw(new MySqlException("Already enlisted"));
      }

      // now see if we need to swap out drivers.  We would need to do this since
      // we have to make sure all ops for a given transaction are done on the
      // same physical connection.
      Driver existingDriver = DriverTransactionManager.GetDriverInTransaction(transaction);
      if (existingDriver != null)
      {
        // we can't allow more than one driver to contribute to the same connection
        if (existingDriver.IsInActiveUse)
          Throw(new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported));

        // there is an existing driver and it's not being currently used.
        // now we need to see if it is using the same connection string
        string text1 = existingDriver.Settings.ConnectionString;
        string text2 = Settings.ConnectionString;
        if (String.Compare(text1, text2, true) != 0)
          Throw(new NotSupportedException(Resources.MultipleConnectionsInTransactionNotSupported));

        // close existing driver
        // set this new driver as our existing driver
        CloseFully();
        driver = existingDriver;
      }

      if (driver.currentTransaction == null)
      {
        MySqlPromotableTransaction t = new MySqlPromotableTransaction(this, transaction);
        if (!transaction.EnlistPromotableSinglePhase(t))
          Throw(new NotSupportedException(Resources.DistributedTxnNotSupported));

        driver.currentTransaction = t;
        DriverTransactionManager.SetDriverInTransaction(driver);
        driver.IsInActiveUse = true;
      }
    }

    void AssertPermissions()
    {
      // Security Asserts can only be done when the assemblies 
      // are put in the GAC as documented in 
      // http://msdn.microsoft.com/en-us/library/ff648665.aspx
      if (this.Settings.IncludeSecurityAsserts)
      {
        PermissionSet set = new PermissionSet(PermissionState.None);
        set.AddPermission(new MySqlClientPermission(ConnectionString));
        set.Demand();
        MySqlSecurityPermission.CreatePermissionSet(true).Assert();
      }
    }

    /// <summary>
    /// Creates a new MySqlConnection object with the exact same ConnectionString value
    /// </summary>
    /// <returns>A cloned MySqlConnection object</returns>
    public object Clone()
    {
      MySqlConnection clone = new MySqlConnection();
      string connectionString = Settings.ConnectionString;
      if (connectionString != null)
        clone.ConnectionString = connectionString;
      return clone;
    }
  }
}
