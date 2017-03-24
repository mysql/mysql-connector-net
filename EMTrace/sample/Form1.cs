// Copyright (c) 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

namespace MySql.EMTrace.SampleApplication
{
    public partial class Form1 : Form
    {
        private SampleTraceListener listener = new SampleTraceListener();

        public Form1()
        {
            InitializeComponent();
            Trace.Listeners.Add(listener);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // we are using the provider factory method here just to illustrate how to use
            // mysql client without having a direct reference to the library. 
            // This is the hard way of doing things.  It is for illusration only.
            DbProviderFactory f = null;
            try
            {
                f = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
                AssemblyName name = f.GetType().Assembly.GetName();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception instantiating the factory: " + ex.Message);
                return;
            }

            DbConnection c = f.CreateConnection();
            c.ConnectionString = connectionString.Text;
            using (c)
            {
                c.Open();

                DbDataAdapter da = f.CreateDataAdapter();
                DbCommand cmd = c.CreateCommand();
                cmd.CommandText = queryText.Text;
                cmd.Connection = c;
                da.SelectCommand = cmd;
                DataTable dt = new DataTable();
                try
                {
                    da.Fill(dt);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception during query execution: " + ex.Message);
                }
                resultsGrid.DataSource = dt;
            }
        }

        private void queryText_TextChanged(object sender, EventArgs e)
        {
            goButton.Enabled = queryText.Text.Trim().Length > 0 &&
                connectionString.Text.Trim().Length > 0;
        }

        private void connectionString_TextChanged(object sender, EventArgs e)
        {
            goButton.Enabled = queryText.Text.Trim().Length > 0 &&
                connectionString.Text.Trim().Length > 0;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            listener.UpdateLog(log);
        }

    }
}
