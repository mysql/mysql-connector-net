using System;
using System.Data.Common;

namespace MySql.Data.MySqlClient
{
#if !RT
  internal class BaseTransaction : DbTransaction
#else
  internal class BaseTransaction : IDisposable
#endif
  {
  }
}
