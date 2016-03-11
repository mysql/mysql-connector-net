using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.MySqlClient.Test
{
  public class Command : Common
  {

    [Fact]
    public void CommandExecute()
    {
      MySqlConnection connection = new MySqlConnection(ConnectionString);
      connection.Open();

      MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM sakila.category;", connection);
      //MySqlCommand command = new MySqlCommand("SELECT * FROM sakila.category;", connection);
      int result = Convert.ToInt32(command.ExecuteScalar());

      connection.Close();

      Assert.True(result == 16);
    }
  }
}
