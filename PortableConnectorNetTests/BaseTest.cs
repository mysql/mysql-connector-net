using MySql.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableConnectorNetTests
{
    public class BaseTest
    {
        protected Session GetSession()
        {
            return MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1");
        }
    }
}
