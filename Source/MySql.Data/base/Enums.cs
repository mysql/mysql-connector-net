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
