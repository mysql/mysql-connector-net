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

namespace MySql.Data.Common.DnsClient
{
  #region RFC info
  /*
  3.2. RR definitions

  3.2.1. Format

  All RRs have the same top level format shown below:

                    1  1  1  1  1  1
      0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                                               |
    /                                               /
    /                      NAME                     /
    |                                               |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                      TYPE                     |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                     CLASS                     |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                      TTL                      |
    |                                               |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                   RDLENGTH                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
    /                     RDATA                     /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+


  where:

  NAME            an owner name, i.e., the name of the node to which this
          resource record pertains.

  TYPE            two octets containing one of the RR TYPE codes.

  CLASS           two octets containing one of the RR CLASS codes.

  TTL             a 32 bit signed integer that specifies the time interval
          that the resource record may be cached before the source
          of the information should again be consulted.  Zero
          values are interpreted to mean that the RR can only be
          used for the transaction in progress, and should not be
          cached.  For example, SOA records are always distributed
          with a zero TTL to prohibit caching.  Zero values can
          also be used for extremely volatile data.

  RDLENGTH        an unsigned 16 bit integer that specifies the length in
          octets of the RDATA field.

  RDATA           a variable length string of octets that describes the
          resource.  The format of this information varies
          according to the TYPE and CLASS of the resource record.
  */
  #endregion
  internal class DnsResourceRecord
  {
    /// <summary>
    /// Gets the name of the node to which this resource record pertains.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the type of resource record.
    /// </summary>
    internal RecordType Type { get; }

    /// <summary>
    /// Gets the type class of resource record, mostly IN but can be CS, CH or HS.
    /// </summary>
    internal QueryClass Class { get; }

    /// <summary>
    /// Gets the time to live, in seconds, that the resource record may be cached.
    /// </summary>
    internal uint TimeToLive { get; }

    /// <summary>
    /// Gets the record length.
    /// </summary>
    internal ushort RecordLength { get; }

    /// <summary>
    /// Gets one of the Record* classes.
    /// </summary>
    internal DnsRecord Record { get; }

    internal DnsResourceRecord(DnsRecordReader recordReader)
    {
      Name = recordReader.ReadDomainName();
      Type = (RecordType)recordReader.ReadUInt16();
      Class = (QueryClass)recordReader.ReadUInt16();
      TimeToLive = recordReader.ReadUInt32();
      RecordLength = recordReader.ReadUInt16();
      Record = recordReader.ReadRecord(Type);
      Record.ResourceRecord = this;
    }
  }

  /// <summary>
  /// Answer resource record.
  /// </summary>
  internal class AnswerResourceRecord : DnsResourceRecord
  {
    internal AnswerResourceRecord(DnsRecordReader recordReader)
      : base(recordReader)
    {
    }
  }

  /// <summary>
  /// Authority resource record.
  /// </summary>
  internal class AuthorityResourceRecord : DnsResourceRecord
  {
    internal AuthorityResourceRecord(DnsRecordReader recordReader)
      : base(recordReader)
    {
    }
  }

  /// <summary>
  /// Additional resource record.
  /// </summary>
  internal class AdditionalResourceRecord : DnsResourceRecord
  {
    internal AdditionalResourceRecord(DnsRecordReader recordReader)
      : base(recordReader)
    {
    }
  }
}
