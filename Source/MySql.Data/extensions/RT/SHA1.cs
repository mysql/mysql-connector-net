using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace System.Security.Cryptography
{
  abstract class SHA1
  {
    public abstract byte[] ComputeHash(byte[] buffer);
  }

  class SHA1CryptoServiceProvider : SHA1
  {
    public override byte[] ComputeHash(byte[] buffer)
    {
      HashAlgorithmProvider sha1Provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
      IBuffer bufferIn = CryptographicBuffer.CreateFromByteArray(buffer);
      IBuffer bufferHash = sha1Provider.HashData(bufferIn);
      if (bufferHash.Length != sha1Provider.HashLength)
        throw new FormatException("There was an error creating the hash");

      byte[] bufferOut = new byte[bufferHash.Length];
      CryptographicBuffer.CopyToByteArray(bufferHash, out bufferOut);

      return bufferOut;
    }
  }
}
