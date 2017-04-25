// Copyright (c) 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Globalization;

namespace MySql.EMTrace
{
    internal class SummaryReport : JsonReport
    {
        public string text_hash;
        public string text;
        public string database;
        public string query_type;
        public long bytes;
        public long min_bytes;
        public long max_bytes;
        public long exec_time;
        public long min_exec_time;
        public long max_exec_time;
        public int rows;
        public int min_rows;
        public int max_rows;
        public int count;
        public long errors;
        public long warnings;
        public long no_good_index_used;
        public long no_index_used;

        public SummaryReport(MySqlTraceQuery qi)
        {
            DbConnectionStringBuilder settings = ClassFactory.CreateConnectionStringBuilder();
            settings.ConnectionString = qi.ConnectionString;

            text = qi.CommandText;
            text_hash = qi.CommandTextHash;
            database = settings["database"] as string;
            query_type = qi.QueryType;
            bytes = min_bytes = max_bytes = qi.TotalBytes;
            exec_time = min_exec_time = max_exec_time = qi.ExecutionTime;
            rows = min_rows = max_rows = qi.TotalRows;
            errors = qi.Errors;
            warnings = qi.Warnings;
            count = 1;
            if (qi.Results != null)
            {
                foreach (MySqlTraceQueryResult rs in qi.Results)
                {
                    if (rs.BadIndex)
                        no_good_index_used++;
                    if (rs.NoIndex)
                        no_index_used++;
                }
            }
        }

        public void UpdateFromQuery(MySqlTraceQuery qi)
        {
            // update execution time
            exec_time += qi.ExecutionTime;
            if (qi.ExecutionTime < min_exec_time)
                min_exec_time = qi.ExecutionTime;
            else if (qi.ExecutionTime > max_exec_time)
                max_exec_time = qi.ExecutionTime;

            // update row count
            rows += qi.TotalRows;
            if (qi.TotalRows < min_rows)
                min_rows = qi.TotalRows;
            else if (qi.TotalRows > max_rows)
                max_rows = qi.TotalRows;

            // update byte count
            bytes += qi.TotalBytes;
            if (qi.TotalBytes < min_bytes)
                min_bytes = qi.TotalBytes;
            else if (qi.TotalBytes > max_bytes)
                max_bytes = qi.TotalBytes;

            errors += qi.Errors;
            warnings += qi.Warnings;
            if (qi.Results != null)
            {
                foreach (MySqlTraceQueryResult rs in qi.Results)
                {
                    if (rs.BadIndex)
                        no_good_index_used++;
                    if (rs.NoIndex)
                        no_index_used++;
                }
            }

            count++;
        }

        public void Post(ServerAggregationFactory factory, string UUID, string Parent)
        {
            StringBuilder url = new StringBuilder();
            name = String.Format("{0}.{1}.{2}", UUID, database, text_hash);
            parent = Parent;
            url.AppendFormat("{0}/v2/rest/instance/mysql/statementsummary/{1}",
                factory.EMHost, name);
            Upload(factory, url.ToString());
        }
    }

    internal class WorstCaseReport : JsonReport
    {
        public string host_to;
        public string user;
        public long exec_time;
        public int rows;
        public string text;
        public int connection_id;
        public string host_from;
        public string comment;
        public long bytes;
        public long errors;
        public long warnings;
        public long no_good_index_used;
        public long no_index_used;

        public WorstCaseReport(MySqlTraceQuery qi)
        {
            DbConnectionStringBuilder settings = ClassFactory.CreateConnectionStringBuilder();
            settings.ConnectionString = qi.ConnectionString;

            host_to = settings["server"] as string;
            user = settings["user id"] as string;
            exec_time = qi.ExecutionTime;
            rows = qi.TotalRows;
            text = qi.CommandText;
            connection_id = qi.ConnectionId;
            host_from = qi.CallingHost;
            comment = EscapeForJson(qi.CallStack);
            bytes = qi.TotalBytes;
            valueString = "values";
            errors = qi.Errors;
            warnings = qi.Warnings;

            if (qi.Results != null)
            {
                foreach (MySqlTraceQueryResult rs in qi.Results)
                {
                    if (rs.BadIndex)
                        no_good_index_used++;
                    if (rs.NoIndex)
                        no_index_used++;
                }
            }
        }

        public void Post(ServerAggregationFactory factory, string UUID, SummaryReport summary)
        {
            StringBuilder url = new StringBuilder();
            name = String.Format("{0}.{1}.{2}.{3}", UUID, summary.database, summary.text_hash, summary.text_hash);
            parent = String.Format("/instance/mysql/statementsummary/{0}", summary.Name);
            url.AppendFormat("{0}/v2/rest/instance/mysql/statement/{1}",
                factory.EMHost, name);
            Upload(factory, url.ToString());
        }
    }
}
