using System;
using System.Collections;
using System.ComponentModel;
using MySql.Data.MySqlClient.Properties;

namespace MySql.Data.MySqlClient
{
  public sealed partial class MySqlParameterCollection : RTParameterCollection
  {
  }

  public abstract class RTParameterCollection
  {
    internal RTParameterCollection() { }

    public abstract int Count { get; }
    public abstract void Clear();
    public abstract int IndexOf(string parameterName);
    public abstract int IndexOf(object value);
  }
}
