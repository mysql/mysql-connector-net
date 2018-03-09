// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
using MySql.Data;
using MySqlX;
using System.IO;
using Google.Protobuf;
using static Mysqlx.Datatypes.Object.Types;

namespace MySqlX.Protocol
{
  internal class XProtocol : ProtocolBase
  {
    private CommunicationPacket pendingPacket;
    private XPacketReaderWriter _reader;
    private XPacketReaderWriter _writer;

    public Capabilities Capabilities { get; protected set; }

    public XProtocol(XPacketReaderWriter reader, XPacketReaderWriter writer)
    {
      _reader = reader;
      _writer = writer;
    }

    #region Authentication

    public void SendAuthStart(string method, byte[] authData, byte[] initialResponse)
    {
      AuthenticateStart authStart = new AuthenticateStart();
      authStart.MechName = method;
      if (authData != null) authStart.AuthData = (ByteString.CopyFrom(authData));
      if (initialResponse != null) authStart.InitialResponse = (ByteString.CopyFrom(initialResponse));
      _writer.Write(ClientMessageId.SESS_AUTHENTICATE_START, authStart);
    }

    public byte[] ReadAuthContinue()
    {
      CommunicationPacket p = ReadPacket();
      if (p.MessageType != (int)ServerMessageId.SESS_AUTHENTICATE_CONTINUE)
        throw new MySqlException("Unexpected message encountered during authentication handshake");
      AuthenticateContinue response = AuthenticateContinue.Parser.ParseFrom(p.Buffer);
      if (!response.AuthData.IsEmpty)
        return response.AuthData.ToByteArray();
      return null;
    }

    public void SendAuthContinue(byte[] data)
    {
      Debug.Assert(data != null);
      AuthenticateContinue authCont = new AuthenticateContinue();
      authCont.AuthData = (ByteString.CopyFrom(data));
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
          var error = Error.Parser.ParseFrom(p.Buffer);
          throw new MySqlException(error.Code, error.SqlState, error.Msg);

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
      _writer.Write(ClientMessageId.CON_CAPABILITIES_GET, new CapabilitiesGet());
      CommunicationPacket packet = ReadPacket();
      if (packet.MessageType != (int)ServerMessageId.CONN_CAPABILITIES)
        ThrowUnexpectedMessage(packet.MessageType, (int)ServerMessageId.CONN_CAPABILITIES);
      Capabilities = Capabilities.Parser.ParseFrom(packet.Buffer);
    }

    public void SetCapabilities(Dictionary<string,object> clientCapabilities)
    {
      if (clientCapabilities == null || clientCapabilities.Count == 0)
        return;

      var builder = new CapabilitiesSet();
      var capabilities = new Capabilities();
      foreach(var cap in clientCapabilities)
      {
        var capabilityMsg = new Capability() { Name = (cap.Key), Value = ExprUtil.BuildAny(cap.Value) };
        capabilities.Capabilities_.Add(capabilityMsg);
      }
      builder.Capabilities = capabilities;
      _writer.Write(ClientMessageId.CON_CAPABILITIES_SET, builder);
      ReadOk();
    }

    private void ThrowUnexpectedMessage(int received, int expected)
    {
      throw new MySqlException(
        String.Format("Expected message id: {0}.  Received message id: {1}", expected, received));
    }

    public override void SendSQL(string sql, params object[] args)
    {
      SendExecuteSQLStatement(sql, args);
    }

    public override bool HasData(BaseResult rs)
    {
      while (true)
      {
        CommunicationPacket packet = PeekPacket();
        switch (packet.MessageType)
        {
          case (int)ServerMessageId.RESULTSET_COLUMN_META_DATA:
            return true;
          case (int)ServerMessageId.NOTICE:
            ProcessNotice(packet, rs);
            packet = ReadPacket();
            break;
          case (int)ServerMessageId.ERROR:
            packet = ReadPacket();
            DecodeAndThrowError(packet);
            break;
          default:
            return false;
        }
      }
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
      Frame frame = Frame.Parser.ParseFrom(packet.Buffer);
      if (frame.Scope == Frame.Types.Scope.Global)
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
      SessionStateChanged state = SessionStateChanged.Parser.ParseFrom(payload);
      switch (state.Param)
      {
        case SessionStateChanged.Types.Parameter.RowsAffected:
            rs._recordsAffected = state.Value[0].VUnsignedInt;
          break;
        case SessionStateChanged.Types.Parameter.GeneratedInsertId:
            rs._autoIncrementValue = state.Value[0].VUnsignedInt;
          break;
        case SessionStateChanged.Types.Parameter.ProducedMessage:
          rs.AddWarning(new WarningInfo(0, state.Value[0].VString.Value.ToStringUtf8()));
          break;
        case SessionStateChanged.Types.Parameter.GeneratedDocumentIds:
          foreach (var value in state.Value)
            rs._documentIds.Add(value.VOctets.Value.ToStringUtf8());
          break;
          // handle the other ones
//      default: SessionStateChanged(state);
      }
    }

    private void ProcessWarning(BaseResult rs, byte[] payload)
    {
      Warning w = Warning.Parser.ParseFrom(payload);
      WarningInfo warning = new WarningInfo(w.Code, w.Msg);
      if (w.Level != Warning.Types.Level.None)
        warning.Level = (uint)w.Level;
      rs.AddWarning(warning);
    }

    private Any CreateAny(object o)
    {
      if (o is string) return ExprUtil.BuildAny((string)o);
      if (o is bool) return ExprUtil.BuildAny((bool)o);
      return ExprUtil.BuildAny(o);
    }

    private ObjectField CreateObject(string key, object value, bool evaluateStringExpression = true)
    {
      return ExprUtil.BuildObject(key, value, evaluateStringExpression);
    }

    public void SendSessionClose()
    {
      _writer.Write(ClientMessageId.SESS_CLOSE, new Mysqlx.Session.Close());
    }

    public void SendConnectionClose()
    {
      Mysqlx.Connection.Close connClose = new Mysqlx.Connection.Close();
      _writer.Write(ClientMessageId.CON_CLOSE, connClose);
    }

    public void SendExecuteSQLStatement(string stmt, params object[] args)
    {
      StmtExecute stmtExecute = new StmtExecute();
      stmtExecute.Namespace = "sql";
      stmtExecute.Stmt = ByteString.CopyFromUtf8(stmt);
      stmtExecute.CompactMetadata = false;
      if (args != null)
      {
        foreach (object arg in args)
          stmtExecute.Args.Add(CreateAny(arg));
      }

      _writer.Write(ClientMessageId.SQL_STMT_EXECUTE, stmtExecute);
    }

    public void SendExecuteStatement(string ns, string stmt, params KeyValuePair<string, object>[] args)
    {
      StmtExecute stmtExecute = new StmtExecute();
      stmtExecute.Namespace = ns;
      stmtExecute.Stmt = ByteString.CopyFromUtf8(stmt);
      stmtExecute.CompactMetadata = false;
      if (args != null)
      {
        var any = ExprUtil.BuildEmptyAny(Any.Types.Type.Object);
        foreach (var arg in args)
        {
          switch(stmt)
          {
            case "drop_collection_index":
              any.Obj.Fld.Add(CreateObject(arg.Key, arg.Value, false));
              break;
            default:
              any.Obj.Fld.Add(CreateObject(arg.Key, arg.Value));
              break;
          }
        }

        stmtExecute.Args.Add(any);
      }

      _writer.Write(ClientMessageId.SQL_STMT_EXECUTE, stmtExecute);
    }

    public void SendCreateCollectionIndexStatement(string ns, string stmt, params KeyValuePair<string, object>[] args)
    {
      StmtExecute stmtExecute = new StmtExecute();
      stmtExecute.Namespace = ns;
      stmtExecute.Stmt = ByteString.CopyFromUtf8(stmt);
      stmtExecute.CompactMetadata = false;
      if (args != null)
      {
        var any = ExprUtil.BuildEmptyAny(Any.Types.Type.Object);
        var array = new Mysqlx.Datatypes.Array();
        foreach (var arg in args)
        {
          if (arg.Value is Dictionary<string, object> && arg.Key=="constraint")
          {
            var innerAny = ExprUtil.BuildEmptyAny(Any.Types.Type.Object);
            foreach (var field in arg.Value as Dictionary<string, object>)
              innerAny.Obj.Fld.Add(CreateObject(field.Key, field.Value, field.Key == "member" ? true : false));

            array.Value.Add(innerAny);
          }
          else
            any.Obj.Fld.Add(CreateObject(arg.Key, arg.Value, false));
        }

        if (array.Value.Count>0)
        {
          var constraint = new ObjectField();
          constraint.Key = "constraint";
          var constraintAny = ExprUtil.BuildEmptyAny(Any.Types.Type.Array);
          constraintAny.Array = array;
          constraint.Value = constraintAny;
          any.Obj.Fld.Add(constraint);
        }

        stmtExecute.Args.Add(any);
      }

      _writer.Write(ClientMessageId.SQL_STMT_EXECUTE, stmtExecute);
    }


    private void DecodeAndThrowError(CommunicationPacket p)
    {
      Error e = Error.Parser.ParseFrom(p.Buffer);
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

      Mysqlx.Resultset.Row protoRow = Mysqlx.Resultset.Row.Parser.ParseFrom(ReadPacket().Buffer);
      List<byte[]> values = new List<byte[]>(protoRow.Field.Count);
      for (int i = 0; i < protoRow.Field.Count; i++)
        values.Add(protoRow.Field[i].ToByteArray());
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
        if (p.MessageType == (int)ServerMessageId.OK)
        {
          ReadOk();
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
          throw new MySqlException(ResourcesX.ThrowingAwayResults);
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
        ColumnMetaData response = ColumnMetaData.Parser.ParseFrom(p.Buffer);
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

    public void SendInsert(string schema, bool isRelational, string collection, object[] rows, string[] columns, bool upsert)
    {
      Insert msg = new Mysqlx.Crud.Insert();
      msg.Collection = ExprUtil.BuildCollection(schema, collection);
      msg.DataModel = (isRelational ? DataModel.Table : DataModel.Document);
      msg.Upsert = upsert;
      if(columns != null && columns.Length > 0)
      {
        foreach(string column in columns)
        {
          msg.Projection.Add(new ExprParser(column).ParseTableInsertField());
        }
      }
      foreach (object row in rows)
      {
        Mysqlx.Crud.Insert.Types.TypedRow typedRow = new Mysqlx.Crud.Insert.Types.TypedRow();
        object[] fields = row.GetType().IsArray ? (object[])row : new object[] { row };
        foreach (object field in fields)
        {
          typedRow.Field.Add(ExprUtil.ArgObjectToExpr(field, isRelational));
        }

        msg.Row.Add(typedRow);
      }
      _writer.Write(ClientMessageId.CRUD_INSERT, msg);
    }

    private void ApplyFilter(Action<Limit> setLimit, Action<Expr> setCriteria, Action<IEnumerable<Order>> setOrder, FilterParams filter, Action<IEnumerable<Scalar>> addParams)
    {
      if (filter.HasLimit)
      {
        var limit = new Limit();
        limit.RowCount = (ulong)filter.Limit;
        if (filter.Offset != -1) limit.Offset = (ulong)filter.Offset;
        setLimit(limit);
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
      var msg = new Delete();
      msg.DataModel = (isRelational ? DataModel.Table : DataModel.Document);
      msg.Collection = ExprUtil.BuildCollection(schema, collection);
      ApplyFilter(v => msg.Limit = v, v => msg.Criteria = v, msg.Order.Add, filter, msg.Args.Add);
      _writer.Write(ClientMessageId.CRUD_DELETE, msg);
    }

    /// <summary>
    /// Sends the CRUD modify message
    /// </summary>
    public void SendUpdate(string schema, string collection, bool isRelational, FilterParams filter, List<UpdateSpec> updates)
    {
      var msg = new Update();
      msg.DataModel = (isRelational ? DataModel.Table : DataModel.Document);
      msg.Collection = ExprUtil.BuildCollection(schema, collection);
      ApplyFilter(v => msg.Limit = v, v => msg.Criteria = v, msg.Order.Add, filter, msg.Args.Add);

      foreach (var update in updates)
      {
        var updateBuilder = new UpdateOperation();
        updateBuilder.Operation = update.Type;
        updateBuilder.Source = update.GetSource(isRelational);
        if (update.HasValue)
          updateBuilder.Value = update.GetValue();
        msg.Operation.Add(updateBuilder);
      }
      _writer.Write(ClientMessageId.CRUD_UPDATE, msg);
    }

    internal Find CreateFindMessage(string schema, string collection, bool isRelational, FilterParams filter, FindParams findParams)
    {
      var builder = new Find();
      builder.Collection = ExprUtil.BuildCollection(schema, collection);
      builder.DataModel = (isRelational ? DataModel.Table : DataModel.Document);
      if (findParams.Locking != 0) builder.Locking = (Find.Types.RowLock) findParams.Locking;
      if (findParams.LockingOption != 0) builder.LockingOptions =(Find.Types.RowLockOptions)findParams.LockingOption;
      if (findParams.Projection != null && findParams.Projection.Length > 0)
        builder.Projection.Add(new ExprParser(ExprUtil.JoinString(findParams.Projection)).ParseTableSelectProjection());
      ApplyFilter(v => builder.Limit = v, v => builder.Criteria = v, builder.Order.Add, filter, builder.Args.Add);
      return builder;
    }

    public void SendFind(string schema, string collection, bool isRelational, FilterParams filter, FindParams findParams)
    {
      var builder = CreateFindMessage(schema, collection, isRelational, filter, findParams);
      _writer.Write(ClientMessageId.CRUD_FIND, builder);
    }

    public void SendExpectOpen(Mysqlx.Expect.Open.Types.Condition.Types.Key condition)
    {
      var builder = new Mysqlx.Expect.Open();
      var cond = new Mysqlx.Expect.Open.Types.Condition();
      cond.ConditionKey = (uint)condition;
      builder.Cond.Add(cond);
      _writer.Write(ClientMessageId.EXPECT_OPEN, builder);
    }

    internal void ReadOkClose()
    {
      try
      {
        string response = ReadOk();
        if (response.IndexOf("bye", 0, StringComparison.OrdinalIgnoreCase) < 0)
          throw new ArgumentException();
      }
      catch (IOException)
      {
        // TODO connection is closed 
      }
      catch (Exception ex)
      {
        throw new MySqlException("Unexpected message encountered during closing session", ex);
      }
    }

    internal string ReadOk()
    {
      CommunicationPacket p = ReadPacket();
      if (p.MessageType == (int)ServerMessageId.ERROR)
      {
        var error = Error.Parser.ParseFrom(p.Buffer);
        throw new MySqlException(error.Msg);
      }
      if (p.MessageType == (int)ServerMessageId.OK)
      {
        var response = Ok.Parser.ParseFrom(p.Buffer);
        return response.Msg;
      }
      else
        throw new InvalidOperationException();
    }

    internal void SetXPackets(XPacketReaderWriter reader, XPacketReaderWriter writer)
    {
      _reader = reader;
      _writer = writer;
    }
  }
}
