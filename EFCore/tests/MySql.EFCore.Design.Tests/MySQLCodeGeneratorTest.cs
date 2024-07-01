// Copyright © 2021, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using Microsoft.EntityFrameworkCore.Scaffolding;
using MySql.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Scaffolding.Internal;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Design.Tests
{
  public class MySQLCodeGeneratorTest
  {
    [Test]
    public virtual void UseProviderMethodIsGeneratedCorrectly()
    {
      var codeGenerator = new MySQLCodeGenerator(
          new ProviderCodeGeneratorDependencies(
              Enumerable.Empty<IProviderCodeGeneratorPlugin>()));

      var result = codeGenerator.GenerateUseProvider("Data Source=Test", providerOptions: null);

      Assert.That(result.Method, Is.EqualTo("UseMySQL"));
      Assert.That(result.Arguments, Has.Exactly(1).EqualTo("Data Source=Test"));
      Assert.That(result.ChainedCall, Is.Null);
    }

    [Test]
    public virtual void UseProviderMethodIsGeneratedCorrectlyWithOptions()
    {
      var codeGenerator = new MySQLCodeGenerator(
          new ProviderCodeGeneratorDependencies(
              Enumerable.Empty<IProviderCodeGeneratorPlugin>()));

      var providerOptions = new MethodCallCodeFragment(UseMySqlServerMethodInfo);

      var result = codeGenerator.GenerateUseProvider("Data Source=Test", providerOptions);

      Assert.That(result.Method, Is.EqualTo("UseMySQL"));
      Assert.That(result.Arguments, Has.Exactly(1).EqualTo("Data Source=Test"));
      Assert.That(result.Arguments[1], Is.InstanceOf<NestedClosureCodeFragment>());
      NestedClosureCodeFragment nestedClosure = (NestedClosureCodeFragment)result.Arguments[1]!;
      Assert.That(nestedClosure?.Parameter, Is.EqualTo("x"));
      Assert.That(nestedClosure?.MethodCalls[0], Is.SameAs(providerOptions));
      Assert.That(result.ChainedCall, Is.Null);
    }

    private static readonly MethodInfo UseMySqlServerMethodInfo
      = typeof(MySQLDbContextOptionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbContextOptionsExtensions.UseMySQL),
        new[] { typeof(DbContextOptionsBuilder), typeof(string), typeof(Action<MySQLDbContextOptionsBuilder>) })!;
  }
}
