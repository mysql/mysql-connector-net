using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  public static class ExtensionMethods
  {
    public static byte[] GetBuffer(this Stream stream)
    {
      int len = (int)(stream.Length - stream.Position);
      byte[] temp = new byte[len];
      stream.Read(temp, (int)stream.Position, len);
      return temp;
    }

    public static void Close(this Stream stream)
    {
      stream.Dispose();
    }

    public static string ToLower(this String newString, System.Globalization.CultureInfo culture)
    {
      if (culture == System.Globalization.CultureInfo.InvariantCulture)
        return newString.ToLowerInvariant();
      else
        return newString.ToLower();
    }

    public static string ToLongTimeString(this DateTime dateTime)
    {
      return dateTime.ToString(System.Globalization.DateTimeFormatInfo.CurrentInfo.LongTimePattern);
    }
  }
}
