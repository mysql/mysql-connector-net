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
    internal class ExplainReportManager
    {
        List<ExplainReport> reports = new List<ExplainReport>();

        public ExplainReportManager(MySqlTraceQuery qi)
        {
            // we don't want enterprise monitor to pick up this query so we turn off logging
            // on our connection string
            DbConnectionStringBuilder sb = ClassFactory.CreateConnectionStringBuilder();
            sb.ConnectionString = qi.ConnectionString;
            sb["logging"] = false;
            sb["use usage advisor"] = false;

            using (DbConnection c = ClassFactory.CreateConnection(sb.ConnectionString))
            {
                c.Open();
                try
                {
                    DbCommand cmd = ClassFactory.CreateCommand("EXPLAIN " + qi.FullCommandText, c);
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        int row = 1;
                        while (reader.Read())
                        {
                            ExplainReport report = new ExplainReport(row++);
                            report.id = reader.GetInt64(reader.GetOrdinal("id"));
                            report.select_type = reader.GetValue(reader.GetOrdinal("select_type")).ToString();
                            report.table = reader.GetValue(reader.GetOrdinal("table")).ToString();
                            report.type = reader.GetValue(reader.GetOrdinal("type")).ToString();
                            report.possible_keys = reader.GetValue(reader.GetOrdinal("possible_keys")).ToString();
                            report.key = reader.GetValue(reader.GetOrdinal("key")).ToString();
                            report.refValue = reader.GetValue(reader.GetOrdinal("ref")).ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("rows"))) report.rows = reader.GetInt64(reader.GetOrdinal("rows"));
                            report.Extra = reader.GetValue(reader.GetOrdinal("Extra")).ToString();
                            reports.Add(report);
                        }
                            
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Exception generating explain report: " + ex.Message);
                    reports.Clear();
                }
            }
        }

        public void Post(ServerAggregationFactory factory, string UUID, SummaryReport parent)
        {
            foreach (ExplainReport report in reports)
                report.Post(factory, UUID, parent);
        }
    }

    internal class ExplainReport : JsonReport
    {
        public long id;
        public string select_type;
        public string table;
        public string type;
        public string possible_keys;
        public string key;
        public string key_len;
        public string refValue;
        public long rows;
        public string Extra;
        private int rowNumber;

        public ExplainReport(int row)
        {
            rowNumber = row;
        }

        public override string ShouldSerialize(string name, object value)
        {
            if (name == "refValue") return "ref";
            return name;
        }

        public void Post(ServerAggregationFactory factory, string UUID, SummaryReport summary)
        {
            StringBuilder url = new StringBuilder();
            name = String.Format("{0}.{1}.{2}.{2}.{3}", UUID, summary.database, summary.text_hash, rowNumber);
            parent = String.Format("/instance/mysql/statement/{0}.{1}.{2}.{2}", UUID, summary.database, summary.text_hash);
            url.AppendFormat("{0}/v2/rest/instance/mysql/explain/{1}",
                factory.EMHost, name);
            Upload(factory, url.ToString());
        }
    }
}
