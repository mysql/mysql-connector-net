// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Globalization;
#if !EF6
using System.Data.Metadata.Edm; 
#else
using System.Data.Entity.Core.Metadata.Edm; 
#endif
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class ProviderManifestTests : IUseFixture<SetUpEntityTests>
  {
    private SetUpEntityTests st;

    public void SetFixture(SetUpEntityTests data)
    {
      st = data;
    }

    [Fact]
    public void TestingMaxLengthFacet()
    {
      using (MySqlConnection connection = new MySqlConnection(st.GetConnectionString(true)))
      {
        MySqlProviderManifest pm = new MySqlProviderManifest(st.Version.ToString());
        TypeUsage tu = TypeUsage.CreateStringTypeUsage(
          PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), false, false);
        TypeUsage result = pm.GetStoreType(tu);
        Assert.Equal("longtext", result.EdmType.Name);

        tu = TypeUsage.CreateStringTypeUsage(
          PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), false, false, Int32.MaxValue);
        result = pm.GetStoreType(tu);
        Assert.Equal("longtext", result.EdmType.Name);

        tu = TypeUsage.CreateStringTypeUsage(
          PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), false, false, 70000);
        result = pm.GetStoreType(tu);
        Assert.Equal("mediumtext", result.EdmType.Name);

      }
    }

    /// <summary>
    /// Bug #62135 Connector/Net Incorrectly Maps PrimitiveTypeKind.Byte to "tinyint"
    /// 
    /// </summary
    [Fact]
    public void CanMapByteTypeToUTinyInt()
    {
      using (MySqlConnection connection = new MySqlConnection(st.GetConnectionString(true)))
      {
        MySqlProviderManifest pm = new MySqlProviderManifest(st.Version.ToString());
        TypeUsage tu = TypeUsage.CreateDefaultTypeUsage(
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Byte));
        TypeUsage result = pm.GetStoreType(tu);
        Assert.Equal("utinyint", result.EdmType.Name);

      }
    }

    [Fact]
    public void TestingMaxLengthWithFixedLenghtTrueFacets()
    {
      using (MySqlConnection connection = new MySqlConnection(st.GetConnectionString(true)))
      {
        MySqlProviderManifest pm = new MySqlProviderManifest(st.Version.ToString());
        TypeUsage tu = TypeUsage.CreateStringTypeUsage(
          PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), false, true);
        TypeUsage result = pm.GetStoreType(tu);
        Assert.Equal("char", result.EdmType.Name);

        tu = TypeUsage.CreateStringTypeUsage(
          PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), false, true, Int32.MaxValue);
        result = pm.GetStoreType(tu);
        Assert.Equal("char", result.EdmType.Name);

        tu = TypeUsage.CreateStringTypeUsage(
          PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), false, true, 70000);
        result = pm.GetStoreType(tu);
        Assert.Equal("char", result.EdmType.Name);

      }
    }

  }
}
