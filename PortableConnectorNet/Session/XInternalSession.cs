using System;
using MySql.XDevAPI;
using MySql.Communication;
using System.Text;
using MySql.Security;
using MySql.Data;
using MySql.Protocol;

namespace MySql.Session
{
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
      _reader = new XPacketReaderWriter(_stream);
      _writer = new XPacketReaderWriter(_stream);
      protocol = new XProtocol(_reader, _writer);

      Settings.CharacterSet = String.IsNullOrWhiteSpace(Settings.CharacterSet) ? "UTF-8" : Settings.CharacterSet;

      var encoding = Encoding.GetEncoding(Settings.CharacterSet);

      // do the authentication
      AuthenticationPlugin plugin = GetAuthenticationPlugin();
      protocol.SendAuthStart(plugin.AuthName);
      byte[] extraData = protocol.ReadAuthContinue();
      protocol.SendAuthContinue(plugin.Continue(extraData));
      protocol.ReadAuthOk();
    }

    protected override void Close()
    {
    }

    public void CreateCollection(string schemaName, string collectionName)
    {
      protocol.SendExecuteStatement("xplugin", "create_collection", schemaName, collectionName);
      Result r = protocol.ReadStmtExecuteResult();
      if (r.Failed)
        throw new MySqlException(r);
    }
    public ResultSet GetResultSet(string sql)
    {
      protocol.SendExecuteStatement("sql", sql, null);
      return protocol.ReadResultSet();
    }


  }
}
