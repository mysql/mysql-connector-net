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

using MySql.Communication;
using MySql.Data;
using MySql.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MySql;
using Mysqlx.Session;
using Google.ProtocolBuffers;
using Mysqlx;
using System.IO;

namespace MySql.Security
{

  internal abstract class AuthenticationPlugin
  {
    public abstract string PluginName { get; }
    public abstract string AuthName { get; }

    public abstract byte[] Continue(byte[] data);
  }


  internal class MySQL41AuthenticationPlugin : AuthenticationPlugin
  {
    private MySqlConnectionStringBuilder _settings;

    public MySQL41AuthenticationPlugin(MySqlConnectionStringBuilder settings)
    {
      _settings = settings;
    }

    public override string PluginName
    {
      get { return "MySQL 4.1 Authentication Plugin"; }
    }

    public override string AuthName
    {
      get
      {
        return "MYSQL41";
      }
    }

    protected byte[] GetPassword(Encoding encoding, string password, byte[] seedBytes)
    {
      if (password.Length == 0) return new byte[1];

      SHA1 sha = new SHA1CryptoServiceProvider();

      byte[] firstHash = sha.ComputeHash(Encoding.Default.GetBytes(password));
      byte[] secondHash = sha.ComputeHash(firstHash);

      byte[] input = new byte[seedBytes.Length + secondHash.Length];
      Array.Copy(seedBytes, 0, input, 0, seedBytes.Length);
      Array.Copy(secondHash, 0, input, seedBytes.Length, secondHash.Length);
      byte[] thirdHash = sha.ComputeHash(input);


      byte[] finalHash = new byte[thirdHash.Length];

      for (int i = 0; i < thirdHash.Length; i++)
        finalHash[i] = (byte)(thirdHash[i] ^ firstHash[i]);

      if (finalHash != null && finalHash.Length == 1 && finalHash[0] == 0)
        return null;

      return finalHash;
    }

    public override byte[] Continue(byte[] salt)
    {
      var encoding = Encoding.GetEncoding("utf-8");

      byte[] userBytes = encoding.GetBytes(_settings.UserID);
      byte[] databaseBytes = encoding.GetBytes(_settings.Database);
      byte[] hashedPassword = new byte[0];
      byte[] hex = new byte[0];
      if (!string.IsNullOrWhiteSpace(_settings.Password))
      {
        hashedPassword = GetPassword(encoding, _settings.Password, salt);
        //convert to hex value 
        hex = encoding.GetBytes(string.Format("*{0}", BitConverter.ToString(hashedPassword).Replace("-", string.Empty)));
      }

      // create response
      byte[] response = new byte[databaseBytes.Length + userBytes.Length + hex.Length + 2];
      databaseBytes.CopyTo(response, 0);
      var index = databaseBytes.Length;
      response[index++] = 0;
      userBytes.CopyTo(response, index);
      index += userBytes.Length;
      response[index++] = 0;
      hex.CopyTo(response, index);
      return response;
    }
  }
}
//    public override AuthenticateOk Authenticate(bool reset, AuthenticateContinue message)
//  {

//    byte[] salt = null;

//    if (message != null)
//    {
//      salt = message.AuthData.ToByteArray();
//    }

//    if (salt  != null)
//    {

//        var encoding = Encoding.GetEncoding("utf8");

//    byte[] userBytes = encoding.GetBytes(_settings.UserID);    
//    byte[] databaseBytes = encoding.GetBytes(_settings.Database);
//    byte[] hashedPassword = GetPassword(_settings.Password, salt);
   
//    //convert to hex value 
//    byte[] hex = encoding.GetBytes(string.Format("*{0}", BitConverter.ToString(hashedPassword).Replace("-", string.Empty)));

//    // create response
//    byte[] response = new byte[databaseBytes.Length + userBytes.Length + hex.Length + 2];

//    databaseBytes.CopyTo(response, 0);
//    var index = databaseBytes.Length;
//    response[index++] = 0;
//    userBytes.CopyTo(response, index);
//    index += userBytes.Length;
//    response[index++] = 0;
//    hex.CopyTo(response, index);

//    AuthenticateContinue.Builder builder = AuthenticateContinue.CreateBuilder();
//    builder.SetAuthData(ByteString.CopyFrom(response));
//    AuthenticateContinue authCont = builder.Build();
//    byte[] authenticationContinueBytes;
//    using (MemoryStream stream = new MemoryStream())
//    {
//      int size = authCont.SerializedSize + 1;
//      stream.Write(BitConverter.GetBytes(size), 0, 4);
//      stream.WriteByte((byte)ClientMessageId.SESS_AUTHENTICATE_CONTINUE);
//      authCont.WriteTo(stream);
//      authenticationContinueBytes = stream.ToArray();
//    }

//    universalStream.SendPacket(authenticationContinueBytes);

//    CommunicationPacket packet = universalStream.Read();

//    if (packet != null)
//    {
//      if (packet.MessageType == (int)ServerMessageId.ERROR)
//      {
//        var error = Error.ParseFrom(packet.Buffer);
//        throw new Exception(error.Msg);
//      }            
//      if (packet.MessageType == (int)ServerMessageId.SESS_AUTHENTICATE_OK)
//        {
//          return AuthenticateOk.ParseFrom(packet.Buffer);
//        } 
   
//      }
//    }
//    return null; 
//  }


//  public override void AuthenticateFail(Exception ex)
//  {
//    throw new MySqlException(ex.Message, true, ex.InnerException);
//  }


//}

//  internal class PlainAuthentication : AuthenticationBase
//  {

//    public override string PluginName
//    {
//      get
//      {
//        return "PlainAuthentication";
//      }      
//    }

//    public PlainAuthentication(UniversalStream baseStream, MySqlConnectionStringBuilder settings): base(baseStream, settings)
//    {   }

//    public override AuthenticateOk Authenticate(bool reset, AuthenticateContinue message)
//    { 
//      //TODO
//      return null;
//    }
//    protected override byte[] GetPassword(string password, byte[] seedBytes)
//    {
//      return new byte[1];
//    }

//    public override void AuthenticateFail(Exception ex)
//    {

//      string msg = String.Format("AuthenticationFailedMessage", "server", GetUsername(), PluginName, ex.Message);

//      //throw new MySqlException(msg, ex);
//      //TODO
//    }
//  }

//}
