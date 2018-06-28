// Copyright © 2009, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using MySql.Data.Types;

namespace MySql.Data.MySqlClient
{
  internal class ResultSet
  {
    private Driver _driver;
    private bool[] _uaFieldsUsed;
    private Dictionary<string, int> _fieldHashCi;
    private int _rowIndex;
    private bool _readDone;
    private bool _isSequential;
    private int _seqIndex;
    private readonly int _statementId;
    private bool _cached;
    private List<IMySqlValue[]> _cachedValues;

    public ResultSet(int affectedRows, long insertedId)
    {
      AffectedRows = affectedRows;
      InsertedId = insertedId;
      _readDone = true;
    }

    public ResultSet(Driver d, int statementId, int numCols)
    {
      AffectedRows = -1;
      InsertedId = -1;
      _driver = d;
      _statementId = statementId;
      _rowIndex = -1;
      LoadColumns(numCols);
      IsOutputParameters = IsOutputParameterResultSet();
      HasRows = GetNextRow();
      _readDone = !HasRows;
    }

#region Properties

    public bool HasRows { get; }

    public int Size => Fields?.Length ?? 0;

    public MySqlField[] Fields { get; private set; }

    public IMySqlValue[] Values { get; private set; }

    public bool IsOutputParameters { get; set; }

    public int AffectedRows { get; private set; }

    public long InsertedId { get; private set; }

    public int TotalRows { get; private set; }

    public int SkippedRows { get; private set; }

    public bool Cached
    {
      get { return _cached; }
      set
      {
        _cached = value;
        if (_cached && _cachedValues == null)
          _cachedValues = new List<IMySqlValue[]>();
      }
    }

#endregion

    /// <summary>
    /// return the ordinal for the given column name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int GetOrdinal(string name)
    {
      int ordinal;

      // quick hash lookup using CI hash
      if (_fieldHashCi.TryGetValue( name, out ordinal ))
        return ordinal;

      // Throw an exception if the ordinal cannot be found.
      throw new IndexOutOfRangeException(
          String.Format(Resources.CouldNotFindColumnName, name));
    }

    /// <summary>
    /// Retrieve the value as the given column index
    /// </summary>
    /// <param name="index">The column value to retrieve</param>
    /// <returns>The value as the given column</returns>
    public IMySqlValue this[int index]
    {
      get
      {
        if (_rowIndex < 0)
          throw new MySqlException(Resources.AttemptToAccessBeforeRead);

        // keep count of how many columns we have left to access
        _uaFieldsUsed[index] = true;

        if (_isSequential && index != _seqIndex)
        {
          if (index < _seqIndex)
            throw new MySqlException(Resources.ReadingPriorColumnUsingSeqAccess);
          while (_seqIndex < (index - 1))
            _driver.SkipColumnValue(Values[++_seqIndex]);
          Values[index] = _driver.ReadColumnValue(index, Fields[index], Values[index]);
          _seqIndex = index;
        }

        return Values[index];
      }
    }

    private bool GetNextRow()
    {
      bool fetched = _driver.FetchDataRow(_statementId, Size);
      if (fetched)
        TotalRows++;
      return fetched;
    }


    public bool NextRow(CommandBehavior behavior)
    {
      if (_readDone)
      {
        if (Cached) return CachedNextRow(behavior);
        return false;
      }

      if ((behavior & CommandBehavior.SingleRow) != 0 && _rowIndex == 0)
        return false;

      _isSequential = (behavior & CommandBehavior.SequentialAccess) != 0;
      _seqIndex = -1;

      // if we are at row index >= 0 then we need to fetch the data row and load it
      if (_rowIndex >= 0)
      {
        bool fetched = false;
        try
        {
          fetched = GetNextRow();
        }
        catch (MySqlException ex)
        {
          if (ex.IsQueryAborted)
          {
            // avoid hanging on Close()
            _readDone = true;
          }
          throw;
        }

        if (!fetched)
        {
          _readDone = true;
          return false;
        }
      }

      if (!_isSequential) ReadColumnData(false);
      _rowIndex++;
      return true;
    }

    private bool CachedNextRow(CommandBehavior behavior)
    {
      if ((behavior & CommandBehavior.SingleRow) != 0 && _rowIndex == 0)
        return false;
      if (_rowIndex == (TotalRows - 1)) return false;
      _rowIndex++;
      Values = _cachedValues[_rowIndex];
      return true;
    }

    /// <summary>
    /// Closes the current resultset, dumping any data still on the wire
    /// </summary>
    public void Close()
    {
      if (!_readDone)
      {

        // if we have rows but the user didn't read the first one then mark it as skipped
        if (HasRows && _rowIndex == -1)
          SkippedRows++;
        try
        {
          while (_driver.IsOpen && _driver.SkipDataRow())
          {
            TotalRows++;
            SkippedRows++;
          }
        }
        catch (System.IO.IOException)
        {
          // it is ok to eat IO exceptions here, we just want to 
          // close the result set
        }
        _readDone = true;
      }
      else if (_driver == null)
        CacheClose();

      _driver = null;
      if (Cached) CacheReset();
    }

    private void CacheClose()
    {
      SkippedRows = TotalRows - _rowIndex - 1;
    }

    private void CacheReset()
    {
      if (!Cached) return;
      _rowIndex = -1;
      AffectedRows = -1;
      InsertedId = -1;
      SkippedRows = 0;
    }

    public bool FieldRead(int index)
    {
      Debug.Assert(Size > index);
      return _uaFieldsUsed[index];
    }

    public void SetValueObject(int i, IMySqlValue valueObject)
    {
      Debug.Assert(Values != null);
      Debug.Assert(i < Values.Length);
      Values[i] = valueObject;
    }

    private bool IsOutputParameterResultSet()
    {
      if (_driver.HasStatus(ServerStatusFlags.OutputParameters)) return true;

      if (Fields.Length == 0) return false;

      for (int x = 0; x < Fields.Length; x++)
        if (!Fields[x].ColumnName.StartsWith("@" + StoredProcedure.ParameterPrefix, StringComparison.OrdinalIgnoreCase)) return false;
      return true;
    }

    /// <summary>
    /// Loads the column metadata for the current resultset
    /// </summary>
    private void LoadColumns(int numCols)
    {
      Fields = _driver.GetColumns(numCols);

      Values = new IMySqlValue[numCols];
      _uaFieldsUsed = new bool[numCols];
      _fieldHashCi = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

      for (int i = 0; i < Fields.Length; i++)
      {
        string columnName = Fields[i].ColumnName;
        if (!_fieldHashCi.ContainsKey(columnName))
          _fieldHashCi.Add(columnName, i);
        Values[i] = Fields[i].GetValueObject();
      }
    }

    private void ReadColumnData(bool outputParms)
    {
      for (int i = 0; i < Size; i++)
        Values[i] = _driver.ReadColumnValue(i, Fields[i], Values[i]);

      // if we are caching then we need to save a copy of this row of data values
      if (Cached)
        _cachedValues.Add((IMySqlValue[])Values.Clone());

      // we don't need to worry about caching the following since you won't have output
      // params with TableDirect commands
      if (!outputParms) return;

      bool rowExists = _driver.FetchDataRow(_statementId, Fields.Length);
      _rowIndex = 0;
      if (rowExists)
        throw new MySqlException(Resources.MoreThanOneOPRow);
    }
  }
}
