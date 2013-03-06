using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;

namespace MySql.Parser
{
	public partial class MySQLParser : Parser
	{
		partial void EnterRule(string ruleName, int ruleIndex)
		{
			return;
		}

		partial void LeaveRule(string ruleName, int ruleIndex)
		{
			return;
		}
	}
}
