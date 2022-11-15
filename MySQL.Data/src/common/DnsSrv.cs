// Copyright (c) 2019, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Common.DnsClient;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.Data.Common
{
  /// <summary>
  /// DNS resolver that runs queries against a server.
  /// </summary>
  internal static class DnsSrv
  {
    // Resolver object that looks up for DNS SRV records.
    private static DnsResolver _resolver;
    // DNS domain.
    internal static string ServiceName { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ubiety.Dns.Core.Resolver"/> class.
    /// </summary>
    internal static void CreateResolver(string serviceName)
    {
      _resolver = new DnsResolver();
      ServiceName = serviceName;
    }

    /// <summary>
    /// Gets the DNS SVR records of the service name that is provided.
    /// </summary>
    /// <returns>A list of <see cref="DnsSrvRecord"/>s sorted as described in RFC2782.</returns>
    internal static List<DnsSrvRecord> GetDnsSrvRecords(string serviceName)
    {
      if (_resolver == null)
        CreateResolver(serviceName);

      List<DnsSrvRecord> records = new List<DnsSrvRecord>();
      DnsResponse response = _resolver.Query(ServiceName);

      foreach (var record in response.RecordsSRV)
        records.Add(record);

      if (records.Count > 0)
      {
        Reset();
        return SortSrvRecords(records);
      }
      else
        throw new MySqlException(string.Format(Resources.DnsSrvNoHostsAvailable, ServiceName));
    }

    /// <summary>
    /// Sorts a list of DNS SRV records according to the sorting rules described in RFC2782.
    /// </summary>
    /// <param name="srvRecords">List of <see cref="DnsSrvRecord"/>s to sort.</param>
    /// <returns>A new list of sorted <see cref="DnsSrvRecord"/>s.</returns>
    internal static List<DnsSrvRecord> SortSrvRecords(List<DnsSrvRecord> srvRecords)
    {
      srvRecords.Sort(new DnsSrvRecord());

      Random random = new Random();
      List<DnsSrvRecord> srvRecordsSortedRfc2782 = new List<DnsSrvRecord>();

      List<int> priorities = srvRecords.Select(s => s.Priority).Distinct().ToList();
      foreach (int priority in priorities)
      {
        List<DnsSrvRecord> srvRecordsSamePriority = srvRecords.Where(r => r.Priority == priority).ToList();
        while (srvRecordsSamePriority.Count > 1)
        {
          int recCount = srvRecordsSamePriority.Count;
          int sumOfWeights = 0;
          int[] weights = new int[recCount];
          for (int i = 0; i < recCount; i++)
          {
            sumOfWeights += srvRecordsSamePriority[i].Weight;
            weights[i] = sumOfWeights;
          }
          int selection = random.Next(sumOfWeights + 1);
          int pos = 0;
          for (; pos < recCount && weights[pos] < selection; pos++) { }
          srvRecordsSortedRfc2782.Add(srvRecordsSamePriority[pos]);
          srvRecordsSamePriority.RemoveAt(pos);
        }
        srvRecordsSortedRfc2782.Add(srvRecordsSamePriority[0]);
      }
      return srvRecordsSortedRfc2782;
    }

    /// <summary>
    /// Resets the DnsSrvResolver
    /// </summary>
    private static void Reset() => _resolver = null;
  }
}