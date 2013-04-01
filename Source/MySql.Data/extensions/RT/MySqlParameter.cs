using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace MySql.Data.MySqlClient
{
  public sealed partial class MySqlParameter : RTParameter
  {
  }

  public abstract class RTParameter
  {
    internal RTParameter() { }

    public abstract object Value { get; set; }
    public abstract int Size { get; set; }
    public abstract bool IsNullable { get; set; }
    public abstract ParameterDirection Direction  { get; set; }
    public abstract string ParameterName { get; set; }
  }

  public enum ParameterDirection
  {
    Input,
    Output,
    InputOutput,
    ReturnValue
  }
}
