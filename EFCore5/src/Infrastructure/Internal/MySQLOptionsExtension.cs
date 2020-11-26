// Copyright (c) 2020 Oracle and/or its affiliates.
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

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Extensions;
using MySql.Data.MySqlClient;

namespace MySql.EntityFrameworkCore.Infrastructure.Internal
{
  /// <summary>
  /// Represents the <see cref="RelationalOptionsExtension"/> implementation for MySQL.
  /// </summary>
  public class MySQLOptionsExtension : RelationalOptionsExtension
  {
    private DbContextOptionsExtensionInfo _info;
    private CharacterSet? _charset;

    public MySQLOptionsExtension()
    {
    }

    public MySQLOptionsExtension(MySQLOptionsExtension copyFrom)
      : base(copyFrom)
    {
      _charset = copyFrom._charset;
    }

    //public CharacterSet CharSet { get; private set; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override DbContextOptionsExtensionInfo Info
    => _info ??= new ExtensionInfo(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalOptionsExtension Clone()
  => new MySQLOptionsExtension(this);

    public virtual CharacterSet? CharSet => _charset;
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public MySQLOptionsExtension WithCharSet(CharacterSet charSet)
    {
      var clone = (MySQLOptionsExtension)Clone();

      clone._charset = charSet;

      return clone;
    }

    /// <summary>
    ///     This method supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This method may change or be removed in future releases.
    /// </summary>
    public override void ApplyServices(IServiceCollection services)
      => services.AddEntityFrameworkMySQL();

    private sealed class ExtensionInfo : RelationalExtensionInfo
    {
      private long? _serviceProviderHash;
      private string _logFragment;

      public ExtensionInfo(IDbContextOptionsExtension extension)
          : base(extension)
      {
      }

      private new MySQLOptionsExtension Extension
          => (MySQLOptionsExtension)base.Extension;

      public override bool IsDatabaseProvider => true;

      public override string LogFragment
      {
        get
        {
          if (_logFragment == null)
          {
            var builder = new StringBuilder();

            builder.Append(base.LogFragment);

            _logFragment = builder.ToString();
          }

          return _logFragment;
        }
      }

      public override long GetServiceProviderHashCode()
      {
        if (_serviceProviderHash == null)
        {
          _serviceProviderHash = (base.GetServiceProviderHashCode() * 397) ^ (Extension.CharSet?.GetHashCode() ?? 0L);
        }

        return _serviceProviderHash.Value;
      }

      public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        => debugInfo["MySQL:" + nameof(MySQLDbContextOptionsBuilder.CharSet)]
                    = (Extension.CharSet?.GetHashCode() ?? 0L).ToString(CultureInfo.InvariantCulture);
    }
  }
}