// Copyright (c) 2009 Sun Microsystems, Inc.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MySql.EMTrace
{
    public enum MySqlTraceEventType : int
    {
        ConnectionOpened = 1,
        ConnectionClosed,
        QueryOpened,
        ResultOpened,
        ResultClosed,
        QueryClosed,
        StatementPrepared,
        StatementExecuted,
        StatementClosed,
        NonQuery,
        UsageAdvisorWarning,
        Warning,
        Error,
        QueryNormalized
    }

    public enum UsageAdvisorWarningFlags
    {
        NoIndex = 1,
        BadIndex,
        SkippedRows,
        SkippedColumns,
        FieldConversion
    }

    internal class MySqlTraceQueryResult
    {
        public int RowsRead;
        public int RowsChanged;
        public int RowsSkipped;
        public int SizeInBytes;
        public long InsertedId;
        public List<string> FieldsNotAccessed = new List<string>();
        public bool NoIndex;
        public bool BadIndex;
    }

    internal class MySqlTraceQuery
    {
        public string FullCommandText;
        public string CommandText;
        public string CommandTextHash;
        public string CallingHost;
        public string ConnectionString;
        public DateTime TimeOfQuery;
        public string CallStack;
        public long StartTimestamp;
        public long StopTimestamp;
        public int TotalRows;
        public long TotalBytes;
        public long ExecutionTime;
        public int ConnectionId;
        public string QueryType;
        public int Warnings;
        public int Errors;
        public MySqlTraceQuery Parent;
        public MySqlTraceQueryResult ActiveResult;
        public List<MySqlTraceQueryResult> Results = new List<MySqlTraceQueryResult>();
        public bool ShouldExplain;
    }

}
