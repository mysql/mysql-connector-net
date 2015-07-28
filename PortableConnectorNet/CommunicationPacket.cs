using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Communication
{
  internal class CommunicationPacket
  {
    private byte[] _tempBuffer = new byte[256];
    private MemoryStream _buffer = new MemoryStream();

    #region Properties
    
    public int Position
    {
      get { return (int)_buffer.Position; }
      set { _buffer.Position = (long)value; }
    }

    public int Length
    {
      get { return (int)_buffer.Length; }
      set { _buffer.SetLength(value); }
    }
   
    public byte[] Buffer
    {
      get {
        return _buffer.GetBuffer().Take(Length).ToArray();
      }        
    }

    public int MessageType { get; set; }

    #endregion


    #region String Methods
    #endregion

    #region Byte Methods
    public byte ReadByte()
    {
      return (byte)_buffer.ReadByte();
    }

    public int Read(byte[] byteBuffer, int offset, int count)
    {
      return _buffer.Read(byteBuffer, offset, count);
    }


    public void WriteByte(byte b)
    {
      _buffer.WriteByte(b);
    }

    public void Write(byte[] bytesToWrite)
    {
      Write(bytesToWrite, 0, bytesToWrite.Length);
    }

    public void Write(byte[] bytesToWrite, int offset, int countToWrite)
    {
      _buffer.Write(bytesToWrite, offset, countToWrite);
    }

    public void SetByte(long position, byte value)
    {
      long currentPosition = _buffer.Position;
      _buffer.Position = position;
      _buffer.WriteByte(value);
      _buffer.Position = currentPosition;
    }

    #endregion

    #region Integer Methods
    #endregion



  }
}
