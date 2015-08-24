using MySql.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI
{
  public class Collection<T> : Collection
  {
    public Collection(Schema s, string name) : base(s, name)
    {
    }

    public Result Add(T value)
    {
      string json = JsonSerializer.ToJson(value);
      return Add(json);
    }


  }
}
