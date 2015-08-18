using Google.ProtocolBuffers;
using MySql.Communication;
using MySql.Data;
using MySql.DataAccess;
using MySql.Procotol;
using MySql.Security;
using Mysqlx.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MySql
{
  internal class XProtocol : ProtocolBase<UniversalStream>
  {
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
        authenticationPlugin = new Mysql41Authentication(baseStream);
      }      

      if (authenticationPlugin.AuthenticationMode == AuthenticationMode.MySQL41)
      {
        AuthenticateStart.Builder authStart = AuthenticateStart.CreateBuilder();       
        authStart.SetMechName("MYSQL41");
        AuthenticateStart message = authStart.Build();
        baseStream.SendPacket<AuthenticateStart>(message, (int)ClientMessageId.SESS_AUTHENTICATE_START);

        CommunicationPacket packet = baseStream.Read();

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


    public override int ExecuteStatement()
    {
      throw new NotImplementedException();
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
