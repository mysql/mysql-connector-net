// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
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
