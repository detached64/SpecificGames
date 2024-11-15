using System;
using System.Collections.Generic;
using System.IO;

namespace Kochuuten
{
    internal class Program
    {
        private class Entry
        {
            public string Path { get; set; }
            public uint Offset { get; set; }
            public uint Size { get; set; }
            public uint UnpackedSize { get; set; }
            public uint Type { get; set; }
        }

        private static string ArcName => "arc.___";
        private static string ScrName => "nscript.___";

        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Drag and drop archive to exe.");
                Console.ReadLine();
                return;
            }
            string name = Path.GetFileName(args[0]);

            if (name.Equals(ArcName, StringComparison.OrdinalIgnoreCase))
            {
                FileStream fs = File.OpenRead(args[0]);
                BinaryReader br = new BinaryReader(fs);
                uint count = ReadUInt16(br);
                uint dataOffset = ReadUInt32(br);
                string folder = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]));
                List<Entry> entries = new List<Entry>();
                for (int i = 0; i < count; i++)
                {
                    Entry e = new Entry();
                    e.Path = Path.Combine(folder, ReadString(br));
                    e.Type = bytes[br.ReadByte()];
                    e.Offset = ReadUInt32(br);
                    e.Size = ReadUInt32(br);
                    e.UnpackedSize = ReadUInt32(br);
                    entries.Add(e);
                }
                Directory.CreateDirectory(folder);
                uint baseOffset = (uint)fs.Position;
                foreach (Entry e in entries)
                {
                    fs.Position = e.Offset + baseOffset;
                    byte[] buf = br.ReadBytes((int)e.Size);
                    string parent = Path.GetDirectoryName(e.Path);
                    Directory.CreateDirectory(parent);

                    switch (e.Type)
                    {
                        case 0:
                            for (int i = 0; i < buf.Length; i++)
                            {
                                buf[i] = bytes[buf[i]];
                            }
                            File.WriteAllBytes(e.Path, buf);
                            buf = null;
                            break;
                        case 1:
                            for (int i = 0; i < buf.Length; i++)
                            {
                                buf[i] = bytes[buf[i]];
                            }
                            string ext = Path.GetExtension(e.Path);
                            using (MemoryStream ms = new MemoryStream(buf))
                            {
                                using (Decoder decoder = new Decoder(ms, e.UnpackedSize))
                                {
                                    using (Stream output = decoder.SpbDecodedStream())
                                    {
                                        byte[] outputBuf = new byte[e.UnpackedSize];
                                        output.Read(outputBuf, 0, (int)e.UnpackedSize);
                                        File.WriteAllBytes(e.Path, outputBuf);
                                        outputBuf = null;
                                    }
                                }
                            }
                            break;
                        default:
                            Console.WriteLine("Unknown type: " + e.Type);
                            return;
                    }
                    Console.WriteLine(e.Type);
                }

            }
            else if (name.Equals(ScrName, StringComparison.OrdinalIgnoreCase))
            {
                byte[] buffer = File.ReadAllBytes(args[0]);
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)(bytes[buffer[i]] ^ 0x84);
                }
                string folder = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]));
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, name), buffer);
                buffer = null;
            }
            else
            {
                Console.WriteLine("Unknown file: " + name);
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Finished.");
            Console.ReadLine();

        }

        public static ushort ReadUInt16(BinaryReader br)
        {
            byte[] buf = new byte[2];
            buf[1] = bytes[br.ReadByte()];
            buf[0] = bytes[br.ReadByte()];
            return BitConverter.ToUInt16(buf, 0);     // big endian
        }

        public static uint ReadUInt32(BinaryReader br)
        {
            byte[] buf = new byte[4];
            buf[3] = bytes[br.ReadByte()];
            buf[2] = bytes[br.ReadByte()];
            buf[1] = bytes[br.ReadByte()];
            buf[0] = bytes[br.ReadByte()];
            return BitConverter.ToUInt32(buf, 0);     // big endian
        }

        public static string ReadString(BinaryReader br)
        {
            string path = string.Empty;
            while (true)
            {
                byte b = bytes[br.ReadByte()];
                if (b == 0)
                {
                    return path;
                }
                else
                {
                    path += (char)b;
                }
            }
        }

        public static byte[] bytes =
        {
            0xE4, 0x12, 0x7D, 0x78, 0xD4, 0x8F, 0x79, 0x60, 0x4D, 0x54, 0x40, 0xF8, 0xFB, 0x9B, 0x3C, 0x02, 0x08,
            0x5F, 0x32, 0x7B, 0xDD, 0x43, 0xFC, 0x36, 0xC3, 0x20, 0x21, 0xE6, 0xA4, 0xBE, 0xA7, 0x90, 0xF0, 0xAB,
            0x9D, 0x7A, 0x55, 0xA8, 0x3B, 0x9C, 0x7E, 0xD0, 0x0B, 0xD1, 0xE7, 0xBD, 0xBB, 0xF5, 0xE9, 0xB1, 0x9F,
            0x7F, 0x46, 0x8D, 0x28, 0x9E, 0xB3, 0xA3, 0x13, 0x61, 0xC5, 0x1A, 0xAF, 0x17, 0xF6, 0x49, 0xDC, 0x66,
            0x0C, 0xBF, 0x3A, 0xC1, 0x70, 0x76, 0x56, 0x35, 0xBC, 0x47, 0x2B, 0x03, 0x1D, 0x4B, 0x4E, 0xE8, 0x38,
            0x11, 0xDB, 0xDF, 0xAE, 0x62, 0x39, 0x07, 0x18, 0xA9, 0x01, 0xDA, 0x5D, 0x8C, 0x84, 0x7C, 0x59, 0xAA,
            0x5A, 0xD6, 0xB2, 0x45, 0xD2, 0xA5, 0x05, 0xF9, 0x4F, 0x5B, 0xD3, 0x51, 0x4C, 0x1F, 0x96, 0x73, 0xFF,
            0xD8, 0x3E, 0xE3, 0x88, 0x10, 0xFA, 0xCD, 0xD7, 0xA0, 0xA1, 0x41, 0x65, 0xEF, 0xB0, 0xE0, 0x63, 0x71,
            0x80, 0x25, 0x42, 0x23, 0xEE, 0xFD, 0xD5, 0x81, 0xD9, 0xEA, 0xCF, 0x0A, 0x50, 0x14, 0x22, 0x04, 0xCC,
            0x2D, 0x85, 0x69, 0x31, 0x1C, 0x6E, 0x64, 0xC6, 0xEB, 0x2F, 0x57, 0x06, 0xB7, 0x8E, 0x2C, 0xA6, 0x98,
            0xB6, 0xBA, 0xB9, 0x99, 0x83, 0xF3, 0x53, 0x3D, 0x0D, 0xC9, 0x94, 0x2E, 0xCB, 0xE2, 0x8A, 0x44, 0x6C,
            0xCA, 0xE5, 0x72, 0x48, 0x58, 0x19, 0xB8, 0x37, 0xAD, 0x5E, 0x52, 0x27, 0xCE, 0xAC, 0x24, 0xA2, 0x00,
            0xE1, 0x75, 0x6F, 0x16, 0x95, 0x09, 0x93, 0xC8, 0x29, 0x9A, 0x86, 0x92, 0xDE, 0x3F, 0x0E, 0x74, 0x97,
            0xEC, 0x8B, 0x6D, 0xF1, 0xF2, 0xB4, 0x33, 0xC4, 0x67, 0xC0, 0x6A, 0x77, 0x4A, 0x34, 0xFE, 0xF4, 0x91,
            0xC2, 0x87, 0x6B, 0x15, 0x89, 0x1B, 0x82, 0x30, 0xC7, 0xF7, 0x68, 0x1E, 0xB5, 0x26, 0x5C, 0x0F, 0xED,
            0x2A
        };
    }
}
