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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.ProtocolBuffers;
using MySql.Communication;
using MySql.Data;
using Mysqlx.Resultset;
using Mysqlx.Session;
using Mysqlx.Sql;
using MySql.XDevAPI;
using MySql.Protocol.X;
using Mysqlx.Expr;
using Mysqlx.Datatypes;
using Mysqlx;
using Mysqlx.Crud;
using Mysqlx.Notice;
using MySql.XDevAPI.Statements;
using MySql.XDevAPI.Results;
using MySQL.Common;

namespace MySql.Protocol
{
  internal class XProtocol : ProtocolBase
  {
    private CommunicationPacket pendingPacket;
    private XPacketReaderWriter _reader;
    private XPacketReaderWriter _writer;

    public XProtocol(XPacketReaderWriter reader, XPacketReaderWriter writer)
    {
      _reader = reader;
      _writer = writer;
    }

    #region Authentication

    public void SendAuthStart(string method)
    {
      AuthenticateStart.Builder authStart = AuthenticateStart.CreateBuilder();
      authStart.SetMechName(method);
      AuthenticateStart message = authStart.Build();
      _writer.Write(ClientMessageId.SESS_AUTHENTICATE_START, message);
    }

    public byte[] ReadAuthContinue()
    {
      CommunicationPacket p = ReadPacket();
      if (p.MessageType != (int)ServerMessageId.SESS_AUTHENTICATE_CONTINUE)
        throw new MySqlException("Unexpected message encountered during authentication handshake");
      AuthenticateContinue response = AuthenticateContinue.ParseFrom(p.Buffer);
      if (response.HasAuthData)
        return response.AuthData.ToByteArray();
      return null;
    }

    public void SendAuthContinue(byte[] data)
    {
      Debug.Assert(data != null);
      AuthenticateContinue.Builder builder = AuthenticateContinue.CreateBuilder();
      builder.SetAuthData(ByteString.CopyFrom(data));
      AuthenticateContinue authCont = builder.Build();
      _writer.Write(ClientMessageId.SESS_AUTHENTICATE_CONTINUE, authCont);
    }

    public void ReadAuthOk()
    {
      CommunicationPacket p = ReadPacket();
      if (p.MessageType == (int)ServerMessageId.ERROR)
      {
        var error = Error.ParseFrom(p.Buffer);
        throw new MySqlException("Unable to connect: " + error.Msg);
      }
      if (p.MessageType != (int)ServerMessageId.SESS_AUTHENTICATE_OK)
        throw new MySqlException("Unexpected message encountered during authentication handshake");
    }

    #endregion

    public override void SendSQL(string sql)
    {
      SendExecuteStatement("sql", sql, null);
    }



    private CommunicationPacket PeekPacket()
    {
      if (pendingPacket != null) return pendingPacket;
      pendingPacket = _reader.Read();
      return pendingPacket;
    }

    private CommunicationPacket ReadPacket()
    {
      while (true)
      {
        CommunicationPacket p = pendingPacket != null ? pendingPacket : _reader.Read();
        pendingPacket = null;
        return p;
      }
    }

    private void ProcessGlobalNotice(Mysqlx.Notice.Frame frame)
    {
    }

    private void ProcessNotice(CommunicationPacket packet, Result rs)
    {
      Frame frame = Frame.ParseFrom(packet.Buffer);
      if (frame.Scope == Frame.Types.Scope.GLOBAL)
      {
        ProcessGlobalNotice(frame);
        return;
      }

      // if we get here the notice is local
      switch ((NoticeType)frame.Type)
      {
        case NoticeType.Warning:
          ProcessWarning(rs, frame.Payload.ToByteArray());
          break;
        case NoticeType.SessionStateChanged:
          ProcessSessionStateChanged(rs, frame.Payload.ToByteArray());
          break;
        case NoticeType.SessionVariableChanged:
          break;
      }
    }

    private void ProcessSessionStateChanged(Result rs, byte[] payload)
    {
      SessionStateChanged state = SessionStateChanged.ParseFrom(payload);
      switch (state.Param)
      {
        case SessionStateChanged.Types.Parameter.ROWS_AFFECTED:
          Tools.EnsureType(rs, typeof(UpdateResult));
          (rs as UpdateResult).RecordsAffected = state.Value.VUnsignedInt;
          break;
        case SessionStateChanged.Types.Parameter.GENERATED_INSERT_ID:
          Tools.EnsureType(rs, typeof(UpdateResult));
          (rs as UpdateResult).LastInsertId = state.Value.VUnsignedInt;
          break;
          // handle the other ones
//      default: SessionStateChanged(state);
      }
    }

    private void ProcessWarning(Result rs, byte[] payload)
    {
      Warning w = Warning.ParseFrom(payload);
      Result.Warning warning = new Result.Warning(w.Code, w.Msg);
      if (w.HasLevel)
        warning.Level = (uint)w.Level;
      rs.AddWarning(warning);
    }

    private Any CreateAny(object o)
    {
      if (o is string) return ExprUtil.BuildAny((string)o);
      if (o is bool) return ExprUtil.BuildAny((bool)o);
      return null;
    }

    public void SendSessionClose()
    {
      Close.Builder builder = Close.CreateBuilder();
      Mysqlx.Session.Close sessionClose = builder.Build();
      _writer.Write(ClientMessageId.SESS_CLOSE, sessionClose);
    }

    public void SendConnectionClose()
    {
      Mysqlx.Connection.Close.Builder builder = Mysqlx.Connection.Close.CreateBuilder();
      Mysqlx.Connection.Close connClose = builder.Build();
      _writer.Write(ClientMessageId.CON_CLOSE, connClose);
    }

    public void SendExecuteStatement(string ns, string stmt, params object[] args)
    {
      StmtExecute.Builder stmtExecute = StmtExecute.CreateBuilder();
      stmtExecute.SetNamespace(ns);
      stmtExecute.SetStmt(ByteString.CopyFromUtf8(stmt));
      stmtExecute.SetCompactMetadata(false);
      if (args != null)
      {
        foreach (object arg in args)
          stmtExecute.AddArgs(CreateAny(arg));
      }
      StmtExecute msg = stmtExecute.Build();

      _writer.Write(ClientMessageId.SQL_STMT_EXECUTE, msg);
    }


    private Result.Error DecodeError(CommunicationPacket p)
    {
      Error e = Error.ParseFrom(p.Buffer);
      Result.Error re = new Result.Error();
      re.Code = e.Code;
      re.SqlState = e.SqlState;
      re.Message = e.Msg;
      return re;
    }

    public override List<byte[]> ReadRow()
    {
      if (PeekPacket().MessageType != (int)ServerMessageId.RESULTSET_ROW) return null;

      Row protoRow = Row.ParseFrom(ReadPacket().Buffer);
      List<byte[]> values = new List<byte[]>(protoRow.FieldCount);
      for (int i = 0; i < protoRow.FieldCount; i++)
        values.Add(protoRow.GetField(i).ToByteArray());
      return values;
    }

    public override void CloseResult(Result rs)
    {
      while (true)
      {
        CommunicationPacket p = PeekPacket();
        if (p.MessageType == (int)ServerMessageId.NOTICE)
          ProcessNotice(ReadPacket(), rs);
        else if (p.MessageType == (int)ServerMessageId.ERROR)
        {
          rs.ErrorInfo = DecodeError(ReadPacket());
          break;
        }
        else if (p.MessageType == (int)ServerMessageId.SQL_STMT_EXECUTE_OK)
        {
          ReadPacket();
          break;
        }
      }
    }

    public override Result GetNextResult()
    {
      CommunicationPacket p = PeekPacket();
      if (p.MessageType == (int)ServerMessageId.RESULTSET_COLUMN_META_DATA)
        return new TableResult(this);
      if (p.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE)
      {
        ReadPacket();
        return null; 
      }
      if (p.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE_MORE_RESULTSETS)
      {
        ReadPacket();
        return new TableResult(this);
      }
      return null;
    }

    //public override bool HasAnotherResultSet()
    //{
    //  CommunicationPacket p = PeekPacket();
    //  if (p.MessageType == (int)ServerMessageId.RESULTSET_COLUMN_META_DATA) return true;
    //  if (p.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE)
    //  {
    //    ReadPacket();
    //    return false;
    //  }
    //  if (p.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE_MORE_RESULTSETS)
    //  {
    //    ReadPacket();
    //    return true;
    //  }
    //  return false;
    //}

    public override List<TableColumn> LoadColumnMetadata()
    {
      List<TableColumn> columns = new List<TableColumn>();
      // we assume our caller has already validated that metadata is there
      while (true)
      {
        if (PeekPacket().MessageType != (int)ServerMessageId.RESULTSET_COLUMN_META_DATA) return columns;
        CommunicationPacket p = ReadPacket();
        ColumnMetaData response = ColumnMetaData.ParseFrom(p.Buffer);
        columns.Add(DecodeColumn(response));
      }
    }

    private TableColumn DecodeColumn(ColumnMetaData colData)
    {
      TableColumn c = new TableColumn();
      c._decoder = XValueDecoderFactory.GetValueDecoder(c, colData.Type);
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
      c._decoder.SetMetadata();
      return c;
    }

    public void SendInsert(string schema, string collection, JsonDoc[] rows)
    {
      Insert.Builder builder = Mysqlx.Crud.Insert.CreateBuilder().SetCollection(ExprUtil.BuildCollection(schema, collection));
      foreach (JsonDoc row in rows)
      {
        Mysqlx.Crud.Insert.Types.TypedRow typedRow = Mysqlx.Crud.Insert.Types.TypedRow.CreateBuilder().AddField(ExprUtil.ArgObjectToExpr(row.ToString(), false)).Build();
        builder.AddRow(typedRow);
      }
      Mysqlx.Crud.Insert msg = builder.Build();
      _writer.Write(ClientMessageId.CRUD_INSERT, msg);
    }

    private void ApplyFilter<T>(Func<Limit, T> setLimit, Func<Expr, T> setCriteria, FilterParams filter)
    {
      if (filter.HasLimit)
      {
        var limit = Limit.CreateBuilder().SetRowCount((ulong)filter.Limit).Build();
        setLimit(limit);
      }
      if (filter.HasCondition)
      {
        setCriteria(filter.GetConditionExpression(false));
      }
    }

    /// <summary>
    /// Sends the delete documents message
    /// </summary>
    public void SendDocDelete(string schema, string collection, FilterParams filter)
    {
      var builder = Delete.CreateBuilder();
      builder.SetCollection(ExprUtil.BuildCollection(schema, collection));
      ApplyFilter(builder.SetLimit, builder.SetCriteria, filter);
      var msg = builder.Build();
      _writer.Write(ClientMessageId.CRUD_DELETE, msg);
    }

    /// <summary>
    /// Sends the CRUD modify message
    /// </summary>
    public void SendDocModify(string schema, string collection, FilterParams filter, List<UpdateSpec> updates)
    {
      var builder = Update.CreateBuilder();
      builder.SetCollection(ExprUtil.BuildCollection(schema, collection));
      ApplyFilter(builder.SetLimit, builder.SetCriteria, filter);

      foreach (var update in updates)
      {
        var updateBuilder = UpdateOperation.CreateBuilder();
        updateBuilder.SetOperation(update.Type);
        updateBuilder.SetSource(update.GetSource());
        if (update.HasValue)
          updateBuilder.SetValue(update.GetValue());
        builder.AddOperation(updateBuilder.Build());
      }
      var msg = builder.Build();
      _writer.Write(ClientMessageId.CRUD_UPDATE, msg);
    }

    public void SendFind(string schema, string collection, bool isRelational, FilterParams filter)
    {
      var builder = Find.CreateBuilder().SetCollection(ExprUtil.BuildCollection(schema, collection));
      builder.SetDataModel(isRelational ? DataModel.TABLE : DataModel.DOCUMENT);
      ApplyFilter(builder.SetLimit, builder.SetCriteria, filter);
      _writer.Write(ClientMessageId.CRUD_FIND, builder.Build());
    }

    //public void SendFind(SelectStatement statement)
    //{
    //  Result result = new Result(this);

    //  Mysqlx.Crud.Find.Builder builder = Mysqlx.Crud.Find.CreateBuilder();
    //  // :param collection:
    //  builder.SetCollection(Mysqlx.Crud.Collection.CreateBuilder()
    //    .SetSchema(statement.schema)
    //    .SetName(statement.table));
    //  // :param data_model:
    //  builder.SetDataModel(statement.isTable ? DataModel.TABLE : DataModel.DOCUMENT);
    //  // :param projection:
    //  foreach (string columnName in statement.columns)
    //  {
    //    builder.AddProjection(Projection.CreateBuilder()
    //      .SetSource(Expr.CreateBuilder()
    //        .SetType(Expr.Types.Type.IDENT)
    //        .SetIdentifier(ColumnIdentifier.CreateBuilder()
    //          .SetName(columnName))));
    //  }
    //  // :param criteria:
    //  builder.SetCriteria(Expr.CreateBuilder()
    //    .SetType(Expr.Types.Type.LITERAL)
    //    .SetVariable(statement.where));
    //  // :param args:

    //  // :param limit:

    //  // :param order:

    //  // :param grouping:

    //  // :param grouping_criteria:


    //  Mysqlx.Crud.Find message = builder.Build();
    //  _writer.Write(ClientMessageId.CRUD_FIND, message);
    //}

    internal void ReadOK()
    {
      CommunicationPacket p = ReadPacket();
      if (p.MessageType == (int)ServerMessageId.ERROR)
      {
        var error = Error.ParseFrom(p.Buffer);
        throw new MySqlException("Received error when closing session: " + error.Msg);
      }
      if (p.MessageType == (int)ServerMessageId.OK)
      {
        var response = Ok.ParseFrom(p.Buffer);
        if (!(response.Msg.IndexOf("bye", 0 , StringComparison.InvariantCultureIgnoreCase) < 0))
        return;
      }
      throw new MySqlException("Unexpected message encountered during closing session");
    }
  }
}
