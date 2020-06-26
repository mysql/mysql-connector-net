// Copyright (c) 2019, 2020 Oracle and/or its affiliates.
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

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// This object store the required parameters to create a Collection with schema validation.
  /// </summary>
  public struct CreateCollectionOptions
  {
    /// <summary>
    /// If false, throws an exception if the collection exists.
    /// </summary>
    public bool ReuseExisting { get; set; }
    /// <summary>
    /// Object which hold the Level and Schema parameters.
    /// </summary>
    public Validation Validation { get; set; }
  }

  /// <summary>
  /// This object store the required parameters to modify a Collection with schema validation.
  /// </summary>
  public struct ModifyCollectionOptions
  {
    /// <summary>
    /// This object store the required parameters to Modify a Collection with schema validation.
    /// </summary>
    public Validation Validation { get; set; }
  }

  /// <summary>
  /// This object store the required parameters to create a Collection with schema validation.
  /// </summary>
  public struct Validation
  {
    /// <summary>
    /// It can be STRICT to enable schema validation or OFF to disable <see cref="ValidationLevel"/>.
    /// </summary>
    public ValidationLevel? Level { get; set; }
    /// <summary>
    /// The JSON which define the rules to be validated in the collection.
    /// </summary>
    public string Schema { get; set; }
  }

  /// <summary>
  /// The possible values for parameter Level in Validation object.
  /// </summary>
  public enum ValidationLevel
  {
    //Disable schema validation
    OFF,
    //Enable schema validation
    STRICT
  }
}