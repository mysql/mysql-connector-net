// Copyright (c) 2018, 2023, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using static MySql.Data.Common.MySqlConnectionStringOption;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Abstract class that provides common functionality for connection options that apply for all protocols.
  /// </summary>
  public abstract class MySqlBaseConnectionStringBuilder : DbConnectionStringBuilder
  {
    /// <summary>
    /// Readonly field containing a collection of protocol shared connection options.
    /// </summary>
    internal static readonly MySqlConnectionStringOptionCollection Options = new MySqlConnectionStringOptionCollection();

    static MySqlBaseConnectionStringBuilder()
    {
      // Server options.
      Options.Add(new MySqlConnectionStringOption("server", "host,data source,datasource", typeof(string), "" /*"localhost"*/, false));
      Options.Add(new MySqlConnectionStringOption("database", "initial catalog", typeof(string), string.Empty, false));
      Options.Add(new MySqlConnectionStringOption("protocol", "connection protocol,connectionprotocol", typeof(MySqlConnectionProtocol), MySqlConnectionProtocol.Sockets, false,
        (SetterDelegate)((msb, sender, value) =>
        {
#if !NETFRAMEWORK
          MySqlConnectionProtocol enumValue;
          if (Enum.TryParse<MySqlConnectionProtocol>(value.ToString(), true, out enumValue))
          {
            if (!Platform.IsWindows() && (enumValue == MySqlConnectionProtocol.Memory || enumValue == MySqlConnectionProtocol.Pipe))
              throw new PlatformNotSupportedException(string.Format(Resources.OptionNotCurrentlySupported, $"Protocol={value}"));
          }
#endif
          msb.SetValue("protocol", value);
        }),
        (GetterDelegate)((msb, sender) => msb.ConnectionProtocol)));
      Options.Add(new MySqlConnectionStringOption("port", null, typeof(uint), (uint)3306, false));
      Options.Add(new MySqlConnectionStringOption("dns-srv", "dnssrv", typeof(bool), false, false));

      // Authentication options.
      Options.Add(new MySqlConnectionStringOption("user id", "uid,username,user name,user,userid", typeof(string), "", false));
      Options.Add(new MySqlConnectionStringOption("password", "pwd,password1,pwd1", typeof(string), "", false));
      Options.Add(new MySqlConnectionStringOption("password2", "pwd2", typeof(string), "", false));
      Options.Add(new MySqlConnectionStringOption("password3", "pwd3", typeof(string), "", false));
      Options.Add(new MySqlConnectionStringOption("certificatefile", "certificate file", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("certificatepassword", "certificate password,ssl-ca-pwd", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("certificatestorelocation", "certificate store location", typeof(MySqlCertificateStoreLocation), MySqlCertificateStoreLocation.None, false));
      Options.Add(new MySqlConnectionStringOption("certificatethumbprint", "certificate thumb print", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("sslmode", "ssl mode,ssl-mode", typeof(MySqlSslMode), MySqlSslMode.Preferred, false,
        (SetterDelegate)((msb, sender, value) =>
        {
          MySqlSslMode newValue = (MySqlSslMode)Enum.Parse(typeof(MySqlSslMode), value.ToString(), true);
          msb.SetValue("sslmode", newValue);
        }),
        (GetterDelegate)((msb, sender) => { return msb.SslMode; })));
      Options.Add(new MySqlConnectionStringOption("sslca", "ssl-ca", typeof(string), null, false,
        (SetterDelegate)((msb, sender, value) => { msb.SslCa = value as string; }),
        (GetterDelegate)((msb, sender) => { return msb.SslCa; })));
      Options.Add(new MySqlConnectionStringOption("sslkey", "ssl-key", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("sslcert", "ssl-cert", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("tlsversion", "tls-version,tls version", typeof(string), null, false,
        (SetterDelegate)((msb, sender, value) =>
        {
          if (value == null || string.IsNullOrWhiteSpace((string)value))
          {
            msb.SetValue("tlsversion", null);
            return;
          }

          string strProtocols = TlsValidation(value);
          msb.SetValue("tlsversion", strProtocols);
        }),
        (GetterDelegate)((msb, sender) => { return msb.TlsVersion; })));

      // Other properties.
      Options.Add(new MySqlConnectionStringOption("keepalive", "keep alive", typeof(uint), (uint)0, false));

      // Language and charset options.
      Options.Add(new MySqlConnectionStringOption("characterset", "character set,charset", typeof(string), "", false));
    }

    private static string TlsValidation(object value)
    {
      string strValue = ((string)value).TrimStart('[', '(').TrimEnd(']', ')').Replace(" ", string.Empty);
      string strProtocols;
      SslProtocols protocols = SslProtocols.None;
      bool unsupported = false;
      bool nonValid = false;

      if (string.IsNullOrWhiteSpace(strValue) || strValue == ",")
        throw new ArgumentException(Resources.TlsVersionsEmpty);

      foreach (string opt in strValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
      {
        string tls = opt.ToLowerInvariant().Replace("v", "").Replace(".", "");
        tls = tls.Equals("tls1") || tls.Equals("tls10") ? "tls" : tls;

        if (Enum.TryParse<SslProtocols>(tls, true, out SslProtocols protocol) && ((int)protocol) >= 3072)
          protocols |= protocol;
        else if (protocol.HasFlag(SslProtocols.Tls) || protocol.HasFlag(SslProtocols.Tls11))
          unsupported = true;
        else
          nonValid = true;
      }

      if (protocols == SslProtocols.None)
      {
        if (unsupported)
          throw new ArgumentException(Resources.TlsUnsupportedVersions);
        if (nonValid)
          throw new ArgumentException(Resources.TlsNonValidProtocols);
      }

      strProtocols = protocols == SslProtocols.None ? string.Empty : Enum.Format(typeof(SslProtocols), protocols, "G");
      strProtocols = (value.ToString().Equals("Tls13", StringComparison.OrdinalIgnoreCase)
      || value.ToString().Equals("Tlsv1.3", StringComparison.OrdinalIgnoreCase)) ? "Tls13" : strProtocols;

      return strProtocols;
    }

    /// <summary>
    /// Gets or sets a dictionary representing key-value pairs for each connection option.
    /// </summary>
    internal Dictionary<string, object> values { get; set; }
    #region Server Properties

    /// <summary>
    /// Gets or sets the name of the server.
    /// </summary>
    /// <value>The server.</value>
    /// <remarks>
    /// If this property is not set, then the provider will attempt to connect to<b>localhost</b> 
    /// even though this property will return <b>String.Empty</b>.</remarks>
    [Category("Connection")]
    [Description("Server to connect to")]
    [RefreshProperties(RefreshProperties.All)]
    public string Server
    {
      get { return this["server"] as string; }
      set { SetValue("server", value); }
    }

    /// <summary>
    /// Gets or sets the name of the database for the initial connection.
    /// </summary>
    /// <remarks> There is no default for this property and, if not set, the connection will not have a current database.
    /// </remarks>
    [Category("Connection")]
    [Description("Database to use initially")]
    [RefreshProperties(RefreshProperties.All)]
    public string Database
    {
      get { return values["database"] as string; }
      set { SetValue("database", value); }
    }

    /// <summary>
    /// Gets or sets the protocol that should be used for communicating
    /// with MySQL.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Connection Protocol")]
    [Description("Protocol to use for connection to MySQL")]
    [RefreshProperties(RefreshProperties.All)]
    public MySqlConnectionProtocol ConnectionProtocol
    {
      get { return (MySqlConnectionProtocol)values["protocol"]; }
      set { SetValue("protocol", value); }
    }

    /// <summary>
    /// Gets or sets the port number that is used when the socket
    /// protocol is being used.
    /// </summary>
    [Category("Connection")]
    [Description("Port to use for TCP/IP connections")]
    [RefreshProperties(RefreshProperties.All)]
    public uint Port
    {
      get { return (uint)values["port"]; }
      set { SetValue("port", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether this connection
    /// should resolve DNS SRV records.
    /// </summary>
    [Category("Connection")]
    [DisplayName("DNS SRV")]
    [Description("The connection should resolve DNS SRV records.")]
    [RefreshProperties(RefreshProperties.All)]
    public bool DnsSrv
    {
      get { return (bool)values["dns-srv"]; }
      set { SetValue("dns-srv", value); }
    }

    #endregion

    #region Authentication Properties

    /// <summary>
    /// Gets or sets the user ID that should be used to connect with.
    /// </summary>
    [Category("Security")]
    [DisplayName("User ID")]
    [Description("Indicates the user ID to be used when connecting to the data source.")]
    [RefreshProperties(RefreshProperties.All)]
    public string UserID
    {
      get { return (string)values["user id"]; }
      set { SetValue("user id", value); }
    }

    /// <summary>
    /// Gets or sets the password that should be used to make a connection.
    /// </summary>
    [Category("Security")]
    [Description("Indicates the password to be used when connecting to the data source.")]
    [RefreshProperties(RefreshProperties.All)]
    [PasswordPropertyText(true)]
    public string Password
    {
      get { return (string)values["password"]; }
      set { SetValue("password", value); }
    }

    /// <summary>
    /// Gets or sets the password for a second authentication that should be used to make a connection.
    /// </summary>
    [Category("Security")]
    [Description("Indicates the password for a second authentication to be used when connecting to the data source.")]
    [RefreshProperties(RefreshProperties.All)]
    [PasswordPropertyText(true)]
    public string Password2
    {
      get { return (string)values["password2"]; }
      set { SetValue("password2", value); }
    }

    /// <summary>
    /// Gets or sets the password for a third authentication that should be used to make a connection.
    /// </summary>
    [Category("Security")]
    [Description("Indicates the password for a third authentication to be used when connecting to the data source.")]
    [RefreshProperties(RefreshProperties.All)]
    [PasswordPropertyText(true)]
    public string Password3
    {
      get { return (string)values["password3"]; }
      set { SetValue("password3", value); }
    }

    /// <summary>
    ///  Gets or sets the path to the certificate file to be used.
    /// </summary>
    [Category("Authentication")]
    [DisplayName("Certificate File")]
    [Description("Certificate file in PKCS#12 format (.pfx) or path to a local file that " +
            "contains a list of trusted TLS/SSL CAs (.pem).")]
    public string CertificateFile
    {
      get { return (string)values["certificatefile"]; }
      set { SetValue("certificatefile", value); }
    }

    /// <summary>
    /// Gets or sets the password to be used in conjunction with the certificate file.
    /// </summary>
    [Category("Authentication")]
    [DisplayName("Certificate Password")]
    [Description("Password for certificate file")]
    public string CertificatePassword
    {
      get { return (string)values["certificatepassword"]; }
      set { SetValue("certificatepassword", value); }
    }

    /// <summary>
    /// Gets or sets the location to a personal store where a certificate is held.
    /// </summary>
    [Category("Authentication")]
    [DisplayName("Certificate Store Location")]
    [Description("Certificate Store Location for client certificates")]
    [DefaultValue(MySqlCertificateStoreLocation.None)]
    public MySqlCertificateStoreLocation CertificateStoreLocation
    {
      get { return (MySqlCertificateStoreLocation)values["certificatestorelocation"]; }
      set { SetValue("certificatestorelocation", value); }
    }

    /// <summary>
    /// Gets or sets a certificate thumbprint to ensure correct identification of a certificate contained within a personal store.
    /// </summary>
    [Category("Authentication")]
    [DisplayName("Certificate Thumbprint")]
    [Description("Certificate thumbprint. Can be used together with Certificate " +
        "Store Location parameter to uniquely identify the certificate to be used " +
        "for SSL authentication.")]
    public string CertificateThumbprint
    {
      get { return (string)values["certificatethumbprint"]; }
      set { SetValue("certificatethumbprint", value); }
    }

    /// <summary>
    /// Indicates whether to use SSL connections and how to handle server certificate errors.
    /// </summary>
    [DisplayName("Ssl Mode")]
    [Category("Authentication")]
    [Description("SSL properties for connection.")]
    [DefaultValue(MySqlSslMode.Disabled)]
    public MySqlSslMode SslMode
    {
      get { return (MySqlSslMode)values["sslmode"]; }
      set { SetValue("sslmode", value); }
    }

    [DisplayName("Ssl Ca")]
    [Category("Authentication")]
    [Description("Path to a local file that contains a list of trusted TLS/SSL CAs.")]
    public string SslCa
    {
      get { return CertificateFile; }
      set
      {
        CertificateFile = value;
      }
    }

    /// <summary>
    /// Sets the TLS versions to use in a <see cref="SslMode">SSL connection</see> to the server.
    /// </summary>
    /// <example>
    /// Tls version=TLSv1.2,TLSv1.3;
    /// </example>
    [DisplayName("TLS version")]
    [Category("Security")]
    [Description("TLS versions to use in a SSL connection to the server.")]
    public string TlsVersion
    {
      get { return (string)values["tlsversion"]; }
      set { SetValue("tlsversion", value); }
    }

    /// <summary>
    /// Gets or sets the path to a local key file in PEM format to use for establishing an encrypted connection.
    /// </summary>
    [DisplayName("Ssl Key")]
    [Category("Authentication")]
    [Description("Name of the SSL key file in PEM format to use for establishing an encrypted connection.")]
    public string SslKey
    {
      get { return (string)values["sslkey"]; }
      set { SetValue("sslkey", value); }
    }

    /// <summary>
    /// Gets or sets the path to a local certificate file in PEM format to use for establishing an encrypted connection.
    /// </summary>
    [DisplayName("Ssl Cert")]
    [Category("Authentication")]
    [Description("Name of the SSL certificate file in PEM format to use for establishing an encrypted connection.")]
    public string SslCert
    {
      get { return (string)values["sslcert"]; }
      set { SetValue("sslcert", value); }
    }

    #endregion

    #region Other Properties

    /// <summary>
    /// Gets or sets the idle connection time(seconds) for TCP connections.
    /// </summary>
    [DisplayName("Keep Alive")]
    [Description("For TCP connections, the idle connection time (in seconds) before the first keepalive packet is sent." +
        "A value of 0 indicates that keepalive is not used.")]
    [DefaultValue(0)]
    public uint Keepalive
    {
      get { return (uint)values["keepalive"]; }
      set { SetValue("keepalive", value); }
    }

    #endregion

    #region Language and Character Set Properties

    /// <summary>
    /// Gets or sets the character set that should be used for sending queries to the server.
    /// </summary>
    [DisplayName("Character Set")]
    [Category("Advanced")]
    [Description("Character set this connection should use.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    public string CharacterSet
    {
      get { return (string)values["characterset"]; }
      set { SetValue("characterset", value); }
    }

    #endregion

    /// <summary>
    /// Analyzes the connection string for potential duplicated or invalid connection options.
    /// </summary>
    /// <param name="connectionString">Connection string.</param>
    /// <param name="isXProtocol">Flag that indicates if the connection is using X Protocol.</param>
    /// <param name="isDefaultPort">Flag that indicates if the default port is used.</param>
    /// <param name="isAnalyzed">Flag that indicates if the connection string has been analyzed.</param>
    internal void AnalyzeConnectionString(string connectionString, bool isXProtocol, bool isDefaultPort = true, bool isAnalyzed = false)
    {
      if (!isAnalyzed && !string.IsNullOrWhiteSpace(connectionString))
      {
        string[] queries = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        bool isDnsSrv = false;

        if (queries.FirstOrDefault(q => q.ToLowerInvariant().Contains("dns-srv=true")) != null
              || queries.FirstOrDefault(q => q.ToLowerInvariant().Contains("dnssrv=true")) != null)
          isDnsSrv = true;

        foreach (string query in queries)
        {
          string[] keyValue = query.Split('=');
          if (keyValue.Length % 2 != 0)
            continue;

          var keyword = keyValue[0].ToLowerInvariant().Trim();
          var value = query.Contains(",") ? query.Replace(keyword, "") : keyValue[1].ToLowerInvariant();
          MySqlConnectionStringOption option = Options.Options.Where(o => o.Keyword == keyword || (o.Synonyms != null && o.Synonyms.Contains(keyword))).FirstOrDefault();

          // DNS SRV option can't be used if Port, Unix Socket or Multihost are specified
          if (isDnsSrv)
          {
            if (option.Keyword == "port" && !isDefaultPort)
              throw new ArgumentException(Resources.DnsSrvInvalidConnOptionPort);
            if (option.Keyword == "server" && ((value.Contains("address") && value.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Length > 2) || value.Contains(",")))
              throw new ArgumentException(Resources.DnsSrvInvalidConnOptionMultihost);
            if (option.Keyword == "protocol" && (value.ToLowerInvariant().Contains("unix") || value.ToLowerInvariant().Contains("unixsocket")))
              throw new ArgumentException(Resources.DnsSrvInvalidConnOptionUnixSocket);
          }

          if (option == null) continue;

          // Preferred is not allowed for the X Protocol.
          if (isXProtocol && option.Keyword == "sslmode" && (value == "preferred" || value == "prefered"))
            throw new ArgumentException(string.Format(Resources.InvalidSslMode, keyValue[1]));
        }
      }
    }

    public string GetConnectionString(bool includePass)
    {
      if (includePass) return ConnectionString;

      var conn = new StringBuilder();
      string delimiter = "";
      foreach (string key in this.Keys)
      {
        if (string.Compare(key, "password", StringComparison.OrdinalIgnoreCase) == 0 ||
            string.Compare(key, "pwd", StringComparison.OrdinalIgnoreCase) == 0) continue;
        conn.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}={2}",
            delimiter, key, this[key]);
        delimiter = ";";
      }

      return conn.ToString();
    }

    internal abstract MySqlConnectionStringOption GetOption(string key);
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    internal void SetValue(string keyword, object value, [CallerMemberName] string callerName = "")
    {
      MySqlConnectionStringOption option = GetOption(keyword);
      if (callerName != ".cctor" && option.IsCustomized)
        this[keyword] = value;
      else
        SetInternalValue(keyword, value);
    }

    internal abstract void SetInternalValue(string keyword, object value);

    public abstract override bool TryGetValue(string keyword, out object value);
  }
}