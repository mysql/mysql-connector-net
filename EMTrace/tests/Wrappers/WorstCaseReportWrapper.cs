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
    public class WorstCaseReportWrapper
    {
        private object innerObject;

        public WorstCaseReportWrapper(object o)
        {
            innerObject = o;
        }

        public string host_to
        {
            get { return (string)GetValue("host_to"); }
        }

        public string user
        {
            get { return (string)GetValue("user"); }
        }

        public long exec_time
        {
            get { return (long)GetValue("exec_time"); }
        }

        public int rows
        {
            get { return (int)GetValue("rows"); }
        }

        public string text
        {
            get { return (string)GetValue("text"); }
        }

        public int connection_id
        {
            get { return (int)GetValue("connection_id"); }
        }

        public string host_from
        {
            get { return (string)GetValue("host_from"); }
        }

        public string comment
        {
            get { return (string)GetValue("comment"); }
        }

        public long bytes
        {
            get { return (long)GetValue("bytes"); }
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
