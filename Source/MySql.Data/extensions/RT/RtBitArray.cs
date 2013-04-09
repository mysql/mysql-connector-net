using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
    /// <summary>
    /// An implementation of BitArray that has the missing CopyTo method in RT.
    /// </summary>
    internal class RtBitArray
    {
        internal RtBitArray(int length)
        {
            buf = new byte[( length + 7 ) >> 3];
            this.length = length;
        }

        internal void CopyTo(byte[] buffer, int targetIdx)
        {
            Array.Copy(buf, 0, buffer, targetIdx, ( length + 7 ) >> 3 );
        }

        internal int Count { get { return length; } }

        private int length;
        private byte[] buf;

        internal bool this[int i]
        {
            get
            {
                return ( buf[i >> 3] & ( byte )(1 << (i & 7)) ) != 0;
            }
            set
            {
                if (value)
                    buf[i >> 3] |= (byte)(1 << (i & 7));
                else
                    buf[i >> 3] &= (byte)~(1 << (i & 7));
            }
        }
    }
}
