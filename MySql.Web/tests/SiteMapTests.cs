// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Hosting;
using Xunit;
using MySql.Data.MySqlClient;
using MySql.Web.SiteMap;

namespace MySql.Web.Tests
{
  public class SiteMapTests : WebTestBase
  {
    private void PopulateSiteMapTable()
    {
      string sql = @"
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 1, 'Index', 'The Index page', '~/Index.aspx', null, null );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 2, 'Chess Openings', 'Collection of Chess openings articles', '~/Openings.aspx', null, 1 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 3, 'King''s Gambit', 'The hyper sharp King''s Gambit', '~/Openings/KingsGambit.aspx', null, 2 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 4, 'Ruy Lopez', 'The spanish opening', '~/RuyLopez.aspx', null, 2 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 5, 'Evan''s Gambit', 'The Funny Italian Game', '~/EvansGambit.aspx', null, 2 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 6, 'Sicilian Defense', 'Sharp Double Edge Defense', '~/Sicilian.aspx', null, 2 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 7, 'Middle Game', 'Middle Game Topics', '~/MiddleGame.aspx', null, 1 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 8, 'Isolated Queen Pawn', 'Isolani Typical Positions', '~/Isolani.aspx', null, 7 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentId ) values ( 9, 'Rook vs Two Minor pieces', 'Rook vs Two Minor Pieces', '~/RookVsTwoMinor.aspx', null, 7 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentID ) values (10, 'Exchange Sacrifice', 'Sacrifice of Rook per Bishop or Knight', '~/ExchangeSacrifice.aspx', null, 7 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentID ) values (11, 'Nd5 Sacrifice in Sicilian', 'Sacrifice Nc3-Nd5 against Schevening like structures', '~/Nd5SacSicilian.aspx', null, 7 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentID ) values (12, 'Endings', 'Theory of chess endings & practical endings', '~/Endings.aspx', null, 1 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentID ) values (13, 'Rook Endings', 'Rook Endings', '~/RookEndigs.aspx', null, 12 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentID ) values (14, 'Queen vs Rook', 'Queen vs Rook, pawnless endings', '~/QueenVsRook.aspx ', null, 12 );
insert into my_aspnet_sitemap( Id, Title, Description, Url, Roles, ParentID ) values (15, 'Isolated Queen Pawn Ending', 'Endings with queen pawn isolated', '~/IQPending.aspx', null, 12 );
";
      MySqlScript script = new MySqlScript(Connection, sql);
      script.Execute();
    }

    [Fact]
    public void TestBuildSiteMap()
    {
      PopulateSiteMapTable();

      MySqlSiteMapProvider prov = new MySqlSiteMapProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      config.Add("enableExpireCallback", "false");

      prov.Initialize("SiteMapTests", config);
      prov.BuildSiteMap();
      SiteMapNode node = prov.FindSiteMapNodeFromKey("5");
      SimpleWorkerRequest req = new SimpleWorkerRequest("/dummy", Environment.CurrentDirectory, "default.aspx", null, new StringWriter());
      HttpContext.Current = new HttpContext(req);

      Assert.Equal(node.Title, "Evan's Gambit");
      SiteMapNode nodep = prov.GetParentNode(node);
      Assert.Equal(node.Description, "The Funny Italian Game");
      Assert.False(node.HasChildNodes);
      SiteMapNode node2 = node.NextSibling;
      Assert.NotNull(node2);
      Assert.Equal(node2.Title, "Sicilian Defense");
      Assert.Equal(node2.Description, "Sharp Double Edge Defense");

      node = node.PreviousSibling;
      Assert.NotNull(node);
      Assert.Equal(node.Title, "Ruy Lopez");
      Assert.Equal(node.Description, "The spanish opening");
      Assert.False(node.HasChildNodes);
      Assert.NotNull(node.NextSibling);

      node = node.ParentNode;
      Assert.Equal(node.Title, "Chess Openings");

      node = node.ParentNode;
      Assert.Equal(node.Title, "Index");

      node = node.ParentNode;
      Assert.Null(node);

      node = prov.RootNode;
      Assert.Equal(node.Title, "Index");
      string[] childData = new string[] { "Chess Openings", "Middle Game", "Endings" };

      for (int i = 0; i < node.ChildNodes.Count; i++)
      {
        SiteMapNode child = node.ChildNodes[i];
        Assert.Equal(child.Title, childData[i]);
      }
    }
  }
}
