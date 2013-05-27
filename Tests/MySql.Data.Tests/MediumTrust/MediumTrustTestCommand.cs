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
