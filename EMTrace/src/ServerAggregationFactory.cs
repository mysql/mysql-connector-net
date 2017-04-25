// Copyright © 2009, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using System.Net;
using System.Data.Common;
using System.Web;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MySql.EMTrace.Properties;
using System.IO;
using System.Diagnostics;

namespace MySql.EMTrace
{
    internal class ServerAggregationFactory
    {
        public string EMHost;
        public string UserId;
        public string Password;
        public int DefaultPostInterval;
        public Dictionary<string, ServerAggregation> Servers = new Dictionary<string, ServerAggregation>();

        public ServerAggregationFactory(string emHost, int defaultInterval, string userId, string pwd)
        {
            EMHost = emHost;
            UserId = userId;
            Password = pwd;
            DefaultPostInterval = defaultInterval;
            RemoteCertificateValidationCallback ValidateCallback =
                new RemoteCertificateValidationCallback(ValidateRemoteCertificate);
            ServicePointManager.ServerCertificateValidationCallback = ValidateCallback;
        }

        // callback used to validate the certificate in an SSL conversation
        private static bool ValidateRemoteCertificate(object sender,
            X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        public ServerAggregation GetServer(DbConnectionStringBuilder settings)
        {
            string key = String.Format("{0}-{1}-{2}", settings["server"], settings["port"], settings["database"]);
            if (!Servers.ContainsKey(key))
            {
                ServerAggregation sa = CreateServer(settings);
                Servers.Add(key, sa);
                return sa;
            }
            return Servers[key];
        }

        public ServerAggregation GetServer(string connectionString)
        {
            DbConnectionStringBuilder settings = ClassFactory.CreateConnectionStringBuilder();
            settings.ConnectionString = connectionString;
            return GetServer(settings);
        }

        private ServerAggregation CreateServer(DbConnectionStringBuilder settings)
        {
            ServerAggregation sa = new ServerAggregation(this, "0000");
            sa.Enabled = true;
            if (EMHost == "test") return sa;
            if (EMHost == "testWithExplain")
            {
              sa.CaptureExamples = true;
              sa.CaptureExplain = true;
              return sa;
            }

            sa.UUID = GetUUID(settings);
            if (EMHost == "test-uuid") return sa;

            sa.ReloadOptions();
            return sa;
        }

        public string DownloadData(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential(UserId, Password);
                return client.DownloadString(url);
            }
        }

        public void PostData(string url, string data)
        {
            Trace.WriteLine("url = " + url);
            Trace.Write(data);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            request.Credentials = new NetworkCredential(UserId, Password);
            request.Method = "PUT";
            request.ContentType = "application/json";
            request.ContentLength = bytes.Length;
            request.UserAgent = "MySql.EMTrace";
            request.ProtocolVersion = HttpVersion.Version10;

            Stream stream = request.GetRequestStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            stream.Close();
            Trace.WriteLine("Closed request stream");

            using (HttpWebResponse r = request.GetResponse() as HttpWebResponse)
            {
                Trace.WriteLine("posting returned status code = " + r.StatusCode);
                r.Close();
            }
            Trace.WriteLine("Closed response stream");
        }

        private string GetUUID(DbConnectionStringBuilder settings)
        {
            using (DbConnection c = ClassFactory.CreateConnection(settings.ConnectionString))
            {
                c.Open();
                object uuid = null;
                try
                {
                    DbCommand cmd = ClassFactory.CreateCommand("SELECT `value` FROM mysql.inventory where `name`='uuid'", c);
                    uuid = cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    // we either had insufficient privs to read the table or the table didn't exist
                    throw new Exception(Resources.UnableToReadUUID, ex);
                }
                if (uuid == null) throw new Exception(Resources.InvalidUUID);
                return (string)uuid;
            }
        }
    }

    /// <summary>
    /// Internal object used to allow setting WebRequest.CertificatePolicy to 
    /// not fail on Cert errors
    /// </summary>
    internal class AcceptAllCertificatePolicy : ICertificatePolicy
    {
        public AcceptAllCertificatePolicy()
        {
        }

        public bool CheckValidationResult(ServicePoint sPoint,
           X509Certificate cert, WebRequest wRequest, int certProb)
        {
            // *** Always accept
            return true;
        }
    }
}
