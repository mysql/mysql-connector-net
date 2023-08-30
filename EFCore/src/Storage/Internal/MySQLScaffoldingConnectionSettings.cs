// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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
using System.Data.Common;

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal class MySQLScaffoldingConnectionSettings
  {
    public const string ScaffoldPrefix = "Scaffold:";
    public const string CharSetKey = ScaffoldPrefix + "CharSet";
    public const string CollationKey = ScaffoldPrefix + "Collation";
    public const string ViewsKey = ScaffoldPrefix + "Views";

    private readonly DbConnectionStringBuilder _csb;

    public MySQLScaffoldingConnectionSettings(string connectionString)
    {
      _csb = new DbConnectionStringBuilder { ConnectionString = connectionString };

      CharSet = GetBoolean(CharSetKey, true);
      Collation = GetBoolean(CollationKey, true);
      Views = GetBoolean(ViewsKey, true);
    }

    public virtual bool CharSet { get; set; }
    public virtual bool Collation { get; set; }
    public virtual bool Views { get; set; }

    public virtual string GetProviderCompatibleConnectionString()
    {
      var csb = new DbConnectionStringBuilder { ConnectionString = _csb.ConnectionString };

      csb.Remove(CharSetKey);
      csb.Remove(CollationKey);
      csb.Remove(ViewsKey);

      return csb.ConnectionString;
    }

    protected virtual bool GetBoolean(string key, bool defaultValue = default)
    {
      if (_csb.TryGetValue(key, out var value))
      {
        if (value is string stringValue)
        {
          if (int.TryParse(stringValue, out var intValue))
          {
            return intValue != 0;
          }

          if (bool.TryParse(stringValue, out var boolValue))
          {
            return boolValue;
          }

          if (stringValue.Equals("on", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("enable", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("enabled", StringComparison.OrdinalIgnoreCase))
            return true;

          if (stringValue.Equals("off", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("disable", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("disabled", StringComparison.OrdinalIgnoreCase))
            return false;
        }

        return Convert.ToBoolean(value);
      }

      return defaultValue;
    }

    protected virtual bool Equals(MySQLScaffoldingConnectionSettings other)
    {
      return CharSet == other.CharSet &&
         Collation == other.Collation &&
         Views == other.Views;
    }

    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }

      if (ReferenceEquals(this, obj))
      {
        return true;
      }

      if (obj.GetType() != this.GetType())
      {
        return false;
      }

      return Equals((MySQLScaffoldingConnectionSettings)obj);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return (CharSet.GetHashCode() * 397) ^ Collation.GetHashCode();
      }
    }
  }
}
