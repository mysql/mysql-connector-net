// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.Entity.Tests.DbContextClasses
{
  public class Blog
  {
    public int BlogId { get; set; }
    public string Url { get; set; }

    //public List<Post> Posts { get; set; }

    public Post RecentPost { get; set; }
    public BlogMetadata Metadata { get; set; }

  }

  public class Post
  {
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public int BlogIdFK { get; set; }
    public Blog Blog { get; set; }
  }


  public class Read
  {
    public int ReadId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public List<ReadTag> ReadTags { get; set; }
  }

  public class Tag
  {
    public int TagId { get; set; }

    public List<ReadTag> ReadTags { get; set; }
  }

  public class ReadTag
  {
    public int ReadId { get; set; }
    public Read Read { get; set; }

    public int TagId { get; set; }
    public Tag Tag { get; set; }
  }


  public class AuditEntry
  {
    public int AuditEntryId { get; set; }
    public string Username { get; set; }
    public string Action { get; set; }
  }

  
  public class BlogMetadata
  {
    [Key]    
    public int BlogId { get; set; }
    public Blog Blog { get; set; }
    public DateTime LoadedFromDatabase { get; set; }
  }


  public class Employee
  {
    public int EmployeeId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }

    public DateTime Timestamp { get; set; }

  }


  public class Car
  {
    public int CarId { get; set; }
    [MaxLength(30)]
    public string LicensePlate { get; set; }
    [MaxLength(30)]
    public string State { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }

    public List<RecordOfSale> SaleHistory { get; set; }
  }


  public class RecordOfSale
  {
    public int RecordOfSaleId { get; set; }
    public DateTime DateSold { get; set; }
    public decimal Price { get; set; }
    public string CarState { get; set; }
    public string CarLicensePlate { get; set; }
    public Car Car { get; set; }
  }

  public class Person
  {
    public int PersonId { get; set; }

    [ConcurrencyCheck]
    public string SocialSecurityNumber { get; set; }
    public string Name { get; set; }
  }

}
