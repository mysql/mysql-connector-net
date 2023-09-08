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
using System;
using System.Globalization;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   Provides CLR methods that are translated to database functions when used in a LINQ to Entities queries.
  ///   The methods in this class are accessed with <see cref="EF.Functions" />.
  /// </summary>
  public static class MySQLDbFunctionsExtensions
  {
    /// <summary>
    ///   Counts the number of year boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(YEAR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of year boundaries crossed between the dates.</returns>
    public static int DateDiffYear(
      [CanBeNull] this DbFunctions _,
      DateTime startDate,
      DateTime endDate)
      => endDate.Year - startDate.Year;

    /// <summary>
    ///   Counts the number of year boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(YEAR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of year boundaries crossed between the dates.</returns>
    public static int? DateDiffYear(
      [CanBeNull] this DbFunctions _,
      DateTime? startDate,
      DateTime? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffYear(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of year boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(YEAR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of year boundaries crossed between the dates.</returns>
    public static int DateDiffYear(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset startDate,
      DateTimeOffset endDate)
      => DateDiffYear(_, startDate.UtcDateTime, endDate.UtcDateTime);

    /// <summary>
    ///   Counts the number of year boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(YEAR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of year boundaries crossed between the dates.</returns>
    public static int? DateDiffYear(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset? startDate,
      DateTimeOffset? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffYear(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///     Counts the number of year boundaries crossed between the startDate and endDate.
    ///     Corresponds to TIMESTAMPDIFF(YEAR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of year boundaries crossed between the dates.</returns>
    public static int DateDiffYear(
        [CanBeNull] this DbFunctions _,
        DateOnly startDate,
        DateOnly endDate)
        => endDate.Year - startDate.Year;

    /// <summary>
    ///     Counts the number of year boundaries crossed between the startDate and endDate.
    ///     Corresponds to TIMESTAMPDIFF(YEAR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of year boundaries crossed between the dates.</returns>
    public static int? DateDiffYear(
        [CanBeNull] this DbFunctions _,
        DateOnly? startDate,
        DateOnly? endDate)
        => (startDate.HasValue && endDate.HasValue)
            ? (int?)DateDiffYear(_, startDate.Value, endDate.Value)
            : null;

    /// <summary>
    ///   Counts the number of month boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MONTH,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of month boundaries crossed between the dates.</returns>
    public static int DateDiffMonth(
      [CanBeNull] this DbFunctions _,
      DateTime startDate,
      DateTime endDate)
      => 12 * (endDate.Year - startDate.Year) + endDate.Month - startDate.Month;

    /// <summary>
    ///   Counts the number of month boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MONTH,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of month boundaries crossed between the dates.</returns>
    public static int? DateDiffMonth(
      [CanBeNull] this DbFunctions _,
      DateTime? startDate,
      DateTime? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffMonth(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of month boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MONTH,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of month boundaries crossed between the dates.</returns>
    public static int DateDiffMonth(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset startDate,
      DateTimeOffset endDate)
      => DateDiffMonth(_, startDate.UtcDateTime, endDate.UtcDateTime);

    /// <summary>
    ///   Counts the number of month boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MONTH,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of month boundaries crossed between the dates.</returns>
    public static int? DateDiffMonth(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset? startDate,
      DateTimeOffset? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffMonth(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///     Counts the number of month boundaries crossed between the startDate and endDate.
    ///     Corresponds to TIMESTAMPDIFF(MONTH,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of month boundaries crossed between the dates.</returns>
    public static int DateDiffMonth(
        [CanBeNull] this DbFunctions _,
        DateOnly startDate,
        DateOnly endDate)
        => 12 * (endDate.Year - startDate.Year) + endDate.Month - startDate.Month;

    /// <summary>
    ///     Counts the number of month boundaries crossed between the startDate and endDate.
    ///     Corresponds to TIMESTAMPDIFF(MONTH,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of month boundaries crossed between the dates.</returns>
    public static int? DateDiffMonth(
        [CanBeNull] this DbFunctions _,
        DateOnly? startDate,
        DateOnly? endDate)
        => (startDate.HasValue && endDate.HasValue)
            ? (int?)DateDiffMonth(_, startDate.Value, endDate.Value)
            : null;

    /// <summary>
    ///   Counts the number of day boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(DAY,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of day boundaries crossed between the dates.</returns>
    public static int DateDiffDay(
      [CanBeNull] this DbFunctions _,
      DateTime startDate,
      DateTime endDate)
      => (endDate.Date - startDate.Date).Days;

    /// <summary>
    ///   Counts the number of day boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(DAY,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of day boundaries crossed between the dates.</returns>
    public static int? DateDiffDay(
      [CanBeNull] this DbFunctions _,
      DateTime? startDate,
      DateTime? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffDay(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of day boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(DAY,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of day boundaries crossed between the dates.</returns>
    public static int DateDiffDay(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset startDate,
      DateTimeOffset endDate)
      => DateDiffDay(_, startDate.UtcDateTime, endDate.UtcDateTime);

    /// <summary>
    ///   Counts the number of day boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(DAY,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of day boundaries crossed between the dates.</returns>
    public static int? DateDiffDay(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset? startDate,
      DateTimeOffset? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffDay(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///     Counts the number of day boundaries crossed between the startDate and endDate.
    ///     Corresponds to TIMESTAMPDIFF(DAY,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of day boundaries crossed between the dates.</returns>
    public static int DateDiffDay(
        [CanBeNull] this DbFunctions _,
        DateOnly startDate,
        DateOnly endDate)
        => endDate.Day - startDate.Day;

    /// <summary>
    ///     Counts the number of day boundaries crossed between the startDate and endDate.
    ///     Corresponds to TIMESTAMPDIFF(DAY,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of day boundaries crossed between the dates.</returns>
    public static int? DateDiffDay(
        [CanBeNull] this DbFunctions _,
        DateOnly? startDate,
        DateOnly? endDate)
        => (startDate.HasValue && endDate.HasValue)
            ? (int?)DateDiffDay(_, startDate.Value, endDate.Value)
            : null;

    /// <summary>
    ///   Counts the number of hour boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(HOUR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of hour boundaries crossed between the dates.</returns>
    public static int DateDiffHour(
      [CanBeNull] this DbFunctions _,
      DateTime startDate,
      DateTime endDate)
    {
      checked
      {
        return DateDiffDay(_, startDate, endDate) * 24 + endDate.Hour - startDate.Hour;
      }
    }

    /// <summary>
    ///   Counts the number of hour boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(HOUR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of hour boundaries crossed between the dates.</returns>
    public static int? DateDiffHour(
      [CanBeNull] this DbFunctions _,
      DateTime? startDate,
      DateTime? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffHour(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of hour boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(HOUR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of hour boundaries crossed between the dates.</returns>
    public static int DateDiffHour(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset startDate,
      DateTimeOffset endDate)
      => DateDiffHour(_, startDate.UtcDateTime, endDate.UtcDateTime);

    /// <summary>
    ///   Counts the number of hour boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(HOUR,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of hour boundaries crossed between the dates.</returns>
    public static int? DateDiffHour(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset? startDate,
      DateTimeOffset? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffHour(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of minute boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MINUTE,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of minute boundaries crossed between the dates.</returns>
    public static int DateDiffMinute(
      [CanBeNull] this DbFunctions _,
      DateTime startDate,
      DateTime endDate)
    {
      checked
      {
        return DateDiffHour(_, startDate, endDate) * 60 + endDate.Minute - startDate.Minute;
      }
    }

    /// <summary>
    ///   Counts the number of minute boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MINUTE,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of minute boundaries crossed between the dates.</returns>
    public static int? DateDiffMinute(
      [CanBeNull] this DbFunctions _,
      DateTime? startDate,
      DateTime? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffMinute(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of minute boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MINUTE,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of minute boundaries crossed between the dates.</returns>
    public static int DateDiffMinute(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset startDate,
      DateTimeOffset endDate)
      => DateDiffMinute(_, startDate.UtcDateTime, endDate.UtcDateTime);

    /// <summary>
    ///   Counts the number of minute boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MINUTE,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of minute boundaries crossed between the dates.</returns>
    public static int? DateDiffMinute(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset? startDate,
      DateTimeOffset? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffMinute(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of boundaries (in seconds) crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(SECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of boundaries (in seconds) crossed between the dates.</returns>
    public static int DateDiffSecond(
      [CanBeNull] this DbFunctions _,
      DateTime startDate,
      DateTime endDate)
    {
      checked
      {
        return DateDiffMinute(_, startDate, endDate) * 60 + endDate.Second - startDate.Second;
      }
    }

    /// <summary>
    ///   Counts the number of boundaries (in seconds) crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(SECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of boundaries (in seconds) crossed between the dates.</returns>
    public static int? DateDiffSecond(
      [CanBeNull] this DbFunctions _,
      DateTime? startDate,
      DateTime? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffSecond(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of boundaries (in seconds) crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(SECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of boundaries (in seconds) crossed between the dates.</returns>
    public static int DateDiffSecond(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset startDate,
      DateTimeOffset endDate)
      => DateDiffSecond(_, startDate.UtcDateTime, endDate.UtcDateTime);

    /// <summary>
    ///   Counts the number of boundaries (in seconds) crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(SECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of boundaries (in seconds) crossed between the dates.</returns>
    public static int? DateDiffSecond(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset? startDate,
      DateTimeOffset? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffSecond(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of microsecond boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MICROSECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of microsecond boundaries crossed between the dates.</returns>
    public static int DateDiffMicrosecond(
      [CanBeNull] this DbFunctions _,
      DateTime startDate,
      DateTime endDate)
    {
      checked
      {
        return (int)((endDate.Ticks - startDate.Ticks) / 10);
      }
    }

    /// <summary>
    ///   Counts the number of microsecond boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MICROSECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of microsecond boundaries crossed between the dates.</returns>
    public static int? DateDiffMicrosecond(
      [CanBeNull] this DbFunctions _,
      DateTime? startDate,
      DateTime? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffMicrosecond(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   Counts the number of microsecond boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MICROSECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of microsecond boundaries crossed between the dates.</returns>
    public static int DateDiffMicrosecond(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset startDate,
      DateTimeOffset endDate)
      => DateDiffMicrosecond(_, startDate.UtcDateTime, endDate.UtcDateTime);

    /// <summary>
    ///   Counts the number of microsecond boundaries crossed between the startDate and endDate.
    ///   Corresponds to TIMESTAMPDIFF(MICROSECOND,startDate,endDate).
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="startDate">Starting date for the calculation.</param>
    /// <param name="endDate">Ending date for the calculation.</param>
    /// <returns>Number of microsecond boundaries crossed between the dates.</returns>
    public static int? DateDiffMicrosecond(
      [CanBeNull] this DbFunctions _,
      DateTimeOffset? startDate,
      DateTimeOffset? endDate)
      => (startDate.HasValue && endDate.HasValue)
      ? (int?)DateDiffMicrosecond(_, startDate.Value, endDate.Value)
      : null;

    /// <summary>
    ///   <para>
    ///   An implementation of the SQL LIKE operation. In relational databases this is usually directly
    ///   translated to SQL.
    ///   </para>
    ///   <para>
    ///   Note that if this function is translated into SQL, then the semantics of the comparison 
    ///   depends on the database configuration. In particular, it may be either case-sensitive or
    ///   case-insensitive. If this function is evaluated on the client, then it always uses
    ///   a case-insensitive comparison.
    ///   </para>
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="matchExpression">The property of entity that is to be matched.</param>
    /// <param name="pattern">A pattern that may involve wildcards %,_,[,],^.</param>
    /// <returns><see langword="true"/> if there is a match; otherwise, <see langword="false"/>.</returns>
    public static bool Like<T>(
      this DbFunctions _,
      T matchExpression,
      string pattern)
      => LikeCore(matchExpression, pattern, null);

    /// <summary>
    ///   <para>
    ///   An implementation of the SQL LIKE operation. In relational databases, this is usually directly
    ///   translated to SQL.
    ///   </para>
    ///   <para>
    ///   Note that if this function is translated into SQL, then the semantics of the comparison
    ///   depends on the database configuration. In particular, it may be either case-sensitive or
    ///   case-insensitive. If this function is evaluated on the client, then it always uses
    ///   a case-insensitive comparison.
    ///   </para>
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="matchExpression">The property of entity that is to be matched.</param>
    /// <param name="pattern">A pattern that may involve wildcards %,_,[,],^.</param>
    /// <param name="escapeCharacter">
    ///   The escape character (as a single character string) to use in front of %,_,[,],^
    ///   if they are not used as wildcards.
    /// </param>
    /// <returns><see langword="true"/> if there is a match; otherwise, <see langword="false"/>.</returns>
    public static bool Like<T>(
      this DbFunctions _,
      T matchExpression,
      string pattern,
      string? escapeCharacter)
      => LikeCore(matchExpression, pattern, escapeCharacter);

    private static bool LikeCore<T>(T matchExpression, string pattern, string? escapeCharacter)
    {
      if (matchExpression is IConvertible convertible)
      {
        return EF.Functions.Like(convertible.ToString(CultureInfo.InvariantCulture), pattern, escapeCharacter);
      }

      if (matchExpression is byte[] byteArray)
      {
        var value = BitConverter.ToString(byteArray);
        return EF.Functions.Like(value.Replace("-", string.Empty), pattern, escapeCharacter);
      }

      return false;
    }

    /// <summary>
    ///   <para>
    ///   An implementation of the SQL MATCH function used to perform a natural language search for a string against a text collection.
    ///   A collection is a set of one or more columns included in a FULLTEXT index.
    ///   </para>
    ///   <para>
    ///   MATCH (col1,col2,...) AGAINST (expr [search_modifier])
    ///   </para>
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="properties">The columns of the entity that is to be matched.</param>
    /// <param name="pattern">A pattern that may involve wildcards %,_,[,],^.</param>
    /// <param name="searchMode">
    ///   Indicates what type of search to perform
    /// </param>
    /// <returns><see langword="true"/> if there is a match; otherwise, <see langword="false"/>.</returns>
    public static bool Match(
      [CanBeNull] this DbFunctions _,
      [NotNull] string[] properties,
      [CanBeNull] string pattern,
      MySQLMatchSearchMode searchMode)
      => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Match)));
  }
}
