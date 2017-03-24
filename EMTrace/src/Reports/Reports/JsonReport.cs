// Copyright (c) 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Globalization;

namespace MySql.EMTrace
{
    internal class JsonReport
    {
        protected string name;
        protected string parent;
        protected string valueString = "values";

        public string Name
        {
            get { return name; }
        }

        public string Parent
        {
            get { return parent; }
        }

        public virtual string ShouldSerialize(string name, object value)
        {
            return name;
        }

        public override string ToString()
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendFormat("\"name\": \"{0}\",{1}", name, Environment.NewLine);
            json.AppendFormat("\"parent\": \"{0}\",{1}", parent, Environment.NewLine);
            json.AppendFormat("\"{0}\": {{", valueString);

            string delimiter = "";
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo fi in fields)
            {
                object value = fi.GetValue(this);
                string fieldName = ShouldSerialize(fi.Name, value);
                if (fieldName != null)
                {
                    if (value is Array)
                        AppendArray(value as Array, json, ref delimiter);
                    else
                    {
                        json.AppendFormat("{0}{1}\"{2}\": \"{3}\"", delimiter, Environment.NewLine, fieldName, value);
                        delimiter = ",";
                    }
                }
            }
            json.AppendLine("}");
            json.AppendLine("}");
            return json.ToString();
        }

        private void AppendArray(Array a, StringBuilder json, ref string delimiter)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Int64 v = Convert.ToInt64(a.GetValue(i));
                if (v != 0)
                {
                    json.AppendFormat("{0}{1}\"{2}\": \"{3}\"", delimiter, Environment.NewLine, i, v);
                    delimiter = ",";
                }
            }
        }

        protected void Upload(ServerAggregationFactory factory, string url)
        {
            string content = this.ToString();
            if (factory.EMHost.ToLowerInvariant().StartsWith("test"))
            {
                Trace.WriteLine("==== post data ====");
                Trace.WriteLine("url = " + url.ToString());
                Trace.WriteLine("content = " + content);
                Trace.WriteLine("==== end of post data ====");
            }
            else
                factory.PostData(url, content);
        }

        protected string EscapeForJson(string inString)
        {
            if (String.IsNullOrEmpty(inString))
                return "\"\"";

            string lettersToEscape = "\\>\"\b\t\n\f\r";
            StringBuilder outString = new StringBuilder(inString.Length+4);
            foreach (char c in inString)
            {
                if (lettersToEscape.IndexOf(c) >= 0)
                    outString.Append("\\");
                else if (c < ' ')
                {
                    string val = "000" + int.Parse(c.ToString(), NumberStyles.HexNumber);
                    outString.Append("\\u" + val.Substring(val.Length - 4));
                    continue;
                }
                outString.Append(c);
            }
            return outString.ToString();
        }
    }
}
