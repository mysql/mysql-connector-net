using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Sdk;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient.Tests;

namespace MySql.Data.MySqlClient.Tests.Xunit
{

 [RunWith(typeof(MediumTrustTestClassCommand))] 
 [MediumTrustFixture]
 public class MySqlMediumTrustTests : MarshalByRefObject
  {   
   
    [Fact]
    public void TestConnectionStrings()
    {      
      MySqlConnection c = new MySqlConnection();
      c.ConnectionString = "server=localhost;userid=root;database=mysql;port=3305;includesecurityasserts=true;";
      c.Open();
      c.Close();     
    }
  }
}
