// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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


using MySql.Data.MySqlClient;
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
    private DBVersion version;

    public Ssl(MySqlConnectionStringBuilder settings, DBVersion version)
    {
      this.settings = settings;
      this.version = version;
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
        if (!version.isAtLeast(5, 1, 0))
          throw new MySqlException(Resources.FileBasedCertificateNotSupported);

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


    public MySqlStream StartSSL(ref Stream baseStream, Encoding encoding, bool isPluginX = false)
    {
      RemoteCertificateValidationCallback sslValidateCallback =
          new RemoteCertificateValidationCallback(ServerCheckValidation);
      SslStream ss = new SslStream(baseStream, true, sslValidateCallback, null);
      X509CertificateCollection certs = GetClientCertificates();
      SslProtocols sslProtocols = SslProtocols.Tls;

      if (!isPluginX)
      {
        sslProtocols |= SslProtocols.Tls11;
        if (version.isAtLeast(5, 6, 0) && version.IsEnterprise)
          sslProtocols |= SslProtocols.Tls12;
      }
#if !NETCORE10
      ss.AuthenticateAsClient(settings.Server, certs, sslProtocols, false);
#endif
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

      if (settings.SslMode == MySqlSslMode.Preferred ||
          settings.SslMode == MySqlSslMode.Required)
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
