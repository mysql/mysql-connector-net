// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using System.Data.Metadata.Edm;
using System.Data;
using MySql.Data.MySqlClient;

namespace MySql.Data.Entity
{
  class EFMySqlCommand : DbCommand, ICloneable
  {
    private bool designTimeVisible = true;
    private DbConnection connection;
    private MySqlCommand command = new MySqlCommand();

    internal PrimitiveType[] ColumnTypes;

    #region Properties

    public override string CommandText
    {
      get { return command.CommandText; }
      set { command.CommandText = value; }
    }

    public override int CommandTimeout
    {
      get { return command.CommandTimeout; }
      set { command.CommandTimeout = value; }
    }

    public override CommandType CommandType
    {
      get { return command.CommandType; }
      set { command.CommandType = value; }
    }

    public override bool DesignTimeVisible
    {
      get { return designTimeVisible; }
      set { designTimeVisible = value; }
    }

    protected override DbConnection DbConnection
    {
      get { return connection; }
      set
      {
        connection = value;
        command.Connection = (MySqlConnection)value;
        MySqlConnection _con = (MySqlConnection)connection;
        if (_con.Settings.UseDefaultCommandTimeoutForEF)
        {
          command.CommandTimeout = (int)(_con.Settings.DefaultCommandTimeout);
        }
      }
    }

    protected override DbTransaction DbTransaction
    {
      get { return command.Transaction; }
      set { command.Transaction = (MySqlTransaction)value; }
    }

    protected override DbParameterCollection DbParameterCollection
    {
      get { return command.Parameters; }
    }

    public override UpdateRowSource UpdatedRowSource
    {
      get { return command.UpdatedRowSource; }
      set { command.UpdatedRowSource = value; }
    }

    #endregion

    public override void Cancel()
    {
      command.Cancel();
    }

    protected override DbParameter CreateDbParameter()
    {
      return new MySqlParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
      return new EFMySqlDataReader(this, command.ExecuteReader(behavior));
    }

    public override int ExecuteNonQuery()
    {
      return command.ExecuteNonQuery();
    }

    public override object ExecuteScalar()
    {
      return command.ExecuteScalar();
    }

    public override void Prepare()
    {
      command.Prepare();
    }

    #region ICloneable Members

    public object Clone()
    {
      EFMySqlCommand clone = new EFMySqlCommand();

      clone.connection = connection;
      clone.ColumnTypes = ColumnTypes;
      clone.command = (MySqlCommand)((ICloneable)command).Clone();

      return clone;
    }

    #endregion
  }
}
