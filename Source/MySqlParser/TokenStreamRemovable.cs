using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;
using MySql.Parser;

namespace MySql.Parser
{
  /// <summary>
  /// These class adds remove token capability to the CommonTokenStream.
  /// </summary>
  public class TokenStreamRemovable : CommonTokenStream
  {
    public TokenStreamRemovable() : base() { }
    public TokenStreamRemovable(ITokenSource tokenSource) : base(tokenSource) { }
    public TokenStreamRemovable(ITokenSource tokenSource, int channel) : base(tokenSource, channel) { }

    public void Remove(IToken t)
    {
      this._tokens.Remove(t);
    }
  }
}
