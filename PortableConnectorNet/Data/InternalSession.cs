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

using MySql.Communication;
using MySql.Data;
using MySql.RoutingServices;
using System;
using MySql.Procotol;
using System.Collections.Generic;
using MySql.XDevAPI;

namespace MySql.DataAccess
{
  internal class InternalSession : IDisposable
  {
    protected MySqlConnectionStringBuilder currentSettings;
    protected RoutingServiceBase routingService;
    protected ProtocolBase<UniversalStream> protocol;
    protected internal MySqlConnectionStringBuilder settings;
    
    private bool disposed = false;

    public MySqlConnectionStringBuilder Settings { get; private set; }

    public InternalSession(MySqlConnectionStringBuilder settings)
    {
      this.Settings = settings;

      routingService = new DefaultRoutingService(settings);

      //routingService = Factory.GetRoutingService(settings);
      currentSettings = routingService.GetCurrentConnection();
      protocol = Factory.GetProtocol(currentSettings);

    }

    public InternalSession(string connectionString)
      : this(new MySqlConnectionStringBuilder(connectionString))
    {
      Open();
    }

    public InternalSession()
      : this(new MySqlConnectionStringBuilder())
    {
      Open();
    }

    public ResultSet GetResultSet(string sql)
    {
      protocol.SendExecuteStatement("sql", sql, null);
      return protocol.ReadResultSet();
    }

    public void CreateCollection(string schemaName, string collectionName)
    {
      protocol.SendExecuteStatement("xplugin", "create_collection", schemaName, collectionName);
      Result r = protocol.ReadStmtExecuteResult();
      if (r.Failed)
        throw new MySqlException(r);
    }

    #region Actions

    public void Open()
    {
      protocol.OpenConnection();
    }

    public void Close()
    {
      protocol.CloseConnection();
    }

    public void SetSchema(string schema)
    {
      currentSettings.Database = schema;
    }

    public void ExecutePrepareStatement()
    {
      protocol.ExecutePrepareStatement();
    }

    public void ExecuteReader(string query, params Parameter[] parameters)
    {
      protocol.ExecuteReader();
    }

    public ResultSet Find(SelectStatement statement)
    {
      return protocol.Find(statement);
    }

    public void Insert()
    {
      protocol.Insert();
    }

    public void Update()
    {
      protocol.Update();
    }

    public void Delete()
    {
      protocol.Delete();
    }

    public void Reset()
    {
      protocol.Reset();
    }

    public void ExecuteBatch()
    {
      protocol.ExecuteBatch();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposed) return;

      if (disposing)
      {
        // Free any other managed objects here. 
        //
      }

      // Free any unmanaged objects here. 
      //
      disposed = true;
    }

    //~BaseSession()
    //{
    //  Dispose(false);
    //}

    #endregion
  }
}
