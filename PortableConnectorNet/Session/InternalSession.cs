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

using System;
using System.IO;
using MySql.Data;
using MySql.RoutingServices;
using MySql.Security;
using MySql.Protocol;
using MySql.XDevAPI;
using MySql.XDevAPI.Results;

namespace MySql.Session
{
  internal abstract class InternalSession : IDisposable
  {
    protected Stream _stream;
    //protected MySqlConnectionStringBuilder _settings;
    //protected RoutingServiceBase routingService;
    //protected internal MySqlConnectionStringBuilder settings;
    
    private bool disposed = false;

    public InternalSession(MySqlConnectionStringBuilder settings)
    {
      Settings = settings;

//      routingService = new DefaultRoutingService(settings);

      //routingService = Factory.GetRoutingService(settings);
  //    currentSettings = routingService.GetCurrentConnection();
    }

    protected abstract void Open();

    public abstract void Close();
    
    protected abstract ProtocolBase GetProtocol();


    protected MySqlConnectionStringBuilder Settings;

    protected AuthenticationPlugin GetAuthenticationPlugin()
    {
      return new MySQL41AuthenticationPlugin(Settings);
    }
   

    public static InternalSession GetSession(MySqlConnectionStringBuilder settings)
    {
      InternalSession session = new XInternalSession(settings);
      session.Open();
      return session;
    }

    public TableResult ExecuteQuery(string sql)
    {
      GetProtocol().SendSQL(sql);
      return new TableResult(GetProtocol());
    }

    public object ExecuteQueryAsScalar(string sql)
    {
      GetProtocol().SendSQL(sql);
      TableResult r = new TableResult(GetProtocol());
      r.Buffer();

      if (r.Failed)
        throw new MySqlException("Query execution failed: " + r.ErrorInfo.Message);
      return r[0];
    }

    public void SetSchema(string schema)
    {
//      currentSettings.Database = schema;
    }


    //public ResultSet Find(SelectStatement statement)
    //{
    //  return (protocol as XProtocol).Find(statement);
    //}

    


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
