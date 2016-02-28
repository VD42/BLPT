namespace BLPT.IO.Compression
{
    /// <summary>
    ///     (De)compression algorithm interface.
    /// </summary>
    interface ICompression
    {
        /// <summary>
        ///     Compresses an Array of bytes.
        /// </summary>
        /// <param name="Data">The data to compress</param>
        /// <returns>The compressed data</returns>
        byte[] Compress(byte[] Data);

        /// <summary>
        ///     Decompresses an Array of bytes.
        /// </summary>
        /// <param name="Data">The data to decompress</param>
        /// <returns>The decompressed data</returns>
        byte[] Decompress(byte[] Data, uint DecompressedLength);
    }
}
