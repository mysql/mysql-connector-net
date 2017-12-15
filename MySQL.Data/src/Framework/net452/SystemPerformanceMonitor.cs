// Copyright (c) 2004, 2016, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Diagnostics;


namespace MySql.Data.MySqlClient
{
    internal class SystemPerformanceMonitor : PerformanceMonitor
    {
        private static PerformanceCounter procedureHardQueries;
        private static PerformanceCounter procedureSoftQueries;

        public SystemPerformanceMonitor(MySqlConnection connection) : base(connection)
        {
            string categoryName = Resources.PerfMonCategoryName;

            if (connection.Settings.UsePerformanceMonitor && procedureHardQueries == null)
            {
                try
                {
                    procedureHardQueries = new PerformanceCounter(categoryName,
                                                                  "HardProcedureQueries", false);
                    procedureSoftQueries = new PerformanceCounter(categoryName,
                                                                  "SoftProcedureQueries", false);
                }
                catch (Exception ex)
                {
                    MySqlTrace.LogError(connection.ServerThread, ex.Message);
                }
            }
        }

        private void EnsurePerfCategoryExist()
        {
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection();
            CounterCreationData ccd = new CounterCreationData();
            ccd.CounterType = PerformanceCounterType.NumberOfItems32;
            ccd.CounterName = "HardProcedureQueries";
            ccdc.Add(ccd);

            ccd = new CounterCreationData();
            ccd.CounterType = PerformanceCounterType.NumberOfItems32;
            ccd.CounterName = "SoftProcedureQueries";
            ccdc.Add(ccd);
            if (!PerformanceCounterCategory.Exists(Resources.PerfMonCategoryName))
                PerformanceCounterCategory.Create(Resources.PerfMonCategoryName,"", new PerformanceCounterCategoryType(), ccdc);
        }

        public override void AddHardProcedureQuery()
        {
            if (!Connection.Settings.UsePerformanceMonitor ||
                procedureHardQueries == null) return;
            procedureHardQueries.Increment();
        }

        public override void AddSoftProcedureQuery()
        {
            if (!Connection.Settings.UsePerformanceMonitor ||
                procedureSoftQueries == null) return;
            procedureSoftQueries.Increment();
        }
    }
}