using System;
using System.Collections.Generic;
using System.IO;

namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public class TiffWriter : IDisposable
    {
        const uint _headerOffset = 0;

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

        private TiffWriter()
        {
        }

        public TiffWriter(string fileName, bool reverseBytes)
            : this(File.Open(fileName, FileMode.CreateNew), reverseBytes)
        {
            _closeBaseStream = true;
        }

        public TiffWriter(Stream stream, bool reverseBytes)
        {
            _baseStream = stream;
            _reverseBytes = reverseBytes;
            _closeBaseStream = false;
        }

        public void WriteDocument(TiffDocument tiffDocument)
        {
            WriteImageFiles(tiffDocument.ImageFiles);

            tiffDocument.Header.OffsetOfFirstImageFile = tiffDocument.ImageFiles[0].Offset;
            WriteHeader(tiffDocument.Header);
        }

        public void WriteHeader(Header header)
        {
            _baseStream.Seek(_headerOffset, SeekOrigin.Begin);
            _baseStream.Write(BitConverter.GetBytes((ushort)header.ByteOrder), 0, 2);

            _baseStream.Seek(_headerOffset + 2, SeekOrigin.Begin);
            _baseStream.Write(BitConverter.GetBytes(header.Signature), 0, 1);

            _baseStream.Seek(_headerOffset + 4, SeekOrigin.Begin);
            _baseStream.Write(_reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes(header.OffsetOfFirstImageFile)) : BitConverter.GetBytes(header.OffsetOfFirstImageFile), 0, 4);
        }

        public void WriteImageFiles(List<ImageFile> imageFiles)
        {
            uint ImageFileOffset = 8;

            for (int ImageFileIndex = 0; ImageFileIndex <= imageFiles.Count - 1; ImageFileIndex++)
            {
                WriteImageFile(ref ImageFileOffset, imageFiles[ImageFileIndex], ImageFileIndex == imageFiles.Count - 1);
            }

            for (int ImageFileIndex = 0; ImageFileIndex <= imageFiles.Count - 1; ImageFileIndex++)
            {
                if (ImageFileIndex == imageFiles.Count - 1)
                {
                    imageFiles[ImageFileIndex].OffsetOfNextImageFile = 0;
                }
                else
                {
                    imageFiles[ImageFileIndex].OffsetOfNextImageFile = imageFiles[ImageFileIndex + 1].Offset;
                }

                WriteImageFileOffsetOfNextImageFile((uint)(imageFiles[ImageFileIndex].Offset + 2 + (imageFiles[ImageFileIndex].Tags.Count * 12)), imageFiles[ImageFileIndex].OffsetOfNextImageFile);
            }
        }

        public void WriteImageFile(ref uint offset, ImageFile imageFile, bool isLastImageFile)
        {
            //ATTENTION: Do not change order of execution because each call updates information used by next calls

            if (imageFile.Tiles.Count > 0)
            {
                WriteImageFileTiles(ref offset, imageFile.Tiles, imageFile.Tags[TagCode.TileOffsets], imageFile.Tags[TagCode.TileByteCounts]);
            }

            if (imageFile.Strips.Count > 0)
            {
                WriteImageFileStrips(ref offset, imageFile.Strips, imageFile.Tags[TagCode.StripOffsets], imageFile.Tags[TagCode.StripByteCounts]);
            }

            WriteImageFileTagsSpecialValues(ref offset, imageFile.Tags);

            if ((offset % 2) != 0) { offset += 1; } // to file offset start on an even offset
            imageFile.Offset = offset;

            _baseStream.Seek(imageFile.Offset, SeekOrigin.Begin);
            _baseStream.Write(_reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes(imageFile.Tags.Count)) : BitConverter.GetBytes(imageFile.Tags.Count), 0, 2);
            offset += 2;

            WriteImageFileTags(ref offset, imageFile.Tags);
            offset += 4; // to skip space allocated to OffsetOfNextImageFile
        }

        private void WriteImageFileOffsetOfNextImageFile(uint offset, uint offsetOfNextImageFile)
        {
            _baseStream.Seek(offset, SeekOrigin.Begin);
            _baseStream.Write(_reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes(offsetOfNextImageFile)) : BitConverter.GetBytes(offsetOfNextImageFile), 0, 4);
        }

        public void WriteImageFileTiles(ref uint offsetOfFirstImageFileTile, List<ImageFileTile> imageFileTiles, ImageFileTag ImageFileTagTileOffsets, ImageFileTag ImageFileTagTileByteCounts)
        {
            uint ImageFileStripOffset = offsetOfFirstImageFileTile;

            for (int ImageFileTileIndex = 0; ImageFileTileIndex <= imageFileTiles.Count - 1; ImageFileTileIndex++)
            {
                ImageFileTagTileOffsets.Values[ImageFileTileIndex] = _reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes(ImageFileStripOffset)) : BitConverter.GetBytes(ImageFileStripOffset);
                imageFileTiles[ImageFileTileIndex].BinaryContentOffset = ImageFileStripOffset;

                _baseStream.Seek(ImageFileStripOffset, SeekOrigin.Begin);
                _baseStream.Write(imageFileTiles[ImageFileTileIndex].BinaryContent, 0, imageFileTiles[ImageFileTileIndex].BinaryContent.Length);

                ImageFileStripOffset += (uint)imageFileTiles[ImageFileTileIndex].BinaryContent.Length;
            }

            offsetOfFirstImageFileTile = ImageFileStripOffset;
        }

        public void WriteImageFileStrips(ref uint offsetOfFirstImageFileStrip, List<ImageFileStrip> imageFileStrips, ImageFileTag ImageFileTagStripOffsets, ImageFileTag ImageFileTagStripByteCounts)
        {
            uint ImageFileStripOffset = offsetOfFirstImageFileStrip;

            for (int ImageFileStripIndex = 0; ImageFileStripIndex <= imageFileStrips.Count - 1; ImageFileStripIndex++)
            {
                ImageFileTagStripOffsets.Values[ImageFileStripIndex] = _reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes(ImageFileStripOffset)) : BitConverter.GetBytes(ImageFileStripOffset);
                imageFileStrips[ImageFileStripIndex].BinaryContentOffset = ImageFileStripOffset;

                _baseStream.Seek(ImageFileStripOffset, SeekOrigin.Begin);
                _baseStream.Write(imageFileStrips[ImageFileStripIndex].BinaryContent, 0, imageFileStrips[ImageFileStripIndex].BinaryContent.Length);

                ImageFileStripOffset += (uint)imageFileStrips[ImageFileStripIndex].BinaryContent.Length;
            }

            offsetOfFirstImageFileStrip = ImageFileStripOffset;
        }

        public void WriteImageFileTagsSpecialValues(ref uint offsetOfFirstImageFileTagSpecialValue, Dictionary<TagCode, ImageFileTag> imageFileTags)
        {
            uint ImageFileTagSpecialValueOffset = offsetOfFirstImageFileTagSpecialValue;

            foreach (ImageFileTag ImageFileTag in imageFileTags.Values)
            {
                if (ImageFileTag.Count > 1 || (byte)Enum.Parse(typeof(TagTypeAllocation), Enum.GetName(typeof(TagType), ImageFileTag.Type)) > 4)
                {
                    ImageFileTag.ValuesOffset = ImageFileTagSpecialValueOffset;

                    foreach (byte[] Value in ImageFileTag.Values)
                    {
                        _baseStream.Seek(ImageFileTagSpecialValueOffset, SeekOrigin.Begin);
                        _baseStream.Write(_reverseBytes ? ByteArray.Reverse(Value) : Value, 0, Value.Length);

                        ImageFileTagSpecialValueOffset += (uint)Value.Length;
                    }
                }
            }

            offsetOfFirstImageFileTagSpecialValue = ImageFileTagSpecialValueOffset;
        }

        public void WriteImageFileTags(ref uint offsetOfFirstImageFileTag, Dictionary<TagCode, ImageFileTag> imageFileTags)
        {
            uint ImageFileTagOffset = offsetOfFirstImageFileTag;

            foreach (ImageFileTag ImageFileTag in imageFileTags.Values)
            {
                WriteImageFileTag(ref ImageFileTagOffset, ImageFileTag);
            }

            offsetOfFirstImageFileTag = ImageFileTagOffset;
        }

        public void WriteImageFileTag(ref uint offsetOfImageFileTag, ImageFileTag imageFileTag)
        {
            imageFileTag.Offset = offsetOfImageFileTag;

            _baseStream.Seek(offsetOfImageFileTag, SeekOrigin.Begin);
            _baseStream.Write(_reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes((ushort)imageFileTag.Code)) : BitConverter.GetBytes((ushort)imageFileTag.Code), 0, 2);

            _baseStream.Seek(offsetOfImageFileTag + 2, SeekOrigin.Begin);
            _baseStream.Write(_reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes((ushort)imageFileTag.Type)) : BitConverter.GetBytes((ushort)imageFileTag.Type), 0, 2);

            _baseStream.Seek(offsetOfImageFileTag + 4, SeekOrigin.Begin);
            _baseStream.Write(_reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes(imageFileTag.Count)) : BitConverter.GetBytes(imageFileTag.Count), 0, 4);

            _baseStream.Seek(offsetOfImageFileTag + 8, SeekOrigin.Begin);
            if (imageFileTag.Count > 1 || (byte)Enum.Parse(typeof(TagTypeAllocation), Enum.GetName(typeof(TagType), imageFileTag.Type)) > 4)
            {
                _baseStream.Write(_reverseBytes ? ByteArray.Reverse(BitConverter.GetBytes(imageFileTag.ValuesOffset)) : BitConverter.GetBytes(imageFileTag.ValuesOffset), 0, 4);
            }
            else
            {
                _baseStream.Write(_reverseBytes ? ByteArray.Reverse(imageFileTag.Values[0]) : imageFileTag.Values[0], 0, imageFileTag.Values[0].Length);
            }

            offsetOfImageFileTag += 12;
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
