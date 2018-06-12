// Copyright (c) 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;
using System.Data.Entity.Core.Metadata.Edm;
using Xunit;

namespace MySql.Data.EntityFramework.Tests
{
  public class ProviderManifestTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public ProviderManifestTests(DefaultFixture data)
    {
      st = data;
      st.Setup(this.GetType());
    }

    [Fact]
    public void TestingMaxLengthFacet()
    {
      using (MySqlConnection connection = new MySqlConnection(st.ConnectionString))
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
    /// Bug #62135 Connector/NET Incorrectly Maps PrimitiveTypeKind.Byte to "tinyint"
    /// 
    /// </summary
    [Fact]
    public void CanMapByteTypeToUTinyInt()
    {
      using (MySqlConnection connection = new MySqlConnection(st.ConnectionString))
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
      using (MySqlConnection connection = new MySqlConnection(st.ConnectionString))
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
