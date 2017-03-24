// Copyright (c) 2009 Sun Microsystems, Inc.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace MySql.EMTrace.Tests
{
    public class SummaryReportWrapper
    {
        private object innerObject;

        public SummaryReportWrapper(object o)
        {
            innerObject = o;
        }

        public string text_hash
        {
            get { return (string)GetValue("text_hash"); }
        }

        public string text
        {
            get { return (string)GetValue("text"); }
        }

        public string query_type
        {
            get { return (string)GetValue("query_type"); }
        }

        public string database
        {
            get { return (string)GetValue("database"); }
        }

        public long bytes
        {
            get { return (long)GetValue("bytes"); }
        }

        public long min_bytes
        {
            get { return (long)GetValue("min_bytes"); }
        }

        public long max_bytes
        {
            get { return (long)GetValue("max_bytes"); }
        }

        public long exec_time
        {
            get { return (long)GetValue("exec_time"); }
        }

        public long min_exec_time
        {
            get { return (long)GetValue("min_exec_time"); }
        }

        public long max_exec_time
        {
            get { return (long)GetValue("max_exec_time"); }
        }

        public int rows
        {
            get { return (int)GetValue("rows"); }
        }

        public int min_rows
        {
            get { return (int)GetValue("min_rows"); }
        }

        public int max_rows
        {
            get { return (int)GetValue("max_rows"); }
        }

        public int count
        {
            get { return (int)GetValue("count"); }
        }

        public long errors
        {
            get { return (long)GetValue("errors"); }
        }

        public long warnings
        {
            get { return (long)GetValue("warnings"); }
        }

        public long no_good_index_used
        {
            get { return (long)GetValue("no_good_index_used"); }
        }

        public long no_index_used
        {
            get { return (long)GetValue("no_index_used"); }
        }

        private object GetValue(string field)
        {
            FieldInfo fi = innerObject.GetType().GetField(field);
            return fi.GetValue(innerObject);
        }

        public string ToString()
        {
            MethodInfo mi = innerObject.GetType().GetMethod("ToString");
            return (string)mi.Invoke(innerObject, null);
        }
    }
}
