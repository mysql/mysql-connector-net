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
using MySql.Communication;
using MySql.Data;
using MySql.DataAccess;
using MySql.Procotol;
using MySql.Security;
using Mysqlx.Resultset;
using Mysqlx.Session;
using Mysqlx.Sql;
using System;
using System.Collections.Generic;
using System.Text;
using MySql.XDevAPI;
using MySql.Protocol.X;
using crud = Mysqlx.Crud;
using Mysqlx.Expr;
using System.IO;
using Mysqlx.Datatypes;
using Mysqlx;

namespace MySql
{
  internal class XProtocol : ProtocolBase<UniversalStream>
  {
    private CommunicationPacket pendingPacket;

    public XProtocol(MySqlConnectionStringBuilder sessionSettings, string authMode)
      : base(sessionSettings, authMode)
    {  }


    public override void OpenConnection()
    {
      try
      {
        UniversalStream._networkStream = CommunicationTcp.CreateStream(settings, false);       
      }
      catch (System.Security.SecurityException)
      {
        throw;
      }
      catch
      {
        throw new Exception("Unable to connect to host");
      }

      if (UniversalStream._networkStream == null)
        throw new Exception("Unable to connect to host");


      settings.CharacterSet = String.IsNullOrWhiteSpace(settings.CharacterSet) ? "UTF-8" : settings.CharacterSet;

      var encoding = Encoding.GetEncoding(settings.CharacterSet);

      baseStream = new CommunicationTcp(UniversalStream._networkStream, encoding, false);

      if (_authMode.Equals("MYSQL41"))
      {
        authenticationPlugin = new Mysql41Authentication(baseStream, settings);
      }      

      if (authenticationPlugin.AuthenticationMode == AuthenticationMode.MySQL41)
      {
        AuthenticateStart.Builder authStart = AuthenticateStart.CreateBuilder();       
        authStart.SetMechName("MYSQL41");
        AuthenticateStart message = authStart.Build();
        SendPacket(message, (int)ClientMessageId.SESS_AUTHENTICATE_START);

        CommunicationPacket packet = ReadPacket();

        if (packet.MessageType == (int)ServerMessageId.SESS_AUTHENTICATE_CONTINUE)
        {
          AuthenticateContinue response = AuthenticateContinue.ParseFrom(packet.Buffer);
          AuthenticateOk authOK = authenticationPlugin.Authenticate(false, response);
          if (authOK == null)
          {
            throw new MySqlException("Failed authentication");
          }
        }
      }      
    }

    public override void CloseConnection()
    {
      throw new NotImplementedException();
    }

    public override void ExecuteBatch()
    {
      throw new NotImplementedException();
    }

    public override void Delete()
    {
      throw new NotImplementedException();
    }

    public override void ExecutePrepareStatement()
    {
      throw new NotImplementedException();
    }

    private CommunicationPacket ReadPacket()
    {
      while (true)
      {
        CommunicationPacket p = pendingPacket != null ? pendingPacket : baseStream.Read();
        pendingPacket = null;
        if (p.MessageType != (int)ServerMessageId.NOTICE) return p;
        ProcessNotice(p);
      }
    }

    private void ProcessNotice(CommunicationPacket packet)
    {

    }

    private Any CreateAny(object arg)
    {
      if (arg is string)
      {
        Scalar.Types.String stringValue = Scalar.Types.String.CreateBuilder().SetValue(ByteString.CopyFromUtf8(arg as string)).Build();
        Scalar scalarValue = Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_STRING).SetVString(stringValue).Build();
        return Any.CreateBuilder().SetType(Any.Types.Type.SCALAR).SetScalar(scalarValue).Build();
      }
      ///TODO: handle other data types
      return null;
    }

    public override void SendExecuteStatement(string ns, string stmt, params object[] args)
    {
      StmtExecute.Builder stmtExecute = StmtExecute.CreateBuilder();
      stmtExecute.SetNamespace(ns);
      stmtExecute.SetStmt(ByteString.CopyFromUtf8(stmt));
      stmtExecute.SetCompactMetadata(false);
      foreach (object arg in args)
        stmtExecute.AddArgs(CreateAny(arg));
      StmtExecute msg = stmtExecute.Build();

      SendPacket(msg, (int)ClientMessageId.SQL_STMT_EXECUTE);
    }

    public override Result ReadStmtExecuteResult()
    {
      Result r = new Result();
      CommunicationPacket p = ReadPacket();
      if (p.MessageType == (int)ServerMessageId.ERROR)
      {
        Error e = Error.ParseFrom(p.Buffer);
        Result.Error re = new Result.Error();
        re.Code = e.Code;
        re.SqlState = e.SqlState;
        re.Message = e.Msg;
        r.ErrorInfo = re;
      }
      else if (p.MessageType != (int)ServerMessageId.SQL_STMT_EXECUTE_OK)
        throw new MySqlException("Unexpected message type: " + p.MessageType);
      return r;
    }

    public override List<byte[]> ReadRow()
    {
      CommunicationPacket packet = ReadPacket();
      ///TODO:  handle this
      if (packet.MessageType != (int)ServerMessageId.RESULTSET_ROW) return null;

      Row protoRow = Row.ParseFrom(packet.Buffer);
      List<byte[]> values = new List<byte[]>(protoRow.FieldCount);
      for (int i = 0; i < protoRow.FieldCount; i++)
        values.Add(protoRow.GetField(i).ToByteArray());
      return values;
    }

    public override ResultSet ReadResultSet()
    {
      CommunicationPacket packet = ReadMessageHeader(ServerMessageId.RESULTSET_COLUMN_META_DATA);
      ResultSet resultSet = new ResultSet();

      // read the metadata
      while (packet.MessageType == (int)ServerMessageId.RESULTSET_COLUMN_META_DATA)
      {
        ColumnMetaData response = ColumnMetaData.ParseFrom(packet.Buffer);
        resultSet.AddColumn(DecodeColumn(response));
        packet = ReadPacket();
      }

      // if we have no rows then we are done
      if (packet.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE) return resultSet;

      if (packet.MessageType == (int)ServerMessageId.RESULTSET_ROW)
        pendingPacket = packet;
      resultSet.Protocol = this;
      return resultSet;
    }

    private Column DecodeColumn(ColumnMetaData colData)
    {
      Column c = new Column();
      c._decoder = XValueDecoderFactory.GetValueDecoder(colData.Type);
      c._decoder.Column = c;

      if (colData.HasName)
        c.Name = colData.Name.ToStringUtf8();
      if (colData.HasOriginalName)
        c.OriginalName = colData.OriginalName.ToStringUtf8();
      if (colData.HasTable)
        c.Table = colData.Table.ToStringUtf8();
      if (colData.HasOriginalTable)
        c.OriginalTable = colData.OriginalTable.ToStringUtf8();
      if (colData.HasSchema)
        c.Schema = colData.Schema.ToStringUtf8();
      if (colData.HasCatalog)
        c.Catalog = colData.Catalog.ToStringUtf8();
      if (colData.HasCollation)
      {
        c._collationNumber = colData.Collation;
        c.Collation = CollationMap.GetCollationName((int)colData.Collation);
      }
      if (colData.HasLength)
        c.Length = colData.Length;
      if (colData.HasFractionalDigits)
        c.FractionalDigits = colData.FractionalDigits;
      if (colData.HasFlags)
        c._decoder.Flags = colData.Flags;
      c._decoder.DecodeMetadata();
      return c;
    }

    public override void ExecuteReader()
    {
      throw new NotImplementedException();
    }

    public ResultSet Find(SelectStatement statement)
    {
      ResultSet result = new ResultSet();

      Mysqlx.Crud.Find.Builder builder = Mysqlx.Crud.Find.CreateBuilder();
      // :param collection:
      builder.SetCollection(Mysqlx.Crud.Collection.CreateBuilder()
        .SetSchema(statement.schema)
        .SetName(statement.table));
      // :param data_model:
      builder.SetDataModel(statement.isTable ? crud.DataModel.TABLE : crud.DataModel.DOCUMENT);
      // :param projection:
      foreach (string columnName in statement.columns)
      {
        builder.AddProjection(crud.Projection.CreateBuilder()
          .SetSource(Expr.CreateBuilder()
            .SetType(Expr.Types.Type.IDENT)
            .SetIdentifier(ColumnIdentifier.CreateBuilder()
              .SetName(columnName))));
      }
      // :param criteria:

      // :param limit:

      // :param order:

      // :param grouping:
      
      // :param grouping_criteria:


      Mysqlx.Crud.Find message = builder.Build();
      SendPacket(message, (int)ClientMessageId.CRUD_FIND);
      return ReadResultSet();
    }

    public override void Update()
    {
      throw new NotImplementedException();
    }

    public override void Insert()
    {
      throw new NotImplementedException();
    }

    public override void Reset()
    {
      throw new NotImplementedException();
    }

    internal void SendPacket(IMessageLite message, int messageId)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        int size = message.SerializedSize + 1;
        stream.Write(BitConverter.GetBytes(size), 0, 4);
        stream.WriteByte((byte)messageId);
        message.WriteTo(stream);
        stream.Flush();

        baseStream.SendPacket(stream.ToArray());
      }
    }

    internal CommunicationPacket ReadMessageHeader(ServerMessageId expectedServerMessage)
    {
      CommunicationPacket packet = ReadPacket();
      if (packet.MessageType == (int)ServerMessageId.ERROR)
      {
        Mysqlx.Error errorMessage = Mysqlx.Error.ParseFrom(packet.Buffer);
        throw new MySqlException(errorMessage.Msg);
      }

      if (packet.MessageType != (int)expectedServerMessage)
        throw new MySqlException(string.Format("Expected {0} message but was received {1}",
          Enum.GetName(typeof(ServerMessageId), expectedServerMessage),
          Enum.GetName(typeof(ServerMessageId), packet.MessageType)));

      return packet;
    }
  }
}
