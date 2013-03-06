// Copyright (c) 2008 MySQL AB, 2008-2009 Sun Microsystems, Inc.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using System.Runtime.InteropServices;

namespace MySql.Data.VisualStudio.Editors
{
    internal class TextBufferEditor 
    {
        private bool noContent;
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp;

        public TextBufferEditor(System.IServiceProvider sp)
        {
            psp = sp as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
            noContent = true;
            CreateCodeEditor();
        }

        private IVsCodeWindow _codeWindow;
        public IVsCodeWindow CodeWindow 
        {
            get { return _codeWindow; }
            private set { _codeWindow = value; }
        }

        private IVsTextBuffer _textBuffer;
        public IVsTextBuffer TextBuffer 
        {
            get { return _textBuffer; }
            private set { _textBuffer = value; }
        }

        private void CreateCodeEditor()
        {
            Guid clsidTextBuffer = typeof(VsTextBufferClass).GUID;
            Guid iidTextBuffer = VSConstants.IID_IUnknown;
            TextBuffer = (IVsTextBuffer)MySqlDataProviderPackage.Instance.CreateInstance(
                                 ref clsidTextBuffer,
                                 ref iidTextBuffer,
                                 typeof(object));
            if (TextBuffer == null)
                throw new Exception("Failed to create core editor");

            // first we need to site our buffer
            IObjectWithSite textBufferSite = TextBuffer as IObjectWithSite;
            if (textBufferSite != null)
            {
                textBufferSite.SetSite(psp);
            }

            // then we need to tell our buffer not to attempt to autodetect the
            // language settings
            IVsUserData userData = TextBuffer as IVsUserData;
            Guid g = EditorFactory.GuidVSBufferDetectLangSid;
            int result = userData.SetData(ref g, false);

            Guid clsidCodeWindow = typeof(VsCodeWindowClass).GUID;
            Guid iidCodeWindow = typeof(IVsCodeWindow).GUID;
            IVsCodeWindow pCodeWindow = (IVsCodeWindow)MySqlDataProviderPackage.Instance.CreateInstance(
                   ref clsidCodeWindow,
                   ref iidCodeWindow,
                   typeof(IVsCodeWindow));
            if (pCodeWindow == null)
                throw new Exception("Failed to create core editor");

            // Give the text buffer to the code window.                    
            // We are giving up ownership of the text buffer!                    
            pCodeWindow.SetBuffer((IVsTextLines)TextBuffer);

            CodeWindow = pCodeWindow;
        }

        public bool Dirty
        {
            get
            {
                uint flags;
                TextBuffer.GetStateFlags(out flags);
                return (flags & (uint)BUFFERSTATEFLAGS.BSF_MODIFIED) != 0;
            }
            set
            {
                uint flags;
                TextBuffer.GetStateFlags(out flags);
                if (value)
                    flags |= (uint)BUFFERSTATEFLAGS.BSF_MODIFIED;
                else
                    flags &= ~(uint)BUFFERSTATEFLAGS.BSF_MODIFIED;
                TextBuffer.SetStateFlags(flags);
            }
        }

        public string Text
        {
            get
            {
                IVsTextLines lines = (IVsTextLines)TextBuffer;
                int lineCount, lineLength;
                string text;
                lines.GetLineCount(out lineCount);
                lines.GetLengthOfLine(lineCount - 1, out lineLength);
                lines.GetLineText(0, 0, lineCount - 1, lineLength, out text);
                return text;
            }
            set 
            {
                if (noContent)
                    Initialize(value);
                else
                    Replace(value);
                noContent = false;
            }
        }

        private void Initialize(string value)
        {
            IVsTextLines lines = (IVsTextLines)TextBuffer;
            lines.InitializeContent(value, value.Length);
        }

        private void Replace(string value)
        {
            int endLine, endCol;
            IVsTextLines lines = (IVsTextLines)TextBuffer;
            lines.GetLastLineIndex(out endLine, out endCol);

            IntPtr pText = Marshal.StringToCoTaskMemAuto(value);

            try
            {
                lines.ReplaceLines(0, 0, endLine, endCol, pText, value.Length, null);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pText);
            }
        }
    }
}
