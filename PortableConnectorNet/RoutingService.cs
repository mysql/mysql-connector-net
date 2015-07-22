using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableConnectorNet
{
  internal abstract class RoutingService
  {
    public abstract ConnectionBase GetCurrentConnection(int mode);

  }


  internal class RoutingServiceFabric
  { 
  
  }
}
