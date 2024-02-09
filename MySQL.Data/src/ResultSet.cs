// Copyright © 2009, 2024, Oracle and/or its affiliates.
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

using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

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

    public static async Task<ResultSet> CreateResultSetAsync(Driver d, int statementId, int numCols, bool execAsync)
    {
      ResultSet resultSet = new ResultSet(d, statementId);
      await resultSet.InitializeAsync(numCols, execAsync).ConfigureAwait(false);
      return resultSet;
    }

    private ResultSet(Driver d, int statementId)
    {
      AffectedRows = -1;
      InsertedId = -1;
      _driver = d;
      _statementId = statementId;
      _rowIndex = -1;
    }

    private async Task InitializeAsync(int numCols, bool execAsync)
    {
      await LoadColumnsAsync(numCols, execAsync).ConfigureAwait(false);
      IsOutputParameters = IsOutputParameterResultSet();
      HasRows = await GetNextRowAsync(execAsync).ConfigureAwait(false);
      _readDone = !HasRows;
    }

    #region Properties

    public bool HasRows { get; private set; }

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
      if (_fieldHashCi.TryGetValue(name, out ordinal))
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
          Values[index] = _driver.ReadColumnValueAsync(index, Fields[index], Values[index], false).GetAwaiter().GetResult();
          _seqIndex = index;
        }

        return Values[index];
      }
    }

    private async Task<bool> GetNextRowAsync(bool execAsync)
    {
      bool fetched = await _driver.FetchDataRowAsync(_statementId, Size, execAsync).ConfigureAwait(false);

      if (fetched)
        TotalRows++;

      return fetched;
    }

    public async Task<bool> NextRowAsync(CommandBehavior behavior, bool execAsync)
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
        bool fetched;
        try
        {
          fetched = await GetNextRowAsync(execAsync).ConfigureAwait(false);
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

      if (!_isSequential)
        await ReadColumnDataAsync(false, execAsync).ConfigureAwait(false);

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
    public async Task CloseAsync(bool execAsync)
    {
      if (!_readDone)
      {

        // if we have rows but the user didn't read the first one then mark it as skipped
        if (HasRows && _rowIndex == -1)
          SkippedRows++;
        try
        {
          while (_driver.IsOpen && await _driver.SkipDataRowAsync(execAsync).ConfigureAwait(false))
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
    private async Task LoadColumnsAsync(int numCols, bool execAsync)
    {
      Fields = await _driver.GetColumnsAsync(numCols, execAsync).ConfigureAwait(false);

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

    private async Task ReadColumnDataAsync(bool outputParms, bool execAsync)
    {
      for (int i = 0; i < Size; i++)
        Values[i] = await _driver.ReadColumnValueAsync(i, Fields[i], Values[i], execAsync).ConfigureAwait(false);

      // if we are caching then we need to save a copy of this row of data values
      if (Cached)
        _cachedValues.Add((IMySqlValue[])Values.Clone());

      // we don't need to worry about caching the following since you won't have output
      // params with TableDirect commands
      if (!outputParms) return;

      bool rowExists = await _driver.FetchDataRowAsync(_statementId, Fields.Length, execAsync).ConfigureAwait(false);
      _rowIndex = 0;

      if (rowExists)
        throw new MySqlException(Resources.MoreThanOneOPRow);
    }
  }
}
