using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr.Runtime;

namespace MySql.Parser
{
  /// <summary>
  /// Case insensitive implementation of an input stream.
  /// </summary>
  public class CaseInsensitiveInputStream : ANTLRInputStream
  {
    public CaseInsensitiveInputStream( Stream s ) : base( s ) 
    {
    }

    // Only the lookahead is converted to lowercase. The original case is preserved in the stream.
    public override int LA(int i) 
    {
      if (i == 0) {
        return 0;
      }

      if (i < 0) {
        i++;
      }

      if (((p + i) - 1) >= n) {
        return (int) CharStreamConstants.EndOfFile;
      }
      // This is how "case insensitive" is defined, i.e., could also use a special culture...
      return Char.ToUpperInvariant(data[(p + i) - 1]); 
    }
  }
}
