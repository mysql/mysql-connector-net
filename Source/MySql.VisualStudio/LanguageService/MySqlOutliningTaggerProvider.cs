using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

namespace MySql.Data.VisualStudio.LanguageService
{
  
  //[Export(typeof(ITaggerProvider))]
  //[TagType(typeof(IOutliningRegionTag))]
  //[ContentType("text")]
  internal sealed class MySqlOutliningTaggerProvider : ITaggerProvider
  {
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
      //create a single tagger for each buffer.
      Func<ITagger<T>> sc = delegate() { return new MySqlOutliningTagger(buffer) as ITagger<T>; };
      return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
    } 
  }
}
