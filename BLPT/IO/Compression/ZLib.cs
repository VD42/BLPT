using System;
using System.IO;
using System.IO.Compression;

namespace BLPT.IO.Compression
{
    /// <summary>
    ///     ZLib inflate/deflate.
    /// </summary>
    class ZLib : ICompression
    {
        public byte[] Compress(byte[] Data)
        {
            using (MemoryStream Stream = new MemoryStream())
            {
                DeflateStream Compressor = new DeflateStream(Stream, CompressionLevel.Optimal);
                Compressor.Write(Data, 0, Data.Length);
                Compressor.Close();

                using (MemoryStream NewStream = new MemoryStream())
                {
                    EndianBinaryWriter Writer = new EndianBinaryWriter(NewStream, Endian.Big);
                    Writer.Write((ushort)0x78da);
                    Writer.Write(Stream.ToArray());
                    Writer.Write(Adler32(Data));
                    return NewStream.ToArray();
                }
            }
        }

        private uint Adler32(byte[] Data)
        {
            const int MOD_ADLER = 65521;

            uint a = 1, b = 0;
            for (int Index = 0; Index < Data.Length; Index++)
            {
                a = (a + Data[Index]) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            return (b << 16) | a;
        }

        public byte[] Decompress(byte[] Data, uint DecompressedLength)
        {
            byte[] Headerless = new byte[Data.Length - 6];
            Buffer.BlockCopy(Data, 2, Headerless, 0, Headerless.Length);

            using (MemoryStream Stream = new MemoryStream(Headerless))
            {
                DeflateStream Decompressor = new DeflateStream(Stream, CompressionMode.Decompress);
                byte[] Decompressed = new byte[DecompressedLength];
                Decompressor.Read(Decompressed, 0, Decompressed.Length);
                Decompressor.Close();

                return Decompressed;
            }
        }
    }
}
