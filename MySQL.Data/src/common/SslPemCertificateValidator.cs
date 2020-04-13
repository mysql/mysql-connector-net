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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Security;
using System;
using MySql.Data.MySqlClient;
using System.Net.Security;
using System.IO;

namespace MySql.Data.common
{
  /// <summary>
  /// Provides functionality to read SSL PEM certificates and to perform multiple validations via Bouncy Castle.
  /// </summary>
  internal static class SslPemCertificateValidator
  {
    public static void ValidateCertificate(
      System.Security.Cryptography.X509Certificates.X509Certificate certificate,
      MySqlBaseConnectionStringBuilder settings)
    {
      if (settings.SslMode >= MySqlSslMode.VerifyCA)
      {
        VerifyEmptyOrWhitespaceSslConnectionOption(settings.SslCa, nameof(settings.SslCa));
        var sslCA = ReadSslCertificate(settings.SslCa);
        VerifyIssuer(sslCA, certificate);
        VerifyDates(sslCA);
        VerifyCAStatus(sslCA, true);
#if NET452
        VerifySignature(sslCA, DotNetUtilities.FromX509Certificate(certificate));
#else
        VerifySignature(sslCA, new X509CertificateParser().ReadCertificate(certificate.GetRawCertData()));
#endif
      }

      if (settings.SslMode == MySqlSslMode.VerifyFull)
      {
        VerifyEmptyOrWhitespaceSslConnectionOption(settings.SslCert, nameof(settings.SslCert));
        var sslCert = ReadSslCertificate(settings.SslCert);
        VerifyDates(sslCert);
        VerifyCAStatus(sslCert, false);

        VerifyEmptyOrWhitespaceSslConnectionOption(settings.SslKey, nameof(settings.SslKey));
        var sslKey = ReadKey(settings.SslKey);
        VerifyKeyCorrespondsToCertificateKey(sslCert, sslKey);
      }
    }

    /// <summary>
    /// Raises an exception if the specified connection option is null, empty or whitespace.
    /// </summary>
    /// <param name="connectionOption">The connection option to verify.</param>
    private static void VerifyEmptyOrWhitespaceSslConnectionOption(string connectionOption, string connectionOptionName)
    {
      if (string.IsNullOrWhiteSpace(connectionOption))
        throw new MySqlException(
          Resources.SslConnectionError,
          new FileNotFoundException(string.Format(Resources.FilePathNotSet, connectionOptionName)));
    }

    #region Certificate Readers

    /// <summary>
    /// Reads the specified file as a byte array.
    /// </summary>
    /// <param name="fileName">The path of the file to read.</param>
    /// <returns>A byte array representing the read file.</returns>
    private static byte[] GetBuffer(string filePath)
    {
      byte[] buffer;
      if (filePath == null)
      {
        throw new ArgumentNullException(nameof(filePath));
      }

      try
      {
        FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        buffer = new byte[stream.Length];
        int offset = stream.Read(buffer, 0, buffer.Length);
        while (true)
        {
          if (offset >= stream.Length)
          {
            stream.Close();
            break;
          }
          offset += stream.Read(buffer, offset, buffer.Length - offset);
        }
      }
      catch (Exception)
      {
        throw new MySqlException(Resources.SslConnectionError, new FileNotFoundException(Resources.FileNotFound, filePath));
      }

      return buffer;
    }

    /// <summary>
    /// Reads the SSL certificate file.
    /// </summary>
    /// <param name="filePath">The path to the certificate file.</param>
    /// <returns>A <see cref="Org.BouncyCastle.X509.X509Certificate"/> instance representing the SSL certificate file.</returns>
    private static Org.BouncyCastle.X509.X509Certificate ReadSslCertificate(string filePath)
    {
      byte[] buffer = GetBuffer(filePath);
      var PR = new PemReader(new StreamReader(new MemoryStream(buffer)));

      try
      {
        var certificate = (Org.BouncyCastle.X509.X509Certificate)PR.ReadObject();
        if (certificate == null)
          throw new InvalidCastException();

        return certificate;
      }
      catch (InvalidCastException)
      {
        throw new MySqlException(Resources.SslConnectionError, new Exception(Resources.FileIsNotACertificate));
      }
    }

    /// <summary>
    /// Reads the SSL certificate key file.
    /// </summary>
    /// <param name="filePath">The path to the certificate key file.</param>
    /// <returns>A <see cref="AsymmetricCipherKeyPair"/> instance representing the SSL certificate key file.</returns>
    private static AsymmetricCipherKeyPair ReadKey(string filePath)
    {
      byte[] buffer = GetBuffer(filePath);
      var PR = new PemReader(new StreamReader(new MemoryStream(buffer)));

      try
      {
        var key = (AsymmetricCipherKeyPair)PR.ReadObject();
        if (key == null)
          throw new InvalidCastException();

        return key;
      }
      catch (InvalidCastException)
      {
        throw new MySqlException(Resources.SslConnectionError, new Exception(Resources.FileIsNotAKey));
      }
    }

    #endregion

    #region Certificate Veryfiers

    /// <summary>
    /// Verifies that the certificate has not yet expired.
    /// </summary>
    /// <param name="certificate">The certificate to verify.</param>
    private static void VerifyDates(Org.BouncyCastle.X509.X509Certificate certificate)
    {
      try
      {
        certificate.CheckValidity();
      }
      catch (CertificateExpiredException exception)
      {
        throw new MySqlException(Resources.SslConnectionError, exception);
      }
      catch (CertificateNotYetValidException exception)
      {
        throw new MySqlException(Resources.SslConnectionError, exception);
      }
    }

    /// <summary>
    /// Verifies a certificate CA status.
    /// </summary>
    /// <param name="certificate">The certificate to validate.</param>
    /// <param name="expectedCAStatus">A flag indicating the expected CA status.</param>
    private static void VerifyCAStatus(Org.BouncyCastle.X509.X509Certificate certificate, bool expectedCAStatus)
    {
      bool? isCA = IsCA(certificate, out var certificatePathLength);
      if (isCA == true && !expectedCAStatus)
        throw new MySqlException(Resources.SslConnectionError, new Exception(Resources.InvalidSslCertificate));
      else if (expectedCAStatus && certificate.Version == 3 && (isCA == false || isCA == null))
        throw new MySqlException(Resources.SslConnectionError, new Exception(Resources.SslCertificateIsNotCA));
    }

    /// <summary>
    /// Verifies that the certificate was signed using the private key that corresponds to the specified public key
    /// </summary>
    /// <param name="certificate">The client side certificate containing the public key.</param>
    /// <param name="serverCertificate">The server certificate.</param>
    private static void VerifySignature(Org.BouncyCastle.X509.X509Certificate certificate, Org.BouncyCastle.X509.X509Certificate serverCertificate)
    {
      VerifySignature(serverCertificate, certificate.GetPublicKey());
    }

    private static void VerifySignatureUsingKey(Org.BouncyCastle.X509.X509Certificate certificate, AsymmetricCipherKeyPair key)
    {
      VerifySignature(certificate, key.Public);
    }

    private static void VerifySignature(Org.BouncyCastle.X509.X509Certificate certificate, AsymmetricKeyParameter key)
    {
      try
      {
        certificate.Verify(key);
      }
      catch (InvalidKeyException exception)
      {
        throw new Exception(Resources.InvalidCertificateKey, exception);
      }
      catch (SignatureException exception)
      {
        throw new Exception(Resources.InvalidSslCertificateSignature, exception);
      }
      catch (CertificateException exception)
      {
        throw new Exception(Resources.EncodingError, exception);
      }
      catch (Exception exception)
      {
        throw new Exception(Resources.InvalidSslCertificateSignatureGeneral, exception);
      }
    }

    /// <summary>
    /// Verifies that no SSL policy errors regarding the identitfy of the host were raised.
    /// </summary>
    /// <param name="sslPolicyErrors">A <see cref="SslPolicyErrors"/> instance set with the raised SSL errors.</param>
    private static void VerifyIdentity(SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
        throw new MySqlException(Resources.SslConnectionError, new Exception(Resources.SslCertificateHostNameMismatch));
    }

    /// <summary>
    /// Verifies that the issuer matches the CA by comparing the CA certificate issuer and the server certificate issuer.
    /// </summary>
    /// <param name="CACertificate">The CA certificate.</param>
    /// <param name="serverCertificate">The server certificate.</param>
    private static void VerifyIssuer(Org.BouncyCastle.X509.X509Certificate CACertificate, System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate)
    {
      var certificate = new System.Security.Cryptography.X509Certificates.X509Certificate(CACertificate.GetEncoded());
      if (certificate.Issuer != serverCertificate.Issuer)
        throw new MySqlException(Resources.SslConnectionError, new Exception(Resources.SslCertificateCAMismatch));
    }

    private static void VerifyKeyCorrespondsToCertificateKey(Org.BouncyCastle.X509.X509Certificate certificate, AsymmetricCipherKeyPair key)
    {
      var certificateKey = certificate.GetPublicKey().ToString();
      if (!string.IsNullOrEmpty(certificateKey) && certificateKey != key.Public.ToString())
        throw new InvalidKeyException();
    }

    /// Validates that the certificate provided is a CA certificate.
    /// </summary>
    /// <param name="certificate">The certificate to validate.</param>
    /// <param name="certificationPathLength">The allowed certification path length.</param>
    /// <returns><c>null</c> if the certificate info does not allow to determine the CA status;
    /// otherwise, a boolean value indicating the CA status.</null></returns>
    private static bool? IsCA(Org.BouncyCastle.X509.X509Certificate certificate, out int certificationPathLength)
    {
      // If certificate version equal to 3 then the isCA property can be retrieved.
      if (certificate.Version == 3)
      {
        // A value of -1 indicates certificate is not a CA.
        // A value of Integer.MAX_VALUE indicates there is no limit on the allowed length of the certification path.
        certificationPathLength = certificate.GetBasicConstraints();
        return certificationPathLength != -1;
      }

      certificationPathLength = -1;
      return null;
    }

    #endregion
  }
}
