// Copyright (c) 2012, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Text;

namespace MySql.Data.MySqlClient.Authentication
{
    public class ScramMethod
    {
        private string userName;
        private string password;
        private string cnonce;

        public ScramMethod(MySqlConnectionStringBuilder settings)
        {
            this.userName = settings.UserID;
            this.password = settings.Password;
        }

        public byte[] NextCycle(byte[] input)
        {
            if (input == null || input.Length == 0)
                return ClientInitial();
            ProcessServerResponse(input);
            return null;
        }

        void ProcessServerResponse(byte[] data)
        {
            string response = Encoding.UTF8.GetString(data, 0, data.Length);
            var dict = new Dictionary<char, string>();
            string[] parts = response.Split(',');
            foreach (string part in parts)
            {
                if (part[1] == '=') dict.Add(part[0], part.Substring(2));
            }
            if (!dict.ContainsKey('s')) throw new MySqlException("Unable to authenticate");
            if (!dict.ContainsKey('r')) throw new MySqlException("Unable to authenticate");
            if (!dict.ContainsKey('i')) throw new MySqlException("Unable to authenticate");
            if (!dict['r'].StartsWith(cnonce, StringComparison.Ordinal)) throw new MySqlException("Unable to authenticate");

            int count;
            if (!int.TryParse(dict['i'], out count)) throw new MySqlException("Unable to authenticate");

            var password = Encoding.UTF8.GetBytes(password);
            salted = Hi(password, Convert.FromBase64String(dict['s']), count);
            Array.Clear(password, 0, password.Length);

            var withoutProof = "c=" + Convert.ToBase64String(Encoding.ASCII.GetBytes("n,,")) + ",r=" + nonce;
            auth = Encoding.UTF8.GetBytes(client + "," + server + "," + withoutProof);

            var key = HMAC(salted, Encoding.ASCII.GetBytes("Client Key"));
            signature = HMAC(Hash(key), auth);
            Xor(key, signature);

            response = Encoding.UTF8.GetBytes(withoutProof + ",p=" + Convert.ToBase64String(key));

        }

        internal byte[] ClientInitial()
        {
            cnonce = cnonce ?? GetRandomBytes(32);
            var response = $"n,a={userName},n={userName},r={cnonce}";
            return Encoding.UTF8.GetBytes(response);
        }

        internal static string GetRandomBytes(int n)
        {
            var bytes = new byte[n];

            Random rnd = new Random();
            rnd.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}


