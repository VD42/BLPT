using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

/*
 * gdkchan's note for anyone working on the project:
 * Please rewrite this to use either a open source lib or implement the M$ LZX compression in C#. TY ;*
 */

namespace BLPT.IO.Compression
{
    /// <summary>
    ///     Microsoft LZX (De)compressor.
    /// </summary>
    class LZX : ICompression
    {
        public byte[] Compress(byte[] Data)
        {
            string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string XboxDecompress = Path.Combine(AppPath, "xbcompress.exe");

            string InputFile = Path.GetTempFileName();
            string OutputFile = Path.GetTempFileName();

            File.WriteAllBytes(InputFile, Data);
            ProcessStartInfo Info = new ProcessStartInfo();
            Info.FileName = XboxDecompress;
            Info.Arguments = "/Q /Y /N " + InputFile + " " + OutputFile;
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Process Compressor = Process.Start(Info);
            Compressor.WaitForExit();
            Compressor.Close();

            byte[] Compressed = File.ReadAllBytes(OutputFile);
            byte[] Headerless = new byte[Compressed.Length - 0x34];
            Buffer.BlockCopy(Compressed, 0x34, Headerless, 0, Headerless.Length);
            File.Delete(InputFile);
            File.Delete(OutputFile);
            return Headerless;
        }

        public byte[] Decompress(byte[] Data, uint DecompressedLength)
        {
            string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string XboxDecompress = Path.Combine(AppPath, "xbdecompress.exe");

            string InputFile = Path.GetTempFileName();
            string OutputFile = Path.GetTempFileName();

            using (MemoryStream Output = new MemoryStream())
            {
                EndianBinaryWriter Writer = new EndianBinaryWriter(Output, Endian.Big);

                Writer.Write(0xff512ee);
                Writer.Write(0x1030000);
                Writer.Write(0);
                Writer.Write(0);
                Writer.Write(0x20000);
                Writer.Write(0x80000);
                Writer.Write(0);
                Writer.Write(DecompressedLength);
                Writer.Write(0);
                Writer.Write(Data.Length + 4);
                Writer.Write(DecompressedLength);
                Writer.Write(Data.Length);
                Writer.Write(Data.Length);
                Writer.Write(Data);

                File.WriteAllBytes(InputFile, Output.ToArray());
            }

            ProcessStartInfo Info = new ProcessStartInfo();
            Info.FileName = XboxDecompress;
            Info.Arguments = "/Q /Y " + InputFile + " " + OutputFile;
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Process Decompressor = Process.Start(Info);
            Decompressor.WaitForExit();
            Decompressor.Close();

            byte[] Decompressed = File.ReadAllBytes(OutputFile);
            File.Delete(InputFile);
            File.Delete(OutputFile);
            return Decompressed;
        }
    }
}
