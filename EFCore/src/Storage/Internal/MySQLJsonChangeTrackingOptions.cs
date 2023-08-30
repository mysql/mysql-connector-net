// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

namespace MySql.EntityFrameworkCore.Storage.Internal
{
  internal enum MySQLJsonChangeTrackingOptions
  {
    /// <summary>
    /// The default is to serialize everything, which is the most precise, but also the slowest.
    /// </summary>
    None = 0,

    /// <summary>
    /// Do not track changes inside of JSON mapped properties but only for the root property itself.
    /// For example, if the JSON mapped property is a top level array of `int`, then changes to items of the
    /// array are not tracked, but changes to the array property itself (the reference) are.
    /// </summary>
    CompareRootPropertyOnly = 0x00000001 | CompareStringRootPropertyByEquals | CompareDomRootPropertyByEquals,

    /// <summary>
    /// Compare strings as is, without further processing. This means that adding whitespaces between inner
    /// properties of a JSON object, that have no effect at all to the JSON object itself, would lead to a change
    /// being discovered to the JSON object, resulting in the JSON mapped property being marked as modified.
    /// </summary>
    CompareStringRootPropertyByEquals = 0x00000002,

    /// <summary>
    /// Only check the JSON root property for DOM objects.
    /// </summary>
    CompareDomRootPropertyByEquals = 0x00000004,

    /// <summary>
    /// Traverse the DOM to check for changes.
    /// </summary>
    CompareDomSemantically = 0x00000008,

    /// <summary>
    /// Fully traverse the DOM to generate a hash.
    /// </summary>
    HashDomSemantially = 0x00010000,

    /// <summary>
    /// Traverse part of the DOM to generate a hash.
    /// </summary>
    HashDomSemantiallyOptimized = 0x00020000,

    /// <summary>
    /// Call DeepClone() whenever a type, for which a snapshot needs to be generated, implements it.
    /// </summary>
    SnapshotCallsDeepClone = 0x01000000,

    /// <summary>
    /// Call Clone() whenever a type, for which a snapshot needs to be generated, implements it.
    /// </summary>
    SnapshotCallsClone = 0x02000000,
  }
}
