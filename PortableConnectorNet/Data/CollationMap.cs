using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data
{
  public class CollationMap
  {
    private static Dictionary<int, string> collations = new Dictionary<int, string>();

    public static string GetCollationName(int collation)
    {
      if (collations.Keys.Count == 0)
        Load();
      if (!collations.ContainsKey(collation))
        throw new MySqlException(String.Format("Collation with id {0} not found.", collation));
      return collations[collation];
    }

    private static void Load()
    {
      collations.Add(33, "utf8");
    }
  }
}
