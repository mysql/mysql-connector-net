using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.RT
{
  public class Encoding
  {
    private static Encoding instance = new Encoding();
    private System.Text.Encoding defaultEncoding = new System.Text.UTF8Encoding();

    public static Encoding Instance
    {
      get { return instance; }
    }

    public static System.Text.Encoding Default
    {
      get { return instance.defaultEncoding; }
    }
  }
}
