using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MySql.Data.VisualStudio
{
    internal class MySqlSource : Source
    {
		public MySqlSource(LanguageService service, IVsTextLines textLines, Colorizer colorizer)
			: base(service, textLines, colorizer)
		{
		}

        public override CommentInfo GetCommentFormat()
        {
            CommentInfo ci = new CommentInfo();
            ci.UseLineComments = true;
            ci.LineStart = "#";
            ci.BlockStart = "/*";
            ci.BlockEnd = "*/";
            return ci;
        }
    }
}
