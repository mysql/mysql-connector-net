// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;
using MySql.Data;
using static MySql.Data.MySqlClient.MySqlConnectionStringOption;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Enables the creation of connection strings by exposing the connection options as properties.
  /// Contains connection options specific to the X protocol.
  /// </summary>
  public sealed class MySqlXConnectionStringBuilder : MySqlBaseConnectionStringBuilder
  {
    static MySqlXConnectionStringBuilder()
    {
      // TODO: Remove this conditions when the connection options have been removed from MySqlBaseConnectionStringBuilder.
      if (Options.ContainsKey("auth"))
        Options.Remove("auth");

      if (Options.ContainsKey("sslca"))
        Options.Remove("sslca");

      if (Options.ContainsKey("ssl-ca"))
        Options.Remove("ssl-ca");

      if (Options.ContainsKey("sslcrl"))
        Options.Remove("sslcrl");

      if (Options.ContainsKey("ssl-crl"))
        Options.Remove("ssl-crl");

      // Authentication options.
      Options.Add(new MySqlConnectionStringOption("auth", null, typeof(MySqlAuthenticationMode), MySqlAuthenticationMode.Default, false));
      Options.Add(new MySqlConnectionStringOption("sslca", "ssl-ca", typeof(string), null, false,
        (XSetterDelegate)((msb, sender, value) => { msb.SslCa = value as string; }),
        (XGetterDelegate)((msb, sender) => { return msb.SslCa; })));
      Options.Add(new MySqlConnectionStringOption("sslcrl", "ssl-crl", typeof(string), null, false,
        (XSetterDelegate)((msb, sender, value) => { msb.SslCrl = value as string; }),
        (XGetterDelegate)((msb, sender) => { return msb.SslCrl; })));
    }

    public MySqlXConnectionStringBuilder() : base()
    { }

    public MySqlXConnectionStringBuilder(string connStr) : base(connStr)
    { }

    #region Authentication Properties

    [Category("Authentication")]
    [DisplayName("Auth")]
    [Description("Authentication mechanism")]
    [DefaultValue(MySqlAuthenticationMode.Default)]
    public MySqlAuthenticationMode Auth
    {
      get { return (MySqlAuthenticationMode) values["auth"]; }
      set { SetValue("auth", value); }
    }

    [Description("Path to a local file that contains a list of trusted TLS/SSL CAs")]
    public string SslCa
    {
      get { return CertificateFile; }
      set
      {
        SslMode = MySqlSslMode.Required;
        CertificateFile = value;
      }
    }

    [Description("Path to a local file containing certificate revocation lists")]
    public string SslCrl
    {
      get { throw new NotSupportedException(); }
      set { throw new NotSupportedException(); }
    }

    #endregion

    public override object this[string keyword]
    {
      get
      {
        MySqlConnectionStringOption opt = GetOption(keyword);
        if (opt.BaseSetter != null)
          return opt.BaseGetter(this, opt);
        else if (opt.XGetter != null)
          return opt.XGetter(this, opt);
        else
          throw new ArgumentException(Resources.KeywordNotSupported, keyword);
      }
      set
      {
        MySqlConnectionStringOption opt = GetOption(keyword);
        if (opt.BaseSetter != null)
          opt.BaseSetter(this, opt, value);
        else if (opt.XSetter != null)
          opt.XSetter(this, opt, value);
        else
          throw new ArgumentException(Resources.KeywordNotSupported, keyword);
      }
    }
  }
}
