using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MySql.Data.MySqlClient
{
  public class StringUtility
  {
    public static string ToUpperInvariant(string s)
    {
#if CF
      return s.ToUpper(CultureInfo.InvariantCulture);
#else
      return s.ToUpperInvariant();
#endif
    }

    public static string ToLowerInvariant(string s)
    {
#if CF
      return s.ToLower(CultureInfo.InvariantCulture);
#else
      return s.ToLowerInvariant();
#endif
    }
  
  }
}
