using System;

namespace MySql.Data.Types
{
  [Serializable]
  public partial struct MySqlDateTime : IConvertible
  {
    #region IConvertible Members

    ulong IConvertible.ToUInt64(IFormatProvider provider)
    {
      return 0;
    }

    sbyte IConvertible.ToSByte(IFormatProvider provider)
    {
      // TODO:  Add MySqlDateTime.ToSByte implementation
      return 0;
    }

    double IConvertible.ToDouble(IFormatProvider provider)
    {
      return 0;
    }

    DateTime IConvertible.ToDateTime(IFormatProvider provider)
    {
      return GetDateTime();
    }

    float IConvertible.ToSingle(IFormatProvider provider)
    {
      return 0;
    }

    bool IConvertible.ToBoolean(IFormatProvider provider)
    {
      return false;
    }

    int IConvertible.ToInt32(IFormatProvider provider)
    {
      return 0;
    }

    ushort IConvertible.ToUInt16(IFormatProvider provider)
    {
      return 0;
    }

    short IConvertible.ToInt16(IFormatProvider provider)
    {
      return 0;
    }

    string System.IConvertible.ToString(IFormatProvider provider)
    {
      return null;
    }

    byte IConvertible.ToByte(IFormatProvider provider)
    {
      return 0;
    }

    char IConvertible.ToChar(IFormatProvider provider)
    {
      return '\0';
    }

    long IConvertible.ToInt64(IFormatProvider provider)
    {
      return 0;
    }

    System.TypeCode IConvertible.GetTypeCode()
    {
      return new System.TypeCode();
    }

    decimal IConvertible.ToDecimal(IFormatProvider provider)
    {
      return 0;
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider provider)
    {
      return null;
    }

    uint IConvertible.ToUInt32(IFormatProvider provider)
    {
      return 0;
    }

    #endregion

  }
}
