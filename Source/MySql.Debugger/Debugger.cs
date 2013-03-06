// Copyright © 2004, 2012, Oracle and/or its affiliates. All rights reserved.
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

#define PREINSTRUMENT_FUNCTIONS_AND_TRIGGERS

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.MySqlClient;
using MySql.Parser;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace MySql.Debugger
{
  public class Debugger
  {
    
    private MySqlConnection _connection;
    
    private MySqlConnection _utilCon;

    private const string _backupDirGuid = "5F9C6611-B03F-4323-82E1-D50FB3BC7271";

    /// <summary>
    /// Main connection for communication between debugger & debuggee.
    /// </summary>
    public MySqlConnection Connection
    {
      get
      {
        return _connection;
      }
      set
      {
        _connection = value;
      }
    }

    /// <summary>
    /// Secondary connection for observability logic.
    /// </summary>
    public MySqlConnection UtilityConnection
    {
      get
      {
        return _utilCon;
      }
      set
      {
        _utilCon = value;
      }
    }

    private MySqlConnection _lockingCon;

    public MySqlConnection LockingConnection
    {
      get
      {
        return _lockingCon;
      }
      set
      {
        _lockingCon = value;
      }
    }

    private string _sqlInput;

    /// <summary>
    /// The sql statements to be debugged.
    /// </summary>
    public string SqlInput
    {
      get { return _sqlInput; }
      set { _sqlInput = value; }
    }

    public Debugger()
    {
      _worker.WorkerSupportsCancellation = true;
      _worker.DoWork += worker_DoWork;
      _worker.RunWorkerCompleted += worker_RunWorkerCompleted;
    }

    public string FormatValue(object value)
    {
      return StoreType.WrapValue(value);
    }
    
    public Dictionary<string, StoreType> ScopeVariables { get { return CurrentScope.Variables; } }

    // The token stream, to rewrite individual tokens (like replacing variables by their values).
    internal CommonTokenStream tokenStream = null;

    public SteppingTypeEnum SteppingType = SteppingTypeEnum.None;

    /// <summary>
    /// Restores all the instrumented routines to its original state.
    /// </summary>
    public void RestoreRoutinesBackup()
    {
      if ((_utilCon.State & ConnectionState.Open) == 0)
        _utilCon.Open();
      try
      {
        foreach (RoutineInfo ri in _preinstrumentedRoutines.Values)
        {
          string rName = string.Format( "{0}.{1}", 
            ( string.IsNullOrEmpty( ri.Schema )? _utilCon.Database : ri.Schema ), ri.Name );
          ExecuteScalar(string.Format("drop {0} {1}", ri.Type, rName));
          Debug.WriteLine(string.Format("Debugger: Restoring {0}", rName));
          string db = _utilCon.Database;
          if( !string.IsNullOrEmpty( ri.Schema ) )
          {
            ExecuteRaw(string.Format( "use `{0}`", ri.Schema ));
          }
          ExecuteRaw( string.Format( "delimiter //\n{0};\n //", ri.SourceCode ));
          ExecuteRaw(string.Format("use `{0}`", db));
        }
      }
      finally
      {
        _utilCon.Close();
        _preinstrumentedRoutines.Clear();
      }
    }

    private void ExecuteSetupScripts()
    {
      MySqlConnection con = new MySqlConnection(_connection.ConnectionString);      
      Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MySql.Debugger.MySql_Scripts.Schema.sql");
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Close();

      con.Open();
      try
      {
        MySqlScript script = new MySqlScript(con);
        script.Query = sql;
        script.Delimiter = "//";
        script.Execute();

        stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MySql.Debugger.MySql_Scripts.Logic.sql");
        sr = new StreamReader(stream);
        sql = sr.ReadToEnd();
        sr.Close();
        script.Query = sql;
        script.Execute();
      }
      finally
      {
        con.Close();
      }
    }

    public void Run(string[] values, string[] InOutValues)
    { 
      _stopping = false;
      /*
       * - Start stepping in code.
       * - Always get sp code. 
       * - Instrument first routine (even if not in step into, because can be breakpoints defined).
       * - Keep a backup of sp (in a folder specific to the debug session).
       * - PreInstrument the code (need to keep track of line number with local vars).
       * - Wait for lock with debugged code.
       * - Start sp execution in async call, but keep track of result (like failure 
       *   to execute due to permissions, or no longer reachable code).
       * */
      IsRunning = true;
      // Open connections...
      if ((_connection.State & ConnectionState.Open) == 0)
        _connection.Open();
      if ((_utilCon.State & ConnectionState.Open) == 0)
        _utilCon.Open();
      if ((_lockingCon.State & ConnectionState.Open) == 0)
        _lockingCon.Open();
      try
      {
        ExecuteSetupScripts();
        // for now hardcoded
        DebugSessionId = 1;
        // Clean debugscope table.
        CleanupScopeAll();
        // setup backup folder.
        //DirectoryInfo di = Directory.CreateDirectory(
        //  Path.Combine(@"..\..\backup", DebugSessionId.ToString()));

        DirectoryInfo di = Directory.CreateDirectory(
          Path.Combine(Path.Combine(Path.GetTempPath(), _backupDirGuid), DebugSessionId.ToString()));

        BackupPath = di.FullName;
        StringBuilder sb = new StringBuilder();
        CommonTokenStream cts = null;
        MySQL51Parser.program_return r = ParseSql(SqlInput, false, out sb, out cts);
        //RoutineInfo ri = GetRoutineFromDb("", true);
        CommonTree t = (CommonTree)r.Tree;
        if (t.IsNil)
          t = (CommonTree)t.GetChild(0);
        RoutineInfo ri = MakeRoutineInfo(t, cts, SqlInput);
        // Instrument the code (need to keep track of line number with local vars).
        Debug.WriteLine(string.Format("Debugger: Instrumenting {0}.{1}", ri.Schema, ri.Name ));
        SetCurrentScopeLevel(0);
        InstrumentRoutine( ri );
        AddToPreinstrumentedRoutines(ri);
        RoutineScope rs = GetRoutineScopeFromRoutineInfo(ri);
        // Start sp execution in async call, but keep track of result (like failure 
        // to execute due to permissions, or no longer reachable code).
        // Make call statement.
        MakeScopeCreateProc(ri, values, InOutValues);
        _sqlToRun = CurrentScope.OwningRoutine.SourceCode;
        // run in background worker
        ExecuteScalar("set net_write_timeout=999999;");
        ExecuteScalar("set net_read_timeout=999999;");
        // this way debugger can see table updates by debuggee instrumented code when debuggee is within a transaction 
        // (ie. when a function is being invoked).
        ExecuteScalar("set session transaction isolation level read uncommitted;");
        SetNoDebuggingFlag(1, _utilCon);
        SetDebuggeeLock();
        SetDebuggerLock();
        _completed = false;
        RaiseStartDebugger();
        try
        {
          _worker.RunWorkerAsync();
          do
          {
            // Wait for lock with debuggee code.
            while (true)
            {
              /* busy wait */
              int netWriteTimeout = Convert.ToInt32(ExecuteScalar("select  @@global.net_write_timeout"));
              if (netWriteTimeout != (NET_WRITE_TIMEOUT_BASE_VALUE + 1)) break;
              Thread.Sleep(100);
            }
            ExecuteScalarLongWait("select get_lock( 'lock1', 999999 );");
            SetDebuggerLock();
            // checking the flags inside the critical section avoids a very unlikely race condition.
            if (_errorOnAsync)
            {
              throw new DebuggerException("ErrorOnAsync", _asyncError);
            }
            if (_completed)
            {
              break;
            }
            GetCurrentScopeLevel();
            if (_prevScopeLevel > _scopeLevel)
            {
              // current scope is caller
              _scope.Pop();
              if (_scopeLevel != 0)
                LoadScopeVars(_scopeLevel);
              RaiseEndScope( _scope.Peek().OwningRoutine );
            }
            else if (_prevScopeLevel < _scopeLevel)
            {
              // current scope is callee, create new scope
              string routineName = GetCurrentRoutine();
              // Get new routine source code
              RoutineInfo newRi = GetRoutineInfo(routineName);
              //MakeRoutineInfo((CommonTree)null, (CommonTokenStream)null, "");
              if (string.IsNullOrEmpty(newRi.InstrumentedSourceCode))
              {
                // instrument it ...
                InstrumentRoutine(newRi);
              }
              RoutineScope newScope = GetRoutineScopeFromRoutineInfo(newRi);
              _scope.Push(newScope);
              LoadScopeVars(_scopeLevel);
              RaiseStartScope(_scope.Peek().OwningRoutine);
            }
            else
            {
              LoadScopeVars(_scopeLevel);
            }
            int lineNumber = GetCurrentLineNumber();
            int colNumber = GetCurrentColNumber();
            // Locate next statement to debug & preinstrument current statement dependencies
            CommonTree nextCt = GetStatementFromPosition(lineNumber, colNumber);
            CheckBreakpoints(lineNumber, colNumber, nextCt);
            if (_completed) break;
            // Preinstrument it
            if (nextCt != null)
            {
              // no valid stmt for the 'end' of a begin-end block.
              PreinstrumentStatement(nextCt);
            }
            // Release locks for further execution of debuggee.
            ExecuteScalar("select release_lock( 'lock1' );");
            // Hint mysql thread scheduler to give debuggee a chance to execute
            ExecuteScalar("select sleep( 0.010 );");

          } while (true);
        }
        finally
        {
          ExecuteScalar("select release_lock( 'lock1' );");
        }
      }
      finally
      {
        if (!_stopping)
        {
          try { this.Stop(); }
          catch { }
        }
        try { _utilCon.Close(); } catch { }
        try { _lockingCon.Close(); } catch { }
        try { _connection.Close(); } catch { }
        IsRunning = false;
        if (RestoreAtExit)
        {
          RestoreRoutinesBackup();
        }
        RaiseEndDebugger();
      }
    }

    public bool RestoreAtExit { get; set; }

    public delegate void StartDebugger();
    public event StartDebugger OnStartDebugger;
    private void RaiseStartDebugger()
    {
      if (OnStartDebugger != null)
        OnStartDebugger();
    }

    public delegate void EndDebugger();
    public event EndDebugger OnEndDebugger;
    public void RaiseEndDebugger()
    {
      if( OnEndDebugger != null )
        OnEndDebugger();
    }

    private RoutineScope GetRoutineScopeFromRoutineInfo(RoutineInfo ri)
    {
      RoutineScope rs = new RoutineScope() { OwningRoutine = ri, Variables = new Dictionary<string,StoreType>() };
      foreach (StoreType st in ri.Locals.Values)
      {
        rs.Variables.Add(st.Name, new StoreType(st));
      }
      return rs;
    }

    /// <summary>
    /// Given a routien name, returns its defintion along with type.
    /// </summary>
    /// <param name="Name"></param>
    /// <returns></returns>
    internal RoutineInfo GetRoutineInfo(string Name)
    {
      string[] fullName = RoutineInfo.GetFullName(_utilCon.Database, Name);
      RoutineInfoType type = RoutineInfoType.Trigger;
      MySqlCommand cmd = new MySqlCommand( string.Format( 
        "select routine_type from information_schema.routines where ( routine_name = '{0}' ) and ( routine_schema = '{1}' )", 
        fullName[ 1 ], fullName[ 0 ] ), _utilCon );
      MySqlDataReader r = cmd.ExecuteReader();
      try
      {
        // No triggers are listed in this query, if no data returned, a trigger is assumed.
        if (r.Read())
        {
          string sType = r.GetString( 0 );
          type = (RoutineInfoType)Enum.Parse( typeof( RoutineInfoType ), sType, true );
        }
      }
      finally
      {
        r.Close();
      }
      return LookupRoutine(string.Join(".", fullName), type);
    }

    internal RoutineInfo LookupRoutine(string Name, RoutineInfoType type)
    {
      RoutineInfo routine = null;
      if (!_preinstrumentedRoutines.TryGetValue(Name, out routine))
      {
        routine = GetRoutineFromDb(Name, type);
        RegisterRoutine(Name, routine);
      }
      return routine;
    }

    internal void RegisterRoutine(string sName, RoutineInfo r)
    {
      _preinstrumentedRoutines.Add(sName, r);
    }

    /// <summary>
    /// Look for routine defintiion in the database with "show create procedure" or information_schema.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private RoutineInfo GetRoutineFromDb(string sName, RoutineInfoType type)
    {
      string sql;

      if (type == RoutineInfoType.Trigger)
      {
        // there is no such thing as "show create trigger"
        string[] fullName = sName.Split('.');
        if (fullName.Length != 2)
          fullName = string.Format("{0}.{1}", _utilCon.Database, sName).Split('.');
        sql = GetCreateTriggerFor( fullName[ 0 ], fullName[ 1 ] );
      }
      else
      {
        sql = GetFieldAsString( string.Format("show create {0} {1}", type.ToString(), sName), 2);
        sql = sql.Replace("\r", string.Empty).Replace("\n", Environment.NewLine);
      }
      StringBuilder sb;
      CommonTokenStream cts;
      CommonTree t = (CommonTree)(ParseSql(sql, false, out sb, out cts).Tree);
      if (t.IsNil)
      {
        t = (CommonTree)t.GetChild(0);
      }
      return new RoutineInfo()
      {
        Name = RoutineInfo.GetRoutineName( sName ),
        ParsedTree = t,
        TokenStream = cts,
        SourceCode = sql,
        Type = type
      };
    }

    /// <summary>
    /// Get trigger definition.
    /// </summary>
    /// <param name="TriggerSchema"></param>
    /// <param name="TriggerName"></param>
    /// <returns></returns>
    private string GetCreateTriggerFor(string TriggerSchema, string TriggerName)
    {
      // TODO: The "definer user" is lost (no way to retrieve it from information_schema.triggers
      string eventManipulation = string.Empty;
      string actionStmt = string.Empty;
      string actionTiming = string.Empty;
      string eventObjectTable = string.Empty;
      string eventObjectSchema = string.Empty;

      MySqlCommand cmd = new MySqlCommand( string.Format(
        @"select event_manipulation, event_object_schema, event_object_table, action_statement, action_timing 
          from information_schema.triggers where ( trigger_schema = '{0}' ) and ( trigger_name = '{1}' )",
        TriggerSchema, TriggerName ), _utilCon );
      MySqlDataReader r = cmd.ExecuteReader();
      try {
        r.Read();
        if (r.HasRows)
        {
          eventManipulation = r.GetString(0);
          eventObjectSchema = r.GetString(1);
          eventObjectTable = r.GetString(2);
          actionStmt = r.GetString(3);
          actionTiming = r.GetString(4);
        }
      } finally {
        r.Close();
      }
      // Format is "create trigger trigger_name trigger_time trigger_event on tblName for each row body"
      return string.Format("create trigger {0} {1} {2} on {3} for each row {4}", TriggerName, actionTiming, eventManipulation, 
        string.Format( "{0}.{1}", eventObjectSchema, eventObjectTable ), actionStmt );
    }

    private void GetCurrentScopeLevel()
    {
      _scopeLevel = Convert.ToInt32(ExecuteScalar("select `serversidedebugger`.`DebugData`.`Val` from `serversidedebugger`.`DebugData` where `serversidedebugger`.`DebugData`.`Id` = 1"));
    }

    private void SetCurrentScopeLevel(int newScope)
    {
      _scopeLevel = newScope;
      ExecuteRaw(string.Format("update `serversidedebugger`.`DebugData` set `serversidedebugger`.`DebugData`.`Val` = {0} where `serversidedebugger`.`DebugData`.`Id` = 1;", newScope));
      //ExecuteScalar(string.Format("set {0} = {1};", VAR_DBG_SCOPE_LEVEL, newScope));
    }

    private bool _stopping = false;
    public void Stop()
    {
      _stopping = true;
      this._worker.CancelAsync();
      _completed = true;
      try {
        MySqlConnection con = new MySqlConnection(_connection.ConnectionString);
        con.Open();
        MySqlCommand cmd = new MySqlCommand(string.Format("kill {0}", _connection.ServerThread), con);
        cmd.ExecuteNonQuery();
        con.Close();        
      }
      catch { }
      try { _connection.Close(); }
      catch { }
    }

    private void AddToPreinstrumentedRoutines(RoutineInfo ri)
    {
      _preinstrumentedRoutines.Add(string.Format("{0}", ri.GetFullName(_connection.Database)), ri);
    }

    private void SetDebuggeeLock()
    {
      MySqlCommand cmd = new MySqlCommand("select get_lock( 'lock1', 0 )", _connection);
      int value = Convert.ToInt32(cmd.ExecuteScalar());
      if( value == 0 )
        throw new InvalidOperationException( "Cannot take lock1." );
    }

    private const int NET_WRITE_TIMEOUT_BASE_VALUE = 999998;

    private void SetDebuggerLock()
    {
      MySqlCommand cmd = new MySqlCommand(string.Format("set @@global.net_write_timeout = {0} + 1;", NET_WRITE_TIMEOUT_BASE_VALUE), _lockingCon);
      cmd.ExecuteNonQuery();
    }

    private void ReleaseDebuggerLock()
    {
      MySqlCommand cmd = new MySqlCommand("unlock tables;", _lockingCon);
      cmd.ExecuteNonQuery();
    }

    private void ReleaseDebuggeeLock()
    {
      MySqlCommand cmd = new MySqlCommand("do release_lock( 'lock1' );", _connection);
      cmd.ExecuteNonQuery();
    }

    private CommonTree GetStatementFromPosition(int lineNumber, int colNumber)
    {
      RoutineInfo ri = CurrentScope.OwningRoutine;
      return ri.GetStatementFromPos(lineNumber, colNumber);
    }

    /// <summary>
    /// Generates a call statement for a given routine.
    /// The routine has already been instrumented.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="ArgsValues"></param>
    private void MakeScopeCreateProc(RoutineInfo ri, string[] ArgsValues, string[] InOutArgsValues)
    {
      CommonTree t = ri.ParsedTree;
      StringBuilder sb = new StringBuilder();
      string spName = ri.Name;
      // build call
      StringBuilder sbCall = new StringBuilder(string.Format("call `{0}`( ", spName));
      // adds inout arguments definition before the call statement
      if (InOutArgsValues != null && InOutArgsValues.Length > 0)
        sbCall.Insert(0, "SET " + string.Join(";SET ", InOutArgsValues) + ";");
      if (ArgsValues != null)
      {
        foreach (string val in ArgsValues)
        {
          sbCall.Append(val).Append(',');
        }
      }
      sbCall.Length--;
      sbCall.Append(");");
      // Parse & create scope
      CommonTokenStream cts;
      CommonTree callTree = (CommonTree)(ParseSql(sbCall.ToString(), false, out sb, out cts).Tree);
      if (callTree.IsNil)
        callTree = (CommonTree)callTree.GetChild(0);
      _scope.Push(
        new RoutineScope() {
          OwningRoutine = new RoutineInfo() {
            ParsedTree = callTree,
            Name = "<main>",
            TokenStream = cts,
            SourceCode = sbCall.ToString() },
          Variables = new Dictionary<string, StoreType>() });
    }

    public static string GetRoutineName(string sql)
    {
      MySQL51Parser.program_return r = new MySQL51Parser.program_return();
      StringBuilder sb;
      bool expectErrors = false;
      CommonTokenStream cts;
      
      MemoryStream ms = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(sql));
      CaseInsensitiveInputStream input = new CaseInsensitiveInputStream(ms);
      MySQLLexer lexer = new MySQLLexer(input);
      CommonTokenStream tokens = new CommonTokenStream(lexer);
      MySQLParser parser = new MySQLParser(tokens);
      sb = new StringBuilder();
      TextWriter tw = new StringWriter(sb);
      parser.TraceDestination = tw;
      try
      {
        r = parser.program();
      }
      catch (RewriteEmptyStreamException e)
      {
        sb.AppendLine();
        sb.Append(e.Message);
      }
      cts = tokens;
      if (!expectErrors && sb.Length != 0)
      {
        throw new DebugSyntaxException(sb.ToString());
      }
      CommonTree t = (CommonTree)r.Tree;
      if (t.IsNil)
        t = (CommonTree)t.GetChild(0);
      return GetRoutineName(t);
    }

    private static string GetRoutineName(CommonTree t)
    {
      string name = null;
      if (Cmp(t.GetChild(1).Text, "definer") == 0)
        name = t.GetChild(3).Text;
      else
        name = t.GetChild(1).Text;
      return name;
    }

    private RoutineInfo MakeRoutineInfo(CommonTree t, CommonTokenStream cts, string sql)
    {
      string name = Debugger.GetRoutineName(t);
      RoutineInfo ri = new RoutineInfo() {
        Name = name,
        SourceCode = sql,
        ParsedTree = t,
        Type = RoutineInfoType.Procedure,
        TokenStream = cts
      };
      return ri;
    }

    private int GetCurrentLineNumber()
    {
      return Convert.ToInt32(CurrentScope.Variables["@@@lineno"].Value);
    }

    private int GetCurrentColNumber()
    {
      return Convert.ToInt32(CurrentScope.Variables["@@@colno"].Value);
    }

    // Scope of variables
    internal Stack<RoutineScope> _scope = new Stack<RoutineScope>();
    
    internal Stack<RoutineScope> Scope { get { return _scope; } }

    public IEnumerable<RoutineScope> GetScopes()
    {
      return (Scope as IEnumerable<RoutineScope>);
    }

    public RoutineScope CurrentScope { get { return _scope.Peek(); } }

    private Dictionary<string, StoreType> LoadScopeVars(int DebugScopeLevel)
    {
      string sql =
        @"select cast( `serversidedebugger`.`DebugScope`.`VarValue` as {3} ) from `serversidedebugger`.`DebugScope` where `serversidedebugger`.`DebugScope`.`DebugSessionId` = {0} and `serversidedebugger`.`DebugScope`.`DebugScopeLevel` = {1} and `serversidedebugger`.`DebugScope`.`VarName` = '{2}' and `serversidedebugger`.`DebugScope`.`Id` = ( select max( `serversidedebugger`.`DebugScope`.`Id` ) from `serversidedebugger`.`DebugScope` where `serversidedebugger`.`DebugScope`.`DebugSessionId` = {0} and `serversidedebugger`.`DebugScope`.`DebugScopeLevel` = {1} and `serversidedebugger`.`DebugScope`.`VarName` = '{2}' )";
      Dictionary<string, StoreType> vars = CurrentScope.Variables;
      MySqlCommand cmd = new MySqlCommand(sql, _utilCon);
      foreach (StoreType st in vars.Values)
      {
        cmd.CommandText = string.Format(sql, DebugSessionId, DebugScopeLevel, st.Name, st.GetCastExpressionFromBinary());
        MySqlDataReader r = cmd.ExecuteReader();
        try
        {
          r.Read();
          if (r.IsDBNull(0))
            st.Value = DBNull.Value;
          else
            // using this instead of r.GetString( 0 ) to cover the case when date is zeroed.
            st.Value = r.GetValue(0).ToString();
        }
        finally
        {
          r.Close();
        }
      }
      return vars;
    }

    /// <summary>
    /// Writes locals values back to the routine scope.
    /// </summary>
    public void CommitLocals()
    {
      StringBuilder sql = new StringBuilder();
      foreach (StoreType st in CurrentScope.Variables.Values)
      {
        if (st.VarKind == VarKindEnum.Internal) continue;
        if (st.ValueChanged)
        {
          //sql.Append("replace `serversidedebugger`.`DebugScope`( DebugSessionId, DebugScopeLevel, VarName, VarValue ) ").
          //  AppendFormat("values ( {0}, {1}, '{2}', cast( {3} as binary ) );", DebugSessionId, _scopeLevel, st.Name, st.WrapValue()).AppendLine();
          sql.AppendFormat(" call `serversidedebugger`.`SetDebugScopeVar`( {0}, {1}, '{2}', cast( {3} as binary ) );", DebugSessionId, _scopeLevel, st.Name, st.WrapValue()).AppendLine();
          st.ValueChanged = false;
        }
      }
      if (sql.Length != 0)
      {
        MySqlScript script = new MySqlScript(_utilCon, sql.ToString());
        script.Execute();
      }
    }
    
    public bool IsRunning { get; private set; }

    public delegate void ScopeChangedHandler(RoutineInfo r);

    public event ScopeChangedHandler OnStartScope;
    public event ScopeChangedHandler OnEndScope;

    internal void RaiseStartScope(RoutineInfo r)
    {
      if (OnStartScope != null)
        OnStartScope(r);
    }

    internal void RaiseEndScope(RoutineInfo r)
    {
      if (OnEndScope != null)
        OnEndScope(r);
    }

    public delegate void BreakpointHandler(Breakpoint bp);

    public event BreakpointHandler OnBreakpoint;

    internal void RaiseBreakpoint(Breakpoint bp)
    {
      if (OnBreakpoint != null)
        OnBreakpoint(bp);
    }

    internal void HitBreakpoint(Breakpoint bp)
    {
      if (bp.IsTriggered())
        RaiseBreakpoint(bp);
    }

    public static int GetTagHashCode(string tag)
    {
      // Normalize string
      tag = tag.Replace(" ", "").Replace("\n", "").Replace("\r", "");
      return tag.GetHashCode();
    }

    public static string NormalizeTag(string tag)
    {
      return tag.Replace(" ", "").Replace("\n", "").Replace("\r", "");
    }

    // Scope nesting level
    private int _prevScopeLevel;
    private int __scopeLevel;
    private int _scopeLevel
    {
      get
      {
        return __scopeLevel;
      }
      set
      {
        _prevScopeLevel = __scopeLevel;
        __scopeLevel = value;
      }
    }

    // Scope level to jump in the next step out.
    private int nextStepOut = -1;

    private int stepOverScope = -1;

    internal void CheckBreakpoints(int line, int colNumber, CommonTree ct)
    {
      int startColumn = 0;
      int endColumn = UInt16.MaxValue;
      int startLine = 0;
      int endLine = 0;
      RoutineInfo ri = CurrentScope.OwningRoutine;
      if ( (ct != null) && 
        ( Cmp( ct.Text, "if" ) != 0 ) && ( Cmp( ct.Text, "while" ) != 0 ) &&
        ( Cmp( ct.Text, "repeat" ) != 0 ) && ( Cmp( ct.Text, "loop" ) != 0 ) &&
        ( Cmp( ct.Text, "case_stmt" ) != 0 ) && ( Cmp( ct.Text, "declare_handler" ) != 0 ))
      {
        IToken tkStart = ri.TokenStream.Get(ct.TokenStartIndex);
        IToken tkEnd = ri.TokenStream.Get(ct.TokenStopIndex + 1);
        if (tkEnd == null)
          tkEnd = ri.TokenStream.Get(ct.TokenStopIndex);
        startColumn = tkStart.CharPositionInLine;
        endColumn = tkEnd.CharPositionInLine + 1;
        startLine = tkStart.Line;
        endLine = tkEnd.Line;
      }
      else
      {
        startLine = endLine = line;
        startColumn = colNumber;
      }
      int hash = GetTagHashCode(ri.SourceCode);
      string routineName = ri.GetFullName(_utilCon.Database);
      Breakpoint fakeBreakpoint = new Breakpoint( this ) { StartLine = startLine, EndLine = endLine, IsFake = true, Hash = hash, 
        RoutineName = routineName, StartColumn = startColumn, EndColumn = endColumn };
      CurrentScope.CurrentPosition = fakeBreakpoint;
      // Breakpoints on the same line but different files are treated by making a breakpoint uniquely identified
      // by line number and hash of current routine source code.
      if (OnBreakpoint == null) return;
      if (SteppingType != SteppingTypeEnum.StepOver)
      {
        stepOverScope = __scopeLevel;
      }
      if (SteppingType == SteppingTypeEnum.StepOver)
      {
        bool fireBp = false;
        if (stepOverScope != -1)
        {
          if (__scopeLevel == stepOverScope)
          {
            fireBp = true;
          }
        }
        else
          fireBp = true;
        if (fireBp)
          RaiseBreakpoint(fakeBreakpoint);
        return;
      }
      else if (SteppingType == SteppingTypeEnum.StepInto)
      {
        RaiseBreakpoint(fakeBreakpoint);
        return;
      }
      else if (SteppingType == SteppingTypeEnum.StepOut)
      {
        nextStepOut = _scopeLevel - 1;
        SteppingType = SteppingTypeEnum.None;
      }
      else if (nextStepOut != -1)
      {
        if (_scopeLevel == nextStepOut)
        {
          nextStepOut = -1;
          RaiseBreakpoint(fakeBreakpoint);
          return;
        }
      }
      BreakpointKey bpKey = new BreakpointKey() { Line = line, Hash = hash };
      Breakpoint bp = null;
      if (_breakpoints.TryGetValue(bpKey, out bp) && !bp.Disabled)
      {
        stepOverScope = __scopeLevel;
        HitBreakpoint(bp);
      }
    }

    private Dictionary<BreakpointKey, Breakpoint> _breakpoints = new Dictionary<BreakpointKey, Breakpoint>( new BreakpointKeyDictionaryComparer() );
    private List<Watch> _watches = new List<Watch>();
    
    public Breakpoint SetBreakpoint( string tag, int line)
    {
      BreakpointKey bpKey = new BreakpointKey() { Tag = tag, Line = line };
      Breakpoint bp = null;
      if (_breakpoints.TryGetValue(bpKey, out bp))
      {
        return bp;
      }
      else
      {
        bp = new Breakpoint( this ) { Hash = bpKey.Hash, StartLine = line };
        _breakpoints.Add( bpKey, bp );
        return bp;
      }
    }

    /// <summary>
    /// Returns true if the line number if valid for putting a breakpoint.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public bool CanBindBreakpoint(int line)
    {
      RoutineInfo ri = CurrentScope.OwningRoutine;
      return ri.HasLineValidStatement(line);
    }

    public Watch SetWatch(string expression)
    {
      Watch result;
      _watches.Add(result = new Watch() { Debugger = this, Expression = expression });
      return result;
    }

    private BackgroundWorker _worker = new BackgroundWorker();

    private volatile bool _errorOnAsync = false;
    private volatile Exception _asyncError = null;
    private string _sqlToRun = "";

    private void SetNoDebuggingFlag(int newValue, MySqlConnection con )
    {
      MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = con;
      cmd.CommandText = string.Format("set {0} = {1};", VAR_DBG_NO_DEBUGGING, newValue);
      cmd.ExecuteNonQuery();
    }

    private void worker_DoWork(object sender, DoWorkEventArgs e)
    {
      MySqlCommand cmd = new MySqlCommand("", _connection);
      try
      {
        // long command timeout so it spans the full debug session time.
        cmd.CommandTimeout = Int32.MaxValue / 1000;

        // net_xxx are set to avoid EndOfStreamException
        cmd.CommandText = "set net_write_timeout=999998;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "set net_read_timeout=999998;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "set session transaction isolation level read uncommitted;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = string.Format( "set {0} = last_insert_id();", VAR_DBG_LAST_INSERT_ID);
        cmd.ExecuteNonQuery();
        cmd.CommandText = string.Format("set {0} = found_rows();", VAR_DBG_FOUND_ROWS);
        cmd.ExecuteNonQuery();
        cmd.CommandText = string.Format("set {0} = row_count();", VAR_DBG_ROW_COUNT);
        cmd.ExecuteNonQuery();
        SetNoDebuggingFlag(0, _connection);
        //cmd.CommandText = string.Format("set {0} = 0;", VAR_DBG_SCOPE_LEVEL);
        //cmd.ExecuteNonQuery();

        // run the command
        cmd.CommandText = _sqlToRun;
        cmd.ExecuteNonQuery();
      }
      catch (ThreadAbortException tae) 
      {
        // nothing
      }
      catch (Exception ex)
      {
        if (!_stopping)
        {
          _asyncError = ex;
          _errorOnAsync = true;
        }
      }
      finally
      {
        // Release debuggee lock
        if (!_completed && !_stopping)
        {
          cmd.CommandText = string.Format("set @@global.net_write_timeout = {0};", NET_WRITE_TIMEOUT_BASE_VALUE);
          cmd.ExecuteNonQuery();
          ReleaseDebuggeeLock();
        }
        _completed = true;
      }
    }

    private volatile bool _completed = false;

    private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      // Notify end of debug session to clients      
    }    

    /// <summary>
    /// Return the fieldNum-th column of the first row of sql statement.
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="fieldNum"></param>
    /// <returns></returns>
    private string GetFieldAsString(string sql, int fieldNum)
    {
      MySqlCommand cmd = new MySqlCommand(sql, _utilCon);
      MySqlDataReader r = cmd.ExecuteReader();
      string data = String.Empty;
      try
      {
        r.Read();
        data = r.GetString(fieldNum);
      }
      finally
      {
        r.Close();
      }
      return data;
    }

    private string BackupPath;

    /// <summary>
    /// The id of the debug session, used for making backup folders.
    /// </summary>
    internal int DebugSessionId { get; set; }

    /// <summary>
    /// Instruments a routine.
    /// </summary>
    /// <param name="routine"></param>
    private void InstrumentRoutine( RoutineInfo routine )
    {
      /*
       * - Parse formal arguments.
       * - Parse begin block.
       * - For each begin block parse declare's, then instructions.
       * - When finish parsing tree, make a backup.
       * - Then instrument it.
       * */
      // Parse args
      StringBuilder sb = new StringBuilder();
      CommonTokenStream tokenStream;
      MySQL51Parser.program_return tree = 
        ParseSql(routine.SourceCode, false, out sb, out tokenStream);
      routine.ParsedTree = ( CommonTree )( tree.Tree );
      if (routine.ParsedTree.IsNil)
      {
        routine.ParsedTree = ( CommonTree )routine.ParsedTree.GetChild(0);
      }
      routine.TokenStream = tokenStream;
      Dictionary<string, StoreType> args = ParseArgs( routine.ParsedTree );
      
      
      // Parse Begin block
      CommonTree beginEnd = GetBeginEnd(routine.ParsedTree);
      // There won't be always a begin/end block.
      if (beginEnd == null)
      {
        // begin/end block
        StringBuilder sbNewRoutineSql = new StringBuilder();
        ITree lastChild = routine.ParsedTree.GetChild(routine.ParsedTree.ChildCount - 1);
        ConcatTokens(sbNewRoutineSql, routine.TokenStream, 0, lastChild.TokenStartIndex - 1);
        sbNewRoutineSql.AppendLine();
        sbNewRoutineSql.AppendLine("begin");
        ConcatTokens(sbNewRoutineSql, routine.TokenStream, lastChild.TokenStartIndex, lastChild.TokenStopIndex);
        if ( Cmp( routine.TokenStream.Get( lastChild.TokenStopIndex ).Text, ";" ) != 0 )
        {
          sbNewRoutineSql.Append(';');
        }
        sbNewRoutineSql.AppendLine();
        sbNewRoutineSql.AppendLine( "end" );
        StringBuilder sbErrors;
        tokenStream = null;
        string sSql = sbNewRoutineSql.ToString();
        tree = ParseSql(sSql, false, out sbErrors, out tokenStream);
        routine.TokenStream = tokenStream;
        routine.SourceCode = sSql;
        routine.ParsedTree = (CommonTree)(tree.Tree);
        if (routine.ParsedTree.IsNil)
        {
          routine.ParsedTree = (CommonTree)routine.ParsedTree.GetChild(0);
        }
        // No need to reparse args, just begin/end
        beginEnd = GetBeginEnd(routine.ParsedTree);
      }
      // Get declare variables, sessions, new & old
      ParseDeclares(beginEnd, args);
      ParseSessions(tokenStream, args);
      RegisterInternalVars(args);
      if (routine.Type == RoutineInfoType.Trigger)
      {
        RegisterNewOldVars(routine, args);
      }

      // forces to backup the routine
      if (Scope.Count == 0)
      {
        RoutineScope tempScope = new RoutineScope();
        tempScope.OwningRoutine = routine;
        tempScope.GetFileName();
      }
      else
        CurrentScope.GetFileName();

      // generate instrumentation code
      StringBuilder preInscode = new StringBuilder();
      // track internal variables...
      // Workaround: row_count() is affected by any precious SET statement, so must be first
      preInscode.AppendFormat("set {0} = row_count();", VAR_DBG_ROW_COUNT);
      preInscode.AppendLine();
      preInscode.AppendFormat("set {0} = last_insert_id();", VAR_DBG_LAST_INSERT_ID);
      preInscode.AppendLine();
      preInscode.AppendFormat("set {0} = found_rows();", VAR_DBG_FOUND_ROWS);
      preInscode.AppendLine();
      preInscode.Append(" call `serversidedebugger`.`SetDebugScopeVar`( {3}, {0}, '@@@lineno', {1} );" ).AppendLine();      
      preInscode.Append(" call `serversidedebugger`.`SetDebugScopeVar`( {3}, {0}, '@@@colno', {4} );").AppendLine();      
      // ...and user variables
      foreach (StoreType st in args.Values)
      {
        if (st.VarKind == VarKindEnum.Internal) continue;        
        preInscode.AppendLine("call `serversidedebugger`.`SetDebugScopeVar`( {3}, {0}, ").
          AppendFormat("'{0}', cast( {0} as binary ) );", st.Name).AppendLine();
      }
      StringBuilder postInscode = new StringBuilder();
      foreach (StoreType st in args.Values)
      {
        if (st.VarKind == VarKindEnum.Internal) continue;
        postInscode.AppendFormat(
          @"set {0} = ( select cast( `serversidedebugger`.`DebugScope`.`VarValue` as {1} ) from `serversidedebugger`.`DebugScope` where `serversidedebugger`.`DebugScope`.`DebugSessionId` = {2} ", st.Name, st.GetCastExpressionFromBinary(), DebugSessionId).
          AppendFormat(" and `serversidedebugger`.`DebugScope`.`DebugScopeLevel` = {0} and `serversidedebugger`.`DebugScope`.`VarName` = '{1}' and `serversidedebugger`.`DebugScope`.`Id` = ( select max( `serversidedebugger`.`DebugScope`.`Id` ) from `serversidedebugger`.`DebugScope` where `serversidedebugger`.`DebugScope`.`DebugSessionId` = {2} and `serversidedebugger`.`DebugScope`.`DebugScopeLevel` = {0} and `serversidedebugger`.`DebugScope`.`VarName` = '{1}' ));",
            "{0}", st.Name, DebugSessionId );
        postInscode.AppendLine();
      }
      routine.PreInstrumentationCode = preInscode.ToString();
      routine.PostInstrumentationCode = postInscode.ToString();
      routine.Locals = args;
      // finally instrument.
      StringBuilder sql = new StringBuilder();
      // Instrumeting code...
      GenerateInstrumentedCode(routine, sql);
      routine.InstrumentedSourceCode = sql.ToString();
      string sqlDrop = string.Format("drop {0} {1}", routine.Type.ToString(), routine.Name);
      string db = _utilCon.Database;
      if (!string.IsNullOrEmpty(routine.Schema))
      {
        ExecuteRaw(string.Format("use `{0}`", routine.Schema ));
      }
      ExecuteRaw(sqlDrop);
      try
      {
        ExecuteRaw(string.Format("delimiter //\n{0}\n//", routine.InstrumentedSourceCode));
      }
      catch (Exception)
      {
        // In case of exception restore previous non-instrumented version.
        ExecuteRaw(string.Format("delimiter //\n{0}\n//", routine.SourceCode));
        throw;
      }
      finally
      {
        ExecuteRaw(string.Format("use `{0}`", db));
      }
    }

    private void RegisterInternalVars(Dictionary<string, StoreType> vars)
    {
      vars.Add("@@@lineno", new StoreType() { Name = "@@@lineno", VarKind = VarKindEnum.Internal, Value = 1, Type = "int" });
      vars.Add("@@@colno", new StoreType() { Name = "@@@colno", VarKind = VarKindEnum.Internal, Value = 1, Type = "int" });
    }

    /// <summary>
    /// Returns begin/end for a create routine block.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    private CommonTree GetBeginEnd(CommonTree t)
    {
      CommonTree beginEnd = null;
      int i = 0;
      while (i < t.ChildCount && Cmp(t.GetChild(i).Text, "begin_end") != 0)
        i++;
      if (i < t.ChildCount)
        beginEnd = (CommonTree)t.GetChild(i);
      return beginEnd;
    }

    private void ExecuteRaw(string sql)
    {
      MySqlScript script = new MySqlScript(_utilCon, sql);
      script.Execute();
    }

    private object ExecuteScalar(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, _utilCon);
      return cmd.ExecuteScalar();
    }

    private object ExecuteScalarLongWait(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, _utilCon);
      cmd.CommandTimeout = Int32.MaxValue / 1000;
      return cmd.ExecuteScalar();
    }

    private void GenerateInstrumentedCode(
      RoutineInfo routine,
      StringBuilder sql)
    {
      // Get initial begin_end,
      // TODO: there may be no begin/end.
      CommonTree t = routine.ParsedTree;
      CommonTree beginEnd = null;
      int i = 0;
      while (i < t.ChildCount && Debugger.Cmp(t.GetChild(i).Text, "begin_end") != 0)
        i++;
      beginEnd = (CommonTree)t.GetChild(i);
      routine.BeginEnd = beginEnd;
      // generate stub method "create proc... begin"
      ConcatTokens(sql, routine.TokenStream, t.TokenStartIndex, beginEnd.TokenStartIndex - 1);
      if( Cmp( beginEnd.GetChild( 0 ).Text, "label" ) == 0 )
      {
        routine._leaveLabel = beginEnd.GetChild(0).GetChild(0).Text;
      }
      
      // Generate scope entry:
      if (!string.IsNullOrEmpty(routine._leaveLabel))
      {
        sql.Append(routine._leaveLabel).Append(" : ");
      }
      sql.AppendLine( "begin" );
      sql.AppendLine("#begin scope");
      // Generate recursive code:
      routine._endOfDeclare = false;
      GenerateInstrumentedCodeRecursive(beginEnd.Children, routine, sql);
      // Generate scope exit:
      if (routine.Type != RoutineInfoType.Function)
      {
        IToken tk = routine.TokenStream.Get(beginEnd.TokenStopIndex);
        EmitInstrumentationCode(sql, routine, tk.Line, tk.CharPositionInLine);
      }
      sql.AppendLine("#end scope");
      if (routine.Type != RoutineInfoType.Function)
      {
        // Generate code to cleanup scope.
        EmitEndScopeCode(sql);
      }
      sql.AppendLine("end;");
    }

    private void EmitInstrumentationCode(StringBuilder sql, RoutineInfo routine, int lineno, int colno)
    {
      sql.AppendFormat("if {0} = 0 then ", VAR_DBG_NO_DEBUGGING);
      sql.AppendLine();
      sql.AppendFormat(routine.PreInstrumentationCode, VAR_DBG_SCOPE_LEVEL, lineno, routine.FullName, DebugSessionId, colno);
      sql.AppendLine(string.Format("call `serversidedebugger`.`ExitEnterCriticalSection`( '{0}', {1} );", routine.Name, lineno ));
      sql.AppendFormat(routine.PostInstrumentationCode, VAR_DBG_SCOPE_LEVEL, DebugSessionId);
      sql.AppendLine(" end if;");
    }

    private void CleanupScopeAll()
    {
      ExecuteRaw(string.Format("delete from `serversidedebugger`.`DebugScope` where DebugSessionId = {0}", DebugSessionId));
      ExecuteRaw(string.Format("delete from `serversidedebugger`.`DebugCallStack` where DebugSessionId = {0}", DebugSessionId));
    }
    
    /// <summary>
    /// Concats a set of tokens from start to finish token indexes. Also Wraps an expression of the 
    /// form "a" to "( `ExitEnterCriticalSectionFunction`() XX ( a ) )".
    /// Effectively adding a breakpoint just before the evaluation of a boolean condition.
    /// XX can be AND (if validResult is true) or OR (if validResult is false), this is important
    /// if the expression is for a WHILE or a REPEAT-UNTIL.
    /// `ExitEnterCriticalSectionFunction`() returns 0 or 1 so it doesn't affect short circuit evaluation in the server.
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="cts"></param>
    /// <param name="StartTokenIndex"></param>
    /// <param name="EndTokenIndex"></param>
    /// <param name="ShortCircuitResult">If true joins the expressions with AND, otherwise joins them with OR.</param>
    private void ConcatTokens(StringBuilder sb, CommonTokenStream cts, int StartTokenIndex, int EndTokenIndex, bool ShortCircuitResult )
    {
      ConcatTokens(sb, cts, StartTokenIndex, EndTokenIndex);
      // TODO: disabled for now, because is a half-baked solution (ie. how to edit locals values after the breakpoint?)
      //sb.AppendFormat("( `serversidedebugger`.`ExitEnterCriticalSectionFunction`( {0} ) ", ShortCircuitResult ? "1" : "0" );
      //if (ShortCircuitResult)
      //  sb.Append(" and ( ");
      //else
      //  sb.Append(" or ( ");
      //foreach (IToken tok in cts.GetTokens( StartTokenIndex, EndTokenIndex ))
      //{
      //  sb.Append(tok.Text);
      //}
      //sb.Append(" ) ) ");
    }

    /// <summary>
    /// Concats a set of tokens from start to finish token indexes.
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="cts"></param>
    /// <param name="StartTokenIndex"></param>
    /// <param name="EndTokenIndex"></param>
    private void ConcatTokens(StringBuilder sb, CommonTokenStream cts, int StartTokenIndex, int EndTokenIndex)
    {
      for (int i = StartTokenIndex; i <= EndTokenIndex; i++)
      {
        IToken tok = cts.Get(i);
        if (((i + 1) <= EndTokenIndex) && (cts.Get(i + 1).Type == MySQL51Parser.LPAREN) &&
          ((i + 2) <= EndTokenIndex) && (cts.Get(i + 2).Type == MySQL51Parser.RPAREN))
        {
          if (Cmp(tok.Text, "last_insert_id") == 0)
          {
            sb.Append(VAR_DBG_LAST_INSERT_ID);
            i += 2;
            continue;
          }
          else if (Cmp(tok.Text, "row_count") == 0) 
          {
            sb.Append(VAR_DBG_ROW_COUNT);
            i += 2;
            continue;
          }
          else if (Cmp(tok.Text, "found_rows") == 0)
          {
            sb.Append(VAR_DBG_FOUND_ROWS);
            i += 2;
            continue;
          }
        }
        sb.Append(tok.Text);
      }
    }

    // Session variables
    private const string VAR_DBG_BINARY_BUF = "@dbg_binary_buf";
    private const string VAR_DBG_ROW_COUNT = "@dbg_row_count";
    private const string VAR_DBG_LAST_INSERT_ID = "@dbg_last_insert_id";
    private const string VAR_DBG_FOUND_ROWS = "@dbg_found_rows";
    //private const string VAR_DBG_SCOPE_LEVEL = "@dbg_ScopeLevel";
    private const string VAR_DBG_SCOPE_LEVEL = "(select `serversidedebugger`.`debugdata`.`Val` from `serversidedebugger`.`debugdata` where `serversidedebugger`.`debugdata`.`id` = 1 limit 1 )";
    private const string VAR_DBG_NO_DEBUGGING = "@dbg_NoDebugging";

    private void EmitBeginScopeCode( StringBuilder sql, RoutineInfo ri )
    {
      sql.AppendFormat("if {0} = 0 then ", VAR_DBG_NO_DEBUGGING);
      sql.AppendLine();
      sql.Append("update `serversidedebugger`.`DebugData` set `serversidedebugger`.`DebugData`.`Val` = `serversidedebugger`.`DebugData`.`Val` + 1 where `serversidedebugger`.`DebugData`.`Id` = 1;");
      sql.AppendLine();
      sql.AppendFormat("call `serversidedebugger`.`Push`( {0}, '{1}' );", DebugSessionId, ri.FullName );
      sql.AppendLine();
      sql.Append("end if;");
      sql.AppendLine();
    }

    private void EmitEndScopeCode(StringBuilder sql)
    {
      sql.AppendFormat("if {0} = 0 then ", VAR_DBG_NO_DEBUGGING);
      sql.AppendLine();
      sql.Append("call `serversidedebugger`.`CleanupScope`( 1 );");
      sql.AppendLine();
      sql.AppendFormat("call `serversidedebugger`.`Pop`( {0} );", DebugSessionId );
      sql.AppendLine();
      sql.Append("end if;");
      sql.AppendLine();
    }

    private string GetCurrentRoutine()
    {
      object o = ExecuteScalar( string.Format( 
        "select `serversidedebugger`.`Peek`( {0} );", DebugSessionId ) );
      if (o == DBNull.Value)
        return null;
      else
      {
        string name = (string)o;
        if( name.IndexOf( '.' ) == -1 )
          return string.Format( "{0}.{1}", _utilCon.Database, name );
        else
          return name;
      }
    }

    private void GenerateInstrumentedCodeRecursive(
      IList<ITree> children,
      RoutineInfo routine,
      StringBuilder sql)
    {
      IToken tkTmp;
      ITree trTmp;
      CommonTokenStream tokenStream = routine.TokenStream;
      string PreInstrumentationCode = routine.PreInstrumentationCode;
      string PostInstrumentationCode = routine.PostInstrumentationCode;
      // The following statements are qualified to have "statement lists"
      // begin_end, if, while, repeat, loop, declare condition, declare handler, 
      // case's when, then, else.
      if (children == null) return;
      foreach (ITree ic in children)
      {
        CommonTree tc = (CommonTree)ic;
        if( !routine._endOfDeclare && ( routine.BeginEnd.Children == children ))
        {
          if ((Cmp(tc.Text, "declare") != 0) && (Cmp(tc.Text, "declare_handler") != 0) )
          {
            routine._endOfDeclare = true;
            // Emit begin scope code
            EmitBeginScopeCode(sql, routine);
          }
        }
        switch (tc.Text.ToLowerInvariant())
        {
          case "begin_end":
            {
              if (Cmp(tc.GetChild(0).Text, "label") == 0)
              {
                sql.Append(tc.GetChild(0).GetChild(0).Text).Append(" : ");
              }
              sql.AppendLine("begin");
              GenerateInstrumentedCodeRecursive(tc.Children, routine, sql);
              tkTmp = tokenStream.GetTokens(tc.TokenStopIndex, tc.TokenStopIndex).Last();
              EmitInstrumentationCode(sql, routine, tkTmp.Line, tkTmp.CharPositionInLine);
              // TODO: Add END token to AST, so you can put breakpoints in the END
              //routine.RegisterStatement(tc);
              sql.AppendLine("end;");
            }
            break;
          case "if":
            {
              int i = 0, j = 0;
              int idxThen = -1;
              EmitInstrumentationCode(sql, routine, tc.Line, tc.CharPositionInLine);
              routine.RegisterStatement(tc);
              do
              {
                while (i < tc.ChildCount &&
                  ((Debugger.Cmp(tc.GetChild(i).Text, "if_cond") != 0) &&
                  (Debugger.Cmp(tc.GetChild(i).Text, "elseif") != 0)))
                  i++;
                if (i == tc.ChildCount) break;
                CommonTree child = (CommonTree)tc.GetChild(i);
                j = 0;
                while (j < tc.ChildCount && Debugger.Cmp(child.GetChild(j).Text, "then") != 0)
                  j++;
                idxThen = j;
                CommonTree thenTree = (CommonTree)child.GetChild(idxThen);
                CommonTree exprTree = (CommonTree)child.GetChild(idxThen - 1);
                // Concat "elseif/if ... then"
                if (Cmp(child.Text, "if_cond") == 0)
                  sql.Append("if ");
                else
                  sql.Append("elseif ");
                ConcatTokens(sql, tokenStream, exprTree.TokenStartIndex, exprTree.TokenStopIndex, true );
                sql.AppendLine(" then ");
                GenerateInstrumentedCodeRecursive(thenTree.Children, routine, sql);
                i++;
              } while (true);
              // look for else
              i = 0;
              while (i < tc.ChildCount && Debugger.Cmp(tc.GetChild(i).Text, "else") != 0)
                i++;
              if (i < tc.ChildCount)
              {
                CommonTree elseTree = (CommonTree)tc.GetChild(i);
                // concat "else"
                ConcatTokens(sql, tokenStream, elseTree.TokenStartIndex, elseTree.GetChild(0).TokenStartIndex - 1);
                GenerateInstrumentedCodeRecursive(elseTree.Children, routine, sql);
              }
              sql.AppendLine("end if;");
            }
            break;
          case "while":
            {
              EmitInstrumentationCode(sql, routine, tc.Line, tc.CharPositionInLine);
              bool hasLabel = false;
              if (Cmp(tc.GetChild(0).Text, "label") == 0)
              {
                sql.Append(tc.GetChild(0).GetChild(0).Text).Append(" : ");
                hasLabel = true;
              }
              // Concat "while ... do"
              sql.Append(" while ");
              ConcatTokens(sql, tokenStream, tc.GetChild( 0 ).TokenStartIndex, tc.GetChild(0).TokenStopIndex, false);
              sql.Append(" do ");
              if (!hasLabel)
              {
                GenerateInstrumentedCodeRecursive(
                  // skip while condition
                  tc.Children.Skip(1).ToList(), routine, sql);
              }
              else
              {
                GenerateInstrumentedCodeRecursive(
                  // skip while condition & label
                  tc.Children.Skip(2).ToList(), routine, sql);
              }
              sql.AppendLine("end while;");
            }
            break;
          case "repeat":
            {
              bool hasLabel = false;
              if (Cmp(tc.GetChild(0).Text, "label") == 0)
              {
                sql.Append(tc.GetChild(0).GetChild(0).Text).Append(" : ");
                hasLabel = true;
              }
              IList<ITree> childColl = tc.Children;
              sql.AppendLine("repeat");
              if (!hasLabel)
              {
                GenerateInstrumentedCodeRecursive(
                  // skip until & until-condition
                   childColl.Take(childColl.Count - 2).ToList(), routine, sql);
              }
              else
              {
                GenerateInstrumentedCodeRecursive(
                  // skip label, until & until-condition
                   childColl.Take(childColl.Count - 2).Skip(1).ToList(), routine, sql);
              }
              trTmp = tc.GetChild(tc.ChildCount - 1);
              EmitInstrumentationCode(sql, routine, trTmp.Line, trTmp.CharPositionInLine);
              sql.Append("until ");
              // Concat until condition
              ConcatTokens(sql, tokenStream, 
                childColl[childColl.Count - 1].TokenStartIndex, 
                childColl[childColl.Count - 1].TokenStopIndex, true );
              sql.AppendLine( " end repeat; " );
            }
            break;
          case "loop":
            {
              bool hasLabel = false;
              if (Cmp(tc.GetChild(0).Text, "label") == 0)
              {
                sql.Append(tc.GetChild(0).GetChild(0).Text).Append(" : ");
                hasLabel = true;
              }
              sql.AppendLine("loop");
              if (hasLabel)
              {
                // skip label children
                GenerateInstrumentedCodeRecursive(tc.Children.Skip(1).ToList(), routine, sql);
              }
              else
              {
                GenerateInstrumentedCodeRecursive(tc.Children, routine, sql);
              }
              sql.AppendLine("end loop;");
            }
            break;
          case "declare_handler":
            {
              CommonTree beginEnd = (CommonTree)tc.GetChild(2);
              // Concat declare handler ... (until before begin)
              ConcatTokens(sql, tokenStream, tc.TokenStartIndex, beginEnd.TokenStartIndex - 1);
              if (Cmp("begin_end", beginEnd.Text) != 0)
              {
                // if there is no begin/end block, need to rewrite code so it appears to be one.
                List<ITree> nodes = new List<ITree>();
                nodes.Add(beginEnd);
                sql.AppendLine(" begin ");
                GenerateInstrumentedCodeRecursive(nodes, routine, sql);
                sql.AppendLine(" end; ");
              }
              else
              {
                GenerateInstrumentedCodeRecursive( tc.Children.Skip( 2 ).ToList(), routine, sql );
              }
            }
            break;
          case "case_stmt":
            {
              int i = 0;
              routine.RegisterStatement(tc);
              EmitInstrumentationCode(sql, routine, tc.Line, tc.CharPositionInLine);
              sql.AppendLine("case");
              CommonTree whenCt = (CommonTree)tc.GetChild(0);
              if (Cmp(whenCt.Text, "when") != 0)
              {
                CommonTree firstCt = whenCt;
                ConcatTokens(sql, tokenStream, firstCt.TokenStartIndex, firstCt.TokenStopIndex );
                whenCt = (CommonTree)tc.GetChild(1);
              }
              while (Cmp(whenCt.Text, "when") == 0)
              {
                CommonTree whenExpr = ( CommonTree )whenCt.GetChild( 0 );
                sql.Append(" when ");
                ConcatTokens(sql, tokenStream, whenExpr.TokenStartIndex, whenExpr.TokenStopIndex, false);
                sql.Append(" then ");
                GenerateInstrumentedCodeRecursive( whenCt.Children.Skip( 1 ).ToList(), routine, sql );
                if (++i >= tc.ChildCount) break;
                whenCt = (CommonTree)tc.GetChild(i);
              }
              if (i < tc.ChildCount)
              {
                CommonTree elseCt = (CommonTree)tc.GetChild(tc.ChildCount - 1);
                sql.Append(" else");
                GenerateInstrumentedCodeRecursive( elseCt.Children, routine, sql);
              }
              sql.AppendLine("end case;");
            }
            break;
          case "call":
            {
              routine.RegisterStatement(tc);
              EmitInstrumentationCode(sql, routine, tc.Line, tc.CharPositionInLine);
              ConcatTokens(sql, tokenStream, tc.TokenStartIndex, tc.TokenStopIndex);
              // Workaround: sometimes last token of declare statement is not the expected semicolon, if so, add it.
              if (Cmp(tokenStream.Get(tc.TokenStopIndex).Text, ";") != 0)
              {
                sql.Append(';');
              }
              // end workaround
            }
            break;
          case "label":
            // nothing
            break;
          case "leave":
            {
              routine.RegisterStatement(tc);
              string label = tc.GetChild(0).Text;
              EmitInstrumentationCode(sql, routine, tc.Line, tc.CharPositionInLine);
              if (routine._leaveLabel == label)
              {
                EmitEndScopeCode(sql);
              }
              ConcatTokens(sql, tokenStream, tc.TokenStartIndex, tc.TokenStopIndex);
            }
            break;
          case "return":
            {
              routine.RegisterStatement(tc);
              EmitInstrumentationCode(sql, routine, tc.Line, tc.CharPositionInLine);
              EmitEndScopeCode(sql);
              ConcatTokens(sql, tokenStream, tc.TokenStartIndex, tc.TokenStopIndex);
              // Workaround: sometimes last token of declare statement is not the expected semicolon, if so, add it.
              if (Cmp(tokenStream.Get(tc.TokenStopIndex).Text, ";") != 0)
              {
                sql.Append(';');
              }
              // end workaround
              sql.AppendLine();
            }
            break;
          default:
            // not compound code, skip over local variables.
            if (Cmp(tc.Text, "declare") != 0)
            {
              routine.RegisterStatement(tc);
              EmitInstrumentationCode(sql, routine, tc.Line, tc.CharPositionInLine);
              ConcatTokens(sql, tokenStream, tc.TokenStartIndex, tc.TokenStopIndex);
            }
            else
            {
              ConcatTokens(sql, tokenStream, tc.TokenStartIndex, tc.TokenStopIndex);
            }
            // Workaround: sometimes last token of declare statement is not the expected semicolon, if so, add it.
            if (Cmp(tokenStream.Get(tc.TokenStopIndex).Text, ";") != 0)
            {
              sql.Append(';');
            }
            // end workaround
            sql.AppendLine();
            break;
        }
      }
    }

    private StoreType GetStoreType(MySqlDataReader r, string prefix)
    {
      StoreType st;
      st = new StoreType();
      st.Name = string.Format( "{0}.{1}",prefix, r.GetString(0) );
      st.Type = r.GetString(1);
      if (StoreType.IsString(st.Type))
        st.Length = r.GetInt32(4);
      else if ( StoreType.IsNumeric( st.Type ) )
        st.Length = r.GetInt32(2);
      st.Values = null;   // TODO: For now, no supporting enum types
      st.Precision = StoreType.IsNumeric(st.Type) ? r.GetInt32(3) : 0;
      st.Unsigned = false;
      st.Value = DBNull.Value;
      return st;
    }

    private void RegisterNewOldVars(RoutineInfo ri, Dictionary<string, StoreType> vars)
    {
      string sql = string.Format(
@"select column_name, data_type, numeric_precision, numeric_scale, character_maximum_length, column_type 
  from information_schema.columns where table_name = '{0}' and table_schema = '{1}'",
ri.TriggerInfo.Table, ri.TriggerInfo.ObjectSchema);
      MySqlCommand cmd = new MySqlCommand(sql, _utilCon);
      MySqlDataReader r = cmd.ExecuteReader();
      try
      {
        while (r.Read())
        {
          StoreType st;
          if( ( ri.TriggerInfo.Event == TriggerEvent.Insert ) ||
            ( ri.TriggerInfo.Event == TriggerEvent.Update ))
          {
            st = GetStoreType(r, "new");
            vars.Add(st.Name, st);
          }
          if ((ri.TriggerInfo.Event == TriggerEvent.Delete) ||
            (ri.TriggerInfo.Event == TriggerEvent.Update))
          {
            st = GetStoreType(r, "old");
            vars.Add(st.Name, st);
          }
        }
      }
      finally
      {
        r.Close();
      }
    }

    /// <summary>
    /// Registers in the current scope all the session variables, so they can be properly emulated.
    /// </summary>
    /// <param name="cts"></param>
    /// <param name="vars"></param>
    private void ParseSessions(CommonTokenStream cts, Dictionary<string, StoreType> vars)
    {
      for( int i = 0; i < cts.Count; i++ )
      {
        IToken tok = cts.Get(i);
        if ((tok.Type == MySQL51Parser.AT1) && ((i + 1) < cts.Count) &&
          (cts.Get(i + 1).Type == MySQL51Parser.ID))
        {
          if ((i > 0) && (cts.Get(i - 1).Type == MySQL51Parser.ID)) continue;
          string id = string.Format( "@{0}", cts.Get( i + 1 ).Text );
          StoreType st;
          if( !vars.TryGetValue( id, out st ) )
          {
            st = new StoreType() { Name = id, Type = "varchar", Value = DBNull.Value, VarKind = VarKindEnum.Session };
            vars.Add(id, st );
          }
        }
      }
    }

    /// <summary>
    /// Parses begin/end block gathering info in all declared local variables.
    /// TODO: how to take care of session & globals? same as in emulation: by providing a watches facility.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="vars"></param>
    private void ParseDeclares(CommonTree node, Dictionary<string, StoreType> vars)
    {
      if (( Cmp(node.Text, "declare") == 0 ) && 
          ( Cmp(node.GetChild(0).Text, "condition") != 0 ) &&
          ( Cmp( node.GetChild( 0 ).Text, "cursor") != 0 ))
      {
        // Register vars...
        StoreType[] stVars = ParseDeclare(node);
        StoreType st2;
        foreach (StoreType st in stVars)
        {
          // variables with same name, are hidden by already declared ones.
          if (!vars.TryGetValue(st.Name, out st2))
          {
            vars.Add(st.Name, st);
          }
        }        
      }
      else
      {
        if (node.Children == null) return;
        foreach (ITree ct in node.Children)
        {
          ParseDeclares((CommonTree)ct, vars);
        }
      }
    }

    /// <summary>
    /// Returns a description of the variable (or variables) declared within a single DECLARE statement.
    /// </summary>
    /// <param name="Tree"></param>
    /// <returns>Array with one or more variables.</returns>
    internal StoreType[] ParseDeclare( CommonTree Tree )
    {
      StoreType st = new StoreType();
      StoreType[] stVars = null;
      int idx = 0, i, idxDataType;
      while( ( idx < Tree.ChildCount ) && ( Cmp( Tree.GetChild( idx ).Text, "data_type") != 0) )
        idx++;
      if (idx >= Tree.ChildCount)
        throw new InvalidDataException("Argument node");
      stVars = new StoreType[ idx ];
      idxDataType = idx;
      ParseDataType(st, (CommonTree)Tree.GetChild(idx));
      while ((idx < Tree.ChildCount) && (Cmp(Tree.GetChild(idx).Text, "default") != 0))
        idx++;
      if (idx < Tree.ChildCount)
      {
        st.Value = Tree.GetChild(idx).GetChild(0).Text;
      }
      else
      {
        st.Value = DBNull.Value;
      }
      i = 0;
      while ((i < Tree.ChildCount) && (i < idxDataType))
      {
        stVars[ i ] = new StoreType();
        stVars[ i ].Name = Tree.GetChild(i).Text;
        stVars[ i ].Type = st.Type;
        stVars[ i ].Length = st.Length;
        stVars[ i ].Values = st.Values;
        stVars[ i ].Precision = st.Precision;
        stVars[ i ].Unsigned = st.Unsigned;
        stVars[ i ].Value = st.Value;
        i++;
      }
      return stVars;
    }

    // Cache of instrumented code.
    private Dictionary<string, RoutineInfo> _cacheInstrumented = new Dictionary<string, RoutineInfo>();

    /// <summary>
    /// For a given statement, instruments all triggers & functions that will be invoked
    /// at its execution.
    /// </summary>
    /// <param name="stmt"></param>
    private void PreinstrumentStatement(CommonTree stmt)
    {
      // Identify all current dependencies and instrument them:
      if ( (Cmp( stmt.Text, "begin_end" ) == 0) ||
        ( Cmp( stmt.Text, "loop" ) == 0 ))
      {
        // Nothing to preinstrument, body has already being instrumented.
        return;
      }
      if (Cmp(stmt.Text, "while") == 0)
      {
        stmt = ( CommonTree )stmt.GetChild(0);
      }
      else if (Cmp(stmt.Text, "repeat") == 0)
      {
        stmt = (CommonTree)stmt.GetChild(stmt.ChildCount - 1);
      }
      // TODO: if, etc.
      else if (Cmp(stmt.Text, "call") == 0)
      {
        // Get dependencies from args (like functions)
        RoutineInfo ri = GetRoutineInfo(((CommonTree)stmt).GetChild(0).Text);
        if (string.IsNullOrEmpty(ri.InstrumentedSourceCode))
          InstrumentRoutine(ri);
      }
      DoPreinstrumentStatement(stmt);
    }

    private void DoPreinstrumentStatement(CommonTree stmt)
    {
#if PREINSTRUMENT_FUNCTIONS_AND_TRIGGERS
      List<RoutineInfo> _routines = GetDependenciesToInstrument(stmt);
#else
      // TODO: Not generating any dependency instrumentations for now.
      List<RoutineInfo> _routines = new List<RoutineInfo>();
#endif
      // Eliminate already repeated ones:
      List<RoutineInfo> routines = new List<RoutineInfo>();
      foreach (RoutineInfo r in _routines)
      {
        RoutineInfo ri;
        string routineKey = r.GetFullName(_utilCon.Database);
        if (!_preinstrumentedRoutines.TryGetValue(routineKey, out ri))
        {
          routines.Add(r);
          _preinstrumentedRoutines.Add(routineKey, r);
        }
      }
      // Instrument dependencies
      foreach (RoutineInfo ri in routines)
      {
        RoutineInfo rip = null;
        string routineKey = ri.GetFullName(_utilCon.Database);
        if (!_cacheInstrumented.TryGetValue(routineKey, out rip))
        {
          Debug.WriteLine(string.Format("Debugger: Instrumenting {0}", routineKey ));
          InstrumentRoutine(ri);
          _cacheInstrumented.Add(routineKey, ri);
        }
      }
    }

    private Dictionary<string, RoutineInfo> _preinstrumentedRoutines = 
      new Dictionary<string, RoutineInfo>();

    /// <summary>
    /// For a given statement, returns all the triggers and functions that will 
    /// be invoked at its execution.
    /// </summary>
    /// <param name="stmt"></param>
    /// <returns></returns>
    private List<RoutineInfo> GetDependenciesToInstrument(CommonTree stmt)
    {
      List<RoutineInfo> routines = new List<RoutineInfo>();
      /* For triggers need to get tables affected, but only for the following statements:
       * insert, load data, replace, update, delete, 
       * */
      List<MetaTrigger> lt = GetAllTriggersFromStmt(stmt);
      foreach (MetaTrigger tr in lt)
      {
        routines.Add( new RoutineInfo() {
          Schema = tr.TriggerSchema,
          Name = tr.Name,
          SourceCode = GetCreateTriggerFor( tr.TriggerSchema, tr.Name ),
          Type = RoutineInfoType.Trigger,
          TriggerInfo = tr
        });
      }
      /* For functions need to parse all expressions.
       * */
      List<MetaRoutine> lr = GetFunctions(stmt);
      foreach (MetaRoutine mr in lr)
      {
        // skip built-in functions
        if (!string.IsNullOrEmpty(mr.RoutineDefinition))
        {
          routines.Add(new RoutineInfo()
          {
            Schema = mr.Schema,
            Name = mr.Name,
            SourceCode = mr.RoutineDefinition,
            Type = (mr.Type == RoutineType.Function) ?
              RoutineInfoType.Function : RoutineInfoType.Procedure
          });
        }
      }
      return routines;
    }

    //// cache of metadata for routines & triggers
    //private List<MetaRoutine> routines = new List<MetaRoutine>();
    //private List<MetaTrigger> triggers = new List<MetaTrigger>();

    /// <summary>
    /// Given an statement's AST gets all the triggers associated with it.
    /// </summary>
    /// <param name="t"></param>
    /// <returns>a list of triggers</returns>
    /// <remarks>Triggers are only searched for some DMLs (insert, delete & update).
    /// TODO: Add replace & load statements.</remarks>
    private List<MetaTrigger> GetAllTriggersFromStmt(CommonTree t)
    {
      List<MetaTrigger> triggers = new List<MetaTrigger>();
      // Validate if stmt is insert, load data, replace, update, delete then return.
      if( 
        ( Cmp( t.Text, "insert" ) == 0 ) ||
        ( Cmp( t.Text, "delete" ) == 0 ) ||
        ( Cmp( t.Text, "update" ) == 0 ) ||
        ( Cmp(t.Text, "replace" ) == 0 ) //||
        // "load data" is not available in stored procedures, see http://bugs.mysql.com/bug.php?id=14977
        //( ( Cmp( t.Text, "load" ) == 0 ) && ( Cmp( t.GetChild( 0 ).Text, "data" ) == 0 ) )
        )
      {
        GetAllTriggers(t, triggers);
      }
      return triggers;
    }

    /// <summary>
    /// Gets all triggers associated with all the tables of the current statement.
    /// TODO: Not *all* the tables are actually necessary, ie for an
    /// "insert into ... select ..." no need to get the tables from select's from clause,
    /// this to be filter out later in this method.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="triggers"></param>
    private void GetAllTriggers(CommonTree t, List<MetaTrigger> triggers )
    {
      foreach (ITree ct in t.Children)
      {
        if (Cmp(ct.Text, "table_ref") == 0)
        {
          TableWithAlias twa = ParserUtils.ExtractTableName(ct);
          List<MetaTrigger> lt = GetTriggersFrom(twa.Database, twa.TableName);
          // TODO: replace this for a comparison vs table only.
          foreach (MetaTrigger mt in lt)
          {
            if (!triggers.Exists((tr) => tr.Equals( mt ) ))
            {
              triggers.Add(mt);
            }
          }
        }
        else
        {
          if (((CommonTree)ct).Children != null)
          {
            GetAllTriggers((CommonTree)ct, triggers);
          }
        }
      }
    }

    /// <summary>
    /// Returns list of triggers associated with Table.
    /// To cache or not to cache?
    /// </summary>
    private List<MetaTrigger> GetTriggersFrom( string Schema, string Table )
    {
      List<MetaTrigger> triggers = new List<MetaTrigger>();
      if (string.IsNullOrEmpty(Schema)) Schema = _utilCon.Database;
      MySqlCommand cmd = new MySqlCommand( string.Format( 
        @"select trigger_schema, trigger_name, event_manipulation, event_object_schema, event_object_table, 
          action_statement, action_timing from information_schema.triggers 
          where ( event_object_schema = '{0}' ) and ( event_object_table = '{1}' )", Schema, Table ), _utilCon );
      MySqlDataReader r = cmd.ExecuteReader();
      try
      {
        while (r.Read())
        {
          MetaTrigger mt = new MetaTrigger() {
            TriggerSchema = r.GetString( 0 ),
            Name = r.GetString( 1 ),
            Event = ( TriggerEvent )Enum.Parse( typeof( TriggerEvent ), r.GetString( 2 ), true ),
            ObjectSchema = r.GetString( 3 ),
            Table = r.GetString( 4 ),
            Source = r.GetString( 5 ),
            ActionTiming = ( TriggerActionTiming )Enum.Parse( typeof( TriggerActionTiming ), 
              r.GetString( 6 ), true )
          };
          triggers.Add(mt);
        }
      }
      finally
      {
        r.Close();
      }
      return triggers;
    }

    /// <summary>
    /// Returns list of functions associated with an statement.
    /// </summary>
    /// <param name="Schema"></param>
    /// <param name="Table"></param>
    /// <returns></returns>
    private List<MetaRoutine> GetFunctions(CommonTree stmt)
    {
      Dictionary<string, MetaRoutine> routines = new Dictionary<string, MetaRoutine>();
      GetFunctionsRecursive(stmt, routines );
      
      if( routines.Count != 0 )
      {
        foreach (MetaRoutine mr in routines.Values)
        {
          string sql;
          int cnt = Convert.ToInt32( ExecuteScalar(string.Format(
            "select count( * ) from information_schema.routines where routine_name like '{0}' and routine_schema like '{1}'", mr.Name, 
            string.IsNullOrEmpty( mr.Schema )? _utilCon.Database : mr.Schema )) );
          if (cnt == 0) continue;
          if (string.IsNullOrEmpty(mr.Schema))
            sql = string.Format("show create function `{1}`", mr.Schema, mr.Name);
          else
            sql = string.Format("show create function `{0}`.`{1}`", mr.Schema, mr.Name);
          mr.RoutineDefinition = GetFieldAsString(sql, 2);
          MetaRoutine mr2 = null;
          if (!routines.TryGetValue(mr.Name, out mr2))
          {
            routines.Add(mr.Name, mr);
          }
          else
          {
            mr2.RoutineDefinition = mr.RoutineDefinition;
          }
        }
      }
      return routines.Values.ToList();
    }

    private void GetFunctionsRecursive(CommonTree stmt, Dictionary<string, MetaRoutine> routines)
    {
      if (stmt.Children == null) return;
      foreach (ITree t in stmt.Children)
      {
        if (Cmp(t.Text, "func") == 0)
        {
          // TODO: where to put the schema?
          string funcName = t.GetChild(0).Text;
          MetaRoutine mr;
          if (!routines.TryGetValue(funcName, out mr))
          {
            routines.Add( funcName, new MetaRoutine() { 
              Name = funcName, Type = RoutineType.Function });
          }
        }
        else
        {
          GetFunctionsRecursive((CommonTree)t, routines);
        }
      }
    }

    /// <summary>
    /// Parses a comma separated list of arguments.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public Dictionary<string, StoreType> ParseArgs(string sql)
    {
      StringBuilder sb = new StringBuilder();
      CommonTokenStream tokenStream;
      MySQL51Parser.program_return treeParsed =
        ParseSql(sql, false, out sb, out tokenStream);
      CommonTree tree = (CommonTree)treeParsed.Tree;
      if (tree.IsNil)
      {
        tree = (CommonTree)tree.GetChild(0);
      }
      Dictionary<string, StoreType> args = ParseArgs(tree);
      return args;
    }

    /// <summary>
    /// Parses a comma separated list of arguments.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    internal Dictionary<string, StoreType> ParseArgs(CommonTree node)
    {
      Dictionary<string, StoreType> args = new Dictionary<string, StoreType>();
      int i = 0;
      while (i < node.ChildCount && Cmp(node.GetChild(i).Text, "param") != 0)
        i++;
      while (i < node.ChildCount)
      {
        if (Cmp(node.GetChild(i).Text, "param") != 0) break;
        KeyValuePair<string, StoreType> arg = ParseArg((CommonTree)node.GetChild(i));
        args.Add(arg.Key, arg.Value);
        i++;
      }
      return args;
    }

    internal KeyValuePair<string, StoreType> ParseArg(CommonTree node)
    {
      StoreType st = new StoreType();
      int i = 0;
      st.Name = node.GetChild(i).Text;
      st.VarKind = VarKindEnum.Argument;
      i++;
      if (Cmp(node.GetChild(i).Text, "data_type") == 0)
      {
        ParseDataType(st, (CommonTree)node.GetChild(i));
        i++;
      }
      if (i < node.ChildCount)
      {
        string s = node.GetChild(i).Text;
        if ((Cmp("in", s) == 0) || (Cmp("out", s) == 0) || (Cmp("inout", s) == 0))
        {
          st.ArgType = (ArgTypeEnum)Enum.Parse(typeof(ArgTypeEnum), s, true);
        }
      }
      return new KeyValuePair<string, StoreType>(st.Name, st);
    }

    /// <summary>
    /// Parses a data type and update the StoreType with the info gathered.
    /// </summary>
    /// <param name="st"></param>
    /// <param name="node"></param>
    internal void ParseDataType(StoreType st, CommonTree node)
    {
      if (Cmp(node.Text, "data_type") != 0)
      {
        throw new InvalidDataException("Argument node");
      }
      bool isEnum = false;
      CommonTree dtNode = node;
      for (int i = 0; i < dtNode.ChildCount; i++)
      {
        switch (i)
        {
          case 0:
            {
              st.Type = dtNode.GetChild(i).Text;
              if (Debugger.Cmp(st.Type, "set") == 0 || Debugger.Cmp(st.Type, "enum") == 0)
                isEnum = true;
            }
            break;
          case 1:
            {
              int v;
              if (Int32.TryParse(dtNode.GetChild(i).Text, out v))
              {
                st.Length = v;
              }
              else if (Debugger.Cmp(dtNode.GetChild(i).Text, "(") == 0)
              {
                if (isEnum)
                {
                  CommonTree parent = (CommonTree)dtNode.GetChild(i);
                  st.Values = new List<string>();
                  for (int j = 0; j < parent.ChildCount; j++)
                  {
                    st.Values.Add(parent.GetChild(j).Text);
                  }
                }
                else
                {
                  st.Length = Int32.Parse(dtNode.GetChild(i).GetChild(0).Text);
                  st.Precision = Int32.Parse(dtNode.GetChild(i).GetChild(1).Text);
                }
              }
              if (Debugger.Cmp(dtNode.GetChild(i).Text, "unsigned") == 0)
                st.Unsigned = true;
            }
            break;
          default:
            // unsigned & zerofill are not tested, since they are the same and unsigned flag is true by default.
            if (Cmp(dtNode.GetChild(i).Text, "signed") == 0)
            {
              st.Unsigned = false;
            }
            // For now, just ignore collate & character
            // ...
            break;
        }
      }
    }

    /// <summary>
    /// Returns true if was able to evaluate an expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    public bool TryParseExpression(string expression, out string error)
    {
      CommonTokenStream cts;
      StringBuilder sb = new StringBuilder();
      MySQL51Parser.program_return pr = this.ParseSql(
        string.Format("select {0}", expression), false, out sb, out cts);
      error = sb.ToString();
      return error.Length == 0;
    }

    public object Eval(string expression)
    {
      return Eval(expression, CurrentScope);
    }

    public object Eval(string expression, RoutineScope rs)
    {
      CommonTokenStream cts;
      StringBuilder sb = new StringBuilder();
      MySQL51Parser.program_return pr = this.ParseSql(
        string.Format("select {0}", expression), false, out sb, out cts);
      return Eval((CommonTree)((CommonTree)pr.Tree).GetChild(0), cts, rs);
    }

    private object Eval(CommonTree t, CommonTokenStream cts, RoutineScope rs)
    {
      string expr = ReplaceVarsWithValues(t, cts, rs);
      return ExecuteScalar(expr);
    }

    /// <summary>
    /// Replaces in the current statement, any references to local variables/arguments 
    /// with their value.
    /// </summary>
    /// <param name="t"></param>
    /// <returns>A string with the expression after replacing the values.</returns>
    internal string ReplaceVarsWithValues(CommonTree t, CommonTokenStream cts, RoutineScope rs )
    {
      Dictionary<string, StoreType> vars = rs.Variables;
      StringBuilder sb = new StringBuilder();

      //foreach (IToken tok in cts.GetTokens(t.TokenStartIndex, t.TokenStopIndex))
      for (int i = t.TokenStartIndex; i <= t.TokenStopIndex; i++)
      {
        IToken tok = cts.Get(i);
        IToken tok2 = null;
        StoreType st = null;
        if ((tok.Type == MySQL51Parser.AT1) && ((i + 1) <= t.TokenStopIndex) &&
           ((tok2 = cts.Get(i + 1)).Type == MySQL51Parser.ID))
        {
          string id = string.Format("@{0}", tok2.Text );
          // TODO: What about qualified names line a.b? There's no way to add variables like that (until we support triggers and new.col).
          if (vars.TryGetValue(id, out st))
          {
            i++;
            sb.Append(StoreType.WrapValue(st.Value));
          }
        }
        else if (((tok.Type == MySQL51Parser.NEW) || ( Cmp( tok.Text, "old" ) == 0 )) &&
          ((i + 1) <= t.TokenStopIndex) &&
          ( cts.Get( i + 1 ).Type == MySQL51Parser.DOT ) && ( ( i + 2 ) <= t.TokenStopIndex ) &&
          ( ( tok2 = cts.Get( i + 2 ) ).Type == MySQL51Parser.ID ))
        {
          string id = string.Format("{0}.{1}", tok.Text, tok2.Text );
          if (vars.TryGetValue(id, out st))
          {
            i += 2;
            sb.Append(StoreType.WrapValue(st.Value));
          }
          else
          {
            sb.Append(tok.Text);
          }
        }
        else if (vars.TryGetValue(tok.Text, out st))
        {
          sb.Append(StoreType.WrapValue(st.Value));
        }
        else
        {
          sb.Append(tok.Text);
        }
      }
      return sb.ToString();
    }

    internal static int Cmp(string s1, string s2)
    {
      return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
    }

    public MySQL51Parser.program_return ParseSql(string sql)
    {
      StringBuilder sb = new StringBuilder();
      CommonTokenStream cts;
      return ParseSql(sql, false, out sb, out cts);
    }

    public MySQL51Parser.program_return ParseSql(string sql, bool expectErrors, out StringBuilder sb, out CommonTokenStream cts)
    {
      Version ver = ParserUtils.GetVersion( _connection.ServerVersion );
      // The grammar supports upper case only
      MemoryStream ms = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(sql));
      CaseInsensitiveInputStream input = new CaseInsensitiveInputStream(ms);
      MySQLLexer lexer = new MySQLLexer(input);
      lexer.MySqlVersion = ver;
      CommonTokenStream tokens = new CommonTokenStream(lexer);
      MySQLParser parser = new MySQLParser(tokens);
      parser.MySqlVersion = ver;
      sb = new StringBuilder();
      TextWriter tw = new StringWriter(sb);
      parser.TraceDestination = tw;
      MySQL51Parser.program_return r = new MySQL51Parser.program_return();
      r.Tree = null;
      try
      {
        r = parser.program();
      }
      catch (RewriteEmptyStreamException e)
      {
        sb.AppendLine();
        sb.Append(e.Message);
      }
      cts = tokens;
      if (!expectErrors && sb.Length != 0)
      {
        throw new DebugSyntaxException(sb.ToString());
      }
      return r;
    }
  }
}
