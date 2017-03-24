// Copyright (c) 2009 Sun Microsystems, Inc.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Diagnostics;

namespace MySql.EMTrace.Tests
{
    public class QueryAggregationWrapper
    {
        private object innerObject;

        public QueryAggregationWrapper()
        {
            innerObject = typeof(EMTraceListener).Assembly.CreateInstance("MySql.EMTrace.QueryAggregation", true, 
                BindingFlags.NonPublic, null, null, CultureInfo.InvariantCulture, null);
        }

        public QueryAggregationWrapper(object o)
        {
            innerObject = o;
        }

        public void WaitForNumberOfAggregates(int numAgg, int numSeconds)
        {
            for (int i = 0; i < (numSeconds * 2); i++)
            {
                if (NumAggregates >= numAgg) return;
                System.Threading.Thread.Sleep(500);
            }
        }

        public void WaitForSummary(int seconds)
        {
            for (int i=0; i < (seconds * 2); i++)
            {
                if (Summary != null) return;
                System.Threading.Thread.Sleep(500);
            }
        }

        public void WaitForWorstCase(int seconds)
        {
            for (int i = 0; i < (seconds * 2); i++)
            {
                if (WorstCase != null) return;
                System.Threading.Thread.Sleep(500);
            }
        }

        public void WaitForExplainReport(int seconds)
        {
          for (int i = 0; i < (seconds * 2); i++)
          {
            if (ExplainReport != null) return;
            System.Threading.Thread.Sleep(500);
          }
        }

        public int NumAggregates
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("numAggregates", BindingFlags.Instance | BindingFlags.NonPublic);
                return (int)fi.GetValue(innerObject);
            }
        }

        public SummaryReportWrapper Summary
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("summary", BindingFlags.Instance | BindingFlags.NonPublic);
                object o = fi.GetValue(innerObject);
                if (o == null) return null;
                return new SummaryReportWrapper(o);
            }
        }

        public HistogramReportWrapper ExecTimesHistogram
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("execTimeReport", BindingFlags.Instance | BindingFlags.NonPublic);
                object o = fi.GetValue(innerObject);
                if (o == null) return null;
                return new HistogramReportWrapper(o);
            }
        }

        public HistogramReportWrapper RowsHistogram
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("rowsReport", BindingFlags.Instance | BindingFlags.NonPublic);
                object o = fi.GetValue(innerObject);
                if (o == null) return null;
                return new HistogramReportWrapper(o);
            }
        }

        public WorstCaseReportWrapper WorstCase
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("worstCase", BindingFlags.Instance | BindingFlags.NonPublic);
                object o = fi.GetValue(innerObject);
                if (o == null) return null;
                return new WorstCaseReportWrapper(o);
            }
        }

        internal ExplainReport ExplainReport
        {
          get
          {
            FieldInfo fi = innerObject.GetType().GetField("explain", BindingFlags.Instance | BindingFlags.NonPublic);
            object o = fi.GetValue(innerObject);
            if (o == null) return null;

            FieldInfo fieldReports = o.GetType().GetField("reports", BindingFlags.Instance | BindingFlags.NonPublic);
            object objectReports = fieldReports.GetValue(o);
            if (objectReports == null || ((IList)objectReports).Count == 0) return null;
            
            return (ExplainReport)((IList)objectReports)[0];
          }
        }
    }
}
