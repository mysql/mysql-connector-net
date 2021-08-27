// Copyright (c) 2021 Oracle and/or its affiliates.
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

using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFramework.CodeFirst.Tests;

namespace MySql.EntityFramework.CodeFirst.Tests
{
  //ContextForString
  class ContextForString : DbContext
  {
    public ContextForString() : base(CodeFirstFixture.GetEFConnectionString<ContextForString>()) { }
    public DbSet<StringUser> StringUsers { get; set; }
  }
  public class StringUser
  {
    public int StringUserId { get; set; }

    [StringLength(50)]
    public string Name50 { get; set; }

    [StringLength(100)]
    public string Name100 { get; set; }

    [StringLength(200)]
    public string Name200 { get; set; }

    [StringLength(300)]
    public string Name300 { get; set; }
  }

  //ContextForTinyPk
  public class ContextForTinyPk : DbContext
  {
    public ContextForTinyPk() : base(CodeFirstFixture.GetEFConnectionString<ContextForTinyPk>()) { }
    public DbSet<TinyPkUser> TinyPkUseRs { get; set; }
  }

  public class TinyPkUser
  {
    [Key]
    [DataType("TINYINT")]
    public byte StringUserId { get; set; }

    [StringLength(50)]
    public string Name50 { get; set; }

    [StringLength(100)]
    public string Name100 { get; set; }

    [StringLength(200)]
    public string Name200 { get; set; }

    [StringLength(300)]
    public string Name300 { get; set; }
  }

  //ContextForBigIntPk
  public class ContextForBigIntPk : DbContext
  {
    public ContextForBigIntPk() : base(CodeFirstFixture.GetEFConnectionString<ContextForBigIntPk>()) { }
    public DbSet<BigIntPkUser> BigIntPkUseRs { get; set; }
  }

  public class BigIntPkUser
  {
    [Key]
    [DataType("BIGINT")]
    public long StringUserId { get; set; }

    [StringLength(50)]
    public string Name50 { get; set; }

    [StringLength(100)]
    public string Name100 { get; set; }

    [StringLength(200)]
    public string Name200 { get; set; }

    [StringLength(300)]
    public string Name300 { get; set; }
  }

  //EducationContext
  [Table("passports")]
  public class Passport
  {
    [Key]
    public int Key { get; set; }
  }

  public class EducationContext : DbContext
  {
    public EducationContext() : base(CodeFirstFixture.GetEFConnectionString<EducationContext>()) { }
    public DbSet<Passport> Passports { get; set; }
  }
}
