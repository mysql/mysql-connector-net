// Copyright (c) 2020, 2022, Oracle and/or its affiliates.
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

using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// Allows connections to a user account set with the mysql_clear_password authentication plugin.
  /// </summary>
  public class MySqlClearPasswordPlugin : MySqlAuthenticationPlugin
  {
    private byte[] passBytes;
    public override string PluginName => "mysql_clear_password";
    protected override Task<byte[]> MoreDataAsync(byte[] data, bool execAsync)
    {
      if ((Settings.SslMode != MySqlSslMode.Disabled &&
      Settings.ConnectionProtocol != MySqlConnectionProtocol.UnixSocket) ||
      (Settings.ConnectionProtocol == MySqlConnectionProtocol.UnixSocket))
      {
        passBytes = System.Text.Encoding.UTF8.GetBytes(GetMFAPassword());
        return Task.FromResult<byte[]>(passBytes);
      }
      else
      {
        throw new MySqlException(Resources.ClearPasswordNotSupported);
      }
    }
  }
}
