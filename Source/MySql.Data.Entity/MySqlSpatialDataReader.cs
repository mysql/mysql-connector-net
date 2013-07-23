// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System.Reflection;
#if EF6
using System.Data.Entity.Spatial;
#else
using System.Data.Spatial;
#endif


namespace MySql.Data.Entity
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

#if EF6
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
#endif
  }
}
