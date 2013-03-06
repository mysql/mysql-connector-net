using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.Common;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace Profiling
{
    public partial class Form1 : Form
    {
        MySqlConnection connection;

        public Form1()
        {
            InitializeComponent();
        }

        private void Log(string s)
        {
            output.Text += "\r\n" + s;
            output.Refresh();
        }

        private void testBtn_Click(object sender, EventArgs e)
        {
            output.Clear();
            DateTime start = DateTime.Now;
            Log("Starting tests at " + start.ToString());

            try
            {
                if (sockets.Checked)
                {
                    Log("\r\nTesting sockets");
                    TestSockets();
                }
                if (useNamedPipes.Checked)
                {
                    Log("\r\nTesting named pipes");
                    TestNamedPipes();
                }
                if (useSharedMem.Checked)
                {
                    Log("\r\nTesting shared memory");
                    TestSharedMemory();
                }
                Log("Done!");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            TimeSpan ts = DateTime.Now.Subtract(start);
            Log(ts.ToString());
        }

        private void TestSockets()
        {
            Test(String.Format(
                "server={0};uid={1};password={2};database={3};port={4}",
                server.Text, userid.Text, password.Text, database.Text, portNum.Text));
        }

        private void TestNamedPipes()
        {
            Test(String.Format(
                "server={0};uid={1};password={2};database={3};protocol=pipe;pipe={4}",
                server.Text, userid.Text, password.Text, database.Text, pipeName.Text));
        }

        private void TestSharedMemory()
        {
            Test(String.Format(
                "server={0};uid={1};password={2};database={3};protocol=memory;shared memory name={4}",
                server.Text, userid.Text, password.Text, database.Text, memName.Text));
        }

        private void Test(string connectionString)
        {
            string[] desc = new string[4] { "no compression, no pooling",
                "no compression, pooling", "compression, no pooling", "compression, pooling" };

            string[] addons = new string[4] { ";compress=false;pooling=false",
                ";compress=false;pooling=true", ";compress=true;pooling=false",
                ";compress=true;pooling=true"};

            for (int x=0; x < addons.Length; x++)
            {
                if (connection != null)
                    connection.Close();

                Log("\r\nTest " + desc[x]);
                connection = new MySqlConnection(connectionString + addons[x]);
                connection.Open();

                Log("\r\nnot using prepared tests");
                DoInsertTests(false);
                DoSelectTests(false);

                Log("\r\nusing prepared tests");
                DoInsertTests(true);
                DoSelectTests(true);
            }
        }

        private void DoInsertTests(bool prepared)
        {
            Log("Starting Insert tests");
            MySqlCommand cmd = new MySqlCommand("DROP TABLE IF EXISTS test", connection);
            cmd.ExecuteNonQuery();

            cmd.CommandText = "CREATE TABLE test (id INT, name VARCHAR(50), fl FLOAT, dt DATETIME)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO test VALUES (?id, ?name, ?f1, ?dt)";
            cmd.Parameters.Add("?id", MySqlDbType.Int32);
            cmd.Parameters.Add("?name", MySqlDbType.VarChar);
            cmd.Parameters.Add("?f1", MySqlDbType.Float);
            cmd.Parameters.Add("?dt", MySqlDbType.Datetime);
            if (prepared)
                cmd.Prepare();

            for (int x=0; x < 500; x++)
            {
                cmd.Parameters[0].Value = x;
                cmd.Parameters[1].Value = "Test";
                cmd.Parameters[2].Value = 0.0;
                cmd.Parameters[3].Value = DateTime.Now;
                cmd.ExecuteNonQuery();
            }
        }

        private void DoSelectTests(bool prepared)
        {
            Log("Starting select tests");
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", connection);
            cmd.CommandTimeout = 0;
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                int count = 0;
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    Trace.WriteLine("id = " + id);
                    string name = reader.GetString(1);
                    float fl = reader.GetFloat(2);
                    DateTime dt = reader.GetDateTime(3);
                    count++;
                }
                Log("read " + count + " records");
            }
        }
    }
}