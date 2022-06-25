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
using System.Text;

namespace MySql.Data.Common.DnsClient
{
  #region Rfc 1034/1035
  /*
  4.1.2. Question section format

  The question section is used to carry the "question" in most queries,
  i.e., the parameters that define what is being asked.  The section
  contains QDCOUNT (usually 1) entries, each of the following format:

                    1  1  1  1  1  1
      0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                                               |
    /                     QNAME                     /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                     QTYPE                     |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                     QCLASS                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

  where:

  QNAME           a domain name represented as a sequence of labels, where
          each label consists of a length octet followed by that
          number of octets.  The domain name terminates with the
          zero length octet for the null label of the root.  Note
          that this field may be an odd number of octets; no
          padding is used.

  QTYPE           a two octet code which specifies the type of the query.
          The values for this field include all codes valid for a
          TYPE field, together with some more general codes which
          can match more than one type of RR.


  QCLASS          a two octet code that specifies the class of the query.
          For example, the QCLASS field is IN for the Internet.
  */
  #endregion

  /// <summary>
  /// The <see cref="DnsQuestion"/> class transports information of the lookup query performed.
  /// </summary>
  internal class DnsQuestion
  {
    /// <summary>
    /// Gets the domain name
    /// </summary>
    internal string DomainName { get; }

    /// <summary>
    /// Gets the type of the question.
    /// </summary>
    internal QueryType QuestionType { get; }

    /// <summary>
    /// Gets the question class.
    /// </summary>
    internal QueryClass QuestionClass { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Question"/> class.
    /// </summary>
    /// <param name="domainName">Domain name.</param>
    /// <param name="questionType">Type of the question.</param>
    /// <param name="questionClass">The question class.</param>
    internal DnsQuestion(string domainName, QueryType questionType = QueryType.SRV, QueryClass questionClass = QueryClass.IN)
    {
      if (string.IsNullOrWhiteSpace(domainName)) throw new ArgumentNullException(nameof(domainName));
      if (!domainName.EndsWith(".", StringComparison.InvariantCulture)) domainName += ".";

      DomainName = domainName;
      QuestionType = questionType;
      QuestionClass = questionClass;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsQuestion" /> class.
    /// </summary>
    /// <param name="reader"><see cref="DnsRecordReader" /> of the record.</param>
    internal DnsQuestion(DnsRecordReader reader)
    {
      DomainName = reader.ReadDomainName();
      QuestionType = (QueryType)reader.ReadUInt16();
      QuestionClass = (QueryClass)reader.ReadUInt16();
    }

    /// <summary>
    /// Gets the bytes in this collection.
    /// </summary>
    internal byte[] GetData()
    {
      List<byte> data = new List<byte>();
      data.AddRange(WriteName(DomainName));
      data.AddRange(WriteShort((ushort)QuestionType));
      data.AddRange(WriteShort((ushort)QuestionClass));
      return data.ToArray();
    }

    private static byte[] WriteName(string src)
    {
      if (src == ".")
        return new byte[1];

      StringBuilder sb = new StringBuilder();
      sb.Append('\0');

      for (int i = 0, j = 0; i < src.Length; i++, j++)
      {
        sb.Append(src[i]);
        if (src[i] == '.')
        {
          sb[i - j] = (char)(j & 0xff);
          j = -1;
        }
      }

      sb[sb.Length - 1] = '\0';
      return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static byte[] WriteShort(ushort sValue)
    {
      return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)sValue));
    }
  }
}
