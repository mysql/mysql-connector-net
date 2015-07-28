using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql;

namespace PortableConnectorNetTests
{
    public class ConnectionTest
    {
      public void TestConnection()
      {
        Session session = new Session("connectionstring");
        session.Open();  
      }

    //ResultSet result  =  session.SendQuery("Select * from table");
    //foreach (NextRow row in result)
    //{
    //  ///read files
    //}
    }
}
