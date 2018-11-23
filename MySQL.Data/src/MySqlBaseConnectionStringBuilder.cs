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
using MySqlX.XDevAPI;
using static MySql.Data.MySqlClient.MySqlConnectionStringOption;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Abstract class that provides common functionality for connection options that apply for all protocols.
  /// </summary>
  public abstract class MySqlBaseConnectionStringBuilder : DbConnectionStringBuilder
  {
    internal Dictionary<string, object> values = new Dictionary<string, object>();
    //internal Dictionary<string, object> values
    //{
    //  get { lock (this) { return _values; } }
    //}

    internal static readonly MySqlConnectionStringOptionCollection Options = new MySqlConnectionStringOptionCollection();

    static MySqlBaseConnectionStringBuilder()
    {
      // Server options.
      Options.Add(new MySqlConnectionStringOption("server", "host,data source,datasource,address,addr,network address", typeof(string), "" /*"localhost"*/, false));
      Options.Add(new MySqlConnectionStringOption("database", "initial catalog", typeof(string), string.Empty, false));
      Options.Add(new MySqlConnectionStringOption("protocol", "connection protocol, connectionprotocol", typeof(MySqlConnectionProtocol), MySqlConnectionProtocol.Sockets, false,
        (BaseSetterDelegate)((msb, sender, value) =>
       {
#if NETSTANDARD1_6 || NETSTANDARD2_0 || NETCOREAPP2_0
         MySqlConnectionProtocol enumValue;
         if (Enum.TryParse<MySqlConnectionProtocol>(value.ToString(), true, out enumValue))
         {
           if (enumValue == MySqlConnectionProtocol.Memory || enumValue == MySqlConnectionProtocol.Pipe)
             throw new PlatformNotSupportedException(string.Format(Resources.OptionNotCurrentlySupported, $"Protocol={value}"));
         }
#endif
         msb.SetValue("protocol", value);
       }),
        (msb, sender) => msb.ConnectionProtocol));
      Options.Add(new MySqlConnectionStringOption("port", null, typeof(uint), (uint)3306, false));

      // Authentication options.
      Options.Add(new MySqlConnectionStringOption("user id", "uid,username,user name,user,userid", typeof(string), "", false));
      Options.Add(new MySqlConnectionStringOption("password", "pwd", typeof(string), "", false));
      Options.Add(new MySqlConnectionStringOption("certificatefile", "certificate file", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("certificatepassword", "certificate password,ssl-ca-pwd", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("certificatestorelocation", "certificate store location", typeof(MySqlCertificateStoreLocation), MySqlCertificateStoreLocation.None, false));
      Options.Add(new MySqlConnectionStringOption("certificatethumbprint", "certificate thumb print", typeof(string), null, false));
      Options.Add(new MySqlConnectionStringOption("sslmode", "ssl mode,ssl-mode", typeof(MySqlSslMode), MySqlSslMode.Preferred, false));

      // Other properties.
      Options.Add(new MySqlConnectionStringOption("keepalive", "keep alive", typeof(uint), (uint)0, false));

      // Language and charset options.
      Options.Add(new MySqlConnectionStringOption("characterset", "character set,charset", typeof(string), "", false));

      // X Authentication options.      
      Options.Add(new MySqlConnectionStringOption("sslca", "ssl-ca", typeof(string), null, false,
        (BaseSetterDelegate)((msb, sender, value) => { msb.SslCa = value as string; }),
        (BaseGetterDelegate)((msb, sender) => { return msb.SslCa; })));      
    }

    public MySqlBaseConnectionStringBuilder()
    {
      HasProcAccess = true;
      // Populate initial values
      lock (this)
      {
        foreach (MySqlConnectionStringOption option in Options.Options)
        {
          values[option.Keyword] = option.DefaultValue;
        }
      }
    }

    public MySqlBaseConnectionStringBuilder(string connStr, bool isXProtocol)
      : this()
    {
      AnalyzeConnectionString(connStr, isXProtocol);
      lock (this)
      {
        ConnectionString = connStr;
      }
    }

    #region Server Properties

    /// <summary>
    /// Gets or sets the name of the server.
    /// </summary>
    /// <value>The server.</value>
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
#if !NETSTANDARD1_6
    [PasswordPropertyText(true)]
#endif
    public string Password
    {
      get { return (string)values["password"]; }
      set { SetValue("password", value); }
    }

    /// <summary>
    ///  Gets or sets the path to the certificate file to be used.
    /// </summary>
    [Category("Authentication")]
    [DisplayName("Certificate File")]
    [Description("Certificate file in PKCS#12 format (.pfx)")]
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
    [Category("Security")]
    [Description("SSL properties for connection.")]
    [DefaultValue(MySqlSslMode.None)]
    public MySqlSslMode SslMode
    {
      get { return (MySqlSslMode)values["sslmode"]; }
      set { SetValue("sslmode", value); }
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

    #region XAuthentication Properties

    [Description("Path to a local file that contains a list of trusted TLS/SSL CAs")]
    [Obsolete("Use MySqlXConnectionStringBuilder.SslCa instead.")]
    public string SslCa
    {
      get { return CertificateFile; }
      set
      {
        CertificateFile = value;
      }
    }

    #endregion

    internal bool HasProcAccess { get; set; }

    public override object this[string keyword]
    {
      get { MySqlConnectionStringOption opt = GetOption(keyword); return opt.BaseGetter(this, opt); }
      set { MySqlConnectionStringOption opt = GetOption(keyword); opt.BaseSetter(this, opt, value); }
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

    internal void SetValue(string keyword, object value, [CallerMemberName] string callerName = "")
    {
      MySqlConnectionStringOption option = GetOption(keyword);
      if (callerName != ".cctor" && option.IsCustomized)
        this[keyword] = value;
      else
        SetInternalValue(keyword, value);
    }

    internal void SetInternalValue(string keyword, object value)
    {
      MySqlConnectionStringOption option = GetOption(keyword);
      option.ValidateValue(ref value);

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

    internal MySqlConnectionStringOption GetOption(string key)
    {
      MySqlConnectionStringOption option = Options.Get(key);
      if (option == null)
        throw new ArgumentException(Resources.KeywordNotSupported, key);
      else
        return option;
    }

    public override bool ContainsKey(string keyword)
    {
      MySqlConnectionStringOption option = Options.Get(keyword);
      return option != null;
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

    public string GetConnectionString(bool includePass)
    {
      if (includePass) return ConnectionString;

      StringBuilder conn = new StringBuilder();
      string delimiter = "";
      foreach (string key in this.Keys)
      {
        if (String.Compare(key, "password", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(key, "pwd", StringComparison.OrdinalIgnoreCase) == 0) continue;
        conn.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}={2}",
            delimiter, key, this[key]);
        delimiter = ";";
      }
      return conn.ToString();
    }

    public override bool Equals(object obj)
    {
      MySqlBaseConnectionStringBuilder other = obj as MySqlBaseConnectionStringBuilder;
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

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    /// <summary>
    /// Analyzes the connection string for potential duplicated or invalid connection options.
    /// </summary>
    /// <param name="connectionString">Connection string.</param>
    internal void AnalyzeConnectionString(string connectionString, bool isXProtocol)
    {
      string[] queries = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      List<string> usedSslOptions = new List<string>();
      bool sslModeIsNone = false;
      foreach (string query in queries)
      {
        string[] keyValue = query.Split('=');
        if (keyValue.Length % 2 != 0)
          continue;

        var keyword = keyValue[0].ToLowerInvariant().Trim();
        var value = keyValue[1].ToLowerInvariant();
        MySqlConnectionStringOption option = Options.Options.Where(o => o.Keyword == keyword || (o.Synonyms != null && o.Synonyms.Contains(keyword))).FirstOrDefault();
        if (option == null || (option.Keyword != "sslmode" && option.Keyword != "certificatepassword" && option.Keyword != "sslcrl" && option.Keyword != "sslca"))
          continue;

        // SSL connection options can't be duplicated.
        if (usedSslOptions.Contains(option.Keyword))
          throw new ArgumentException(string.Format(Resources.DuplicatedSslConnectionOption, keyword));

        // SSL connection options can't be used if sslmode=None.
        if (option.Keyword == "sslmode" && (value == "none" || value == "disabled"))
          sslModeIsNone = true;

        if (sslModeIsNone && (option.Keyword == "certificatepassword" || option.Keyword == "sslcrl" || option.Keyword == "sslca"))
          throw new ArgumentException(Resources.InvalidOptionWhenSslDisabled);

        // Preferred is not allowed for the X Protocol.
        if (isXProtocol && option.Keyword == "sslmode" && (value == "preferred" || value == "prefered"))
          throw new ArgumentException(string.Format(Resources.InvalidSslMode, keyValue[1]));

        usedSslOptions.Add(option.Keyword);
      }
    }
  }

  internal class MySqlConnectionStringOption
  {
    public bool IsCustomized { get; }

    public MySqlConnectionStringOption(string keyword, string synonyms, Type baseType, object defaultValue, bool obsolete,
      BaseSetterDelegate setter, BaseGetterDelegate getter)
    {
      Keyword = StringUtility.ToLowerInvariant(keyword);
      if (synonyms != null)
        Synonyms = StringUtility.ToLowerInvariant(synonyms).Split(',');
      BaseType = baseType;
      Obsolete = obsolete;
      DefaultValue = defaultValue;
      BaseSetter = setter;
      BaseGetter = getter;
      IsCustomized = true;
    }

    public MySqlConnectionStringOption(string keyword, string synonyms, Type baseType, object defaultValue, bool obsolete,
      SetterDelegate setter, GetterDelegate getter)
    {
      Keyword = StringUtility.ToLowerInvariant(keyword);
      if (synonyms != null)
        Synonyms = StringUtility.ToLowerInvariant(synonyms).Split(',');
      BaseType = baseType;
      Obsolete = obsolete;
      DefaultValue = defaultValue;
      Setter = setter;
      Getter = getter;
      IsCustomized = true;
    }

    public MySqlConnectionStringOption(string keyword, string synonyms, Type baseType, object defaultValue, bool obsolete,
      XSetterDelegate setter, XGetterDelegate getter)
    {
      Keyword = StringUtility.ToLowerInvariant(keyword);
      if (synonyms != null)
        Synonyms = StringUtility.ToLowerInvariant(synonyms).Split(',');
      BaseType = baseType;
      Obsolete = obsolete;
      DefaultValue = defaultValue;
      XSetter = setter;
      XGetter = getter;
      IsCustomized = true;
    }

    public MySqlConnectionStringOption(string keyword, string synonyms, Type baseType, object defaultValue, bool obsolete) :
      this(keyword, synonyms, baseType, defaultValue, obsolete,
       delegate (MySqlBaseConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value)
       {
         sender.ValidateValue(ref value);
         //if ( sender.BaseType.IsEnum )
         //  msb.SetValue( sender.Keyword, Enum.Parse( sender.BaseType, ( string )value, true ));
         //else
         msb.SetInternalValue(sender.Keyword, Convert.ChangeType(value, sender.BaseType));
       },
        (msb, sender) => msb.values[sender.Keyword]
      )
    {
      IsCustomized = false;
    }

    public string[] Synonyms { get; private set; }
    public bool Obsolete { get; private set; }
    public Type BaseType { get; private set; }
    public string Keyword { get; private set; }
    public object DefaultValue { get; private set; }
    public BaseSetterDelegate BaseSetter { get; private set; }
    public BaseGetterDelegate BaseGetter { get; private set; }
    public SetterDelegate Setter { get; private set; }
    public GetterDelegate Getter { get; private set; }
    public XSetterDelegate XSetter { get; private set; }
    public XGetterDelegate XGetter { get; private set; }

    public delegate void BaseSetterDelegate(MySqlBaseConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value);
    public delegate object BaseGetterDelegate(MySqlBaseConnectionStringBuilder msb, MySqlConnectionStringOption sender);
    public delegate void SetterDelegate(MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value);
    public delegate object GetterDelegate(MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender);
    public delegate void XSetterDelegate(MySqlXConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value);
    public delegate object XGetterDelegate(MySqlXConnectionStringBuilder msb, MySqlConnectionStringOption sender);

    public bool HasKeyword(string key)
    {
      if (Keyword == key) return true;
      if (Synonyms == null) return false;
      return Synonyms.Any(syn => syn == key);
    }

    public void Clean(MySqlBaseConnectionStringBuilder builder)
    {
      builder.Remove(Keyword);
      if (Synonyms == null) return;
      foreach (var syn in Synonyms)
        builder.Remove(syn);
    }

    public void ValidateValue(ref object value)
    {
      bool b;
      if (value == null) return;
      string typeName = BaseType.Name;
      Type valueType = value.GetType();
      if (valueType.Name == "String")
      {
        if (BaseType == valueType) return;
        else if (BaseType == typeof(bool))
        {
          if (string.Compare("yes", (string)value, StringComparison.OrdinalIgnoreCase) == 0) value = true;
          else if (string.Compare("no", (string)value, StringComparison.OrdinalIgnoreCase) == 0) value = false;
          else if (Boolean.TryParse(value.ToString(), out b)) value = b;
          else throw new ArgumentException(String.Format(Resources.ValueNotCorrectType, value));
          return;
        }
      }

      if (typeName == "Boolean" && Boolean.TryParse(value.ToString(), out b)) { value = b; return; }

      UInt64 uintVal;
      if (typeName.StartsWith("UInt64") && UInt64.TryParse(value.ToString(), out uintVal)) { value = uintVal; return; }

      UInt32 uintVal32;
      if (typeName.StartsWith("UInt32") && UInt32.TryParse(value.ToString(), out uintVal32)) { value = uintVal32; return; }

      Int64 intVal;
      if (typeName.StartsWith("Int64") && Int64.TryParse(value.ToString(), out intVal)) { value = intVal; return; }

      Int32 intVal32;
      if (typeName.StartsWith("Int32") && Int32.TryParse(value.ToString(), out intVal32)) { value = intVal32; return; }

      object objValue;
      Type baseType = BaseType.GetTypeInfo().BaseType;
      if (baseType != null && baseType.Name == "Enum" && ParseEnum(value.ToString(), out objValue))
      {
        value = objValue; return;
      }

      throw new ArgumentException(String.Format(Resources.ValueNotCorrectType, value));
    }

    public void ValidateValue(ref object value, string keyword)
    {
      string typeName = BaseType.Name;
      Type valueType = value.GetType();

      switch (keyword)
      {
        case "connect-timeout":
          if (typeName != valueType.Name  && !uint.TryParse(value.ToString(), out uint uintVal)) throw new FormatException(ResourcesX.InvalidConnectionTimeoutValue);
          break;
      }
    }

    private bool ParseEnum(string requestedValue, out object value)
    {
      value = null;
      try
      {
        value = Enum.Parse(BaseType, requestedValue, true);
        return true;
      }
      catch (ArgumentException)
      {
        return false;
      }
    }
  }

  internal class MySqlConnectionStringOptionCollection : Dictionary<string, MySqlConnectionStringOption>
  {
    internal List<MySqlConnectionStringOption> Options { get; }

    internal MySqlConnectionStringOptionCollection() : base(StringComparer.OrdinalIgnoreCase)
    {
      Options = new List<MySqlConnectionStringOption>();
    }

    internal void Add(MySqlConnectionStringOption option)
    {
      Options.Add(option);
      // Register the option with all the keywords.
      base.Add(option.Keyword, option);
      if (option.Synonyms == null) return;

      foreach (string t in option.Synonyms)
        base.Add(t, option);
    }

    internal MySqlConnectionStringOption Get(string keyword)
    {
      MySqlConnectionStringOption option = null;
      base.TryGetValue(keyword, out option);
      return option;
    }
  }
}
