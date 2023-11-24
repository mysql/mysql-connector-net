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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Metadata.Internal
{
  internal class MySQLAnnotationProvider : RelationalAnnotationProvider
  {
    [NotNull] private readonly IMySQLOptions _options;

    public MySQLAnnotationProvider(
        [NotNull] RelationalAnnotationProviderDependencies dependencies,
        [NotNull] IMySQLOptions options)
        : base(dependencies)
    {
      _options = options;
    }

    public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
    {
      if (!designTime)
      {
        yield break;
      }

      if (GetActualModelCharSet(model.Model) is string charSet)
      {
        yield return new Annotation(MySQLAnnotationNames.Charset, charSet);
      }
    }

    public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
    {
      if (!designTime)
      {
        yield break;
      }
#if !NET8_0
      var entityType = table.EntityTypeMappings.First().EntityType;

      if (GetActualEntityTypeCharSet(entityType) is string charSet)
        yield return new Annotation(MySQLAnnotationNames.Charset, charSet);

      if (GetActualEntityTypeCollation(entityType) is string collation)
        yield return new Annotation(RelationalAnnotationNames.Collation, collation);

      foreach (var annotation in entityType.GetAnnotations().Where(a => a.Name is MySQLAnnotationNames.StoreOptions))
        yield return annotation;
#else
      var typeBase = table.EntityTypeMappings.First().TypeBase;

      if (GetActualTypeBaseCharSet(typeBase) is string charSet)
        yield return new Annotation(MySQLAnnotationNames.Charset, charSet);

      if (GetActualTypeBaseCollation(typeBase) is string collation)
        yield return new Annotation(RelationalAnnotationNames.Collation, collation);

      foreach (var annotation in typeBase.GetAnnotations().Where(a => a.Name is MySQLAnnotationNames.StoreOptions))
        yield return annotation;
#endif
    }

    public override IEnumerable<IAnnotation> For(IUniqueConstraint constraint, bool designTime)
    {
      if (!designTime)
        yield break;

      var key = constraint.MappedKeys.First();

      var prefixLength = key.PrefixLength();
      if (prefixLength != null && prefixLength.Length > 0)
        yield return new Annotation(MySQLAnnotationNames.IndexPrefixLength, prefixLength);
    }

    public override IEnumerable<IAnnotation> For(ITableIndex index, bool designTime)
    {
      if (!designTime)
        yield break;

      var modelIndex = index.MappedIndexes.First();

      var prefixLength = modelIndex.PrefixLength();
      if (prefixLength != null && prefixLength.Length > 0)
        yield return new Annotation(MySQLAnnotationNames.IndexPrefixLength, prefixLength);

      var isFullText = modelIndex.IsFullText();
      if (isFullText.HasValue)
        yield return new Annotation(MySQLAnnotationNames.FullTextIndex, isFullText.Value);

      var fullTextParser = modelIndex.FullTextParser();
      if (!string.IsNullOrEmpty(fullTextParser))
        yield return new Annotation(MySQLAnnotationNames.FullTextParser, fullTextParser);

      var isSpatial = modelIndex.IsSpatial();
      if (isSpatial.HasValue)
        yield return new Annotation(MySQLAnnotationNames.SpatialIndex, isSpatial.Value);
    }

#if NET6_0
    public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
    {
      if (!designTime)
        yield break;

      var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
      var properties = column.PropertyMappings.Select(m => m.Property).ToArray();

      if (column.PropertyMappings.Where(
        m => m.TableMapping.IsSharedTablePrincipal && m.TableMapping.EntityType == m.Property.DeclaringEntityType)
        .Select(m => m.Property)
        .FirstOrDefault(p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.IdentityColumn) is IProperty identityProperty)
      {
        var valueGenerationStrategy = identityProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
      else if (properties.FirstOrDefault
        (p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.ComputedColumn) is IProperty computedProperty)
      {
        var valueGenerationStrategy = computedProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
    }
#elif NET7_0
      public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
    {
      if (!designTime)
        yield break;

      var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
      var properties = column.PropertyMappings.Select(m => m.Property).ToArray();

      if (column.PropertyMappings.Where(m => m.TableMapping.IsSharedTablePrincipal ?? true
      && m.TableMapping.EntityType == m.Property.DeclaringEntityType)
        .Select(m => m.Property)
        .FirstOrDefault(p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.IdentityColumn) is IProperty identityProperty)
      {
        var valueGenerationStrategy = identityProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
      else if (properties.FirstOrDefault
        (p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.ComputedColumn) is IProperty computedProperty)
      {
        var valueGenerationStrategy = computedProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
    }

#elif NET8_0

public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
    {
      if (!designTime)
        yield break;

      var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
      var properties = column.PropertyMappings.Select(m => m.Property).ToArray();

      if (column.PropertyMappings.Where(m => m.TableMapping.IsSharedTablePrincipal ?? true
      && m.TableMapping.TypeBase == m.Property.DeclaringEntityType)
        .Select(m => m.Property)
        .FirstOrDefault(p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.IdentityColumn) is IProperty identityProperty)
      {
        var valueGenerationStrategy = identityProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
      else if (properties.FirstOrDefault
        (p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.ComputedColumn) is IProperty computedProperty)
      {
        var valueGenerationStrategy = computedProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
    }
#endif


    protected virtual string GetActualModelCharSet(IModel model)
    {
      return model.GetCharSet() is null && model.GetCollation() is null ? _options.CharSet.name : model.GetCharSet()!;
    }

    protected virtual string GetActualModelCollation(IModel model)
    {
      return model.GetCollation()!;
    }


#if !NET8_0
    protected virtual string? GetActualEntityTypeCharSet(IEntityType entityType)
    {
      var entityTypeCharSet = entityType.GetCharSet();

      if (entityTypeCharSet is not null)
        return entityTypeCharSet;

      if (entityTypeCharSet is null)
      {
        var entityTypeCollation = entityType.GetCollation();
        var actualModelCharSet = GetActualModelCharSet(entityType.Model);

        if (entityTypeCollation is not null)
        {
          return actualModelCharSet is not null && entityTypeCollation.StartsWith(actualModelCharSet, StringComparison.OrdinalIgnoreCase)
            ? actualModelCharSet : null;
        }

        var actualModelCollation = GetActualModelCollation(entityType.Model);

        if (actualModelCollation is not null)
        {
          return actualModelCharSet is not null && actualModelCollation.StartsWith(actualModelCharSet, StringComparison.OrdinalIgnoreCase)
            ? actualModelCharSet : null;
        }

        return actualModelCharSet;
      }

      return null;
    }

    protected virtual string? GetActualEntityTypeCollation(IEntityType entityType)
    {
      var entityTypeCollation = entityType.GetCollation();

      if (entityTypeCollation is not null)
        return entityTypeCollation;

      if (entityTypeCollation is null)
      {
        var entityTypeCharSet = entityType.GetCharSet();
        var actualModelCollation = GetActualModelCollation(entityType.Model);

        if (entityTypeCharSet is not null)
        {
          return actualModelCollation is not null && actualModelCollation.StartsWith(entityTypeCharSet, StringComparison.OrdinalIgnoreCase)
            ? actualModelCollation : null;
        }

        return actualModelCollation;
      }

      return null;
    }

    protected virtual string? GetActualPropertyCharSet(IProperty[] properties)
    {
      return properties.Select(p => p.GetCharSet()).FirstOrDefault(s => s is not null) ??
        properties.Select(
          p => p.FindTypeMapping() is MySQLStringTypeMapping
          ? GetActualEntityTypeCharSet(p.DeclaringEntityType) is string charSet &&
          (p.GetCollation() is not string collation ||
          collation.StartsWith(charSet, StringComparison.OrdinalIgnoreCase))
          ? charSet
          : null
          : null)
        .FirstOrDefault(s => s is not null);
    }
#else
    protected virtual string? GetActualTypeBaseCharSet(ITypeBase typeBase)
    {
      var typeBaseCharSet = typeBase.GetCharSet();

      if (typeBaseCharSet is not null)
        return typeBaseCharSet;

      if (typeBaseCharSet is null)
      {
        var typeBaseCollation = typeBase.GetCollation();
        var actualModelCharSet = GetActualModelCharSet(typeBase.Model);

        if (typeBaseCollation is not null)
        {
          return actualModelCharSet is not null && typeBaseCollation.StartsWith(actualModelCharSet, StringComparison.OrdinalIgnoreCase)
            ? actualModelCharSet : null;
        }

        var actualModelCollation = GetActualModelCollation(typeBase.Model);

        if (actualModelCollation is not null)
        {
          return actualModelCharSet is not null && actualModelCollation.StartsWith(actualModelCharSet, StringComparison.OrdinalIgnoreCase)
            ? actualModelCharSet : null;
        }

        return actualModelCharSet;
      }

      return null;
    }

    protected virtual string? GetActualTypeBaseCollation(ITypeBase typeBase)
    {
      var typeBaseCollation = typeBase.GetCollation();

      if (typeBaseCollation is not null)
        return typeBaseCollation;

      if (typeBaseCollation is null)
      {
        var typeBaseCharSet = typeBase.GetCharSet();
        var actualModelCollation = GetActualModelCollation(typeBase.Model);

        if (typeBaseCharSet is not null)
        {
          return actualModelCollation is not null && actualModelCollation.StartsWith(typeBaseCharSet, StringComparison.OrdinalIgnoreCase)
            ? actualModelCollation : null;
        }

        return actualModelCollation;
      }

      return null;
    }

    protected virtual string? GetActualPropertyCharSet(IProperty[] properties)
    {
      return properties.Select(p => p.GetCharSet()).FirstOrDefault(s => s is not null) ??
        properties.Select(
          p => p.FindTypeMapping() is MySQLStringTypeMapping
          ? GetActualTypeBaseCharSet(p.DeclaringType) is string charSet &&
          (p.GetCollation() is not string collation ||
          collation.StartsWith(charSet, StringComparison.OrdinalIgnoreCase))
          ? charSet
          : null
          : null)
        .FirstOrDefault(s => s is not null);
    }
#endif

  }
}
