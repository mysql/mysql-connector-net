// Copyright (c) 2009 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace MySql.EMTrace.Tests
{
    class TraceCollector : TraceListener
    {
        StringCollection strings;
        StringBuilder partial;

        public TraceCollector()
        {
            strings = new StringCollection();
            partial = new StringBuilder();
        }

        public StringCollection Strings
        {
            get { return strings; }
        }

        public int Find(string sToFind)
        {
            int count = 0;
            foreach (string s in strings)
                if (s.IndexOf(sToFind) != -1)
                    count++;
            return count;
        }

        public void Clear()
        {
            partial.Remove(0, partial.Length);
            strings.Clear();
        }

        public override void Write(string message)
        {
            partial.Append(message);
        }

        public override void WriteLine(string message)
        {
            Write(message);
            strings.Add(partial.ToString());
            partial.Remove(0, partial.Length);
        }
    }
}
