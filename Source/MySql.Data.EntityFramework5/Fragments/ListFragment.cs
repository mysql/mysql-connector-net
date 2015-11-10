// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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

using System.Collections.Generic;
using System.Text;

namespace MySql.Data.Entity
{
    class ListFragment : SqlFragment 
    {
        public ListFragment(string sep)
        {
            Items = new List<SqlFragment>();
            Seperator = sep;
        }

        public List<SqlFragment> Items { get; private set; }
        public string Seperator { get; set; }

        public void Append(string s)
        {
            Items.Add(new SqlFragment(s));
        }

        public void Append(SqlFragment s)
        {
            Items.Add(s);
        }

/*        protected override string InnerText
        {
            get
            {
                string seperator = "";
                StringBuilder sb = new StringBuilder();

                foreach (SqlFragment f in Items)
                {
                    sb.AppendFormat("{0}{1}", seperator, f);
                    seperator = Seperator;
                }
                return sb.ToString();
            }
        }*/
    }
}
