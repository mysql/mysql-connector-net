// Copyright © 2026, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  /// <summary>
  /// MySQL refuses LIMIT in an IN subquery ("This version of MySQL doesn't yet support
  /// 'LIMIT &amp; IN/ALL/ANY/SOME subquery'", error 1235); Contains over a paged subquery must still run.
  /// </summary>
  public class LimitInSubqueryTests
  {
    public class Shelf
    {
      public int Id { get; set; }
      public string Name { get; set; } = "";
      public List<Volume> Volumes { get; set; } = new List<Volume>();
    }

    public class Volume
    {
      public int Id { get; set; }
      public int ShelfId { get; set; }
      public string Title { get; set; } = "";
    }

    public class LimitInSubqueryContext : MyTestContext
    {
      public DbSet<Shelf> Shelves => Set<Shelf>();
      public DbSet<Volume> Volumes => Set<Volume>();
    }

    private List<Shelf> _shelves = new List<Shelf>();
    private List<Volume> _volumes = new List<Volume>();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      using var context = new LimitInSubqueryContext();
      context.Database.EnsureDeleted();
      context.Database.EnsureCreated();
      foreach (var name in new[] { "a", "b", "c" })
      {
        var shelf = new Shelf { Name = name };
        shelf.Volumes.Add(new Volume { Title = name + "1" });
        shelf.Volumes.Add(new Volume { Title = name + "2" });
        context.Add(shelf);
      }

      context.SaveChanges();
      _shelves = context.Shelves.AsNoTracking().ToList();
      _volumes = context.Volumes.AsNoTracking().ToList();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using var context = new LimitInSubqueryContext();
      context.Database.EnsureDeleted();
    }

    [Test]
    public void ContainsOverTakeSubquery()
    {
      using var context = new LimitInSubqueryContext();

      var query = context.Volumes
        .Where(v => context.Shelves.OrderBy(s => s.Id).Take(1).Select(s => s.Id).Contains(v.ShelfId))
        .OrderBy(v => v.Title).Select(v => v.Title);
      var titles = query.ToList();

      // The subquery is still paged on the server, inside a derived table.
      Assert.That(query.ToQueryString(), Does.Contain("LIMIT").And.Contain(") AS `t`"));

      var firstShelf = _shelves.OrderBy(s => s.Id).First().Id;
      Assert.That(titles, Is.EqualTo(_volumes.Where(v => v.ShelfId == firstShelf).OrderBy(v => v.Title).Select(v => v.Title)));
    }

    [Test]
    public void MultipleContainsOverTakeSubqueriesWithOr()
    {
      using var context = new LimitInSubqueryContext();

      var count = context.Volumes.Count(v =>
        context.Shelves.OrderBy(s => s.Id).Take(1).Select(s => s.Id).Contains(v.ShelfId)
        || context.Volumes.OrderByDescending(o => o.Id).Take(1).Select(o => o.Id).Contains(v.Id));

      var firstShelf = _shelves.OrderBy(s => s.Id).First().Id;
      var lastVolume = _volumes.Max(v => v.Id);
      Assert.That(count, Is.EqualTo(_volumes.Count(v => v.ShelfId == firstShelf || v.Id == lastVolume)));
    }

    [Test]
    public void NegatedContainsOverSkipTakeSubquery()
    {
      using var context = new LimitInSubqueryContext();

      var titles = context.Volumes
        .Where(v => !context.Shelves.OrderBy(s => s.Id).Skip(1).Take(1).Select(s => s.Id).Contains(v.ShelfId))
        .OrderBy(v => v.Title).Select(v => v.Title).ToList();

      var secondShelf = _shelves.OrderBy(s => s.Id).Skip(1).First().Id;
      Assert.That(titles, Is.EqualTo(_volumes.Where(v => v.ShelfId != secondShelf).OrderBy(v => v.Title).Select(v => v.Title)));
    }

    [Test]
    public void ContainsOverSkipOnlySubquery()
    {
      using var context = new LimitInSubqueryContext();

      var count = context.Volumes
        .Count(v => context.Shelves.OrderBy(s => s.Id).Skip(2).Select(s => s.Id).Contains(v.ShelfId));

      var thirdShelf = _shelves.OrderBy(s => s.Id).Skip(2).First().Id;
      Assert.That(count, Is.EqualTo(_volumes.Count(v => v.ShelfId == thirdShelf)));
    }

    [Test]
    public void ContainsOverCorrelatedTakeSubquery()
    {
      using var context = new LimitInSubqueryContext();

      // For each volume, the first other volume of its shelf by Id: the shelf's first volume
      // is in that list only when it isn't itself.
      var first = _volumes.Min(m => m.Id);
      var titles = context.Volumes
        .Where(v => context.Volumes.Where(o => o.ShelfId == v.ShelfId && o.Id != v.Id)
          .OrderBy(o => o.Id).Take(1).Select(o => o.Id).Contains(first))
        .OrderBy(v => v.Title).Select(v => v.Title).ToList();

      var expected = _volumes
        .Where(v => _volumes.Where(o => o.ShelfId == v.ShelfId && o.Id != v.Id).OrderBy(o => o.Id).Take(1).Select(o => o.Id).Contains(first))
        .OrderBy(v => v.Title).Select(v => v.Title);
      Assert.That(titles, Is.EqualTo(expected));
      Assert.That(titles, Is.Not.Empty);
    }

    [Test]
    public void ContainsOverUnpagedSubqueryIsUnchanged()
    {
      using var context = new LimitInSubqueryContext();

      var query = context.Volumes.Where(v => context.Shelves.Where(s => s.Name != "b").Select(s => s.Id).Contains(v.ShelfId));

      Assert.That(query.ToQueryString(), Does.Not.Contain("SELECT *"));
      Assert.That(query.Count(), Is.EqualTo(4));
    }
  }
}
