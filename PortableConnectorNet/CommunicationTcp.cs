using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Communication
{
  internal class CommunicationTcp : UniversalStream
  {
    internal CommunicationPacket packet;

    public override bool CanRead
    {
      get { return _baseStream.CanRead; }
    }

    public override bool CanSeek
    {
      get { return _baseStream.CanSeek; }
    }

    public override bool CanWrite
    {
      get { return _baseStream.CanWrite; }
    }



    public CommunicationTcp()
    {
      _maxPacketSize = ulong.MaxValue;
      _maxBlockSize = Int32.MaxValue;
    }

    public CommunicationTcp(Stream stream, Encoding encoding, bool compress = false)
      : this()
    {
      _baseStream = stream;
      _encoding = encoding;
      _inStream = new BufferedStream(stream);
      _outStream = stream;      
    }


    public override CommunicationPacket Read()
    {
      LoadPacket();

      return packet;
    }


    public override void Write()
    {
      throw new NotImplementedException();
    }


    public override void Close()
    {
      throw new NotImplementedException();
    }

    public override void Flush()
    {
      throw new NotImplementedException();
    }

    private void LoadPacket()
    {
      try
      {
        _length = 0;
        int offset = 0;
        while (true)
        {
          ReadFully(_inStream, _header, offset, _header.Length);
          if (!BitConverter.IsLittleEndian)
          {
            System.Array.Reverse(_header);
          }

          _length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_header, 0));

          packet.MessageType = _header[4];
          var tempBuffer = new Byte[_length - _header.Length];
          ReadFully(_inStream, tempBuffer, offset, _length - _header.Length);
          packet.Write(tempBuffer);

          // if this block was < maxBlock then it's last one in a multipacket series
          if (_length < _maxBlockSize) break;
          offset += _length;

        }
      }
      catch (Exception)
      {
        
        throw;
      }
    }

    internal static void ReadFully(Stream stream, byte[] buffer, int offset, int count)
    {
      int numRead = 0;
      int numToRead = count;
      while (numToRead > 0)
      {
        int read = stream.Read(buffer, offset + numRead, numToRead);
        if (read == 0)
        {
          throw new EndOfStreamException();
        }
        numRead += read;
        numToRead -= read;
      }
    }
  }
}
