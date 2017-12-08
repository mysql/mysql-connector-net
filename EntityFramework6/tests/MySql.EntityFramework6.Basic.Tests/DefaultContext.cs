// Copyright (c) 2014, 2017 Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace MySql.Data.Entity.Tests
{
  public class Book
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime PubDate { get; set; }
    public Author Author { get; set;  }
    public int Pages { get; set;  }
  }

  public class Author
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set;  }
    public List<Book> Books { get; set; }
    public Address Address { get; set; }
  }

  public class Product
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set;  }
    public int MinAge { get; set; }
    public float Weight { get; set; }
    [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedDate { get; set;  }
  }

  public class Child
  {
    public string ChildId { get; set; }
    public string Name { get; set; }
    public Guid Label { get; set; }
    public TimeSpan BirthTime { get; set; }
  }

  public class ContractAuthor
  {
    public int Id { get; set;  }
    public Author Author { get; set;  }
    public DateTime StartDate { get; set;  }
  }

  [ComplexType]
  public class Address
  {
    public string City { get; set; }
    public string Street { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
  }

  public class Company
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime DateBegan { get; set; }
    public int NumEmployees { get; set; }
    public Address Address { get; set; }
  }


  public class DefaultContext : DbContext
  {
    public DefaultContext(string connStr) : base(connStr)
    {
      Database.SetInitializer<DefaultContext>(null);
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Product> Products { get; set;  }
    public DbSet<ContractAuthor> ContractAuthors { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Child> Children { get; set; }
  }
}
