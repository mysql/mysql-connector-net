// Copyright (c) 2019, 2021, Oracle and/or its affiliates.
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
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace MySql.Data.Common
{
  internal class MySqlConnectionStringOption
  {
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
      ClassicSetterDelegate setter, ClassicGetterDelegate getter)
    {
      Keyword = StringUtility.ToLowerInvariant(keyword);
      if (synonyms != null)
        Synonyms = StringUtility.ToLowerInvariant(synonyms).Split(',');
      BaseType = baseType;
      Obsolete = obsolete;
      DefaultValue = defaultValue;
      ClassicSetter = setter;
      ClassicGetter = getter;
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
         msb.SetInternalValue(sender.Keyword, Convert.ChangeType(value, sender.BaseType));
       },
        (msb, sender) => msb.values[sender.Keyword]
      )
    {
      IsCustomized = false;
    }

    #region Properties

    public Type BaseType { get; private set; }
    public bool IsCustomized { get; }
    public string[] Synonyms { get; private set; }
    public bool Obsolete { get; private set; }
    public string Keyword { get; private set; }
    public object DefaultValue { get; private set; }
    public SetterDelegate Setter { get; private set; }
    public GetterDelegate Getter { get; private set; }
    public ClassicSetterDelegate ClassicSetter { get; private set; }
    public ClassicGetterDelegate ClassicGetter { get; private set; }
    public XSetterDelegate XSetter { get; private set; }
    public XGetterDelegate XGetter { get; private set; }

    #endregion

    #region Delegates

    public delegate void SetterDelegate(MySqlBaseConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value);

    public delegate object GetterDelegate(MySqlBaseConnectionStringBuilder msb, MySqlConnectionStringOption sender);

    public delegate void ClassicSetterDelegate(MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value);

    public delegate object ClassicGetterDelegate(MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender);

    public delegate void XSetterDelegate(MySqlXConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value);

    public delegate object XGetterDelegate(MySqlXConnectionStringBuilder msb, MySqlConnectionStringOption sender);

    #endregion

    public bool HasKeyword(string key)
    {
      if (Keyword == key) return true;
      if (Synonyms == null) return false;
      return Synonyms.Any(syn => syn == key);
    }

    public void Clean(DbConnectionStringBuilder builder)
    {
      builder.Remove(Keyword);
      if (Synonyms == null) return;
      foreach (var syn in Synonyms)
        builder.Remove(syn);
    }

    public void ValidateValue(ref object value, string keyword = null, bool isXProtocol = false)
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
      if (typeName.StartsWith("UInt64") && UInt64.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out uintVal)) { value = uintVal; return; }

      UInt32 uintVal32;
      if (typeName.StartsWith("UInt32") && UInt32.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out uintVal32)) { value = uintVal32; return; }

      Int64 intVal;
      if (typeName.StartsWith("Int64") && Int64.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out intVal)) { value = intVal; return; }

      Int32 intVal32;
      if (typeName.StartsWith("Int32") && Int32.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out intVal32)) { value = intVal32; return; }

      object objValue;
      Type baseType = BaseType.GetTypeInfo().BaseType;
      if (baseType != null && baseType.Name == "Enum" && ParseEnum(value.ToString(), out objValue))
      {
        value = objValue;
        return;
      }

      if (!string.IsNullOrEmpty(keyword) && isXProtocol)
      {
        switch (keyword)
        {
          case "compression":
            throw new ArgumentException(string.Format(ResourcesX.CompressionInvalidValue, value));
        }
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
          if (typeName != valueType.Name && !uint.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out uint uintVal)) throw new FormatException(ResourcesX.InvalidConnectionTimeoutValue);
          break;
      }
    }

    private bool ParseEnum(string requestedValue, out object value)
    {
      value = null;
      try
      {
        value = Enum.Parse(BaseType, requestedValue, true);
        if (value != null && Enum.IsDefined(BaseType, value.ToString()))
        {
          return true;
        }
        else
        {
          value = null;
          return false;
        }
      }
      catch (ArgumentException)
      {
        return false;
      }
    }
  }
}
