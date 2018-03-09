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
using MySqlX.XDevAPI;
using MySqlX.Communication;
using System.Text;
using MySql.Data.Common;
using MySqlX.Protocol;
using System.Collections.Generic;
using System.Reflection;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using MySqlX.XDevAPI.CRUD;
using MySqlX.Protocol.X;
using Mysqlx.Datatypes;
using MySql.Data.MySqlClient;
using MySqlX.Security;
using MySqlX;
using System.Linq;
using MySql.Data;
using MySql.Data.MySqlClient.Authentication;

namespace MySqlX.Sessions
{

  /// <summary>
  /// Implementation class of InternalSession to manage connections using the Xprotocol type object.
  /// </summary>
  internal class XInternalSession : InternalSession
  {
    private XProtocol protocol;
    private XPacketReaderWriter _reader;
    private XPacketReaderWriter _writer;
    private bool serverSupportsTls = false;
    private const string mysqlxNamespace = "mysqlx";


    public XInternalSession(MySqlConnectionStringBuilder settings) : base(settings)
    {
    }

    protected override void Open()
    {
      bool isUnix = Settings.ConnectionProtocol == MySqlConnectionProtocol.Unix ||
        Settings.ConnectionProtocol == MySqlConnectionProtocol.UnixSocket;
      _stream = MyNetworkStream.CreateStream(Settings, isUnix);
      if (_stream == null)
        throw new MySqlException(ResourcesX.UnableToConnect);
      _reader = new XPacketReaderWriter(_stream);
      _writer = new XPacketReaderWriter(_stream);
      protocol = new XProtocol(_reader, _writer);

      Settings.CharacterSet = String.IsNullOrWhiteSpace(Settings.CharacterSet) ? "utf8mb4" : Settings.CharacterSet;

      var encoding = Encoding.GetEncoding(String.Compare(Settings.CharacterSet, "utf8mb4", true) == 0 ? "UTF-8" : Settings.CharacterSet);

      SetState(SessionState.Connecting, false);

      GetAndSetCapabilities();

      // validates TLS use
      if (Settings.SslMode != MySqlSslMode.None)
      {
        if (serverSupportsTls)
        {
          new Ssl(Settings).StartSSL(ref _stream, encoding, Settings.ToString());
          _reader = new XPacketReaderWriter(_stream);
          _writer = new XPacketReaderWriter(_stream);
          protocol.SetXPackets(_reader, _writer);
        }
        else
        {
          // Client requires SSL connections.
          string message = String.Format(Resources.NoServerSSLSupport,
              Settings.Server);
          throw new MySqlException(message);
        }
      }

      // Default authentication
      if (Settings.Auth == MySqlAuthenticationMode.Default)
      {
        if ((Settings.SslMode != MySqlSslMode.None && serverSupportsTls) || Settings.ConnectionProtocol == MySqlConnectionProtocol.Unix)
        {
          Settings.Auth = MySqlAuthenticationMode.PLAIN;
          AuthenticatePlain();
        }
        else
        {
          bool authenticated = false;
          // first try using MYSQL41
          Settings.Auth = MySqlAuthenticationMode.MYSQL41;
          try
          {
            AuthenticateMySQL41();
            authenticated = true;
          }
          catch (MySqlException ex)
          {
            // code 1045 Invalid user or password
            if (ex.Code != 1045)
              throw;
          }

          // second try using SHA256_MEMORY
          if (!authenticated)
          {
            try
            {
              Settings.Auth = MySqlAuthenticationMode.SHA256_MEMORY;
              AuthenticateSha256Memory();
              authenticated = true;
            }
            catch(MySqlException ex)
            {
              // code 1045 Invalid user or password
              if (ex.Code == 1045)
                throw new MySqlException(1045, "HY000", ResourcesX.AuthenticationFailed);
              else
                throw;
            }
          }
        }
      }
      // User defined authentication
      else
      {
        switch (Settings.Auth)
        {
          case MySqlAuthenticationMode.PLAIN:
            AuthenticatePlain();
            break;
          case MySqlAuthenticationMode.MYSQL41:
            AuthenticateMySQL41();
            break;
          case MySqlAuthenticationMode.EXTERNAL:
            AuthenticateExternal();
            break;
          case MySqlAuthenticationMode.SHA256_MEMORY:
            AuthenticateSha256Memory();
            break;
          default:
            throw new NotImplementedException(Settings.Auth.ToString());
        }
      }

      SetState(SessionState.Open, false);
    }

    private void GetAndSetCapabilities()
    {
      protocol.GetServerCapabilities();

      Dictionary<string, object> clientCapabilities = new Dictionary<string, object>();

      // validates TLS use
      if (Settings.SslMode != MySqlSslMode.None)
      {
        var capability = protocol.Capabilities.Capabilities_.FirstOrDefault(i => i.Name.ToLowerInvariant() == "tls");
        if (capability != null)
        {
          serverSupportsTls = true;
          clientCapabilities.Add("tls", "1");
        }
      }
      protocol.SetCapabilities(clientCapabilities);
    }

    private void AuthenticateMySQL41()
    {
      MySQL41AuthenticationPlugin plugin = new MySQL41AuthenticationPlugin(Settings);
      protocol.SendAuthStart(plugin.AuthName, null, null);
      byte[] extraData = protocol.ReadAuthContinue();
      protocol.SendAuthContinue(plugin.Continue(extraData));
      protocol.ReadAuthOk();
    }

    private void AuthenticatePlain()
    {
      PlainAuthenticationPlugin plugin = new PlainAuthenticationPlugin(Settings);
      protocol.SendAuthStart(plugin.AuthName, plugin.GetAuthData(), null);
      protocol.ReadAuthOk();
    }

    private void AuthenticateExternal()
    {
      protocol.SendAuthStart("EXTERNAL", Encoding.UTF8.GetBytes(""), null);
      protocol.ReadAuthOk();
    }

    private void AuthenticateSha256Memory()
    {
      Sha256MemoryAuthenticationPlugin plugin = new Sha256MemoryAuthenticationPlugin();
      protocol.SendAuthStart(plugin.PluginName, null, null);
      byte[] nonce = protocol.ReadAuthContinue();

      string data = $"{Settings.Database}\0{Settings.UserID}\0";
      byte[] byteData = Encoding.UTF8.GetBytes(data);
      byte[] clientHash = plugin.GetClientHash(Settings.Password, nonce);
      byte[] authData = new byte[byteData.Length + clientHash.Length];
      byteData.CopyTo(authData, 0);
      clientHash.CopyTo(authData, byteData.Length);

      protocol.SendAuthContinue(authData);
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
      }
      finally
      {
        SessionState = SessionState.Closed;
        _stream.Dispose();
      }
    }


    public void CreateCollection(string schemaName, string collectionName)
    {
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_CREATE_COLLECTION,
        true,
        new KeyValuePair<string, object>("schema", schemaName),
        new KeyValuePair<string, object>("name", collectionName));
    }

    public void DropCollection(string schemaName, string collectionName)
    {
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_DROP_COLLECTION,
        true,
        new KeyValuePair<string, object>("schema", schemaName),
        new KeyValuePair<string, object>("name", collectionName));
    }

    public Result CreateCollectionIndex(CreateCollectionIndexStatement statement)
    {
      List<KeyValuePair<string, object>> args = new List<KeyValuePair<string, object>>();
      args.Add(new KeyValuePair<string, object>("name", statement.createIndexParams.IndexName));
      args.Add(new KeyValuePair<string, object>("collection", statement.Target.Name));
      args.Add(new KeyValuePair<string, object>("schema", statement.Target.Schema.Name));
      args.Add(new KeyValuePair<string, object>("unique", false));

      if (statement.createIndexParams.Type != null)
        args.Add(new KeyValuePair<string, object>("type", statement.createIndexParams.Type));

      for (int i = 0; i < statement.createIndexParams.Fields.Count; i++)
      {
        var field = statement.createIndexParams.Fields[i];
        var dictionary = new Dictionary<string, object>();
        dictionary.Add("member", field.Field);
        if (field.Type != null)
          dictionary.Add("type", field.Type == "TEXT" ? "TEXT(64)" : field.Type);

        if (field.Required == null)
          dictionary.Add("required", field.Type == "GEOJSON" ? true : false);
        else
          dictionary.Add("required", (bool)field.Required);

        if (field.Options != null)
          dictionary.Add("options", (ulong)field.Options);

        if (field.Srid != null)
          dictionary.Add("srid", (ulong)field.Srid);

        args.Add(new KeyValuePair<string, object>("constraint", dictionary));
      }

      return ExecuteCreateCollectionIndex(XpluginStatementCommand.XPLUGIN_STMT_CREATE_COLLECTION_INDEX, false, args.ToArray());
    }

    public void DropCollectionIndex(string schemaName, string collectionName, string indexName)
    {
      List<KeyValuePair<string, object>> args = new List<KeyValuePair<string, object>>();
      args.Add(new KeyValuePair<string, object>("schema", schemaName));
      args.Add(new KeyValuePair<string, object>("collection", collectionName));
      args.Add(new KeyValuePair<string, object>("name", indexName));
      ExecuteCmdNonQuery(XpluginStatementCommand.XPLUGIN_STMT_DROP_COLLECTION_INDEX, false, args.ToArray());
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

    private Result ExecuteCmdNonQuery(string cmd, bool throwOnFail, params KeyValuePair<string, object>[] args)
    {
      protocol.SendExecuteStatement(mysqlxNamespace, cmd, args);
      return new Result(this);
    }

    private Result ExecuteCreateCollectionIndex(string cmd, bool throwOnFail, params KeyValuePair<string, object>[] args)
    {
      protocol.SendCreateCollectionIndexStatement(mysqlxNamespace, cmd, args);
      return new Result(this);
    }

    public List<T> GetObjectList<T>(Schema s, params string[] types) where T : DatabaseObject
    {
      for (int i = 0; i < types.Length; i++)
        types[i] = types[i].ToUpperInvariant();
      RowResult result = GetRowResult("list_objects", new KeyValuePair<string, object>("schema", s.Name));
      var rows = result.FetchAll();

      List<T> docs = new List<T>();
      foreach (var row in rows)
      {
        if (!types.Contains(row.GetString("type").ToUpperInvariant())) continue;

        List<object> parameters = new List<object>(new object[] { s, row.GetString("name") });
        if (row["name"] is Byte[])
        {
          Byte[] byteArray = row["name"] as Byte[];
          parameters[1] = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
        }

        switch (row.GetString("type").ToUpperInvariant())
        {
          case "TABLE":
            parameters.Add(false);
            break;
          case "VIEW":
            parameters.Add(true);
            break;
        }
#if NETSTANDARD1_6
        T t = (T)Activator.CreateInstance(typeof(T), true);
        ((DatabaseObject)t).Schema = s;
        ((DatabaseObject)t).Name = parameters[1].ToString();
        if (parameters.Count == 3)
          t.GetType().GetProperty("IsView").SetValue(t, parameters[2]);
#else
        T t = (T)Activator.CreateInstance(typeof(T),
          BindingFlags.NonPublic | BindingFlags.Instance,
          null, parameters.ToArray(), null);
#endif
        docs.Add(t);
      }
      return docs;
    }

    public string GetObjectType(Schema s, string name)
    {
      RowResult result = GetRowResult("list_objects",
        new KeyValuePair<string, object>("schema", s.Name),
        new KeyValuePair<string, object>("pattern", name));
      var row = result.FetchOne();
      if (row == null)
        throw new MySqlException(string.Format(ResourcesX.NoObjectFound, name));
      System.Diagnostics.Debug.Assert(result.FetchOne() == null);
      return row.GetString("type");
    }

    public RowResult GetRowResult(string cmd, params KeyValuePair<string, object>[] args)
    {
      protocol.SendExecuteStatement(mysqlxNamespace, cmd, args);
      return new RowResult(this);
    }

    public Result Insert(Collection collection, DbDoc[] json, List<string> newIds, bool upsert)
    {
      protocol.SendInsert(collection.Schema.Name, false, collection.Name, json, null, upsert);
      return new Result(this);
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
      protocol.SendFind(fs.Target.Schema.Name, fs.Target.Name, false, fs.FilterData, fs.findParams);
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
      protocol.SendInsert(statement.Target.Schema.Name, true, statement.Target.Name, statement.values.ToArray(), statement.fields, false);
      return new Result(this);
    }

    protected Result ExpectOpen(Mysqlx.Expect.Open.Types.Condition.Types.Key condition)
    {
      protocol.SendExpectOpen(condition);
      return new Result(this);
  }

    public Result ExpectDocidGenerated()
    {
      return ExpectOpen(Mysqlx.Expect.Open.Types.Condition.Types.Key.ExpectDocidGenerated);
}
  }
}
