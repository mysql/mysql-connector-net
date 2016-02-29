// Copyright © 2008, 2015, Oracle and/or its affiliates. All rights reserved.
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

using System.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if EF6
[assembly: AssemblyTitle("MySql.Data.Entity for EF6")]
[assembly: AssemblyDescription("Entity Framework 6.0 supported")]
#else
[assembly: AssemblyTitle("MySql.Data.Entity")]
[assembly: AssemblyDescription("")]
[assembly: AllowPartiallyTrustedCallers()]
#endif

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Oracle")]
[assembly: AssemblyProduct("MySql.Data.Entity")]
[assembly: AssemblyCopyright("Copyright © 2008, 2015, Oracle and/or its affiliates. All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("540e7b3c-bd0b-4980-96d1-5d140d303f7e")]
[assembly: AssemblyDelaySign(true)]
[assembly: AssemblyKeyFileAttribute(@"..\..\ConnectorNetPublicKey.snk")]
[assembly: InternalsVisibleTo("MySql.Data.Entity.Tests, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100d973bda91f71752c78294126974a41a08643168271f65fc0fb3cd45f658da01fbca75ac74067d18e7afbf1467d7a519ce0248b13719717281bb4ddd4ecd71a580dfe0912dfc3690b1d24c7e1975bf7eed90e4ab14e10501eedf763bff8ac204f955c9c15c2cf4ebf6563d8320b6ea8d1ea3807623141f4b81ae30a6c886b3ee1")]
[assembly: InternalsVisibleTo("MySql.Data.Entity.Migrations.Tests, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100d973bda91f71752c78294126974a41a08643168271f65fc0fb3cd45f658da01fbca75ac74067d18e7afbf1467d7a519ce0248b13719717281bb4ddd4ecd71a580dfe0912dfc3690b1d24c7e1975bf7eed90e4ab14e10501eedf763bff8ac204f955c9c15c2cf4ebf6563d8320b6ea8d1ea3807623141f4b81ae30a6c886b3ee1")]
[assembly: InternalsVisibleTo("MySql.Data.Entity.CodeFirst.Tests, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100d973bda91f71752c78294126974a41a08643168271f65fc0fb3cd45f658da01fbca75ac74067d18e7afbf1467d7a519ce0248b13719717281bb4ddd4ecd71a580dfe0912dfc3690b1d24c7e1975bf7eed90e4ab14e10501eedf763bff8ac204f955c9c15c2cf4ebf6563d8320b6ea8d1ea3807623141f4b81ae30a6c886b3ee1")]