// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Data.Common;
#if !RT
using System.Security.Permissions;
#endif
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using MySql.Data.MySqlClient.Properties;
using System.Collections;
using System.Globalization;
using System.Security;

namespace MySql.Data.MySqlClient
{
  public class MySqlConnectionStringBuilder : DbConnectionStringBuilder
  {
    private static Dictionary<string, string> validKeywords =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, PropertyDefaultValue> defaultValues =
        new Dictionary<string, PropertyDefaultValue>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, object> _values =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, object> values
    {
      get { lock (this) { return _values; } }
    }

    #region Backwards compatibility properties

    [DisplayName("Use Default Command Timeout For EF")]
    [Category("Backwards Compatibility")]
    [Description("Enforces the command timeout of EFMySqlCommand to the value provided in 'DefaultCommandTimeout' property")]
    [DefaultValue(false)]
    public bool UseDefaultCommandTimeoutForEF
    {
      get { return (bool)values["Use Default Command Timeout For EF"]; }
      set { SetValue("Use Default Command Timeout For EF", value); }
    }

    #endregion

    private bool hasProcAccess = true;
#if !CF && !RT
    private PermissionSet _permissionset;
#endif
    static MySqlConnectionStringBuilder()
    {
      // load up our valid keywords and default values only once
      Initialize();
    }

    public MySqlConnectionStringBuilder()
    {
      Clear();
    }

    public MySqlConnectionStringBuilder(string connStr)
      : this()
    {
      ConnectionString = connStr;
    }

    #region Server Properties

    /// <summary>
    /// Gets or sets the name of the server.
    /// </summary>
    /// <value>The server.</value>
    [Category("Connection")]
    [Description("Server to connect to")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    [ValidKeywords("host, data source, datasource, address, addr, network address")]
    public string Server
    {
      get { return values["server"] as string; }
      set { SetValue("server", value); }
    }

    /// <summary>
    /// Gets or sets the name of the database the connection should 
    /// initially connect to.
    /// </summary>
    [Category("Connection")]
    [Description("Database to use initially")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    [ValidKeywords("initial catalog")]
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
    [DefaultValue(MySqlConnectionProtocol.Sockets)]
    [ValidKeywords("protocol")]
    public MySqlConnectionProtocol ConnectionProtocol
    {
      get { return (MySqlConnectionProtocol)values["Connection Protocol"]; }
      set { SetValue("Connection Protocol", value); }
    }

    /// <summary>
    /// Gets or sets the name of the named pipe that should be used
    /// for communicating with MySQL.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Pipe Name")]
    [Description("Name of pipe to use when connecting with named pipes (Win32 only)")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("MYSQL")]
    [ValidKeywords("pipe")]
    public string PipeName
    {
      get { return (string)values["Pipe Name"]; }
      set { SetValue("Pipe Name", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether this connection
    /// should use compression.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Use Compression")]
    [Description("Should the connection use compression")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    [ValidKeywords("compress")]
    public bool UseCompression
    {
      get { return (bool)values["Use Compression"]; }
      set { SetValue("Use Compression", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether this connection will allow
    /// commands to send multiple SQL statements in one execution.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Allow Batch")]
    [Description("Allows execution of multiple SQL commands in a single statement")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(true)]
    public bool AllowBatch
    {
      get { return (bool)values["Allow Batch"]; }
      set { SetValue("Allow Batch", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether logging is enabled.
    /// </summary>
    [Category("Connection")]
    [Description("Enables output of diagnostic messages")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool Logging
    {
      get { return (bool)values["Logging"]; }
      set { SetValue("Logging", value); }
    }

    /// <summary>
    /// Gets or sets the base name of the shared memory objects used to 
    /// communicate with MySQL when the shared memory protocol is being used.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Shared Memory Name")]
    [Description("Name of the shared memory object to use")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("MYSQL")]
    public string SharedMemoryName
    {
      get { return (string)values["Shared Memory Name"]; }
      set { SetValue("Shared Memory Name", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether this connection uses
    /// the old style (@) parameter markers or the new (?) style.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Use Old Syntax")]
    [Description("Allows the use of old style @ syntax for parameters")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    [ValidKeywords("old syntax, oldsyntax")]
    [Obsolete("Use Old Syntax is no longer needed.  See documentation")]
    public bool UseOldSyntax
    {
      get { return (bool)values["Use Old Syntax"]; }
      set { SetValue("Use Old Syntax", value); }
    }

    /// <summary>
    /// Gets or sets the port number that is used when the socket
    /// protocol is being used.
    /// </summary>
    [Category("Connection")]
    [Description("Port to use for TCP/IP connections")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(3306)]
    public uint Port
    {
      get { return (uint)values["Port"]; }
      set { SetValue("Port", value); }
    }

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Connect Timeout")]
    [Description("The length of time (in seconds) to wait for a connection " +
                 "to the server before terminating the attempt and generating an error.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(15)]
    [ValidKeywords("connection timeout")]
    public uint ConnectionTimeout
    {
      get { return (uint)values["Connect Timeout"]; }

      set
      {
        // Timeout in milliseconds should not exceed maximum for 32 bit
        // signed integer (~24 days). We truncate the value if it exceeds 
        // maximum (MySqlCommand.CommandTimeout uses the same technique
        uint timeout = Math.Min(value, Int32.MaxValue / 1000);
        if (timeout != value)
        {
          MySqlTrace.LogWarning(-1, "Connection timeout value too large ("
              + value + " seconds). Changed to max. possible value" +
              +timeout + " seconds)");
        }
        SetValue("Connect Timeout", timeout);
      }
    }

    /// <summary>
    /// Gets or sets the default command timeout.
    /// </summary>
    [Category("Connection")]
    [DisplayName("Default Command Timeout")]
    [Description(@"The default timeout that MySqlCommand objects will use
                     unless changed.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(30)]
    [ValidKeywords("command timeout")]
    public uint DefaultCommandTimeout
    {
      get { return (uint)values["Default Command Timeout"]; }
      set { SetValue("Default Command Timeout", value); }
    }

    #endregion

    #region Authentication Properties

    /// <summary>
    /// Gets or sets the user id that should be used to connect with.
    /// </summary>
    [Category("Security")]
    [DisplayName("User Id")]
    [Description("Indicates the user ID to be used when connecting to the data source.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    [ValidKeywords("uid, username, user name, user")]
    public string UserID
    {
      get { return (string)values["User Id"]; }
      set { SetValue("User Id", value); }
    }

    /// <summary>
    /// Gets or sets the password that should be used to connect with.
    /// </summary>
    [Category("Security")]
    [Description("Indicates the password to be used when connecting to the data source.")]
    [PasswordPropertyText(true)]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    [ValidKeywords("pwd")]
    public string Password
    {
      get { return (string)values["Password"]; }
      set { SetValue("Password", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value that indicates if the password should be persisted
    /// in the connection string.
    /// </summary>
    [Category("Security")]
    [DisplayName("Persist Security Info")]
    [Description("When false, security-sensitive information, such as the password, " +
                 "is not returned as part of the connection if the connection is open or " +
                 "has ever been in an open state.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool PersistSecurityInfo
    {
      get { return (bool)values["Persist Security Info"]; }
      set { SetValue("Persist Security Info", value); }
    }

#if !CF 
    [Category("Authentication")]
    [Description("Should the connection use SSL.")]
    [DefaultValue(false)]
    [Obsolete("Use Ssl Mode instead.")]
    internal bool Encrypt
    {
      get { return SslMode != MySqlSslMode.None; }
      set
      {
        SetValue("Ssl Mode", value ? MySqlSslMode.Prefered : MySqlSslMode.None);
      }
    }

    [Category("Authentication")]
    [DisplayName("Certificate File")]
    [Description("Certificate file in PKCS#12 format (.pfx)")]
    [DefaultValue(null)]
    public string CertificateFile
    {
      get { return (string)values["Certificate File"]; }
      set
      {
        SetValue("Certificate File", value);
      }
    }

    [Category("Authentication")]
    [DisplayName("Certificate Password")]
    [Description("Password for certificate file")]
    [DefaultValue(null)]
    public string CertificatePassword
    {
      get { return (string)values["Certificate Password"]; }
      set
      {
        SetValue("Certificate Password", value);
      }
    }

    [Category("Authentication")]
    [DisplayName("Certificate Store Location")]
    [Description("Certificate Store Location for client certificates")]
    [DefaultValue(MySqlCertificateStoreLocation.None)]
    public MySqlCertificateStoreLocation CertificateStoreLocation
    {
      get { return (MySqlCertificateStoreLocation)values["Certificate Store Location"]; }
      set
      {
        SetValue("Certificate Store Location", value);
      }
    }

    [Category("Authentication")]
    [DisplayName("Certificate Thumbprint")]
    [Description("Certificate thumbprint. Can be used together with Certificate " +
        "Store Location parameter to uniquely identify certificate to be used " +
        "for SSL authentication.")]
    [DefaultValue(null)]
    public string CertificateThumbprint
    {
      get { return (string)values["Certificate Thumbprint"]; }
      set
      {
        SetValue("Certificate Thumbprint", value);
      }
    }
#endif

    [Category("Authentication")]
    [DisplayName("Integrated Security")]
    [Description("Use windows authentication when connecting to server")]
    [DefaultValue(false)]
    public bool IntegratedSecurity
    {
      get
      {
        object val = values["Integrated Security"];
        return (bool)val;
      }
      set
      {
        if (!MySql.Data.Common.Platform.IsWindows())
          throw new MySqlException("IntegratedSecurity is supported on Windows only");

        SetValue("Integrated Security", value);
      }
    }

    #endregion

    #region Other Properties

    /// <summary>
    /// Gets or sets a boolean value that indicates if zero date time values are supported.
    /// </summary>
    [Category("Advanced")]
    [DisplayName("Allow Zero Datetime")]
    [Description("Should zero datetimes be supported")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool AllowZeroDateTime
    {
      get { return (bool)values["Allow Zero Datetime"]; }
      set { SetValue("Allow Zero DateTime", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value indicating if zero datetime values should be 
    /// converted to DateTime.MinValue.
    /// </summary>
    [Category("Advanced")]
    [DisplayName("Convert Zero Datetime")]
    [Description("Should illegal datetime values be converted to DateTime.MinValue")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool ConvertZeroDateTime
    {
      get { return (bool)values["Convert Zero Datetime"]; }
      set { SetValue("Convert Zero DateTime", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value indicating if the Usage Advisor should be enabled.
    /// </summary>
    [Category("Advanced")]
    [DisplayName("Use Usage Advisor")]
    [Description("Logs inefficient database operations")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    [ValidKeywords("usage advisor")]
    public bool UseUsageAdvisor
    {
      get { return (bool)values["Use Usage Advisor"]; }
      set { SetValue("Use Usage Advisor", value); }
    }

    /// <summary>
    /// Gets or sets the size of the stored procedure cache.
    /// </summary>
    [Category("Advanced")]
    [DisplayName("Procedure Cache Size")]
    [Description("Indicates how many stored procedures can be cached at one time. " +
                 "A value of 0 effectively disables the procedure cache.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(25)]
    [ValidKeywords("procedure cache, procedurecache")]
    public uint ProcedureCacheSize
    {
      get { return (uint)values["Procedure Cache Size"]; }
      set { SetValue("Procedure Cache Size", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value indicating if the permon hooks should be enabled.
    /// </summary>
    [Category("Advanced")]
    [DisplayName("Use Performance Monitor")]
    [Description("Indicates that performance counters should be updated during execution.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    [ValidKeywords("userperfmon, perfmon")]
    public bool UsePerformanceMonitor
    {
      get { return (bool)values["Use Performance Monitor"]; }
      set { SetValue("Use Performance Monitor", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value indicating if calls to Prepare() should be ignored.
    /// </summary>
    [Category("Advanced")]
    [DisplayName("Ignore Prepare")]
    [Description("Instructs the provider to ignore any attempts to prepare a command.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(true)]
    public bool IgnorePrepare
    {
      get { return (bool)values["Ignore Prepare"]; }
      set { SetValue("Ignore Prepare", value); }
    }

    [Category("Advanced")]
    [DisplayName("Use Procedure Bodies")]
    [Description("Indicates if stored procedure bodies will be available for parameter detection.")]
    [DefaultValue(true)]
    [ValidKeywords("procedure bodies")]
    [Obsolete("Use CheckParameters instead")]
    public bool UseProcedureBodies
    {
      get { return (bool)values["Use Procedure Bodies"]; }
      set { SetValue("Use Procedure Bodies", value); }
    }

    [Category("Advanced")]
    [DisplayName("Auto Enlist")]
    [Description("Should the connetion automatically enlist in the active connection, if there are any.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(true)]
    public bool AutoEnlist
    {
      get { return (bool)values["Auto Enlist"]; }
      set { SetValue("Auto Enlist", value); }
    }

    [Category("Advanced")]
    [DisplayName("Respect Binary Flags")]
    [Description("Should binary flags on column metadata be respected.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(true)]
    public bool RespectBinaryFlags
    {
      get { return (bool)values["Respect Binary Flags"]; }
      set { SetValue("Respect Binary Flags", value); }
    }

    [Category("Advanced")]
    [DisplayName("Treat Tiny As Boolean")]
    [Description("Should the provider treat TINYINT(1) columns as boolean.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(true)]
    public bool TreatTinyAsBoolean
    {
      get { return (bool)values["Treat Tiny As Boolean"]; }
      set { SetValue("Treat Tiny As Boolean", value); }
    }

    [Category("Advanced")]
    [DisplayName("Allow User Variables")]
    [Description("Should the provider expect user variables to appear in the SQL.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool AllowUserVariables
    {
      get { return (bool)values["Allow User Variables"]; }
      set { SetValue("Allow User Variables", value); }
    }

    [Category("Advanced")]
    [DisplayName("Interactive Session")]
    [Description("Should this session be considered interactive?")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    [ValidKeywords("interactive")]
    public bool InteractiveSession
    {
      get { return (bool)values["Interactive Session"]; }
      set { SetValue("Interactive Session", value); }
    }

    [Category("Advanced")]
    [DisplayName("Functions Return String")]
    [Description("Should all server functions be treated as returning string?")]
    [DefaultValue(false)]
    public bool FunctionsReturnString
    {
      get { return (bool)values["Functions Return String"]; }
      set { SetValue("Functions Return String", value); }
    }

    [Category("Advanced")]
    [DisplayName("Use Affected Rows")]
    [Description("Should the returned affected row count reflect affected rows instead of found rows?")]
    [DefaultValue(false)]
    public bool UseAffectedRows
    {
      get { return (bool)values["Use Affected Rows"]; }
      set { SetValue("Use Affected Rows", value); }
    }

    [Category("Advanced")]
    [DisplayName("Old Guids")]
    [Description("Treat binary(16) columns as guids")]
    [DefaultValue(false)]
    public bool OldGuids
    {
      get { return (bool)values["Old Guids"]; }
      set { SetValue("Old Guids", value); }
    }

    [DisplayName("Keep Alive")]
    [Description("For TCP connections, idle connection time measured in seconds, before the first keepalive packet is sent." +
        "A value of 0 indicates that keepalive is not used.")]
    [DefaultValue(0)]
    public uint Keepalive
    {
      get { return (uint)values["Keep Alive"]; }
      set { SetValue("Keep Alive", value); }
    }

    [Category("Advanced")]
    [DisplayName("Sql Server Mode")]
    [Description("Allow Sql Server syntax.  " +
        "A value of yes allows symbols to be enclosed with [] instead of ``.  This does incur " +
        "a performance hit so only use when necessary.")]
    [DefaultValue(false)]
    [ValidKeywords("sqlservermode, sql server mode")]
    public bool SqlServerMode
    {
      get { return (bool)values["Sql Server Mode"]; }
      set { SetValue("Sql Server Mode", value); }
    }

    [Category("Advanced")]
    [DisplayName("Table Cache")]
    [Description(@"Enables or disables caching of TableDirect command.  
            A value of yes enables the cache while no disables it.")]
    [DefaultValue(false)]
    [ValidKeywords("tablecache, table cache")]
    public bool TableCaching
    {
      get { return (bool)values["Table Cache"]; }
      set { SetValue("Table Cache", value); }
    }

    [Category("Advanced")]
    [DisplayName("Default Table Cache Age")]
    [Description(@"Specifies how long a TableDirect result should be cached in seconds.")]
    [DefaultValue(60)]
    public int DefaultTableCacheAge
    {
      get { return (int)values["Default Table Cache Age"]; }
      set { SetValue("Default Table Cache Age", value); }
    }

    [Category("Advanced")]
    [DisplayName("Check Parameters")]
    [Description("Indicates if stored routine parameters should be checked against the server.")]
    [DefaultValue(true)]
    public bool CheckParameters
    {
      get { return (bool)values["Check Parameters"]; }
      set { SetValue("Check Parameters", value); }
    }

    [Category("Advanced")]
    [DisplayName("Replication")]
    [Description("Indicates if this connection is to use replicated servers.")]
    [DefaultValue(false)]
    public bool Replication
    {
      get { return (bool)values["Replication"]; }
      set { SetValue("Replication", value); }
    }

    [Category("Advanced")]
    [DisplayName("Exception Interceptors")]
    [Description("The list of interceptors that can triage thrown MySqlExceptions.")]
    [DefaultValue("")]
    public string ExceptionInterceptors
    {
      get { return (string)values["Exception Interceptors"]; }
      set { SetValue("Exception Interceptors", value); }
    }

    [Category("Advanced")]
    [DisplayName("Command Interceptors")]
    [Description("The list of interceptors that can intercept command operations.")]
    [DefaultValue("")]
    public string CommandInterceptors
    {
      get { return (string)values["Command Interceptors"]; }
      set { SetValue("Command Interceptors", value); }
    }

    [Category("Advanced")]
    [DisplayName("Include Security Asserts")]
    [Description("Include security asserts to support Medium Trust")]
    [DefaultValue(false)]
    [ValidKeywords("includesecurityasserts, include security asserts")]
    public bool IncludeSecurityAsserts
    {
      get { return (bool)values["Include Security Asserts"]; }
      set { SetValue("Include Security Asserts", value); }
    }

    #endregion

    #region Pooling Properties

    /// <summary>
    /// Gets or sets the lifetime of a pooled connection.
    /// </summary>
    [Category("Pooling")]
    [DisplayName("Connection Lifetime")]
    [Description("The minimum amount of time (in seconds) for this connection to " +
                 "live in the pool before being destroyed.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(0)]
    public uint ConnectionLifeTime
    {
      get { return (uint)values["Connection LifeTime"]; }
      set { SetValue("Connection LifeTime", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value indicating if connection pooling is enabled.
    /// </summary>
    [Category("Pooling")]
    [Description("When true, the connection object is drawn from the appropriate " +
                 "pool, or if necessary, is created and added to the appropriate pool.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(true)]
    public bool Pooling
    {
      get { return (bool)values["Pooling"]; }
      set { SetValue("Pooling", value); }
    }

    /// <summary>
    /// Gets the minimum connection pool size.
    /// </summary>
    [Category("Pooling")]
    [DisplayName("Minimum Pool Size")]
    [Description("The minimum number of connections allowed in the pool.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(0)]
    [ValidKeywords("min pool size")]
    public uint MinimumPoolSize
    {
      get { return (uint)values["Minimum Pool Size"]; }
      set { SetValue("Minimum Pool Size", value); }
    }

    /// <summary>
    /// Gets or sets the maximum connection pool setting.
    /// </summary>
    [Category("Pooling")]
    [DisplayName("Maximum Pool Size")]
    [Description("The maximum number of connections allowed in the pool.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(100)]
    [ValidKeywords("max pool size")]
    public uint MaximumPoolSize
    {
      get { return (uint)values["Maximum Pool Size"]; }
      set { SetValue("Maximum Pool Size", value); }
    }

    /// <summary>
    /// Gets or sets a boolean value indicating if the connection should be reset when retrieved
    /// from the pool.
    /// </summary>
    [Category("Pooling")]
    [DisplayName("Connection Reset")]
    [Description("When true, indicates the connection state is reset when " +
                 "removed from the pool.")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool ConnectionReset
    {
      get { return (bool)values["Connection Reset"]; }
      set { SetValue("Connection Reset", value); }
    }

    [Category("Pooling")]
    [DisplayName("Cache Server Properties")]
    [Description("When true, server properties will be cached after the first server in the pool is created")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool CacheServerProperties
    {
      get { return (bool)values["Cache Server Properties"]; }
      set { SetValue("Cache Server Properties", value); }
    }

    #endregion

    #region Language and Character Set Properties

    /// <summary>
    /// Gets or sets the character set that should be used for sending queries to the server.
    /// </summary>
    [DisplayName("Character Set")]
    [Category("Advanced")]
    [Description("Character set this connection should use")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    [ValidKeywords("charset")]
    public string CharacterSet
    {
      get { return (string)values["Character Set"]; }
      set { SetValue("Character Set", value); }
    }

    /// <summary>
    /// Indicates whether the driver should treat binary blobs as UTF8
    /// </summary>
    [DisplayName("Treat Blobs As UTF8")]
    [Category("Advanced")]
    [Description("Should binary blobs be treated as UTF8")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue(false)]
    public bool TreatBlobsAsUTF8
    {
      get { return (bool)values["Treat Blobs As UTF8"]; }
      set { SetValue("Treat Blobs As UTF8", value); }
    }

    /// <summary>
    /// Gets or sets the pattern that matches the columns that should be treated as UTF8
    /// </summary>
    [Category("Advanced")]
    [Description("Pattern that matches columns that should be treated as UTF8")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    public string BlobAsUTF8IncludePattern
    {
      get { return (string)values["BlobAsUTF8IncludePattern"]; }
      set { SetValue("BlobAsUTF8IncludePattern", value); }
    }

    /// <summary>
    /// Gets or sets the pattern that matches the columns that should not be treated as UTF8
    /// </summary>
    [Category("Advanced")]
    [Description("Pattern that matches columns that should not be treated as UTF8")]
    [RefreshProperties(RefreshProperties.All)]
    [DefaultValue("")]
    public string BlobAsUTF8ExcludePattern
    {
      get { return (string)values["BlobAsUTF8ExcludePattern"]; }
      set { SetValue("BlobAsUTF8ExcludePattern", value); }
    }

#if !CF && !RT
    /// <summary>
    /// Indicates whether to use SSL connections and how to handle server certificate errors.
    /// </summary>
    [DisplayName("Ssl Mode")]
    [Category("Security")]
    [Description("SSL properties for connection")]
    [DefaultValue(MySqlSslMode.None)]
    public MySqlSslMode SslMode
    {
      get { return (MySqlSslMode)values["Ssl Mode"]; }
      set { SetValue("Ssl Mode", value); }
    }
#endif

    #endregion

    internal bool HasProcAccess
    {
      get { return hasProcAccess; }
      set { hasProcAccess = value; }
    }

    internal Regex GetBlobAsUTF8IncludeRegex()
    {
      if (String.IsNullOrEmpty(BlobAsUTF8IncludePattern)) return null;
      return new Regex(BlobAsUTF8IncludePattern);
    }

    internal Regex GetBlobAsUTF8ExcludeRegex()
    {
      if (String.IsNullOrEmpty(BlobAsUTF8ExcludePattern)) return null;
      return new Regex(BlobAsUTF8ExcludePattern);
    }

#if !CF
    public override bool ContainsKey(string keyword)
    {
      try
      {
        object value;
        ValidateKeyword(keyword);
        return values.TryGetValue(validKeywords[keyword], out value);
      }
      catch (Exception)
      {
        return false;
      }
    }
#endif

    public override object this[string keyword]
    {
      get { return values[validKeywords[keyword]]; }
      set
      {
        ValidateKeyword(keyword);
        if (value == null)
          Remove(keyword);
        else
          SetValue(keyword, value);
      }
    }

    public override void Clear()
    {
      base.Clear();

      // make a copy of our default values array
      foreach (string key in defaultValues.Keys)
        values[key] = defaultValues[key].DefaultValue;
    }

#if !CF

    public override bool Remove(string keyword)
    {
      ValidateKeyword(keyword);
      string primaryKey = validKeywords[keyword];

      values.Remove(primaryKey);
      base.Remove(primaryKey);

      values[primaryKey] = defaultValues[primaryKey].DefaultValue;
      return true;
    }

    public override bool TryGetValue(string keyword, out object value)
    {
      if (validKeywords.ContainsKey(keyword))
      {
        if (values.TryGetValue(validKeywords[keyword], out value))
          return true;
      }
      value = null;
      return false;
    }

#endif

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

    private void SetValue(string keyword, object value)
    {
      ValidateKeyword(keyword);
      keyword = validKeywords[keyword];

      lock (this)
      {
        Remove(keyword);

        NormalizeValue(keyword, ref value);

        object val = null;
        if (value is string && defaultValues[keyword].DefaultValue is Enum)
          val = ParseEnum(defaultValues[keyword].Type, (string)value, keyword);
        else if (value is string && string.IsNullOrEmpty(value.ToString()))
          val = defaultValues[keyword].DefaultValue;
        else
          val = ChangeType(value, defaultValues[keyword].Type);
        HandleObsolete(keyword, val);
        values[keyword] = val;
        base[keyword] = val;
      }
    }

    private static void NormalizeValue(string keyword, ref object value)
    {
      // Handle special case "Integrated Security=SSPI"
      // Integrated Security is a logically bool parameter, SSPI value 
      // for it is the same as "true" (SSPI is SQL Server legacy value
      if (keyword == "Integrated Security" && value is string &&
          ((string)value).ToLower(CultureInfo.InvariantCulture) == "sspi")
      {
        value = true;
      }
    }

    private void HandleObsolete(string keyword, object value)
    {
      if (String.Compare(keyword, "Use Old Syntax", StringComparison.OrdinalIgnoreCase) == 0)
        MySqlTrace.LogWarning(-1, "Use Old Syntax is now obsolete.  Please see documentation");
#if !CF
      else if (String.Compare(keyword, "Encrypt", StringComparison.OrdinalIgnoreCase) == 0)
      {
        MySqlTrace.LogWarning(-1, "Encrypt is now obsolete. Use Ssl Mode instead");
        Encrypt = (bool)value;
      }
#endif
      else if (String.Compare(keyword, "Use Procedure Bodies", StringComparison.OrdinalIgnoreCase) == 0)
      {
        MySqlTrace.LogWarning(-1, "Use Procedure Bodies is now obsolete.  Use Check Parameters instead");
        CheckParameters = (bool)value;
      }
    }

    private object ParseEnum(Type t, string requestedValue, string key)
    {
      try
      {
        return Enum.Parse(t, requestedValue, true);
      }
      catch (ArgumentException)
      {
        throw new InvalidOperationException(String.Format(
            Resources.InvalidConnectionStringValue, requestedValue, key));
      }
    }

    private object ChangeType(object value, Type t)
    {
      if (t == typeof(bool) && value is string)
      {
        string s = value.ToString().ToLower(CultureInfo.InvariantCulture);
        if (s == "yes" || s == "true") return true;
        if (s == "no" || s == "false") return false;

        throw new FormatException(String.Format(Resources.InvalidValueForBoolean, value));
      }
      else
        return Convert.ChangeType(value, t, CultureInfo.CurrentCulture);
    }

    private void ValidateKeyword(string keyword)
    {
      string key = keyword.ToLower(CultureInfo.InvariantCulture);
      if (!validKeywords.ContainsKey(key))
        throw new ArgumentException(Resources.KeywordNotSupported, keyword);
#if CF
            if (validKeywords[key] == "Certificate File" || validKeywords[key] == "Certificate Password" || validKeywords[key] == "SSL Mode" 
                || validKeywords[key] == "Encrypt" || validKeywords[key] == "Certificate Store Location" || validKeywords[key] == "Certificate Thumbprint")
                throw new ArgumentException(Resources.KeywordNotSupported, validKeywords[key]);
#endif
    }

    private static void Initialize()
    {
      PropertyInfo[] properties = typeof(MySqlConnectionStringBuilder).GetProperties();
      foreach (PropertyInfo pi in properties)
        AddKeywordFromProperty(pi);

#if !CF
      // remove this starting with 6.4
      PropertyInfo encrypt = typeof(MySqlConnectionStringBuilder).GetProperty(
          "Encrypt", BindingFlags.Instance | BindingFlags.NonPublic);
      AddKeywordFromProperty(encrypt);
#endif
    }

    private static void AddKeywordFromProperty(PropertyInfo pi)
    {
      string name = pi.Name.ToLower(CultureInfo.InvariantCulture);
      string displayName = name;

      // now see if we have defined a display name for this property
      object[] attr = pi.GetCustomAttributes(false);
      foreach (Attribute a in attr)
        if (a is DisplayNameAttribute)
        {
          displayName = (a as DisplayNameAttribute).DisplayName;
          break;
        }

      validKeywords[name] = displayName;
      validKeywords[displayName] = displayName;

      foreach (Attribute a in attr)
      {
        if (a is ValidKeywordsAttribute)
        {
          foreach (string keyword in (a as ValidKeywordsAttribute).Keywords)
            validKeywords[keyword.ToLower(CultureInfo.InvariantCulture).Trim()] = displayName;
        }
        else if (a is DefaultValueAttribute)
        {
          defaultValues[displayName] = new PropertyDefaultValue(pi.PropertyType,
                  Convert.ChangeType((a as DefaultValueAttribute).Value, pi.PropertyType, CultureInfo.CurrentCulture));
        }
      }
    }
#if !CF && !RT
    protected internal PermissionSet CreatePermissionSet()
    {
      PermissionSet set = new PermissionSet(PermissionState.None);
      set.AddPermission(new MySqlClientPermission(this.ConnectionString));
      return set;
    }

    internal void DemandPermissions()
    {
      if (this._permissionset == null)
      {
        this._permissionset = this.CreatePermissionSet();
      }
      this._permissionset.Demand();
    }
#endif
  }

  internal struct PropertyDefaultValue
  {
    public PropertyDefaultValue(Type t, object v)
    {
      Type = t;
      DefaultValue = v;
    }

    public Type Type;
    public object DefaultValue;
  }

  internal class ValidKeywordsAttribute : Attribute
  {
    private string keywords;

    public ValidKeywordsAttribute(string keywords)
    {
      this.keywords = keywords.ToLower(CultureInfo.InvariantCulture);
    }

    public string[] Keywords
    {
      get { return keywords.Split(','); }
    }
  }
}
