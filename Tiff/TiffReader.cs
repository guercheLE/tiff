using System;
using System.Collections.Generic;
using System.IO;

namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public class TiffReader : IDisposable
    {
        bool _closeBaseStream;

        Stream _baseStream;
        public Stream BaseStream
        {
            get { return _baseStream; }
        }

        bool _reverseBytes;
        public bool ReverseBytes
        {
            get { return _reverseBytes; }
        }

        private TiffReader()
        {
        }

        public TiffReader(string fileName)
            : this(File.Open(fileName, FileMode.Open))
        {
            _closeBaseStream = true;
        }

        public TiffReader(Stream stream)
        {
            _baseStream = stream;
            _closeBaseStream = false;
            ByteOrder ByteOrder = ReadHeader().ByteOrder;
            _reverseBytes = BitConverter.IsLittleEndian ? ByteOrder == ByteOrder.BigEndian : ByteOrder == ByteOrder.LittleEndian;
        }

        public TiffDocument ReadDocument()
        {
            TiffDocument ValueToReturn = new TiffDocument();

            ValueToReturn.Header = ReadHeader();
            ValueToReturn.ImageFiles = ReadImageFiles(ValueToReturn.Header.OffsetOfFirstImageFile);

            return ValueToReturn;
        }

        public Header ReadHeader()
        {
            Header ValueToReturn = new Header();
            byte[] Buffer;
            uint HeaderOffset = 0;

            ValueToReturn.Offset = HeaderOffset;

            _baseStream.Seek(HeaderOffset, SeekOrigin.Begin);
            Buffer = new byte[2];
            _baseStream.Read(Buffer, 0, 2);
            ValueToReturn.ByteOrder = (ByteOrder)BitConverter.ToUInt16(Buffer, 0);

            _baseStream.Seek(HeaderOffset + 2, SeekOrigin.Begin);
            Buffer = new byte[1];
            _baseStream.Read(Buffer, 0, 1);
            ValueToReturn.Signature = Buffer[0];

            _baseStream.Seek(HeaderOffset + 4, SeekOrigin.Begin);
            Buffer = new byte[4];
            _baseStream.Read(Buffer, 0, 4);
            ValueToReturn.OffsetOfFirstImageFile = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer, 0);

            return ValueToReturn;
        }

        public List<ImageFile> ReadImageFiles(uint offsetOfFirstImageFile)
        {
            List<ImageFile> ValueToReturn = new List<ImageFile>();
            uint Offset = offsetOfFirstImageFile;

            while (Offset != 0)
            {
                ImageFile ImageFile = ReadImageFile(ref Offset);
                ValueToReturn.Add(ImageFile);
            }

            return ValueToReturn;
        }

        public ImageFile ReadImageFile(ref uint ImageFileOffset)
        {
            ImageFile ValueToReturn = new ImageFile();
            byte[] Buffer;
            uint Offset = ImageFileOffset;

            ValueToReturn.Offset = Offset;

            _baseStream.Seek(Offset, SeekOrigin.Begin);
            Buffer = new byte[2];
            _baseStream.Read(Buffer, 0, 2);
            ValueToReturn.TagsCount = BitConverter.ToUInt16(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer, 0);
            Offset += 2;

            ValueToReturn.Tags = ReadImageFileTags(ref Offset, ValueToReturn.TagsCount);

            _baseStream.Seek(Offset, SeekOrigin.Begin);
            Buffer = new byte[4];
            _baseStream.Read(Buffer, 0, 4);
            ValueToReturn.OffsetOfNextImageFile = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer, 0);
            Offset += 4;

            if (ValueToReturn.Tags.ContainsKey(TagCode.TileOffsets) && ValueToReturn.Tags.ContainsKey(TagCode.TileByteCounts))
            {
                ValueToReturn.Tiles = ReadImageFileTiles(ValueToReturn.Tags[TagCode.TileOffsets], ValueToReturn.Tags[TagCode.TileByteCounts]);
            }

            if (ValueToReturn.Tags.ContainsKey(TagCode.StripOffsets) && ValueToReturn.Tags.ContainsKey(TagCode.StripByteCounts))
            {
                ValueToReturn.Strips = ReadImageFileStrips(ValueToReturn.Tags[TagCode.StripOffsets], ValueToReturn.Tags[TagCode.StripByteCounts]);
            }

            ImageFileOffset = ValueToReturn.OffsetOfNextImageFile;

            return ValueToReturn;
        }

        public Dictionary<TagCode, ImageFileTag> ReadImageFileTags(ref uint offsetOfFirstImageFileTag, ushort imageFileTagsCount)
        {
            Dictionary<TagCode, ImageFileTag> ValueToReturn = new Dictionary<TagCode, ImageFileTag>();
            uint OffsetOfImageFileTag = offsetOfFirstImageFileTag;

            for (ushort ImageFileTagIndex = 0; ImageFileTagIndex <= imageFileTagsCount - 1; ImageFileTagIndex++)
            {
                ImageFileTag ImageFileTag = ReadImageFileTag(ref OffsetOfImageFileTag);
                ValueToReturn.Add(ImageFileTag.Code, ImageFileTag);
            }

            offsetOfFirstImageFileTag = OffsetOfImageFileTag;

            return ValueToReturn;
        }

        public ImageFileTag ReadImageFileTag(ref uint offsetOfImageFileTag)
        {
            byte[] Buffer;
            TagTypeAllocation TagTypeAllocation;
            ImageFileTag ValueToReturn = new ImageFileTag();

            ValueToReturn.Offset = offsetOfImageFileTag;

            _baseStream.Seek(offsetOfImageFileTag, SeekOrigin.Begin);
            Buffer = new byte[2];
            _baseStream.Read(Buffer, 0, 2);
            ValueToReturn.Code = (TagCode)BitConverter.ToUInt16(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer, 0);

            _baseStream.Seek(offsetOfImageFileTag + 2, SeekOrigin.Begin);
            Buffer = new byte[2];
            _baseStream.Read(Buffer, 0, 2);
            ValueToReturn.Type = (TagType)BitConverter.ToUInt16(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer, 0);
            TagTypeAllocation = (TagTypeAllocation)Enum.Parse(typeof(TagTypeAllocation), Enum.GetName(typeof(TagType), ValueToReturn.Type), true);

            _baseStream.Seek(offsetOfImageFileTag + 4, SeekOrigin.Begin);
            Buffer = new byte[4];
            _baseStream.Read(Buffer, 0, 4);
            ValueToReturn.Count = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer, 0);

            if (ValueToReturn.Count > 1 || (uint)TagTypeAllocation > 4)
            {
                _baseStream.Seek(offsetOfImageFileTag + 8, SeekOrigin.Begin);
                Buffer = new byte[4];
                _baseStream.Read(Buffer, 0, 4);
                ValueToReturn.ValuesOffset = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer, 0);
            }
            else
            {
                ValueToReturn.ValuesOffset = offsetOfImageFileTag + 8;
            }

            _baseStream.Seek(ValueToReturn.ValuesOffset, SeekOrigin.Begin);
            for (uint ValueIndex = 0; ValueIndex <= ValueToReturn.Count - 1; ValueIndex++)
            {
                Buffer = new byte[ValueToReturn.Count == 1 && (int)TagTypeAllocation < 4 ? 4 : (int)TagTypeAllocation];
                _baseStream.Read(Buffer, 0, Buffer.Length);
                ValueToReturn.Values.Add(ReverseBytes ? ByteArray.Reverse(Buffer) : Buffer);
            }

            offsetOfImageFileTag += 12;

            return ValueToReturn;
        }

        public List<ImageFileTile> ReadImageFileTiles(ImageFileTag ImageFileTagTileOffsets, ImageFileTag ImageFileTagTileByteCounts)
        {
            uint Offset;
            uint Count;
            byte[] Buffer;
            ImageFileTile ImageFileTile;
            List<ImageFileTile> ValueToReturn = new List<ImageFileTile>();

            for (int TileOffsetIndex = 0; TileOffsetIndex <= ImageFileTagTileOffsets.Values.Count - 1; TileOffsetIndex++)
            {
                Offset = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(ImageFileTagTileOffsets.Values[TileOffsetIndex]) : ImageFileTagTileOffsets.Values[TileOffsetIndex], 0);
                Count = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(ImageFileTagTileByteCounts.Values[TileOffsetIndex]) : ImageFileTagTileByteCounts.Values[TileOffsetIndex], 0);

                _baseStream.Seek(Offset, SeekOrigin.Begin);
                Buffer = new byte[Count];
                _baseStream.Read(Buffer, 0, (int)Count);

                ImageFileTile = new ImageFileTile();
                ImageFileTile.BinaryContentOffset = Offset;
                ImageFileTile.BinaryContent = Buffer;

                ValueToReturn.Add(ImageFileTile);
            }

            return ValueToReturn;
        }

        public List<ImageFileStrip> ReadImageFileStrips(ImageFileTag ImageFileTagStripOffsets, ImageFileTag ImageFileTagStripByteCounts)
        {
            uint Offset;
            uint Count;
            byte[] Buffer;
            ImageFileStrip ImageFileStrip;
            List<ImageFileStrip> ValueToReturn = new List<ImageFileStrip>();

            for (int StripOffsetIndex = 0; StripOffsetIndex <= ImageFileTagStripOffsets.Values.Count - 1; StripOffsetIndex++)
            {
                Offset = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(ImageFileTagStripOffsets.Values[StripOffsetIndex]) : ImageFileTagStripOffsets.Values[StripOffsetIndex], 0);
                Count = BitConverter.ToUInt32(ReverseBytes ? ByteArray.Reverse(ImageFileTagStripByteCounts.Values[StripOffsetIndex]) : ImageFileTagStripByteCounts.Values[StripOffsetIndex], 0);

                _baseStream.Seek(Offset, SeekOrigin.Begin);
                Buffer = new byte[Count];
                _baseStream.Read(Buffer, 0, (int)Count);

                ImageFileStrip = new ImageFileStrip();
                ImageFileStrip.BinaryContentOffset = Offset;
                ImageFileStrip.BinaryContent = Buffer;

                ValueToReturn.Add(ImageFileStrip);
            }

            return ValueToReturn;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _baseStream.Flush();
            if (_closeBaseStream)
            {
                _baseStream.Close();
            }
        }

        #endregion
    }
}
