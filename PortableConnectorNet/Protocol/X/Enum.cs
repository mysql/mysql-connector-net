using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Protocol.X
{
  internal enum NoticeType : int
  {
    Warning = 1,
    SessionVariableChanged = 2,
    SessionStateChanged = 3,
  }
}
