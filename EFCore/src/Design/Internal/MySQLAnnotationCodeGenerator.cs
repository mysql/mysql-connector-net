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
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Utils;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Design.Internal
{
  internal class MySQLAnnotationCodeGenerator : AnnotationCodeGenerator
  {
    private static readonly MethodInfo _modelHasCharSetMethodInfo
      = typeof(MySQLModelBuilderExtensions).GetRequiredRuntimeMethod(
      nameof(MySQLModelBuilderExtensions.HasCharSet),
      typeof(ModelBuilder),
      typeof(string));

    private static readonly MethodInfo _modelUseCollationMethodInfo
      = typeof(MySQLModelBuilderExtensions).GetRequiredRuntimeMethod(
      nameof(MySQLModelBuilderExtensions.UseCollation),
      typeof(ModelBuilder), typeof(string));

    private static readonly MethodInfo _entityTypeHasCharSetMethodInfo
      = typeof(MySQLEntityTypeBuilderExtensions).GetRequiredRuntimeMethod(
      nameof(MySQLEntityTypeBuilderExtensions.HasCharSet),
      typeof(EntityTypeBuilder),
      typeof(string));

    private static readonly MethodInfo _entityTypeUseCollationMethodInfo
      = typeof(MySQLEntityTypeBuilderExtensions).GetRequiredRuntimeMethod(
      nameof(MySQLEntityTypeBuilderExtensions.UseCollation),
      typeof(EntityTypeBuilder),
      typeof(string));

    private static readonly MethodInfo _propertyHasCharSetMethodInfo
      = typeof(MySQLPropertyBuilderExtensions).GetRequiredRuntimeMethod(
      nameof(MySQLPropertyBuilderExtensions.ForMySQLHasCharset),
      typeof(PropertyBuilder),
      typeof(string));

    private static readonly MethodInfo _propertyHasCollationMethodInfo
      = typeof(MySQLPropertyBuilderExtensions).GetRequiredRuntimeMethod(
      nameof(MySQLPropertyBuilderExtensions.ForMySQLHasCollation),
      typeof(PropertyBuilder),
      typeof(string));

    public MySQLAnnotationCodeGenerator([NotNull] AnnotationCodeGeneratorDependencies dependencies)
      : base(dependencies)
    {
    }

    protected override bool IsHandledByConvention(IModel model, IAnnotation annotation)
    {
      return true;
    }

    protected override MethodCallCodeFragment? GenerateFluentApi(IModel model, IAnnotation annotation)
    {
      if (annotation.Name == MySQLAnnotationNames.Charset)
        return new MethodCallCodeFragment(_modelHasCharSetMethodInfo, annotation.Value);

      // EF Core currently just falls back on using the `Relational:Collation` annotation instead of generating the `UseCollation()`
      // method call (though it could), so we can return our method call fragment here, without generating an ugly duplicate.
      if (annotation.Name == RelationalAnnotationNames.Collation)
        return new MethodCallCodeFragment(_modelUseCollationMethodInfo, annotation.Value);

      return null;
    }

    protected override MethodCallCodeFragment? GenerateFluentApi(IEntityType entityType, IAnnotation annotation)
    {
      if (annotation.Name == MySQLAnnotationNames.Charset)
        return new MethodCallCodeFragment(_entityTypeHasCharSetMethodInfo, annotation.Value);

      if (annotation.Name == RelationalAnnotationNames.Collation)
        return new MethodCallCodeFragment(_entityTypeUseCollationMethodInfo, annotation.Value);

      return null;
    }

    public static MethodCallCodeFragment? GenFluentApi(IProperty property, IAnnotation annotation)
    {
      Check.NotNull(property, nameof(property));
      Check.NotNull(annotation, nameof(annotation));

      return annotation.Name switch
      {
        MySQLAnnotationNames.Charset when annotation.Value is string { Length: > 0 } charSet => new MethodCallCodeFragment(_propertyHasCharSetMethodInfo, charSet),
        MySQLAnnotationNames.Collation when annotation.Value is string { Length: > 0 } collation => new MethodCallCodeFragment(_propertyHasCollationMethodInfo, collation),
        _ => null,
      };
    }
  }
}
