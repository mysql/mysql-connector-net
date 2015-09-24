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

using MySql.XDevAPI;
using MySql.XDevAPI.Relational;
using System;
using Xunit;

namespace PortableConnectorNetTests
{
  public class TableFixture : BaseTest, IDisposable
  {
    public string Schema { get; private set; }
    public string Table { get; private set; }
    public string TableInsert { get; private set; }

    public TableFixture()
    {
      Schema = BaseTest.schemaName;
      Table = "employees";
      TableInsert = "tableInsert";

      var nodeSession = GetNodeSession();

      // Create Tables
      string script = string.Format(Properties.Resources.TableScripts, Schema, Table, TableInsert).Replace("\r", string.Empty);
      string[] sqlInParts = script.Split(new string[] { ";\n\n\n", ";\n\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries);
      foreach (string sqlPart in sqlInParts)
      {
        if (string.IsNullOrWhiteSpace(sqlPart)) continue;
        nodeSession.SQL(sqlPart.Replace("\n", " ")).Execute();
      }
    }

    public Table GetTable()
    {
      return GetSession().GetSchema(Schema).GetTable(Table);
    }

    public Table GetTableInsert()
    {
      return GetSession().GetSchema(Schema).GetTable(TableInsert);
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          // TODO: dispose managed state (managed objects).
          GetSession().DropSchema(Schema);
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~TableFixture() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }
}
