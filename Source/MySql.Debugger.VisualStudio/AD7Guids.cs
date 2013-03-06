// Copyright © 2004, 2012, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql.Debugger.VisualStudio
{
  public class AD7Guids
  {
    public const string EngineString = "EEEE0740-10F7-4e5f-8BC4-1CC0AC9ED5B0";
    public static readonly Guid EngineGuid = new Guid(EngineString);

    public const string CLSIDString = "EEEE066A-1103-451f-BC7A-6AEF76558AE2";
    public static readonly Guid CLSIDGuid = new Guid(CLSIDString);

    public const string ProgramProviderString = "EEEE9AB0-511C-4bf0-BBE8-F763A73DA5EF";
    public static readonly Guid ProgramProviderGuid = new Guid(ProgramProviderString);

    public const string PortSupplierString = "EEEE547D-6B37-4F46-9567-F4AC7ACAFCBE";
    public static readonly Guid PortSupplierGuid = new Guid(ProgramProviderString);

    public const string EngineName = "MySql Stored Procedure Debug Engine";

    public const string LanguageName = "MySql language";
  }
}
