// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.EntityFrameworkCore;
using System;
using System.Data;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  public class MySQLSizeableMapping : RelationalTypeMapping
  {

    public MySQLSizeableMapping([NotNull] string storeType,
            [NotNull] Type clrType,
            DbType? dbType,
            bool unicode,
            int? size,
            bool hasNonDefaultUnicode = false,
            bool hasNonDefaultSize = false)
            : base(storeType, clrType, dbType, unicode, GetSizeForString(unicode, size), hasNonDefaultUnicode, hasNonDefaultSize)

    {
    }

    private static int GetSizeForString(bool unicode, int? size)
    {
      int _textMaxLength;
      int _medTextMaxLength;
      int _longTextMaxLength;

      //max lenght for text types considering 3-bytes character sets.      
      //_textMaxLength = ((int)Math.Pow(2, 16) - 1) / 3;
      //_medTextMaxLength = ((int)Math.Pow(2, 24) - 1) / 3;
      //_longTextMaxLength = ((int)Math.Pow(2, 32) - 1) / 3;

      _medTextMaxLength = 255; // 65535 / 4;  // ((int)Math.Pow(2, 24) - 1) / 3;
      _longTextMaxLength = 255; //65535 / 3; //((int)Math.Pow(2, 32) - 1) / 3;
     _textMaxLength = 255; //65535;  // ((int)Math.Pow(2, 16) - 1) / 3;

      if (unicode)
      {
        //_textMaxLength = ((int)Math.Pow(2, 16) - 1) / 4;
        //_medTextMaxLength = ((int)Math.Pow(2, 24) - 1) / 4;
        //_longTextMaxLength = ((int)Math.Pow(2, 32) - 1) / 4;

        _medTextMaxLength = 255; //65535 / 4;  // ((int)Math.Pow(2, 24) - 1) / 3;
        _longTextMaxLength = 255; // 65535 / 3; //((int)Math.Pow(2, 32) - 1) / 3;
        _textMaxLength = 255; //65535;  // ((int)Math.Pow(2, 16) - 1) / 3;
      }            

      if (size.HasValue)
      {
        if (size > _medTextMaxLength)
          return _longTextMaxLength;
        else
          return size.Value < _textMaxLength ? size.Value : _medTextMaxLength;
      }

      return _textMaxLength;
    }
  }
}
