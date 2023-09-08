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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MySql.EntityFrameworkCore.Design.Internal;
using MySql.EntityFrameworkCore.Internal;
using MySql.EntityFrameworkCore.Metadata.Conventions;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Storage.Internal;
using NUnit.Framework;
using System.Linq;

namespace MySql.EntityFrameworkCore.Design.Tests
{
  public class MySQLAnnotationCodeGeneratorTest
  {
    [TestCase(MySQLAnnotationNames.Charset)]
    [TestCase(MySQLAnnotationNames.Collation)]
    public void GenerateFluentApiHasCharset(string mySQLAnnotation)
    {
      var typeMappingSource = new MySQLTypeMappingSource(
                      TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
                      TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
                      TestServiceFactory.Instance.Create<MySQLOptions>());

      var generator = new MySQLAnnotationCodeGenerator(new AnnotationCodeGeneratorDependencies(typeMappingSource));
      var modelBuilder = new ModelBuilder(MySQLConventionSetBuilder.Build());
      modelBuilder.Entity(
          "Post",
          x =>
          {
            x.Property<int>("Id").HasAnnotation(mySQLAnnotation, "utf8mb4");
          });

      var key = modelBuilder.Model.FindEntityType("Post")?.GetProperties().Single();
      var annotation = key?.FindAnnotation(mySQLAnnotation);
      var result = MySQLAnnotationCodeGenerator.GenFluentApi((Microsoft.EntityFrameworkCore.Metadata.IProperty)key!, annotation!);

      Assert.AreEqual(mySQLAnnotation == MySQLAnnotationNames.Charset ? "ForMySQLHasCharset" : "ForMySQLHasCollation", result?.Method);
      Assert.AreEqual(1, result?.Arguments.Count);
    }
  }
}
