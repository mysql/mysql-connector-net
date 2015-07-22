using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableConnectorNet
{
  internal abstract class UniversalStream
  {
    	public abstract void Read();
	    public abstract void Write();
  }
}
