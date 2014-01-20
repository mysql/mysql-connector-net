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
using System.Text;
using MySql.Data.MySqlClient.Tests.Xunit.MediumTrust;
using Xunit.Sdk;

namespace MySql.Data.MySqlClient.Tests.Xunit
{
  class MediumTrustTestClassCommand : ITestClassCommand
  {

    readonly TestClassCommand _cmd = new TestClassCommand();
    Random randomizer = new Random();

    #region ITestClassCommand Members
    public object ObjectUnderTest
    {
      get { return _cmd.ObjectUnderTest; }
    }

    public ITypeInfo TypeUnderTest
    {
      get { return _cmd.TypeUnderTest; }
      set { _cmd.TypeUnderTest = value; }
    }

    public int ChooseNextTest(ICollection<IMethodInfo> testsLeftToRun)
    {
      return randomizer.Next(testsLeftToRun.Count);
    }

    public Exception ClassFinish()
    {
      return _cmd.ClassFinish();
    }

    public Exception ClassStart()
    {
      return _cmd.ClassStart();
    }

    public IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo testMethod)
    {
      foreach (var testCommand in _cmd.EnumerateTestCommands(testMethod))
      {
        if (testCommand is MediumTrustTestCommand)
        {
          yield return testCommand;
          continue;
        }

        yield return new MediumTrustTestCommand(testCommand, null);
      }
    }

    public bool IsTestMethod(IMethodInfo testMethod)
    {
      return _cmd.IsTestMethod(testMethod);
    }

    public IEnumerable<IMethodInfo> EnumerateTestMethods()
    {
      return _cmd.EnumerateTestMethods();
    }
    #endregion
  }
}
