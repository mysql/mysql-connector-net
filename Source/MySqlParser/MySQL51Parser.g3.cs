// Copyright © 2012, Oracle and/or its affiliates. All rights reserved.
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

//#define CSharp3Target

using System;
using Antlr.Runtime;
using System.Collections.Generic;

namespace MySql.Parser
{

  

#if CSharp3Target
  partial class MySQL51Parser
#else
  public partial class MySQL51Parser
  {
  }

  public class MySQLParserBase : Antlr.Runtime.Parser
    //: MySQL51Parser
#endif
	{
    public MySQLParserBase( ITokenStream input, RecognizerSharedState state ) : base( input, state )
    {
      // default value
      mysqlVersion = 5.1;
    }

    // holds values like 5.0, 5.1, 5.5, 5.6, etc.
    protected double mysqlVersion;

    public Version MySqlVersion {
      get { return new Version((int)mysqlVersion, (int)(mysqlVersion * 10 - (int)mysqlVersion * 10)); }
      set { mysqlVersion = (double)value.Major + (double)value.Minor / 10; }
    }

    protected int simple_table_ref_no_alias_existing_cnt;

    protected Stack<string> Scope = new Stack<string>();
    protected static Dictionary<string, string> errKeywords = new Dictionary<string, string>();
    //private Stack<int> cntTablesUpdate = new Stack<int>();
    protected int cntUpdateTables = 0;
    protected bool multiTableDelete = false;

    static MySQLParserBase()
    {
      errKeywords.Add("table_factor", "");
      errKeywords.Add("simple_table_ref_no_alias_existing", "");
      errKeywords.Add("column_name", "");
      errKeywords.Add("proc_name", "");
    }
	}

  public class MySQLParser : MySQL51Parser
  {
    public MySQLParser(ITokenStream input) : base( input )
    {

    }

#if CSharp3Target
      partial void EnterRule_declare_handler()
#else
    protected override void Enter_declare_handler()
#endif
    {
    }

#if CSharp3Target
      partial void EnterRule_begin_end_stmt()
#else
    protected override void Enter_begin_end_stmt()
#endif
    {

    }

#if CSharp3Target
      partial void EnterRule_drop_table()
#else
    protected override void Enter_drop_table()
#endif
    {
    }

#if CSharp3Target
      partial void EnterRule_simple_obj_ref_no_alias()
#else
    protected override void Enter_simple_obj_ref_no_alias()
#endif
    {
    }

#if CSharp3Target
      partial void EnterRule_update()
#else
    protected override void Enter_update()
#endif
    {
      cntUpdateTables = 0;
    }

#if CSharp3Target
      partial void EnterRule_statement_list()
#else
    protected override void Enter_statement_list()
#endif
    {
      Scope.Push("statement_list");
    }

#if CSharp3Target
      partial void LeaveRule_statement_list()
#else
    protected override void Leave_statement_list()
#endif
    {
      Scope.Pop();
    }

#if CSharp3Target
      partial void EnterRule_expr()
#else
    protected override void Enter_expr()
#endif
    {
      Scope.Push("expr");
    }

#if CSharp3Target
      partial void LeaveRule_expr()
#else
    protected override void Leave_expr()
#endif
    {
      Scope.Pop();
    }

#if CSharp3Target
      partial void EnterRule_field_name()
#else
    protected override void Enter_field_name()
#endif
    {
      Scope.Push("field_name");
    }

#if CSharp3Target
      partial void LeaveRule_field_name()
#else
    protected override void Leave_field_name()
#endif
    {
      Scope.Pop();
    }

#if CSharp3Target
      partial void EnterRule_simple_table_ref_no_alias_existing()
#else
    protected override void Enter_simple_table_ref_no_alias_existing()
#endif
    {
      simple_table_ref_no_alias_existing_cnt++;
    }

#if CSharp3Target
      partial void LeaveRule_simple_table_ref_no_alias_existing()
#else
    protected override void Leave_simple_table_ref_no_alias_existing()
#endif
    {
      simple_table_ref_no_alias_existing_cnt--;
    }

#if CSharp3Target
    partial void EnterRule_primary()
#else
    protected override void Enter_primary()
#endif
    {
      Scope.Push("expr");
    }

#if CSharp3Target
    partial void LeaveRule_primary()
#else
    protected override void Leave_primary()
#endif
    {
      Scope.Pop();
    }

    public override string GetErrorMessage(
        RecognitionException e,
        string[] tokenNames)
    {
      if (e is NoViableAltException)
      {
        NoViableAltException nvae = (e as NoViableAltException);
        if (errKeywords.ContainsKey(nvae.GrammarDecisionDescription))
        {
          /* Returns previous implementation format plus expected rule */
          return string.Format("no viable alternative at input {0}. Expected {1}.",
            this.GetTokenErrorDisplay(e.Token), (e as NoViableAltException).GrammarDecisionDescription);
        }
      }
      return base.GetErrorMessage(e, tokenNames);
    }
  }
}

