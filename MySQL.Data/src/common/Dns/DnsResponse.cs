// Copyright (c) 2022, Oracle and/or its affiliates.
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

using System;
using System.Collections.Generic;
using System.Net;

namespace MySql.Data.Common.DnsClient
{
  internal class DnsResponse
  {
    /// <summary>
    /// List of Question records.
    /// </summary>
    internal List<DnsQuestion> Questions { get; }

    /// <summary>
    /// List of AnswerResourceRecord records.
    /// </summary>
    internal List<AnswerResourceRecord> Answers { get; }

    /// <summary>
    /// List of AuthorityResourceRecord records.
    /// </summary>
    internal List<AuthorityResourceRecord> Authorities { get; }

    /// <summary>
    /// List of AdditionalResourceRecord records.
    /// </summary>
    internal List<AdditionalResourceRecord> Additionals { get; }

    /// <summary>
    /// The record header.
    /// </summary>
    internal DnsRecordHeader Header { get; }

    /// <summary>
    /// Server which delivered this response.
    /// </summary>
    internal IPEndPoint Server { get; }

    /// <summary>
    /// The Size of the message.
    /// </summary>
    internal int MessageSize { get; }

    /// <summary>
    /// Error message, empty when no error.
    /// </summary>
    internal string Error { get; set; }

    /// <summary>
    /// TimeStamp when cached.
    /// </summary>
    internal DateTime TimeStamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsResponse" /> class.
    /// </summary>
    internal DnsResponse()
    {
      Questions = new List<DnsQuestion>();
      Answers = new List<AnswerResourceRecord>();
      Authorities = new List<AuthorityResourceRecord>();
      Additionals = new List<AdditionalResourceRecord>();

      Server = new IPEndPoint(0, 0);
      Error = String.Empty;
      MessageSize = 0;
      TimeStamp = DateTime.Now;
      Header = new DnsRecordHeader();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsResponse" /> class.
    /// </summary>
    /// <param name="iPEndPoint"><see cref="IPEndPoint" /> of the DNS server that responded to the query.</param>
    /// <param name="data"><see cref="byte" /> array of the response data.</param>
    internal DnsResponse(IPEndPoint iPEndPoint, byte[] data)
      : this()
    {
      Server = iPEndPoint;
      MessageSize = data.Length;
      DnsRecordReader recordReader = new DnsRecordReader(data);
      Header = new DnsRecordHeader(recordReader);

      for (int intI = 0; intI < Header.QuestionCount; intI++)
        Questions.Add(new DnsQuestion(recordReader));

      for (int intI = 0; intI < Header.AnswerCount; intI++)
        Answers.Add(new AnswerResourceRecord(recordReader));

      for (int intI = 0; intI < Header.NameserverCount; intI++)
        Authorities.Add(new AuthorityResourceRecord(recordReader));

      for (int intI = 0; intI < Header.AdditionalRecordsCount; intI++)
        Additionals.Add(new AdditionalResourceRecord(recordReader));
    }

    /// <summary>
    /// List of RecordSRV in Response.Answers
    /// </summary>
    internal DnsSrvRecord[] RecordsSRV
    {
      get
      {
        List<DnsSrvRecord> list = new List<DnsSrvRecord>();
        foreach (var answer in Answers)
        {
          var record = answer.Record as DnsSrvRecord;

          if (record != null)
            list.Add(record);
        }
        return list.ToArray();
      }
    }
  }
}
