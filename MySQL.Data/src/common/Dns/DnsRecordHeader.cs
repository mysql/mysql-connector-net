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
  internal class DnsRecordHeader
  {
    #region RFC specification
    /*
    4.1.1. Header section format

    The header contains the following fields:

                      1  1  1  1  1  1
        0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
      +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
      |                      ID                       |
      +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
      |QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
      +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
      |                    QDCOUNT                    |
      +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
      |                    ANCOUNT                    |
      +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
      |                    NSCOUNT                    |
      +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
      |                    ARCOUNT                    |
      +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

      where:

      ID              A 16 bit identifier assigned by the program that
              generates any kind of query.  This identifier is copied
              the corresponding reply and can be used by the requester
              to match up replies to outstanding queries.

      QR              A one bit field that specifies whether this message is a
              query (0), or a response (1).

      OPCODE          A four bit field that specifies kind of query in this
              message.  This value is set by the originator of a query
              and copied into the response.  The values are:

              0               a standard query (QUERY)

              1               an inverse query (IQUERY)

              2               a server status request (STATUS)

              3-15            reserved for future use

      AA              Authoritative Answer - this bit is valid in responses,
              and specifies that the responding name server is an
              authority for the domain name in question section.

              Note that the contents of the answer section may have
              multiple owner names because of aliases.  The AA bit
              corresponds to the name which matches the query name, or
              the first owner name in the answer section.

      TC              TrunCation - specifies that this message was truncated
              due to length greater than that permitted on the
              transmission channel.

      RD              Recursion Desired - this bit may be set in a query and
              is copied into the response.  If RD is set, it directs
              the name server to pursue the query recursively.
              Recursive query support is optional.

      RA              Recursion Available - this be is set or cleared in a
              response, and denotes whether recursive query support is
              available in the name server.

      Z               Reserved for future use.  Must be zero in all queries
              and responses.

      RCODE           Response code - this 4 bit field is set as part of
              responses.  The values have the following
              interpretation:

              0               No error condition

              1               Format error - The name server was
                      unable to interpret the query.

              2               Server failure - The name server was
                      unable to process this query due to a
                      problem with the name server.

              3               Name Error - Meaningful only for
                      responses from an authoritative name
                      server, this code signifies that the
                      domain name referenced in the query does
                      not exist.

              4               Not Implemented - The name server does
                      not support the requested kind of query.

              5               Refused - The name server refuses to
                      perform the specified operation for
                      policy reasons.  For example, a name
                      server may not wish to provide the
                      information to the particular requester,
                      or a name server may not wish to perform
                      a particular operation (e.g., zone
                      transfer) for particular data.

              6-15            Reserved for future use.

      QDCOUNT         an unsigned 16 bit integer specifying the number of
              entries in the question section.

      ANCOUNT         an unsigned 16 bit integer specifying the number of
              resource records in the answer section.

      NSCOUNT         an unsigned 16 bit integer specifying the number of name
              server resource records in the authority records
              section.

      ARCOUNT         an unsigned 16 bit integer specifying the number of
              resource records in the additional records section.

      */
    #endregion

    // internal flag
    private ushort _flags;

    /// <summary>
    /// Gets or sets the unique identifier of the record.
    /// </summary>
    internal ushort Id { get; set; }

    /// <summary>
    /// Gets or sets the number of questions in the record.
    /// </summary>
    internal ushort QuestionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of answers in the record.
    /// </summary>
    internal ushort AnswerCount { get; set; }

    /// <summary>
    /// Gets or sets the number of name servers in the record.
    /// </summary>
    internal ushort NameserverCount { get; set; }

    /// <summary>
    /// Gets or sets the number of additional records in the record.
    /// </summary>
    internal ushort AdditionalRecordsCount { get; set; }

    /// <summary>
    /// Specifies kind of query.
    /// </summary>
    internal OPCode OperationCode
    {
      get { return (OPCode)GetBits(_flags, 11, 4); }
      set { _flags = SetBits(_flags, 11, 4, (ushort)value); }
    }

    /// <summary>
    /// Recursion Desired
    /// </summary>
    internal bool RecursionDesired
    {
      get
      { return GetBits(_flags, 8, 1) == 1; }
      set { _flags = SetBits(_flags, 8, 1, value); }
    }

    internal DnsRecordHeader()
    {
    }

    internal DnsRecordHeader(DnsRecordReader recordReader)
    {
      Id = recordReader.ReadUInt16();
      _flags = recordReader.ReadUInt16();
      QuestionCount = recordReader.ReadUInt16();
      AnswerCount = recordReader.ReadUInt16();
      NameserverCount = recordReader.ReadUInt16();
      AdditionalRecordsCount = recordReader.ReadUInt16();
    }

    private static ushort GetBits(ushort oldValue, int position, int length)
    {
      if (length <= 0 || position >= 16)
        return 0;

      int mask = (2 << (length - 1)) - 1;

      return (ushort)((oldValue >> position) & mask);
    }

    private static ushort SetBits(ushort oldValue, int position, int length, bool blnValue)
    {
      return SetBits(oldValue, position, length, blnValue ? (ushort)1 : (ushort)0);
    }

    private static ushort SetBits(ushort oldValue, int position, int length, ushort newValue)
    {
      if (length <= 0 || position >= 16)
        return oldValue;

      int mask = (2 << (length - 1)) - 1;

      oldValue &= (ushort)~(mask << position);
      oldValue |= (ushort)((newValue & mask) << position);
      return oldValue;
    }

    /// <summary>
    /// Represents the header as a byte array
    /// </summary>
    internal byte[] GetData()
    {
      List<byte> data = new List<byte>();
      data.AddRange(WriteShort(Id));
      data.AddRange(WriteShort(_flags));
      data.AddRange(WriteShort(QuestionCount));
      data.AddRange(WriteShort(AnswerCount));
      data.AddRange(WriteShort(NameserverCount));
      data.AddRange(WriteShort(AdditionalRecordsCount));
      return data.ToArray();
    }

    private static byte[] WriteShort(ushort sValue)
    {
      return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)sValue));
    }
  }
}
