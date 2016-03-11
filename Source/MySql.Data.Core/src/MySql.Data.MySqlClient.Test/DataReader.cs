using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.MySqlClient.Test
{
  public class DataReader : Common
  {
    [Fact]
    public void ReadDataReader()
    {
      //Test the same result using two differents connections to the same db and table, should be the same
      MySqlConnection connection1 = new MySqlConnection(ConnectionString);
      MySqlConnection connection2 = new MySqlConnection(ConnectionString);
      connection1.Open();
      connection2.Open();

      MySqlCommand command1 = new MySqlCommand("SELECT * FROM sakila.category;", connection1);
      MySqlCommand command2 = new MySqlCommand("SELECT * FROM sakila.category;", connection2);

      using (MySqlDataReader reader1 = command1.ExecuteReader())
      {
        using (MySqlDataReader reader2 = command2.ExecuteReader())
        {
          while (true)
          {
            if (!reader1.Read() && !reader2.Read())
            {
              reader2.Read();
              Assert.Equal(reader1, reader2);
            }
            else
            {
              break;
            }
          }
        }
      }

      connection1.Close();
      connection2.Close();
    }
  }
}
