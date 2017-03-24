// Copyright (c) 2009 Sun Microsystems, Inc.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using System.Data.Common;
using System.Diagnostics;
using System.Web;

namespace MySql.EMTrace
{
    internal class ServerAggregation
    {
        private static MD5CryptoServiceProvider hasher = new MD5CryptoServiceProvider();
        public Dictionary<string, QueryAggregation> queries = new Dictionary<string, QueryAggregation>();
        public ServerAggregationFactory Factory;
        public string UUID;
        public string Parent;
        public bool CaptureExamples;
        public bool CaptureExplain;
        public int PostIntervalCounter;
        public int PostInterval;
        public bool Enabled;
        public int minExplainTime;
        private bool posting;

        public ServerAggregation(ServerAggregationFactory factory, string uuid)
        {
            UUID = uuid;
            Factory = factory;
            PostInterval = PostIntervalCounter = factory.DefaultPostInterval;
        }

        public void DecrementPostCounter(int seconds)
        {
            PostIntervalCounter -= seconds;
            if (PostIntervalCounter <= 0)
            {
                PostData();
                ReloadOptions();
                PostIntervalCounter = PostInterval;
            }
        }

        public void ReloadOptions()
        {
            Parent = String.Format("/instance/mysql/server/{0}", UUID);
            string url = String.Format("{0}/v2/rest/instance/mysql/StatementAnalysisSupport/{1}",
                Factory.EMHost, UUID);

            string configData = Factory.DownloadData(url);

            // if config data is null or we get an error we assume defaults
            // we don't yet support capture of example "worst case" queries or explains
            configData = configData.Trim('{', '}', ' ', '\r', '\n');
            string[] lines = configData.Split(',');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                string[] keyValue = line.Trim().Split(':');
                int index = 0;
                if (keyValue.Length == 3)
                {
                    index = 1;
                    keyValue[index] = keyValue[index].Trim('{', ' ', '\r', '\n');
                }
                if (keyValue.Length < 2) continue;
                string key = keyValue[index++].Trim(' ', '"').ToLowerInvariant();
                string value = keyValue[index].Trim(' ', '"').ToLowerInvariant();
                if (key == "parent")
                    Parent = value;
                else if (key == "auto_explain_min_exec_time_ms")
                    minExplainTime = Int32.Parse(value);
                else if (key == "capture_examples")
                    CaptureExamples = Boolean.Parse(value);
                else if (key == "capture_explain")
                    CaptureExplain = Boolean.Parse(value);
                else if (key == "enabled")
                    Enabled = Boolean.Parse(value);
                else if (key == "frequency" && value != "null")
                    PostInterval = Int32.Parse(value);
            }
            Trace.WriteLine("Updated config data.  Parent = " + Parent);
        }

        public void AddNewQuery(MySqlTraceQuery qi)
        {
            QueryNormalizer qn = new QueryNormalizer();
            string normalizedText = qn.Normalize(qi.CommandText);

            // if both fullcommandtext and commandtext are non-empty
            // we assume it is prepared and no need to normalize
            if (String.IsNullOrEmpty(qi.FullCommandText))
            {
                qi.FullCommandText = qi.CommandText;
                qi.CommandText = normalizedText;
            }

            byte[] bytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(qi.CommandText));
            StringBuilder hash = new StringBuilder();
            foreach (byte b in bytes)
                hash.AppendFormat("{0:X2}", b);
            qi.CommandTextHash = hash.ToString();
            qi.QueryType = qn.QueryType;

            lock (queries)
            {
                qi.TotalRows = 0;
                qi.TotalBytes = 0;
                foreach (MySqlTraceQueryResult ri in qi.Results)
                {
                    qi.TotalRows += ri.RowsRead;
                    qi.TotalBytes += ri.SizeInBytes;
                }

                TimeSpan queryTime = new TimeSpan(qi.StopTimestamp - qi.StartTimestamp);
                qi.ExecutionTime = (long)(queryTime.TotalMilliseconds * 1000);
                qi.ShouldExplain = queryTime.TotalMilliseconds >= minExplainTime;

                QueryAggregation qa = null;
                if (queries.ContainsKey(qi.CommandText))
                    qa = queries[qi.CommandText];
                else
                {
                    qa = new QueryAggregation(this);
                    queries.Add(qi.CommandText, qa);
                }
                qa.AddQuery(qi);
            }
        }

        private void PostData()
        {
            if (posting) return;

            posting = true;
            try 
            {
                List<QueryAggregation> qaToPost = null;

                lock (queries)
                {
                    qaToPost = new List<QueryAggregation>(queries.Values);
                    queries.Clear();
                }

                foreach (QueryAggregation qa in qaToPost)
                {
                    try
                    {
                        qa.PostData(Factory, UUID, Parent);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Caught exception posting query data: " + ex.Message);
                    }
                }
            }
            finally 
            {
                posting = false;
            }
        }
    }
}
