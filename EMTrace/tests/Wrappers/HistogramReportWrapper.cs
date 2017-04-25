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
    public class HistogramReportWrapper
    {
        private object innerObject;

        public HistogramReportWrapper(object o)
        {
            innerObject = o;
        }

        public object binValues
        {
            get { return GetValue("binValues"); }
        }

        public long nul
        {
            get { return (long)GetValue("nul"); }
        }

        public int baseValue
        {
            get { return (int)GetValue("baseValue"); }
        }

        public string nameSuffix
        {
            get { return (string)GetValue("nameSuffix"); }
        }

        public double divisor
        {
            get { return (double)GetValue("divisor"); }
        }

        private object GetValue(string field)
        {
            FieldInfo fi = innerObject.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic);
            return fi.GetValue(innerObject);
        }

        public string ToString()
        {
            MethodInfo mi = innerObject.GetType().GetMethod("ToString");
            return (string)mi.Invoke(innerObject, null);
        }
    }
}
