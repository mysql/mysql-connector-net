// Copyright (c) 2009 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Net;

namespace MySql.EMTrace
{
    internal class QueryAggregation
    {
        private SummaryReport summary;
        private WorstCaseReport worstCase;
        private ExplainReportManager explain;
        private ExecTimeHistogramReport execTimeReport;
        private RowsHistogramReport rowsReport;
        private ServerAggregation aggregatingServer;
        private int numAggregates;
        private static readonly string[] explainStatements = new string[] { "SELECT", "INSERT", "REPLACE", "UPDATE", "DELETE" };

        public QueryAggregation(ServerAggregation server)
        {
            aggregatingServer = server;
        }

        public string Text
        {
            get { return summary != null ? summary.text : null; }
        }

        private bool NeedsExplain(MySqlTraceQuery qi)
        {
            if (!aggregatingServer.CaptureExamples) return false;
            if (!aggregatingServer.CaptureExplain) return false;
            if (!qi.ShouldExplain) return false;
            if (!ValidateExplain(qi.FullCommandText)) return false;
            if (explain == null) return true;
            if (worstCase == null) return true;
            if (qi.ExecutionTime >= (worstCase.exec_time * 1.15))
                return true;
            return false;
        }

        public void AddQuery(MySqlTraceQuery qi)
        {
            if (summary == null)
            {
                summary = new SummaryReport(qi);
                execTimeReport = new ExecTimeHistogramReport(2);
                rowsReport = new RowsHistogramReport(2);
            }
            else
                summary.UpdateFromQuery(qi);

            execTimeReport.UpdateFromQuery(qi);
            rowsReport.UpdateFromQuery(qi);

            if (NeedsExplain(qi))
                explain = new ExplainReportManager(qi);

            if (worstCase == null || qi.ExecutionTime > worstCase.exec_time)
                worstCase = new WorstCaseReport(qi);
            numAggregates++;
        }

        public void PostData(ServerAggregationFactory factory, string UUID, string Parent)
        {
            // post summary
            Trace.WriteLine("Posting summary data for query " + summary.text);
            summary.Post(factory, UUID, Parent);
            Trace.WriteLine("Done");

            // then execution times histogram
            Trace.WriteLine("Posting execution time histogram for query " + summary.text);
            execTimeReport.Post(factory, UUID, summary);
            Trace.WriteLine("Done");

            // then rows histogram
            Trace.WriteLine("Posting rows histogram for query " + summary.text);
            rowsReport.Post(factory, UUID, summary);
            Trace.WriteLine("Done");

            // then worst query
            if (aggregatingServer.CaptureExamples)
            {
                Trace.WriteLine("Posting worst case data for query " + summary.text);
                worstCase.Post(factory, UUID, summary);
                Trace.WriteLine("Done");

                // then explain report
                if (explain != null)
                {
                    Trace.WriteLine("Posting explain report for query " + summary.text);
                    explain.Post(factory, UUID, summary);
                    Trace.WriteLine("Done");
                }
            }
        }

        private bool ValidateExplain(string commandText)
        {
          foreach (string statement in explainStatements)
          {
            if (commandText.StartsWith(statement, StringComparison.InvariantCultureIgnoreCase)) return true;
          }
          return false;
        }

    }


}
