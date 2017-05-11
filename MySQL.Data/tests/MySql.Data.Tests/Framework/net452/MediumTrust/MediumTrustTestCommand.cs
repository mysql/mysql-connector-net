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
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace MySql.Data.MySqlClient.Tests.Xunit.MediumTrust
{
  public class MediumTrustTestCommand: ITestCommand
  {
    private readonly ITestCommand _command;
    private readonly IDictionary<MethodInfo, object> _fixtures;


    public MediumTrustTestCommand(ITestCommand command,IDictionary<MethodInfo, object> fixtures)
    {
      this._command = command;
      this._fixtures = fixtures;
    }

    public string DisplayName
    {
      get { return _command.DisplayName; }
    }

    public bool ShouldCreateInstance
    {
      get { return _command.ShouldCreateInstance; }
    }

    public System.Xml.XmlNode ToStartXml()
    {
      return _command.ToStartXml();
    }

    public int Timeout
    {
      get { return _command.Timeout; }
    }

    public MethodResult Execute(object testClass)
    {
      try
      {
        if (testClass == null) return null;

        var testClassType = testClass.GetType();

        if (!typeof(MarshalByRefObject).IsAssignableFrom(testClassType))
        {
          throw new InvalidOperationException(
              string.Format("Test class attribute '{0}' must derive from MarshalByRefObject.",
                  testClassType.FullName));
        }

        object sandboxedClass = null;

        var mediumTrustSandbox = new MediumTrustDomain();
        var partialTrustDomain = mediumTrustSandbox.CreatePartialTrustAppDomain();

        sandboxedClass = partialTrustDomain.CreateInstanceAndUnwrap(testClassType.Assembly.FullName, testClassType.FullName);

        if (_fixtures != null)
        {
          foreach (var fixture in _fixtures)
          {
            fixture.Key.Invoke(sandboxedClass, new object[] { fixture.Value });
          }
        }

        var result = _command.Execute(sandboxedClass);
        mediumTrustSandbox.Dispose();
        return result;
      }
      catch (Exception ex)
      {
        if (ex.Message.Equals("Assembly is still loading"))
        { 
          //This case is when our assembly was not found.
        }
      }

      return null;
    }

  }
}
