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

        private int Adler32(byte[] Data)
        {
            const uint a32mod = 65521;
            uint s1 = 1, s2 = 0;
            foreach (byte b in Data)
            {
                s1 = (s1 + b) % a32mod;
                s2 = (s2 + s1) % a32mod;
            }

            return unchecked((int)((s2 << 16) + s1));
        }

        public byte[] Decompress(byte[] Data, uint DecompressedLength)
        {
            byte[] Headerless = new byte[Data.Length - 2];
            Buffer.BlockCopy(Data, 2, Headerless, 0, Headerless.Length - 4);

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
