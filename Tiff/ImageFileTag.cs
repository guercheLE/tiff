using System.Collections.Generic;

namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public class ImageFileTag
    {
        uint _offset;
        public uint Offset
        {
            get { return _offset; }
            internal set { _offset = value; }
        }

        TagCode _code;
        public TagCode Code
        {
            get { return _code; }
            set { _code = value; }
        }

        TagType _type;
        public TagType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        uint _count;
        public uint Count
        {
            get { return _count; }
            set { _count = value; }
        }

        uint _valuesOffset;
        public uint ValuesOffset
        {
            get { return _valuesOffset; }
            internal set { _valuesOffset = value; }
        }

        List<byte[]> _values = new List<byte[]>();
        public List<byte[]> Values
        {
            get { return _values; }
            internal set { _values = value; }
        }

        public byte[] ValuesJoined
        {
            get
            {
                int Allocation = 0;

                for (int ValueIndex = 0; ValueIndex <= _values.Count - 1; ValueIndex++)
                {
                    Allocation += _values[ValueIndex].Length;
                }

                int ValueToReturnIndex = 0;
                byte[] ValueToReturn = new byte[Allocation];

                for (int ValueIndex = 0; ValueIndex <= _values.Count - 1; ValueIndex++)
                {
                    _values[ValueIndex].CopyTo(ValueToReturn, ValueToReturnIndex);
                    ValueToReturnIndex += _values[ValueIndex].Length;
                }

                return ValueToReturn;
            }
        }
    }
}
