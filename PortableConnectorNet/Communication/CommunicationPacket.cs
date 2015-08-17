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

using System.IO;
using System.Linq;

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

  public enum ClientMessageId
  {
    ConnectionGetCapabilities = 1,
    ConnectionSetCapabilities = 2,
    ConnectionClose = 3,
    AuthenticateStart = 4,
    AuthenticateContinue = 5,
    SqlPrepareStmt = 6,
    SqlPreparedStmtExecute = 7,
    SqlCursorFetchRows = 8,
    SqlCursorFetchMetaData = 9,
    SqlCursorClose = 10,
    SqlCursorsPoll = 11,
    CrudPrepareFind = 12,
    CrudPrepareInsert = 13,
    CrudPrepareUpdate = 14,
    CrudPrepareDelete = 15,
    CrudPrepareStmtClose = 16,
    SessionReset = 17,
    SessionClose = 18
  }


  public enum ServerMessageId
  {
    Ok = 1,
    Error = 2,
    Notice = 3,
    ParameterChangedNotification = 4,
    ConnectionCapabilities = 5,
    AuthenticateContinue = 6,
    AuthenticateOk = 7,
    AuthenticateFail = 8,
    SqlPrepareStmtOk = 9,
    SqlPreparedStmtExecuteOk = 10,
    SqlColumnMetaData = 11,
    SqlRow = 12,
    SqlCursorFetchDone = 13,
    SqlCursorFetchSuspended = 14,
    SqlCursorsPoll = 15,
    SqlCursorCloseOk = 16,
    SqlCursorFetchDoneMoreResultsets = 17
  }

}
