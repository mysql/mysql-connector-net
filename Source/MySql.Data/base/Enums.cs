using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Data
{
    public enum CommandType
    {
      Text,
      StoredProcedure,
      TableDirect
    }

    public enum UpdateRowSource
    {
      None,
      OutputParameters,
      FirstReturnedRecord,
      Both
    }

    public enum CommandBehavior
    {
      Default, 
      SingleResult,
      SchemaOnly,
      KeyInfo,
      SingleRow,
      SequentialAccess,
      CloseConnection
    }

    public enum DataRowVersion
    {
      Original, 
      Current,
      Proposed,
      Default
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
