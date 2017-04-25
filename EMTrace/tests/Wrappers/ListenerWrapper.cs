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

namespace MySql.EMTrace.Tests
{
    public class ListenerWrapper
    {
        private EMTraceListener listener;

        public ListenerWrapper(EMTraceListener l)
        {
            listener = l;
        }

        public IDictionary<long, string> ConnectionStrings
        {
            get
            {
                FieldInfo fi = listener.GetType().GetField("connectionStrings", BindingFlags.Instance | BindingFlags.NonPublic);
                return fi.GetValue(listener) as IDictionary<long, string>;
            }
        }

        public ServerAggregationFactoryWrapper ServerFactory
        {
            get
            {
                FieldInfo fi = listener.GetType().GetField("serverFactory", BindingFlags.Instance | BindingFlags.NonPublic);
                return new ServerAggregationFactoryWrapper(fi.GetValue(listener));
            }
        }

        public IDictionary InProcessQueries
        {
            get
            {
                FieldInfo fi = listener.GetType().GetField("inProcessQueries", BindingFlags.Instance | BindingFlags.NonPublic);
                return fi.GetValue(listener) as IDictionary;
            }
        }


    }
}
