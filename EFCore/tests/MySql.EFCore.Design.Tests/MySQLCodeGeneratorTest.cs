// Copyright (c) 2020, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using MySql.Data.EntityFrameworkCore.Scaffolding.Internal;
using System.Linq;
using Xunit;

namespace MySql.EntityFrameworkCore.Design.Tests
{
  public class MySQLCodeGeneratorTest
  {
    [Fact]
    public virtual void UseProviderMethodIsGeneratedCorrectly()
    {
      var codeGenerator = new MySQLCodeGenerator(
          new ProviderCodeGeneratorDependencies(
              Enumerable.Empty<IProviderCodeGeneratorPlugin>()));

      var result = codeGenerator.GenerateUseProvider("Data Source=Test", providerOptions: null);

      Assert.Equal("UseMySQL", result.Method);
      Assert.Collection(
          result.Arguments,
          a => Assert.Equal("Data Source=Test", a));
      Assert.Null(result.ChainedCall);
    }

    [Fact]
    public virtual void UseProviderMethodIsGeneratedCorrectlyWithOptions()
    {
      var codeGenerator = new MySQLCodeGenerator(
          new ProviderCodeGeneratorDependencies(
              Enumerable.Empty<IProviderCodeGeneratorPlugin>()));

      var providerOptions = new MethodCallCodeFragment("SetProviderOption");

      var result = codeGenerator.GenerateUseProvider("Data Source=Test", providerOptions);

      Assert.Equal("UseMySQL", result.Method);
      Assert.Collection(
          result.Arguments,
          a => Assert.Equal("Data Source=Test", a),
          a =>
          {
            var nestedClosure = Assert.IsType<NestedClosureCodeFragment>(a);

            Assert.Equal("x", nestedClosure.Parameter);
            Assert.Same(providerOptions, nestedClosure.MethodCall);
          });
      Assert.Null(result.ChainedCall);
    }
  }
}
