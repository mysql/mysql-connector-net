using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Data
{
    public enum UpdateRowSource
    {
      None,
      OutputParameters,
      FirstReturnedRecord,
      Both
    }

}

namespace System.ComponentModel
{
    public enum DesignerSerializationVisibility
    {
      Hidden, Visible, Content
    }


    public enum RefreshProperties
    {
      None, All, Repaint
    }
}

namespace System.IO
{
  public enum FileMode
  {
    CreateNew = 1,
    Create = 2,
    Open = 3,
    OpenOrCreate = 4,
    Truncate = 5,
    Append = 6,
  }

  [Flags]
  public enum FileAccess
  {
    Read = 1,
    Write = 2,
    ReadWrite = 3,
  }
}
