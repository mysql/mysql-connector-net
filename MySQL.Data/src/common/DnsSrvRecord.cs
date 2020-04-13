// Copyright (c) 2019, Oracle and/or its affiliates. All rights reserved.
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
using Ubiety.Dns.Core.Records;

namespace MySql.Data.Common
{
  /// <summary>
  /// Class that represents a DNS SRV record.
  /// RFC 2782 (https://tools.ietf.org/html/rfc2782)
  /// </summary>
  internal class DnsSrvRecord : IComparer<DnsSrvRecord>
  {
    /// <summary>
    /// Gets the port.
    /// </summary>
    internal int Port { get; }

    /// <summary>
    /// Gets the priority.
    /// </summary>
    internal int Priority { get; }

    /// <summary>
    /// Gets the target domain name.
    /// </summary>
    internal string Target { get; }

    /// <summary>
    /// Gets the weight.
    /// </summary>
    internal int Weight { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DnsSrvRecord"/> class.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <param name="priority">The priority.</param>
    /// <param name="target">The target.</param>
    /// <param name="weight">The weight.</param>
    internal DnsSrvRecord(int port, int priority, string target, int weight)
    {
      Port = port;
      Priority = priority;
      Target = target;
      Weight = weight;
    }

    internal DnsSrvRecord() { }

    /// <summary>
    /// Initializes a new instance of <see cref="DnsSrvRecord"/> class.
    /// </summary>
    /// <param name="recordSRV">The DNS SRV record.</param>
    internal DnsSrvRecord(RecordSrv recordSRV)
      : this(recordSRV.Port, recordSRV.Priority, recordSRV.Target, recordSRV.Weight)
    { }

    /// <summary>
    /// Compare two <see cref="DnsSrvRecord"/> objects. First, using their priority and
    /// if both have the same, then using their weights.
    /// </summary>
    /// <param name="x">A <see cref="DnsSrvRecord"/> to compare.</param>
    /// <param name="y">A <see cref="DnsSrvRecord"/> to compare.</param>
    /// <returns></returns>
    public int Compare(DnsSrvRecord x, DnsSrvRecord y)
    {
      int priorityDiff = x.Priority.CompareTo(y.Priority);
      return priorityDiff == 0 ? y.Weight.CompareTo(x.Weight) : priorityDiff;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsSrvRecord"/> class from a <see cref="RecordSrv"/>.
    /// </summary>
    /// <param name="recordSRV">The DNS SRV record.</param>
    public static implicit operator DnsSrvRecord(RecordSrv recordSRV)
    {
      if (recordSRV == null)
        return null;

      recordSRV.Target = recordSRV.Target.TrimEnd('.');

      return new DnsSrvRecord(recordSRV);
    }
  }
}