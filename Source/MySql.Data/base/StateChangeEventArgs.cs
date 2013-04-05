using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  public class StateChangeEventArgs : EventArgs
    {
      public StateChangeEventArgs(ConnectionState originalState, ConnectionState currentState)
      {
        CurrentState = currentState;
        OriginalState = originalState;
      }

      public ConnectionState CurrentState { get; private set; }

      public ConnectionState OriginalState { get; private set; }
    }
}
