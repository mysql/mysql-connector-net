using System;
using System.Data;
using System.Data.Common;

namespace MySql.Data.MySqlClient
{
#if RT
  public abstract class BaseDataReader
  {
  }

  public abstract class BaseParameter
  {
  }

#else
  public abstract class BaseDataReader : DbDataReader, IDataReader, IDataRecord
  {
  }

  public abstract class BaseParameter : DbParameter, IDataParameter, IDbDataParameter
  {
  }
#endif
}
