// Copyright (c) 2009 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Data.Common;
using MySql.EMTrace.Properties;
using System.Reflection;
using System.Diagnostics;

namespace MySql.EMTrace
{
    internal class ClassFactory
    {
        private static DbProviderFactory factory;

        public static bool LoadAndCheckFactory()
        {
            try
            {
                factory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("{0} - {1}",
                    Resources.MySqlNotPresent, ex.Message));
                return false;
            }
            AssemblyName name = factory.GetType().Assembly.GetName();
            if (name.Version < new Version(6, 2, 2))
            {
                Trace.WriteLine(Resources.MySqlDataTooOld);
                factory = null;
                return false;
            }
            return true;
        }

        public static DbConnection CreateConnection(string connectionString)
        {
            DbConnection c = factory.CreateConnection();
            c.ConnectionString = connectionString;
            return c;
        }

        public static DbCommand CreateCommand(string sql, DbConnection c)
        {
            DbCommand cmd = factory.CreateCommand();
            cmd.Connection = c;
            cmd.CommandText = sql;
            return cmd;
        }

        public static DbConnectionStringBuilder CreateConnectionStringBuilder() 
        {
          if (factory == null) throw new ArgumentNullException("DbProviderFactory");
          return factory.CreateConnectionStringBuilder();
        }
    }
}
