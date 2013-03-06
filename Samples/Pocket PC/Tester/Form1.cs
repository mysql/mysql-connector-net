// Copyright (C) 2004-2007 MySQL AB
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient.Tests;
using System.Reflection;
using System.Collections;
using System.Threading;

namespace DeviceApplication1
{
    public partial class MainForm : Form
    {
        private TestRunner runner;
        public delegate void NodeUpdater(int fixture, int test, int index);
        public NodeUpdater updater;
        int fixtureStart;
        int fixtureEnd;
        int testStart;
        int testEnd;

        public MainForm()
        {
            InitializeComponent();
            LoadTests();
            updater = new NodeUpdater(UpdateNode);
        }

        private void LoadTests()
        {
            runner = new TestRunner();
            ArrayList tests = runner.LoadTests();
            foreach (TestCollection tc in tests)
            {
                TreeNode fixture = testTree.Nodes.Add(tc.name);
                fixture.Tag = tc;
                foreach (TestMethod tm in tc.testMethods)
                {
                    TreeNode test = fixture.Nodes.Add(tm.member.Name);
                    test.Tag = tm;
                }
            }
        }

        private void RunTests()
        {
            Thread t = new Thread(new ThreadStart(RunTestsWorker));
            t.Start();
        }

        private void UpdateNode(int fixture, int test, int index)
        {
            TreeNode node;

            if (test == -1)
                node = testTree.Nodes[fixture];
            else
                node = testTree.Nodes[fixture].Nodes[test];
            node.ImageIndex = index;
            node.SelectedImageIndex = index;
        }

        private void RunTestsWorker()
        {
            for (int i = fixtureStart; i < fixtureEnd; i++)
            {
                int fixtureIndex = 1;

                try
                {
                    TestCollection tc = (TestCollection)testTree.Nodes[i].Tag;
                    runner.StartFixture(tc);

                    int myTestEnd = testEnd;
                    if (testEnd == -1)
                        myTestEnd = tc.testMethods.Count;
                    for (int x = testStart; x < myTestEnd; x++)
                    {
                        int index = 1;
                        if (!runner.RunTest(i, x))
                        {
                            fixtureIndex = 0;
                            index = 0;
                        }
                        this.Invoke(updater, i, x, index);
                    }

                    runner.EndFixture(tc);
                }
                catch (Exception ex)
                {
                    fixtureIndex = 0;
                }
                this.Invoke(updater, i, -1, fixtureIndex);
            }
        }

        private void showDetailsMenu_Click(object sender, EventArgs e)
        {
            if (testTree.SelectedNode == null)
                MessageBox.Show("No test selected");
            FailDetails fd = new FailDetails();
            TreeNode node = testTree.SelectedNode;
            if (node.Parent == null)
            {
                TestCollection tc = (TestCollection)node.Tag;
                fd.Message = tc.message;
                fd.Trace = tc.stack;
            }
            else
            {
                TestMethod tm = (TestMethod)node.Tag;
                fd.Message = tm.message;
                fd.Trace = tm.stack;
            }
            fd.ShowDialog();
        }

        private void runTestsMenu_Click(object sender, EventArgs e)
        {
            ResetTree();
            fixtureStart = 0;
            fixtureEnd = testTree.Nodes.Count;
            testStart = 0;
            testEnd = -1;
            RunTests();
        }

        private void runSelected_Click(object sender, EventArgs e)
        {
            if (testTree.SelectedNode == null)
                MessageBox.Show("No test selected");
            ResetTree();
            TreeNode selNode = testTree.SelectedNode;
            testStart = 0;
            testEnd = -1;
            if (selNode.Parent != null)
            {
                testStart = selNode.Index;
                testEnd = testStart + 1;
                selNode = selNode.Parent;
            }
            fixtureStart = selNode.Index;
            fixtureEnd = fixtureStart + 1;
            RunTests();
        }

        private void ResetTree()
        {
            foreach (TreeNode node in testTree.Nodes)
            {
                node.ImageIndex = 2;
                node.SelectedImageIndex = 2;
                foreach (TreeNode test in node.Nodes)
                {
                    test.ImageIndex = 2;
                    test.SelectedImageIndex = 2;
                }
            }
        }
    }
}