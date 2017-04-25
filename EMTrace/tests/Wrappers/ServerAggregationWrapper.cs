// Copyright (c) 2009 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using Xunit;

namespace MySql.EMTrace.Tests
{
    public class ServerAggregationWrapper
    {
        private object innerObject;

        public ServerAggregationWrapper(object o)
        {
            innerObject = o;
        }

        public string UUID
        {
            get 
            {
                FieldInfo fi = innerObject.GetType().GetField("UUID");
                return fi.GetValue(innerObject) as string;
            }
        }

        public IDictionary Queries
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("queries", BindingFlags.Instance | BindingFlags.Public);
                return fi.GetValue(innerObject) as IDictionary;
            }
        }

        public QueryAggregationWrapper GetQuery(int index)
        {
            Debug.Assert(index < Queries.Count);
            string[] keys = new string[Queries.Keys.Count];
            Queries.Keys.CopyTo(keys, 0);
            return new QueryAggregationWrapper(Queries[keys[index]]);
        }

        public void WaitForNumberOfQueries(int num, int maxSeconds)
        {
            DateTime start = DateTime.Now;
            while (true)
            {
                if (Queries.Count >= num) return;
                if (DateTime.Now.Subtract(start).TotalSeconds >= maxSeconds)
                    Assert.True(false, "Timeout waiting for number of queries to appear");
            }
        }
    }
}
