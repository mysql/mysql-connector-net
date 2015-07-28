using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Routing
{
  internal abstract class RoutingService
  {
    public abstract ConnectionBase GetCurrentConnection(int mode);

  }


  internal class RoutingServiceFabric
  { 
    
  }


  internal class RoutingToServer : RoutingService
  {
    public override ConnectionBase GetCurrentConnection(int mode)
    {
      throw new NotImplementedException();
    }  
  }
}
