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

using MySql.common;
using MySql.Data;
using MySql.DataAccess;
using System;
using System.Collections.Generic;

namespace MySql.XDevAPI
{
  public abstract class BaseSession : IDisposable
  {
    private string connectionString;
    private InternalSession internalSession = null;
    private bool disposed = false;

    public MySqlConnectionStringBuilder Settings
    {
      get { return internalSession.Settings; }
    }

    public Schema Schema { get; protected set; }


    public BaseSession(string connectionString)
    {
      this.connectionString = connectionString;
      internalSession = new InternalSession(connectionString);
    }

    public BaseSession(object connectionData)
    {
      internalSession = new InternalSession();
      if (!connectionData.GetType().IsGenericType)
        throw new MySqlException("Connection Data format is incorrect.");

      var values = Tools.GetDictionaryFromAnonymous(connectionData);
      foreach (var value in values)
      {
        if (!Settings.ContainsKey(value.Key))
          throw new MySqlException(string.Format("Attribute '{0}' is not defined in the connection", value.Key));
        Settings.SetValue(value.Key, value.Value);
      }
      this.connectionString = Settings.ToString();
    }


    public Schema GetSchema(string schema)
    {
      internalSession.SetSchema(schema);
      this.Schema = new Schema(this);
      return this.Schema;
    }

    public Schema GetDefaultSchema()
    {
      return new Schema(this);
    }

    public Schema UseDefaultSchema()
    {
      return new Schema(this);
    }

    public List<Schema> GetSchemas()
    {
      return new List<Schema>();
    }

    public Type GetTopologyType()
    {
      throw new NotImplementedException();
    }

    public List<Nodes> GetSlaveNodes()
    {
      throw new NotImplementedException();
    }

    #region InternalMethods


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
        if (internalSession != null) internalSession.Dispose();
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
