using MySql.Common;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MySql.Communication
{
  internal abstract class UniversalStream
  {

    protected int _length;
    protected int _maxBlockSize;
    protected ulong _maxPacketSize;
    protected int _timeout;
    protected int _lastReadTimeout;
    protected int _lastWriteTimeout;
    LowResolutionStopwatch _stopwatch;
    bool _isClosed;
    protected Stream _baseStream;
    protected Stream _inStream;
    protected Stream _outStream;
    protected byte[] _header = new byte[5];
    protected Encoding _encoding;


    public abstract bool CanRead
    {
      get;
    }

    public abstract bool CanSeek
    {
      get;
    }

    public abstract bool CanWrite
    {
      get;
    }

    public UniversalStream()
    {
      _isClosed = false;
      _stopwatch = new LowResolutionStopwatch();
    }

    public abstract CommunicationPacket Read();
    public abstract void Write();

    public abstract void Flush();

    public abstract void Close();    
     
    //protected void StartTimer(IOKind op);

    //protected void StopTimer();


  }

  public enum IOKind
  {
    Read,
    Write
  };
}
