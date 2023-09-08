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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MySql.EntityFrameworkCore.Diagnostics
{
  /// <summary>
  /// <para>
  ///   Event IDs for MySQL events that correspond to messages logged to an <see cref="ILogger" />
  ///   and events sent to a <see cref="DiagnosticSource" />.
  /// </para>
  /// <para>
  ///   These IDs are also used with <see cref="WarningsConfigurationBuilder" /> to configure the
  ///   behavior of warnings.
  /// </para>
  /// </summary>
  public static class MySQLEventId
  {
    // Warning: These values must not change between releases.
    // Only add new values to the end of sections, never in the middle.
    // Try to use <Noun><Verb> naming and be consistent with existing names.
    private enum Id
    {
      // Model validation events
      DecimalTypeDefaultWarning = CoreEventId.ProviderBaseId,
      ByteIdentityColumnWarning,

      // Scaffolding events
      ColumnFound = CoreEventId.ProviderDesignBaseId,
      DefaultSchemaFound,
      ForeignKeyReferencesMissingPrincipalTableWarning,
      MissingSchemaWarning,
      MissingTableWarning,
      SequenceFound,
      TableFound,
      TypeAliasFound,
      PrimaryKeyFound,
      UniqueConstraintFound,
      IndexFound,
      ForeignKeyFound,
      ForeignKeyPrincipalColumnMissingWarning,
      ReflexiveConstraintIgnored
    }

    private static readonly string _validationPrefix = DbLoggerCategory.Model.Validation.Name + ".";
    private static EventId MakeValidationId(Id id) => new EventId((int)id, _validationPrefix + id);

    /// <summary>
    /// <para>
    ///   No explicit type for a decimal column.
    /// </para>
    /// <para>
    ///   This event is in the <see cref="DbLoggerCategory.Model.Validation" /> category.
    /// </para>
    /// <para>
    ///   This event uses the <see cref="PropertyEventData" /> payload when used with a <see cref="DiagnosticSource" />.
    /// </para>
    /// </summary>
    public static readonly EventId DecimalTypeDefaultWarning = MakeValidationId(Id.DecimalTypeDefaultWarning);

    /// <summary>
    /// <para>
    ///   A byte property is set up to use a MySQL identity column.
    /// </para>
    /// <para>
    ///   This event is in the <see cref="DbLoggerCategory.Model.Validation" /> category.
    /// </para>
    /// <para>
    ///   This event uses the <see cref="PropertyEventData" /> payload when used with a <see cref="DiagnosticSource" />.
    /// </para>
    /// </summary>
    public static readonly EventId ByteIdentityColumnWarning = MakeValidationId(Id.ByteIdentityColumnWarning);

    private static readonly string _scaffoldingPrefix = DbLoggerCategory.Scaffolding.Name + ".";
    private static EventId MakeScaffoldingId(Id id) => new EventId((int)id, _scaffoldingPrefix + id);

    /// <summary>
    ///   A column was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId ColumnFound = MakeScaffoldingId(Id.ColumnFound);

    /// <summary>
    ///   A default schema was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId DefaultSchemaFound = MakeScaffoldingId(Id.DefaultSchemaFound);

    /// <summary>
    ///   A type alias was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId TypeAliasFound = MakeScaffoldingId(Id.TypeAliasFound);

    /// <summary>
    ///   The database is missing a schema.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId MissingSchemaWarning = MakeScaffoldingId(Id.MissingSchemaWarning);

    /// <summary>
    ///   The database is missing a table.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId MissingTableWarning = MakeScaffoldingId(Id.MissingTableWarning);

    /// <summary>
    ///   A foreign key references a missing table at the principal end.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId ForeignKeyReferencesMissingPrincipalTableWarning = MakeScaffoldingId(Id.ForeignKeyReferencesMissingPrincipalTableWarning);

    /// <summary>
    ///   A table was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId TableFound = MakeScaffoldingId(Id.TableFound);

    /// <summary>
    ///   A sequence was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId SequenceFound = MakeScaffoldingId(Id.SequenceFound);

    /// <summary>
    ///   Primary key was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId PrimaryKeyFound = MakeScaffoldingId(Id.PrimaryKeyFound);

    /// <summary>
    ///   An unique constraint was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId UniqueConstraintFound = MakeScaffoldingId(Id.UniqueConstraintFound);

    /// <summary>
    ///   An index was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId IndexFound = MakeScaffoldingId(Id.IndexFound);

    /// <summary>
    ///   A foreign key was found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId ForeignKeyFound = MakeScaffoldingId(Id.ForeignKeyFound);

    /// <summary>
    ///   A principal column referenced by a foreign key was not found.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId ForeignKeyPrincipalColumnMissingWarning = MakeScaffoldingId(Id.ForeignKeyPrincipalColumnMissingWarning);

    /// <summary>
    ///   A reflexive foreign key constraint was skipped.
    ///   This event is in the <see cref="DbLoggerCategory.Scaffolding" /> category.
    /// </summary>
    public static readonly EventId ReflexiveConstraintIgnored = MakeScaffoldingId(Id.ReflexiveConstraintIgnored);
  }
}