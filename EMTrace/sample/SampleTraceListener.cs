// Copyright (c) 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace MySql.EMTrace.SampleApplication
{
    class SampleTraceListener : TraceListener
    {
        private StringBuilder text = new StringBuilder();

        public SampleTraceListener()
        {
        }

        public void UpdateLog(TextBox log)
        {
            lock (this)
            {
                log.Text += text.ToString();
                text = new StringBuilder(); 
            }
        }

        public override void Write(string message)
        {
            lock (this)
            {
                text.Append(String.Format("[{0}]-{1}", DateTime.Now, message));
            }
        }

        public override void WriteLine(string message)
        {
            lock (this)
            {
                text.AppendLine(String.Format("[{0}]-{1}", DateTime.Now, message));
            }
        }
    }
}
