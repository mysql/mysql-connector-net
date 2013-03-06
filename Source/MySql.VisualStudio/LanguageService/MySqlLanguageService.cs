// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using System;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// This language service will provide language
  /// </summary>
  [ComVisible(true)]
  [Guid(MySqlLanguageService.IID)]
  [ProvideLanguageService(typeof(MySqlLanguageService),
                            MySqlLanguageService.LanguageName,
                            -1,
        // Optional language service properties
        CodeSense             = true,  // General IntelliSense support
        RequestStockColors    = false, // Custom colorable items        
        MatchBraces           = true,  // Match braces on command
        QuickInfo             = true,
        ShowCompletion        = true,
        MatchBracesAtCaret    = true   // Match braces while typing 
        )]
  class MySqlLanguageService : Microsoft.VisualStudio.Package.LanguageService
  {
    private LanguagePreferences preferences;

    public const string LanguageName = "MySQL";
    public const string IID = "FA498A2D-116A-4f25-9B55-7938E8E6DDA7";

    public override AuthoringScope ParseSource(ParseRequest req)
    {
      return null;
    }

    public override string Name
    {
      get { return LanguageName; }
    }

    public override string GetFormatFilterList()
    {
      return String.Empty;
    }

    public override IScanner GetScanner(IVsTextLines buffer)
    {
      return new MySqlScanner();
    }

    public override LanguagePreferences GetLanguagePreferences()
    {
      if (preferences == null)
      {
        preferences = new LanguagePreferences(this.Site,
            typeof(MySqlLanguageService).GUID, LanguageName);
        preferences.Init();
      }
      return preferences;
    }

    public override int ValidateBreakpointLocation(IVsTextBuffer buffer, int line, int col, TextSpan[] pCodeSpan)
    {
      return base.ValidateBreakpointLocation(buffer, line, col, pCodeSpan);
    }
  }
}
