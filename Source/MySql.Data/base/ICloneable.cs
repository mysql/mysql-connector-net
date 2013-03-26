using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql.Data.MySqlClient
{
    interface ICloneable
    {
      object Clone();
    }
}
