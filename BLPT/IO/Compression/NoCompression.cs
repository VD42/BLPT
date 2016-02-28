namespace BLPT.IO.Compression
{
    /// <summary>
    ///     Doesn't compress/decompress the data, always return it as-is.
    /// </summary>
    class NoCompression : ICompression
    {
        public byte[] Compress(byte[] Data)
        {
            return Data;
        }

        public byte[] Decompress(byte[] Data, uint DecompressedLength)
        {
            return Data;
        }
    }
}
