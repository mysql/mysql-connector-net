// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Storage;
using System;
using Xunit;

namespace EntityFrameworkCore.Basic.Tests
{
    public class MySQLTypeMapperTests : RelationalTypeMapperTestBase
    {

        [Fact]
        public void SimpleMappingsToDDLTypes()
        {
            Assert.Equal("datetimeoffset", GetTypeMapping(typeof(DateTimeOffset)).StoreType);
        }

        private static RelationalTypeMapping GetTypeMapping(
          Type propertyType,
          bool? nullable = null,
          int? maxLength = null,
          bool? unicode = null)
        {
            //var property = CreateEntityType().AddProperty("MyProp", propertyType);

            //if (nullable.HasValue)
            //{
            //    property.IsNullable = nullable.Value;
            //}

            //if (maxLength.HasValue)
            //{
            //    property.SetMaxLength(maxLength);
            //}

            //if (unicode.HasValue)
            //{
            //    property.IsUnicode(unicode);
            //}

            //return new MySQLTypeMapper().GetMapping();

            return null;
        }

    }   
 }

public class RelationalTypeMapperTestBase
{
    //protected static EntityType CreateEntityType()
    //    => (EntityType)CreateModel().FindEntityType(typeof(MyType));


    //protected static IModel CreateModel()
    //{
    //    var modelBuilder = MySql.Data.EntityFrameworkCore.TestsContextUtils.Instance.CreateModelBuilder();
    //    buildAction(modelBuilder);


    //    var builder = TestHelpers.Instance.CreateConventionBuilder();

    //}
}



