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
