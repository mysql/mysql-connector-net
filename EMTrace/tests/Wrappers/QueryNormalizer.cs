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
    public class QueryNormalizer
    {
        object instance;

        public QueryNormalizer()
        {
            instance = typeof(EMTraceListener).Assembly.CreateInstance("MySql.EMTrace.QueryNormalizer",
                false, BindingFlags.CreateInstance, null, null, null, null);
        }

        public string QueryType
        {
            get
            {
                FieldInfo fi = instance.GetType().GetField("queryType", BindingFlags.Instance | BindingFlags.NonPublic);
                return fi.GetValue(instance) as string;
            }
        }

        public string Normalize(string sql)
        {
            return (string)instance.GetType().InvokeMember("Normalize",
                BindingFlags.InvokeMethod, null, instance, new object[] { sql });
        }
    }
}
