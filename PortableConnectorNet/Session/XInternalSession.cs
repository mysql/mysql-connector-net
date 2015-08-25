using System;
using MySql.XDevAPI;
using MySql.Communication;
using System.Text;
using MySql.Security;
using MySql.Data;
using MySql.Protocol;
using System.Collections.Generic;

namespace MySql.Session
{
  internal class XInternalSession : InternalSession
  {
    private XProtocol protocol;
    private XPacketReaderWriter _reader;
    private XPacketReaderWriter _writer;
    private SessionState _sessionState;

    public SessionState SessionState {
      get
      {
        return _sessionState;      
      }
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
      protocol.SendExecuteStatement("xplugin", cmd, args);
      Result r = (RowResult)GetResult(true);
      if (throwOnFail && r.Failed)
        throw new MySqlException(r);
      return r;
    }

    public RowResult GetCollections(string schemaName)
    {
      protocol.SendExecuteStatement("xplugin", "list_object", schemaName);
      RowResult r = (RowResult)GetResult(true);
      r.Buffer();
      return r;
    }

    public Result Insert(string schema, string collection, string json)
    {
      protocol.SendInsert(schema, collection, json);
      return GetResult(true);
    }
  }
}
