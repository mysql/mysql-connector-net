// Copyright © 2015, 2025, Oracle and/or its affiliates.
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

using System.Data.Entity;
using System.Data.Common;

namespace MySql.Data.EntityFramework.CodeFirst.Tests
{
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public partial class SakilaDb : DbContext
  {

    public SakilaDb()
    : base(CodeFirstFixture.GetEFConnectionString<SakilaDb>("sakila"))
    {
    }

    public SakilaDb(DbConnection existingConnection, bool contextOwnsConnection)
        : base(existingConnection, contextOwnsConnection)
    {
    }

    public virtual DbSet<Actor> actors { get; set; }
    public virtual DbSet<Address> addresses { get; set; }
    public virtual DbSet<Category> categories { get; set; }
    public virtual DbSet<City> cities { get; set; }
    public virtual DbSet<Country> countries { get; set; }
    public virtual DbSet<Customer> customers { get; set; }
    public virtual DbSet<Film> films { get; set; }
    public virtual DbSet<film_actor> film_actor { get; set; }
    public virtual DbSet<film_category> film_category { get; set; }
    public virtual DbSet<film_text> film_text { get; set; }
    public virtual DbSet<Inventory> inventories { get; set; }
    public virtual DbSet<Language> languages { get; set; }
    public virtual DbSet<Payment> payments { get; set; }
    public virtual DbSet<Rental> rentals { get; set; }
    public virtual DbSet<Staff> staffs { get; set; }
    public virtual DbSet<Store> stores { get; set; }
    public virtual DbSet<actor_info> actor_info { get; set; }
    public virtual DbSet<customer_list> customer_list { get; set; }
    public virtual DbSet<film_list> film_list { get; set; }
    public virtual DbSet<nicer_but_slower_film_list> nicer_but_slower_film_list { get; set; }
    public virtual DbSet<sales_by_film_category> sales_by_film_category { get; set; }
    public virtual DbSet<sales_by_store> sales_by_store { get; set; }
    public virtual DbSet<staff_list> staff_list { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Actor>()
          .Property(e => e.first_name)
          .IsUnicode(false);

      modelBuilder.Entity<Actor>()
          .Property(e => e.last_name)
          .IsUnicode(false);

      modelBuilder.Entity<Actor>()
          .HasMany(e => e.film_actor)
          .WithRequired(e => e.actor)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Address>()
          .Property(e => e.address1)
          .IsUnicode(false);

      modelBuilder.Entity<Address>()
          .Property(e => e.address2)
          .IsUnicode(false);

      modelBuilder.Entity<Address>()
          .Property(e => e.district)
          .IsUnicode(false);

      modelBuilder.Entity<Address>()
          .Property(e => e.postal_code)
          .IsUnicode(false);

      modelBuilder.Entity<Address>()
          .Property(e => e.phone)
          .IsUnicode(false);

      modelBuilder.Entity<Address>()
          .HasMany(e => e.customers)
          .WithRequired(e => e.address)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Address>()
          .HasMany(e => e.staffs)
          .WithRequired(e => e.address)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Address>()
          .HasMany(e => e.stores)
          .WithRequired(e => e.address)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Category>()
          .Property(e => e.name)
          .IsUnicode(false);

      modelBuilder.Entity<Category>()
          .HasMany(e => e.film_category)
          .WithRequired(e => e.category)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<City>()
          .Property(e => e.city1)
          .IsUnicode(false);

      modelBuilder.Entity<City>()
          .HasMany(e => e.addresses)
          .WithRequired(e => e.city)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Country>()
          .Property(e => e.country1)
          .IsUnicode(false);

      modelBuilder.Entity<Country>()
          .HasMany(e => e.cities)
          .WithRequired(e => e.country)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Customer>()
          .Property(e => e.first_name)
          .IsUnicode(false);

      modelBuilder.Entity<Customer>()
          .Property(e => e.last_name)
          .IsUnicode(false);

      modelBuilder.Entity<Customer>()
          .Property(e => e.email)
          .IsUnicode(false);

      modelBuilder.Entity<Customer>()
          .HasMany(e => e.payments)
          .WithRequired(e => e.customer)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Customer>()
          .HasMany(e => e.rentals)
          .WithRequired(e => e.customer)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Film>()
          .Property(e => e.title)
          .IsUnicode(false);

      modelBuilder.Entity<Film>()
          .Property(e => e.description)
          .IsUnicode(false);

      modelBuilder.Entity<Film>()
          .Property(e => e.rating)
          .IsUnicode(false);

      modelBuilder.Entity<Film>()
          .Property(e => e.special_features)
          .IsUnicode(false);

      modelBuilder.Entity<Film>()
          .HasMany(e => e.film_actor)
          .WithRequired(e => e.film)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Film>()
          .HasMany(e => e.film_category)
          .WithRequired(e => e.film)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Film>()
          .HasMany(e => e.inventories)
          .WithRequired(e => e.film)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<film_text>()
          .Property(e => e.title)
          .IsUnicode(false);

      modelBuilder.Entity<film_text>()
          .Property(e => e.description)
          .IsUnicode(false);

      modelBuilder.Entity<Inventory>()
          .HasMany(e => e.rentals)
          .WithRequired(e => e.inventory)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Language>()
          .Property(e => e.name)
          .IsUnicode(false);

      modelBuilder.Entity<Language>()
          .HasMany(e => e.films)
          .WithRequired(e => e.language)
          .HasForeignKey(e => e.language_id)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Language>()
          .HasMany(e => e.films1)
          .WithOptional(e => e.language1)
          .HasForeignKey(e => e.original_language_id);

      modelBuilder.Entity<Staff>()
          .Property(e => e.first_name)
          .IsUnicode(false);

      modelBuilder.Entity<Staff>()
          .Property(e => e.last_name)
          .IsUnicode(false);

      modelBuilder.Entity<Staff>()
          .Property(e => e.email)
          .IsUnicode(false);

      modelBuilder.Entity<Staff>()
          .Property(e => e.username)
          .IsUnicode(false);

      modelBuilder.Entity<Staff>()
          .Property(e => e.password)
          .IsUnicode(false);

      modelBuilder.Entity<Staff>()
          .HasMany(e => e.payments)
          .WithRequired(e => e.staff)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Staff>()
          .HasMany(e => e.rentals)
          .WithRequired(e => e.staff)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Staff>()
          .HasMany(e => e.stores)
          .WithRequired(e => e.staff)
          .HasForeignKey(e => e.manager_staff_id)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Store>()
          .HasMany(e => e.customers)
          .WithRequired(e => e.store)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Store>()
          .HasMany(e => e.inventories)
          .WithRequired(e => e.store)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<Store>()
          .HasMany(e => e.staffs)
          .WithRequired(e => e.store)
          .HasForeignKey(e => e.store_id)
          .WillCascadeOnDelete(false);

      modelBuilder.Entity<actor_info>()
          .Property(e => e.first_name)
          .IsUnicode(false);

      modelBuilder.Entity<actor_info>()
          .Property(e => e.last_name)
          .IsUnicode(false);

      modelBuilder.Entity<actor_info>()
          .Property(e => e.film_info)
          .IsUnicode(false);

      modelBuilder.Entity<customer_list>()
          .Property(e => e.name)
          .IsUnicode(false);

      modelBuilder.Entity<customer_list>()
          .Property(e => e.address)
          .IsUnicode(false);

      modelBuilder.Entity<customer_list>()
          .Property(e => e.zip_code)
          .IsUnicode(false);

      modelBuilder.Entity<customer_list>()
          .Property(e => e.phone)
          .IsUnicode(false);

      modelBuilder.Entity<customer_list>()
          .Property(e => e.city)
          .IsUnicode(false);

      modelBuilder.Entity<customer_list>()
          .Property(e => e.country)
          .IsUnicode(false);

      modelBuilder.Entity<customer_list>()
          .Property(e => e.notes)
          .IsUnicode(false);

      modelBuilder.Entity<film_list>()
          .Property(e => e.title)
          .IsUnicode(false);

      modelBuilder.Entity<film_list>()
          .Property(e => e.description)
          .IsUnicode(false);

      modelBuilder.Entity<film_list>()
          .Property(e => e.category)
          .IsUnicode(false);

      modelBuilder.Entity<film_list>()
          .Property(e => e.rating)
          .IsUnicode(false);

      modelBuilder.Entity<film_list>()
          .Property(e => e.actors)
          .IsUnicode(false);

      modelBuilder.Entity<nicer_but_slower_film_list>()
          .Property(e => e.title)
          .IsUnicode(false);

      modelBuilder.Entity<nicer_but_slower_film_list>()
          .Property(e => e.description)
          .IsUnicode(false);

      modelBuilder.Entity<nicer_but_slower_film_list>()
          .Property(e => e.category)
          .IsUnicode(false);

      modelBuilder.Entity<nicer_but_slower_film_list>()
          .Property(e => e.rating)
          .IsUnicode(false);

      modelBuilder.Entity<nicer_but_slower_film_list>()
          .Property(e => e.actors)
          .IsUnicode(false);

      modelBuilder.Entity<sales_by_film_category>()
          .Property(e => e.category)
          .IsUnicode(false);

      modelBuilder.Entity<sales_by_store>()
          .Property(e => e.store)
          .IsUnicode(false);

      modelBuilder.Entity<sales_by_store>()
          .Property(e => e.manager)
          .IsUnicode(false);

      modelBuilder.Entity<staff_list>()
          .Property(e => e.name)
          .IsUnicode(false);

      modelBuilder.Entity<staff_list>()
          .Property(e => e.address)
          .IsUnicode(false);

      modelBuilder.Entity<staff_list>()
          .Property(e => e.zip_code)
          .IsUnicode(false);

      modelBuilder.Entity<staff_list>()
          .Property(e => e.phone)
          .IsUnicode(false);

      modelBuilder.Entity<staff_list>()
          .Property(e => e.city)
          .IsUnicode(false);

      modelBuilder.Entity<staff_list>()
          .Property(e => e.country)
          .IsUnicode(false);
    }
  }
}
