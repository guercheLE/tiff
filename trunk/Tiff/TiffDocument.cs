using System;
using System.Collections.Generic;
using System.IO;

namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public class TiffDocument
    {
        Header _header;
        public Header Header
        {
            get { return _header; }
            set { _header = value; }
        }

        List<ImageFile> _imageFiles;
        public List<ImageFile> ImageFiles
        {
            get { return _imageFiles; }
            set { _imageFiles = value; }
        }

        public static TiffDocument Load(string inputFileName)
        {
            using (TiffReader TiffReader = new TiffReader(inputFileName))
            {
                return Load(TiffReader);
            }
        }

        public static TiffDocument Load(Stream inputStream)
        {
            using (TiffReader TiffReader = new TiffReader(inputStream))
            {
                return Load(TiffReader);
            }
        }

        public static TiffDocument Load(TiffReader inputTiffReader)
        {
            return inputTiffReader.ReadDocument();
        }

        public void Save(string outputFileName)
        {
            using (TiffWriter TiffWriter = new TiffWriter(outputFileName, BitConverter.IsLittleEndian ? this.Header.ByteOrder == ByteOrder.BigEndian : this.Header.ByteOrder == ByteOrder.LittleEndian))
            {
                Save(TiffWriter);
            }
        }

        public void Save(Stream outputStream)
        {
            using (TiffWriter TiffWriter = new TiffWriter(outputStream, BitConverter.IsLittleEndian ? this.Header.ByteOrder == ByteOrder.BigEndian : this.Header.ByteOrder == ByteOrder.LittleEndian))
            {
                Save(TiffWriter);
            }
        }

        public void Save(TiffWriter outputTiffWriter)
        {
            outputTiffWriter.WriteDocument(this);
            outputTiffWriter.BaseStream.Flush();
        }
    }
}
