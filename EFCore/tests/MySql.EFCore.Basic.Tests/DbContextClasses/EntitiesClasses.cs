// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.EntityFrameworkCore.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySql.Data.EntityFrameworkCore.Tests.DbContextClasses
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

    public string Url { get; set; }

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

  public class BodyShop
  {
    public int BodyShopId { get; set; }

    public string Name { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string Brand { get; set; }
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
    [MaxLength(30)]
    public string CarState { get; set; }
    [MaxLength(30)]
    public string CarLicensePlate { get; set; }
    public Car Car { get; set; }
  }

  public class Person
  {
    public int PersonId { get; set; }

    [ConcurrencyCheck]
    public string SocialSecurityNumber { get; set; }
    public string PhoneNumber { get; set; }
    [ConcurrencyCheck]
    public string Name { get; set; }
  }

    public class Guest
    {
        [Key]
        public int IdGuest { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        [Key]
        public int IdAddress { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public Guest Guest { get; set; }
    }

    public class Relative
    {
        [Key]
        public int IdRelative { get; set; }
        public string Name { get; set; }
        public AddressRelative Address { get; set; }
    }

    public class AddressRelative
    {
        [Key]
        public int IdAddressRelative { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public Relative Relative { get; set; }
    }


    public class QuickEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset Created { get; set; }

    }

  public class JsonData
  {
    [Key]
    [Column(TypeName = "smallint")]
    public int Id { get; set; }

    [Column(TypeName = "json")]
    public string jsoncol { get; set; }
  }

  public partial class AllDataTypes
  {
    public byte AddressNumber1 { get; set; }
    public short AddressNumber2 { get; set; }
    public int AddressNumber3 { get; set; }
    public int AddressNumber4 { get; set; }
    public long AddressNumber5 { get; set; }
    public float AddressNumber6 { get; set; }
    public float AddressNumber7 { get; set; }
    public double AddressNumber8 { get; set; }
    public decimal AddressNumber9 { get; set; }
    public short AddressNumber10 { get; set; }
    public string BuildingName1 { get; set; }
    public string BuildingName2 { get; set; }
    public string BuildingName3 { get; set; }
    public string BuildingName4 { get; set; }
    public string BuildingName5 { get; set; }
    public byte[] BuildingName6 { get; set; }
    public byte[] BuildingName7 { get; set; }
    public byte[] BuildingName8 { get; set; }
    public byte[] BuildingName9 { get; set; }
    public byte[] BuildingName10 { get; set; }
    public string BuildingName11 { get; set; }
    public string BuildingName12 { get; set; }
    public DateTime BuildingName13 { get; set; }
    public DateTime BuildingName14 { get; set; }
    public TimeSpan BuildingName15 { get; set; }
    public DateTime BuildingName16 { get; set; }
    public short BuildingName17 { get; set; }
  }

  [Table("CountryList")]
  public class Country
  {
    [Key]
    public string Code { get; set; }
    public string Name { get; set; }
    public virtual Continent Continent { get; set; }
    public string Region { get; set; }
    public int IndepYear { get; set; }
  }

  public class Continent
  {
    public string Code { get; set; }

    public string Name { get; set; }

    public virtual ICollection<Country> Countries { get; set; }
  }

  public class Triangle
  {
    public int Id { get; set; }

    public int Base { get; set; }

    public int Height { get; set; }

    public int Area { get; set; }
  }

  public class StringTypes
  {
    public string TinyString { get; set; }
    public string NormalString { get; set; }
    public string MediumString { get; set; }
    public string LongString { get; set; }
  }

  public class MyTest
  {
    public int MyTestId { get; set; }

    public DateTime Date { get; set; }
  }

  [MySqlCharset("ascii")]
  public class TestCharsetDA
  {
    [MySqlCharset("binary")]
    [MaxLength(255)]
    public string TestCharsetDAId { get; set; }
  }

  public class TestCharsetFA
  {
    public string TestCharsetFAId { get; set; }
  }

  [MySqlCollation("cp932_bin")]
  public class TestCollationDA
  {
    [MySqlCollation("greek_bin")]
    [MaxLength(255)]
    public string TestCollationDAId { get; set; }
  }

  public class TestCollationFA
  {
    public string TestCollationFAId { get; set; }
  }
}
