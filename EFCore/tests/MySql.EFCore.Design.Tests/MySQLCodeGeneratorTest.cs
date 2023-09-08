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
using Microsoft.EntityFrameworkCore.Scaffolding;
using MySql.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Scaffolding.Internal;
using NUnit.Framework;
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

      Assert.AreEqual("UseMySQL", result.Method);
      Assert.That(result.Arguments, Has.Exactly(1).EqualTo("Data Source=Test"));
      Assert.Null(result.ChainedCall);
    }

    [Test]
    public virtual void UseProviderMethodIsGeneratedCorrectlyWithOptions()
    {
      var codeGenerator = new MySQLCodeGenerator(
          new ProviderCodeGeneratorDependencies(
              Enumerable.Empty<IProviderCodeGeneratorPlugin>()));

      var providerOptions = new MethodCallCodeFragment(UseMySqlServerMethodInfo);

      var result = codeGenerator.GenerateUseProvider("Data Source=Test", providerOptions);

      Assert.AreEqual("UseMySQL", result.Method);
      Assert.That(result.Arguments, Has.Exactly(1).EqualTo("Data Source=Test"));
      Assert.IsInstanceOf<NestedClosureCodeFragment>(result.Arguments[1]);
      NestedClosureCodeFragment nestedClosure = (NestedClosureCodeFragment)result.Arguments[1]!;
      Assert.AreEqual("x", nestedClosure?.Parameter);
      Assert.AreSame(providerOptions, nestedClosure?.MethodCalls[0]);
      Assert.Null(result.ChainedCall);
    }

    private static readonly MethodInfo UseMySqlServerMethodInfo
      = typeof(MySQLDbContextOptionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbContextOptionsExtensions.UseMySQL),
        new[] { typeof(DbContextOptionsBuilder), typeof(string), typeof(Action<MySQLDbContextOptionsBuilder>) })!;
  }
}
