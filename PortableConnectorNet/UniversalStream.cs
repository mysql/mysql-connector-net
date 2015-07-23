using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableConnectorNet
{
  internal abstract class UniversalStream
  {
    private int _length;

    private byte[] packetHeader = new byte[5];

    public int PackageLenght
    {
      get
      {
        return _length;
      }
    }

    public UniversalStream()
    {
      _length = 0;
    }

    	public abstract void Read();
	    public abstract void Write();
  }
}
