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

using MySql.Data;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
      // Add options shared between classic and X protocols from base class.
      Options = MySqlBaseConnectionStringBuilder.Options.Clone();

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
      Options.Add(new MySqlConnectionStringOption("connection-attributes", "connectionattributes", typeof(string), "true", false,
        (msb, sender, value) => { msb.SetValue("connection-attributes", value); }, (msb, sender) => msb.ConnectionAttributes));
      Options.Add(new MySqlConnectionStringOption("compression", "use-compression", typeof(CompressionType), CompressionType.Preferred, false,
        (msb, sender, value) => { msb.SetValue("compression", value); }, (msb, sender) => msb.Compression));
      Options.Add(new MySqlConnectionStringOption("compression-algorithms", "compressionalgorithms", typeof(string), string.Empty, false,
              (msb, sender, value) => { msb.SetValue("compression-algorithms", value); }, (msb, sender) => msb.CompressionAlgorithm));

      // Authentication options.
      Options.Add(new MySqlConnectionStringOption("auth", null, typeof(MySqlAuthenticationMode), MySqlAuthenticationMode.Default, false,
        (msb, sender, value) => { msb.SetValue("auth", value); }, (msb, sender) => msb.Auth));
      Options.Add(new MySqlConnectionStringOption("sslcrl", "ssl-crl", typeof(string), null, false,
        (msb, sender, value) => { msb.SslCrl = value as string; }, ((msb, sender) => { return msb.SslCrl; })));
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
    public MySqlXConnectionStringBuilder()
    {
      values = new Dictionary<string, object>();

      // Populate initial values.
      lock (this)
      {
        foreach (MySqlConnectionStringOption option in Options.Options)
        {
          values[option.Keyword] = option.DefaultValue;
        }
      }
    }

    /// <summary>
    /// Constructor accepting a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="isDefaultPort">A flag indicating if the default port is used in the connection.</param>
    public MySqlXConnectionStringBuilder(string connectionString, bool isDefaultPort = true) : this()
    {
      AnalyzeConnectionString(connectionString, true, isDefaultPort);
      lock (this)
      {
        ConnectionString = connectionString;
      }

      if (SslMode == MySqlSslMode.Preferred)
        SslMode = MySqlSslMode.Required;
    }

    /// <summary>
    /// Readonly field containing a collection of classic protocol and protocol shared connection options.
    /// </summary>
    internal new static readonly MySqlConnectionStringOptionCollection Options;

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

    /// <summary>
    /// Gets or sets the compression type between client and server.
    /// </summary>
    [Category("Server")]
    [DisplayName("Compression Type")]
    [Description("Compression type")]
    [DefaultValue(CompressionType.Preferred)]
    public CompressionType Compression
    {
      get { return (CompressionType)values["compression"]; }
      set { SetValue("compression", value); }
    }

    /// <summary>
    /// Gets or sets the compression algorithm.
    /// </summary>
    [Category("Server")]
    [DisplayName("Compression Algorithm")]
    [Description("Compression algorithm")]
    public string CompressionAlgorithm
    {
      get { return values["compression-algorithms"] is null ? string.Empty : values["compression-algorithms"].ToString(); }
      set { SetValue("compression-algorithms", value); }
    }


    #endregion

    /// <summary>
    /// Gets or sets a connection option.
    /// </summary>
    /// <param name="keyword">The keyword that identifies the connection option to modify.</param>
    public override object this[string keyword]
    {
      get
      {
        MySqlConnectionStringOption opt = GetOption(keyword);
        if (opt.XGetter != null)
          return opt.XGetter(this, opt);
        else if (opt.Getter != null)
          return opt.Getter(this, opt);
        else
          throw new ArgumentException(Resources.KeywordNotSupported, keyword);
      }
      set
      {
        MySqlConnectionStringOption opt = GetOption(keyword);
        if (opt.XSetter != null)
          opt.XSetter(this, opt, value);
        else if (opt.Setter != null)
          opt.Setter(this, opt, value);
        else
          throw new ArgumentException(Resources.KeywordNotSupported, keyword);
      }
    }

    public override void Clear()
    {
      base.Clear();
      lock (this)
      {
        foreach (var option in Options.Options)
          if (option.DefaultValue != null)
            values[option.Keyword] = option.DefaultValue;
          else
            values[option.Keyword] = null;
      }
    }

    public override bool ContainsKey(string keyword)
    {
      MySqlConnectionStringOption option = Options.Get(keyword);
      return option != null;
    }

    public override bool Equals(object obj)
    {
      var other = obj as MySqlXConnectionStringBuilder;
      if (obj == null)
        return false;

      if (this.values.Count != other.values.Count) return false;

      foreach (KeyValuePair<string, object> kvp in this.values)
      {
        if (other.values.ContainsKey(kvp.Key))
        {
          object v = other.values[kvp.Key];
          if (v == null && kvp.Value != null) return false;
          if (kvp.Value == null && v != null) return false;
          if (kvp.Value == null && v == null) return true;
          if (!v.Equals(kvp.Value)) return false;
        }
        else
        {
          return false;
        }
      }

      return true;
    }

    internal override MySqlConnectionStringOption GetOption(string key)
    {
      MySqlConnectionStringOption option = Options.Get(key);
      if (option == null)
        throw new ArgumentException(Resources.KeywordNotSupported, key);
      else
        return option;
    }

    internal override void SetInternalValue(string keyword, object value)
    {
      MySqlConnectionStringOption option = GetOption(keyword);
      option.ValidateValue(ref value, keyword, true);

      // remove all related keywords
      option.Clean(this);

      if (value != null)
      {
        lock (this)
        {
          // set value for the given keyword
          values[option.Keyword] = value;
          base[keyword] = value;
        }
      }
    }

    public override bool Remove(string keyword)
    {
      bool removed = false;
      lock (this) { removed = base.Remove(keyword); }
      if (!removed) return false;
      MySqlConnectionStringOption option = GetOption(keyword);
      lock (this)
      {
        values[option.Keyword] = option.DefaultValue;
      }
      return true;
    }

    /// <summary>
    /// Retrieves the value corresponding to the supplied key from this <see cref="MySqlXConnectionStringBuilder"/>.
    /// </summary>
    /// <param name="keyword">The key of the item to retrieve.</param>
    /// <param name="value">The value corresponding to the <paramref name="keyword"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="keyword"/> was found within the connection string; 
    /// otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keyword"/> contains a null value.</exception>
    public override bool TryGetValue(string keyword, out object value)
    {
      if (keyword == null) throw new ArgumentNullException(keyword);

      MySqlConnectionStringOption option = Options.Get(keyword);

      value = option == null ? null : this[keyword];
      return option != null;
    }
  }
}
