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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Class that represents the response OK Packet
  /// https://dev.mysql.com/doc/internals/en/packet-OK_Packet.html
  /// </summary>
  internal class OkPacket
  {
    internal long AffectedRows { get; private set; }
    internal long LastInsertId { get; private set; }
    internal ServerStatusFlags ServerStatusFlags { get; private set; }
    internal int WarningCount { get; private set; }
    internal string Info { get; private set; }
    internal List<SessionTracker> SessionTrackers { get; private set; }

    /// <summary>
    /// Creates an instance of the OKPacket object with all of its metadata
    /// </summary>
    /// <param name="packet">The packet to parse</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    public static async Task<OkPacket> CreateAsync(MySqlPacket packet, bool execAsync)
    {
      OkPacket okPacket = new OkPacket();
      await okPacket.InitializeAsync(packet, execAsync).ConfigureAwait(false);
      return okPacket;
    }

    private OkPacket() { }

    private async Task InitializeAsync(MySqlPacket packet, bool execAsync)
    {
      AffectedRows = packet.ReadFieldLength(); // affected rows
      LastInsertId = packet.ReadFieldLength(); // last insert-id
      ServerStatusFlags = (ServerStatusFlags)packet.ReadInteger(2); // status flags
      WarningCount = packet.ReadInteger(2); // warning count
      Info = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false); // info
      SessionTrackers = new List<SessionTracker>();

      if ((ServerStatusFlags & ServerStatusFlags.SessionStateChanged) != 0)
      {
        int totalLen = packet.ReadPackedInteger();
        int start = packet.Position;
        int end = start + totalLen;

        while (totalLen > 0 && end > start)
        {
          SessionTrackType type = (SessionTrackType)packet.ReadByte();
          int dataLength = (int)packet.ReadByte();
          string name, value;

          // for specification of the packet structure, see WL#4797
          switch (type)
          {
            case SessionTrackType.SystemVariables:
              name = await packet.ReadStringAsync(packet.ReadByte(), execAsync).ConfigureAwait(false);
              value = await packet.ReadStringAsync(packet.ReadByte(), execAsync).ConfigureAwait(false);
              AddTracker(type, name, value);
              break;
            case SessionTrackType.GTIDS:
              packet.ReadByte(); // skip the byte reserved for the encoding specification, see WL#6128 
              name = await packet.ReadStringAsync(packet.ReadByte(), execAsync).ConfigureAwait(false);
              AddTracker(type, name, null);
              break;
            case SessionTrackType.Schema:
            case SessionTrackType.TransactionCharacteristics:
            case SessionTrackType.TransactionState:
              name = await packet.ReadStringAsync(packet.ReadByte(), execAsync).ConfigureAwait(false);
              AddTracker(type, name, null);
              break;
            case SessionTrackType.StateChange:
            default:
              AddTracker(type, packet.ReadString(), null);
              break;
          }

          start = packet.Position;
        }
      }
    }

    /// <summary>
    /// Add a session tracker to the list
    /// </summary>
    /// <param name="type">Type of the session tracker</param>
    /// <param name="name">Name of the element changed</param>
    /// <param name="value">Value of the changed system variable (only for SessionTrackType.SystemVariables; otherwise, null)</param>
    private void AddTracker(SessionTrackType type, string name, string value)
    {
      SessionTracker tracker;
      tracker.TrackType = type;
      tracker.Name = name;
      tracker.Value = value;
      SessionTrackers.Add(tracker);
    }
  }

  internal struct SessionTracker
  {
    internal SessionTrackType TrackType;
    internal string Name;
    internal string Value;
  }
}
