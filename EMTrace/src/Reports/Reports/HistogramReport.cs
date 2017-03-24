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
    internal class HistogramReport : JsonReport
    {
        public long[] binValues;
        public long nul;
        public int baseValue;
        private string nameSuffix;
        double divisor;

        public HistogramReport(string nameSuffix, int binBase)
        {
            baseValue = binBase;
            divisor = Math.Log(baseValue);
            this.nameSuffix = nameSuffix;

            int power = 1;
            while (Math.Pow(binBase, power) <= Int64.MaxValue)
                power++;
            binValues = new long[power];
        }

        public override string ShouldSerialize(string name, object value)
        {
            if (name == "nul")
            {
                long v = Convert.ToInt64(value);
                if (v == 0) return null;
            }
            else if (name == "baseValue")
                return "base";
            return name;
        }

        protected void UpdateValue(long value)
        {
            if (value == 0)
                nul++;
            else
            {
                int bin = (int)(Math.Log(value) / divisor);
                binValues[bin]++;
            }
        }

        public void Post(ServerAggregationFactory factory, string UUID, SummaryReport summary)
        {
            StringBuilder url = new StringBuilder();
            name = String.Format("{0}.{1}.{2}.{3}", UUID, summary.database, summary.text_hash, nameSuffix);
            parent = String.Format("/instance/mysql/statementsummary/{0}", summary.Name);
            url.AppendFormat("{0}/v2/rest/instance/util/loghistogram/{1}",
                factory.EMHost, name);
            Upload(factory, url.ToString());
        }
    }

    internal class ExecTimeHistogramReport : HistogramReport
    {
        public ExecTimeHistogramReport(int binBase) : base("exec_time", binBase)
        {
        }

        public void UpdateFromQuery(MySqlTraceQuery qi)
        {
            UpdateValue(qi.ExecutionTime);
        }

    }

    internal class RowsHistogramReport : HistogramReport
    {
        public RowsHistogramReport(int binBase) : base("row_count", binBase)
        {
        }

        public void UpdateFromQuery(MySqlTraceQuery qi)
        {
            UpdateValue(qi.TotalRows);
        }
    }
}
