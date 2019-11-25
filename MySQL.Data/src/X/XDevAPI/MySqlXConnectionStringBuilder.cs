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

using System;
using System.ComponentModel;
using MySql.Data.MySqlClient;
using MySql.Data;

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
      // Server options.
      Options.Add(new MySqlConnectionStringOption("connect-timeout", "connecttimeout", typeof(uint), (uint)10000, false,
        delegate (MySqlXConnectionStringBuilder msb, MySqlConnectionStringOption sender, object Value)
        {
          sender.ValidateValue(ref Value, sender.Keyword);
          uint value = (uint)Convert.ChangeType(Value, sender.BaseType);
          // Timeout in milliseconds should not exceed maximum for 32 bit
          // signed integer (~24 days). We truncate the value if it exceeds
          // maximum (MySqlCommand.CommandTimeout uses the same technique)
          uint timeout = Math.Min(value, Int32.MaxValue);
          if (timeout != value)
          {
            MySqlTrace.LogWarning(-1, "Connection timeout value too large ("
                + value + " milliseconds). Changed to max. possible value " +
                +timeout + " milliseconds)");
          }
          msb.SetValue("connect-timeout", timeout);

        },
        (msb, sender) => (uint)msb.values["connect-timeout"]
        ));
      Options.Add(new MySqlConnectionStringOption("connection-attributes", "connectionattributes", typeof(string), "true", false));

      // Authentication options.
      Options.Add(new MySqlConnectionStringOption("auth", null, typeof(MySqlAuthenticationMode), MySqlAuthenticationMode.Default, false));
      Options.Add(new MySqlConnectionStringOption("sslcrl", "ssl-crl", typeof(string), null, false,
        (msb, sender, value) => { msb.SslCrl = value as string; }, ((msb, sender) => { return msb.SslCrl; })));
    }

    public MySqlXConnectionStringBuilder() : base()
    {
      if (SslMode == MySqlSslMode.Preferred)
        SslMode = MySqlSslMode.Required;
    }

    public MySqlXConnectionStringBuilder(string connStr, bool isDefaulPort = true) : base(connStr, true, isDefaulPort)
    {
      if (SslMode == MySqlSslMode.Preferred)
        SslMode = MySqlSslMode.Required;
    }

    #region Server Properties

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Connect Timeout")]
    [Description("The length of time (in milliseconds) to wait for a connection " +
                 "to the server before terminating the attempt and generating an error.")]
    [RefreshProperties(RefreshProperties.All)]
    public uint ConnectTimeout
    {
      get { return (uint)values["connect-timeout"]; }

      set
      {
        // Timeout in milliseconds should not exceed maximum for 32 bit
        // signed integer (~24 days). We truncate the value if it exceeds
        // maximum (MySqlCommand.CommandTimeout uses the same technique
        uint timeout = Math.Min(value, Int32.MaxValue);
        if (timeout != value)
        {
          MySqlTrace.LogWarning(-1, "Connection timeout value too large ("
              + value + " milliseconds). Changed to max. possible value" +
              +timeout + " milliseconds)");
        }
        SetValue("connect-timeout", timeout);
      }
    }

    /// <summary>
    /// Gets or sets the connection attributes.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Connection Attributes")]
    [Description("Gets or sets a comma-delimited list of key-value pairs " +
      "(in addition to standard XProtocol predefined keys) to be passed to MySQL Server" +
      "for display as connection attributes.")]
    public string ConnectionAttributes
    {
      get { return (string)values["connection-attributes"]; }
      set { SetValue("connection-attributes", value); }
    }
    #endregion

    #region Authentication Properties

    [Category("Authentication")]
    [DisplayName("Auth")]
    [Description("Authentication mechanism")]
    [DefaultValue(MySqlAuthenticationMode.Default)]
    public MySqlAuthenticationMode Auth
    {
      get { return (MySqlAuthenticationMode)values["auth"]; }
      set { SetValue("auth", value); }
    }

    /// <summary>
    /// Path to a local file containing certificate revocation lists.
    /// </summary>
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
