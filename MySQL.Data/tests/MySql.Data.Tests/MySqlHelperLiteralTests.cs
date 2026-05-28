// Copyright (c) 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
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

using NUnit.Framework;
using System.Text;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlHelperLiteralTests
  {
    /// <summary>
    /// Verifies binary literals use the legacy quoted form when backslash escaping is enabled.
    /// </summary>
    [Test]
    public void BinaryLiteralUsesLegacyEscapingByDefault()
    {
      MySqlPacket packet = new MySqlPacket(Encoding.ASCII);
      byte[] value = new byte[] { 0x00, (byte)'\'', (byte)'\"', (byte)'\\' };

      MySqlHelper.WriteBinaryLiteral(packet, value, value.Length);

      Assert.That(GetPacketPayload(packet), Is.EqualTo("_binary '" + "\\0" + "\\'" + "\\\"" + "\\\\" + "'"));
    }

    /// <summary>
    /// Verifies binary literals use a binary introducer with hexadecimal bytes when backslash escaping is disabled.
    /// </summary>
    [Test]
    public void BinaryLiteralUsesHexWithBinaryIntroducerWhenNoBackslashEscapes()
    {
      MySqlPacket packet = new MySqlPacket(Encoding.ASCII)
      {
        NoBackslashEscapes = true
      };
      byte[] value = new byte[] { 0x00, (byte)'\'', (byte)'\"', (byte)'\\', 0xFF };

      MySqlHelper.WriteBinaryLiteral(packet, value, value.Length);

      Assert.That(GetPacketPayload(packet), Is.EqualTo("_binary X'0027225CFF'"));
    }

    private static string GetPacketPayload(MySqlPacket packet)
    {
      return Encoding.ASCII.GetString(packet.Buffer, 4, packet.Length - 4);
    }
  }
}
