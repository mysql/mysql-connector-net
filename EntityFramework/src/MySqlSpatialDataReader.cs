// Copyright © 2013, 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.Types;
using System.Data.Entity.Spatial;


namespace MySql.Data.EntityFramework
{
  internal sealed class MySqlSpatialDataReader : DbSpatialDataReader
  {
    private readonly EFMySqlDataReader reader;

    static MySqlSpatialDataReader()
    {
    }

    public MySqlSpatialDataReader(EFMySqlDataReader underlyingReader)
    {
      this.reader = underlyingReader;
    }

    public override DbGeography GetGeography(int ordinal)
    {
      throw new NotImplementedException();
    }

    public override DbGeometry GetGeometry(int ordinal)
    {
      ReturnGeometryColumn(ordinal);

      var geometryBytes = this.reader.GetValue(ordinal);
      Type t = geometryBytes.GetType();

      object geometry = Activator.CreateInstance(t);

      var providerValue = new MySqlGeometry();

      MySqlGeometry.TryParse(geometryBytes.ToString(), out providerValue);      

      return MySqlSpatialServices.Instance.GeometryFromProviderValue(providerValue);
      
    }

    private void ReturnGeometryColumn(int ordinal)
    {
      string fieldTypeName = this.reader.GetDataTypeName(ordinal);
      if (!fieldTypeName.Equals("Geometry", StringComparison.Ordinal))
      {
        throw new InvalidOperationException(
            string.Format(
                "Invalid Geometry type reading operation on type {0}",
                fieldTypeName));
      }
    }

    public override bool IsGeographyColumn(int ordinal)
    {
      //throw new NotImplementedException();
      return false;
    }

    public override bool IsGeometryColumn(int ordinal)
    {
      //throw new NotImplementedException();
      return false;
    }
  }
}
