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
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySql.Core.Test.Console
{
  public class Program
  {
    public static void Main(string[] args)
    {
      MySqlConnection connection = new MySqlConnection("server=localhost;user id=root;password=root;persistsecurityinfo=True;port=3306;database=sakila;");
      connection.Open();

      MySqlCommand command = new MySqlCommand("SELECT * FROM sakila.category;", connection);

      var result = command.ExecuteNonQuery();

      using (MySqlDataReader reader =  command.ExecuteReader())
      {
        System.Console.WriteLine("Category Id\t\tName\t\tLast Update");
        while (reader.Read())
        {
          string row = string.Format("{0}\t\t{1}\t\t{2}", reader["category_id"], reader["name"], reader["last_update"]);
          System.Console.WriteLine(row);
        }
      }

      connection.Close();
      System.Console.ReadKey();
    }
  }
}
