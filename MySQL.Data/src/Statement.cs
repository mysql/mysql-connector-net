// Copyright © 2004, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  internal abstract class Statement
  {
    protected MySqlCommand command;
    private readonly List<MySqlPacket> _buffers;
    protected string commandText;
    protected int paramsPosition;

    internal const string ParameterPrefix = "_cnet_param_";
    public virtual bool ServerProvidingOutputParameters { get; internal set; }

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

    public virtual void Close(MySqlDataReader reader) { }

    public virtual void Resolve(bool preparing)
    {
      if (!command.InternallyCreated || !(ResolvedCommandText != null && Parameters.Count == 0))
      {
        ServerProvidingOutputParameters = Driver.SupportsOutputParameters && preparing;
        if (Parameters.Cast<MySqlParameter>().Where(x => x.Direction == ParameterDirection.Output).Any())
        {
          string setSql = SetSql(Parameters, preparing);
          string outSql = CreateOutputSelect(Parameters, preparing);
          commandText = String.Format("{0}{1}", setSql, outSql);
        }
      }
    }

    private string SetSql(MySqlParameterCollection parms, bool preparing)
    {
      StringBuilder setSql = new StringBuilder();
      setSql.Append(commandText);
      foreach (MySqlParameter p in parms)
      {
        if (p.Direction != ParameterDirection.Output) continue;

        string uName = "@" + ParameterPrefix + p.BaseName;
        setSql.Replace(p.ParameterName, uName);
      }
      return setSql.ToString();
    }

    private string CreateOutputSelect(MySqlParameterCollection parms, bool preparing)
    {
      StringBuilder outputSql = new StringBuilder();
      string delimiter = string.Empty;

      foreach (MySqlParameter p in parms)
      {
        if (p.Direction == ParameterDirection.Output)
        {
          string uName = "@" + ParameterPrefix + p.BaseName;
          outputSql.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", delimiter, uName);
          delimiter = ", ";
        }
      }

      if (outputSql.Length == 0) return String.Empty;

      else if (command.Connection.Settings.AllowBatch && !preparing)
        return String.Format("; SELECT {0}", outputSql.ToString());
      else
      {
        command.OutSql = String.Format("SELECT {0}", outputSql.ToString());
        return String.Empty;
      }
    }
    /// <summary>
    /// Executes the SQL statement by binding parameters and sending the query packets to the server.
    /// This method handles both single statements and batched commands.
    /// </summary>
    public virtual void Execute()
    {
      // we keep a reference to this until we are done
      BindParameters();
      ExecuteNext();
    }

    /// <summary>
    /// Asynchronously executes the SQL statement by binding parameters and sending the query packets to the server.
    /// This method handles both single statements and batched commands.
    /// </summary>
    public virtual async Task ExecuteAsync()
    {
      // we keep a reference to this until we are done
      await BindParametersAsync().ConfigureAwait(false);
      await ExecuteNextAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the next query packet from the buffer.
    /// </summary>
    /// <returns>
    /// <c>true</c> if a packet was executed successfully; otherwise, <c>false</c> if no packets remain in the buffer.
    /// </returns>
    public virtual bool ExecuteNext()
    {
      if (_buffers.Count == 0)
        return false;

      MySqlPacket packet = _buffers[0];
      Driver.SendQuery(packet, paramsPosition);
      _buffers.RemoveAt(0);
      return true;
    }

    /// <summary>
    /// Asynchronously executes the next query packet from the buffer.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <c>true</c> if a packet was executed successfully; otherwise, <c>false</c> if no packets remain in the buffer.
    /// </returns>
    public virtual async Task<bool> ExecuteNextAsync()
    {
      if (_buffers.Count == 0)
        return false;

      MySqlPacket packet = _buffers[0];
      await Driver.SendQueryAsync(packet, paramsPosition).ConfigureAwait(false);
      _buffers.RemoveAt(0);
      return true;
    }

    /// <summary>
    /// Binds the command parameters and attributes to the query packets, supporting batch execution.
    /// This method constructs packets for query attributes and SQL statements, handling multiple batched commands if present.
    /// </summary>
    protected void BindParameters()
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
          string text = "";
          if (Connection.driver.Settings.RewriteBatchedStatements)
            text = batchedCmd.BatchableCommandText;
          else
            text = ResolvedCommandText;

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
    /// Asynchronously binds the command parameters and attributes to the query packets, supporting batch execution.
    /// This method constructs packets for query attributes and SQL statements, handling multiple batched commands if present.
    /// </summary>
    protected async Task BindParametersAsync()
    {
      MySqlParameterCollection parameters = command.Parameters;
      MySqlAttributeCollection attributes = command.Attributes;
      int index = 0;

      while (true)
      {
        MySqlPacket packet = await BuildQueryAttributesPacketAsync(attributes).ConfigureAwait(false);
        await InternalBindParametersAsync(ResolvedCommandText, parameters, packet).ConfigureAwait(false);

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
          string text = "";
          if (Connection.driver.Settings.RewriteBatchedStatements)
            text = batchedCmd.BatchableCommandText;
          else
            text = ResolvedCommandText;

          if (text.StartsWith("(", StringComparison.Ordinal))
            await packet.WriteStringNoNullAsync(", ").ConfigureAwait(false);
          else
            await packet.WriteStringNoNullAsync("; ").ConfigureAwait(false);

          await InternalBindParametersAsync(text, batchedCmd.Parameters, packet).ConfigureAwait(false);
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
    /// Builds the initial part of the COM_QUERY packet, including query attributes if supported by the server.
    /// </summary>
    /// <param name="attributes">The collection of query attributes to include in the packet.</param>
    /// <returns>A <see cref="MySqlPacket"/> containing the query attributes and prepared for SQL statement insertion.</returns>
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

    /// <summary>
    /// Asynchronously builds the initial part of the COM_QUERY packet, including query attributes if supported by the server.
    /// </summary>
    /// <param name="attributes">The collection of query attributes to include in the packet.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a <see cref="MySqlPacket"/> containing the query attributes and prepared for SQL statement insertion.</returns>
    private async Task<MySqlPacket> BuildQueryAttributesPacketAsync(MySqlAttributeCollection attributes)
    {
      MySqlPacket packet;
      packet = new MySqlPacket(Driver.Encoding) { Version = Driver.Version };
      packet.WriteByte(0);

      if (attributes.Count > 0 && !Driver.SupportsQueryAttributes)
        MySqlTrace.LogWarning(Connection.ServerThread, string.Format(Resources.QueryAttributesNotSupported, Driver.Version));
      else if (Driver.SupportsQueryAttributes)
      {
        int paramCount = attributes.Count;
        await packet.WriteLengthAsync(paramCount).ConfigureAwait(false); // int<lenenc> parameter_count - Number of parameters
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
            await packet.WriteIntegerAsync(attribute.GetPSType(), 2).ConfigureAwait(false);
            await packet.WriteLenStringAsync(attribute.AttributeName).ConfigureAwait(false);
          }

          // set value for each attribute
          for (int i = 0; i < attributes.Count; i++)
          {
            MySqlAttribute attr = attributes[i];
            _nullMap[i] = (attr.Value == DBNull.Value || attr.Value == null);
            if (_nullMap[i]) continue;
            await attr.SerializeAsync(packet, true, Connection.Settings).ConfigureAwait(false);
          }

          byte[] tempByteArray = new byte[(_nullMap.Length + 7) >> 3];
          _nullMap.CopyTo(tempByteArray, 0);

          Array.Copy(tempByteArray, 0, packet.Buffer, _nullMapPosition, tempByteArray.Length);
        }
      }

      paramsPosition = packet.Position;
      return packet;
    }

    /// <summary>
    /// Binds parameters to the SQL statement by tokenizing the SQL and serializing parameters into the packet.
    /// Handles parameter replacement and SQL Server mode adjustments.
    /// </summary>
    /// <param name="sql">The SQL command text to process.</param>
    /// <param name="parameters">The collection of parameters to bind.</param>
    /// <param name="packet">The packet to write the bound SQL and parameters to.</param>
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

    /// <summary>
    /// Asynchronously binds parameters to the SQL statement by tokenizing the SQL and serializing parameters into the packet.
    /// Handles parameter replacement and SQL Server mode adjustments.
    /// </summary>
    /// <param name="sql">The SQL command text to process.</param>
    /// <param name="parameters">The collection of parameters to bind.</param>
    /// <param name="packet">The packet to write the bound SQL and parameters to.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task InternalBindParametersAsync(string sql, MySqlParameterCollection parameters, MySqlPacket packet)
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
        await packet.WriteStringNoNullAsync(sql.Substring(pos, tokenizer.StartIndex - pos)).ConfigureAwait(false);
        pos = tokenizer.StopIndex;

        if (MySqlTokenizer.IsParameter(token))
        {
          if ((!parameters.containsUnnamedParameters && token.Length == 1 && parameterCount > 0) || parameters.containsUnnamedParameters && token.Length > 1)
            throw new MySqlException(Resources.MixedParameterNamingNotAllowed);

          parameters.containsUnnamedParameters = token.Length == 1;
          if (await SerializeParameterAsync(parameters, packet, token, parameterCount).ConfigureAwait(false))
            token = null;
          parameterCount++;
        }

        if (token != null)
        {
          if (sqlServerMode && tokenizer.Quoted && token.StartsWith("[", StringComparison.Ordinal))
            token = String.Format("`{0}`", token.Substring(1, token.Length - 2));

          await packet.WriteStringNoNullAsync(token).ConfigureAwait(false);
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
    /// Retrieves the MySqlParameter from the collection, returning null if the parameter is ignorable (e.g., user variable).
    /// </summary>
    /// <param name="parameters">The parameter collection.</param>
    /// <param name="parmName">The parameter name.</param>
    /// <param name="parameterIndex">The parameter index for unnamed parameters.</param>
    /// <returns>The parameter or null if ignorable or not found (but throws if required and not found).</returns>
    /// <exception cref="MySqlException">If unnamed parameter index is invalid or required parameter is missing.</exception>
    private MySqlParameter GetParameter(MySqlParameterCollection parameters, string parmName, int parameterIndex)
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

      if (parameter == null && parmName.StartsWith("@", StringComparison.Ordinal) && ShouldIgnoreMissingParameter(parmName))
        return null;

      if (parameter == null)
        throw new MySqlException(String.Format(Resources.ParameterMustBeDefined, parmName));

      return parameter;
    }

    /// <summary>
    /// Serializes the specified parameter into the packet, replacing the parameter placeholder in the SQL.
    /// </summary>
    /// <param name="parameters">The collection containing the parameter.</param>
    /// <param name="packet">The packet to serialize the parameter into.</param>
    /// <param name="parmName">The name of the parameter placeholder.</param>
    /// <param name="parameterIndex">The index of the parameter for unnamed parameters.</param>
    /// <returns>
    /// <c>true</c> if the parameter was found and serialized successfully; <c>false</c> if the parameter is a user variable or missing but ignorable.
    /// </returns>
    /// <remarks>
    /// This method is called by <see cref="InternalBindParameters"/> to convert the parameter to bytes and write them to the packet.
    /// Throws <see cref="MySqlException"/> if the parameter is required but not found.
    /// </remarks>
    private bool SerializeParameter(MySqlParameterCollection parameters, MySqlPacket packet, string parmName, int parameterIndex)
    {
      MySqlParameter parameter = GetParameter(parameters, parmName, parameterIndex);

      if (parameter == null)
        return false;

      parameter.Serialize(packet, false, Connection.Settings);
      return true;
    }

    /// <summary>
    /// Asynchronously serializes the specified parameter into the packet, replacing the parameter placeholder in the SQL.
    /// </summary>
    /// <param name="parameters">The collection containing the parameter.</param>
    /// <param name="packet">The packet to serialize the parameter into.</param>
    /// <param name="parmName">The name of the parameter placeholder.</param>
    /// <param name="parameterIndex">The index of the parameter for unnamed parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <c>true</c> if the parameter was found and serialized successfully; <c>false</c> if the parameter is a user variable or missing but ignorable.
    /// </returns>
    /// <remarks>
    /// This method is called by <see cref="InternalBindParametersAsync"/> to convert the parameter to bytes and write them to the packet.
    /// Throws <see cref="MySqlException"/> if the parameter is required but not found.
    /// </remarks>
    private async Task<bool> SerializeParameterAsync(MySqlParameterCollection parameters, MySqlPacket packet, string parmName, int parameterIndex)
    {
      MySqlParameter parameter = GetParameter(parameters, parmName, parameterIndex);

      if (parameter == null)
        return false;

      await parameter.SerializeAsync(packet, false, Connection.Settings).ConfigureAwait(false);
      return true;
    }
  }
}