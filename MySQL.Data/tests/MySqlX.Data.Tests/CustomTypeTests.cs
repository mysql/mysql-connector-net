// Copyright (c) 2023, Oracle and/or its affiliates.
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

using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  internal class CustomTypeTests : BaseTest
  {
    Schema Schema;
    Collection<CustomType> Collection;
    CustomType[] Data { get; set; }

    public class CustomType
    {
      public int _id { get; set; }
      public string Name { get; set; }
      public Dictionary<string, Core> DictData { get; set; }
      public DateTime Date { get; set; }
    }

    public class Core
    {
      public int idMeta { get; set; }
      public string[] Meta { get; set; }
    }

    private void InitCollection()
    {
      var data = new CustomType[20];

      for (int i = 0; i < 20; i++)
      {
        Core core = new()
        {
          idMeta = i,
          Meta = new string[] { $"Core1_{i}", $"Core2_{i}", $"Core3_{i}" }
        };

        Dictionary<string, Core> dictData = new()
        {
          { $"DictData1_{i}", core },
          { $"DictData2_{i}", core }
        };

        data[i] = new() { _id = i, Name = $"Name_{i}", DictData = dictData, Date = DateTime.Now };
      }

      Data = data;

      Session session = GetSession();
      Schema = session.GetSchema(schemaName);
      CreateCollection("test");
      Collection = Schema.GetCollection<CustomType>("test");
      Collection.Add(Data).Execute();

      var count = session.SQL("SELECT COUNT(*) FROM test.test").Execute().FetchOne()[0];
      Assert.AreEqual(count, Collection.Count());
    }

    [Test]
    public void Find()
    {
      InitCollection();

      var result = Collection.Find("_id = :id").Bind("id", 1).Execute().FetchOne();
      Assert.NotNull(result);
      Assert.True(typeof(CustomType).Equals(result.GetType()));
      Assert.AreEqual(result.DictData["DictData1_1"].Meta, Data[1].DictData["DictData1_1"].Meta);
    }

    [Test]
    public void RemoveOne()
    {
      InitCollection();

      var removeStmt = Collection.RemoveOne(1);
      Assert.AreEqual(1, removeStmt.AffectedItemsCount);
      Assert.AreEqual(19, Collection.Count());
    }

    [TestCase("_id = :id","id",3)]
    [TestCase("Name = :name","name", "Name_3")]
    public void Remove(string condition, string bind,object value)
    {
      InitCollection();

      var removeStmt = Collection.Remove(condition).Bind(bind,value).Execute();
      Assert.AreEqual(1, removeStmt.AffectedItemsCount);
      Assert.AreEqual(19, Collection.Count());
    }

    [Test]
    public void Modify()
    {
      InitCollection();

      CustomType customTypeNew = new() { Date = DateTime.Now, Name = "NewDoc" };
      var modifyStmt = Collection.Modify("_id = :id").Bind("id", 7).Patch(customTypeNew).Execute();
      Assert.AreEqual(1, modifyStmt.AffectedItemsCount);
      Assert.AreEqual("NewDoc", Collection.GetOne(7).Name);
    }

    [Test]
    public void PrepareStatement()
    {
      InitCollection();

      var findStmt = Collection.Find("_id = :id and Name = :name").Bind("id", 15).Bind("name", "Name_15");
      var doc = findStmt.Execute();
      Assert.AreEqual("Name_15", doc.FetchOne().Name);
      Assert.False(findStmt._isPrepared);

      for (int i = 0; i < Data.Length; i++)
      {
        doc = findStmt.Bind("id", i).Bind("name", $"Name_{i}").Limit(1).Execute();
        Assert.AreEqual($"Name_{i}", doc.FetchOne().Name);
        Assert.True(findStmt._isPrepared || !findStmt.Session.SupportsPreparedStatements);
      }
    }

    [Test]
    public void InsertAsync()
    {
      Session session = GetSession();
      Schema = session.GetSchema(schemaName);
      CreateCollection("test");
      Collection = Schema.GetCollection<CustomType>("test");
      var data = new CustomType[20];
      List<Task<Result>> tasksList = new List<Task<Result>>();

      for (int i = 0; i < 20; i++)
      {
        Core core = new()
        {
          idMeta = i,
          Meta = new string[] { $"Core1_{i}", $"Core2_{i}", $"Core3_{i}" }
        };

        Dictionary<string, Core> dictData = new()
        {
          { $"DictData1_{i}", core },
          { $"DictData2_{i}", core }
        };

        data[i] = new() { _id = i, Name = $"Name_{i}", DictData = dictData, Date = DateTime.Now };

        tasksList.Add(Collection.Add(data[i]).ExecuteAsync());
      }

      Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(1));

      var count = session.SQL("SELECT COUNT(*) FROM test.test").Execute().FetchOne()[0];
      Assert.AreEqual(count, Collection.Count());
    }
  }
}
