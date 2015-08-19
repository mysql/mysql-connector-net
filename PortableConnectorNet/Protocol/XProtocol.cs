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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.XDevAPI;

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
        baseStream.SendPacket(message, (int)ClientMessageId.SESS_AUTHENTICATE_START);

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
      CommunicationPacket p = pendingPacket != null ? pendingPacket : baseStream.Read();
      pendingPacket = null;
      return p;
    }

    public override void SendExecuteStatement(string ns, string stmt, params object[] args)
    {
      StmtExecute.Builder stmtExecute = StmtExecute.CreateBuilder();
      stmtExecute.SetNamespace(ns);
      stmtExecute.SetStmt(ByteString.CopyFromUtf8(stmt));
      stmtExecute.SetCompactMetadata(false);
      //      Scalar.Types.String stringValue = Scalar.Types.String.CreateBuilder().SetValue(ByteString.CopyFromUtf8("sakila")).Build();
      //    Scalar scalarValue = Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_STRING).SetVString(stringValue).Build();
      //  Any a = Any.CreateBuilder().SetType(Any.Types.Type.SCALAR).SetScalar(scalarValue).Build();
      //stmtExecute.AddArgs(a);
      StmtExecute msg = stmtExecute.Build();

      baseStream.SendPacket(msg, (int)ClientMessageId.SQL_STMT_EXECUTE);
    }

    public override List<byte[]> ReadRow()
    {
      byte[] buffer = pendingPacket != null ? pendingPacket.Buffer : null;

      if (buffer == null)
      {
        CommunicationPacket packet = ReadPacket();
        ///TODO:  handle this
        if (packet.MessageType != (int)ServerMessageId.RESULTSET_ROW) return null;
        buffer = packet.Buffer;
      }
      pendingPacket = null;
      Row protoRow = Row.ParseFrom(buffer);
      List<byte[]> values = new List<byte[]>(protoRow.FieldCount);
      for (int i = 0; i < protoRow.FieldCount; i++)
        values.Add(protoRow.GetField(i).ToByteArray());
      return values;
    }

    public override ResultSet ReadResultSet()
    {
      ResultSet resultSet = new ResultSet();
      CommunicationPacket packet = ReadPacket();
      if (packet.MessageType != (int)ServerMessageId.RESULTSET_COLUMN_META_DATA)
        throw new MySqlException("Failed to recieve column metadata packet");

      // read the metadata
      while (packet.MessageType == (int)ServerMessageId.RESULTSET_COLUMN_META_DATA)
      {
        ColumnMetaData response = ColumnMetaData.ParseFrom(packet.Buffer);
        resultSet.AddColumn(new Column(response.Name.ToStringUtf8()));
        packet = ReadPacket();
      }

      // if we have no rows then we are done
      if (packet.MessageType == (int)ServerMessageId.RESULTSET_FETCH_DONE) return resultSet;

      if (packet.MessageType == (int)ServerMessageId.RESULTSET_ROW)
        pendingPacket = packet;
      resultSet.Protocol = this;
      return resultSet;
    }

    public override void ExecuteReader()
    {
      throw new NotImplementedException();
    }

    public override void Find()
    {
      throw new NotImplementedException();
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

  }
}
