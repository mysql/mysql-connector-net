using System;
using MySql.Data;
using MySql.XDevAPI;

namespace MySql.Protocol
{
  internal abstract class ValueDecoder
  {
    public Column Column { get; set; }

    public uint Flags { get; set; }

    public  abstract void DecodeMetadata();

    public abstract object GetClrValue(byte[] value);
  }
}
