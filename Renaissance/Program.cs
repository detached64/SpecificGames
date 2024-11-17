using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Renaissance
{
    internal class Program
    {
        private class Entry
        {
            public string Path { get; set; }
            public uint Offset { get; set; }
            public uint Size { get; set; }
        }

        private static string EbpName => "ebp.fga";
        private static string SrpName => "srp.fga";

        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Drag and drop archive to exe.");
                Console.ReadLine();
                return;
            }
            FileStream fs = File.OpenRead(args[0]);
            BinaryReader br = new BinaryReader(fs);
            List<Entry> entries = new List<Entry>();
            byte[] bytes = new byte[12];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 0xff;
            }
            string folder = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]));
Label:
            byte[] nameBuf;
            while (!(nameBuf = br.ReadBytes(12)).SequenceEqual(bytes))
            {
                Entry e = new Entry();
                e.Path = Path.Combine(folder, Encoding.GetEncoding(932).GetString(nameBuf).TrimEnd('\0'));
                e.Offset = br.ReadUInt32();
                e.Size = br.ReadUInt32();
                fs.Position += 4;
                entries.Add(e);
            }
            uint offset = br.ReadUInt32();
            if (offset < fs.Length && offset > 0)
            {
                fs.Position = offset;
                goto Label;
            }

            Directory.CreateDirectory(folder);

            if (Path.GetFileName(args[0]).Equals(EbpName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (Entry e in entries)
                {
                    if (string.IsNullOrEmpty(e.Path))
                    {
                        continue;
                    }
                    fs.Position = e.Offset;
                    byte[] buffer = br.ReadBytes((int)e.Size);
                    if (e.Size % 4 != 0)
                    {
                        Console.WriteLine($"Invalid file {Path.GetFileName(e.Path)}. Skip.");
                        buffer = null;
                        continue;
                    }
                    byte[] bytes1 = new byte[3];
                    int count = 0;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        for (int i = 0; i < e.Size; i += 4)
                        {
                            bytes1[0] = buffer[i];
                            bytes1[1] = buffer[i + 1];
                            bytes1[2] = buffer[i + 2];
                            count = buffer[i + 3];
                            for (int j = 0; j < count; j++)
                            {
                                ms.WriteByte(bytes1[0]);
                                ms.WriteByte(bytes1[1]);
                                ms.WriteByte(bytes1[2]);
                            }
                        }
                        buffer = ms.ToArray();
                        File.WriteAllBytes(e.Path + ".bmp", buffer);
                        buffer = null;
                    }
                }
            }
            else if (Path.GetFileName(args[0]).Equals(SrpName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Not implemented yet.");
            }
            else
            {
                Console.WriteLine("Unknown archive.");
            }
            Console.ReadLine();
        }
    }
}
