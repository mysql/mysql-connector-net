using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.MySqlClient.Test
{
  // This project can output the Class library as a NuGet Package.
  // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
  public class Connection : Common
  {
    [Fact]
    public void ConnectionOpen()
    {
      MySqlConnection connection = new MySqlConnection(ConnectionString);

      connection.Open();
      Assert.True(connection.State == ConnectionState.Open);

      connection.Close();
      Assert.True(connection.State == ConnectionState.Closed);

    }
  }
}
