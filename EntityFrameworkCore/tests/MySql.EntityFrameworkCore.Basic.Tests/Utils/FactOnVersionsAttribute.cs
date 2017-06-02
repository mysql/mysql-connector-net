using MySql.Data.MySqlClient;
using System;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  internal class FactOnVersionsAttribute : FactAttribute
  {
    private static Version _version = null;

    public FactOnVersionsAttribute(string initial, string final) : base()
    {
      if (_version == null)
      {
        using (MySqlConnection conn = new MySqlConnection(MySQLTestStore.baseConnectionString))
        {
          conn.Open();
          _version = new Version(conn.ServerVersion);
        }
      }
      Version initialVersion = new Version(initial ?? "0.0.0");
      Version finalVersion = new Version(final ?? "99.99.99");
      if (initialVersion <= _version && _version <= finalVersion)
        base.Skip = null;
      else
        base.Skip = $"Skipping test because MySql Server is not between {initialVersion} and {finalVersion}";
    }
  }
}