using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableConnectorNet
{
  internal class Session
  {
    ConnectionStringBuilder _csb;
    RoutingServiceFabric _rounting;
    XConnection _currentConnetion;
    ProtocolInstance _protocol;
    PlainAuthentication _authMethod;

    public Session(string connectionString)
    {
      _csb = new ConnectionStringBuilder(connectionString);
      _authMethod = new PlainAuthentication();
      _rounting = new RoutingServiceFabric();
      _protocol = new ProtocolInstance();
    }

    public void Open()
    {
       _currentConnetion =  _rounting.GetCurrentConnection(mode);
       _currentConnetion.Open();     
    }

    public void Find(collection)
    {}


    public void Query(sqlStmt)
    {}

    public void Execute()
    {}

  }
}
