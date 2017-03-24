// Copyright (c) 2009 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Data.Common;
using Xunit;

namespace MySql.EMTrace.Tests
{
    public class MySqlServerTests : BaseTest
    {
        public MySqlServerTests() : base("test-uuid")
        {
        }

        public override void SetFixture(SetUpTests data)
        {
          base.SetFixture(data);
          data.ExecRootSql("DROP TABLE IF EXISTS `mysql`.`inventory`");
          data.ExecRootSql(@"CREATE TABLE `mysql`.`inventory`(`name` VARCHAR(64) NOT NULL, 
                `value` VARCHAR(64) DEFAULT NULL, PRIMARY KEY (`name`)) ENGINE=MyISAM DEFAULT CHARSET=utf8");
        }

        [Fact]
        public void FetchUUID()
        {
            Guid g = Guid.NewGuid();
            SetupData.ExecRootSql(String.Format("INSERT INTO `mysql`.`inventory` VALUES ('uuid', '{0}')", g));

            // simulate an open connection event
            OpenConnection(1, SetupData.rootConnectionString, 1);
            OpenQuery(1, 1, "SELECT 1");
            OpenResult(1, 1, -1, -1);
            CloseResult(1, 1, 1, 76);
            CloseQuery(1);
            CloseConnection(1);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 2);
            Assert.Equal(1, wrapper.ServerFactory.Servers.Count);
            ServerAggregationWrapper saw = wrapper.ServerFactory.GetServer(0);
            Assert.Equal(g.ToString(), saw.UUID);
        }
    }
}
