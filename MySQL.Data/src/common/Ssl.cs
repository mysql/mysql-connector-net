// Copyright (c) 2004, 2023, Oracle and/or its affiliates.
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

using MySql.Data.common;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Utilities.IO.Pem;

namespace MySql.Data.Common
{
  /// <summary>
  /// Handles SSL connections for the Classic and X protocols.
  /// </summary>
  internal class Ssl
  {
    #region Fields

    /// <summary>
    /// Contains the connection options provided by the user.
    /// </summary>
    private MySqlConnectionStringBuilder _settings;

    /// <summary>
    /// A flag to establish how certificates are to be treated and validated.
    /// </summary>
    private bool _treatCertificatesAsPemFormat;

    /// <summary>
    /// Defines the supported TLS protocols.
    /// </summary>
    private static SslProtocols[] tlsProtocols = new SslProtocols[] { SslProtocols.Tls12 };
    private static Dictionary<string, SslProtocols> tlsConnectionRef = new Dictionary<string, SslProtocols>();
    private static Dictionary<string, int> tlsRetry = new Dictionary<string, int>();
    private static Object thisLock = new Object();

    #endregion

    public Ssl(MySqlConnectionStringBuilder settings)
    {
      this._settings = settings;
      // Set default value to true since PEM files is the standard for MySQL SSL certificates.
      _treatCertificatesAsPemFormat = true;
    }

    public Ssl(string server, MySqlSslMode sslMode, string certificateFile, MySqlCertificateStoreLocation certificateStoreLocation,
        string certificatePassword, string certificateThumbprint, string sslCa, string sslCert, string sslKey, string tlsVersion, uint connectionTimeout)
    {
      this._settings = new MySqlConnectionStringBuilder()
      {
        Server = server,
        SslMode = sslMode,
        CertificateFile = certificateFile,
        CertificateStoreLocation = certificateStoreLocation,
        CertificatePassword = certificatePassword,
        CertificateThumbprint = certificateThumbprint,
        SslCa = sslCa,
        SslCert = sslCert,
        SslKey = sslKey,
        TlsVersion = tlsVersion,
        ConnectionTimeout = connectionTimeout
      };
      // Set default value to true since PEM files is the standard for MySQL SSL certificates.
      _treatCertificatesAsPemFormat = true;
    }

    /// <summary>
    /// Retrieves a certificate from PEM file.
    /// </summary>
    private X509Certificate2 GetCertificateFromPEM(string certificatePath, string certificatePassword)
    {
      var certParser = new Org.BouncyCastle.X509.X509CertificateParser();
      var cert = certParser.ReadCertificate(File.ReadAllBytes(certificatePath));
      return new X509Certificate2(cert.GetEncoded(),certificatePassword);
    }
    

    /// <summary>
    /// Retrieves a collection containing the client SSL PFX certificates.
    /// </summary>
    /// <remarks>Dependent on connection string settings.
    /// Either file or store based certificates are used.</remarks>
    private X509CertificateCollection GetPFXClientCertificates()
    {
      X509CertificateCollection certs = new X509CertificateCollection();

      // Check for file-based certificate
      if (_settings.CertificateFile != null)
      {
        if (_treatCertificatesAsPemFormat)
        {
          certs.Add(GetCertificateFromPEM(_settings.CertificateFile, _settings.CertificatePassword));
          return certs;
        }
        else
        {
          X509Certificate2 clientCert = new X509Certificate2(_settings.CertificateFile,
            _settings.CertificatePassword);
          certs.Add(clientCert);
          return certs;
        }
      }

      if (_settings.CertificateStoreLocation == MySqlCertificateStoreLocation.None)
        return certs;

      StoreLocation location =
          (_settings.CertificateStoreLocation == MySqlCertificateStoreLocation.CurrentUser) ?
          StoreLocation.CurrentUser : StoreLocation.LocalMachine;

      try
      {
        // Check for store-based certificate
        X509Store store = new X509Store(StoreName.My, location);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);


        if (_settings.CertificateThumbprint == null)
        {
          // Return all certificates from the store.
          certs.AddRange(store.Certificates);

          if (certs.Count == 0)
            throw new MySqlException("No certificates were found in the certificate store");

          return certs;
        }
        else
        {
          bool validateCert = _settings.SslMode == MySqlSslMode.VerifyCA || _settings.SslMode == MySqlSslMode.VerifyFull;

          // Find certificate with given thumbprint
          certs.AddRange(store.Certificates.Find(X509FindType.FindByThumbprint,
                    _settings.CertificateThumbprint, validateCert));

          if (certs.Count == 0)
            throw new MySqlException(String.Format(Resources.InvalidCertificateThumbprint, _settings.CertificateThumbprint));

          return certs;
        }
      }
      catch (CryptographicException ex)
      {
        throw new MySqlException("Certificate couldn't be loaded from the CertificateStoreLocation", ex);
      }
    }

    /// <summary>
    /// Initiates the SSL connection.
    /// </summary>
    /// <param name="baseStream">The base stream.</param>
    /// <param name="encoding">The encoding used in the SSL connection.</param>
    /// <param name="connectionString">The connection string used to establish the connection.</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="MySqlStream"/> instance ready to initiate an SSL connection.</returns>
    public async Task<Tuple<MySqlStream, Stream>> StartSSLAsync(Stream baseStream, Encoding encoding, string connectionString, CancellationToken cancellationToken, bool execAsync)
    {
      // If SslCa connection option was provided, check for the file extension as it can also be set as a PFX file.
      if (_settings.SslCa != null)
      {
        var fileExtension = GetCertificateFileExtension(_settings.SslCa, true);

        if (fileExtension != null)
          _treatCertificatesAsPemFormat = fileExtension != "pfx";
      }

      RemoteCertificateValidationCallback sslValidateCallback = new RemoteCertificateValidationCallback(ServerCheckValidation);
      SslStream sslStream = new SslStream(baseStream, false, sslValidateCallback, null);
      X509CertificateCollection certs = (_treatCertificatesAsPemFormat &&
        _settings.CertificateStoreLocation == MySqlCertificateStoreLocation.None)
        ? new X509CertificateCollection()
        : GetPFXClientCertificates();

      string connectionId = connectionString.GetHashCode().ToString();
      SslProtocols tlsProtocol = SslProtocols.None;

      if (_settings.TlsVersion != null)
      {
#if NET452 || NETSTANDARD2_0
        if (_settings.TlsVersion.Equals("Tls13", StringComparison.OrdinalIgnoreCase))
          throw new NotSupportedException(Resources.Tlsv13NotSupported);
#endif

        SslProtocols sslProtocolsToUse = (SslProtocols)Enum.Parse(typeof(SslProtocols), _settings.TlsVersion);
        List<SslProtocols> listProtocols = new List<SslProtocols>();

#if NET48 || NETSTANDARD2_1 || NET5_0_OR_GREATER
        if (sslProtocolsToUse.HasFlag((SslProtocols)12288))
          listProtocols.Add((SslProtocols)12288);
#endif

        if (sslProtocolsToUse.HasFlag(SslProtocols.Tls12))
          listProtocols.Add(SslProtocols.Tls12);

        tlsProtocols = listProtocols.ToArray();
      }

      if (tlsConnectionRef.ContainsKey(connectionId))
      {
        tlsProtocol = tlsConnectionRef[connectionId];
      }
      else
      {
        if (!tlsRetry.ContainsKey(connectionId))
        {
          lock (tlsRetry)
          {
            tlsRetry[connectionId] = 0;
          }
        }
        for (int i = tlsRetry[connectionId]; i < tlsProtocols.Length; i++)
        {
          tlsProtocol |= tlsProtocols[i];
        }
      }
      try
      {
        tlsProtocol = (tlsProtocol == SslProtocols.None) ? SslProtocols.Tls12 : tlsProtocol;

        if (execAsync)
        {
          using (cancellationToken.Register(() => throw new AggregateException($"Authentication to host '{_settings.Server}' failed.", new IOException())))
            await sslStream.AuthenticateAsClientAsync(_settings.Server, certs, tlsProtocol, false).ConfigureAwait(false);
        }
        else
        {
          using (cancellationToken.Register(() => throw new AggregateException($"Authentication to host '{_settings.Server}' failed.", new IOException())))
            sslStream.AuthenticateAsClientAsync(_settings.Server, certs, tlsProtocol, false).GetAwaiter().GetResult();
        }

        lock (tlsConnectionRef)
        {
          tlsConnectionRef[connectionId] = tlsProtocol;
        }
        tlsRetry.Remove(connectionId);
      }
      catch (AggregateException ex)
      {
        if (ex.GetBaseException() is IOException)
        {
          tlsConnectionRef.Remove(connectionId);
          if (tlsRetry.ContainsKey(connectionId))
          {
            if (tlsRetry[connectionId] > tlsProtocols.Length)
              throw new MySqlException(Resources.SslConnectionError, ex);
            tlsRetry[connectionId] += 1;
          }
        }
        throw ex.GetBaseException();
      }

      baseStream = sslStream;
      MySqlStream stream = new MySqlStream(sslStream, encoding, false);
      stream.SequenceByte = 2;

      return new Tuple<MySqlStream, Stream>(stream, baseStream);
    }

    /// <summary>
    /// Verifies the SSL certificates used for authentication.
    /// </summary>
    /// <param name="sender">An object that contains state information for this validation.</param>
    /// <param name="certificate">The MySQL server certificate used to authenticate the remote party.</param>
    /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
    /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
    /// <returns><c>true</c> if no errors were found based on the selected SSL mode; <c>false</c>, otherwise.</returns>
    private bool ServerCheckValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.None)
        return true;

      if (_settings.SslMode == MySqlSslMode.Required ||
          _settings.SslMode == MySqlSslMode.Preferred)
      {
        // Tolerate all certificate errors.
        return true;
      }

      // Validate PEM certificates using Bouncy Castle.
      if (_treatCertificatesAsPemFormat)
      {
        SslPemCertificateValidator.ValidateCertificate(chain, _settings);
        return true;
      }
      // Validate PFX certificate errors.
      else if (_settings.SslMode == MySqlSslMode.VerifyCA &&
          sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
      {
        // Tolerate name mismatch in certificate, if full validation is not requested.
        return true;
      }

      return false;
    }

    /// <summary>
    /// Gets the extension of the specified file.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <param name="toLowerCase">Flag to indicate if the result should be converted to lower case.</param>
    /// <remarks>The . character is ommited from the result.</remarks>
    /// <returns></returns>
    private string GetCertificateFileExtension(string filePath, bool toLowerCase)
    {
      if (filePath == null || !File.Exists(filePath))
        return null;

      var extension = Path.GetExtension(filePath);
      extension = string.IsNullOrEmpty(extension)
        ? null
        : extension.Substring(extension.IndexOf(".") + 1);

      return toLowerCase
        ? extension.ToLowerInvariant()
        : extension;
    }
  }
}
