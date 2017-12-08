// Copyright � 2014, 2017 Oracle and/or its affiliates. All rights reserved.
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


using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class JoinTests : IClassFixture<DefaultFixture>
  {
    private DefaultFixture st;

    public JoinTests(DefaultFixture fixture)
    {
      st = fixture;
      st.Setup(this.GetType());
    }

    [Fact]
    public void SimpleJoin()
    {
      using (DefaultContext ctx = st.GetDefaultContext())
      {
        var q = from b in ctx.Books
                join a in ctx.Authors
                on b.Author.Id equals a.Id
                select new
                {
                  bookId = b.Id,
                  bookName = b.Name,
                  authorName = a.Name
                };
        var expected = @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent2`.`Name` AS `Name1`
                        FROM `Books` AS `Extent1` INNER JOIN `Authors` AS `Extent2` ON `Extent1`.`Author_Id` = `Extent2`.`Id`";
        st.CheckSql(q.ToString(), expected);
      }
    }

    [Fact]
    public void SimpleJoinWithPredicate()
    {
      using (DefaultContext ctx = st.GetDefaultContext())
      {
        var q = from b in ctx.Books
                join a in ctx.Authors
                on b.Author.Id equals a.Id
                where b.Pages > 300
                select new
                {
                  bookId = b.Id,
                  bookName = b.Name,
                  authorName = a.Name
                };

        var expected = @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent2`.`Name` AS `Name1` FROM `Books` AS `Extent1` 
                        INNER JOIN `Authors` AS `Extent2` ON `Extent1`.`Author_Id` = `Extent2`.`Id`
                        WHERE `Extent1`.`Pages` > 300";
        st.CheckSql(q.ToString(), expected);
      }
    }

    [Fact]
    public void JoinOnRightSideAsDerivedTable()
    {
      using (DefaultContext ctx = st.GetDefaultContext())
      {
        var q = from b in ctx.Books
                join a in ctx.ContractAuthors
                on b.Author.Id equals a.Author.Id
                where b.Pages > 300
                select b;
        string sql = q.ToString();
        var expected = @"SELECT `Extent1`.`Id`, `Extent1`.`Name`, `Extent1`.`PubDate`, `Extent1`.`Pages`, `Extent1`.`Author_Id`
                        FROM `Books` AS `Extent1` INNER JOIN `ContractAuthors` AS `Extent2` ON (`Extent1`.`Author_Id` = 
                        `Extent2`.`Author_Id`) OR ((`Extent1`.`Author_Id` IS  NULL) AND (`Extent2`.`Author_Id` IS  NULL))
                        WHERE `Extent1`.`Pages` > 300";
        st.CheckSql(sql, expected);
      }
    }

    //    [Fact]
    //    public void JoinOfUnionsOnRightSideofJoin()
    //    {
    //      using (testEntities context = new testEntities())
    //      {
    //        string eSql = @"SELECT c.Id, c.Name, Union1.Id, Union1.Name, 
    //                                Union2.Id, Union2.Name FROM 
    //                                testEntities.Companies AS c JOIN (
    //                                ((SELECT t.Id, t.Name FROM testEntities.Toys as t) 
    //                                UNION ALL 
    //                                (SELECT s.Id, s.Name FROM testEntities.Shops as s)) AS Union1
    //                                JOIN 
    //                                ((SELECT a.Id, a.Name FROM testEntities.Authors AS a) 
    //                                UNION ALL 
    //                                (SELECT b.Id, b.Name FROM testEntities.Books AS b)) AS Union2
    //                                ON Union1.Id = Union2.Id) ON c.Id = Union1.Id";
    //        ObjectQuery<DbDataRecord> query = context.CreateQuery<DbDataRecord>(eSql);

    //        string[] data = new string[] { 
    //          "1,Hasbro,1,Slinky,1,Tom Clancy",
    //          "1,Hasbro,1,Target,1,Tom Clancy",
    //          "1,Hasbro,1,Slinky,1,Debt of Honor",
    //          "1,Hasbro,1,Target,1,Debt of Honor",
    //          "2,Acme,2,K-Mart,2,Insomnia",
    //          "2,Acme,2,Rubiks Cube,2,Stephen King",
    //          "2,Acme,2,K-Mart,2,Stephen King",
    //          "2,Acme,2,Rubiks Cube,2,Insomnia",
    //          "3,Bandai America,3,Wal-Mart,3,Rainmaker",
    //          "3,Bandai America,3,Lincoln Logs,3,John Grisham",
    //          "3,Bandai America,3,Wal-Mart,3,John Grisham",
    //          "3,Bandai America,3,Lincoln Logs,3,Rainmaker",
    //          "4,Lego Group,4,Legos,4,Hallo",
    //          "4,Lego Group,4,Legos,4,Dean Koontz" 
    //        };
    //        Dictionary<string, string> outData = new Dictionary<string, string>();
    //        string sql = query.ToTraceString();
    //        st.CheckSql(sql, SQLSyntax.JoinOfUnionsOnRightSideOfJoin);
    //        // check data integrity
    //        foreach (DbDataRecord record in query)
    //        {
    //          outData.Add(string.Format("{0},{1},{2},{3},{4},{5}", record.GetInt32( 0 ),
    //            record.GetString( 1 ), record.GetInt32( 2 ), record.GetString( 3 ), 
    //            record.GetInt32( 4 ), record.GetString( 5 )), null);
    //        }
    //        Assert.Equal(data.Length, outData.Count);
    //        for( int i = 0; i < data.Length; i++ )
    //        {
    //          Assert.True(outData.ContainsKey(data[i]));
    //        }
    //      }
    //    }

    //    [Fact]
    //    public void t()
    //    {
    //      using (testEntities context = new testEntities())
    //      {
    //        var q = from d in context.Computers
    //                join l in context.Computers on d.Id equals l.Id
    //                where (d is DesktopComputer)
    //                select new { d.Id, d.Brand };
    //        foreach (var o in q)
    //        {
    //        }
    //      }
    //    }

    //    /// <summary>
    //    /// Tests for bug http://bugs.mysql.com/bug.php?id=61729 
    //    /// (Skip/Take Clauses Causes Null Reference Exception in 6.3.7 and 6.4.1 Only).
    //    /// </summary>
    //    [Fact]
    //    public void JoinOfNestedUnionsWithLimit()
    //    {
    //      using (testEntities context = new testEntities())
    //      {
    //        var q = context.Books.Include("Author");
    //        q = q.Include("Publisher");
    //        q = q.Include("Publisher.Books");
    //        string sql = q.ToTraceString();

    //        var  i = 0;
    //        foreach (var o in q.Where(p => p.Id > 0).OrderBy(p => p.Name).ThenByDescending(p => p.Id).Skip(0).Take(32).ToList())
    //        {
    //           switch (i)
    //            {
    //             case 0:
    //               Assert.Equal(5, o.Id);
    //               Assert.Equal("Debt of Honor", o.Name);
    //             break;
    //             case 1:
    //               Assert.Equal(1, o.Id);
    //               Assert.Equal("Debt of Honor", o.Name);
    //             break;
    //             case 4:
    //               Assert.Equal(3, o.Id);
    //               Assert.Equal("Rainmaker", o.Name);
    //             break;             
    //            }
    //           i++;
    //        }
    //      }
    //    }

    //    [Fact]
    //    public void JoinOnRightSideNameClash()
    //    {
    //      using (testEntities context = new testEntities())
    //      {
    //        string eSql = @"SELECT c.Id, c.Name, a.Id, a.Name, b.Id, b.Name FROM
    //                                testEntities.Companies AS c JOIN (testEntities.Authors AS a
    //                                JOIN testEntities.Books AS b ON a.Id = b.Id) ON c.Id = a.Id";
    //        ObjectQuery<DbDataRecord> query = context.CreateQuery<DbDataRecord>(eSql);
    //        string sql = query.ToTraceString();
    //        st.CheckSql(sql, SQLSyntax.JoinOnRightSideNameClash);
    //        foreach (DbDataRecord record in query)
    //        {
    //          Assert.Equal(6, record.FieldCount);
    //        }
    //      }
    //    }

    //    /// <summary>
    //    /// Test for fix of Oracle bug 12807366.
    //    /// </summary>
    //    [Fact]
    //    public void JoinsAndConcatsWithComposedKeys()
    //    {
    //      using (testEntities1 ctx = new testEntities1())
    //      {
    //        IQueryable<gamingplatform> l2 = ctx.gamingplatform.Where(p => string.IsNullOrEmpty(p.Name)).Take(10);
    //        IQueryable<gamingplatform> l = ctx.gamingplatform.Where(p => string.IsNullOrEmpty(p.Name)).Take(10);
    //        var l4 = ctx.gamingplatform.Where(p => string.IsNullOrEmpty(p.Name)).Take(10);
    //        l = l.Concat(l4);
    //        l = l.Concat(ctx.gamingplatform.Where(p => string.IsNullOrEmpty(p.Name)).Take(10).Distinct());

    //        IQueryable<gamingplatform> q = (from i in l join i2 in l2 on i.Id equals i2.Id select i);
    //        IQueryable<videogameplatform> l3 = from t1 in q
    //                                           join t2 in q.SelectMany(p => p.videogameplatform)
    //                                               on t1.Id equals t2.IdGamingPlatform
    //                                           select t2;
    //        videogameplatform pu = null;

    //        pu = l3.FirstOrDefault();
    //      }
    //    }

    //    /// <summary>
    //    /// Test to fix Oracle bug 13491698
    //    /// </summary>
    //    [Fact]
    //    public void CanIncludeWithEagerLoading()
    //    {
    //      var myarray = new ArrayList();
    //      using (var db = new mybooksEntities())
    //      {
    //        var author = db.myauthors.Include("mybooks.myeditions").AsEnumerable().First();
    //        var strquery = ((ObjectQuery)db.myauthors.Include("mybooks.myeditions").AsEnumerable()).ToTraceString();
    //        st.CheckSql(strquery, SQLSyntax.JoinUsingInclude);
    //        foreach (var book in author.mybooks.ToList())
    //        {
    //          foreach (var edition in book.myeditions.ToList())
    //          {
    //            myarray.Add(edition.Title);
    //          }
    //        }
    //        myarray.Sort();
    //        Assert.Equal(0, myarray.IndexOf("Another Book First Edition"));
    //        Assert.Equal(1, myarray.IndexOf("Another Book Second Edition"));
    //        Assert.Equal(2, myarray.IndexOf("Another Book Third Edition"));
    //        Assert.Equal(3, myarray.IndexOf("Some Book First Edition"));
    //        Assert.Equal(myarray.Count, 4);
    //      }
    //    }

    //    /// <summary>
    //    /// Tests Fix for Error of "Every derived table must have an alias" in LINQ to Entities when using EF6 + DbFirst + View + Take  (MySql Bug #72148, Oracle bug #19356006).
    //    /// </summary>
    //    [Fact]
    //    public void TakeWithView()
    //    {
    //      using (testEntities1 ctx = new testEntities1())
    //      {        
    //        var q = ctx.vivideogametitle.Take(10);
    //        string sql = q.ToTraceString();
    //        st.CheckSql(sql, SQLSyntax.TakeWithView);
    //#if DEBUG
    //        Debug.WriteLine(sql);
    //#endif
    //        foreach( var row in q )
    //        {
    //          //
    //        }
    //      }
    //    }
  }
}