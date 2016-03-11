// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI;
using MySqlX.Communication;
using System.Text;
using MySqlX.Data;
using MySqlX.Protocol;
using System.Collections.Generic;
using System.Reflection;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using MySqlX.XDevAPI.CRUD;
using MySqlX.Protocol.X;
using Mysqlx.Datatypes;
using MySql.Data.MySqlClient;
using MySql.Data.Common;
using MySql.Data.MySqlClient.Authentication;
using MySqlX.Security;
using MySqlX.Properties;

namespace MySqlX.Session
{

  /// <summary>
  /// Implementation class of InternalSession to manage coonections using the Xprotocol type object.
  /// </summary>  
  internal class XInternalSession : InternalSession
  {
    private XProtocol protocol;
    private XPacketReaderWriter _reader;
    private XPacketReaderWriter _writer;


    public XInternalSession(MySqlConnectionStringBuilder settings) : base(settings)
    {
    }

    protected override void Open()
    {
      _stream = MyNetworkStream.CreateStream(Settings, false);
      if (_stream == null)
        throw new MySqlException(ResourcesX.UnableToConnect);
      _reader = new XPacketReaderWriter(_stream);
      _writer = new XPacketReaderWriter(_stream);
      protocol = new XProtocol(_reader, _writer);

      Settings.CharacterSet = String.IsNullOrWhiteSpace(Settings.CharacterSet) ? "UTF-8" : Settings.CharacterSet;

      var encoding = Encoding.GetEncoding(Settings.CharacterSet);

      SetState(SessionState.Connecting, false);

      GetAndSetCapabilities();
      Authenticate();
      SetState(SessionState.Open, false);
    }

    private void GetAndSetCapabilities()
    {
      protocol.GetServerCapabilities();
      protocol.SetCapabilities();
    }

    private void Authenticate()
    {
      // do the authentication
      MySQL41AuthenticationPlugin plugin = new MySQL41AuthenticationPlugin(Settings);
      protocol.SendAuthStart(plugin.AuthName);
      byte[] extraData = protocol.ReadAuthContinue();
      protocol.SendAuthContinue(plugin.Continue(extraData));
      protocol.ReadAuthOk();
    }

    protected void SetState(SessionState newState, bool broadcast)
    {
      if (newState == SessionState && !broadcast)
        return;
      SessionState oldSessionState = SessionState;
      SessionState = newState;
      
      //TODO check if we need to send this event
      //if (broadcast)
        //OnStateChange(new StateChangeEventArgs(oldConnectionState, connectionState));
    }

    
    internal override ProtocolBase GetProtocol()
    {
      return protocol;
    }

    public override void Close()
    {
      try
      {
        protocol.SendSessionClose();
        protocol.ReadOK();
        _stream.Dispose();
        SessionState = SessionState.Closed;
      }
      catch (Exception ex)
      {        
        //TODO
        throw;
      }
    }


    public void CreateCollection(string schemaName, string collectionName)
    {
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_CREATE_COLLECTION, true, schemaName, collectionName);
    }

    public void DropCollection(string schemaName, string collectionName)
    {
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_DROP_COLLECTION, true, schemaName, collectionName);
    }

    public Result CreateCollectionIndex(CreateCollectionIndexStatement statement)
    {
      List<object> args = new List<object>();
      args.Add(statement.Target.Schema.Name);
      args.Add(statement.Target.Name);
      args.Add(statement.createIndexParams.IndexName);
      args.Add(statement.createIndexParams.IsUnique);
      for(int i = 0; i < statement.createIndexParams.DocPaths.Count; i++)
      {
        args.Add(statement.createIndexParams.DocPaths[i]);
        args.Add(statement.createIndexParams.Types[i]);
        args.Add(statement.createIndexParams.NotNulls[i]);
      }

      return ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_CREATE_COLLECTION_INDEX, false, args.ToArray());
    }

    public Result DropCollectionIndex(string schemaName, string collectionName, string indexName)
    {
      List<object> args = new List<object>();
      args.Add(schemaName);
      args.Add(collectionName);
      args.Add(indexName);
      return ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_DROP_COLLECTION_INDEX, false, args.ToArray());
    }

    public long TableCount(Schema schema, string name)
    {
      string sql = String.Format("SELECT COUNT(*) FROM {0}.{1}",
        ExprUnparser.QuoteIdentifier(schema.Name), ExprUnparser.QuoteIdentifier(name));
      return (long)ExecuteQueryAsScalar(sql);
    }

    public bool TableExists(Schema schema, string name)
    {
      string sql = String.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{0}' AND table_name = '{1}'",
        schema.Name, name);
      long count = (long)ExecuteQueryAsScalar(sql);
      return count != 0;
    }

    private Result ExecuteCmdNonQuery(string cmd, bool throwOnFail, params object[] args)
    {
      protocol.SendExecuteStatement("xplugin", cmd, args);
      return new Result(this);
    }

    public List<T> GetObjectList<T>(Schema s, string type)
    {
      RowResult result = GetRowResult("list_objects", s.Name);
      var rows = result.FetchAll();

      List<T> docs = new List<T>();
      foreach (var row in rows)
      {
        if (row.GetString("type") != type) continue;
        T t = (T)Activator.CreateInstance(typeof(T),
          BindingFlags.NonPublic | BindingFlags.Instance,
          null, new object[] { s, row.GetString("name") }, null);
        docs.Add(t);
      }
      return docs;
    }

    public RowResult GetRowResult(string cmd, params object[] args)
    {
      protocol.SendExecuteStatement("xplugin", cmd, args);
      return new RowResult(this);
    }

    public Result Insert(Collection collection, DbDoc[] json, List<string> newIds)
    {
      protocol.SendInsert(collection.Schema.Name, false, collection.Name, json, null);
      return new Result(this) { DocumentIds = newIds.AsReadOnly() } ;
    }

    public Result DeleteDocs(RemoveStatement rs)
    {
      protocol.SendDelete(rs.Target.Schema.Name, rs.Target.Name, false, rs.FilterData);
      return new Result(this);
    }

    public Result DeleteRows(TableDeleteStatement statement)
    {
      protocol.SendDelete(statement.Target.Schema.Name,
        statement.Target.Name, true,
        statement.FilterData);
      return new Result(this);
    }

    public Result ModifyDocs(ModifyStatement ms)
    {
      protocol.SendUpdate(ms.Target.Schema.Name, ms.Target.Name, false, ms.FilterData, ms.Updates);
      return new Result(this);
    }

    public Result UpdateRows(TableUpdateStatement statement)
    {
      protocol.SendUpdate(statement.Target.Schema.Name,
        statement.Target.Name, true,
        statement.FilterData,
        statement.updates);
      return new Result(this);
    }

    public DocResult FindDocs(FindStatement fs)
    {
      protocol.SendFind(fs.Target.Schema.Name, fs.Target.Name, false, fs.FilterData, null);
      DocResult result = new DocResult(this);
      return result;
    }

    public RowResult FindRows(TableSelectStatement ss)
    {
      protocol.SendFind(ss.Target.Schema.Name, ss.Target.Name, true, ss.FilterData, ss.findParams);
      return new RowResult(this);
    }

    public Result InsertRows(TableInsertStatement statement)
    {
      protocol.SendInsert(statement.Target.Schema.Name, true, statement.Target.Name, statement.values.ToArray(), statement.fields);
      return new Result(this);
    }

  }
}
