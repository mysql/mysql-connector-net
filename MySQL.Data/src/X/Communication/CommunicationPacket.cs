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

namespace MySqlX.Communication
{
  internal class CommunicationPacket
  {

    public CommunicationPacket(int messageType, int length, byte[] data)
    {
      MessageType = messageType;
      Length = length;
      Buffer = data;
    }

    public byte[] Buffer;
    public int MessageType;
    public int Length;
  }

  internal enum ClientMessageId
  {
    CON_CAPABILITIES_GET = 1,
    CON_CAPABILITIES_SET = 2,
    CON_CLOSE = 3,

    SESS_AUTHENTICATE_START = 4,
    SESS_AUTHENTICATE_CONTINUE  = 5,
    SESS_RESET = 6,
    SESS_CLOSE = 7,

    SQL_STMT_EXECUTE = 12,

    CRUD_FIND = 17,
    CRUD_INSERT = 18,
    CRUD_UPDATE = 19,
    CRUD_DELETE = 20,

    EXPECT_OPEN = 24,
    EXPECT_CLOSE = 25
  }


  internal enum ServerMessageId
  {
    OK = 0,
    ERROR = 1,

    CONN_CAPABILITIES = 2,

    SESS_AUTHENTICATE_CONTINUE = 3,
    SESS_AUTHENTICATE_OK = 4,

    // NOTICE has to stay at 11 forever
    NOTICE = 11,

    RESULTSET_COLUMN_META_DATA = 12,
    RESULTSET_ROW = 13,
    RESULTSET_FETCH_DONE = 14,
    RESULTSET_FETCH_SUSPENDED = 15,
    RESULTSET_FETCH_DONE_MORE_RESULTSETS = 16,

    SQL_STMT_EXECUTE_OK = 17,
    RESULTSET_FETCH_DONE_MORE_OUT_PARAMS = 18
  }

}
