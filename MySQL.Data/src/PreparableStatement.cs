// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Data;
using MySql.Data.Common;

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
        //paramList[i].ColumnName = (string) parameter_names[i];
        string parameterName = (string)parameterNames[i];
        MySqlParameter p = Parameters.GetParameterFlexible(parameterName, false);
        if (p == null)
          throw new InvalidOperationException(
              String.Format(Resources.ParameterNotFoundDuringPrepare, parameterName));
        p.Encoding = paramList[i].Encoding;
        _parametersToSend.Add(p);
      }

      // now prepare our null map
      int numNullBytes = 0;
      if (paramList != null && paramList.Length > 0)
      {
          _nullMap = new BitArray(paramList.Length);
          numNullBytes = (_nullMap.Length + 7) / 8;
      }

      _packet = new MySqlPacket(Driver.Encoding);

      // write out some values that do not change run to run
      _packet.WriteByte(0);
      _packet.WriteInteger(StatementId, 4);
      _packet.WriteByte((byte)0); // flags; always 0 for 4.1
      _packet.WriteInteger(1, 4); // interation count; 1 for 4.1
      _nullMapPosition = _packet.Position;
      _packet.Position += numNullBytes;  // leave room for our null map
      _packet.WriteByte(1); // rebound flag
      // write out the parameter types
      foreach (MySqlParameter p in _parametersToSend)
        _packet.WriteInteger(p.GetPSType(), 2);
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

      //TODO: support long data here
      // create our null bitmap

      // we check this because Mono doesn't ignore the case where nullMapBytes
      // is zero length.
      //            if (nullMapBytes.Length > 0)
      //          {
      //            byte[] bits = packet.Buffer;
      //          nullMap.CopyTo(bits, 
      //        nullMap.CopyTo(nullMapBytes, 0);

      // start constructing our packet
      //            if (Parameters.Count > 0)
      //              nullMap.CopyTo(packet.Buffer, nullMapPosition);
      //if (parameters != null && parameters.Count > 0)
      //else
      //	packet.WriteByte( 0 );
      //TODO:  only send rebound if parms change

      // now write out all non-null values
      _packet.Position = _dataPosition;
      for (int i = 0; i < _parametersToSend.Count; i++)
      {
        MySqlParameter p = _parametersToSend[i];
        _nullMap[i] = (p.Value == DBNull.Value || p.Value == null) ||
            p.Direction == ParameterDirection.Output;
        if (_nullMap[i]) continue;
        _packet.Encoding = p.Encoding;
        p.Serialize(_packet, true, Connection.Settings);
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
      while (parameter != null)
      {
        if (parameter.IndexOf(StoredProcedure.ParameterPrefix) == -1)
        {
          newSQL.Append(sql.Substring(startPos, tokenizer.StartIndex - startPos));
          newSQL.Append("?");
          parameterMap.Add(parameter);
          startPos = tokenizer.StopIndex;
        }
        parameter = tokenizer.NextParameter();
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
