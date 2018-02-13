// Copyright © 2013, 2016, Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Data;
using System.Data.SqlTypes;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class MySqlDataReaderTests : TestBase
  {

    /// <summary>
    /// Bug #8630  	Executing a query with the SchemaOnly option reads the entire resultset
    /// </summary>
    [Fact]
    public void SchemaOnly()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id,name) VALUES(1,'test1')");
      executeSQL("INSERT INTO Test (id,name) VALUES(2,'test2')");
      executeSQL("INSERT INTO Test (id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
      {
        DataTable table = reader.GetSchemaTable();
        Assert.Equal(5, table.Rows.Count);
        Assert.Equal(22, table.Columns.Count);
        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Tests fix for bug "ConstraintException when filling a datatable" (MySql bug #65065).
    /// </summary>
    [Fact]
    public void ConstraintExceptionOnLoad()
    {
      using (var con = Fixture.GetConnection())
      {
        MySqlCommand cmd = new MySqlCommand();

        cmd.Connection = con;

        cmd.CommandText = "DROP TABLE IF EXISTS trx";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS camn";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE `camn` (" +
          "`id_camn` int(10) unsigned NOT NULL AUTO_INCREMENT," +
          "`no` int(4) unsigned NOT NULL," +
          "`marq` varchar(45) COLLATE utf8_bin DEFAULT NULL," +
          "`modl` varchar(45) COLLATE utf8_bin DEFAULT NULL," +
          "`no_serie` varchar(17) COLLATE utf8_bin DEFAULT NULL," +
          "`no_plaq` varchar(7) COLLATE utf8_bin DEFAULT NULL," +
          "PRIMARY KEY (`id_camn`)," +
          "UNIQUE KEY `id_camn_UNIQUE` (`id_camn`)," +
          "UNIQUE KEY `no_UNIQUE` (`no`)," +
          "UNIQUE KEY `no_serie_UNIQUE` (`no_serie`)," +
          "UNIQUE KEY `no_plaq_UNIQUE` (`no_plaq`)" +
          ") ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8 COLLATE=utf8_bin";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE `trx` (" +
          "`id_trx` int(10) unsigned NOT NULL AUTO_INCREMENT," +
          "`mnt` decimal(9,2) NOT NULL," +
          "`dat_trx` date NOT NULL," +
          "`typ_trx` varchar(45) COLLATE utf8_bin DEFAULT NULL," +
          "`descr` tinytext COLLATE utf8_bin," +
          "`id_camn` int(10) unsigned DEFAULT NULL," +
          "PRIMARY KEY (`id_trx`)," +
          "UNIQUE KEY `id_trx_UNIQUE` (`id_trx`)," +
          "KEY `fk_trx_camn` (`id_camn`)," +
          "CONSTRAINT `fk_trx_camn` FOREIGN KEY (`id_camn`) REFERENCES `camn` (`id_camn`) ON DELETE NO ACTION ON UPDATE NO ACTION" +
          ") ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8 COLLATE=utf8_bin";

        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO camn(id_camn, no, marq, modl, no_serie, no_plaq) VALUES(9, 3327, null, null, null, null);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO trx(id_trx, mnt, dat_trx, typ_trx, descr, id_camn) VALUES(1, 10, '2012-04-30', null, null, 9);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO trx(id_trx, mnt, dat_trx, typ_trx, descr, id_camn) VALUES(2, 10, '2012-04-15', null, null, 9);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO trx(id_trx, mnt, dat_trx, typ_trx, descr, id_camn) VALUES(3, 10, '2012-04-15', null, null, null);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT cam.no_serie, t.mnt FROM trx t LEFT JOIN camn cam USING(id_camn) ";
        MySqlDataReader dr = cmd.ExecuteReader();
        DataTable dataTable = new DataTable();
        DataSet ds = new DataSet();
        dataTable.Load(dr);
        dr.Close();
      }
    }
    
  }
}
