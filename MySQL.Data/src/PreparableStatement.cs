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
using System.Data;
using System.Text;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for PreparedStatement.
  /// </summary>
  internal class PreparableStatement : Statement
  {
    BitArray _nullMap;
    readonly List<MySqlParameter> _parametersToSend = new List<MySqlParameter>();
    MySqlPacket _packet;
    int _dataPosition;
    int _nullMapPosition;

    const int PARAMETER_COUNT_AVAILABLE = 0x08; // QueryAttributes should be sent to the server

    public PreparableStatement(MySqlCommand command, string text)
      : base(command, text)
    {
    }

    #region Properties

    public int ExecutionCount { get; set; }

    public bool IsPrepared => StatementId > 0;

    public int StatementId { get; private set; }

    #endregion

    public virtual void Prepare()
    {
      // strip out names from parameter markers
      string text;
      List<string> parameterNames = PrepareCommandText(out text);

      // ask our connection to send the prepare command
      MySqlField[] paramList = null;
      StatementId = Driver.PrepareStatement(text, ref paramList);

      // now we need to assign our field names since we stripped them out
      // for the prepare
      for (int i = 0; i < parameterNames.Count; i++)
      {
        string parameterName = (string)parameterNames[i];
        MySqlParameter p = Parameters.GetParameterFlexible(parameterName, false);
        if (p == null)
          throw new InvalidOperationException(
              String.Format(Resources.ParameterNotFoundDuringPrepare, parameterName));
        p.Encoding = paramList[i].Encoding;
        _parametersToSend.Add(p);
      }

      if (Attributes.Count > 0 && !Driver.SupportsQueryAttributes)
        MySqlTrace.LogWarning(Connection.ServerThread, string.Format(Resources.QueryAttributesNotSupported, Driver.Version));

      _packet = new MySqlPacket(Driver.Encoding);

      // write out some values that do not change run to run
      _packet.WriteByte(0);
      _packet.WriteInteger(StatementId, 4);
      // flags; if server supports query attributes, then set PARAMETER_COUNT_AVAILABLE (0x08) in the flags block
      int flags = Driver.SupportsQueryAttributes && Driver.Version.isAtLeast(8, 0, 26) ? PARAMETER_COUNT_AVAILABLE : 0;
      _packet.WriteInteger(flags, 1);
      _packet.WriteInteger(1, 4); // iteration count; 1 for 4.1
      int num_params = paramList != null ? paramList.Length : 0;
      // we don't send QA with PS when MySQL Server is not at least 8.0.26
      if (!Driver.Version.isAtLeast(8, 0, 26) && Attributes.Count > 0)
      {
        MySqlTrace.LogWarning(Connection.ServerThread, Resources.QueryAttributesNotSupportedByCnet);
        Attributes.Clear();
      }

      if (num_params > 0 ||
        (Driver.SupportsQueryAttributes && flags == PARAMETER_COUNT_AVAILABLE)) // if num_params > 0 
      {
        int paramCount = num_params;

        if (Driver.SupportsQueryAttributes) // if CLIENT_QUERY_ATTRIBUTES is on
        {
          paramCount = num_params + Attributes.Count;
          _packet.WriteLength(paramCount);
        }

        if (paramCount > 0)
        {
          // now prepare our null map
          _nullMap = new BitArray(paramCount);
          int numNullBytes = (_nullMap.Length + 7) / 8;
          _nullMapPosition = _packet.Position;
          _packet.Position += numNullBytes;  // leave room for our null map
          _packet.WriteByte(1); // new_params_bind_flag

          // write out the parameter types and names
          foreach (MySqlParameter p in _parametersToSend)
          {
            // parameter type
            _packet.WriteInteger(p.GetPSType(), 2);

            // parameter name
            if (Driver.SupportsQueryAttributes) // if CLIENT_QUERY_ATTRIBUTES is on
              _packet.WriteLenString(p.BaseName);
          }

          // write out the attributes types and names
          foreach (MySqlAttribute a in Attributes)
          {
            // attribute type
            _packet.WriteInteger(a.GetPSType(), 2);

            // attribute name
            if (Driver.SupportsQueryAttributes) // if CLIENT_QUERY_ATTRIBUTES is on
              _packet.WriteLenString(a.AttributeName);
          }
        }
      }

      _dataPosition = _packet.Position;
    }

    public override void Execute()
    {
      // if we are not prepared, then call down to our base
      if (!IsPrepared)
      {
        base.Execute();
        return;
      }

      // now write out all non-null values
      _packet.Position = _dataPosition;

      // set value for each parameter
      for (int i = 0; i < _parametersToSend.Count; i++)
      {
        MySqlParameter p = _parametersToSend[i];
        _nullMap[i] = (p.Value == DBNull.Value || p.Value == null) ||
            p.Direction == ParameterDirection.Output;
        if (_nullMap[i]) continue;
        _packet.Encoding = p.Encoding;
        p.Serialize(_packet, true, Connection.Settings);
      }

      // // set value for each attribute
      for (int i = 0; i < Attributes.Count; i++)
      {
        MySqlAttribute attr = Attributes[i];
        _nullMap[i] = (attr.Value == DBNull.Value || attr.Value == null);
        if (_nullMap[i]) continue;
        attr.Serialize(_packet, true, Connection.Settings);
      }

      if (_nullMap != null)
      {
        byte[] tempByteArray = new byte[(_nullMap.Length + 7) >> 3];
        _nullMap.CopyTo(tempByteArray, 0);

        Array.Copy(tempByteArray, 0, _packet.Buffer, _nullMapPosition, tempByteArray.Length);
      }

      ExecutionCount++;

      Driver.ExecuteStatement(_packet);
    }

    public override bool ExecuteNext()
    {
      if (!IsPrepared)
        return base.ExecuteNext();
      return false;
    }

    /// <summary>
    /// Prepares CommandText for use with the Prepare method
    /// </summary>
    /// <returns>Command text stripped of all paramter names</returns>
    /// <remarks>
    /// Takes the output of TokenizeSql and creates a single string of SQL
    /// that only contains '?' markers for each parameter.  It also creates
    /// the parameterMap array list that includes all the paramter names in the
    /// order they appeared in the SQL
    /// </remarks>
    private List<string> PrepareCommandText(out string stripped_sql)
    {
      StringBuilder newSQL = new StringBuilder();
      List<string> parameterMap = new List<string>();

      int startPos = 0;
      string sql = ResolvedCommandText;
      MySqlTokenizer tokenizer = new MySqlTokenizer(sql);
      string parameter = tokenizer.NextParameter();
      int paramIndex = 0;
      while (parameter != null)
      {
        if (parameter.IndexOf(StoredProcedure.ParameterPrefix) == -1)
        {
          newSQL.Append(sql.Substring(startPos, tokenizer.StartIndex - startPos));
          newSQL.Append("?");
          if (parameter.Length == 1 && tokenizer.IsParameterMarker(parameter.ToCharArray()[0]))
            parameterMap.Add(Parameters[paramIndex].ParameterName);
          else
            parameterMap.Add(parameter);
          startPos = tokenizer.StopIndex;
        }
        parameter = tokenizer.NextParameter();
        paramIndex++;
      }
      newSQL.Append(sql.Substring(startPos));
      stripped_sql = newSQL.ToString();
      return parameterMap;
    }

    public virtual void CloseStatement()
    {
      if (!IsPrepared) return;

      Driver.CloseStatement(StatementId);
      StatementId = 0;
    }
  }
}
