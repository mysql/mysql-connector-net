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
using System;
using System.IO;

namespace MySqlX.Communication
{
  internal class XPacketReaderWriter
  {
    private Stream _stream;

    public XPacketReaderWriter(Stream stream)
    {
      _stream = stream;
    }

    public void Write(ClientMessageId id, IMessageLite message)
    {
      int size = message.SerializedSize + 1;
      _stream.Write(BitConverter.GetBytes(size), 0, 4);
      _stream.WriteByte((byte)id);
      message.WriteTo(_stream);
      _stream.Flush();
    }

    public CommunicationPacket Read()
    {
      byte[] header = new byte[5];
      ReadFully(header, 0, 5);
      int length = BitConverter.ToInt32(header, 0);
      byte[] data = new byte[length - 1];
      ReadFully(data, 0, length - 1);
      return new CommunicationPacket(header[4], length - 1, data);
    }

    void ReadFully(byte[] buffer, int offset, int count)
    {
      int numRead = 0;
      int numToRead = count;
      while (numToRead > 0)
      {
        int read = _stream.Read(buffer, offset + numRead, numToRead);
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
