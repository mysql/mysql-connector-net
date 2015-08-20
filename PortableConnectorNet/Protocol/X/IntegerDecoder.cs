using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Protocol.X
{
  internal class IntegerDecoder : ValueDecoder
  {
    public override void DecodeMetadata()
    {
    }

    public override object GetClrValue(byte[] value)
    {
      return null;
    }
  }
}
