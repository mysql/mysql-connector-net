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

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MySql.EntityFrameworkCore.Infrastructure.Internal
{
  /// <summary>
  ///   Represents the <see cref="RelationalOptionsExtension"/> implementation for MySQL.
  /// </summary>
  public class MySQLOptionsExtension : RelationalOptionsExtension
  {
    private DbContextOptionsExtensionInfo? _info;
    private CharacterSet? _charset;

    /// <summary>
    /// Constructor of <see cref="MySQLOptionsExtension"/>.
    /// </summary>
    public MySQLOptionsExtension()
    {
    }

    /// <summary>
    /// Creates a <see cref="MySQLOptionsExtension"/> based on another <see cref="MySQLOptionsExtension"/>.
    /// </summary>
    /// <param name="copyFrom">The <see cref="MySQLOptionsExtension"/> to copy.</param>
    public MySQLOptionsExtension(MySQLOptionsExtension copyFrom)
      : base(copyFrom)
    {
      _charset = copyFrom._charset;
    }

    /// <summary>
    /// Information/metadata about the extension.
    /// </summary>
    /// <value>The information/metadata.</value>
    public override DbContextOptionsExtensionInfo Info
    => _info ??= new ExtensionInfo(this);

    /// <summary>
    /// Clones a <see cref="MySQLOptionsExtension"/> object.
    /// </summary>
    /// <returns>A clone of this instance, which can be modified before being returned as immutable.</returns>
    protected override RelationalOptionsExtension Clone()
    => new MySQLOptionsExtension(this);

    /// <summary>
    /// The <see cref="CharacterSet"/> to use.
    /// </summary>
    /// <value>The character set.</value>
    public virtual CharacterSet? CharSet => _charset;

    /// <summary>
    ///   Returns a copy of the current instance configured with the specified character set.
    /// </summary>
    /// <param name="charSet">The <see cref="CharacterSet"/> to use.</param>
    /// <returns>A <see cref="MySQLOptionsExtension"/> object with the specified <paramref name="charSet"/>.</returns>
    public MySQLOptionsExtension WithCharSet(CharacterSet charSet)
    {
      var clone = (MySQLOptionsExtension)Clone();

      clone._charset = charSet;

      return clone;
    }

    /// <summary>
    /// Adds the services required to make the selected options work.
    /// </summary>
    /// <param name="services">The collection to add services to.</param>
    public override void ApplyServices(IServiceCollection services)
      => services.AddEntityFrameworkMySQL();

    private sealed class ExtensionInfo : RelationalExtensionInfo
    {
      private int? _serviceProviderHash;
      private string? _logFragment;

      public ExtensionInfo(IDbContextOptionsExtension extension)
        : base(extension)
      {
      }

      private new MySQLOptionsExtension Extension
        => (MySQLOptionsExtension)base.Extension;

      /// <inheritdoc />
      public override bool IsDatabaseProvider => true;

      /// <inheritdoc />
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

      /// <inheritdoc />
      public override int GetServiceProviderHashCode()
      {
        if (_serviceProviderHash == null)
        {
          _serviceProviderHash = (base.GetServiceProviderHashCode() * 397) ^ (Extension.CharSet?.GetHashCode() ?? 0);
        }

        return _serviceProviderHash.Value;
      }

      /// <inheritdoc />
      public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
      => debugInfo["MySQL:" + nameof(MySQLDbContextOptionsBuilder.CharSet)]
            = (Extension.CharSet?.GetHashCode() ?? 0L).ToString(CultureInfo.InvariantCulture);
    }
  }
}