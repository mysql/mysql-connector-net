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
using MySql.XDevAPI;
using MySql.Communication;
using System.Text;
using MySql.Security;
using MySql.Data;
using MySql.Protocol;
using System.Collections.Generic;
using MySql.XDevAPI.Statements;
using MySql.XDevAPI.Results;

namespace MySql.Session
{
  internal class XInternalSession : InternalSession
  {
    private XProtocol protocol;
    private XPacketReaderWriter _reader;
    private XPacketReaderWriter _writer;
    private SessionState _sessionState;

    public SessionState SessionState
    {
      get { return _sessionState; }
    }

    public XInternalSession(MySqlConnectionStringBuilder settings) : base(settings)
    {
    }

    protected override void Open()
    {
      _stream = MyNetworkStream.CreateStream(Settings, false);
      _reader = new XPacketReaderWriter(_stream);
      _writer = new XPacketReaderWriter(_stream);
      protocol = new XProtocol(_reader, _writer);

      Settings.CharacterSet = String.IsNullOrWhiteSpace(Settings.CharacterSet) ? "UTF-8" : Settings.CharacterSet;

      var encoding = Encoding.GetEncoding(Settings.CharacterSet);

      SetState(SessionState.Connecting, false);

      // do the authentication
      AuthenticationPlugin plugin = GetAuthenticationPlugin();
      protocol.SendAuthStart(plugin.AuthName);
      byte[] extraData = protocol.ReadAuthContinue();
      protocol.SendAuthContinue(plugin.Continue(extraData));
      protocol.ReadAuthOk();
      SetState(SessionState.Open, false);
      
    }

    protected void SetState(SessionState newState, bool broadcast)
    {
      if (newState == _sessionState && !broadcast)
        return;
      SessionState oldSessionState = _sessionState;
      _sessionState = newState;
      
      //TODO check if we need to send this event
      //if (broadcast)
        //OnStateChange(new StateChangeEventArgs(oldConnectionState, connectionState));
    }

    
    protected override ProtocolBase GetProtocol()
    {
      return protocol;
    }

    public override void Close()
    {
      try
      {
        protocol.SendSessionClose();
        protocol.ReadOK();
        _sessionState = SessionState.Closed;
      }
      catch
      {        
        //TODO
        throw;
      }
    }


    public void CreateCollection(string schemaName, string collectionName)
    {
      ExecuteNonQueryCmd("create_collection", true, schemaName, collectionName);
    }

    public void DropCollection(string schemaName, string collectionName)
    {
      ExecuteNonQueryCmd("drop_collection", true, schemaName, collectionName);
    }

    private Result ExecuteNonQueryCmd(string cmd, bool throwOnFail, params object[] args)
    {
      return ExecuteNonQuery(false, cmd, throwOnFail, args);
    }

    private Result ExecuteNonQuery(bool isRelational, string cmd, bool throwOnFail, params object[] args)
    {
      protocol.SendExecuteStatement(isRelational ? "sql" : "xplugin", cmd, args);
      Result result = GetUpdateResult();
      if (throwOnFail && result.Failed)
        throw new MySqlException(result);
      return result;
    }

    public Result ExecuteSqlNonQuery(string cmd, bool throwOnFail, params object[] args)
    {
      return ExecuteNonQuery(true, cmd, throwOnFail, args);
    }

    public TableResult GetTableResult(string cmd, bool fullyLoad, params object[] args)
    {
      protocol.SendExecuteStatement("xplugin", "list_object", args);
      TableResult result = new TableResult(protocol);
      if (fullyLoad)
        result.Buffer();
      return result;
    }

    private UpdateResult GetUpdateResult()
    {
      UpdateResult rs = new UpdateResult();
      protocol.CloseResult(rs);
      return rs;
    }

    public UpdateResult Insert(Collection collection, JsonDoc[] json)
    {
      protocol.SendInsert(collection.Schema.Name, collection.Name, json);
      return GetUpdateResult();
    }

    public UpdateResult DeleteDocs(RemoveStatement rs)
    {
      protocol.SendDocDelete(rs.CollectionOrTable.Schema.Name, rs.CollectionOrTable.Name, rs.FilterData);
      return GetUpdateResult();
    }

    public UpdateResult ModifyDocs(ModifyStatement ms)
    {
      protocol.SendDocModify(ms.CollectionOrTable.Schema.Name, ms.CollectionOrTable.Name, ms.FilterData, ms.Updates);
      return GetUpdateResult();
    }

    public DocumentResult FindDocs(FindStatement fs)
    {
      protocol.SendFind(fs.CollectionOrTable.Schema.Name, fs.CollectionOrTable.Name, false, fs.FilterData, null);
      DocumentResult result = new DocumentResult(protocol);
      return result;
    }
    public TableResult FindRows(SelectStatement ss)
    {
      protocol.SendFind(ss.CollectionOrTable.Schema.Name, ss.CollectionOrTable.Name, true, ss.FilterData, ss.findParams);
      TableResult result = new TableResult(protocol);
      return result;
    }

  }
}
