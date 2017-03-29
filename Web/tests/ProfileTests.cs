// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;

using System.Text;
using Xunit;
using MySql.Web.Profile;
using System.Collections.Specialized;
using System.Reflection;
using System.Configuration;
using System.Data;
using System.Web.Profile;
using MySql.Data.MySqlClient;

namespace MySql.Web.Tests
{
  public class ProfileTests : IUseFixture<SetUpWeb>, IDisposable
  {
    private SetUpWeb st;

    public void SetFixture(SetUpWeb data)
    {
      st = data;
      st.rootConn.Close();
      st.rootConn = new MySqlConnection("server=localhost;userid=root;pwd=;database=" + st.conn.Database + ";port=" + st.port);
      st.rootConn.Open();
    }

    public void Dispose()
    {
      st.ExecuteSQLAsRoot("Delete from my_aspnet_profiles");
      st.ExecuteSQLAsRoot("Delete from my_aspnet_users");      
      st.ExecuteSQLAsRoot("Delete from my_aspnet_applications");
    }

    private MySQLProfileProvider InitProfileProvider()
    {
      MySQLProfileProvider p = new MySQLProfileProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      p.Initialize(null, config);
      return p;
    }

    [Fact]
    public void SettingValuesCreatesAnAppAndUserId()
    {
      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", false);
      ctx.Add("UserName", "user1");

      SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
      SettingsProperty property1 = new SettingsProperty("color");
      property1.PropertyType = typeof(string);
      property1.Attributes["AllowAnonymous"] = true;
      SettingsPropertyValue value = new SettingsPropertyValue(property1);
      value.PropertyValue = "blue";
      values.Add(value);

      provider.SetPropertyValues(ctx, values);

      DataTable dt = st.FillTable("SELECT * FROM my_aspnet_applications");
      Assert.True(1 == dt.Rows.Count, "Rows count on table my_aspnet_applications is not 1");      
      
      dt = st.FillTable("SELECT * FROM my_aspnet_users");
      Assert.True(1 == dt.Rows.Count, "Rows count on table my_aspnet_users is not 1");            


      dt = st.FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.True(1 == dt.Rows.Count, "Rows count on table my_aspnet_profiles is not 1");                       

      values["color"].PropertyValue = "green";
      provider.SetPropertyValues(ctx, values);

      dt = st.FillTable("SELECT * FROM my_aspnet_applications");
      Assert.True(1 == dt.Rows.Count, "Rows count on table my_aspnet_applications is not 1 after setting property");      

      dt = st.FillTable("SELECT * FROM my_aspnet_users");
      Assert.True(1 == dt.Rows.Count, "Rows count on table my_aspnet_users is not 1 after setting property");            

      dt = st.FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.True(1 == dt.Rows.Count, "Rows count on table my_aspnet_profiles is not 1 after setting property");                       
    }

    [Fact]
    public void AnonymousUserSettingNonAnonymousProperties()
    {
      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", false);
      ctx.Add("UserName", "user1");

      SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
      SettingsProperty property1 = new SettingsProperty("color");
      property1.PropertyType = typeof(string);
      property1.Attributes["AllowAnonymous"] = false;
      SettingsPropertyValue value = new SettingsPropertyValue(property1);
      value.PropertyValue = "blue";
      values.Add(value);

      provider.SetPropertyValues(ctx, values);

      DataTable dt = st.FillTable("SELECT * FROM my_aspnet_applications");      
      Assert.True(0 == dt.Rows.Count, "Table my_aspnet_applications Rows is not 0");

      dt = st.FillTable("SELECT * FROM my_aspnet_users");      
      Assert.True(0 == dt.Rows.Count, "Table my_aspnet_users Rows is not 0");

      dt = st.FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.True(0 == dt.Rows.Count, "Table my_aspnet_profiles Rows is not 0");
      
    }

    [Fact]
    public void StringCollectionAsProperty()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      StringCollection colors = new StringCollection();
      colors.Add("red");
      colors.Add("green");
      colors.Add("blue");
      profile["FavoriteColors"] = colors;
      profile.Save();

      DataTable dt = st.FillTable("SELECT * FROM my_aspnet_applications");
      Assert.Equal(1, dt.Rows.Count);
      dt = st.FillTable("SELECT * FROM my_aspnet_users");
      Assert.Equal(1, dt.Rows.Count);
      dt = st.FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.Equal(1, dt.Rows.Count);

      // now retrieve them
      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("FavoriteColors");
      getProp1.PropertyType = typeof(StringCollection);
      getProp1.SerializeAs = SettingsSerializeAs.Xml;
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");
      SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
      Assert.Equal(1, getValues.Count);
      SettingsPropertyValue getValue1 = getValues["FavoriteColors"];
      StringCollection outValue = (StringCollection)getValue1.PropertyValue;
      Assert.Equal(3, outValue.Count);
      Assert.Equal("red", outValue[0]);
      Assert.Equal("green", outValue[1]);
      Assert.Equal("blue", outValue[2]);
    }

    [Fact]
    public void AuthenticatedDateTime()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      DateTime date = DateTime.Now;
      profile["BirthDate"] = date;
      profile.Save();

      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("BirthDate");
      getProp1.PropertyType = typeof(DateTime);
      getProp1.SerializeAs = SettingsSerializeAs.Xml;
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");

      SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
      Assert.Equal(1, getValues.Count);
      SettingsPropertyValue getValue1 = getValues["BirthDate"];
      Assert.Equal(date, getValue1.PropertyValue);
    }

    /// <summary>
    /// We have to manually reset the app id because our profile provider is loaded from
    /// previous tests but we are destroying our database between tests.  This means that 
    /// our provider thinks we have an application in our database when we really don't.
    /// Doing this will force the provider to generate a new app id.
    /// Note that this is not really a problem in a normal app that is not destroying
    /// the database behind the back of the provider.
    /// </summary>
    /// <param name="p"></param>
    private void ResetAppId(MySQLProfileProvider p)
    {
      Type t = p.GetType();
      FieldInfo fi = t.GetField("app",
          BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.GetField);
      object appObject = fi.GetValue(p);
      Type appType = appObject.GetType();
      PropertyInfo pi = appType.GetProperty("Id");
      pi.SetValue(appObject, -1, null);
    }

    [Fact]
    public void AuthenticatedStringProperty()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      profile["Name"] = "Fred Flintstone";
      profile.Save();

      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("Name");
      getProp1.PropertyType = typeof(String);
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");

      SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
      Assert.Equal(1, getValues.Count);
      SettingsPropertyValue getValue1 = getValues["Name"];
      Assert.Equal("Fred Flintstone", getValue1.PropertyValue);
    }

    /// <summary>
    /// Bug #41654	FindProfilesByUserName error into Connector .NET
    /// </summary>
    [Fact]
    public void GetAllProfiles()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      profile["Name"] = "Fred Flintstone";
      profile.Save();

      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("Name");
      getProp1.PropertyType = typeof(String);
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");

      int total;
      ProfileInfoCollection profiles = provider.GetAllProfiles(
          ProfileAuthenticationOption.All, 0, 10, out total);
      Assert.Equal(1, total);
    }

    /// <summary>
    /// Tests deleting a user profile
    /// </summary>
    [Fact]
    public void DeleteProfiles()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      profile.SetPropertyValue("Name", "this is my name");
      profile.Save();
      profile = ProfileBase.Create("foo", true); // refresh profile from database
      Assert.Equal("this is my name", profile.GetPropertyValue("Name"));

      Assert.Equal(1, ProfileManager.DeleteProfiles(new string[] { "foo" }));
      profile = ProfileBase.Create("foo", true); // refresh profile from database
      Assert.Equal(string.Empty, profile.GetPropertyValue("Name"));
    }

  }
}
