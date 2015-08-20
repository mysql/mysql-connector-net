// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using Google.ProtocolBuffers;
using MySql.Common;
using System.IO;
using System.Net.Sockets;
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
    //TODO check if we need this
    bool _isClosed;
    internal Stream _baseStream;
    internal Stream _inStream;
    internal Stream _outStream;
    internal static NetworkStream _networkStream;
    protected byte[] _header = new byte[5];
    protected Encoding _encoding;
    internal CommunicationPacket packet;

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

    internal abstract void SendPacket(byte[] bytes);
     
    //protected void StartTimer(IOKind op);

    //protected void StopTimer();


  }

  public enum IOKind
  {
    Read,
    Write
  };
}
