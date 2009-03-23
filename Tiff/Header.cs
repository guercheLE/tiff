using System;

namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public class Header
    {
        uint _offset;
        public uint Offset
        {
            get { return _offset; }
            internal set { _offset = value; }
        }

        ByteOrder _byteOrder;
        public ByteOrder ByteOrder
        {
            get { return _byteOrder; }
            set
            {
                if (value != ByteOrder.LittleEndian && value != ByteOrder.BigEndian)
                {
                    throw new ArgumentOutOfRangeException("ByteOrder");
                }
                _byteOrder = value;
            }
        }

        byte _signature;
        public byte Signature
        {
            get { return _signature; }
            internal set
            {
                if (value != 0x2A)
                {
                    throw new ArgumentOutOfRangeException("Signature");
                }
                _signature = value;
            }
        }

        uint _offsetOfFirstImageFile;
        public uint OffsetOfFirstImageFile
        {
            get { return _offsetOfFirstImageFile; }
            internal set { _offsetOfFirstImageFile = value; }
        }
    }
}
