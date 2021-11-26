// Copyright (c) 2004, 2021, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MySql.Data.MySqlClient
{
  internal abstract class Statement
  {
    protected MySqlCommand command;
    private readonly List<MySqlPacket> _buffers;
    protected string commandText;
    protected int paramsPosition;

    private Statement(MySqlCommand cmd)
    {
      command = cmd;
      _buffers = new List<MySqlPacket>();
    }

    protected Statement(MySqlCommand cmd, string text)
      : this(cmd)
    {
      commandText = text;
    }

    #region Properties

    public virtual string ResolvedCommandText
    {
      get { return commandText; }
    }

    protected Driver Driver => command.Connection.driver;

    protected MySqlConnection Connection => command.Connection;

    protected MySqlParameterCollection Parameters => command.Parameters;

    protected MySqlAttributeCollection Attributes => command.Attributes;

    #endregion

    public virtual void Close(MySqlDataReader reader)
    {
    }

    public virtual void Resolve(bool preparing)
    {
    }

    public virtual void Execute()
    {
      // we keep a reference to this until we are done
      BindParameters();
      ExecuteNext();
    }

    public virtual bool ExecuteNext()
    {
      if (_buffers.Count == 0)
        return false;

      MySqlPacket packet = _buffers[0];
      Driver.SendQuery(packet, paramsPosition);
      _buffers.RemoveAt(0);
      return true;
    }

    protected virtual void BindParameters()
    {
      MySqlParameterCollection parameters = command.Parameters;
      MySqlAttributeCollection attributes = command.Attributes;
      int index = 0;

      while (true)
      {
        MySqlPacket packet = BuildQueryAttributesPacket(attributes);
        InternalBindParameters(ResolvedCommandText, parameters, packet);

        // if we are not batching, then we are done.  This is only really relevant the
        // first time through
        if (command.Batch == null) return;
        while (index < command.Batch.Count)
        {
          MySqlCommand batchedCmd = command.Batch[index++];
          packet = (MySqlPacket)_buffers[_buffers.Count - 1];

          // now we make a guess if this statement will fit in our current stream
          long estimatedCmdSize = batchedCmd.EstimatedSize();
          if (((packet.Length - 4) + estimatedCmdSize) > Connection.driver.MaxPacketSize)
            // it won't, so we raise an exception to avoid a partial batch 
            throw new MySqlException(Resources.QueryTooLarge, (int)MySqlErrorCode.PacketTooLarge);

          // looks like we might have room for it so we remember the current end of the stream
          _buffers.RemoveAt(_buffers.Count - 1);
          //long originalLength = packet.Length - 4;

          // and attempt to stream the next command
          string text = ResolvedCommandText;
          if (text.StartsWith("(", StringComparison.Ordinal))
            packet.WriteStringNoNull(", ");
          else
            packet.WriteStringNoNull("; ");
          InternalBindParameters(text, batchedCmd.Parameters, packet);
          if ((packet.Length - 4) > Connection.driver.MaxPacketSize)
          {
            //TODO
            //stream.InternalBuffer.SetLength(originalLength);
            parameters = batchedCmd.Parameters;
            break;
          }
        }
        if (index == command.Batch.Count)
          return;
      }
    }

    /// <summary>
    /// Builds the initial part of the COM_QUERY packet
    /// </summary>
    /// <param name="attributes">Collection of attributes</param>
    /// <returns>A <see cref="MySqlPacket"/></returns>
    private MySqlPacket BuildQueryAttributesPacket(MySqlAttributeCollection attributes)
    {
      MySqlPacket packet;
      packet = new MySqlPacket(Driver.Encoding) { Version = Driver.Version };
      packet.WriteByte(0);

      if (attributes.Count > 0 && !Driver.SupportsQueryAttributes)
        MySqlTrace.LogWarning(Connection.ServerThread, string.Format(Resources.QueryAttributesNotSupported, Driver.Version));
      else if (Driver.SupportsQueryAttributes)
      {
        int paramCount = attributes.Count;
        packet.WriteLength(paramCount); // int<lenenc> parameter_count - Number of parameters
        packet.WriteByte(1); // int<lenenc> parameter_set_count - Number of parameter sets. Currently always 1

        if (paramCount > 0)
        {
          // now prepare our null map
          BitArray _nullMap = new BitArray(paramCount);
          int numNullBytes = (_nullMap.Length + 7) / 8;
          int _nullMapPosition = packet.Position;
          packet.Position += numNullBytes;  // leave room for our null map
          packet.WriteByte((byte)1); // new_params_bind_flag - Always 1. Malformed packet error if not 1

          // set type and name for each attribute
          foreach (MySqlAttribute attribute in attributes)
          {
            packet.WriteInteger(attribute.GetPSType(), 2);
            packet.WriteLenString(attribute.AttributeName);
          }

          // set value for each attribute
          for (int i = 0; i < attributes.Count; i++)
          {
            MySqlAttribute attr = attributes[i];
            _nullMap[i] = (attr.Value == DBNull.Value || attr.Value == null);
            if (_nullMap[i]) continue;
            attr.Serialize(packet, true, Connection.Settings);
          }

          byte[] tempByteArray = new byte[(_nullMap.Length + 7) >> 3];
          _nullMap.CopyTo(tempByteArray, 0);

          Array.Copy(tempByteArray, 0, packet.Buffer, _nullMapPosition, tempByteArray.Length);
        }
      }

      paramsPosition = packet.Position;
      return packet;
    }

    private void InternalBindParameters(string sql, MySqlParameterCollection parameters, MySqlPacket packet)
    {
      bool sqlServerMode = command.Connection.Settings.SqlServerMode;

      MySqlTokenizer tokenizer = new MySqlTokenizer(sql)
      {
        ReturnComments = true,
        SqlServerMode = sqlServerMode
      };

      int pos = 0;
      string token = tokenizer.NextToken();
      int parameterCount = 0;
      while (token != null)
      {
        // serialize everything that came before the token (i.e. whitespace)
        packet.WriteStringNoNull(sql.Substring(pos, tokenizer.StartIndex - pos));
        pos = tokenizer.StopIndex;

        if (MySqlTokenizer.IsParameter(token))
        {
          if ((!parameters.containsUnnamedParameters && token.Length == 1 && parameterCount > 0) || parameters.containsUnnamedParameters && token.Length > 1)
            throw new MySqlException(Resources.MixedParameterNamingNotAllowed);

          parameters.containsUnnamedParameters = token.Length == 1;
          if (SerializeParameter(parameters, packet, token, parameterCount))
            token = null;
          parameterCount++;
        }

        if (token != null)
        {
          if (sqlServerMode && tokenizer.Quoted && token.StartsWith("[", StringComparison.Ordinal))
            token = String.Format("`{0}`", token.Substring(1, token.Length - 2));
          packet.WriteStringNoNull(token);
        }
        token = tokenizer.NextToken();
      }
      _buffers.Add(packet);
    }

    protected virtual bool ShouldIgnoreMissingParameter(string parameterName)
    {
      if (Connection.Settings.AllowUserVariables)
        return true;
      if (parameterName.StartsWith("@" + StoredProcedure.ParameterPrefix, StringComparison.OrdinalIgnoreCase))
        return true;
      if (parameterName.Length > 1 &&
          (parameterName[1] == '`' || parameterName[1] == '\''))
        return true;
      return false;
    }

    /// <summary>
    /// Serializes the given parameter to the given memory stream
    /// </summary>
    /// <remarks>
    /// <para>This method is called by PrepareSqlBuffers to convert the given
    /// parameter to bytes and write those bytes to the given memory stream.
    /// </para>
    /// </remarks>
    /// <returns>True if the parameter was successfully serialized, false otherwise.</returns>
    private bool SerializeParameter(MySqlParameterCollection parameters,
                                    MySqlPacket packet, string parmName, int parameterIndex)
    {
      MySqlParameter parameter = null;

      if (!parameters.containsUnnamedParameters)
        parameter = parameters.GetParameterFlexible(parmName, false);
      else
      {
        if (parameterIndex <= parameters.Count)
          parameter = parameters[parameterIndex];
        else
          throw new MySqlException(Resources.ParameterIndexNotFound);
      }

      if (parameter == null)
      {
        // if we are allowing user variables and the parameter name starts with @
        // then we can't throw an exception
        if (parmName.StartsWith("@", StringComparison.Ordinal) && ShouldIgnoreMissingParameter(parmName))
          return false;
        throw new MySqlException(
            String.Format(Resources.ParameterMustBeDefined, parmName));
      }
      parameter.Serialize(packet, false, Connection.Settings);
      return true;
    }
  }
}
