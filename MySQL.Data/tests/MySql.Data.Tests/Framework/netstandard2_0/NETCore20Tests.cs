// Copyright © 2018, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using System.Data;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  public class NETCore20Tests : TestBase
  {
    private void CreateTables()
    {
      ExecuteSQL("CREATE TABLE parent (id INT, name VARCHAR(20), PRIMARY KEY (id))");
      ExecuteSQL("CREATE TABLE child (id INT, description VARCHAR(20), parent_id INT, PRIMARY KEY (id))");
      ExecuteSQL("INSERT INTO parent VALUES (1, 'parent1')");
      ExecuteSQL("INSERT INTO parent VALUES (2, 'parent2')");
      ExecuteSQL("INSERT INTO child VALUES (1, 'child1', 1)");
      ExecuteSQL("INSERT INTO child VALUES (2, 'child2', 2)");
      ExecuteSQL("INSERT INTO child VALUES (3, 'child3', 2)");
    }

    [Test]
    public void ConstraintsTest()
    {
      ExecuteSQL("DROP TABLE IF EXISTS parent");
      ExecuteSQL("DROP TABLE IF EXISTS child");
      CreateTables();

      DataSet ds = new DataSet();

      MySqlDataAdapter parentDa = new MySqlDataAdapter("SELECT * FROM parent", Connection);
      DataTable parentDt = new DataTable();
      parentDa.FillSchema(parentDt, SchemaType.Source);
      parentDa.Fill(parentDt);
      parentDt.Columns["id"].Unique = true;

      MySqlDataAdapter childDa = new MySqlDataAdapter("SELECT * FROM child", Connection);
      DataTable childDt = new DataTable();
      childDa.FillSchema(childDt, SchemaType.Source);
      childDa.Fill(childDt);
      childDt.Columns["id"].Unique = true;

      ds.Tables.Add(parentDt);
      ds.Tables.Add(childDt);

      DataRelation dataRelation = new DataRelation("parentChild", parentDt.Columns["id"], childDt.Columns["parent_id"]);
      ds.Relations.Add(dataRelation);

      Assert.That(ds.Tables[0].Constraints, Has.One.Items);
      Assert.IsInstanceOf<UniqueConstraint>(ds.Tables[0].Constraints[0]);

      Assert.AreEqual(2, ds.Tables[1].Constraints.Count);
      Assert.IsInstanceOf<UniqueConstraint>(ds.Tables[0].Constraints[0]);
      Assert.IsInstanceOf<ForeignKeyConstraint>(ds.Tables[1].Constraints[1]);
    }

    [Test]
    public void DataTableReaderTest()
    {
      ExecuteSQL("DROP TABLE IF EXISTS parent");
      ExecuteSQL("DROP TABLE IF EXISTS child");
      CreateTables();

      MySqlDataAdapter childDa = new MySqlDataAdapter("SELECT * FROM child", Connection);
      DataTable childDt = new DataTable();
      childDa.FillSchema(childDt, SchemaType.Source);
      childDa.Fill(childDt);
      childDt.Columns["id"].Unique = true;

      using (DataTableReader dataTableReader = new DataTableReader(childDt))
      {
        Assert.True(dataTableReader.HasRows);
        Assert.AreEqual(3, dataTableReader.FieldCount);
      }

      PropertyCollection propertyCollection = childDt.ExtendedProperties;
      propertyCollection.Add("TimeStamp", DateTime.Now);
      propertyCollection.Add("Version", 1);

      CollectionAssert.IsNotEmpty(childDt.ExtendedProperties);
      Assert.AreEqual(2, childDt.ExtendedProperties.Count);
    }

    [Test]
    public void DataViewSettingsTest()
    {
      ExecuteSQL("DROP TABLE IF EXISTS parent");
      ExecuteSQL("DROP TABLE IF EXISTS child");
      CreateTables();

      DataSet ds = new DataSet();

      MySqlDataAdapter parentDa = new MySqlDataAdapter("SELECT * FROM parent", Connection);
      DataTable parentDt = new DataTable();
      parentDa.FillSchema(parentDt, SchemaType.Source);
      parentDa.Fill(parentDt);
      parentDt.Columns["id"].Unique = true;

      MySqlDataAdapter childDa = new MySqlDataAdapter("SELECT * FROM child", Connection);
      DataTable childDt = new DataTable();
      childDa.FillSchema(childDt, SchemaType.Source);
      childDa.Fill(childDt);
      childDt.Columns["id"].Unique = true;

      ds.Tables.Add(parentDt);
      ds.Tables.Add(childDt);

      DataViewManager dataViewManager = new DataViewManager(ds);

      foreach (DataViewSetting viewSetting in dataViewManager.DataViewSettings)
        viewSetting.ApplyDefaultSort = true;

      dataViewManager.DataViewSettings[parentDt].Sort = "name";

      Assert.True(dataViewManager.DataViewSettings[childDt].Sort == "");
      Assert.AreEqual("name", dataViewManager.DataViewSettings[parentDt].Sort);
      CollectionAssert.IsNotEmpty(dataViewManager.DataViewSettings);
    }

    [Test]
    public void SchemaColumnTest()
    {
      ExecuteSQL("DROP TABLE IF EXISTS parent");
      ExecuteSQL("DROP TABLE IF EXISTS child");
      CreateTables();

      MySqlDataAdapter parentDa = new MySqlDataAdapter("SELECT * FROM parent", Connection);
      DataTable parentDt = new DataTable();
      parentDa.FillSchema(parentDt, SchemaType.Source);
      parentDa.Fill(parentDt);
      parentDt.Columns["id"].Unique = true;

      Assert.True(parentDt.Columns[0].Unique);
      Assert.False(parentDt.Columns[0].AllowDBNull);
      Assert.AreEqual("name", parentDt.Columns[1].ColumnName);

      Assert.AreEqual(0, parentDt.Columns[0].AutoIncrementSeed);
      Assert.False(parentDt.Columns[0].ReadOnly);
      Assert.False(parentDt.Columns[1].AutoIncrement);
    }
  }
}