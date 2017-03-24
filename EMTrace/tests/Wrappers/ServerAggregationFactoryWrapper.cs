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
    public class ServerAggregationFactoryWrapper
    {
        private object innerObject;

        public ServerAggregationFactoryWrapper(object o)
        {
            innerObject = o;
        }

        public string EMHost
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("EMHost", BindingFlags.Instance | BindingFlags.Public);
                return fi.GetValue(innerObject) as string;
            }
        }

        public IDictionary Servers
        {
            get
            {
                FieldInfo fi = innerObject.GetType().GetField("Servers", BindingFlags.Instance | BindingFlags.Public);
                return fi.GetValue(innerObject) as IDictionary;
            }
        }

        public ServerAggregationWrapper GetServer(int index)
        {
            Debug.Assert(index < Servers.Count);
            string[] keys = new string[Servers.Keys.Count];
            Servers.Keys.CopyTo(keys, 0);
            return new ServerAggregationWrapper(Servers[keys[index]]);
        }

        public void WaitForNumberOfServers(int num, int maxSeconds)
        {
            maxSeconds = 10;
            for (int i = 0; i < (maxSeconds * 2); i++)
            {
                if (Servers.Count >= num) return;
                System.Threading.Thread.Sleep(500);
            }
            Assert.True(false, "Failed waiting for the number of servers to appear");
        }
    }
}
