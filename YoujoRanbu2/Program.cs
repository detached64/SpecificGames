using System;
using System.IO;
using System.Text;

namespace YoujoRanbu2
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string file = args[0];
            switch (Path.GetExtension(file))
            {
                case ".MD":
                    ProcessMD(file);
                    break;
                case ".BMP":
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(file) + ".dec.bmp", Decode(File.ReadAllBytes(file)));
                    break;
            }
        }

        private static void ProcessMD(string file)
        {
            FileStream fs = File.OpenRead(file);
            BinaryReader br = new BinaryReader(fs);
            if (Encoding.ASCII.GetString(br.ReadBytes(16)).TrimEnd('\0') != "MDFILE Ver2.0")
                return;
            int fileCount = br.ReadInt32();
            string path = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
            Directory.CreateDirectory(path);
            for (int i = 0; i < fileCount; i++)
            {
                uint nameOffset = br.ReadUInt32();
                uint dataOffset = br.ReadUInt32();
                uint nameLength = dataOffset - nameOffset;
                uint size = br.ReadUInt32();
                long pos = fs.Position;
                fs.Position = nameOffset;
                string name = Encoding.ASCII.GetString(br.ReadBytes((int)nameLength)).TrimEnd('\0');
                fs.Position = dataOffset;
                byte[] data = br.ReadBytes((int)size);
                data = Decode(data);
                File.WriteAllBytes(Path.Combine(path, name), data);
                fs.Position = pos;
            }
        }

        private static byte[] Decode(byte[] data)
        {
            int size = data.Length;
            if (BitConverter.ToUInt32(data, 0) == 0x706d6f63)
            {
                int unpacked_size = BitConverter.ToInt32(data, 16);
                byte[] dec = new byte[size - 20];
                Buffer.BlockCopy(data, 20, dec, 0, (int)(size - 20));
                data = Decompress(dec, unpacked_size);
            }
            return data;
        }

        private static byte[] Decompress(byte[] data, int unpackedSize)
        {
            byte[] unpacked = new byte[unpackedSize];

            byte[] ringBuffer = new byte[4096];

            int ringIndex = 4078;

            int inIdx = 0;
            int outIdx = 0;

            int flags = 0;

            while (outIdx < unpackedSize)
            {
                flags >>= 1;

                if ((flags & 0x100) == 0)
                {
                    if (inIdx >= data.Length)
                        break;

                    flags = data[inIdx++] | 0xFF00;
                }

                if ((flags & 1) != 0)
                {
                    if (inIdx >= data.Length)
                        break;

                    byte v7 = data[inIdx++];

                    unpacked[outIdx++] = v7;

                    ringBuffer[ringIndex] = v7;

                    ringIndex = (ringIndex + 1) & 0xFFF;
                }
                else
                {
                    if (inIdx + 1 >= data.Length)
                        break;

                    int b1 = data[inIdx++];
                    int b2 = data[inIdx++];

                    int offset = ((b2 & 0xF0) << 4) | b1;

                    int length = (b2 & 0x0F) + 3;

                    for (int i = 0; i < length; i++)
                    {
                        if (outIdx >= unpackedSize)
                            break;

                        byte v10 = ringBuffer[(offset + i) & 0xFFF];

                        unpacked[outIdx++] = v10;

                        ringBuffer[ringIndex] = v10;
                        ringIndex = (ringIndex + 1) & 0xFFF;
                    }
                }
            }

            return unpacked;
        }
    }
}
