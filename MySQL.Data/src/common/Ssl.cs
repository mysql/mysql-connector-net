// Copyright (c) 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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


using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MySql.Data.Common
{
  internal class Ssl
  {
    private MySqlConnectionStringBuilder settings;
    private static Dictionary<string, SslProtocols> tlsConnectionRef = new Dictionary<string, SslProtocols>();
    private static Dictionary<string, int> tlsRetry = new Dictionary<string, int>();
    private static SslProtocols[] tlsProtocols = new SslProtocols[] { SslProtocols.Tls12, SslProtocols.Tls11 };
    private static Object thisLock = new Object();

    public Ssl(MySqlConnectionStringBuilder settings)
    {
      this.settings = settings;
    }

    public Ssl(string server, MySqlSslMode sslMode, string certificateFile, MySqlCertificateStoreLocation certificateStoreLocation,
        string certificatePassword, string certificateThumbprint)
    {
      this.settings = new MySqlConnectionStringBuilder()
      {
        Server = server,
        SslMode = sslMode,
        CertificateFile = certificateFile,
        CertificateStoreLocation = certificateStoreLocation,
        CertificatePassword = certificatePassword,
        CertificateThumbprint = certificateThumbprint
      };
    }

    /// <summary>
    /// Retrieve client SSL certificates. Dependent on connection string
    /// settings we use either file or store based certificates.
    /// </summary>
    private X509CertificateCollection GetClientCertificates()
    {
      X509CertificateCollection certs = new X509CertificateCollection();

      // Check for file-based certificate
      if (settings.CertificateFile != null)
      {
        X509Certificate2 clientCert = new X509Certificate2(settings.CertificateFile,
            settings.CertificatePassword);
        certs.Add(clientCert);
        return certs;
      }

      if (settings.CertificateStoreLocation == MySqlCertificateStoreLocation.None)
        return certs;

      StoreLocation location =
          (settings.CertificateStoreLocation == MySqlCertificateStoreLocation.CurrentUser) ?
          StoreLocation.CurrentUser : StoreLocation.LocalMachine;

      // Check for store-based certificate
      X509Store store = new X509Store(StoreName.My, location);
      store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);


      if (settings.CertificateThumbprint == null)
      {
        // Return all certificates from the store.
        certs.AddRange(store.Certificates);
        return certs;
      }

      // Find certificate with given thumbprint
      certs.AddRange(store.Certificates.Find(X509FindType.FindByThumbprint,
                settings.CertificateThumbprint, true));

      if (certs.Count == 0)
      {
        throw new MySqlException("Certificate with Thumbprint " +
           settings.CertificateThumbprint + " not found");
      }
      return certs;
    }


    public MySqlStream StartSSL(ref Stream baseStream, Encoding encoding, string connectionString)
    {
      RemoteCertificateValidationCallback sslValidateCallback =
          new RemoteCertificateValidationCallback(ServerCheckValidation);
      SslStream ss = new SslStream(baseStream, false, sslValidateCallback, null);
      X509CertificateCollection certs = GetClientCertificates();

      string connectionId = connectionString.GetHashCode().ToString();
      SslProtocols tlsProtocol = SslProtocols.Tls;

      lock (thisLock)
      {
        if (tlsConnectionRef.ContainsKey(connectionId))
        {
          tlsProtocol = tlsConnectionRef[connectionId];
        }
        else
        {
          if (!tlsRetry.ContainsKey(connectionId))
          {
            tlsRetry[connectionId] = 0;
          }
          for (int i = tlsRetry[connectionId]; i < tlsProtocols.Length; i++)
          {
            tlsProtocol |= tlsProtocols[i];
          }
        }
        try
        {
          ss.AuthenticateAsClientAsync(settings.Server, certs, tlsProtocol, false).Wait();
          tlsConnectionRef[connectionId] = tlsProtocol;
          tlsRetry.Remove(connectionId);
        }
        catch(AggregateException ex)
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
      }

      baseStream = ss;
      MySqlStream stream = new MySqlStream(ss, encoding, false);
      stream.SequenceByte = 2;

      return stream;
    }

    private bool ServerCheckValidation(object sender, X509Certificate certificate,
                                              X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.None)
        return true;

      if (settings.SslMode == MySqlSslMode.Required ||
          settings.SslMode == MySqlSslMode.Preferred)
      {
        //Tolerate all certificate errors.
        return true;
      }

      if (settings.SslMode == MySqlSslMode.VerifyCA &&
          sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
      {
        // Tolerate name mismatch in certificate, if full validation is not requested.
        return true;
      }

      return false;
    }
  }
}
