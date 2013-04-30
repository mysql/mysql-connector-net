using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Sdk;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient.Tests.XUnit;

namespace MySql.Data.MySqlClient.Tests.Xunit
{

 [RunWith(typeof(MediumTrustTestClassCommand))] 
 [MediumTrustFixture]
 public class MySqlMediumTrustTests : MarshalByRefObject, IDisposable
  {

   private SetUpClass st { get; set; }

   private string db {get; set;}
   
   public MySqlMediumTrustTests()
   {    
     st = new SetUpClass();   
   }

    [Fact]
    public void TestConnectionStrings()
    {
      if(st != null)  st.suExecSQL("Create database Medium_TrustDB");
      MySqlConnection c = new MySqlConnection();
      c.ConnectionString = "server=localhost;userid=root;database=Medium_TrustDB;port=3305;includesecurityasserts=true;";
      c.Open();
      c.Close();
      st.suExecSQL("Drop database Medium_TrustDB");
    }

    public void Dispose()
    {
     if (st != null)
      st.Dispose();
      // TODO: to add more operations when disposing  
    }
  }
}
