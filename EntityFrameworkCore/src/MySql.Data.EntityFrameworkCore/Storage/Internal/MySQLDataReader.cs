// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using MySql.Data.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLDataReader : DbDataReader
  {
    private MySqlDataReader _reader;

    public MySQLDataReader(MySqlDataReader reader)
    {
      _reader = reader;
    }

    public override T GetFieldValue<T>(int ordinal)
    {
      if (typeof(T).Equals(typeof(DateTimeOffset)))
      {
        var dtValue = new DateTime();
        var result = DateTime.TryParse(_reader.GetValue(ordinal).ToString(), out dtValue);
        DateTime datetime = result ? dtValue : DateTime.MinValue;
        return (T)Convert.ChangeType(new DateTimeOffset(datetime), typeof(T));
      }
      else
        return base.GetFieldValue<T>(ordinal);
    }

    public override bool GetBoolean(int ordinal) => GetReader().GetBoolean(ordinal);
    public override byte GetByte(int ordinal) => GetReader().GetByte(ordinal);
    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => GetReader().GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    public override char GetChar(int ordinal) => GetReader().GetChar(ordinal);
    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => GetReader().GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    public override string GetDataTypeName(int ordinal) => GetReader().GetDataTypeName(ordinal);
    public override DateTime GetDateTime(int ordinal) => GetReader().GetDateTime(ordinal);
    public override decimal GetDecimal(int ordinal) => GetReader().GetDecimal(ordinal);
    public override double GetDouble(int ordinal) => GetReader().GetDouble(ordinal);
    public override Type GetFieldType(int ordinal) => GetReader().GetFieldType(ordinal);
    public override float GetFloat(int ordinal) => GetReader().GetFloat(ordinal);
    public override Guid GetGuid(int ordinal) => GetReader().GetGuid(ordinal);
    public override short GetInt16(int ordinal) => GetReader().GetInt16(ordinal);
    public override int GetInt32(int ordinal) => GetReader().GetInt32(ordinal);
    public override long GetInt64(int ordinal) => GetReader().GetInt64(ordinal);
    public override string GetName(int ordinal) => GetReader().GetName(ordinal);
    public override int GetOrdinal(string name) => GetReader().GetOrdinal(name);
    public override string GetString(int ordinal) => GetReader().GetString(ordinal);
    public override object GetValue(int ordinal) => GetReader().GetValue(ordinal);
    public override int GetValues(object[] values) => GetReader().GetValues(values);
    public override bool IsDBNull(int ordinal) => GetReader().IsDBNull(ordinal);
    public override int FieldCount => GetReader().FieldCount;
    public override object this[int ordinal] => GetReader()[ordinal];
    public override object this[string name] => GetReader()[name];
    public override int RecordsAffected => GetReader().RecordsAffected;
    public override bool HasRows => GetReader().HasRows;
    public override bool IsClosed => _reader == null || _reader.IsClosed;
    public override int Depth => GetReader().Depth;
    public override IEnumerator GetEnumerator() => GetReader().GetEnumerator();
    public override Type GetProviderSpecificFieldType(int ordinal) => GetReader().GetProviderSpecificFieldType(ordinal);
    public override object GetProviderSpecificValue(int ordinal) => GetReader().GetProviderSpecificValue(ordinal);
    public override int GetProviderSpecificValues(object[] values) => GetReader().GetProviderSpecificValues(values);
    public override int VisibleFieldCount => GetReader().VisibleFieldCount;


    private MySqlDataReader GetReader()
    {
      if (_reader == null)
        throw new ObjectDisposedException(nameof(MySQLDataReader));
      return _reader;
    }

    public override bool NextResult()
    {
      return GetReader().NextResult();
    }

    public override bool Read()
    {
      return GetReader().Read();
    }
  }
}
