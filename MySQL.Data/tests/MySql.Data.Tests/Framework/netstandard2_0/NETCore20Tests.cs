// Copyright © 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class NETCore20Tests : TestBase
  {
    public NETCore20Tests(TestFixture fixture) : base(fixture) { }

    private void CreateTables()
    {
      executeSQL("CREATE TABLE parent (id INT, name VARCHAR(20), PRIMARY KEY (id))");
      executeSQL("CREATE TABLE child (id INT, description VARCHAR(20), parent_id INT, PRIMARY KEY (id))");
      executeSQL("INSERT INTO parent VALUES (1, 'parent1')");
      executeSQL("INSERT INTO parent VALUES (2, 'parent2')");
      executeSQL("INSERT INTO child VALUES (1, 'child1', 1)");
      executeSQL("INSERT INTO child VALUES (2, 'child2', 2)");
      executeSQL("INSERT INTO child VALUES (3, 'child3', 2)");
    }

    [Fact]
    public void ConstraintsTest()
    {
      executeSQL("DROP TABLE IF EXISTS parent");
      executeSQL("DROP TABLE IF EXISTS child");
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

      Assert.Single(ds.Tables[0].Constraints);
      Assert.IsType<UniqueConstraint>(ds.Tables[0].Constraints[0]);

      Assert.Equal(2, ds.Tables[1].Constraints.Count);
      Assert.IsType<UniqueConstraint>(ds.Tables[0].Constraints[0]);
      Assert.IsType<ForeignKeyConstraint>(ds.Tables[1].Constraints[1]);
    }

    [Fact]
    public void DataTableReaderTest()
    {
      executeSQL("DROP TABLE IF EXISTS parent");
      executeSQL("DROP TABLE IF EXISTS child");
      CreateTables();

      MySqlDataAdapter childDa = new MySqlDataAdapter("SELECT * FROM child", Connection);
      DataTable childDt = new DataTable();
      childDa.FillSchema(childDt, SchemaType.Source);
      childDa.Fill(childDt);
      childDt.Columns["id"].Unique = true;

      using (DataTableReader dataTableReader = new DataTableReader(childDt))
      {
        Assert.True(dataTableReader.HasRows);
        Assert.Equal(3, dataTableReader.FieldCount);
      }

      PropertyCollection propertyCollection = childDt.ExtendedProperties;
      propertyCollection.Add("TimeStamp", DateTime.Now);
      propertyCollection.Add("Version", 1);

      Assert.NotEmpty(childDt.ExtendedProperties);
      Assert.Equal(2, childDt.ExtendedProperties.Count);
    }

    [Fact]
    public void DataViewSettingsTest()
    {
      executeSQL("DROP TABLE IF EXISTS parent");
      executeSQL("DROP TABLE IF EXISTS child");
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
      Assert.Equal("name", dataViewManager.DataViewSettings[parentDt].Sort);
      Assert.NotEmpty(dataViewManager.DataViewSettings);
    }

    [Fact]
    public void SchemaColumnTest()
    {
      executeSQL("DROP TABLE IF EXISTS parent");
      executeSQL("DROP TABLE IF EXISTS child");
      CreateTables();

      MySqlDataAdapter parentDa = new MySqlDataAdapter("SELECT * FROM parent", Connection);
      DataTable parentDt = new DataTable();
      parentDa.FillSchema(parentDt, SchemaType.Source);
      parentDa.Fill(parentDt);
      parentDt.Columns["id"].Unique = true;

      Assert.True(parentDt.Columns[0].Unique);
      Assert.False(parentDt.Columns[0].AllowDBNull);
      Assert.Equal("name", parentDt.Columns[1].ColumnName);

      Assert.Equal(0, parentDt.Columns[0].AutoIncrementSeed);
      Assert.False(parentDt.Columns[0].ReadOnly);
      Assert.False(parentDt.Columns[1].AutoIncrement);
    }
  }
}
