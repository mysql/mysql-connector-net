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
}


internal class Mysql41Authentication : AuthenticationBase
{
	public void Authenticate();
	protected byte[] GetPassword(string password, byte[] seedBytes);
}

  internal class PlainAuthentication : AuthenticationBase
  {
	  public void Authenticate();
    protected byte[] GetPassword(string password, byte[] seedBytes);
  }

}
