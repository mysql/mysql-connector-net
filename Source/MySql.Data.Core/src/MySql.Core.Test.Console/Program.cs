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
