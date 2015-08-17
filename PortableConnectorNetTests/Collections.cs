// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySql.XDevAPI;
using System;

namespace MySqlX_DevAPI.Sections
{
  public class Collections
  {
    static Schema db = MySqlX.GetSession("").GetDefaultSchema();

    static Collection myColl = new Collection();


    static void CreatingACollection()
    {
      // Create a new collection called "my_collection"
      var myColl = db.CreateCollection("my_collection");

      // Create a new collection or reuse existing one
      var myExistingColl = db.CreateCollection("my_collection", ReuseExistingObject: true);

    }

    static void WorkingWithExistingCollections()
    {
      /*
    // Get a collection object for 'my_collection'
    var myColl = db.getCollection('my_collection');
    
    // Get a collection object but also ensure it exists in the database
    var myColl = db.getCollection('my_collection', { validateExistence: true } );
      */

      // Get a collection object for "my_collection"
      var myColl = db.GetCollection("my_collection");

      // Get a collection object but also ensure it exists in the database
      var myColl2 = db.GetCollection("my_collection", ValidateExistence: true);

    }

    static void CollectionAdd()
    {
      /*
    // Create a new collection
    var myColl = db.createCollection('myCollection');
    
    // Insert a document
    myColl.add( { name: 'Sakila', age: 15 } ).run();
    
    // Insert several documents at once
    myColl.add( [ 
      { name: 'Susanne', age: 24 },
      { name: 'Mike', age: 39 } ] ).run();
      */

      // Create a new collection
      var myColl = db.CreateCollection("myCollection");

      // Insert a document
      myColl.Add(new { name = "Sakila", age = 15 }).Run();

      // Insert several documents at once
      myColl.Add(new[] { 
      new { name= "Susanne", age= 24 },
      new { name= "Mike", age= 39 } }).Run();

    }

    static void CollectionFind()
    {
      /*
    // Use the collection 'my_collection'
    var myColl = db.useCollection('my_collection');
    
    // Find a single document that has a field 'name' starts with an 'S'
    var docs = myColl.find('name like ?')
      .bind('S%').limit(1).fetch();
    
    echo(docs.first());
    
    // Get all documents with a field 'name' that starts with an 'S'
    docs = myColl.find('name like ?')
      .bind('S%').fetch();
    
    var myDoc;
    while (myDoc = docs.next()) {
      echo(myDoc);
    }
      */

      // Use the collection "my_collection"
      var myColl = db.GetCollection("my_collection");

      // Find a single document that has a field "name" starts with an "S"
      var docs = myColl.Find("name like ?")
        .Bind("S%").Limit(1).Execute();

      Console.WriteLine(docs.First());

      // Get all documents with a field "name" that starts with an "S"
      docs = myColl.Find("name like ?")
        .Bind("S%").Execute();

      var myDoc = docs.Next();
      while (myDoc != null)
      {
        Console.WriteLine(myDoc);
        myDoc = docs.Next();
      }

    }

    static void JoiningCollections()
    {
      /*
    // Get two collections from the database
    var customers = db.getCollection('customers');
    var orders = db.getCollections('orders');
    
    // Do a straight forward, inner join
    var res = customers.as('c').join( 
        orders.as('o').on('c._id = o.customer_id') )
      .find('o.shipped = false')
      .fields('o.orderDate, c.address.zip, c.name, o.total')
      .sort('o.orderDate DESC')
      .limit(25)
      .fetch();
    
    echo('Recent orders that have not been shipped yet:');
    var myDoc;
    while (myDoc = res.next()) {
      echo(myDoc);
    }
      */

      // Get two collections from the database
      var customers = db.GetCollection("customers");
      var orders = db.GetCollection("orders");

      // Do a straight forward, inner join
      var res = customers.As("c").Join(
          orders.As("o").On("c._id = o.customer_id"))
        .Find("o.shipped = false")
        .Fields("o.orderDate, c.address.zip, c.name, o.total")
        .Sort("o.orderDate DESC")
        .Limit(25)
        .Execute();

      Console.WriteLine("Recent orders that have not been shipped yet:");
      var myDoc = res.Next();
      while (myDoc != null)
      {
        Console.WriteLine(myDoc);
        myDoc = res.Next();
      }

    }

    static void JoiningCollections2()
    {
      /*
    // Get two collections from the database and assign Aliases
    var orders = db.getCollection('orders').as('o');
    var invoices = db.getCollections('invoices').as('i');
    
    // Do a left outer join this time, to also get orders 
    // that do not have an invoice yet
    var myDocs = orders.join( 
        invoices.on('o._id (+)= i.order_id') )
      .find('o.customer_id = :customerId')
      .fields('o.orderDate, o.orderNumber, o.items, i.invoiceDate, i.total')
      .sort('o.orderDate DESC')
      .bind({ customerId: 35001 }).fetch();
    
    echo('All orders of the customer:');
    var myDoc;
    while (myDoc = res.next()) {
      echo(myDoc);
    }
      */

      // Get two collections from the database and assign Aliases
      var orders = db.GetCollection("orders").As("o");
      var invoices = db.GetCollection("invoices").As("i");

      // Do a left outer join this time, to also get orders 
      // that do not have an invoice yet
      var myDocs = orders.Join(
          invoices.On("o._id (+)= i.order_id"))
        .Find("o.customer_id = :customerId")
        .Fields("o.orderDate, o.orderNumber, o.items, i.invoiceDate, i.total")
        .Sort("o.orderDate DESC")
        .Bind(new { customerId = 35001 }).Execute();

      Console.WriteLine("All orders of the customer:");
      var myDoc = myDocs.Next();
      while (myDoc != null)
      {
        Console.WriteLine(myDoc);
        myDoc = myDocs.Next();
      }

    }

    static void JoiningCollections3()
    {
      /*
    // Get two collections from the database and assign an Alias
    var customers = db.getCollection('customers').as('c');
    var orders = db.getCollections('orders').as('o');
    
    // Do a straight forward join
    var res = customers.join(orders, 'c._id = o.customer_id')
      .find('o.shipped = false')
      .fields('o.orderDate, c.address.zip, c.name, o.total')
      .sort('o.orderDate DESC')
      .limit(25)
      .fetch();
    
    echo('Recent orders that have not been shipped yet:');
    var myDoc;
    while (myDoc = res.next()) {
      echo(myDoc);
    }
    
    // Another join example against the products table
    var products = db.getCollections('products').as('p');
    
    res = customers.join(products, 'c._id IN p.reviews[*].customer_id')
      .find('c.nickName = ?')
      .fields('c.name AS customer, p.name AS product')
      .sort('p.name')
      .bind('sakila')
      .fetch();
    
    echo('The products rated by the given customer.');
    while (myDoc = res.next()) {
      echo(myDoc);
    }
      */

      // Get two collections from the database and assign an Alias
      var customers = db.GetCollection("customers").As("c");
      var orders = db.GetCollection("orders").As("o");

      // Do a straight forward join
      var res = customers.Join(orders, "c._id = o.customer_id")
        .Find("o.shipped = false")
        .Fields("o.orderDate, c.address.zip, c.name, o.total")
        .Sort("o.orderDate DESC")
        .Limit(25)
        .Execute();

      Console.WriteLine("Recent orders that have not been shipped yet:");
      var myDoc = res.Next();
      while (myDoc != null)
      {
        Console.WriteLine(myDoc);
        myDoc = res.Next();
      }

      // Another join example against the products table
      var products = db.GetCollection("products").As("p");

      res = customers.Join(products, "c._id IN p.reviews[*].customer_id")
        .Find("c.nickName = ?")
        .Fields("c.name AS customer, p.name AS product")
        .Sort("p.name")
        .Bind("sakila")
        .Execute();

      Console.WriteLine("The products rated by the given customer.");
      while ((myDoc = res.Next()) != null)
      {
        Console.WriteLine(myDoc);
      }

    }

    static void NestingFindFunctions()
    {
      /*
    // Get the customers collection
    var customers = db.getCollection('customers');
    
    // Get the customers with the highest productReviewScore
    var res = customers.find('c.productReviewScore = :maxScore')
      .fields('c.nickName, c.productReviewScore')
      .bind({ 
        maxScore: customers.find().fields('MAX(c2.productReviewScore)') })
      .fetch();
      
    echo('Customer(s) with the highest product review score.');
    var myDoc;
    while (myDoc = res.next()) {
      echo(myDoc);
    }
      */

      // Get the customers collection
      var customers = db.GetCollection("customers");

      // Get the customers with the highest productReviewScore
      var res = customers.Find("c.productReviewScore = :maxScore")
        .Fields("c.nickName, c.productReviewScore")
        .Bind(new
        {
          maxScore = customers.Find().Fields("MAX(c2.productReviewScore)")
        })
        .Execute();

      Console.WriteLine("Customer(s) with the highest product review score.");
      var myDoc = res.Next();
      while (myDoc != null)
      {
        Console.WriteLine(myDoc);
        myDoc = res.Next();
      }

    }

  }
}
