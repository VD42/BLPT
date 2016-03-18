using BLPT.IO;
using BLPT.IO.Compression;

using System;
using System.IO;

namespace BLPT.Brutal
{
    /// <summary>
    ///     Functions to extract or insert data from/into a Brutal Legend package.
    /// </summary>
    class BrutalPackage
    {
        /// <summary>
        ///     Occurs whenever the status of the extraction or insertion changes (a new file is processed).
        /// </summary>
        public static event EventHandler<BrutalStatusReport> OnStatusReport;

        /// <summary>
        ///     Type of the compression used on the file.
        /// </summary>
        private enum CompressionType
        {
            None = 1,
            ZLib = 2,
            LZX = 3
        }

        /// <summary>
        ///     Extracts data from a Brutal Legend package.
        /// </summary>
        /// <param name="HeaderFile">The *.~h Header file path</param>
        /// <param name="DataFile">The ~.~p Data file path</param>
        /// <param name="OutFolder">The output folder</param>
        /// <returns>False if header file is invalid, true otherwise</returns>
        public static bool Extract(string HeaderFile, string DataFile, string OutFolder)
        {
            FileStream Header = new FileStream(HeaderFile, FileMode.Open);
            FileStream Data = new FileStream(DataFile, FileMode.Open);

            EndianBinaryReader Reader = new EndianBinaryReader(Header, Endian.Big);

            if (StringUtilities.ReadASCIIString(Header, 4) != "dfpf") return false;

            Header.Seek(0x14, SeekOrigin.Begin);
            uint StringsTableOffset = Reader.ReadUInt32();
            uint StringsTableFlags = Reader.ReadUInt32();
            uint StringsTableLength = Reader.ReadUInt32();
            uint FilesCount = Reader.ReadUInt32();

            Header.Seek(0x18, SeekOrigin.Current);
            uint FilesTableOffset = Reader.ReadUInt32();

            for (int Index = 0; Index < FilesCount; Index++)
            {
                Header.Seek(FilesTableOffset + Index * 0x10, SeekOrigin.Begin);

                //Lengths
                uint DecompressedLength = Reader.ReadUInt24();
                uint LengthDifference = (uint)(Reader.ReadUInt16() << 1) | ((DecompressedLength & 1) << 17); //Probably for "obfuscation"
                uint CompressedLength = Reader.ReadUInt24();

                LengthDifference |= CompressedLength >> 23;
                DecompressedLength = (DecompressedLength >> 1) + LengthDifference;
                CompressedLength = (CompressedLength & 0x7fffff) >> 1;

                //Offsets
                uint DataOffset = Reader.ReadUInt24() << 5;
                byte DataFormat = Reader.ReadByte();
                DataOffset |= (uint)(DataFormat & 0xf8) >> 3;
                DataFormat &= 7;

                uint NameOffset = (Reader.ReadUInt24() >> 3) + StringsTableOffset;
                byte Flags = Reader.ReadByte();

                Header.Seek(NameOffset, SeekOrigin.Begin);
                string FileName = StringUtilities.ReadASCIIString(Header);

                if (OnStatusReport != null)
                {
                    BrutalStatusReport Report = new BrutalStatusReport();

                    Report.Status = "Extracting " + FileName + "...";
                    Report.ProcessedFiles = Index;
                    Report.TotalFiles = (int)FilesCount;

                    OnStatusReport(null, Report);
                }

                Data.Seek(DataOffset, SeekOrigin.Begin);
                byte[] Buffer = new byte[CompressedLength];
                Data.Read(Buffer, 0, Buffer.Length);

                ICompression Decompressor;
                switch ((CompressionType)((Flags >> 1) & 3))
                {
                    case CompressionType.None: Decompressor = new NoCompression(); break;
                    case CompressionType.ZLib: Decompressor = new ZLib(); break;
                    case CompressionType.LZX: Decompressor = new LZX(); break;
                    default: throw new Exception("Unknown compression!");
                }

                Buffer = Decompressor.Decompress(Buffer, DecompressedLength);

                string FullName = Path.Combine(OutFolder, FileName);
                string DirName = Path.GetDirectoryName(FullName);
                if (!Directory.Exists(DirName)) Directory.CreateDirectory(DirName);
                File.WriteAllBytes(FullName + ".bin", Buffer);
            }

            Header.Close();
            Data.Close();

            return true;
        }

        /// <summary>
        ///     Inserts data into a Brutal Legend package.
        /// </summary>
        /// <param name="HeaderFile">The *.~h Header file path</param>
        /// <param name="DataFile">The ~.~p Data file path</param>
        /// <param name="OutFolder">The input folder</param>
        /// <returns>False if header file is invalid, true otherwise</returns>
        public static bool Insert(string HeaderFile, string DataFile, string FileFolder)
        {
            FileStream Header = new FileStream(HeaderFile, FileMode.Open);
            FileStream Data = new FileStream(DataFile, FileMode.Open);

            EndianBinaryReader Reader = new EndianBinaryReader(Header, Endian.Big);
            EndianBinaryWriter Writer = new EndianBinaryWriter(Header, Endian.Big);

            if (StringUtilities.ReadASCIIString(Header, 4) != "dfpf") return false;
            string[] Files = Directory.GetFiles(FileFolder, "*.*", SearchOption.AllDirectories);

            Header.Seek(0x14, SeekOrigin.Begin);
            uint StringsTableOffset = Reader.ReadUInt32();
            uint StringsTableFlags = Reader.ReadUInt32();
            uint StringsTableLength = Reader.ReadUInt32();
            uint FilesCount = Reader.ReadUInt32();

            Header.Seek(0x18, SeekOrigin.Current);
            uint FilesTableOffset = Reader.ReadUInt32();

            using (MemoryStream NewData = new MemoryStream())
            {
                int Offset = 0;
                int InsertedFiles = 0;

                for (int Index = 0; Index < FilesCount; Index++)
                {
                    Header.Seek(FilesTableOffset + Index * 0x10, SeekOrigin.Begin);

                    //Lengths
                    uint DecompressedLength = Reader.ReadUInt24();
                    uint LengthDifference = (uint)(Reader.ReadUInt16() << 1) | ((DecompressedLength & 1) << 17); //Probably for "obfuscation"
                    uint CompressedLength = Reader.ReadUInt24();

                    LengthDifference |= CompressedLength >> 23;
                    DecompressedLength = (DecompressedLength >> 1) + LengthDifference;
                    CompressedLength = (CompressedLength & 0x7fffff) >> 1;

                    //Offsets
                    uint DataOffset = Reader.ReadUInt24() << 5;
                    byte DataFormat = Reader.ReadByte();
                    DataOffset |= (uint)(DataFormat & 0xf8) >> 3;
                    DataFormat &= 7;

                    uint NameOffset = (Reader.ReadUInt24() >> 3) + StringsTableOffset;
                    byte Flags = Reader.ReadByte();

                    Header.Seek(NameOffset, SeekOrigin.Begin);
                    string FileName = StringUtilities.ReadASCIIString(Header);

                    bool Found = false;
                    foreach (string CurrentFile in Files)
                    {
                        string Name = CurrentFile.Replace(FileFolder, string.Empty);
                        if (Name.StartsWith("\\")) Name = Name.Remove(0, 1);
                        Name = Name.Replace(Path.GetExtension(Name), string.Empty);
                        Name = Name.Replace('\\', '/');

                        if (Name == FileName)
                        {
                            if (OnStatusReport != null)
                            {
                                BrutalStatusReport Report = new BrutalStatusReport();

                                Report.Status = "Inserting " + FileName + "...";
                                Report.ProcessedFiles = InsertedFiles++;
                                Report.TotalFiles = Files.Length;

                                OnStatusReport(null, Report);
                            }

                            ICompression Compressor;
                            switch ((CompressionType)((Flags >> 1) & 3))
                            {
                                case CompressionType.None: Compressor = new NoCompression(); break;
                                case CompressionType.ZLib: Compressor = new ZLib(); break;
                                case CompressionType.LZX: Compressor = new LZX(); break;
                                default: throw new Exception("Unknown compression!");
                            }

                            byte[] Decompressed = File.ReadAllBytes(CurrentFile);
                            byte[] Compressed = Compressor.Compress(Decompressed);

                            NewData.Seek(Offset, SeekOrigin.Begin);
                            NewData.Write(Compressed, 0, Compressed.Length);

                            Header.Seek(FilesTableOffset + Index * 0x10, SeekOrigin.Begin);
                            Writer.Write24((uint)(((Decompressed.Length - LengthDifference) << 1) | (LengthDifference >> 17)));
                            Header.Seek(2, SeekOrigin.Current);
                            Writer.Write24((uint)((Compressed.Length << 1) & 0x7fffff) | ((LengthDifference & 1) << 23));
                            Writer.Write24((uint)(Offset >> 5));
                            Writer.Write(DataFormat);
                            Offset += Compressed.Length;

                            Found = true;
                            break;
                        }
                    }

                    if (!Found)
                    {
                        Data.Seek(DataOffset, SeekOrigin.Begin);
                        byte[] Buffer = new byte[CompressedLength];
                        Data.Read(Buffer, 0, Buffer.Length);

                        NewData.Seek(Offset, SeekOrigin.Begin);
                        NewData.Write(Buffer, 0, Buffer.Length);

                        Header.Seek(FilesTableOffset + Index * 0x10 + 8, SeekOrigin.Begin);
                        Writer.Write24((uint)(Offset >> 5));
                        Offset += Buffer.Length;
                    }

                    //Align offset to next 2KB block if necessary
                    if ((Offset & 0x7ff) != 0) Offset = (Offset & ~0x7ff) + 0x800;
                }

                Header.Close();
                Data.Close();

                while ((NewData.Position & 0x7ff) != 0) NewData.WriteByte(0);
                File.WriteAllBytes(DataFile, NewData.ToArray());
            }

            return true;
        }
    }
}
