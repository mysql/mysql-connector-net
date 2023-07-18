// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
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
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for CharSetMap.
  /// </summary>
  internal class CharSetMap
  {
    private static Dictionary<string, string> _defaultCollations;
    private static Dictionary<string, int> _maxLengths;
    private static Dictionary<string, CharacterSet> _mapping;
    private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

    // we use a static constructor here since we only want to init
    // the mapping once
    static CharSetMap()
    {
      InitializeMapping();
    }

    public static CharacterSet GetCharacterSet(string charSetName)
    {
      if (charSetName == null)
        throw new ArgumentNullException("CharSetName is null");
      CharacterSet cs = null;
      if (_mapping.TryGetValue(charSetName, out var charset))
        cs = charset;

      if (cs == null)
        throw new NotSupportedException("Character set '" + charSetName + "' is not supported by .Net Framework.");
      return cs;
    }

    /// <summary>
    /// Returns the text encoding for a given MySQL character set name
    /// </summary>
    /// <param name="charSetName">Name of the character set to get the encoding for</param>
    /// <returns>Encoding object for the given character set name</returns>
    public static Encoding GetEncoding(string charSetName)
    {
      try
      {
        CharacterSet cs = GetCharacterSet(charSetName);

        return Encoding.GetEncoding(cs.name);
      }
      catch (ArgumentException)
      {
        return Encoding.GetEncoding("utf-8");
      }
      catch (NotSupportedException)
      {
        return Encoding.GetEncoding("utf-8");
      }
    }

    /// <summary>
    /// Initializes the mapping.
    /// </summary>
    private static void InitializeMapping()
    {
      LoadCharsetMap();
    }

    private static void LoadCharsetMap()
    {
      _mapping = new Dictionary<string, CharacterSet>();

      _mapping.Add("latin1", new CharacterSet("windows-1252", 1));
      _mapping.Add("big5", new CharacterSet("big5", 2));
      _mapping.Add("dec8", _mapping["latin1"]);
      _mapping.Add("cp850", new CharacterSet("ibm850", 1));
      _mapping.Add("hp8", _mapping["latin1"]);
      _mapping.Add("koi8r", new CharacterSet("koi8-u", 1));
      _mapping.Add("latin2", new CharacterSet("latin2", 1));
      _mapping.Add("swe7", _mapping["latin1"]);
      _mapping.Add("ujis", new CharacterSet("EUC-JP", 3));
      _mapping.Add("eucjpms", _mapping["ujis"]);
      _mapping.Add("sjis", new CharacterSet("sjis", 2));
      _mapping.Add("cp932", _mapping["sjis"]);
      _mapping.Add("hebrew", new CharacterSet("hebrew", 1));
      _mapping.Add("tis620", new CharacterSet("windows-874", 1));
      _mapping.Add("euckr", new CharacterSet("euc-kr", 2));
      _mapping.Add("euc_kr", _mapping["euckr"]);
      _mapping.Add("koi8u", new CharacterSet("koi8-u", 1));
      _mapping.Add("koi8_ru", _mapping["koi8u"]);
      _mapping.Add("gb2312", new CharacterSet("gb2312", 2));
      _mapping.Add("gbk", _mapping["gb2312"]);
      _mapping.Add("greek", new CharacterSet("greek", 1));
      _mapping.Add("cp1250", new CharacterSet("windows-1250", 1));
      _mapping.Add("win1250", _mapping["cp1250"]);
      _mapping.Add("latin5", new CharacterSet("latin5", 1));
      _mapping.Add("armscii8", _mapping["latin1"]);
      _mapping.Add("utf8", new CharacterSet("utf-8", 3));
      _mapping.Add("ucs2", new CharacterSet("UTF-16BE", 2));
      _mapping.Add("cp866", new CharacterSet("cp866", 1));
      _mapping.Add("keybcs2", _mapping["latin1"]);
      _mapping.Add("macce", new CharacterSet("x-mac-ce", 1));
      _mapping.Add("macroman", new CharacterSet("x-mac-romanian", 1));
      _mapping.Add("cp852", new CharacterSet("ibm852", 2));
      _mapping.Add("latin7", new CharacterSet("iso-8859-7", 1));
      _mapping.Add("cp1251", new CharacterSet("windows-1251", 1));
      _mapping.Add("win1251ukr", _mapping["cp1251"]);
      _mapping.Add("cp1251csas", _mapping["cp1251"]);
      _mapping.Add("cp1251cias", _mapping["cp1251"]);
      _mapping.Add("win1251", _mapping["cp1251"]);
      _mapping.Add("cp1256", new CharacterSet("cp1256", 1));
      _mapping.Add("cp1257", new CharacterSet("windows-1257", 1));
      _mapping.Add("ascii", new CharacterSet("us-ascii", 1));
      _mapping.Add("usa7", _mapping["ascii"]);
      _mapping.Add("binary", _mapping["ascii"]);
      _mapping.Add("latin3", new CharacterSet("latin3", 1));
      _mapping.Add("latin4", new CharacterSet("latin4", 1));
      _mapping.Add("latin1_de", new CharacterSet("iso-8859-1", 1));
      _mapping.Add("german1", new CharacterSet("iso-8859-1", 1));
      _mapping.Add("danish", new CharacterSet("iso-8859-1", 1));
      _mapping.Add("czech", new CharacterSet("iso-8859-2", 1));
      _mapping.Add("hungarian", new CharacterSet("iso-8859-2", 1));
      _mapping.Add("croat", new CharacterSet("iso-8859-2", 1));
      _mapping.Add("latvian", new CharacterSet("iso-8859-13", 1));
      _mapping.Add("latvian1", new CharacterSet("iso-8859-13", 1));
      _mapping.Add("estonia", new CharacterSet("iso-8859-13", 1));
      _mapping.Add("dos", new CharacterSet("ibm437", 1));
      _mapping.Add("utf8mb3", _mapping["utf8"]);
      _mapping.Add("utf8mb4", new CharacterSet("utf-8", 4));
      _mapping.Add("utf16", new CharacterSet("utf-16BE", 2));
      _mapping.Add("utf16le", new CharacterSet("utf-16", 2));
      _mapping.Add("utf32", new CharacterSet("utf-32BE", 4));
      _mapping.Add("gb18030", new CharacterSet("gb18030", 4));
    }

    internal static async Task InitCollectionsAsync(MySqlConnection connection, bool execAsync, CancellationToken cancellationToken = default)
    {
      _defaultCollations = new Dictionary<string, string>();
      _maxLengths = new Dictionary<string, int>();

      MySqlCommand cmd = new MySqlCommand("SHOW CHARSET", connection);
      using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, execAsync, cancellationToken).ConfigureAwait(false))
      {
        while (await reader.ReadAsync(execAsync, cancellationToken).ConfigureAwait(false))
        {
          _defaultCollations.Add(reader.GetString(0), reader.GetString(2));
          _maxLengths.Add(reader.GetString(0), Convert.ToInt32(reader.GetValue(3)));
        }
      }
    }

    internal static async Task<string> GetDefaultCollationAsync(string charset, MySqlConnection connection, bool execAsync, CancellationToken cancellationToken = default)
    {
      if (execAsync)
      {
        await _semaphoreSlim.WaitAsync(cancellationToken);
      }
      else
      {
        _semaphoreSlim.Wait(cancellationToken);
      }

      try
      {
        if (_defaultCollations == null)
          await InitCollectionsAsync(connection, execAsync, cancellationToken).ConfigureAwait(false);
      }
      finally
      {
        _semaphoreSlim.Release();
      }
        
      return !_defaultCollations.TryGetValue(charset, out string collation) ? null : collation;
    }

    internal static async Task<int> GetMaxLengthAsync(string charset, MySqlConnection connection, bool execAsync, CancellationToken cancellationToken = default)
    {
      if (execAsync)
      {
        await _semaphoreSlim.WaitAsync(cancellationToken);
      }
      else
      {
        _semaphoreSlim.Wait(cancellationToken);
      }

      try
      {
        if (_maxLengths == null)
          await InitCollectionsAsync(connection, execAsync, cancellationToken).ConfigureAwait(false);
      }
      finally
      {
        _semaphoreSlim.Release(); 
      }

      return !_maxLengths.TryGetValue(charset, out int maxLength) ? 1 : maxLength;
    }
  }

  /// <summary>
  /// Represents a character set object.
  /// </summary>
  public class CharacterSet
  {
    public string name;
    public int byteCount;

    public CharacterSet(string name, int byteCount)
    {
      this.name = name;
      this.byteCount = byteCount;
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((name != null ? name.GetHashCode() : 0) * 397) ^ byteCount;
      }
    }
  }
}