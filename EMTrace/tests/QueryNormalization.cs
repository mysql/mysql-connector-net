// Copyright (c) 2009 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySql.EMTrace.Tests
{
    public class QueryNormalization
    {
        [Fact]
        public void Numbers()
        {
            QueryNormalizer n = new QueryNormalizer();
            Assert.Equal("SELECT ?", n.Normalize("SELECT 1"));
            Assert.Equal("SELECT ?", n.Normalize("SELECT 1.0"));
            Assert.Equal("SELECT ?", n.Normalize("SELECT -1"));
            Assert.Equal("SELECT ?", n.Normalize("SELECT - 1"));
            Assert.Equal("SELECT ? - ?", n.Normalize("SELECT 1 - 1"));
            Assert.Equal("SELECT ?-?", n.Normalize("SELECT 1-1"));
            Assert.Equal("SELECT a-?", n.Normalize("SELECT a-1"));

            Assert.Equal("SELECT ? + ? / ? * ? - ? * ABS(?)-? -? FROM tbl WHERE a = ?", 
                n.Normalize("SELECT -1 + -1 / -1 * -1 - -1 * ABS(-1)-1 /* */ -1 FROM tbl WHERE a = -1"));
            Assert.Equal("INSERT INTO tbl VALUES (?)", n.Normalize("INSERT INTO tbl VALUES (1)"));
            Assert.Equal("INSERT", n.QueryType);
        }

        [Fact]
        public void ValueLists()
        {
            QueryNormalizer n = new QueryNormalizer();
            Assert.Equal("INSERT INTO tbl VALUES (?) /* , ... */", 
                n.Normalize("INSERT INTO tbl VALUES (1), (2)"));
            Assert.Equal("INSERT INTO tbl VALUES (?) /* , ... */", 
                n.Normalize("INSERT INTO tbl VALUES (1), (2), (3)"));
            Assert.Equal("INSERT INTO tbl VALUES (?) ON DUPLICATE KEY UPDATE id = ?",
                n.Normalize("INSERT INTO tbl VALUES (1) ON DUPLICATE KEY UPDATE id = 1"));
            Assert.Equal("INSERT INTO tbl VALUES (?) /* , ... */ ON DUPLICATE KEY UPDATE id = ?",
                n.Normalize("INSERT INTO tbl VALUES (1), (2), (3) ON DUPLICATE KEY UPDATE id = 1"));
        }

        [Fact]
        public void StoredProcs()
        {
            QueryNormalizer n = new QueryNormalizer();
            Assert.Equal("CALL spTest(?)", n.Normalize("CALL spTest(1)"));
            Assert.Equal("CALL", n.QueryType);
        }

        [Fact]
        public void InClause()
        {
            QueryNormalizer n = new QueryNormalizer();

            Assert.Equal("SELECT ? FROM tbl WHERE a IN ( ? /* , ... */ )",
                n.Normalize("SELECT 1 FROM tbl WHERE a IN ( -1, 1, -1 )"));
            Assert.Equal("SELECT ? FROM tbl WHERE a IN ( ? /* , ... */ )",
                n.Normalize("SELECT -1 FROM tbl WHERE a IN ( -1, 1, -1 )"));
            
            Assert.Equal("SELECT * FROM tbl WHERE fld IN ( ? /* , ... */ )",
                n.Normalize("SELECT * FROM tbl WHERE fld IN ( 1 )"));
            Assert.Equal("SELECT * FROM tbl WHERE fld IN ( ? /* , ... */ )",
                n.Normalize("SELECT * FROM tbl WHERE fld IN ( 1, '2', 3.0 , 4 )"));
            Assert.Equal("SELECT * FROM tbl WHERE fld IN ( ? /* , ... */ )",
                n.Normalize("SELECT * FROM tbl WHERE fld IN ( 1, '2', 3.0 /* , 4 */ )"));
            Assert.Equal("SELECT * FROM tbl WHERE fld IN ( ?, ?, ? /*! , 4 */ )",
                n.Normalize("SELECT * FROM tbl WHERE fld IN ( 1, '2', 3.0 /*! , 4 */ )"));
            Assert.Equal("SELECT * FROM tbl WHERE fld IN ( ?, (?), ?, ? )",
                n.Normalize("SELECT * FROM tbl WHERE fld IN ( 1, (2), 3, 4 )"));
            Assert.Equal("SELECT * FROM tbl WHERE fld IN ( SELECT id FROM tbl2 WHERE id IN (? /* , ... */ ))",
                n.Normalize("SELECT * FROM tbl WHERE fld IN ( SELECT id FROM tbl2 WHERE id IN (1, 2, 3, 4 ))"));
            Assert.Equal("INSERT INTO fld SELECT id FROM tbl2 WHERE id IN (? /* , ... */ )",
                n.Normalize("INSERT INTO fld SELECT id FROM tbl2 WHERE id IN (1, 2, 3, 4 )"));
            Assert.Equal("SELECT * FROM ( SELECT id FROM tbl3 WHERE fld IN (? /* , ... */ )) AS foo WHERE (fld2, fld3) = (?, ?) AND fld2 IN ( SELECT ? )",
                n.Normalize("SELECT * FROM ( SELECT id FROM tbl3 WHERE fld IN (1, 2)) AS foo WHERE (fld2, fld3) = (1, 4) AND fld2 IN ( SELECT 1 )"));
        }

        [Fact]
        public void CommandTypes()
        {
            QueryNormalizer n = new QueryNormalizer();
            Assert.Equal("(SELECT ?)", n.Normalize("(SELECT 1)"));

            Assert.Equal("((SELECT ?))", n.Normalize("((SELECT 1))"));
            Assert.Equal("(INSERT INTO tbl VALUES ())", n.Normalize("(INSERT INTO tbl VALUES ())"));
            Assert.Equal("SELECT ?", n.Normalize("/* */SELECT 1"));
            Assert.Equal("/*! */ SELECT ?", n.Normalize("/*! */ SELECT 1"));
        }

        [Fact]
        public void BinaryParameters()
        {
            string sql = @"INSERT INTO test VALUES ('a', 'b', _binary '\0ab\'\0ab')";
            QueryNormalizer n = new QueryNormalizer();
            Assert.Equal("INSERT INTO test VALUES (?, ?, _binary ?)", n.Normalize(sql));
        }
    }
}
