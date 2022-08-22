// Copyright (c) 2021, 2022, Oracle and/or its affiliates.
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

using NUnit.Framework;
using System;

namespace MySql.Data.MySqlClient.Tests
{
  public class AttributeTests : TestBase
  {
    [OneTimeSetUp]
    public void SetUp()
    {
      if (Version >= new Version(8, 0, 23))
      {
        try { ExecuteSQL(@"INSTALL COMPONENT 'file://component_query_attributes'"); }
        catch { }
      }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SetAttributesWithoutParams(bool prepare)
    {
      if (!Connection.driver.SupportsQueryAttributes) Assert.Ignore("MySQL Server version does not support query attributes.");

      using MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Attributes.SetAttribute("n1", "v1");
      cmd.CommandText = "SELECT mysql_query_attribute_string('n1')";
      if (prepare) cmd.Prepare();

      var result = cmd.ExecuteScalar();
      if (Version >= new Version(8, 0, 26) || !prepare) StringAssert.AreEqualIgnoringCase("v1", result.ToString());
      else Assert.IsEmpty(result.ToString());

      cmd.Attributes.SetAttribute("n2", 123);
      cmd.CommandText = "SELECT mysql_query_attribute_string('n2')";
      if (prepare) cmd.Prepare();
      result = cmd.ExecuteScalar();
      if (Version >= new Version(8, 0, 26) || !prepare) StringAssert.AreEqualIgnoringCase("123", result.ToString());
      else Assert.IsEmpty(result.ToString());

      MySqlAttribute attr = new MySqlAttribute();
      attr.AttributeName = "n3";
      attr.Value = "v3";
      cmd.Attributes.SetAttribute(attr);
      cmd.CommandText = "SELECT mysql_query_attribute_string('n3')";
      if (prepare) cmd.Prepare();
      result = cmd.ExecuteScalar();
      if (Version >= new Version(8, 0, 26) || !prepare) StringAssert.AreEqualIgnoringCase("v3", result.ToString());
      else Assert.IsEmpty(result.ToString());
    }

    [TestCase("StringType", "value1")]
    [TestCase("Int16Type", (Int16)1234)]
    [TestCase("Int32Type", (Int32)2546)]
    [TestCase("Int64Type", (Int64)98756)]
    [TestCase("FloatType", (float)5678)]
    [TestCase("DoubleType", 1234.567)]
    public void ValueTypes(string name, object value)
    {
      if (!Connection.driver.SupportsQueryAttributes) Assert.Ignore("MySQL Server version does not support query attributes.");

      using MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Attributes.SetAttribute(name, value);
      cmd.CommandText = $"SELECT mysql_query_attribute_string('{name}')";
      var result = cmd.ExecuteScalar();
      StringAssert.AreEqualIgnoringCase(value.ToString(), result.ToString());
    }

    [Test]
    public void TimeSpanValueType()
    {
      if (!Connection.driver.SupportsQueryAttributes) Assert.Ignore("MySQL Server version does not support query attributes.");

      TimeSpan time = new TimeSpan(01, 19, 25);

      using MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Attributes.SetAttribute("TimeSpan", time);
      cmd.CommandText = "SELECT mysql_query_attribute_string('TimeSpan')";
      var result = cmd.ExecuteScalar();
      StringAssert.StartsWith(time.ToString(), result.ToString());
    }

    [Test]
    public void DateTimeValueType()
    {
      if (!Connection.driver.SupportsQueryAttributes) Assert.Ignore("MySQL Server version does not support query attributes.");

      DateTime dateTime = DateTime.Now;

      using MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Attributes.SetAttribute("DateTime", dateTime);
      cmd.CommandText = "SELECT mysql_query_attribute_string('DateTime')";
      var result = cmd.ExecuteScalar();
      Assert.AreEqual(dateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), result.ToString());
    }

    [Test]
    public void ClearAttributes()
    {
      using MySqlCommand cmd = new MySqlCommand();
      cmd.Attributes.SetAttribute("foo", "bar");
      Assert.AreEqual(1, cmd.Attributes.Count);

      cmd.Attributes.SetAttribute("bar", "foo");
      Assert.AreEqual(2, cmd.Attributes.Count);

      cmd.Attributes.Clear();
      Assert.AreEqual(0, cmd.Attributes.Count);
    }

    [Test]
    public void SameNameAttribute()
    {
      if (!Connection.driver.SupportsQueryAttributes) Assert.Ignore("MySQL Server version does not support query attributes.");

      using MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Attributes.SetAttribute("foo", "bar");
      cmd.Attributes.SetAttribute("quaz", "fet");
      cmd.Attributes.SetAttribute("foo", "bar2");
      cmd.CommandText = "SELECT mysql_query_attribute_string('foo')";

      var result = cmd.ExecuteScalar();
      StringAssert.AreEqualIgnoringCase("bar", result.ToString());
    }

    [TestCase(true)]
    [TestCase(false)]
    public void QueryAttributesNotSupported(bool prepare)
    {
      if (Connection.driver.SupportsQueryAttributes) Assert.Ignore("Query attributes supported.");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = System.Diagnostics.SourceLevels.Warning;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);

      using var logConn = new MySqlConnection(Connection.ConnectionString);
      using MySqlCommand cmd = new MySqlCommand();
      logConn.Open();
      cmd.Connection = logConn;
      cmd.Attributes.SetAttribute("foo", "bar");
      cmd.Parameters.AddWithValue("", "test");
      cmd.CommandText = "SELECT ?";
      if (prepare) cmd.Prepare();

      var result = cmd.ExecuteScalar();
      StringAssert.AreEqualIgnoringCase("test", result.ToString());
      StringAssert.Contains(string.Format(Resources.QueryAttributesNotSupported, Version), listener.Strings[0]);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AttributesAndParameters(bool prepare)
    {
      if (!Connection.driver.SupportsQueryAttributes) Assert.Ignore("MySQL Server version does not support query attributes.");

      using MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Parameters.AddWithValue("", "Hello World");
      cmd.Parameters.AddWithValue("", "Goodbye World");
      cmd.Attributes.SetAttribute("foo", "bar");
      cmd.CommandText = "SELECT ?, ?, mysql_query_attribute_string('foo')";
      if (prepare) cmd.Prepare();

      using MySqlDataReader reader = cmd.ExecuteReader();
      {
        while (reader.Read())
        {
          StringAssert.AreEqualIgnoringCase("Hello World", reader.GetString(0));
          StringAssert.AreEqualIgnoringCase("Goodbye World", reader.GetString(1));
          if (Version >= new Version(8, 0, 26) || !prepare) StringAssert.AreEqualIgnoringCase("bar", reader.GetString(2));
        }
      }
    }

    [Test]
    public void InvalidValues()
    {
      string name;
      using MySqlCommand cmd = new MySqlCommand();
      Assert.Throws<ArgumentException>(() => cmd.Attributes.SetAttribute(string.Empty, "bar"));
      Assert.Throws<IndexOutOfRangeException>(() => name = cmd.Attributes[2].AttributeName);
    }

    /// <summary>
    /// Bug#33620022 [Parameter name overrides query attributes]
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public void ParameterOverridesAttributeValue(bool prepare)
    {
      if (!Connection.driver.SupportsQueryAttributes) Assert.Ignore("MySQL Server version does not support query attributes.");

      using var cmd = new MySqlCommand("select mysql_query_attribute_string('name') as attribute, mysql_query_attribute_string('name2') as attribute2, @name as parameter, @name2 as parameter2, mysql_query_attribute_string('attr') as attribute3", Connection);
      cmd.Attributes.SetAttribute("name", "attribute");
      cmd.Attributes.SetAttribute("name2", "attribute2");
      cmd.Parameters.AddWithValue("name", "parameter");
      cmd.Parameters.AddWithValue("name2", "parameter2");
      cmd.Attributes.SetAttribute("attr", "attribute3");

      if (prepare) cmd.Prepare();

      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        StringAssert.AreEqualIgnoringCase("attribute", reader.GetValue(0).ToString());
        StringAssert.AreEqualIgnoringCase("attribute2", reader.GetValue(1).ToString());
        StringAssert.AreEqualIgnoringCase("parameter", reader.GetValue(2).ToString());
        StringAssert.AreEqualIgnoringCase("parameter2", reader.GetValue(3).ToString());
        StringAssert.AreEqualIgnoringCase("attribute3", reader.GetValue(4).ToString());
      }
    }
  }
}
