using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Test
{
  public abstract class Common
  {
    public string ConnectionString { get; set; }

    protected Common()
    {
      ConnectionString = "server=localhost;user id=root;password=root;persistsecurityinfo=True;port=3306;database=sakila;";
    }


  }
}
