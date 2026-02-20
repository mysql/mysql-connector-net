// Copyright © 2020, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
      public override string PluginName => "mysql_clear_password";

      /// <summary>
      /// Processes additional data during the mysql_clear_password authentication handshake.
      /// This method sends the password in clear text over secure connections (SSL or Unix socket).
      /// </summary>
      /// <param name="data">The byte array received from the server.</param>
      /// <returns>A byte array containing the clear text password to send to the server.</returns>
      /// <exception cref="MySqlException">Thrown if the connection is not secure.</exception>
      protected override byte[] MoreData(byte[] data) => GetClearPasswordBytes();

      /// <summary>
      /// Asynchronously processes additional data during the mysql_clear_password authentication handshake.
      /// This method sends the password in clear text over secure connections (SSL or Unix socket).
      /// </summary>
      /// <param name="data">The byte array received from the server.</param>
      /// <returns>A task representing the asynchronous operation, containing the clear text password to send to the server.</returns>
      /// <exception cref="MySqlException">Thrown if the connection is not secure.</exception>
      protected override Task<byte[]> MoreDataAsync(byte[] data) => Task.FromResult(GetClearPasswordBytes());

      /// <summary>
      /// Gets the clear text password bytes if the connection is secure, otherwise throws MySqlException.
      /// </summary>
      /// <returns>The UTF8-encoded password bytes.</returns>
      /// <exception cref="MySqlException">Thrown if the connection is not secure (no SSL or Unix socket).</exception>
      private byte[] GetClearPasswordBytes()
      {
        if ((Settings.SslMode != MySqlSslMode.Disabled &&
        Settings.ConnectionProtocol != MySqlConnectionProtocol.UnixSocket) ||
        (Settings.ConnectionProtocol == MySqlConnectionProtocol.UnixSocket))
        {
          return System.Text.Encoding.UTF8.GetBytes(GetMFAPassword());
        }
        else
        {
          throw new MySqlException(Resources.ClearPasswordNotSupported);
        }
      }
  }
}
