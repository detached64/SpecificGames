using System;
using System.IO;
using System.Text;

namespace KoukanJouken
{
    internal static class Program
    {
        private static bool IsEncrypted = false;
        private static bool IsScr = false;
        private static bool IsWav = false;

        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Drag and drop archive to exe.");
                Console.ReadLine();
                return;
            }
            switch (Path.GetFileName(args[0]))
            {
                case "cg.dat":
                    IsEncrypted = true;
                    break;
                case "scr.dat":
                    IsEncrypted = true;
                    IsScr = true;
                    break;
                case "wav.dat":
                    IsWav = true;
                    break;
            }
            string dir = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]));
            Directory.CreateDirectory(dir);
            FileStream fs = File.OpenRead(args[0]);
            BinaryReader br = new BinaryReader(fs);
            int fileCount = br.ReadInt32();
            fs.Position = 16;
            for (int i = 0; i < fileCount; i++)
            {
                long offset = Decrypt(br.ReadUInt32());
                long size = Decrypt(br.ReadUInt32());
                string name = Encoding.ASCII.GetString(Decrypt(br.ReadBytes(24))).TrimEnd('\0');
                string path = Path.Combine(dir, name);
                if (Path.GetExtension(path) == ".wav")
                {
                    path = Path.ChangeExtension(path, ".ogg");
                }
                long cur_pos = fs.Position;
                fs.Position = offset;
                byte[] data = br.ReadBytes((int)size);
                File.WriteAllBytes(path, DecryptData(data));
                fs.Position = cur_pos;
            }
            fs.Dispose();
            br.Dispose();
        }

        private static uint Decrypt(uint value)
        {
            if (IsEncrypted)
            {
                return value ^ 0x81818181;
            }
            return value;
        }

        private static byte[] Decrypt(byte[] data)
        {
            if (IsEncrypted)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= 0x81;
                }
            }
            return data;
        }

        private static byte[] DecryptData(byte[] data)
        {
            if (IsScr)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= 0x81;
                }
            }
            else if (IsWav)
            {
                byte[] bytes = new byte[data.Length - 66];
                Buffer.BlockCopy(data, 66, bytes, 0, bytes.Length);
                return bytes;
            }
            else
            {
                int length = Math.Min(data.Length, 32);
                if (IsEncrypted)
                {
                    for (int i = 0; i < length; i++)
                    {
                        data[i] ^= 0x81;
                    }
                }
            }
            return data;
        }
    }
}
