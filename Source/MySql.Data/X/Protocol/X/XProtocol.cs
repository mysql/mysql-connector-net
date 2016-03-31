// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Communication;
using MySqlX.Data;
using Mysqlx.Resultset;
using Mysqlx.Session;
using Mysqlx.Sql;
using MySqlX.Protocol.X;
using Mysqlx.Expr;
using Mysqlx.Datatypes;
using Mysqlx;
using Mysqlx.Crud;
using Mysqlx.Notice;
using Mysqlx.Connection;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using MySqlX.XDevAPI.CRUD;
using MySql.Data.MySqlClient;
using MySqlX.Properties;

namespace MySqlX.Protocol
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
      switch ((ServerMessageId)p.MessageType)
      {
        case ServerMessageId.SESS_AUTHENTICATE_OK:
          break;

        case ServerMessageId.ERROR:
          var error = Error.ParseFrom(p.Buffer);
          throw new MySqlException("Unable to connect: " + error.Msg);

        case ServerMessageId.NOTICE:
          ///TODO:  fix this
          //ProcessNotice(p, new Result(null));
          ReadAuthOk();
          break;

        default:
          throw new MySqlException("Unexpected message encountered during authentication handshake");
      }
    }

    #endregion

    public void GetServerCapabilities()
    {
      _writer.Write(ClientMessageId.CON_CAPABILITIES_GET, CapabilitiesGet.CreateBuilder().Build());
      CommunicationPacket packet = ReadPacket();
      if (packet.MessageType != (int)ServerMessageId.CONN_CAPABILITIES)
        ThrowUnexpectedMessage(packet.MessageType, (int)ServerMessageId.CONN_CAPABILITIES);
      Capabilities caps = Capabilities.ParseFrom(packet.Buffer);
      foreach (Capability cap in caps.Capabilities_List)
      {
        if (cap.Name == "authentication.mechanism")
        {
        }
      }
    }

    public void SetCapabilities()
    {
      //var builder = Capabilities.CreateBuilder();
      //var cap = Capability.CreateBuilder().SetName("tls").SetValue(ExprUtil.BuildAny("1")).Build();
      //builder.AddCapabilities_(cap);
      //_writer.Write(ClientMessageId.CON_CAPABILITIES_SET, builder.Build());
      //while (true)
      //{
      //  CommunicationPacket p = ReadPacket();
      //  Error e = Error.ParseFrom(p.Buffer);
      //}
    }

    private void ThrowUnexpectedMessage(int received, int expected)
    {
      throw new MySqlException(
        String.Format("Expected message id: {0}.  Received message id: {1}", expected, received));
    }

    public override void SendSQL(string sql, params object[] args)
    {
      SendExecuteStatement("sql", sql, args);
    }

    public override bool HasData()
    {
      return PeekPacket().MessageType == (int)ServerMessageId.RESULTSET_COLUMN_META_DATA;
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

    private void ProcessNotice(CommunicationPacket packet, BaseResult rs)
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

    private void ProcessSessionStateChanged(BaseResult rs, byte[] payload)
    {
      SessionStateChanged state = SessionStateChanged.ParseFrom(payload);
      switch (state.Param)
      {
        case SessionStateChanged.Types.Parameter.ROWS_AFFECTED:
            rs._recordsAffected = state.Value.VUnsignedInt;
          break;
        case SessionStateChanged.Types.Parameter.GENERATED_INSERT_ID:
            rs._autoIncrementValue = state.Value.VUnsignedInt;
          break;
          // handle the other ones
//      default: SessionStateChanged(state);
      }
    }

    private void ProcessWarning(BaseResult rs, byte[] payload)
    {
      Warning w = Warning.ParseFrom(payload);
      WarningInfo warning = new WarningInfo(w.Code, w.Msg);
      if (w.HasLevel)
        warning.Level = (uint)w.Level;
      rs.AddWarning(warning);
    }

    private Any CreateAny(object o)
    {
      if (o is string) return ExprUtil.BuildAny((string)o);
      if (o is bool) return ExprUtil.BuildAny((bool)o);
      return ExprUtil.BuildAny(o);
    }

    public void SendSessionClose()
    {
      _writer.Write(ClientMessageId.SESS_CLOSE, Mysqlx.Session.Close.CreateBuilder().Build());
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


    private void DecodeAndThrowError(CommunicationPacket p)
    {
      Error e = Error.ParseFrom(p.Buffer);
      throw new MySqlException(e.Code, e.SqlState, e.Msg);
    }

    public override List<byte[]> ReadRow(BaseResult rs)
    {
      CommunicationPacket packet = PeekPacket();
      if (packet.MessageType != (int)ServerMessageId.RESULTSET_ROW)
      {
        if (rs != null)
          CloseResult(rs);
        return null;
      }

      Mysqlx.Resultset.Row protoRow = Mysqlx.Resultset.Row.ParseFrom(ReadPacket().Buffer);
      List<byte[]> values = new List<byte[]>(protoRow.FieldCount);
      for (int i = 0; i < protoRow.FieldCount; i++)
        values.Add(protoRow.GetField(i).ToByteArray());
      return values;
    }

    public override void CloseResult(BaseResult rs)
    {
      rs._hasData = false;
      while (true)
      {
        CommunicationPacket p = PeekPacket();
        if (p.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE_MORE_RESULTSETS)
        {
          rs._hasMoreResults = true;
          ReadPacket();
          break;
        }
        if (p.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE)
          ReadPacket();
        else if (p.MessageType == (int)ServerMessageId.NOTICE)
          ProcessNotice(ReadPacket(), rs);
        else if (p.MessageType == (int)ServerMessageId.ERROR)
          DecodeAndThrowError(ReadPacket());
        else if (p.MessageType == (int)ServerMessageId.SQL_STMT_EXECUTE_OK)
        {
          ReadPacket();
          break;
        }
        else
          throw new MySqlException(Properties.ResourcesX.ThrowingAwayResults);
      }
    }

    public override List<XDevAPI.Relational.Column> LoadColumnMetadata()
    {
      List<XDevAPI.Relational.Column> columns = new List<XDevAPI.Relational.Column>();
      // we assume our caller has already validated that metadata is there
      while (true)
      {
        if (PeekPacket().MessageType != (int)ServerMessageId.RESULTSET_COLUMN_META_DATA) return columns;
        CommunicationPacket p = ReadPacket();
        ColumnMetaData response = ColumnMetaData.ParseFrom(p.Buffer);
        columns.Add(DecodeColumn(response));
      }
    }

    private XDevAPI.Relational.Column DecodeColumn(ColumnMetaData colData)
    {
      XDevAPI.Relational.Column c = new XDevAPI.Relational.Column();
      c._decoder = XValueDecoderFactory.GetValueDecoder(c, colData.Type);
      c._decoder.Column = c;
      
      if (!colData.Name.IsEmpty)
        c.ColumnLabel = colData.Name.ToStringUtf8();
      if (!colData.OriginalName.IsEmpty)
        c.ColumnName = colData.OriginalName.ToStringUtf8();
      if (!colData.Table.IsEmpty)
        c.TableLabel = colData.Table.ToStringUtf8();
      if (!colData.OriginalTable.IsEmpty)
        c.TableName = colData.OriginalTable.ToStringUtf8();
      if (!colData.Schema.IsEmpty)
        c.SchemaName = colData.Schema.ToStringUtf8();
      if (!colData.Catalog.IsEmpty)
        c.DatabaseName = colData.Catalog.ToStringUtf8();
      if (colData.Collation > 0)
      {
        c._collationNumber = colData.Collation;
        c.CollationName = CollationMap.GetCollationName((int)colData.Collation);
        c.CharacterSetName = c.CollationName.Split('_')[0];
      }
      if (colData.Length > 0)
        c.Length = colData.Length;
      if (colData.FractionalDigits > 0)
        c.FractionalDigits = colData.FractionalDigits;
      if (colData.Flags > 0)
        c._decoder.Flags = colData.Flags;
      if (colData.ContentType > 0)
        c._decoder.ContentType = colData.ContentType;
      c._decoder.SetMetadata();
      return c;
    }

    public void SendInsert(string schema, bool isRelational, string collection, object[] rows, string[] columns)
    {
      Insert.Builder builder = Mysqlx.Crud.Insert.CreateBuilder().SetCollection(ExprUtil.BuildCollection(schema, collection));
      builder.SetDataModel(isRelational ? DataModel.TABLE : DataModel.DOCUMENT);
      if(columns != null && columns.Length > 0)
      {
        foreach(string column in columns)
        {
          builder.AddProjection(new ExprParser(column).ParseTableInsertField());
        }
      }
      foreach (object row in rows)
      {
        Mysqlx.Crud.Insert.Types.TypedRow.Builder typedRow = Mysqlx.Crud.Insert.Types.TypedRow.CreateBuilder();
        object[] fields = row.GetType().IsArray ? (object[])row : new object[] { row };
        foreach (object field in fields)
        {
          typedRow.AddField(ExprUtil.ArgObjectToExpr(field, isRelational)).Build();
        }

        builder.AddRow(typedRow);
      }
      Mysqlx.Crud.Insert msg = builder.Build();
      _writer.Write(ClientMessageId.CRUD_INSERT, msg);
    }

    private void ApplyFilter<T>(Func<Limit, T> setLimit, Func<Expr, T> setCriteria, Func<IEnumerable<Order>,T> setOrder, FilterParams filter, Func<IEnumerable<Scalar>,T> addParams)
    {
      if (filter.HasLimit)
      {
        var limit = Limit.CreateBuilder().SetRowCount((ulong)filter.Limit);
        if (filter.Offset != -1) limit.SetOffset((ulong)filter.Offset);
        setLimit(limit.Build());
      }
      if (!string.IsNullOrEmpty(filter.Condition))
      {
        setCriteria(filter.GetConditionExpression(filter.IsRelational));
        if (filter.Parameters != null && filter.Parameters.Count > 0)
          addParams(filter.GetArgsExpression(filter.Parameters));
      }
      if (filter.OrderBy != null && filter.OrderBy.Length > 0)
        setOrder(filter.GetOrderByExpressions(filter.IsRelational));

    }

    /// <summary>
    /// Sends the delete documents message
    /// </summary>
    public void SendDelete(string schema, string collection, bool isRelational, FilterParams filter)
    {
      var builder = Delete.CreateBuilder();
      builder.SetDataModel(isRelational ? DataModel.TABLE : DataModel.DOCUMENT);
      builder.SetCollection(ExprUtil.BuildCollection(schema, collection));
      ApplyFilter(builder.SetLimit, builder.SetCriteria, builder.AddRangeOrder, filter, builder.AddRangeArgs);
      var msg = builder.Build();
      _writer.Write(ClientMessageId.CRUD_DELETE, msg);
    }

    /// <summary>
    /// Sends the CRUD modify message
    /// </summary>
    public void SendUpdate(string schema, string collection, bool isRelational, FilterParams filter, List<UpdateSpec> updates)
    {
      var builder = Update.CreateBuilder();
      builder.SetDataModel(isRelational ? DataModel.TABLE : DataModel.DOCUMENT);
      builder.SetCollection(ExprUtil.BuildCollection(schema, collection));
      ApplyFilter(builder.SetLimit, builder.SetCriteria, builder.AddRangeOrder, filter, builder.AddRangeArgs);

      foreach (var update in updates)
      {
        var updateBuilder = UpdateOperation.CreateBuilder();
        updateBuilder.SetOperation(update.Type);
        updateBuilder.SetSource(update.GetSource(isRelational));
        if (update.HasValue)
          updateBuilder.SetValue(update.GetValue());
        builder.AddOperation(updateBuilder.Build());
      }
      var msg = builder.Build();
      _writer.Write(ClientMessageId.CRUD_UPDATE, msg);
    }

    public void SendFind(string schema, string collection, bool isRelational, FilterParams filter, FindParams findParams)
    {
      var builder = Find.CreateBuilder().SetCollection(ExprUtil.BuildCollection(schema, collection));
      builder.SetDataModel(isRelational ? DataModel.TABLE : DataModel.DOCUMENT);
      if (findParams != null && findParams.Projection != null && findParams.Projection.Length > 0)
        builder.AddRangeProjection(new ExprParser(ExprUtil.JoinString(findParams.Projection)).ParseTableSelectProjection());
      ApplyFilter(builder.SetLimit, builder.SetCriteria, builder.AddRangeOrder, filter, builder.AddRangeArgs);
      _writer.Write(ClientMessageId.CRUD_FIND, builder.Build());
    }

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
