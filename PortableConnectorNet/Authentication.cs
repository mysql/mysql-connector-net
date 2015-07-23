using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableConnectorNet
{
 
internal abstract class AuthenticationBase : IDisposable
{
	public abstract void Authenticate();
	protected abstract byte[] GetPassword(string password, byte[] seedBytes);

  public void Dispose()
  { }
}


internal class Mysql41Authentication : AuthenticationBase
{
  public override void Authenticate()
  { 
  }
  protected override byte[] GetPassword(string password, byte[] seedBytes)
  { 
     return new byte[1];
  }
}

  internal class PlainAuthentication : AuthenticationBase
  {
    public override void Authenticate()
    { 
    }
    protected override byte[] GetPassword(string password, byte[] seedBytes)
    {
      return new byte[1];
    }
  }

}
