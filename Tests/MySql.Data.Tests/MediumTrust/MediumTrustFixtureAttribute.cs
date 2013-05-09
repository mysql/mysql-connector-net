using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySql.Data.MySqlClient.Tests.Xunit
{
  class MediumTrustFixtureAttribute : RunWithAttribute
  {
    public MediumTrustFixtureAttribute()
      : base(typeof(MediumTrustTestClassCommand))
    { }
  }
}
