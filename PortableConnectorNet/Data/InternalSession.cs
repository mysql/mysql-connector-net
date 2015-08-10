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

using MySql.Data;
using MySql.Procotol;
using MySql.RoutingService;
using System;

namespace MySql.DataAccess
{
  internal class InternalSession : IDisposable
  {
    protected MySqlConnectionStringBuilder currentSettings;
    protected RoutingServiceBase routingService;
    protected ProtocolBase protocol;

    protected MySqlConnectionStringBuilder settings;

    public InternalSession(MySqlConnectionStringBuilder settings)
    {
      this.settings = settings;

      routingService = Factory.GetRoutingService(settings);
      currentSettings = routingService.GetCurrentConnection();

      protocol = Factory.GetProtocol(currentSettings);

    }

    public InternalSession(string connectionString)
      : this(new MySqlConnectionStringBuilder(connectionString))
    {
      
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

    public int ExecuteStatement(string query, params Parameter[] parameters)
    {
      return protocol.ExecuteStatement();
    }

    public void Find()
    {
      protocol.Find();
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


    public void Dispose()
    {
      throw new NotImplementedException();
    }
  }
}
