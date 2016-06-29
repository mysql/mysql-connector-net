using System;

namespace MySql.Data.MySqlClient.Tests
{
  public class DisplayNameAttribute : Attribute
  {
    public DisplayNameAttribute(string name) : base()
    {
      DisplayName = name;
    }

    public string DisplayName { get; set; }
  }
}