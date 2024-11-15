using System;
using System.Diagnostics;
using System.IO;

namespace Kochuuten
{
    internal class Decoder : MsbBitStream
    {
        public byte[] Output { get; private set; }

        public Decoder(Stream input, uint unpacked_size) : base(input, true)
        {
            Output = new byte[unpacked_size];
        }

        public Stream SpbDecodedStream()
        {
            DecodeSPB();
            return new MemoryStream(Output);
        }

        private uint DecodeSPB()
        {
            uint width = (uint)Input.ReadByte() << 8;
            width |= (uint)Input.ReadByte();
            uint height = (uint)Input.ReadByte() << 8;
            height |= (uint)Input.ReadByte();

            uint width_pad = (4 - width * 3 % 4) % 4;
            int stride = (int)(width * 3 + width_pad);
            uint total_size = (uint)stride * height + 54;

            if ((uint)Output.Length < total_size)
            {
                Output = new byte[total_size];
            }

            /* ---------------------------------------- */
            /* Write header */
            Output[0] = (byte)'B';
            Output[1] = (byte)'M';
            Utils.LittleEndian.Pack(total_size, Output, 2);
            Output[10] = 54; // offset to the body
            Output[14] = 40; // header size
            Utils.LittleEndian.Pack(width, Output, 18);
            Utils.LittleEndian.Pack(height, Output, 22);
            Output[26] = 1; // the number of the plane
            Output[28] = 24; // bpp

            byte[] decomp_buffer = new byte[width * height * 4];

            for (int i = 0; i < 3; i++)
            {
                uint count = 0;
                int c = GetBits(8);
                if (-1 == c)
                {
                    break;
                }

                decomp_buffer[count++] = (byte)c;
                while (count < width * height)
                {
                    int n = GetBits(3);
                    if (0 == n)
                    {
                        decomp_buffer[count++] = (byte)c;
                        decomp_buffer[count++] = (byte)c;
                        decomp_buffer[count++] = (byte)c;
                        decomp_buffer[count++] = (byte)c;
                        continue;
                    }
                    int m;
                    if (7 == n)
                    {
                        m = GetBits(1) + 1;
                    }
                    else
                    {
                        m = n + 2;
                    }

                    for (uint j = 0; j < 4; j++)
                    {
                        if (8 == m)
                        {
                            c = GetBits(8);
                        }
                        else
                        {
                            int k = GetBits(m);
                            if (0 != (k & 1))
                            {
                                c += (k >> 1) + 1;
                            }
                            else
                            {
                                c -= (k >> 1);
                            }
                        }
                        decomp_buffer[count++] = (byte)c;
                    }
                }

                int pbuf = stride * (int)(height - 1) + i + 54; // in m_output
                int psbuf = 0; // in decomp_buffer

                for (uint j = 0; j < height; j++)
                {
                    if (0 != (j & 1))
                    {
                        for (uint k = 0; k < width; k++, pbuf -= 3)
                        {
                            Output[pbuf] = decomp_buffer[psbuf++];
                        }

                        pbuf -= stride - 3;
                    }
                    else
                    {
                        for (uint k = 0; k < width; k++, pbuf += 3)
                        {
                            Output[pbuf] = decomp_buffer[psbuf++];
                        }

                        pbuf -= stride + 3;
                    }
                }
            }
            return total_size;
        }
    }

    public class MsbBitStream : BitStream, IBitStream
    {
        public MsbBitStream(Stream file, bool leave_open = false)
            : base(file, leave_open)
        {
        }

        public int GetBits(int count)
        {
            Debug.Assert(count <= 24, "MsbBitStream does not support sequences longer than 24 bits");
            while (CacheSize < count)
            {
                int b = Input.ReadByte();
                if (-1 == b)
                {
                    return -1;
                }

                m_bits = (m_bits << 8) | b;
                CacheSize += 8;
            }
            int mask = (1 << count) - 1;
            CacheSize -= count;
            return (m_bits >> CacheSize) & mask;
        }

        public int GetNextBit()
        {
            return GetBits(1);
        }
    }

    public class LsbBitStream : BitStream, IBitStream
    {
        public LsbBitStream(Stream file, bool leave_open = false)
            : base(file, leave_open)
        {
        }

        public int GetBits(int count)
        {
            Debug.Assert(count <= 32, "LsbBitStream does not support sequences longer than 32 bits");
            int value;
            if (CacheSize >= count)
            {
                int mask = (1 << count) - 1;
                value = m_bits & mask;
                m_bits = (int)((uint)m_bits >> count);
                CacheSize -= count;
            }
            else
            {
                value = m_bits & ((1 << CacheSize) - 1);
                count -= CacheSize;
                int shift = CacheSize;
                CacheSize = 0;
                while (count >= 8)
                {
                    int b = Input.ReadByte();
                    if (-1 == b)
                    {
                        return -1;
                    }

                    value |= b << shift;
                    shift += 8;
                    count -= 8;
                }
                if (count > 0)
                {
                    int b = Input.ReadByte();
                    if (-1 == b)
                    {
                        return -1;
                    }

                    value |= (b & ((1 << count) - 1)) << shift;
                    m_bits = b >> count;
                    CacheSize = 8 - count;
                }
            }
            return value;
        }

        public int GetNextBit()
        {
            return GetBits(1);
        }
    }

    public class BitStream : IDisposable
    {
        private bool m_should_dispose;

        protected int m_bits = 0;

        public Stream Input { get; }
        public int CacheSize { get; protected set; } = 0;

        protected BitStream(Stream file, bool leave_open)
        {
            Input = file;
            m_should_dispose = !leave_open;
        }

        public void Reset()
        {
            CacheSize = 0;
        }

        #region IDisposable Members
        private bool m_disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing && m_should_dispose && null != Input)
                {
                    Input.Dispose();
                }

                m_disposed = true;
            }
        }
        #endregion
    }

    public interface IBitStream
    {
        int GetBits(int count);
        int GetNextBit();
        void Reset();
    }
}
