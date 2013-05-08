using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySql.Data.MySqlClient.Tests.Xunit
{
  
  public class EntityFrameworkTests
  {
    [Fact]
    public void TestEntityMediumTrust()
    {
      using (sakilaEntities context = new sakilaEntities())
      {
        var actor = context.actors.Where(t => t.actor_id == 4).First();
        Console.WriteLine("actor name " + actor.first_name);
      }
    }
  }
}
