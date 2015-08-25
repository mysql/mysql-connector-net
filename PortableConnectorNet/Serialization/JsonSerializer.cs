using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MySql.Serialization
{
  public class JsonSerializer
  {
    public static string ToJson(object value)
    {
      ///TODO:  see if we can improve on this
      string json = JsonConvert.SerializeObject(value);
      return EnsureId(json);
    }

    public static string EnsureId(string json)
    {
      Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
      if (!dict.ContainsKey("_id"))
        dict["_id"] = Guid.NewGuid().ToString("N");
      return JsonConvert.SerializeObject(dict);
    }
  }
}
