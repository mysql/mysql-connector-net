using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI
{
  public class Result
  {
    public Error ErrorInfo;

    public bool Failed
    {
      get { return ErrorInfo != null;  }
    }

    public bool Succeeded
    {
      get { return !Failed;  }
    }


    public class Error
    {
      public UInt32 Code;
      public string SqlState;
      public string Message;
    }
  }
}
