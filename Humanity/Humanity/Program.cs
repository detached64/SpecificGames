using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Humanity
{
    internal class Program
    {
        private class Entry
        {
            public string Name { get; set; }
            public uint Offset { get; set; }
            public uint Size { get; set; }
        }

        private static void Main(string[] args)
        {
            FileStream fs = File.OpenRead(args[0]);
            BinaryReader br = new BinaryReader(fs);

            int fileCount = (int)Decrypt(br.ReadUInt32());
            List<Entry> entries = new List<Entry>();
            for (int i = 0; i < fileCount; i++)
            {
                Entry e = new Entry();
                br.BaseStream.Position += 4;
                e.Name = i.ToString("D6");
                br.BaseStream.Position++;
                e.Offset = Decrypt(br.ReadUInt32());
                e.Size = Decrypt(br.ReadUInt32());
                br.BaseStream.Position += 4;
                entries.Add(e);
            }

            string folder = args[0].Replace(".dat", string.Empty);
            Directory.CreateDirectory(folder);
            foreach (Entry entry in entries)
            {
                fs.Position = entry.Offset;
                byte[] data = br.ReadBytes((int)entry.Size);
                switch (Path.GetFileName(args[0]))
                {
                    case "arc.dat":
                        switch (BitConverter.ToUInt32(data, 0))
                        {
                            case 0x474e5089:
                                entry.Name += ".png";
                                break;
                            case 0x5367674f:
                                entry.Name += ".ogg";
                                break;
                            default:
                                break;
                        }
                        break;
                    case "script.dat":
                        data = DecompressBytes(data);
                        entry.Name += ".txt";
                        break;
                }

                File.WriteAllBytes(Path.Combine(folder, entry.Name), data);
                data = null;
            }
        }

        public static byte[] DecompressBytes(byte[] input)
        {
            using (MemoryStream inputStream = new MemoryStream(input))
            {
                using (BinaryReader br = new BinaryReader(inputStream))
                {
                    byte magic1 = br.ReadByte();
                    byte magic2 = br.ReadByte();
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        if (magic1 != 0x78 || (magic2 != 0x01 && magic2 != 0x9c && magic2 != 0xda))
                        {
                            // raw deflate
                            inputStream.Position -= 2;
                        }
                        try
                        {
                            using (DeflateStream decompressed = new DeflateStream(inputStream, CompressionMode.Decompress, true))
                            {
                                decompressed.CopyTo(outputStream);
                            }
                        }
                        catch
                        {
                        }
                        input = null;
                        return outputStream.ToArray();
                    }
                }
            }
        }

        private static uint Decrypt(uint z)
        {
            return Byte3(z) | ((Byte2(z) | ((Byte1(z) | (z << 8)) << 8)) << 8);
        }

        private static byte Byte3(uint x)
        {
            return (byte)(x >> 24);
        }

        private static byte Byte2(uint x)
        {
            return (byte)(x >> 16);
        }

        private static byte Byte1(uint x)
        {
            return (byte)(x >> 8);
        }

        private static byte Byte0(uint x)
        {
            return (byte)x;
        }

    }
}
