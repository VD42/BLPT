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

                uint DecompressedLength = Reader.ReadUInt24();
                uint NameOffset = Reader.ReadUInt24();
                uint Something3 = NameOffset & 0x07;
                NameOffset = (NameOffset >> 3) + StringsTableOffset;
                uint DataFormat = Reader.ReadUInt16();
                uint DataOffset = Reader.ReadUInt24() << 5;
                byte Something = Reader.ReadByte();
                uint CompressedLength = Reader.ReadUInt24();
                uint Something2 = CompressedLength & 0x0F;
                CompressedLength = CompressedLength >> 4;
                byte Flags = Reader.ReadByte();

                if (CompressedLength > DecompressedLength)
                    throw new Exception("Something wrong!");

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
                if ((Flags & 0x08) > 0)
                    Decompressor = new ZLib();
                else
                    Decompressor = new NoCompression();

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

                    uint DecompressedLength = Reader.ReadUInt24();
                    uint NameOffset = Reader.ReadUInt24();
                    uint Something3 = NameOffset & 0x07;
                    NameOffset = (NameOffset >> 3) + StringsTableOffset;
                    uint DataFormat = Reader.ReadUInt16();
                    uint DataOffset = Reader.ReadUInt24() << 5;
                    byte Something = Reader.ReadByte();
                    uint CompressedLength = Reader.ReadUInt24();
                    uint Something2 = CompressedLength & 0x0F;
                    CompressedLength = CompressedLength >> 4;
                    byte Flags = Reader.ReadByte();

                    if (CompressedLength > DecompressedLength)
                        throw new Exception("Something wrong!");

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
                            if ((Flags & 0x08) > 0)
                                Compressor = new ZLib();
                            else
                                Compressor = new NoCompression();

                            byte[] Decompressed = File.ReadAllBytes(CurrentFile);
                            byte[] Compressed = Compressor.Compress(Decompressed);

                            if ((Flags & 0x08) == 0 && (uint)Decompressed.Length == DecompressedLength)
                            {
                                if ((uint)Compressed.Length != CompressedLength)
                                    throw new Exception("Bad length!");
                            }

                            if ((uint)Compressed.Length > (uint)Decompressed.Length)
                                throw new Exception("Something wrong!");

                            NewData.Seek(Offset, SeekOrigin.Begin);
                            NewData.Write(Compressed, 0, Compressed.Length);

                            Header.Seek(FilesTableOffset + Index * 0x10, SeekOrigin.Begin);
                            Writer.Write24((uint)Decompressed.Length);
                            Header.Seek(5, SeekOrigin.Current);
                            Writer.Write24((uint)(Offset >> 5));
                            Header.Seek(1, SeekOrigin.Current);
                            Writer.Write24((((uint)Compressed.Length << 4) | Something2));

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
