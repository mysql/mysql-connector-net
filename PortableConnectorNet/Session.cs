using MySql.Communication;
using MySql.Routing;
using MySql.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql
{
  internal class Session
  {
    ConnectionStringBuilder _csb;
    RoutingToServer _rounting;
    XConnection _currentConnetion;
    //ProtocolInstance _protocol;
    PlainAuthentication _authMethod;
    int _mode;

    public Session(string connectionString)
    {
      _csb = new ConnectionStringBuilder(connectionString);
      _authMethod = new PlainAuthentication();
      _rounting = new RoutingToServer();
      _mode = 0;
      //_protocol = new ProtocolInstance();
    }

    public void Open()
    {
       _currentConnetion = (XConnection)_rounting.GetCurrentConnection(_mode);
       //_currentConnetion.Open();     
    }

    //public void Find(collection)
    //{}


    //public void Query(sqlStmt)
    //{}

    public void Execute()
    {}




  }
}
